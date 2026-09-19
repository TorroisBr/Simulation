using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World Simulation/Simulation Config")]
public class SimulationConfigData : ScriptableObject
{
    public string simulationName;
    public CalendarDefinition calendar = new CalendarDefinition();
    public List<SimulationModule> enabledModules = new List<SimulationModule>();
    public List<CityData> cities = new List<CityData>();
    public List<ExplorableSiteConfig> explorableSites = new List<ExplorableSiteConfig>();
    public List<NpcSimulationConfig> npcs = new List<NpcSimulationConfig>();
    public List<NpcActionData> actions = new List<NpcActionData>();
    public List<NpcStatusData> statuses = new List<NpcStatusData>();
    public List<NpcJobData> jobs = new List<NpcJobData>();
    public List<InitialWantedRecordConfig> initialWarrants = new List<InitialWantedRecordConfig>();
    public List<ScheduledDirectiveConfig> scheduledDirectives = new List<ScheduledDirectiveConfig>();
    public float travelCostPerDay = 10f;
    public bool allowMerchantTradeRepositioning;
    public CommercialKnowledgeSettings commercialKnowledge = new CommercialKnowledgeSettings();
    public bool useFixedSimulationSeed;
    public int simulationSeed = 12345;
    public bool includeEconomySnapshots;
    public int economySnapshotIntervalDays = 10;
    public SimulationLogSettings logSettings = new SimulationLogSettings();

    [Header("Demography")]
    public bool naturalMortalityEnabled;
    [Range(0f, 1f)] public double naturalMortalityAnnualProbability;
    public bool aggregateDemographyEnabled;
    public double aggregateAnnualBirthRate;
    public double aggregateAnnualDeathRate;

    [Header("Status References")]
    public NpcStatusData freeStatus;
    public NpcStatusData wantedStatus;
    public NpcStatusData arrestedStatus;
    public NpcStatusData hiddenStatus;

    public CalendarDefinition Calendar => calendar;
    public List<SimulationModule> EnabledModules => enabledModules ?? (enabledModules = new List<SimulationModule>());
    public List<CityData> Cities => cities ?? (cities = new List<CityData>());
    public List<ExplorableSiteConfig> ExplorableSites => explorableSites ?? (explorableSites = new List<ExplorableSiteConfig>());
    public List<NpcSimulationConfig> Npcs => npcs ?? (npcs = new List<NpcSimulationConfig>());
    public List<NpcActionData> Actions => actions ?? (actions = new List<NpcActionData>());
    public List<NpcStatusData> Statuses => statuses ?? (statuses = new List<NpcStatusData>());
    public List<NpcJobData> Jobs => jobs ?? (jobs = new List<NpcJobData>());
    public List<InitialWantedRecordConfig> InitialWarrants => initialWarrants ?? (initialWarrants = new List<InitialWantedRecordConfig>());
    public List<ScheduledDirectiveConfig> ScheduledDirectives => scheduledDirectives ?? (scheduledDirectives = new List<ScheduledDirectiveConfig>());
    public CommercialKnowledgeSettings CommercialKnowledge => commercialKnowledge ?? (commercialKnowledge = new CommercialKnowledgeSettings());
    public SimulationLogSettings LogSettings => logSettings ?? (logSettings = new SimulationLogSettings());

    public bool NaturalMortalityEnabled => naturalMortalityEnabled;
    public double NaturalMortalityAnnualProbability => naturalMortalityAnnualProbability;
    public bool AggregateDemographyEnabled => aggregateDemographyEnabled;
    public double AggregateAnnualBirthRate => aggregateAnnualBirthRate;
    public double AggregateAnnualDeathRate => aggregateAnnualDeathRate;

    public SimulationConfigurationOverrides CreateDemographyConfigurationOverrides()
    {
        return new SimulationConfigurationOverrides(
            naturalMortality: new NaturalMortalityConfigurationOverrides(
                enabled: naturalMortalityEnabled,
                annualProbability: naturalMortalityAnnualProbability),
            aggregateDemography: new AggregateDemographyConfigurationOverrides(
                enabled: aggregateDemographyEnabled,
                annualBirthRate: aggregateAnnualBirthRate,
                annualDeathRate: aggregateAnnualDeathRate));
    }

    public bool HasModule(SimulationModule module)
    {
        return EnabledModules.Contains(module);
    }
}

public enum SimulationModule
{
    Economy,
    Merchant,
    GuardCrime,
    Crime
}

[Serializable]
public class ExplorableSiteConfig
{
    public ExplorableSiteData site;
    public CityData anchorCity;
    public int travelDaysFromAnchor = 1;
}

[Serializable]
public class NpcSimulationConfig
{
    public NpcData npc;
    public CityData startingCity;
    public float initialMoney = 100f;
    public List<NpcInitialInventoryItemConfig> initialInventory = new List<NpcInitialInventoryItemConfig>();
    public List<ExplorableSiteData> initialKnownExplorableSites = new List<ExplorableSiteData>();

    public List<NpcInitialInventoryItemConfig> InitialInventory => initialInventory ?? (initialInventory = new List<NpcInitialInventoryItemConfig>());
    public List<ExplorableSiteData> InitialKnownExplorableSites => initialKnownExplorableSites ?? (initialKnownExplorableSites = new List<ExplorableSiteData>());
}

[Serializable]
public class NpcInitialInventoryItemConfig
{
    public ItemData item;
    public int amount;
    public float averageUnitCost;
}

[Serializable]
public class InitialWantedRecordConfig
{
    public NpcData target;
    public CityData city;
    public float bounty = 100f;
    public int sentenceDays = 3;
}

[Serializable]
public class ScheduledDirectiveConfig
{
    public long absoluteDay = 1L;
    public ScheduledDirectiveMode mode = ScheduledDirectiveMode.RequestAction;
    public ScheduledDirectiveOperation operation = ScheduledDirectiveOperation.EscapePrison;
    public NpcData actor;
    public NpcActionData action;
}
