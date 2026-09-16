using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

public static class SimulationContentValidator
{
    private static readonly Type[] DefinitionAssetTypes =
    {
        typeof(CapabilityAttributeData),
        typeof(TraitData),
        typeof(NpcData),
        typeof(NpcActionData),
        typeof(NpcStatusData),
        typeof(NpcJobData),
        typeof(CityData),
        typeof(ItemData),
        typeof(ExplorableSiteData),
        typeof(LocalPlaceTypeData),
        typeof(LocalConnectionTypeData),
        typeof(OrganizationData),
        typeof(SimulationConfigData)
    };

    public static ContentValidationReport ValidateProject()
    {
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
        Array.Sort(guids, StringComparer.Ordinal);

        List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (IsDefinitionAsset(asset) == true)
            {
                assets.Add(asset);
            }
        }

        return ValidateAssets(assets);
    }

    public static ContentValidationReport ValidateAssets(IEnumerable<UnityEngine.Object> assets)
    {
        List<UnityEngine.Object> uniqueAssets = new List<UnityEngine.Object>();
        HashSet<UnityEngine.Object> seen = new HashSet<UnityEngine.Object>();

        if (assets != null)
        {
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset != null && IsDefinitionAsset(asset) == true && seen.Add(asset) == true)
                {
                    uniqueAssets.Add(asset);
                }
            }
        }

        uniqueAssets.Sort(AssetComparer.Instance);

        List<ContentValidationIssue> issues = new List<ContentValidationIssue>();
        Dictionary<Type, List<UnityEngine.Object>> definitionsByType = CollectDefinitionsByType(uniqueAssets);

        foreach (KeyValuePair<Type, List<UnityEngine.Object>> group in definitionsByType)
        {
            ValidateDefinitionIds(group.Key, group.Value, issues);
        }

        for (int i = 0; i < uniqueAssets.Count; i++)
        {
            UnityEngine.Object asset = uniqueAssets[i];
            switch (asset)
            {
                case CapabilityAttributeData capabilityAttribute:
                    ValidateCapabilityAttribute(capabilityAttribute, issues);
                    break;
                case TraitData trait:
                    ValidateTrait(trait, issues);
                    break;
                case NpcData npc:
                    ValidateNpc(npc, issues);
                    break;
                case NpcActionData action:
                    ValidateAction(action, issues);
                    break;
                case NpcJobData job:
                    ValidateJob(job, issues);
                    break;
                case CityData city:
                    ValidateCity(city, issues);
                    break;
                case ItemData item:
                    ValidateItem(item, issues);
                    break;
                case SimulationConfigData config:
                    ValidateSimulationConfig(config, issues);
                    break;
                case ExplorableSiteData:
                case LocalPlaceTypeData:
                case LocalConnectionTypeData:
                case OrganizationData:
                case NpcStatusData:
                    break;
            }
        }

        return new ContentValidationReport(issues);
    }

    public static bool IsDefinitionAsset(UnityEngine.Object asset)
    {
        if (asset == null)
        {
            return false;
        }

        Type assetType = asset.GetType();
        for (int i = 0; i < DefinitionAssetTypes.Length; i++)
        {
            if (DefinitionAssetTypes[i].IsAssignableFrom(assetType) == true)
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<Type, List<UnityEngine.Object>> CollectDefinitionsByType(
        List<UnityEngine.Object> assets)
    {
        Dictionary<Type, List<UnityEngine.Object>> definitionsByType = new Dictionary<Type, List<UnityEngine.Object>>();

        for (int i = 0; i < assets.Count; i++)
        {
            UnityEngine.Object asset = assets[i];
            Type concreteType = asset.GetType();
            if (definitionsByType.TryGetValue(concreteType, out List<UnityEngine.Object> definitions) == false)
            {
                definitions = new List<UnityEngine.Object>();
                definitionsByType.Add(concreteType, definitions);
            }

            definitions.Add(asset);
        }

        return definitionsByType;
    }

    private static void ValidateDefinitionIds(
        Type definitionType,
        List<UnityEngine.Object> definitions,
        List<ContentValidationIssue> issues)
    {
        Dictionary<string, List<UnityEngine.Object>> assetsById = new Dictionary<string, List<UnityEngine.Object>>(StringComparer.Ordinal);

        for (int i = 0; i < definitions.Count; i++)
        {
            UnityEngine.Object asset = definitions[i];
            string definitionId = GetDefinitionId(asset);

            if (RequiresDefinitionId(asset) == true && string.IsNullOrWhiteSpace(definitionId) == true)
            {
                AddIssue(
                    issues,
                    ContentValidationSeverity.Error,
                    ContentValidationCodes.MissingDefinitionId,
                    asset,
                    definitionId,
                    "DefinitionId",
                    "The authoring definition requires a non-empty DefinitionId.");
                continue;
            }

            if (RequiresDefinitionId(asset) == true)
            {
                if (assetsById.TryGetValue(definitionId, out List<UnityEngine.Object> sameIdAssets) == false)
                {
                    sameIdAssets = new List<UnityEngine.Object>();
                    assetsById.Add(definitionId, sameIdAssets);
                }

                sameIdAssets.Add(asset);
            }
        }

        foreach (KeyValuePair<string, List<UnityEngine.Object>> duplicate in assetsById)
        {
            if (duplicate.Value.Count < 2)
            {
                continue;
            }

            for (int i = 0; i < duplicate.Value.Count; i++)
            {
                AddIssue(
                    issues,
                    ContentValidationSeverity.Error,
                    ContentValidationCodes.DuplicateDefinitionId,
                    duplicate.Value[i],
                    duplicate.Key,
                    "DefinitionId",
                    "DefinitionId '" + duplicate.Key + "' is duplicated within " + definitionType.Name + ".");
            }
        }
    }

    private static void ValidateCapabilityAttribute(CapabilityAttributeData asset, List<ContentValidationIssue> issues)
    {
    }

    private static void ValidateTrait(TraitData asset, List<ContentValidationIssue> issues)
    {
    }

    private static void ValidateNpc(NpcData asset, List<ContentValidationIssue> issues)
    {
        ValidateReferenceList(asset, asset.acoesPadrao, "acoesPadrao", issues);
        if (asset.acoesPadrao != null)
        {
            Dictionary<NpcActionData, int> actionOccurrences = new Dictionary<NpcActionData, int>();
            for (int i = 0; i < asset.acoesPadrao.Count; i++)
            {
                NPCDefaultAction entry = asset.acoesPadrao[i];
                if (entry == null)
                {
                    continue;
                }

                if (entry.action == null)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                        asset, asset.DefinitionId, "acoesPadrao[" + i + "].action",
                        "A default action entry requires an NpcActionData reference.");
                }
                else
                {
                    AddDuplicateReferenceIssueIfNeeded(actionOccurrences, entry.action, asset, asset.DefinitionId,
                        "acoesPadrao[" + i + "].action", issues);
                }

                ValidateFiniteNonNegative(asset, entry.baseUtility, "acoesPadrao[" + i + "].baseUtility", issues);
            }
        }

        ValidateReferenceList(asset, asset.statusPadrao, "statusPadrao", issues);
        ValidateReferenceList(asset, asset.traits, "traits", issues);
        ValidateReferenceList(asset, asset.capabilityValues, "capabilityValues", issues);

        if (asset.traits != null)
        {
            Dictionary<string, int> traitIds = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < asset.traits.Count; i++)
            {
                TraitData trait = asset.traits[i];
                if (trait != null && string.IsNullOrWhiteSpace(trait.DefinitionId) == false)
                {
                    AddDuplicateIdReferenceIssueIfNeeded(traitIds, trait.DefinitionId, asset, asset.DefinitionId,
                        "traits[" + i + "]", issues);
                }
            }
        }

        if (asset.capabilityValues != null)
        {
            Dictionary<string, int> attributeIds = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < asset.capabilityValues.Count; i++)
            {
                CapabilityAttributeValue value = asset.capabilityValues[i];
                if (value == null)
                {
                    continue;
                }

                if (value.attribute == null)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                        asset, asset.DefinitionId, "capabilityValues[" + i + "].attribute",
                        "A capability value requires a CapabilityAttributeData reference.");
                    continue;
                }

                AddDuplicateIdReferenceIssueIfNeeded(attributeIds, value.attribute.DefinitionId, asset,
                    asset.DefinitionId, "capabilityValues[" + i + "].attribute", issues);
                ValidateFiniteNonNegative(asset, value.value, "capabilityValues[" + i + "].value", issues);
            }
        }
    }

    private static void ValidateAction(NpcActionData asset, List<ContentValidationIssue> issues)
    {
        ValidateReferenceList(asset, asset.statusNecessariosParaFazerAcao, "statusNecessariosParaFazerAcao", issues);
        ValidateReferenceList(asset, asset.statusToAdd, "statusToAdd", issues);
        ValidateReferenceList(asset, asset.statusToRemove, "statusToRemove", issues);
        ValidateReferenceList(asset, asset.targetStatusToAdd, "targetStatusToAdd", issues);
        ValidateReferenceList(asset, asset.targetStatusToRemove, "targetStatusToRemove", issues);
        ValidateFiniteNonNegative(asset, asset.baseUtility, "baseUtility", issues);
        ValidateFiniteRange(asset, asset.baseSuccessChance, 0f, 1f, "baseSuccessChance", issues);

        if (asset.statusModifiers != null)
        {
            for (int i = 0; i < asset.statusModifiers.Count; i++)
            {
                StatusWeightModifier modifier = asset.statusModifiers[i];
                if (modifier == null)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullCollectionElement,
                        asset, asset.DefinitionId, "statusModifiers[" + i + "]",
                        "The status modifier list contains a null element.");
                    continue;
                }

                if (modifier.status == null)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                        asset, asset.DefinitionId, "statusModifiers[" + i + "].status",
                        "A status weight modifier requires an NpcStatusData reference.");
                }

                ValidateFinite(asset, modifier.multiplier, "statusModifiers[" + i + "].multiplier", issues);
            }
        }

        if (asset.crimeSettings != null)
        {
            ValidateFiniteNonNegative(asset, asset.crimeSettings.bounty, "crimeSettings.bounty", issues);
            ValidateFiniteNonNegative(asset, asset.crimeSettings.escapeBountyPenalty,
                "crimeSettings.escapeBountyPenalty", issues);
            ValidateNonNegative(asset, asset.crimeSettings.amount, "crimeSettings.amount", issues);
            ValidatePositive(asset, asset.crimeSettings.sentenceDays, "crimeSettings.sentenceDays", issues,
                ContentValidationSeverity.Warning);
            ValidatePositive(asset, asset.crimeSettings.hiddenDays, "crimeSettings.hiddenDays", issues,
                ContentValidationSeverity.Warning);
            ValidateNonNegative(asset, asset.crimeSettings.failedEscapeSentencePenalty,
                "crimeSettings.failedEscapeSentencePenalty", issues);
        }
    }

    private static void ValidateJob(NpcJobData asset, List<ContentValidationIssue> issues)
    {
        ValidateFiniteNonNegative(asset, asset.workUtility, "workUtility", issues);

        if (asset.preferredTradeItems == null)
        {
            return;
        }

        for (int i = 0; i < asset.preferredTradeItems.Count; i++)
        {
            TradeItemPreference preference = asset.preferredTradeItems[i];
            if (preference == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullCollectionElement,
                    asset, string.Empty, "preferredTradeItems[" + i + "]",
                    "The preferred trade item list contains a null element.");
                continue;
            }

            if (preference.item != null)
            {
                ValidateFiniteNonNegative(asset, preference.utilityMultiplier,
                    "preferredTradeItems[" + i + "].utilityMultiplier", issues);
            }
        }
    }

    private static void ValidateCity(CityData asset, List<ContentValidationIssue> issues)
    {
        ValidateNonNegative(asset, asset.initialPopulation, "initialPopulation", issues);
        if (asset.marketLiquidity != null)
        {
            ValidateFiniteNonNegative(asset, asset.marketLiquidity.initialPurchasingPower,
                "marketLiquidity.initialPurchasingPower", issues);
        }

        if (asset.populationConsumption != null)
        {
            ValidateFiniteNonNegative(asset, asset.populationConsumption.initialPurchasingPower,
                "populationConsumption.initialPurchasingPower", issues);
        }

        Dictionary<ItemData, int> marketItems = new Dictionary<ItemData, int>();
        if (asset.marketItems != null)
        {
            for (int i = 0; i < asset.marketItems.Count; i++)
            {
                MarketItemConfig config = asset.marketItems[i];
                if (config == null)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullCollectionElement,
                        asset, asset.DefinitionId, "marketItems[" + i + "]",
                        "The market item list contains a null element.");
                    continue;
                }

                if (config.item == null)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                        asset, asset.DefinitionId, "marketItems[" + i + "].item",
                        "A market item configuration requires an ItemData reference.");
                }
                else
                {
                    AddDuplicateReferenceIssueIfNeeded(marketItems, config.item, asset, asset.DefinitionId,
                        "marketItems[" + i + "].item", issues);
                }

                ValidateNonNegative(asset, config.initialAmount, "marketItems[" + i + "].initialAmount", issues);
                ValidatePositive(asset, config.desiredAmount, "marketItems[" + i + "].desiredAmount", issues,
                    ContentValidationSeverity.Warning);
                ValidateFiniteNonNegative(asset, config.consumptionPer1000Population,
                    "marketItems[" + i + "].consumptionPer1000Population", issues);
            }
        }

        ValidateCityProductionConfigurations(asset, issues);
        ValidateCityConnections(asset, issues);
    }

    private static void ValidateCityProductionConfigurations(CityData asset, List<ContentValidationIssue> issues)
    {
        if (asset.productionConfigs == null)
        {
            return;
        }

        for (int i = 0; i < asset.productionConfigs.Count; i++)
        {
            CityProductionConfig config = asset.productionConfigs[i];
            if (config == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullCollectionElement,
                    asset, asset.DefinitionId, "productionConfigs[" + i + "]",
                    "The production configuration list contains a null element.");
                continue;
            }

            if (config.item == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                    asset, asset.DefinitionId, "productionConfigs[" + i + "].item",
                    "A production configuration requires an ItemData reference.");
            }

            ValidateNonNegative(asset, config.amountPerDay, "productionConfigs[" + i + "].amountPerDay", issues);
        }
    }

    private static void ValidateCityConnections(CityData asset, List<ContentValidationIssue> issues)
    {
        if (asset.connections == null)
        {
            return;
        }

        Dictionary<CityData, int> destinations = new Dictionary<CityData, int>();
        for (int i = 0; i < asset.connections.Count; i++)
        {
            CityConnection connection = asset.connections[i];
            if (connection == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullCollectionElement,
                    asset, asset.DefinitionId, "connections[" + i + "]",
                    "The city connection list contains a null element.");
                continue;
            }

            if (connection.destination == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                    asset, asset.DefinitionId, "connections[" + i + "].destination",
                    "A city connection requires a destination CityData reference.");
            }
            else
            {
                AddDuplicateReferenceIssueIfNeeded(destinations, connection.destination, asset, asset.DefinitionId,
                    "connections[" + i + "].destination", issues);
                if (connection.destination == asset)
                {
                    AddIssue(issues, ContentValidationSeverity.Warning, ContentValidationCodes.IncompatibleConfiguration,
                        asset, asset.DefinitionId, "connections[" + i + "].destination",
                        "A city connection points back to its own CityData and will be skipped by route creation.");
                }
            }

            ValidatePositive(asset, connection.travelDays, "connections[" + i + "].travelDays", issues,
                ContentValidationSeverity.Warning);
        }
    }

    private static void ValidateItem(ItemData asset, List<ContentValidationIssue> issues)
    {
        ValidateFinitePositive(asset, asset.basePrice, "basePrice", issues);

        if (asset.capabilityModifiers == null)
        {
            return;
        }

        for (int i = 0; i < asset.capabilityModifiers.Count; i++)
        {
            CapabilityAttributeModifier modifier = asset.capabilityModifiers[i];
            if (modifier == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullCollectionElement,
                    asset, asset.DefinitionId, "capabilityModifiers[" + i + "]",
                    "The capability modifier list contains a null element.");
                continue;
            }

            if (modifier.attribute == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                    asset, asset.DefinitionId, "capabilityModifiers[" + i + "].attribute",
                    "A capability modifier requires a CapabilityAttributeData reference.");
            }

            ValidateFiniteNonNegative(asset, modifier.additiveValue,
                "capabilityModifiers[" + i + "].additiveValue", issues);
        }
    }

    private static void ValidateSimulationConfig(SimulationConfigData asset, List<ContentValidationIssue> issues)
    {
        if (asset.calendar == null)
        {
            AddIssue(issues, ContentValidationSeverity.Warning, ContentValidationCodes.NullOptionalReference,
                asset, string.Empty, "calendar",
                "CalendarDefinition is missing; simulation runtime will use its safe default.");
        }
        else if (asset.calendar.TryValidate(out string calendarDiagnostic) == false)
        {
            AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.InvalidNumericValue,
                asset, string.Empty, "calendar", calendarDiagnostic);
        }

        ValidateDefinitionList(asset, asset.cities, "cities", issues);
        ValidateDefinitionList(asset, asset.explorableSites, "explorableSites", issues);
        ValidateDefinitionList(asset, asset.npcs, "npcs", issues);
        ValidateDefinitionList(asset, asset.actions, "actions", issues);
        ValidateDefinitionList(asset, asset.statuses, "statuses", issues);
        ValidateDefinitionList(asset, asset.jobs, "jobs", issues);
        ValidateDefinitionList(asset, asset.initialWarrants, "initialWarrants", issues);
        ValidateDefinitionList(asset, asset.scheduledDirectives, "scheduledDirectives", issues);

        ValidateFiniteNonNegative(asset, asset.travelCostPerDay, "travelCostPerDay", issues);
        ValidateNonNegative(asset, asset.economySnapshotIntervalDays, "economySnapshotIntervalDays", issues);
        if (asset.commercialKnowledge != null)
        {
            ValidateNonNegative(asset, asset.commercialKnowledge.freshForDays,
                "commercialKnowledge.freshForDays", issues);
            ValidateNonNegative(asset, asset.commercialKnowledge.maxSharedObservationsPerInteraction,
                "commercialKnowledge.maxSharedObservationsPerInteraction", issues);
        }

        ValidateSimulationCityConfigurations(asset, issues);
        ValidateSimulationExplorableSites(asset, issues);
        ValidateSimulationNpcs(asset, issues);
        ValidateInitialWarrants(asset, issues);
        ValidateScheduledDirectives(asset, issues);

        bool merchantEnabled = asset.enabledModules != null && asset.enabledModules.Contains(SimulationModule.Merchant);
        bool economyEnabled = asset.enabledModules != null && asset.enabledModules.Contains(SimulationModule.Economy);
        if (merchantEnabled == true && economyEnabled == false)
        {
            AddIssue(issues, ContentValidationSeverity.Warning, ContentValidationCodes.IncompatibleConfiguration,
                asset, string.Empty, "enabledModules",
                "Merchant depends on Economy and will be disabled by SimulationModuleSet.");
        }
    }

    private static void ValidateSimulationCityConfigurations(SimulationConfigData asset, List<ContentValidationIssue> issues)
    {
        if (asset.cities == null)
        {
            return;
        }

        HashSet<CityData> configuredCities = ToReferenceSet(asset.cities);
        for (int i = 0; i < asset.cities.Count; i++)
        {
            CityData city = asset.cities[i];
            if (city == null || city.connections == null)
            {
                continue;
            }

            for (int j = 0; j < city.connections.Count; j++)
            {
                CityConnection connection = city.connections[j];
                if (connection != null && connection.destination != null && configuredCities.Contains(connection.destination) == false)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.IncompatibleConfiguration,
                        asset, string.Empty, "cities[" + i + "].connections[" + j + "].destination",
                        "City connection destination is not included in SimulationConfigData.cities.");
                }
            }
        }
    }

    private static void ValidateSimulationExplorableSites(SimulationConfigData asset, List<ContentValidationIssue> issues)
    {
        HashSet<CityData> configuredCities = ToReferenceSet(asset.cities);
        HashSet<ExplorableSiteData> configuredSites = new HashSet<ExplorableSiteData>();

        if (asset.explorableSites != null)
        {
            for (int i = 0; i < asset.explorableSites.Count; i++)
            {
                ExplorableSiteConfig config = asset.explorableSites[i];
                if (config == null)
                {
                    continue;
                }

                if (config.site == null)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                        asset, string.Empty, "explorableSites[" + i + "].site",
                        "An explorable site configuration requires an ExplorableSiteData reference.");
                }
                else
                {
                    configuredSites.Add(config.site);
                }

                if (config.anchorCity != null && configuredCities.Contains(config.anchorCity) == false)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.IncompatibleConfiguration,
                        asset, string.Empty, "explorableSites[" + i + "].anchorCity",
                        "Explorable site anchor city is not included in SimulationConfigData.cities.");
                }

                ValidatePositive(asset, config.travelDaysFromAnchor,
                    "explorableSites[" + i + "].travelDaysFromAnchor", issues, ContentValidationSeverity.Warning);
            }
        }

        if (asset.npcs == null)
        {
            return;
        }

        for (int i = 0; i < asset.npcs.Count; i++)
        {
            NpcSimulationConfig npcConfig = asset.npcs[i];
            if (npcConfig?.initialKnownExplorableSites == null)
            {
                continue;
            }

            for (int j = 0; j < npcConfig.initialKnownExplorableSites.Count; j++)
            {
                ExplorableSiteData site = npcConfig.initialKnownExplorableSites[j];
                if (site == null)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullCollectionElement,
                        asset, string.Empty, "npcs[" + i + "].initialKnownExplorableSites[" + j + "]",
                        "The initial known explorable site list contains a null element.");
                }
                else if (configuredSites.Contains(site) == false)
                {
                    AddIssue(issues, ContentValidationSeverity.Warning, ContentValidationCodes.IncompatibleConfiguration,
                        asset, string.Empty,
                        "npcs[" + i + "].initialKnownExplorableSites[" + j + "]",
                        "Initial known explorable site is not configured in SimulationConfigData.explorableSites.");
                }
            }
        }
    }

    private static void ValidateSimulationNpcs(SimulationConfigData asset, List<ContentValidationIssue> issues)
    {
        if (asset.npcs == null)
        {
            return;
        }

        HashSet<CityData> configuredCities = ToReferenceSet(asset.cities);
        for (int i = 0; i < asset.npcs.Count; i++)
        {
            NpcSimulationConfig config = asset.npcs[i];
            if (config == null)
            {
                continue;
            }

            if (config.npc == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                    asset, string.Empty, "npcs[" + i + "].npc",
                    "An NPC simulation configuration requires an NpcData reference.");
            }

            if (config.startingCity != null && configuredCities.Contains(config.startingCity) == false)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.IncompatibleConfiguration,
                    asset, string.Empty, "npcs[" + i + "].startingCity",
                    "NPC starting city is not included in SimulationConfigData.cities.");
            }

            ValidateFiniteNonNegative(asset, config.initialMoney, "npcs[" + i + "].initialMoney", issues);
            if (config.initialInventory == null)
            {
                continue;
            }

            for (int j = 0; j < config.initialInventory.Count; j++)
            {
                NpcInitialInventoryItemConfig inventory = config.initialInventory[j];
                if (inventory == null)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullCollectionElement,
                        asset, string.Empty, "npcs[" + i + "].initialInventory[" + j + "]",
                        "The initial inventory list contains a null element.");
                    continue;
                }

                if (inventory.item == null)
                {
                    AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                        asset, string.Empty, "npcs[" + i + "].initialInventory[" + j + "].item",
                        "An initial inventory entry requires an ItemData reference.");
                }

                ValidateFiniteNonNegative(asset, inventory.averageUnitCost,
                    "npcs[" + i + "].initialInventory[" + j + "].averageUnitCost", issues);
                ValidateNonNegative(asset, inventory.amount,
                    "npcs[" + i + "].initialInventory[" + j + "].amount", issues);
            }
        }
    }

    private static void ValidateInitialWarrants(SimulationConfigData asset, List<ContentValidationIssue> issues)
    {
        if (asset.initialWarrants == null)
        {
            return;
        }

        for (int i = 0; i < asset.initialWarrants.Count; i++)
        {
            InitialWantedRecordConfig warrant = asset.initialWarrants[i];
            if (warrant == null)
            {
                continue;
            }

            if (warrant.target == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                    asset, string.Empty, "initialWarrants[" + i + "].target",
                    "An initial warrant requires an NpcData target reference.");
            }

            if (warrant.city == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                    asset, string.Empty, "initialWarrants[" + i + "].city",
                    "An initial warrant requires a CityData reference.");
            }

            ValidateFiniteNonNegative(asset, warrant.bounty, "initialWarrants[" + i + "].bounty", issues);
            ValidatePositive(asset, warrant.sentenceDays, "initialWarrants[" + i + "].sentenceDays", issues,
                ContentValidationSeverity.Warning);
        }
    }

    private static void ValidateScheduledDirectives(SimulationConfigData asset, List<ContentValidationIssue> issues)
    {
        if (asset.scheduledDirectives == null)
        {
            return;
        }

        for (int i = 0; i < asset.scheduledDirectives.Count; i++)
        {
            ScheduledDirectiveConfig directive = asset.scheduledDirectives[i];
            if (directive == null)
            {
                continue;
            }

            if (directive.actor == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                    asset, string.Empty, "scheduledDirectives[" + i + "].actor",
                    "A scheduled directive requires an NpcData actor reference.");
            }

            if (directive.action == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullRequiredReference,
                    asset, string.Empty, "scheduledDirectives[" + i + "].action",
                    "A scheduled directive requires an NpcActionData reference.");
            }
            else if (directive.operation != ScheduledDirectiveOperation.EscapePrison
                || directive.action.actionType != NpcActionType.EscapePrison)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.IncompatibleConfiguration,
                    asset, string.Empty, "scheduledDirectives[" + i + "].action",
                    "The current scheduled directive contract requires EscapePrison and an EscapePrison action.");
            }

            if (directive.absoluteDay < 0L)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.InvalidNumericValue,
                    asset, string.Empty, "scheduledDirectives[" + i + "].absoluteDay",
                    "Absolute day cannot be negative.");
            }
            else if (directive.absoluteDay == 0L)
            {
                AddIssue(issues, ContentValidationSeverity.Warning, ContentValidationCodes.InvalidNumericValue,
                    asset, string.Empty, "scheduledDirectives[" + i + "].absoluteDay",
                    "Absolute day zero is accepted by the constructor but skipped by ScheduledDirectiveStore.");
            }
        }
    }

    private static void ValidateReferenceList<T>(
        UnityEngine.Object asset,
        List<T> values,
        string fieldPath,
        List<ContentValidationIssue> issues)
        where T : class
    {
        if (values == null)
        {
            return;
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] == null)
            {
                AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.NullCollectionElement,
                    asset, GetDefinitionId(asset), fieldPath + "[" + i + "]",
                    "The configured definition list contains a null element.");
            }
        }
    }

    private static void ValidateDefinitionList<T>(
        UnityEngine.Object asset,
        List<T> values,
        string fieldPath,
        List<ContentValidationIssue> issues)
        where T : class
    {
        ValidateReferenceList(asset, values, fieldPath, issues);
    }

    private static void AddDuplicateReferenceIssueIfNeeded<T>(
        Dictionary<T, int> occurrences,
        T value,
        UnityEngine.Object asset,
        string definitionId,
        string fieldPath,
        List<ContentValidationIssue> issues)
        where T : UnityEngine.Object
    {
        if (occurrences.TryGetValue(value, out int count) == true)
        {
            occurrences[value] = count + 1;
            AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.DuplicateReference,
                asset, definitionId, fieldPath,
                "The same definition reference appears more than once in this configured list.");
        }
        else
        {
            occurrences.Add(value, 1);
        }
    }

    private static void AddDuplicateIdReferenceIssueIfNeeded(
        Dictionary<string, int> occurrences,
        string value,
        UnityEngine.Object asset,
        string definitionId,
        string fieldPath,
        List<ContentValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            return;
        }

        if (occurrences.TryGetValue(value, out int count) == true)
        {
            occurrences[value] = count + 1;
            AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.DuplicateReference,
                asset, definitionId, fieldPath,
                "The same definition ID appears more than once in this configured list.");
        }
        else
        {
            occurrences.Add(value, 1);
        }
    }

    private static HashSet<CityData> ToReferenceSet(List<CityData> values)
    {
        HashSet<CityData> result = new HashSet<CityData>();
        if (values != null)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] != null)
                {
                    result.Add(values[i]);
                }
            }
        }

        return result;
    }

    private static void ValidateFinite(UnityEngine.Object asset, float value, string fieldPath,
        List<ContentValidationIssue> issues)
    {
        if (float.IsNaN(value) == true || float.IsInfinity(value) == true)
        {
            AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.InvalidNumericValue,
                asset, GetDefinitionId(asset), fieldPath, "Value must be finite.");
        }
    }

    private static void ValidateFiniteNonNegative(UnityEngine.Object asset, float value, string fieldPath,
        List<ContentValidationIssue> issues)
    {
        if (float.IsNaN(value) == true || float.IsInfinity(value) == true || value < 0f)
        {
            AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.InvalidNumericValue,
                asset, GetDefinitionId(asset), fieldPath,
                "Value must be finite and non-negative.");
        }
    }

    private static void ValidateFinitePositive(UnityEngine.Object asset, float value, string fieldPath,
        List<ContentValidationIssue> issues)
    {
        if (float.IsNaN(value) == true || float.IsInfinity(value) == true || value <= 0f)
        {
            AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.InvalidNumericValue,
                asset, GetDefinitionId(asset), fieldPath,
                "Value must be finite and greater than zero.");
        }
    }

    private static void ValidateFiniteRange(UnityEngine.Object asset, float value, float minimum, float maximum,
        string fieldPath, List<ContentValidationIssue> issues)
    {
        if (float.IsNaN(value) == true || float.IsInfinity(value) == true || value < minimum || value > maximum)
        {
            AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.InvalidNumericValue,
                asset, GetDefinitionId(asset), fieldPath,
                "Value must be finite and within [" + minimum.ToString(CultureInfo.InvariantCulture) + ", "
                + maximum.ToString(CultureInfo.InvariantCulture) + "].");
        }
    }

    private static void ValidateNonNegative(UnityEngine.Object asset, int value, string fieldPath,
        List<ContentValidationIssue> issues)
    {
        if (value < 0)
        {
            AddIssue(issues, ContentValidationSeverity.Error, ContentValidationCodes.InvalidNumericValue,
                asset, GetDefinitionId(asset), fieldPath, "Value must be non-negative.");
        }
    }

    private static void ValidatePositive(UnityEngine.Object asset, int value, string fieldPath,
        List<ContentValidationIssue> issues, ContentValidationSeverity severity)
    {
        if (value <= 0)
        {
            AddIssue(issues, severity, ContentValidationCodes.InvalidNumericValue, asset, GetDefinitionId(asset),
                fieldPath, "Value must be greater than zero.");
        }
    }

    private static string GetDefinitionId(UnityEngine.Object asset)
    {
        switch (asset)
        {
            case CapabilityAttributeData capabilityAttribute:
                return capabilityAttribute.DefinitionId;
            case TraitData trait:
                return trait.DefinitionId;
            case NpcData npc:
                return npc.DefinitionId;
            case NpcActionData action:
                return action.DefinitionId;
            case CityData city:
                return city.DefinitionId;
            case ItemData item:
                return item.DefinitionId;
            case ExplorableSiteData site:
                return site.DefinitionId;
            case LocalPlaceTypeData placeType:
                return placeType.DefinitionId;
            case LocalConnectionTypeData connectionType:
                return connectionType.DefinitionId;
            case OrganizationData organization:
                return organization.DefinitionId;
            default:
                return string.Empty;
        }
    }

    private static bool RequiresDefinitionId(UnityEngine.Object asset)
    {
        return asset is CapabilityAttributeData
            || asset is TraitData
            || asset is NpcData
            || asset is NpcActionData
            || asset is CityData
            || asset is ItemData
            || asset is ExplorableSiteData
            || asset is LocalPlaceTypeData
            || asset is LocalConnectionTypeData
            || asset is OrganizationData;
    }

    private static void AddIssue(
        List<ContentValidationIssue> issues,
        ContentValidationSeverity severity,
        string code,
        UnityEngine.Object asset,
        string definitionId,
        string fieldPath,
        string message)
    {
        issues.Add(new ContentValidationIssue(
            severity,
            code,
            AssetDatabase.GetAssetPath(asset),
            asset != null ? asset.GetType().Name : string.Empty,
            definitionId,
            fieldPath,
            message,
            asset));
    }

    private sealed class AssetComparer : IComparer<UnityEngine.Object>
    {
        public static readonly AssetComparer Instance = new AssetComparer();

        public int Compare(UnityEngine.Object left, UnityEngine.Object right)
        {
            int result = string.CompareOrdinal(AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right));
            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(left != null ? left.GetType().Name : string.Empty,
                right != null ? right.GetType().Name : string.Empty);
            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(GetDefinitionId(left), GetDefinitionId(right));
            if (result != 0)
            {
                return result;
            }

            return string.CompareOrdinal(left != null ? left.name : string.Empty,
                right != null ? right.name : string.Empty);
        }
    }
}
