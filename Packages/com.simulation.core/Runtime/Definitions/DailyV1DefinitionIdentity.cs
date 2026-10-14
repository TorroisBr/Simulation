using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>Pure identity encodings retained by the selected Daily-v1 profile.</summary>
public static class DailyV1DefinitionIdentity
{
    public const string ProviderContractIdentity = "p12a.daily-v1.provider-contract";
    public const int ProviderContractVersion = 1;

    /// <summary>Hashes P9 records using its existing big-endian UTF-8 length framing.</summary>
    public static string CreateP9Fingerprint(IEnumerable<string> records)
    {
        if (records == null) throw new ArgumentNullException(nameof(records));
        using (SHA256 sha = SHA256.Create())
        {
            List<byte> bytes = new List<byte>();
            foreach (string record in records)
            {
                byte[] value = Encoding.UTF8.GetBytes(record ?? string.Empty);
                bytes.Add((byte)((value.Length >> 24) & 0xff));
                bytes.Add((byte)((value.Length >> 16) & 0xff));
                bytes.Add((byte)((value.Length >> 8) & 0xff));
                bytes.Add((byte)(value.Length & 0xff));
                bytes.AddRange(value);
            }
            return Hex(sha.ComputeHash(bytes.ToArray()));
        }
    }

    /// <summary>Builds the existing curated P9 record stream from copied Daily-v1 values.</summary>
    public static string CreateP9Fingerprint(DailyV1EffectiveDefinitions definitions,
        out IReadOnlyList<string> canonicalRecords)
    {
        if (definitions == null) throw new ArgumentNullException(nameof(definitions));
        DailyV1SimulationConfigData config = definitions.Configuration;
        EffectiveSimulationConfiguration effective = definitions.EffectiveConfiguration;
        List<string> records = new List<string>
        {
            config.useAuthoredGeographyProfile ? definitions.GeographyProfileContractIdentity : definitions.ProfileContractIdentity,
            config.useAuthoredGeographyProfile ? "schema=2" : "schema=1",
            config.useFixedSimulationSeed ? "fixed-seed" : "default-seed",
            (config.useFixedSimulationSeed ? config.simulationSeed : 0).ToString(CultureInfo.InvariantCulture)
        };

        List<string> modules = new List<string>();
        if (config.enabledModules != null)
            foreach (DailyV1EnumValue module in config.enabledModules) modules.Add(module?.Spelling ?? string.Empty);
        modules.Sort(StringComparer.Ordinal);
        foreach (string module in modules) Add(records, "module", module);

        DailyV1CalendarDefinition calendar = definitions.Calendar;
        Add(records, "calendar", calendar.monthsPerYear, calendar.weeksPerMonth, calendar.daysPerWeek);
        int monthIndex = 0;
        foreach (int monthLength in calendar.monthLengths ?? Array.Empty<int>())
            Add(records, "calendar-month-length", monthIndex++, monthLength);

        Add(records, "configuration",
            config.travelCostPerDay.ToString("R", CultureInfo.InvariantCulture), config.allowMerchantTradeRepositioning.ToString(),
            config.maxMerchantTradeAmount.ToString(CultureInfo.InvariantCulture), config.localMerchantWholesalePriceMultiplier.ToString("R", CultureInfo.InvariantCulture),
            config.localMerchantReserveRatio.ToString("R", CultureInfo.InvariantCulture), config.maxUnprofitablePlanWaitDays.ToString(CultureInfo.InvariantCulture),
            config.crimeAutonomousEnabled.ToString(), config.naturalMortalityEnabled.ToString(), config.naturalMortalityAnnualProbability.ToString("R", CultureInfo.InvariantCulture),
            config.aggregateDemographyEnabled.ToString(), config.aggregateAnnualBirthRate.ToString("R", CultureInfo.InvariantCulture), config.aggregateAnnualDeathRate.ToString("R", CultureInfo.InvariantCulture));
        Add(records, "effective.population", effective.Population.RepresentationMode,
            effective.Population.DecisionScope, effective.Population.MaturityAgeYears);
        Add(records, "effective.economy", effective.Economy.Enabled);
        Add(records, "effective.travel", effective.Travel.TravelCostPerDay.ToString("R", CultureInfo.InvariantCulture));
        Add(records, "effective.crime", effective.Crime.Enabled, effective.Crime.AutonomousEnabled);
        Add(records, "effective.guard-crime", effective.GuardCrime.Enabled);
        Add(records, "effective.merchant-trade", effective.MerchantTrade.Enabled,
            effective.MerchantTrade.AllowAutonomousTradeRepositioning, effective.MerchantTrade.MaxTradeAmount,
            effective.MerchantTrade.LocalWholesalePriceMultiplier.ToString("R", CultureInfo.InvariantCulture),
            effective.MerchantTrade.LocalReserveRatio.ToString("R", CultureInfo.InvariantCulture),
            effective.MerchantTrade.MaxUnprofitablePlanWaitDays);
        Add(records, "effective.commercial-knowledge", effective.CommercialKnowledge.FreshForDays,
            effective.CommercialKnowledge.MaxUsefulAgeDays, effective.CommercialKnowledge.MaxSharedObservationsPerInteraction);
        Add(records, "effective.natural-mortality", effective.NaturalMortality.Policy,
            effective.NaturalMortality.AnnualProbability.ToString("R", CultureInfo.InvariantCulture));
        Add(records, "effective.aggregate-demography", effective.AggregateDemography.Policy,
            effective.AggregateDemography.AnnualBirthRate.ToString("R", CultureInfo.InvariantCulture),
            effective.AggregateDemography.AnnualDeathRate.ToString("R", CultureInfo.InvariantCulture));

        List<string> cityIds = new List<string>(config.cities ?? Array.Empty<string>());
        cityIds.Sort(StringComparer.Ordinal);
        foreach (string cityId in cityIds)
        {
            DailyV1CityData city = City(definitions, cityId);
            Add(records, "city", city?.id, city?.initialPopulation,
                city?.marketLiquidity?.liquidityMode?.Spelling,
                city?.marketLiquidity?.initialPurchasingPower.ToString("R", CultureInfo.InvariantCulture),
                city?.populationConsumption?.paymentMode?.Spelling,
                city?.populationConsumption?.initialPurchasingPower.ToString("R", CultureInfo.InvariantCulture));
            if (city == null) continue;
            List<DailyV1MarketItemConfig> markets = new List<DailyV1MarketItemConfig>(city.marketItems ?? Array.Empty<DailyV1MarketItemConfig>());
            markets.Sort((a, b) => StringComparer.Ordinal.Compare(a?.item, b?.item));
            foreach (DailyV1MarketItemConfig market in markets)
                Add(records, "market", city.id, market?.item, market?.initialAmount, market?.desiredAmount,
                    market?.consumptionPer1000Population.ToString("R", CultureInfo.InvariantCulture));
            int productionIndex = 0;
            foreach (DailyV1CityProductionConfig input in city.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>())
                Add(records, "production", city.id, productionIndex++, input?.item, input?.amountPerDay);
            List<DailyV1CityConnection> routes = new List<DailyV1CityConnection>(city.connections ?? Array.Empty<DailyV1CityConnection>());
            routes.Sort((a, b) =>
            {
                int compare = StringComparer.Ordinal.Compare(a?.destination, b?.destination);
                return compare != 0 ? compare : Nullable.Compare(a?.travelDays, b?.travelDays);
            });
            foreach (DailyV1CityConnection route in routes)
                Add(records, "connection", city.id, route?.destination, route?.travelDays);
        }

        if (config.explorableSites != null && config.explorableSites.Count != 0)
            throw new InvalidOperationException("The selected Daily-v1 value closure does not include populated ExplorableSiteData definitions.");

        List<DailyV1NpcSimulationConfig> npcs = new List<DailyV1NpcSimulationConfig>(config.npcs ?? Array.Empty<DailyV1NpcSimulationConfig>());
        npcs.Sort((a, b) => StringComparer.Ordinal.Compare(a?.npc, b?.npc));
        foreach (DailyV1NpcSimulationConfig row in npcs)
        {
            DailyV1NpcData npc = Npc(definitions, row?.npc);
            Add(records, "npc", row?.npc, row?.startingCity,
                row?.initialMoney.ToString("R", CultureInfo.InvariantCulture), npc?.job);
            if (row == null || npc == null) continue;
            AddOrdered(records, "npc-default-action:" + npc.id, npc.acoesPadrao,
                value => Pack(value?.action, value?.baseUtility.ToString("R", CultureInfo.InvariantCulture)));
            AddOrdered(records, "npc-default-status:" + npc.id, npc.statusPadrao, value => value ?? string.Empty);
            AddOrdered(records, "npc-trait:" + npc.id, npc.traits, value => value ?? string.Empty);
            List<string> capabilities = new List<string>();
            foreach (DailyV1CapabilityAttributeValue value in npc.capabilityValues ?? Array.Empty<DailyV1CapabilityAttributeValue>())
                capabilities.Add(Pack(value?.attribute, value?.value.ToString("R", CultureInfo.InvariantCulture)));
            capabilities.Sort(StringComparer.Ordinal);
            foreach (string capability in capabilities) Add(records, "npc-capability", npc.id, capability);
            AddOrdered(records, "inventory:" + npc.id, row.initialInventory,
                value => Pack(value?.item, value?.amount, value?.averageUnitCost.ToString("R", CultureInfo.InvariantCulture)));
            AddOrdered(records, "known-site:" + npc.id, row.initialKnownExplorableSites, value => value ?? string.Empty);
        }

        List<string> actionIds = new List<string>(config.actions ?? Array.Empty<string>());
        actionIds.Sort(StringComparer.Ordinal);
        foreach (string actionId in actionIds)
        {
            DailyV1NpcActionData action = Action(definitions, actionId);
            Add(records, "action", action?.id, action?.actionType?.Spelling, action?.actionCategory?.Spelling,
                action?.baseUtility.ToString("R", CultureInfo.InvariantCulture), action?.canFail,
                action?.baseSuccessChance.ToString("R", CultureInfo.InvariantCulture), action?.crimeSettings?.amount,
                action?.crimeSettings?.bounty.ToString("R", CultureInfo.InvariantCulture), action?.crimeSettings?.sentenceDays,
                action?.crimeSettings?.hiddenDays, action?.crimeSettings?.escapeBountyPenalty.ToString("R", CultureInfo.InvariantCulture),
                action?.crimeSettings?.failedEscapeSentencePenalty);
            AddOrdered(records, "action-required-status:" + (action?.id ?? string.Empty), action?.statusNecessariosParaFazerAcao, value => value ?? string.Empty);
            AddOrdered(records, "action-add-status:" + (action?.id ?? string.Empty), action?.statusToAdd, value => value ?? string.Empty);
            AddOrdered(records, "action-remove-status:" + (action?.id ?? string.Empty), action?.statusToRemove, value => value ?? string.Empty);
            AddOrdered(records, "action-target-add-status:" + (action?.id ?? string.Empty), action?.targetStatusToAdd, value => value ?? string.Empty);
            AddOrdered(records, "action-target-remove-status:" + (action?.id ?? string.Empty), action?.targetStatusToRemove, value => value ?? string.Empty);
            List<string> weights = new List<string>();
            foreach (DailyV1StatusWeightModifier weight in action?.statusModifiers ?? Array.Empty<DailyV1StatusWeightModifier>())
                weights.Add(Pack(weight?.status, weight?.multiplier.ToString("R", CultureInfo.InvariantCulture)));
            weights.Sort(StringComparer.Ordinal);
            foreach (string weight in weights) Add(records, "action-status-weight", action?.id, weight);
        }

        HashSet<string> selectedStatuses = new HashSet<string>(config.statuses ?? Array.Empty<string>(), StringComparer.Ordinal);
        AddIfPresent(selectedStatuses, config.freeStatus); AddIfPresent(selectedStatuses, config.wantedStatus);
        AddIfPresent(selectedStatuses, config.arrestedStatus); AddIfPresent(selectedStatuses, config.hiddenStatus);
        foreach (DailyV1NpcSimulationConfig row in config.npcs ?? Array.Empty<DailyV1NpcSimulationConfig>())
        {
            DailyV1NpcData npc = Npc(definitions, row?.npc);
            if (npc?.statusPadrao != null) selectedStatuses.UnionWith(npc.statusPadrao);
        }
        foreach (string actionId in config.actions ?? Array.Empty<string>())
        {
            DailyV1NpcActionData action = Action(definitions, actionId);
            if (action == null) continue;
            AddAll(selectedStatuses, action.statusNecessariosParaFazerAcao); AddAll(selectedStatuses, action.statusToAdd);
            AddAll(selectedStatuses, action.statusToRemove); AddAll(selectedStatuses, action.targetStatusToAdd);
            AddAll(selectedStatuses, action.targetStatusToRemove);
            if (action.statusModifiers != null) foreach (DailyV1StatusWeightModifier weight in action.statusModifiers) AddIfPresent(selectedStatuses, weight?.status);
        }
        List<string> orderedStatuses = new List<string>(selectedStatuses);
        orderedStatuses.Sort(StringComparer.Ordinal);
        foreach (string status in orderedStatuses) Add(records, "status", status);

        HashSet<string> selectedJobs = new HashSet<string>(config.jobs ?? Array.Empty<string>(), StringComparer.Ordinal);
        foreach (DailyV1NpcSimulationConfig row in config.npcs ?? Array.Empty<DailyV1NpcSimulationConfig>())
            AddIfPresent(selectedJobs, Npc(definitions, row?.npc)?.job);
        List<string> orderedJobs = new List<string>(selectedJobs); orderedJobs.Sort(StringComparer.Ordinal);
        foreach (string jobId in orderedJobs)
        {
            DailyV1NpcJobData job = Job(definitions, jobId);
            if (job == null) continue;
            Add(records, "job", job.id, job.jobType?.Spelling, job.merchantBehavior?.Spelling,
                job.minimumProfitPerItem.ToString("R", CultureInfo.InvariantCulture), job.workAction,
                job.workUtility.ToString("R", CultureInfo.InvariantCulture));
            AddOrdered(records, "job-preference:" + job.id, job.preferredTradeItems,
                value => Pack(value?.item, value?.utilityMultiplier.ToString("R", CultureInfo.InvariantCulture)));
        }

        HashSet<string> selectedItems = new HashSet<string>(StringComparer.Ordinal);
        foreach (string cityId in config.cities ?? Array.Empty<string>())
        {
            DailyV1CityData city = City(definitions, cityId);
            if (city == null) continue;
            if (city.marketItems != null) foreach (DailyV1MarketItemConfig market in city.marketItems) AddIfPresent(selectedItems, market?.item);
            if (city.productionConfigs != null) foreach (DailyV1CityProductionConfig production in city.productionConfigs) AddIfPresent(selectedItems, production?.item);
        }
        foreach (DailyV1NpcSimulationConfig row in config.npcs ?? Array.Empty<DailyV1NpcSimulationConfig>())
        {
            if (row?.initialInventory != null) foreach (DailyV1NpcInitialInventoryItemConfig item in row.initialInventory) AddIfPresent(selectedItems, item?.item);
            DailyV1NpcJobData job = Job(definitions, Npc(definitions, row?.npc)?.job);
            if (job?.preferredTradeItems != null) foreach (DailyV1TradeItemPreference item in job.preferredTradeItems) AddIfPresent(selectedItems, item?.item);
        }
        foreach (string jobId in config.jobs ?? Array.Empty<string>())
        {
            DailyV1NpcJobData job = Job(definitions, jobId);
            if (job?.preferredTradeItems != null) foreach (DailyV1TradeItemPreference item in job.preferredTradeItems) AddIfPresent(selectedItems, item?.item);
        }
        List<string> orderedItems = new List<string>(selectedItems); orderedItems.Sort(StringComparer.Ordinal);
        foreach (string itemId in orderedItems)
        {
            DailyV1ItemData item = Item(definitions, itemId);
            if (item == null) continue;
            Add(records, "item", item.id, item.basePrice.ToString("R", CultureInfo.InvariantCulture));
            List<string> modifiers = new List<string>();
            foreach (DailyV1CapabilityAttributeModifier modifier in item.capabilityModifiers ?? Array.Empty<DailyV1CapabilityAttributeModifier>())
                modifiers.Add(Pack(modifier?.attribute, modifier?.additiveValue.ToString("R", CultureInfo.InvariantCulture)));
            modifiers.Sort(StringComparer.Ordinal);
            foreach (string modifier in modifiers) Add(records, "item-capability-modifier", item.id, modifier);
        }

        HashSet<string> traits = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> attributes = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1NpcSimulationConfig row in config.npcs ?? Array.Empty<DailyV1NpcSimulationConfig>())
        {
            DailyV1NpcData npc = Npc(definitions, row?.npc);
            if (npc?.traits != null) AddAll(traits, npc.traits);
            if (npc?.capabilityValues != null) foreach (DailyV1CapabilityAttributeValue value in npc.capabilityValues) AddIfPresent(attributes, value?.attribute);
        }
        foreach (string itemId in orderedItems)
        {
            DailyV1ItemData item = Item(definitions, itemId);
            if (item?.capabilityModifiers != null) foreach (DailyV1CapabilityAttributeModifier modifier in item.capabilityModifiers) AddIfPresent(attributes, modifier?.attribute);
        }
        List<string> orderedTraits = new List<string>(traits); orderedTraits.Sort(StringComparer.Ordinal);
        foreach (string trait in orderedTraits) Add(records, "trait", trait);
        List<string> orderedAttributes = new List<string>(attributes); orderedAttributes.Sort(StringComparer.Ordinal);
        foreach (string attribute in orderedAttributes) Add(records, "capability-attribute", attribute);

        AddOrdered(records, "warrant", config.initialWarrants, value =>
            Pack(value?.target, value?.city, value?.bounty.ToString("R", CultureInfo.InvariantCulture), value?.sentenceDays));
        AddOrdered(records, "directive", config.scheduledDirectives, value =>
            Pack(value?.absoluteDay, value?.mode?.Spelling, value?.operation?.Spelling, value?.actor, value?.action));

        if (config.useAuthoredGeographyProfile)
        {
            Add(records, "authored-geography-stage", definitions.GeographyStageIdentity, 1);
            Add(records, "authored-hex", config.authoredHexId, config.authoredHexQ, config.authoredHexR,
                definitions.HexCoordinateConvention, definitions.HexCoordinateCanonicalOrder,
                config.authoredTerrainDefinitionId, config.authoredTerrainRevisionToken);
            Add(records, "authored-location", config.authoredLocationId, config.authoredHexId);
            Add(records, "authored-scale", config.authoredScaleConventionId, config.authoredScaleSourceIdentity,
                config.authoredScaleSourceVersion, config.authoredDistancePerNeighborStep, config.authoredScaleUnit);
            Add(records, "stage-input", definitions.GeographyStageIdentity, "authored-geography-profile/v1");
            Add(records, "stage-output", definitions.GeographyStageIdentity, "SpatialAuthorityStore/geography-v1");
            records.Add("edge:p9.genesis.resolve-profile/v1>" + definitions.GeographyStageIdentity);
            records.Add("edge:p9.genesis.authored-world/v1>" + definitions.GeographyStageIdentity);
            records.Add("edge:" + definitions.GeographyStageIdentity + ">p9.genesis.authored-actors/v1");
            records.Add("edge:" + definitions.GeographyStageIdentity + ">p9.genesis.validate-profile/v1");
        }
        foreach (string stage in definitions.StageOrder) records.Add("stage:" + stage);
        records.Add("edge:p9.genesis.resolve-profile/v1>p9.genesis.authored-world/v1");
        records.Add("edge:p9.genesis.resolve-profile/v1>p9.genesis.authored-actors/v1");
        records.Add("edge:p9.genesis.authored-world/v1>p9.genesis.authored-actors/v1");
        records.Add("edge:p9.genesis.authored-world/v1>p9.genesis.validate-profile/v1");
        records.Add("edge:p9.genesis.authored-actors/v1>p9.genesis.validate-profile/v1");
        records.Add("edge:p9.genesis.validate-profile/v1>p9.genesis.publish/v1");
        foreach (string owner in new[] { "CityRuntime", "MarketCounterpartyRuntime", "PopulationEconomyRuntime", "CityProductionInputs", "SpatialNetworkRuntime", "ExplorableSiteStore", "NpcRuntime", "InventoryRuntime", "InitialKnowledge", "JusticeSystem", "ScheduledDirectiveStore", "SimulationRuntime" })
            Add(records, "output-owner", owner);
        records.Add("first-simulated-boundary:day-1");

        canonicalRecords = records.AsReadOnly();
        return CreateP9Fingerprint(records);
    }

    private static DailyV1CityData City(DailyV1EffectiveDefinitions d, string id) => id != null && d.Cities.TryGetValue(id, out DailyV1CityDefinition value) ? value.Values : null;
    private static DailyV1ItemData Item(DailyV1EffectiveDefinitions d, string id) => id != null && d.Items.TryGetValue(id, out DailyV1ItemDefinition value) ? value.Values : null;
    private static DailyV1NpcData Npc(DailyV1EffectiveDefinitions d, string id) => id != null && d.Npcs.TryGetValue(id, out DailyV1NpcDefinition value) ? value.Values : null;
    private static DailyV1NpcJobData Job(DailyV1EffectiveDefinitions d, string id) => id != null && d.Jobs.TryGetValue(id, out DailyV1JobDefinition value) ? value.Values : null;
    private static DailyV1NpcActionData Action(DailyV1EffectiveDefinitions d, string id) => id != null && d.Actions.TryGetValue(id, out DailyV1ActionDefinition value) ? value.Values : null;
    private static void AddIfPresent(HashSet<string> values, string value) { if (value != null) values.Add(value); }
    private static void AddAll(HashSet<string> values, IEnumerable<string> source) { if (source != null) foreach (string value in source) AddIfPresent(values, value); }

    private static void Add<T>(List<string> records, string kind, params T[] values)
    {
        StringBuilder record = new StringBuilder(kind);
        foreach (object value in values)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            record.Append('|').Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text);
        }
        records.Add(record.ToString());
    }

    private static string Pack(params object[] values)
    {
        StringBuilder record = new StringBuilder();
        foreach (object value in values)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            record.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text);
        }
        return record.ToString();
    }

    private static void AddOrdered<T>(List<string> records, string kind, IEnumerable<T> rows, Func<T, string> value)
    {
        if (rows == null) return;
        int index = 0;
        foreach (T row in rows) Add(records, kind, index++, value(row));
    }

    /// <summary>Hashes the versioned census groups using P12 little-endian wire framing.</summary>
    public static string CreateProviderFingerprint(IReadOnlyList<string> required,
        IReadOnlyList<string> empty, IReadOnlyList<string> npcPrefixes,
        IReadOnlyList<string> cityPrefixes)
    {
        if (required == null || empty == null || npcPrefixes == null || cityPrefixes == null)
            throw new ArgumentNullException("Daily-v1 provider groups cannot be null.");
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
        {
            ContinuationWire.WriteString(writer, ProviderContractIdentity);
            writer.Write(ProviderContractVersion);
            WriteGroup(writer, "required", required);
            WriteGroup(writer, "empty", empty);
            WriteGroup(writer, "npc-prefix", npcPrefixes);
            WriteGroup(writer, "city-prefix", cityPrefixes);
            writer.Flush();
            return Hash(stream.ToArray());
        }
    }

    /// <summary>Hashes resolved calendar fields using the current P12 calendar contract.</summary>
    public static string CreateCalendarFingerprint(DailyV1CalendarDefinition calendar)
    {
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, ContinuationWire.CreateStrictUtf8Encoding(), true))
        {
            writer.Write(calendar.monthsPerYear);
            writer.Write(calendar.weeksPerMonth);
            writer.Write(calendar.daysPerWeek);
            IReadOnlyList<int> lengths = calendar.monthLengths;
            writer.Write(lengths == null ? 0 : lengths.Count);
            if (lengths != null)
                foreach (int length in lengths) writer.Write(length);
            writer.Flush();
            return Hash(stream.ToArray());
        }
    }

    private static void WriteGroup(BinaryWriter writer, string group, IReadOnlyList<string> values)
    {
        ContinuationWire.WriteString(writer, group);
        writer.Write(ProviderContractVersion);
        writer.Write(values.Count);
        foreach (string value in values)
        {
            ContinuationWire.WriteString(writer, value);
            writer.Write(1);
        }
    }

    private static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create()) return Hex(sha.ComputeHash(bytes));
    }

    private static string Hex(byte[] bytes)
    {
        char[] digits = new char[bytes.Length * 2];
        const string alphabet = "0123456789abcdef";
        for (int i = 0; i < bytes.Length; i++)
        {
            digits[i * 2] = alphabet[bytes[i] >> 4];
            digits[i * 2 + 1] = alphabet[bytes[i] & 15];
        }
        return new string(digits);
    }
}
