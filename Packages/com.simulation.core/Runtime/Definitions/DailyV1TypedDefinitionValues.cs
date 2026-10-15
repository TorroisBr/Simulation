using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Existing enum identity retained as its numeric value and Unity-compatible spelling.</summary>
public sealed class DailyV1EnumValue
{
    public int Value { get; }
    public string Spelling { get; }
    public DailyV1EnumValue(int value, string spelling) { Value = value; Spelling = spelling ?? string.Empty; }
}

internal static class DailyV1ValueCopies
{
    internal static IReadOnlyList<T> Copy<T>(IEnumerable<T> values) => values == null ? null : new ReadOnlyCollection<T>(new List<T>(values));
}

/// <summary>Immutable typed projection of CalendarDefinition from the selected source inventory.</summary>
public sealed class DailyV1CalendarDefinition
{
    public int monthsPerYear { get; }
    public int weeksPerMonth { get; }
    public int daysPerWeek { get; }
    public IReadOnlyList<int> monthLengths { get; }

    public DailyV1CalendarDefinition(int monthsPerYear, int weeksPerMonth, int daysPerWeek, IReadOnlyList<int> monthLengths)
    {
        this.monthsPerYear = monthsPerYear;
        this.weeksPerMonth = weeksPerMonth;
        this.daysPerWeek = daysPerWeek;
        this.monthLengths = DailyV1ValueCopies.Copy(monthLengths);
    }
}

/// <summary>Immutable typed projection of CapabilityAttributeModifier from the selected source inventory.</summary>
public sealed class DailyV1CapabilityAttributeModifier
{
    public string attribute { get; }
    public float additiveValue { get; }

    public DailyV1CapabilityAttributeModifier(string attribute, float additiveValue)
    {
        this.attribute = attribute;
        this.additiveValue = additiveValue;
    }
}

/// <summary>Immutable typed projection of CapabilityAttributeValue from the selected source inventory.</summary>
public sealed class DailyV1CapabilityAttributeValue
{
    public string attribute { get; }
    public float value { get; }

    public DailyV1CapabilityAttributeValue(string attribute, float value)
    {
        this.attribute = attribute;
        this.value = value;
    }
}

/// <summary>Immutable typed projection of CityConnection from the selected source inventory.</summary>
public sealed class DailyV1CityConnection
{
    public string destination { get; }
    public int travelDays { get; }

    public DailyV1CityConnection(string destination, int travelDays)
    {
        this.destination = destination;
        this.travelDays = travelDays;
    }
}

/// <summary>Immutable typed projection of CityData from the selected source inventory.</summary>
public sealed class DailyV1CityData
{
    public string id { get; }
    public string cityName { get; }
    public string settlementSemanticId { get; }
    public string materialFlowLocationId { get; }
    public string marketStoreSemanticId { get; }
    public DailyV1EnumValue materialFlowProfile { get; }
    public int initialPopulation { get; }
    public DailyV1MarketLiquidityConfig marketLiquidity { get; }
    public DailyV1PopulationConsumptionConfig populationConsumption { get; }
    public IReadOnlyList<DailyV1MarketItemConfig> marketItems { get; }
    public IReadOnlyList<DailyV1CityProductionConfig> productionConfigs { get; }
    public IReadOnlyList<DailyV1CityConnection> connections { get; }

    public DailyV1CityData(string id, string cityName, string settlementSemanticId, string materialFlowLocationId, string marketStoreSemanticId, DailyV1EnumValue materialFlowProfile, int initialPopulation, DailyV1MarketLiquidityConfig marketLiquidity, DailyV1PopulationConsumptionConfig populationConsumption, IReadOnlyList<DailyV1MarketItemConfig> marketItems, IReadOnlyList<DailyV1CityProductionConfig> productionConfigs, IReadOnlyList<DailyV1CityConnection> connections)
    {
        this.id = id;
        this.cityName = cityName;
        this.settlementSemanticId = settlementSemanticId;
        this.materialFlowLocationId = materialFlowLocationId;
        this.marketStoreSemanticId = marketStoreSemanticId;
        this.materialFlowProfile = materialFlowProfile;
        this.initialPopulation = initialPopulation;
        this.marketLiquidity = marketLiquidity;
        this.populationConsumption = populationConsumption;
        this.marketItems = DailyV1ValueCopies.Copy(marketItems);
        this.productionConfigs = DailyV1ValueCopies.Copy(productionConfigs);
        this.connections = DailyV1ValueCopies.Copy(connections);
    }
}

/// <summary>Immutable typed projection of CityProductionConfig from the selected source inventory.</summary>
public sealed class DailyV1CityProductionConfig
{
    public string item { get; }
    public int amountPerDay { get; }
    public int initialReserve { get; }
    public string productionSourceId { get; }
    public string contentRevision { get; }
    public DailyV1EnumValue sourceKind { get; }

    public DailyV1CityProductionConfig(string item, int amountPerDay, int initialReserve, string productionSourceId, string contentRevision, DailyV1EnumValue sourceKind)
    {
        this.item = item;
        this.amountPerDay = amountPerDay;
        this.initialReserve = initialReserve;
        this.productionSourceId = productionSourceId;
        this.contentRevision = contentRevision;
        this.sourceKind = sourceKind;
    }
}

/// <summary>Immutable typed projection of CommercialKnowledgeSettings from the selected source inventory.</summary>
public sealed class DailyV1CommercialKnowledgeSettings
{
    public int freshForDays { get; }
    public int maxUsefulAgeDays { get; }
    public int maxSharedObservationsPerInteraction { get; }

    public DailyV1CommercialKnowledgeSettings(int freshForDays, int maxUsefulAgeDays, int maxSharedObservationsPerInteraction)
    {
        this.freshForDays = freshForDays;
        this.maxUsefulAgeDays = maxUsefulAgeDays;
        this.maxSharedObservationsPerInteraction = maxSharedObservationsPerInteraction;
    }
}

/// <summary>Immutable typed projection of CrimeActionSettings from the selected source inventory.</summary>
public sealed class DailyV1CrimeActionSettings
{
    public int amount { get; }
    public float bounty { get; }
    public int sentenceDays { get; }
    public int hiddenDays { get; }
    public float escapeBountyPenalty { get; }
    public int failedEscapeSentencePenalty { get; }

    public DailyV1CrimeActionSettings(int amount, float bounty, int sentenceDays, int hiddenDays, float escapeBountyPenalty, int failedEscapeSentencePenalty)
    {
        this.amount = amount;
        this.bounty = bounty;
        this.sentenceDays = sentenceDays;
        this.hiddenDays = hiddenDays;
        this.escapeBountyPenalty = escapeBountyPenalty;
        this.failedEscapeSentencePenalty = failedEscapeSentencePenalty;
    }
}

/// <summary>Immutable typed projection of ExplorableSiteConfig from the selected source inventory.</summary>
public sealed class DailyV1ExplorableSiteConfig
{
    public string site { get; }
    public string anchorCity { get; }
    public int travelDaysFromAnchor { get; }
    public DailyV1EnumValue siteKind { get; }

    public DailyV1ExplorableSiteConfig(string site, string anchorCity, int travelDaysFromAnchor, DailyV1EnumValue siteKind)
    {
        this.site = site;
        this.anchorCity = anchorCity;
        this.travelDaysFromAnchor = travelDaysFromAnchor;
        this.siteKind = siteKind;
    }
}

/// <summary>Immutable typed projection of InitialWantedRecordConfig from the selected source inventory.</summary>
public sealed class DailyV1InitialWantedRecordConfig
{
    public string target { get; }
    public string city { get; }
    public float bounty { get; }
    public int sentenceDays { get; }

    public DailyV1InitialWantedRecordConfig(string target, string city, float bounty, int sentenceDays)
    {
        this.target = target;
        this.city = city;
        this.bounty = bounty;
        this.sentenceDays = sentenceDays;
    }
}

/// <summary>Immutable typed projection of ItemData from the selected source inventory.</summary>
public sealed class DailyV1ItemData
{
    public string id { get; }
    public string itemName { get; }
    public float basePrice { get; }
    public IReadOnlyList<DailyV1CapabilityAttributeModifier> capabilityModifiers { get; }

    public DailyV1ItemData(string id, string itemName, float basePrice, IReadOnlyList<DailyV1CapabilityAttributeModifier> capabilityModifiers)
    {
        this.id = id;
        this.itemName = itemName;
        this.basePrice = basePrice;
        this.capabilityModifiers = DailyV1ValueCopies.Copy(capabilityModifiers);
    }
}

/// <summary>Immutable typed projection of MarketItemConfig from the selected source inventory.</summary>
public sealed class DailyV1MarketItemConfig
{
    public string item { get; }
    public int initialAmount { get; }
    public int desiredAmount { get; }
    public float consumptionPer1000Population { get; }

    public DailyV1MarketItemConfig(string item, int initialAmount, int desiredAmount, float consumptionPer1000Population)
    {
        this.item = item;
        this.initialAmount = initialAmount;
        this.desiredAmount = desiredAmount;
        this.consumptionPer1000Population = consumptionPer1000Population;
    }
}

/// <summary>Immutable typed projection of MarketLiquidityConfig from the selected source inventory.</summary>
public sealed class DailyV1MarketLiquidityConfig
{
    public DailyV1EnumValue liquidityMode { get; }
    public float initialPurchasingPower { get; }

    public DailyV1MarketLiquidityConfig(DailyV1EnumValue liquidityMode, float initialPurchasingPower)
    {
        this.liquidityMode = liquidityMode;
        this.initialPurchasingPower = initialPurchasingPower;
    }
}

/// <summary>Immutable typed projection of NpcActionData from the selected source inventory.</summary>
public sealed class DailyV1NpcActionData
{
    public string id { get; }
    public IReadOnlyList<string> statusNecessariosParaFazerAcao { get; }
    public string actionName { get; }
    public string normalActionLogText { get; }
    public DailyV1EnumValue actionCategory { get; }
    public DailyV1EnumValue actionType { get; }
    public float baseUtility { get; }
    public IReadOnlyList<DailyV1StatusWeightModifier> statusModifiers { get; }
    public bool canFail { get; }
    public float baseSuccessChance { get; }
    public IReadOnlyList<string> statusToAdd { get; }
    public IReadOnlyList<string> statusToRemove { get; }
    public IReadOnlyList<string> targetStatusToAdd { get; }
    public IReadOnlyList<string> targetStatusToRemove { get; }
    public DailyV1CrimeActionSettings crimeSettings { get; }

    public DailyV1NpcActionData(string id, IReadOnlyList<string> statusNecessariosParaFazerAcao, string actionName, string normalActionLogText, DailyV1EnumValue actionCategory, DailyV1EnumValue actionType, float baseUtility, IReadOnlyList<DailyV1StatusWeightModifier> statusModifiers, bool canFail, float baseSuccessChance, IReadOnlyList<string> statusToAdd, IReadOnlyList<string> statusToRemove, IReadOnlyList<string> targetStatusToAdd, IReadOnlyList<string> targetStatusToRemove, DailyV1CrimeActionSettings crimeSettings)
    {
        this.id = id;
        this.statusNecessariosParaFazerAcao = DailyV1ValueCopies.Copy(statusNecessariosParaFazerAcao);
        this.actionName = actionName;
        this.normalActionLogText = normalActionLogText;
        this.actionCategory = actionCategory;
        this.actionType = actionType;
        this.baseUtility = baseUtility;
        this.statusModifiers = DailyV1ValueCopies.Copy(statusModifiers);
        this.canFail = canFail;
        this.baseSuccessChance = baseSuccessChance;
        this.statusToAdd = DailyV1ValueCopies.Copy(statusToAdd);
        this.statusToRemove = DailyV1ValueCopies.Copy(statusToRemove);
        this.targetStatusToAdd = DailyV1ValueCopies.Copy(targetStatusToAdd);
        this.targetStatusToRemove = DailyV1ValueCopies.Copy(targetStatusToRemove);
        this.crimeSettings = crimeSettings;
    }
}

/// <summary>Immutable typed projection of NpcData from the selected source inventory.</summary>
public sealed class DailyV1NpcData
{
    public string id { get; }
    public string name { get; }
    public string job { get; }
    public IReadOnlyList<DailyV1NPCDefaultAction> acoesPadrao { get; }
    public IReadOnlyList<string> statusPadrao { get; }
    public IReadOnlyList<DailyV1CapabilityAttributeValue> capabilityValues { get; }
    public IReadOnlyList<string> traits { get; }

    public DailyV1NpcData(string id, string name, string job, IReadOnlyList<DailyV1NPCDefaultAction> acoesPadrao, IReadOnlyList<string> statusPadrao, IReadOnlyList<DailyV1CapabilityAttributeValue> capabilityValues, IReadOnlyList<string> traits)
    {
        this.id = id;
        this.name = name;
        this.job = job;
        this.acoesPadrao = DailyV1ValueCopies.Copy(acoesPadrao);
        this.statusPadrao = DailyV1ValueCopies.Copy(statusPadrao);
        this.capabilityValues = DailyV1ValueCopies.Copy(capabilityValues);
        this.traits = DailyV1ValueCopies.Copy(traits);
    }
}

/// <summary>Immutable typed projection of NPCDefaultAction from the selected source inventory.</summary>
public sealed class DailyV1NPCDefaultAction
{
    public string action { get; }
    public float baseUtility { get; }

    public DailyV1NPCDefaultAction(string action, float baseUtility)
    {
        this.action = action;
        this.baseUtility = baseUtility;
    }
}

/// <summary>Immutable typed projection of NpcInitialInventoryItemConfig from the selected source inventory.</summary>
public sealed class DailyV1NpcInitialInventoryItemConfig
{
    public string item { get; }
    public int amount { get; }
    public float averageUnitCost { get; }

    public DailyV1NpcInitialInventoryItemConfig(string item, int amount, float averageUnitCost)
    {
        this.item = item;
        this.amount = amount;
        this.averageUnitCost = averageUnitCost;
    }
}

/// <summary>Immutable typed projection of NpcJobData from the selected source inventory.</summary>
public sealed class DailyV1NpcJobData
{
    public string id { get; }
    public string jobName { get; }
    public DailyV1EnumValue jobType { get; }
    public DailyV1EnumValue merchantBehavior { get; }
    public float minimumProfitPerItem { get; }
    public string workAction { get; }
    public float workUtility { get; }
    public IReadOnlyList<DailyV1TradeItemPreference> preferredTradeItems { get; }

    public DailyV1NpcJobData(string id, string jobName, DailyV1EnumValue jobType, DailyV1EnumValue merchantBehavior, float minimumProfitPerItem, string workAction, float workUtility, IReadOnlyList<DailyV1TradeItemPreference> preferredTradeItems)
    {
        this.id = id;
        this.jobName = jobName;
        this.jobType = jobType;
        this.merchantBehavior = merchantBehavior;
        this.minimumProfitPerItem = minimumProfitPerItem;
        this.workAction = workAction;
        this.workUtility = workUtility;
        this.preferredTradeItems = DailyV1ValueCopies.Copy(preferredTradeItems);
    }
}

/// <summary>Immutable typed projection of NpcSimulationConfig from the selected source inventory.</summary>
public sealed class DailyV1NpcSimulationConfig
{
    public string npc { get; }
    public string startingCity { get; }
    public float initialMoney { get; }
    public IReadOnlyList<DailyV1NpcInitialInventoryItemConfig> initialInventory { get; }
    public IReadOnlyList<string> initialKnownExplorableSites { get; }

    public DailyV1NpcSimulationConfig(string npc, string startingCity, float initialMoney, IReadOnlyList<DailyV1NpcInitialInventoryItemConfig> initialInventory, IReadOnlyList<string> initialKnownExplorableSites)
    {
        this.npc = npc;
        this.startingCity = startingCity;
        this.initialMoney = initialMoney;
        this.initialInventory = DailyV1ValueCopies.Copy(initialInventory);
        this.initialKnownExplorableSites = DailyV1ValueCopies.Copy(initialKnownExplorableSites);
    }
}

/// <summary>Immutable typed projection of NpcStatusData from the selected source inventory.</summary>
public sealed class DailyV1NpcStatusData
{
    public string id { get; }
    public string statusName { get; }

    public DailyV1NpcStatusData(string id, string statusName)
    {
        this.id = id;
        this.statusName = statusName;
    }
}

/// <summary>Immutable typed projection of PopulationConsumptionConfig from the selected source inventory.</summary>
public sealed class DailyV1PopulationConsumptionConfig
{
    public DailyV1EnumValue paymentMode { get; }
    public float initialPurchasingPower { get; }

    public DailyV1PopulationConsumptionConfig(DailyV1EnumValue paymentMode, float initialPurchasingPower)
    {
        this.paymentMode = paymentMode;
        this.initialPurchasingPower = initialPurchasingPower;
    }
}

/// <summary>Immutable typed projection of ScheduledDirectiveConfig from the selected source inventory.</summary>
public sealed class DailyV1ScheduledDirectiveConfig
{
    public long absoluteDay { get; }
    public DailyV1EnumValue mode { get; }
    public DailyV1EnumValue operation { get; }
    public string actor { get; }
    public string action { get; }

    public DailyV1ScheduledDirectiveConfig(long absoluteDay, DailyV1EnumValue mode, DailyV1EnumValue operation, string actor, string action)
    {
        this.absoluteDay = absoluteDay;
        this.mode = mode;
        this.operation = operation;
        this.actor = actor;
        this.action = action;
    }
}

/// <summary>Immutable typed projection of SimulationConfigData from the selected source inventory.</summary>
public sealed class DailyV1SimulationConfigData
{
    public string simulationName { get; }
    public DailyV1CalendarDefinition calendar { get; }
    public IReadOnlyList<DailyV1EnumValue> enabledModules { get; }
    public IReadOnlyList<string> cities { get; }
    public IReadOnlyList<DailyV1ExplorableSiteConfig> explorableSites { get; }
    public IReadOnlyList<DailyV1NpcSimulationConfig> npcs { get; }
    public IReadOnlyList<string> actions { get; }
    public IReadOnlyList<string> statuses { get; }
    public IReadOnlyList<string> jobs { get; }
    public IReadOnlyList<DailyV1InitialWantedRecordConfig> initialWarrants { get; }
    public IReadOnlyList<DailyV1ScheduledDirectiveConfig> scheduledDirectives { get; }
    public bool useAuthoredGeographyProfile { get; }
    public string authoredHexId { get; }
    public int authoredHexQ { get; }
    public int authoredHexR { get; }
    public string authoredTerrainDefinitionId { get; }
    public string authoredTerrainRevisionToken { get; }
    public string authoredLocationId { get; }
    public string authoredScaleConventionId { get; }
    public string authoredScaleSourceIdentity { get; }
    public string authoredScaleSourceVersion { get; }
    public string authoredDistancePerNeighborStep { get; }
    public string authoredScaleUnit { get; }
    public string authoredP10RuinSite { get; }
    public string genesisProfileContractIdentity { get; }
    public string p10bStableSiteKey { get; }
    public float travelCostPerDay { get; }
    public bool allowMerchantTradeRepositioning { get; }
    public int maxMerchantTradeAmount { get; }
    public float localMerchantWholesalePriceMultiplier { get; }
    public float localMerchantReserveRatio { get; }
    public int maxUnprofitablePlanWaitDays { get; }
    public bool crimeAutonomousEnabled { get; }
    public DailyV1CommercialKnowledgeSettings commercialKnowledge { get; }
    public bool useFixedSimulationSeed { get; }
    public int simulationSeed { get; }
    public bool includeEconomySnapshots { get; }
    public int economySnapshotIntervalDays { get; }
    public DailyV1SimulationLogSettings logSettings { get; }
    public bool naturalMortalityEnabled { get; }
    public double naturalMortalityAnnualProbability { get; }
    public bool aggregateDemographyEnabled { get; }
    public double aggregateAnnualBirthRate { get; }
    public double aggregateAnnualDeathRate { get; }
    public string freeStatus { get; }
    public string wantedStatus { get; }
    public string arrestedStatus { get; }
    public string hiddenStatus { get; }

    public DailyV1SimulationConfigData(string simulationName, DailyV1CalendarDefinition calendar, IReadOnlyList<DailyV1EnumValue> enabledModules, IReadOnlyList<string> cities, IReadOnlyList<DailyV1ExplorableSiteConfig> explorableSites, IReadOnlyList<DailyV1NpcSimulationConfig> npcs, IReadOnlyList<string> actions, IReadOnlyList<string> statuses, IReadOnlyList<string> jobs, IReadOnlyList<DailyV1InitialWantedRecordConfig> initialWarrants, IReadOnlyList<DailyV1ScheduledDirectiveConfig> scheduledDirectives, bool useAuthoredGeographyProfile, string authoredHexId, int authoredHexQ, int authoredHexR, string authoredTerrainDefinitionId, string authoredTerrainRevisionToken, string authoredLocationId, string authoredScaleConventionId, string authoredScaleSourceIdentity, string authoredScaleSourceVersion, string authoredDistancePerNeighborStep, string authoredScaleUnit, string authoredP10RuinSite, string genesisProfileContractIdentity, string p10bStableSiteKey, float travelCostPerDay, bool allowMerchantTradeRepositioning, int maxMerchantTradeAmount, float localMerchantWholesalePriceMultiplier, float localMerchantReserveRatio, int maxUnprofitablePlanWaitDays, bool crimeAutonomousEnabled, DailyV1CommercialKnowledgeSettings commercialKnowledge, bool useFixedSimulationSeed, int simulationSeed, bool includeEconomySnapshots, int economySnapshotIntervalDays, DailyV1SimulationLogSettings logSettings, bool naturalMortalityEnabled, double naturalMortalityAnnualProbability, bool aggregateDemographyEnabled, double aggregateAnnualBirthRate, double aggregateAnnualDeathRate, string freeStatus, string wantedStatus, string arrestedStatus, string hiddenStatus)
    {
        this.simulationName = simulationName;
        this.calendar = calendar;
        this.enabledModules = DailyV1ValueCopies.Copy(enabledModules);
        this.cities = DailyV1ValueCopies.Copy(cities);
        this.explorableSites = DailyV1ValueCopies.Copy(explorableSites);
        this.npcs = DailyV1ValueCopies.Copy(npcs);
        this.actions = DailyV1ValueCopies.Copy(actions);
        this.statuses = DailyV1ValueCopies.Copy(statuses);
        this.jobs = DailyV1ValueCopies.Copy(jobs);
        this.initialWarrants = DailyV1ValueCopies.Copy(initialWarrants);
        this.scheduledDirectives = DailyV1ValueCopies.Copy(scheduledDirectives);
        this.useAuthoredGeographyProfile = useAuthoredGeographyProfile;
        this.authoredHexId = authoredHexId;
        this.authoredHexQ = authoredHexQ;
        this.authoredHexR = authoredHexR;
        this.authoredTerrainDefinitionId = authoredTerrainDefinitionId;
        this.authoredTerrainRevisionToken = authoredTerrainRevisionToken;
        this.authoredLocationId = authoredLocationId;
        this.authoredScaleConventionId = authoredScaleConventionId;
        this.authoredScaleSourceIdentity = authoredScaleSourceIdentity;
        this.authoredScaleSourceVersion = authoredScaleSourceVersion;
        this.authoredDistancePerNeighborStep = authoredDistancePerNeighborStep;
        this.authoredScaleUnit = authoredScaleUnit;
        this.authoredP10RuinSite = authoredP10RuinSite;
        this.genesisProfileContractIdentity = genesisProfileContractIdentity;
        this.p10bStableSiteKey = p10bStableSiteKey;
        this.travelCostPerDay = travelCostPerDay;
        this.allowMerchantTradeRepositioning = allowMerchantTradeRepositioning;
        this.maxMerchantTradeAmount = maxMerchantTradeAmount;
        this.localMerchantWholesalePriceMultiplier = localMerchantWholesalePriceMultiplier;
        this.localMerchantReserveRatio = localMerchantReserveRatio;
        this.maxUnprofitablePlanWaitDays = maxUnprofitablePlanWaitDays;
        this.crimeAutonomousEnabled = crimeAutonomousEnabled;
        this.commercialKnowledge = commercialKnowledge;
        this.useFixedSimulationSeed = useFixedSimulationSeed;
        this.simulationSeed = simulationSeed;
        this.includeEconomySnapshots = includeEconomySnapshots;
        this.economySnapshotIntervalDays = economySnapshotIntervalDays;
        this.logSettings = logSettings;
        this.naturalMortalityEnabled = naturalMortalityEnabled;
        this.naturalMortalityAnnualProbability = naturalMortalityAnnualProbability;
        this.aggregateDemographyEnabled = aggregateDemographyEnabled;
        this.aggregateAnnualBirthRate = aggregateAnnualBirthRate;
        this.aggregateAnnualDeathRate = aggregateAnnualDeathRate;
        this.freeStatus = freeStatus;
        this.wantedStatus = wantedStatus;
        this.arrestedStatus = arrestedStatus;
        this.hiddenStatus = hiddenStatus;
    }
}

/// <summary>Immutable typed projection of SimulationLogSettings from the selected source inventory.</summary>
public sealed class DailyV1SimulationLogSettings
{
    public bool showDay { get; }
    public bool npcActions { get; }
    public bool trade { get; }
    public bool travel { get; }
    public bool crime { get; }
    public bool justice { get; }
    public bool economyProduction { get; }
    public bool economyConsumption { get; }
    public bool market { get; }

    public DailyV1SimulationLogSettings(bool showDay, bool npcActions, bool trade, bool travel, bool crime, bool justice, bool economyProduction, bool economyConsumption, bool market)
    {
        this.showDay = showDay;
        this.npcActions = npcActions;
        this.trade = trade;
        this.travel = travel;
        this.crime = crime;
        this.justice = justice;
        this.economyProduction = economyProduction;
        this.economyConsumption = economyConsumption;
        this.market = market;
    }
}

/// <summary>Immutable typed projection of StatusWeightModifier from the selected source inventory.</summary>
public sealed class DailyV1StatusWeightModifier
{
    public string status { get; }
    public float multiplier { get; }

    public DailyV1StatusWeightModifier(string status, float multiplier)
    {
        this.status = status;
        this.multiplier = multiplier;
    }
}

/// <summary>Immutable typed projection of TradeItemPreference from the selected source inventory.</summary>
public sealed class DailyV1TradeItemPreference
{
    public string item { get; }
    public float utilityMultiplier { get; }

    public DailyV1TradeItemPreference(string item, float utilityMultiplier)
    {
        this.item = item;
        this.utilityMultiplier = utilityMultiplier;
    }
}




