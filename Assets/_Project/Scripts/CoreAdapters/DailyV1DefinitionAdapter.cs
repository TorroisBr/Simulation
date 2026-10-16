using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>Unity-only boundary converting the selected Daily-v1 profile to copied Core values.</summary>
public static class DailyV1DefinitionAdapter
{
    private const int MaxSelectedAssets = 65536;
    private const int MaxSelectedValueDepth = 64;
    public static bool TryCreate(SimulationConfigData source, EffectiveSimulationConfiguration effective,
        CalendarDefinition calendar, IReadOnlyList<string> stageOrder, string hexConvention,
        string hexCanonicalOrder, out DailyV1EffectiveDefinitions definitions, out string diagnostic)
    {
        definitions = null;
        diagnostic = null;
        if (source == null || effective == null || calendar == null || stageOrder == null)
        { diagnostic = "The selected Daily-v1 definition inputs are incomplete."; return false; }
        try
        {
            PrimeLazyDefaults(source);
            SortedDictionary<string, ScriptableObject> assets = CaptureAssets(source);
            DailyV1SimulationConfigData configValues = ConvertSimulationConfigData(source);
            List<DailyV1CityData> cities = new List<DailyV1CityData>();
            List<DailyV1ItemData> items = new List<DailyV1ItemData>();
            List<DailyV1NpcData> npcs = new List<DailyV1NpcData>();
            List<DailyV1NpcJobData> jobs = new List<DailyV1NpcJobData>();
            List<DailyV1NpcActionData> actions = new List<DailyV1NpcActionData>();
            List<DailyV1NpcStatusData> statuses = new List<DailyV1NpcStatusData>();
            List<KeyValuePair<string, string>> traits = new List<KeyValuePair<string, string>>();
            List<KeyValuePair<string, string>> capabilityAttributes = new List<KeyValuePair<string, string>>();
            List<DailyV1ExplorableSiteData> explorableSites = new List<DailyV1ExplorableSiteData>();
            foreach (ScriptableObject asset in assets.Values)
            {
                // CaptureAssets owns alias detection; recheck Unity fake-null at the adapter edge
                // before reading an admitted Unity object or converting it to a Core value.
                if (asset == null) throw new InvalidOperationException("The selected profile contains a destroyed definition asset.");
                string id = AssetId(asset);
                if (asset.GetType() == typeof(CityData)) cities.Add(ConvertCityData(asset));
                else if (asset.GetType() == typeof(ItemData)) items.Add(ConvertItemData(asset));
                else if (asset.GetType() == typeof(NpcData)) npcs.Add(ConvertNpcData(asset));
                else if (asset.GetType() == typeof(NpcJobData)) jobs.Add(ConvertNpcJobData(asset));
                else if (asset.GetType() == typeof(NpcActionData)) actions.Add(ConvertNpcActionData(asset));
                else if (asset.GetType() == typeof(NpcStatusData)) statuses.Add(ConvertNpcStatusData(asset));
                else if (asset.GetType() == typeof(TraitData))
                {
                    TraitData trait = (TraitData)asset;
                    if (!trait.TryValidate(out string traitDiagnostic)) throw new InvalidOperationException(traitDiagnostic);
                    traits.Add(new KeyValuePair<string, string>(id, trait.DisplayName));
                }
                else if (asset.GetType() == typeof(CapabilityAttributeData))
                {
                    CapabilityAttributeData attribute = (CapabilityAttributeData)asset;
                    if (!attribute.TryValidate(out string attributeDiagnostic)) throw new InvalidOperationException(attributeDiagnostic);
                    capabilityAttributes.Add(new KeyValuePair<string, string>(id, attribute.DisplayName));
                }
                else if (asset.GetType() == typeof(ExplorableSiteData))
                {
                    ExplorableSiteData site = (ExplorableSiteData)asset;
                    explorableSites.Add(new DailyV1ExplorableSiteData(id, site.siteName, EnumValue(site.kind)));
                }
            }
            DailyV1CalendarDefinition resolvedCalendar = new DailyV1CalendarDefinition(
                calendar.MonthsPerYear, calendar.WeeksPerMonth, calendar.DaysPerWeek, calendar.monthLengths);
            definitions = DailyV1EffectiveDefinitions.CreateSelectedSchema(configValues, effective, resolvedCalendar,
                cities, items, npcs, jobs, actions, statuses, stageOrder,
                SimulationGenesisPipeline.ProfileContractIdentity, SimulationGenesisPipeline.GeographyProfileContractIdentity,
                SimulationGenesisPipeline.GeographyStageId, hexConvention, hexCanonicalOrder,
                traits, capabilityAttributes, explorableSites);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException
            || exception is OverflowException || exception is TargetInvocationException
            || exception is EncoderFallbackException)
        {
            definitions = null;
            diagnostic = exception.Message;
            return false;
        }
    }

    private static void PrimeLazyDefaults(SimulationConfigData source)
    {
        _ = source.EnabledModules;
        List<CityData> cities = source.Cities;
        List<CityData> orderedCities = new List<CityData>(cities);
        orderedCities.Sort((left, right) => StringComparer.Ordinal.Compare(left?.DefinitionId, right?.DefinitionId));
        foreach (CityData city in orderedCities) if (city != null) { _ = city.MarketLiquidity; _ = city.PopulationConsumption; }

        _ = source.ExplorableSites;
        List<NpcSimulationConfig> npcs = source.Npcs;
        List<NpcSimulationConfig> orderedNpcs = new List<NpcSimulationConfig>(npcs);
        orderedNpcs.Sort((left, right) => StringComparer.Ordinal.Compare(left?.npc?.DefinitionId, right?.npc?.DefinitionId));
        foreach (NpcSimulationConfig npc in orderedNpcs) if (npc != null) { _ = npc.InitialInventory; _ = npc.InitialKnownExplorableSites; }

        _ = source.Actions;
        _ = source.Statuses;
        List<NpcJobData> jobs = source.Jobs;
        HashSet<NpcJobData> selectedJobs = new HashSet<NpcJobData>(jobs);
        foreach (NpcSimulationConfig npc in npcs)
            if (npc?.npc?.job != null) selectedJobs.Add(npc.npc.job);
        List<NpcJobData> orderedJobs = new List<NpcJobData>(selectedJobs);
        orderedJobs.Sort((left, right) => StringComparer.Ordinal.Compare(left?.DefinitionId, right?.DefinitionId));
        foreach (NpcJobData job in orderedJobs) if (job != null) _ = job.PreferredTradeItems;

        HashSet<ItemData> selectedItems = new HashSet<ItemData>();
        foreach (CityData city in cities)
        {
            if (city == null) continue;
            foreach (MarketItemConfig market in city.marketItems ?? new List<MarketItemConfig>()) selectedItems.Add(market?.item);
            foreach (CityProductionConfig production in city.productionConfigs ?? new List<CityProductionConfig>()) selectedItems.Add(production?.item);
        }
        foreach (NpcSimulationConfig npc in npcs)
        {
            if (npc == null) continue;
            foreach (NpcInitialInventoryItemConfig item in npc.InitialInventory) selectedItems.Add(item?.item);
            if (npc.npc?.job != null) foreach (TradeItemPreference item in npc.npc.job.PreferredTradeItems) selectedItems.Add(item?.item);
        }
        foreach (NpcJobData job in jobs)
            if (job != null) foreach (TradeItemPreference item in job.PreferredTradeItems) selectedItems.Add(item?.item);
        List<ItemData> orderedItems = new List<ItemData>(selectedItems);
        orderedItems.Sort((left, right) => StringComparer.Ordinal.Compare(left?.DefinitionId, right?.DefinitionId));
        foreach (ItemData item in orderedItems) if (item != null) _ = item.CapabilityModifiers;

        _ = source.InitialWarrants;
        _ = source.ScheduledDirectives;
    }

    private static SortedDictionary<string, ScriptableObject> CaptureAssets(SimulationConfigData root)
    {
        SortedDictionary<string, ScriptableObject> assets = new SortedDictionary<string, ScriptableObject>(StringComparer.Ordinal);
        Queue<ScriptableObject> pending = new Queue<ScriptableObject>();
        HashSet<object> visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (FieldInfo field in SerializableFields(root.GetType()))
            CollectReferences(field.GetValue(root), assets, pending, visited, 0);
        while (pending.Count > 0)
        {
            ScriptableObject asset = pending.Dequeue();
            foreach (FieldInfo field in SerializableFields(asset.GetType()))
                CollectReferences(field.GetValue(asset), assets, pending, visited, 0);
        }
        if (assets.Count > MaxSelectedAssets) throw new InvalidOperationException("The selected content graph exceeds its asset bound.");
        return assets;
    }

    private static void CollectReferences(object value, SortedDictionary<string, ScriptableObject> assets,
        Queue<ScriptableObject> pending, HashSet<object> visited, int depth)
    {
        if (depth > MaxSelectedValueDepth) throw new InvalidOperationException("The selected definition graph exceeds its depth bound.");
        if (value == null) return;
        if (value is UnityEngine.Object unityObject)
        {
            if (!(unityObject is ScriptableObject asset)) throw new InvalidOperationException("The selected profile contains an unsupported Unity object reference.");
            if (asset == null) throw new InvalidOperationException("The selected profile contains a destroyed definition asset.");
            string key = AssetKey(asset);
            if (assets.TryGetValue(key, out ScriptableObject previous))
            {
                if (!ReferenceEquals(previous, asset)) throw new InvalidOperationException("The selected profile has duplicate stable definition identities.");
            }
            else
            {
                if (!IsSelectedAssetType(asset.GetType())) throw new InvalidOperationException("Unsupported populated definition family: " + asset.GetType().FullName);
                assets.Add(key, asset);
                pending.Enqueue(asset);
            }
            return;
        }
        if (value is IDictionary) throw new InvalidOperationException("Dictionary values are not admitted in the selected definition graph.");
        if (!visited.Add(value)) return;
        if (value is IList list)
        {
            foreach (object item in list) CollectReferences(item, assets, pending, visited, depth + 1);
            return;
        }
        foreach (FieldInfo field in SerializableFields(value.GetType()))
            CollectReferences(field.GetValue(value), assets, pending, visited, depth + 1);
    }

    private static bool IsSelectedAssetType(Type type) => type == typeof(CityData) || type == typeof(ItemData)
        || type == typeof(NpcData) || type == typeof(NpcJobData) || type == typeof(NpcActionData)
        || type == typeof(NpcStatusData) || type == typeof(TraitData)
        || type == typeof(CapabilityAttributeData) || type == typeof(ExplorableSiteData);

    private static List<FieldInfo> SerializableFields(Type type)
    {
        List<FieldInfo> fields = new List<FieldInfo>();
        for (Type current = type; current != null && current != typeof(object) && current != typeof(UnityEngine.Object); current = current.BaseType)
        {
            foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.IsStatic || field.IsNotSerialized || field.IsInitOnly) continue;
                if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField), true)
                    || Attribute.IsDefined(field, typeof(SerializeReference), true)) fields.Add(field);
            }
        }
        fields.Sort((left, right) =>
        {
            int compare = StringComparer.Ordinal.Compare(left.DeclaringType?.FullName, right.DeclaringType?.FullName);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.Name, right.Name);
        });
        return fields;
    }

    private static string AssetKey(ScriptableObject asset) => (asset.GetType().FullName ?? asset.GetType().Name) + "/" + AssetId(asset);
    private static string AssetId(object value)
    {
        if (value == null) return null;
        Type type = value.GetType();
        PropertyInfo property = type.GetProperty("DefinitionId", BindingFlags.Public | BindingFlags.Instance);
        string id = property?.GetValue(value, null) as string;
        if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("A selected definition has no stable DefinitionId: " + type.FullName);
        return id;
    }

    private static FieldInfo FindField(Type type, string name)
    {
        for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            FieldInfo field = current.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null) return field;
        }
        throw new InvalidOperationException("Selected member mapping is missing: " + type.FullName + "." + name);
    }
    private static object FieldValue(object source, string name) => source == null ? null : FindField(source.GetType(), name).GetValue(source);
    private static T ReadField<T>(object source, string name) { object value = FieldValue(source, name); return value == null ? default(T) : (T)value; }
    private static DailyV1EnumValue EnumValue(object value) => value == null ? null : new DailyV1EnumValue(Convert.ToInt32(value, CultureInfo.InvariantCulture), Convert.ToString(value, CultureInfo.InvariantCulture));
    private static DailyV1EnumValue EnumField(object source, string name) => EnumValue(FieldValue(source, name));
    private static string AssetField(object source, string name) => AssetId(FieldValue(source, name));
    private static T NestedField<T>(object source, string name, Func<object, T> convert) where T : class => convert(FieldValue(source, name));
    private static IReadOnlyList<T> ReadList<T>(object source, string name, Func<object, T> convert)
    {
        object value = FieldValue(source, name);
        if (value == null) return null;
        if (!(value is IList list)) throw new InvalidOperationException("Selected list field has an unsupported runtime value: " + name);
        List<T> copied = new List<T>(list.Count);
        foreach (object item in list) copied.Add(convert(item));
        return copied.AsReadOnly();
    }

    private static DailyV1CalendarDefinition ConvertCalendarDefinition(object source)
    {
        if (source == null) return null;
        return new DailyV1CalendarDefinition(
            ReadField<int>(source, "monthsPerYear"),
            ReadField<int>(source, "weeksPerMonth"),
            ReadField<int>(source, "daysPerWeek"),
            ReadList<int>(source, "monthLengths", item => (int)item));
    }

    private static DailyV1CapabilityAttributeModifier ConvertCapabilityAttributeModifier(object source)
    {
        if (source == null) return null;
        return new DailyV1CapabilityAttributeModifier(
            AssetField(source, "attribute"),
            ReadField<float>(source, "additiveValue"));
    }

    private static DailyV1CapabilityAttributeValue ConvertCapabilityAttributeValue(object source)
    {
        if (source == null) return null;
        return new DailyV1CapabilityAttributeValue(
            AssetField(source, "attribute"),
            ReadField<float>(source, "value"));
    }

    private static DailyV1CityConnection ConvertCityConnection(object source)
    {
        if (source == null) return null;
        return new DailyV1CityConnection(
            AssetField(source, "destination"),
            ReadField<int>(source, "travelDays"));
    }

    private static DailyV1CityData ConvertCityData(object source)
    {
        if (source == null) return null;
        return new DailyV1CityData(
            ReadField<string>(source, "id"),
            ReadField<string>(source, "cityName"),
            ReadField<string>(source, "settlementSemanticId"),
            ReadField<string>(source, "materialFlowLocationId"),
            ReadField<string>(source, "marketStoreSemanticId"),
            EnumField(source, "materialFlowProfile"),
            ReadField<int>(source, "initialPopulation"),
            NestedField(source, "marketLiquidity", ConvertMarketLiquidityConfig),
            NestedField(source, "populationConsumption", ConvertPopulationConsumptionConfig),
            ReadList<DailyV1MarketItemConfig>(source, "marketItems", item => ConvertMarketItemConfig(item)),
            ReadList<DailyV1CityProductionConfig>(source, "productionConfigs", item => ConvertCityProductionConfig(item)),
            ReadList<DailyV1CityConnection>(source, "connections", item => ConvertCityConnection(item)));
    }

    private static DailyV1CityProductionConfig ConvertCityProductionConfig(object source)
    {
        if (source == null) return null;
        return new DailyV1CityProductionConfig(
            AssetField(source, "item"),
            ReadField<int>(source, "amountPerDay"),
            ReadField<int>(source, "initialReserve"),
            ReadField<string>(source, "productionSourceId"),
            ReadField<string>(source, "contentRevision"),
            EnumField(source, "sourceKind"));
    }

    private static DailyV1CommercialKnowledgeSettings ConvertCommercialKnowledgeSettings(object source)
    {
        if (source == null) return null;
        return new DailyV1CommercialKnowledgeSettings(
            ReadField<int>(source, "freshForDays"),
            ReadField<int>(source, "maxUsefulAgeDays"),
            ReadField<int>(source, "maxSharedObservationsPerInteraction"));
    }

    private static DailyV1CrimeActionSettings ConvertCrimeActionSettings(object source)
    {
        if (source == null) return null;
        return new DailyV1CrimeActionSettings(
            ReadField<int>(source, "amount"),
            ReadField<float>(source, "bounty"),
            ReadField<int>(source, "sentenceDays"),
            ReadField<int>(source, "hiddenDays"),
            ReadField<float>(source, "escapeBountyPenalty"),
            ReadField<int>(source, "failedEscapeSentencePenalty"));
    }

    private static DailyV1ExplorableSiteConfig ConvertExplorableSiteConfig(object source)
    {
        if (source == null) return null;
        return new DailyV1ExplorableSiteConfig(
            AssetField(source, "site"),
            AssetField(source, "anchorCity"),
            ReadField<int>(source, "travelDaysFromAnchor"),
            EnumField(FieldValue(source, "site"), "kind"));
    }

    private static DailyV1InitialWantedRecordConfig ConvertInitialWantedRecordConfig(object source)
    {
        if (source == null) return null;
        return new DailyV1InitialWantedRecordConfig(
            AssetField(source, "target"),
            AssetField(source, "city"),
            ReadField<float>(source, "bounty"),
            ReadField<int>(source, "sentenceDays"));
    }

    private static DailyV1ItemData ConvertItemData(object source)
    {
        if (source == null) return null;
        return new DailyV1ItemData(
            ReadField<string>(source, "id"),
            ReadField<string>(source, "itemName"),
            ReadField<float>(source, "basePrice"),
            ReadList<DailyV1CapabilityAttributeModifier>(source, "capabilityModifiers", item => ConvertCapabilityAttributeModifier(item)));
    }

    private static DailyV1MarketItemConfig ConvertMarketItemConfig(object source)
    {
        if (source == null) return null;
        return new DailyV1MarketItemConfig(
            AssetField(source, "item"),
            ReadField<int>(source, "initialAmount"),
            ReadField<int>(source, "desiredAmount"),
            ReadField<float>(source, "consumptionPer1000Population"));
    }

    private static DailyV1MarketLiquidityConfig ConvertMarketLiquidityConfig(object source)
    {
        if (source == null) return null;
        return new DailyV1MarketLiquidityConfig(
            EnumField(source, "liquidityMode"),
            ReadField<float>(source, "initialPurchasingPower"));
    }

    private static DailyV1NpcActionData ConvertNpcActionData(object source)
    {
        if (source == null) return null;
        return new DailyV1NpcActionData(
            ReadField<string>(source, "id"),
            ReadList<string>(source, "statusNecessariosParaFazerAcao", item => AssetId(item)),
            ReadField<string>(source, "actionName"),
            ReadField<string>(source, "normalActionLogText"),
            EnumField(source, "actionCategory"),
            EnumField(source, "actionType"),
            ReadField<float>(source, "baseUtility"),
            ReadList<DailyV1StatusWeightModifier>(source, "statusModifiers", item => ConvertStatusWeightModifier(item)),
            ReadField<bool>(source, "canFail"),
            ReadField<float>(source, "baseSuccessChance"),
            ReadList<string>(source, "statusToAdd", item => AssetId(item)),
            ReadList<string>(source, "statusToRemove", item => AssetId(item)),
            ReadList<string>(source, "targetStatusToAdd", item => AssetId(item)),
            ReadList<string>(source, "targetStatusToRemove", item => AssetId(item)),
            NestedField(source, "crimeSettings", ConvertCrimeActionSettings));
    }

    private static DailyV1NpcData ConvertNpcData(object source)
    {
        if (source == null) return null;
        return new DailyV1NpcData(
            ReadField<string>(source, "id"),
            ReadField<string>(source, "name"),
            AssetField(source, "job"),
            ReadList<DailyV1NPCDefaultAction>(source, "acoesPadrao", item => ConvertNPCDefaultAction(item)),
            ReadList<string>(source, "statusPadrao", item => AssetId(item)),
            ReadList<DailyV1CapabilityAttributeValue>(source, "capabilityValues", item => ConvertCapabilityAttributeValue(item)),
            ReadList<string>(source, "traits", item => AssetId(item)));
    }

    private static DailyV1NPCDefaultAction ConvertNPCDefaultAction(object source)
    {
        if (source == null) return null;
        return new DailyV1NPCDefaultAction(
            AssetField(source, "action"),
            ReadField<float>(source, "baseUtility"));
    }

    private static DailyV1NpcInitialInventoryItemConfig ConvertNpcInitialInventoryItemConfig(object source)
    {
        if (source == null) return null;
        return new DailyV1NpcInitialInventoryItemConfig(
            AssetField(source, "item"),
            ReadField<int>(source, "amount"),
            ReadField<float>(source, "averageUnitCost"));
    }

    private static DailyV1NpcJobData ConvertNpcJobData(object source)
    {
        if (source == null) return null;
        return new DailyV1NpcJobData(
            ReadField<string>(source, "id"),
            ReadField<string>(source, "jobName"),
            EnumField(source, "jobType"),
            EnumField(source, "merchantBehavior"),
            ReadField<float>(source, "minimumProfitPerItem"),
            AssetField(source, "workAction"),
            ReadField<float>(source, "workUtility"),
            ReadList<DailyV1TradeItemPreference>(source, "preferredTradeItems", item => ConvertTradeItemPreference(item)));
    }

    private static DailyV1NpcSimulationConfig ConvertNpcSimulationConfig(object source)
    {
        if (source == null) return null;
        return new DailyV1NpcSimulationConfig(
            AssetField(source, "npc"),
            AssetField(source, "startingCity"),
            ReadField<float>(source, "initialMoney"),
            ReadList<DailyV1NpcInitialInventoryItemConfig>(source, "initialInventory", item => ConvertNpcInitialInventoryItemConfig(item)),
            ReadList<string>(source, "initialKnownExplorableSites", item => AssetId(item)));
    }

    private static DailyV1NpcStatusData ConvertNpcStatusData(object source)
    {
        if (source == null) return null;
        return new DailyV1NpcStatusData(
            ReadField<string>(source, "id"),
            ReadField<string>(source, "statusName"));
    }

    private static DailyV1PopulationConsumptionConfig ConvertPopulationConsumptionConfig(object source)
    {
        if (source == null) return null;
        return new DailyV1PopulationConsumptionConfig(
            EnumField(source, "paymentMode"),
            ReadField<float>(source, "initialPurchasingPower"));
    }

    private static DailyV1ScheduledDirectiveConfig ConvertScheduledDirectiveConfig(object source)
    {
        if (source == null) return null;
        return new DailyV1ScheduledDirectiveConfig(
            ReadField<long>(source, "absoluteDay"),
            EnumField(source, "mode"),
            EnumField(source, "operation"),
            AssetField(source, "actor"),
            AssetField(source, "action"));
    }

    private static DailyV1SimulationConfigData ConvertSimulationConfigData(object source)
    {
        if (source == null) return null;
        return new DailyV1SimulationConfigData(
            ReadField<string>(source, "simulationName"),
            NestedField(source, "calendar", ConvertCalendarDefinition),
            ReadList<DailyV1EnumValue>(source, "enabledModules", item => EnumValue(item)),
            ReadList<string>(source, "cities", item => AssetId(item)),
            ReadList<DailyV1ExplorableSiteConfig>(source, "explorableSites", item => ConvertExplorableSiteConfig(item)),
            ReadList<DailyV1NpcSimulationConfig>(source, "npcs", item => ConvertNpcSimulationConfig(item)),
            ReadList<string>(source, "actions", item => AssetId(item)),
            ReadList<string>(source, "statuses", item => AssetId(item)),
            ReadList<string>(source, "jobs", item => AssetId(item)),
            ReadList<DailyV1InitialWantedRecordConfig>(source, "initialWarrants", item => ConvertInitialWantedRecordConfig(item)),
            ReadList<DailyV1ScheduledDirectiveConfig>(source, "scheduledDirectives", item => ConvertScheduledDirectiveConfig(item)),
            ReadField<bool>(source, "useAuthoredGeographyProfile"),
            ReadField<string>(source, "authoredHexId"),
            ReadField<int>(source, "authoredHexQ"),
            ReadField<int>(source, "authoredHexR"),
            ReadField<string>(source, "authoredTerrainDefinitionId"),
            ReadField<string>(source, "authoredTerrainRevisionToken"),
            ReadField<string>(source, "authoredLocationId"),
            ReadField<string>(source, "authoredScaleConventionId"),
            ReadField<string>(source, "authoredScaleSourceIdentity"),
            ReadField<string>(source, "authoredScaleSourceVersion"),
            ReadField<string>(source, "authoredDistancePerNeighborStep"),
            ReadField<string>(source, "authoredScaleUnit"),
            AssetField(source, "authoredP10RuinSite"),
            ReadField<string>(source, "genesisProfileContractIdentity"),
            ReadField<string>(source, "p10bStableSiteKey"),
            ReadField<float>(source, "travelCostPerDay"),
            ReadField<bool>(source, "allowMerchantTradeRepositioning"),
            ReadField<int>(source, "maxMerchantTradeAmount"),
            ReadField<float>(source, "localMerchantWholesalePriceMultiplier"),
            ReadField<float>(source, "localMerchantReserveRatio"),
            ReadField<int>(source, "maxUnprofitablePlanWaitDays"),
            ReadField<bool>(source, "crimeAutonomousEnabled"),
            NestedField(source, "commercialKnowledge", ConvertCommercialKnowledgeSettings),
            ReadField<bool>(source, "useFixedSimulationSeed"),
            ReadField<int>(source, "simulationSeed"),
            ReadField<bool>(source, "includeEconomySnapshots"),
            ReadField<int>(source, "economySnapshotIntervalDays"),
            NestedField(source, "logSettings", ConvertSimulationLogSettings),
            ReadField<bool>(source, "naturalMortalityEnabled"),
            ReadField<double>(source, "naturalMortalityAnnualProbability"),
            ReadField<bool>(source, "aggregateDemographyEnabled"),
            ReadField<double>(source, "aggregateAnnualBirthRate"),
            ReadField<double>(source, "aggregateAnnualDeathRate"),
            AssetField(source, "freeStatus"),
            AssetField(source, "wantedStatus"),
            AssetField(source, "arrestedStatus"),
            AssetField(source, "hiddenStatus"));
    }

    private static DailyV1SimulationLogSettings ConvertSimulationLogSettings(object source)
    {
        if (source == null) return null;
        return new DailyV1SimulationLogSettings(
            ReadField<bool>(source, "showDay"),
            ReadField<bool>(source, "npcActions"),
            ReadField<bool>(source, "trade"),
            ReadField<bool>(source, "travel"),
            ReadField<bool>(source, "crime"),
            ReadField<bool>(source, "justice"),
            ReadField<bool>(source, "economyProduction"),
            ReadField<bool>(source, "economyConsumption"),
            ReadField<bool>(source, "market"));
    }

    private static DailyV1StatusWeightModifier ConvertStatusWeightModifier(object source)
    {
        if (source == null) return null;
        return new DailyV1StatusWeightModifier(
            AssetField(source, "status"),
            ReadField<float>(source, "multiplier"));
    }

    private static DailyV1TradeItemPreference ConvertTradeItemPreference(object source)
    {
        if (source == null) return null;
        return new DailyV1TradeItemPreference(
            AssetField(source, "item"),
            ReadField<float>(source, "utilityMultiplier"));
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
        public new bool Equals(object left, object right) => ReferenceEquals(left, right);
        public int GetHashCode(object value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
}
