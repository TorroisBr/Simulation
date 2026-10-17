using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

/// <summary>The fixed, built-in dependency order for authored bootstrap genesis v1.</summary>
public static class SimulationGenesisPipeline
{
    public const string ProfileContractIdentity = "unity-authored-bootstrap/genesis-v1";
    public const string GeographyProfileContractIdentity = "unity-authored-bootstrap/authored-geography-v1";
    public const string GeographyStageId = "p9.genesis.authored-geography/v1";
    public const string P10RuinProfileContractIdentity = P10RuinLocalTopologyGenesis.ContractIdentity;
    public const string P10BGeneratedRuinProfileContractIdentity = "unity-authored-bootstrap/ruin-topology-v1";
    public const string P10BGeneratedRuinStageId = "p10b.genesis.ruin-topology/v1";

    [ThreadStatic]
    private static ExecutionProbeForTest activeExecutionProbeForTest;

    /// <summary>
    /// Scoped, thread-local test witness for entry into the P9 genesis stage
    /// pipeline. It observes calls only and does not alter stage execution.
    /// </summary>
    internal sealed class ExecutionProbeForTest : IDisposable
    {
        private bool disposed;

        internal int ExecuteStagesInvocationCount { get; private set; }

        internal ExecutionProbeForTest()
        {
            if (activeExecutionProbeForTest != null)
                throw new InvalidOperationException("A P9 genesis execution probe is already active on this thread.");
            activeExecutionProbeForTest = this;
        }

        internal void RecordExecuteStagesInvocation()
        {
            ExecuteStagesInvocationCount++;
        }

        public void Dispose()
        {
            if (disposed) return;
            if (!ReferenceEquals(activeExecutionProbeForTest, this))
                throw new InvalidOperationException("The P9 genesis execution probe is not the active probe on this thread.");
            activeExecutionProbeForTest = null;
            disposed = true;
        }
    }

    internal static ExecutionProbeForTest BeginExecutionProbeForTest()
    {
        return new ExecutionProbeForTest();
    }

    public static bool IsP10BGeneratedRuinEnabled(SimulationConfigData config) => config != null
        && string.Equals(config.GenesisProfileContractIdentity, P10BGeneratedRuinProfileContractIdentity, StringComparison.Ordinal);

    private static readonly string[] StageIds =
    {
        "p9.genesis.resolve-profile/v1",
        "p9.genesis.authored-world/v1",
        "p9.genesis.authored-actors/v1",
        "p9.genesis.validate-profile/v1",
        "p9.genesis.publish/v1"
    };

    public static IReadOnlyList<string> ResolveStageOrder(
        bool includeAuthoredGeography = false,
        bool includeP10RuinLocalTopology = false,
        bool includeP10BGeneratedRuin = false)
    {
        if (includeP10RuinLocalTopology && includeP10BGeneratedRuin)
            throw new ArgumentException("A genesis profile can select only one P10 topology implementation.");
        if ((includeP10RuinLocalTopology || includeP10BGeneratedRuin) && !includeAuthoredGeography)
            throw new ArgumentException("P10 Ruin topology requires the P9-B authored geography stage.");
        string[] stages = includeP10BGeneratedRuin
            ? new[] { StageIds[0], StageIds[1], GeographyStageId, P10BGeneratedRuinStageId, StageIds[2], StageIds[3], StageIds[4] }
            : includeP10RuinLocalTopology
                ? new[] { StageIds[0], StageIds[1], GeographyStageId, StageIds[2], P10RuinLocalTopologyGenesis.StageId, StageIds[3], StageIds[4] }
                : includeAuthoredGeography
                    ? new[] { StageIds[0], StageIds[1], GeographyStageId, StageIds[2], StageIds[3], StageIds[4] }
                    : StageIds;
        var dependencies = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [StageIds[0]] = Array.Empty<string>(),
            [StageIds[1]] = new[] { StageIds[0] },
            [StageIds[2]] = new[] { StageIds[0], StageIds[1] },
            [StageIds[3]] = new[] { StageIds[1], StageIds[2] },
            [StageIds[4]] = new[] { StageIds[3] }
        };
        if (includeAuthoredGeography)
        {
            dependencies[GeographyStageId] = new[] { StageIds[0], StageIds[1] };
            dependencies[StageIds[2]] = new[] { StageIds[0], StageIds[1], GeographyStageId };
            dependencies[StageIds[3]] = new[] { StageIds[1], StageIds[2], GeographyStageId };
        }
        if (includeP10RuinLocalTopology)
        {
            dependencies[P10RuinLocalTopologyGenesis.StageId] = new[] { StageIds[1], GeographyStageId, StageIds[2] };
            dependencies[StageIds[3]] = new[] { StageIds[1], StageIds[2], GeographyStageId, P10RuinLocalTopologyGenesis.StageId };
        }
        if (includeP10BGeneratedRuin)
        {
            dependencies[P10BGeneratedRuinStageId] = new[] { StageIds[1], GeographyStageId };
            dependencies[StageIds[2]] = new[] { StageIds[0], StageIds[1], GeographyStageId, P10BGeneratedRuinStageId };
            dependencies[StageIds[3]] = new[] { StageIds[1], StageIds[2], GeographyStageId, P10BGeneratedRuinStageId };
        }
        var remaining = new HashSet<string>(stages, StringComparer.Ordinal);
        var ordered = new List<string>(stages.Length);
        while (remaining.Count > 0)
        {
            string next = null;
            foreach (string candidate in stages)
            {
                if (!remaining.Contains(candidate)) continue;
                bool ready = true;
                foreach (string dependency in dependencies[candidate])
                    if (remaining.Contains(dependency)) { ready = false; break; }
                if (!ready) continue;
                next = candidate;
                break;
            }
            if (next == null) throw new InvalidOperationException("Genesis stage dependency graph is invalid or cyclic.");
            remaining.Remove(next);
            ordered.Add(next);
        }
        return ordered.AsReadOnly();
    }

    public static void ExecuteStages(System.Action<string> execute, bool includeAuthoredGeography = false,
        bool includeP10RuinLocalTopology = false, bool includeP10BGeneratedRuin = false)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));
        activeExecutionProbeForTest?.RecordExecuteStagesInvocation();
        foreach (string stageId in ResolveStageOrder(includeAuthoredGeography, includeP10RuinLocalTopology, includeP10BGeneratedRuin))
            execute(stageId);
    }
    public static string CreateP10CombinedFingerprint(
        string selectedP9Fingerprint,
        IReadOnlyList<string> selectedP9Records,
        P10RuinLocalTopologyCandidate candidate,
        out IReadOnlyList<string> canonicalRecords)
    {
        if (string.IsNullOrWhiteSpace(selectedP9Fingerprint)) throw new ArgumentException("The selected P9 fingerprint is required.", nameof(selectedP9Fingerprint));
        if (selectedP9Records == null) throw new ArgumentNullException(nameof(selectedP9Records));
        if (candidate == null) throw new ArgumentNullException(nameof(candidate));
        var fields = new List<string>(selectedP9Records)
        {
            "selected-p9-profile-fingerprint|" + WorldStateSnapshotValue.EncodeStableKey(selectedP9Fingerprint)
        };
        foreach (string record in P10RuinLocalTopologyGenesis.CreateProvenanceRecords(candidate))
            fields.Add("p10-record|" + WorldStateSnapshotValue.EncodeStableKey(record));
        canonicalRecords = fields.AsReadOnly();
        return ComputeFingerprint(fields);
    }

    public static string CreateP10BCombinedFingerprint(
        string selectedP9Fingerprint,
        IReadOnlyList<string> selectedP9Records,
        P10BGeneratedRuinGenesis.PreparedRuin prepared,
        out IReadOnlyList<string> canonicalRecords)
    {
        if (string.IsNullOrWhiteSpace(selectedP9Fingerprint)) throw new ArgumentException("The selected P9 fingerprint is required.", nameof(selectedP9Fingerprint));
        if (selectedP9Records == null) throw new ArgumentNullException(nameof(selectedP9Records));
        if (prepared == null) throw new ArgumentNullException(nameof(prepared));
        var fields = new List<string>(selectedP9Records)
        {
            "selected-p9-profile-fingerprint|" + WorldStateSnapshotValue.EncodeStableKey(selectedP9Fingerprint)
        };
        foreach (string record in P10BGeneratedRuinGenesis.CreateProvenanceRecords(prepared))
            fields.Add("p10b-record|" + WorldStateSnapshotValue.EncodeStableKey(record));
        canonicalRecords = fields.AsReadOnly();
        // P10-B defines the composed profile identity as an explicit framed
        // composition of the selected P9 profile, request, and generated graph.
        // Provenance is recorded alongside it, but must not be re-hashed as a
        // separate generic profile formula.
        if (!string.Equals(selectedP9Fingerprint, prepared.Request.P9ProfileFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("P10-B request fingerprint was prepared for a different selected P9 profile.");
        return prepared.GeneratedTopology.ComposedProfileFingerprint;
    }

    // Hashes only the supplied retained provenance records. Continuation staging
    // uses this pure helper to verify historical records without resolving assets
    // or re-running genesis.
    internal static string ComputeFingerprint(IEnumerable<string> fields)
    {
        return DailyV1DefinitionIdentity.CreateP9Fingerprint(fields);
    }

    private static string CreateUnityFingerprintLegacy(
        SimulationConfigData config,
        EffectiveSimulationConfiguration effectiveConfiguration,
        CalendarDefinition resolvedCalendar,
        out IReadOnlyList<string> canonicalRecords)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (effectiveConfiguration == null) throw new ArgumentNullException(nameof(effectiveConfiguration));
        if (resolvedCalendar == null) throw new ArgumentNullException(nameof(resolvedCalendar));
        var fields = new List<string>
        {
            config.useAuthoredGeographyProfile ? GeographyProfileContractIdentity : ProfileContractIdentity,
            config.useAuthoredGeographyProfile ? "schema=2" : "schema=1",
            config.useFixedSimulationSeed ? "fixed-seed" : "default-seed",
            (config.useFixedSimulationSeed ? config.simulationSeed : 0).ToString(CultureInfo.InvariantCulture),
        };
        var moduleIds = config.EnabledModules.ConvertAll(value => value.ToString());
        moduleIds.Sort(StringComparer.Ordinal);
        foreach (string module in moduleIds) Add(fields, "module", module);
        CalendarDefinition calendar = resolvedCalendar;
        Add(fields, "calendar", calendar.monthsPerYear, calendar.weeksPerMonth, calendar.daysPerWeek);
        int calendarMonthIndex = 0;
        foreach (int monthLength in calendar.monthLengths ?? new List<int>())
            Add(fields, "calendar-month-length", calendarMonthIndex++, monthLength);
        Add(fields, "configuration",
            config.travelCostPerDay.ToString("R", CultureInfo.InvariantCulture), config.allowMerchantTradeRepositioning.ToString(),
            config.maxMerchantTradeAmount.ToString(CultureInfo.InvariantCulture), config.localMerchantWholesalePriceMultiplier.ToString("R", CultureInfo.InvariantCulture),
            config.localMerchantReserveRatio.ToString("R", CultureInfo.InvariantCulture), config.maxUnprofitablePlanWaitDays.ToString(CultureInfo.InvariantCulture),
            config.crimeAutonomousEnabled.ToString(), config.naturalMortalityEnabled.ToString(), config.naturalMortalityAnnualProbability.ToString("R", CultureInfo.InvariantCulture),
            config.aggregateDemographyEnabled.ToString(), config.aggregateAnnualBirthRate.ToString("R", CultureInfo.InvariantCulture), config.aggregateAnnualDeathRate.ToString("R", CultureInfo.InvariantCulture));
        Add(fields, "effective.population", effectiveConfiguration.Population.RepresentationMode,
            effectiveConfiguration.Population.DecisionScope, effectiveConfiguration.Population.MaturityAgeYears);
        Add(fields, "effective.economy", effectiveConfiguration.Economy.Enabled);
        Add(fields, "effective.travel", effectiveConfiguration.Travel.TravelCostPerDay.ToString("R", CultureInfo.InvariantCulture));
        Add(fields, "effective.crime", effectiveConfiguration.Crime.Enabled, effectiveConfiguration.Crime.AutonomousEnabled);
        Add(fields, "effective.guard-crime", effectiveConfiguration.GuardCrime.Enabled);
        Add(fields, "effective.merchant-trade", effectiveConfiguration.MerchantTrade.Enabled,
            effectiveConfiguration.MerchantTrade.AllowAutonomousTradeRepositioning, effectiveConfiguration.MerchantTrade.MaxTradeAmount,
            effectiveConfiguration.MerchantTrade.LocalWholesalePriceMultiplier.ToString("R", CultureInfo.InvariantCulture),
            effectiveConfiguration.MerchantTrade.LocalReserveRatio.ToString("R", CultureInfo.InvariantCulture),
            effectiveConfiguration.MerchantTrade.MaxUnprofitablePlanWaitDays);
        Add(fields, "effective.commercial-knowledge", effectiveConfiguration.CommercialKnowledge.FreshForDays,
            effectiveConfiguration.CommercialKnowledge.MaxUsefulAgeDays, effectiveConfiguration.CommercialKnowledge.MaxSharedObservationsPerInteraction);
        Add(fields, "effective.natural-mortality", effectiveConfiguration.NaturalMortality.Policy,
            effectiveConfiguration.NaturalMortality.AnnualProbability.ToString("R", CultureInfo.InvariantCulture));
        Add(fields, "effective.aggregate-demography", effectiveConfiguration.AggregateDemography.Policy,
            effectiveConfiguration.AggregateDemography.AnnualBirthRate.ToString("R", CultureInfo.InvariantCulture),
            effectiveConfiguration.AggregateDemography.AnnualDeathRate.ToString("R", CultureInfo.InvariantCulture));
        var cities = new List<CityData>(config.Cities);
        cities.Sort((a, b) => StringComparer.Ordinal.Compare(a?.DefinitionId, b?.DefinitionId));
        foreach (CityData city in cities)
        {
            Add(fields, "city", city?.DefinitionId, city?.initialPopulation,
                city?.MarketLiquidity?.liquidityMode, city?.MarketLiquidity?.initialPurchasingPower.ToString("R", CultureInfo.InvariantCulture),
                city?.PopulationConsumption?.paymentMode, city?.PopulationConsumption?.initialPurchasingPower.ToString("R", CultureInfo.InvariantCulture));
            if (city == null) continue;
            var markets = new List<MarketItemConfig>();
            foreach (MarketItemConfig market in city.marketItems ?? new List<MarketItemConfig>())
                markets.Add(market);
            markets.Sort((a, b) => StringComparer.Ordinal.Compare(a?.item?.DefinitionId, b?.item?.DefinitionId));
            foreach (MarketItemConfig market in markets) Add(fields, "market", city.DefinitionId, market?.item?.DefinitionId, market?.initialAmount, market?.desiredAmount, market?.consumptionPer1000Population.ToString("R", CultureInfo.InvariantCulture));
            int productionIndex = 0;
            foreach (CityProductionConfig input in city.productionConfigs ?? new List<CityProductionConfig>())
                Add(fields, "production", city.DefinitionId, productionIndex++, input?.item?.DefinitionId, input?.amountPerDay);
            var routes = new List<CityConnection>();
            foreach (CityConnection route in city.connections ?? new List<CityConnection>())
                routes.Add(route);
            routes.Sort((a, b) => { int key = StringComparer.Ordinal.Compare(a?.destination?.DefinitionId, b?.destination?.DefinitionId); return key != 0 ? key : System.Nullable.Compare(a?.travelDays, b?.travelDays); });
            foreach (CityConnection route in routes) Add(fields, "connection", city.DefinitionId, route?.destination?.DefinitionId, route?.travelDays);
        }
        var sites = new List<ExplorableSiteConfig>(config.ExplorableSites);
        sites.Sort((a, b) => StringComparer.Ordinal.Compare(a?.site?.DefinitionId, b?.site?.DefinitionId));
        foreach (ExplorableSiteConfig site in sites) Add(fields, "site", site?.site?.DefinitionId, site?.anchorCity?.DefinitionId, site?.travelDaysFromAnchor, site?.site?.kind);
        var npcs = new List<NpcSimulationConfig>(config.Npcs);
        npcs.Sort((a, b) => StringComparer.Ordinal.Compare(a?.npc?.DefinitionId, b?.npc?.DefinitionId));
        foreach (NpcSimulationConfig npc in npcs)
        {
            Add(fields, "npc", npc?.npc?.DefinitionId, npc?.startingCity?.DefinitionId, npc?.initialMoney.ToString("R", CultureInfo.InvariantCulture), npc?.npc?.job?.DefinitionId);
            if (npc == null) continue;
            AddOrdered(fields, "npc-default-action:" + npc.npc.DefinitionId, npc.npc.acoesPadrao,
                value => Pack(value?.action?.DefinitionId, value?.baseUtility.ToString("R", CultureInfo.InvariantCulture)));
            AddOrdered(fields, "npc-default-status:" + npc.npc.DefinitionId, npc.npc.statusPadrao, value => value?.DefinitionId ?? "");
            AddOrdered(fields, "npc-trait:" + npc.npc.DefinitionId, npc.npc.traits, value => value?.DefinitionId ?? "");
            var capabilities = new List<string>();
            foreach (CapabilityAttributeValue value in npc.npc.capabilityValues ?? new List<CapabilityAttributeValue>())
                capabilities.Add(Pack(value?.Attribute?.DefinitionId, value?.Value.ToString("R", CultureInfo.InvariantCulture)));
            capabilities.Sort(StringComparer.Ordinal);
            foreach (string capability in capabilities) Add(fields, "npc-capability", npc.npc.DefinitionId, capability);
            AddOrdered(fields, "inventory:" + npc.npc.DefinitionId, npc.InitialInventory, value => Pack(value?.item?.DefinitionId, value?.amount, value?.averageUnitCost.ToString("R", CultureInfo.InvariantCulture)));
            AddOrdered(fields, "known-site:" + npc.npc.DefinitionId, npc.InitialKnownExplorableSites, value => value?.DefinitionId ?? "");
        }
        var actions = new List<NpcActionData>(config.Actions);
        actions.Sort((a, b) => StringComparer.Ordinal.Compare(a?.DefinitionId, b?.DefinitionId));
        foreach (NpcActionData action in actions)
        {
            Add(fields, "action", action?.DefinitionId, action?.actionType, action?.actionCategory,
                action?.baseUtility.ToString("R", CultureInfo.InvariantCulture), action?.canFail,
                action?.baseSuccessChance.ToString("R", CultureInfo.InvariantCulture), action?.crimeSettings?.amount,
                action?.crimeSettings?.bounty.ToString("R", CultureInfo.InvariantCulture), action?.crimeSettings?.sentenceDays,
                action?.crimeSettings?.hiddenDays, action?.crimeSettings?.escapeBountyPenalty.ToString("R", CultureInfo.InvariantCulture),
                action?.crimeSettings?.failedEscapeSentencePenalty);
            AddOrdered(fields, "action-required-status:" + (action?.DefinitionId ?? ""), action?.statusNecessariosParaFazerAcao, value => value?.DefinitionId ?? "");
            AddOrdered(fields, "action-add-status:" + (action?.DefinitionId ?? ""), action?.statusToAdd, value => value?.DefinitionId ?? "");
            AddOrdered(fields, "action-remove-status:" + (action?.DefinitionId ?? ""), action?.statusToRemove, value => value?.DefinitionId ?? "");
            AddOrdered(fields, "action-target-add-status:" + (action?.DefinitionId ?? ""), action?.targetStatusToAdd, value => value?.DefinitionId ?? "");
            AddOrdered(fields, "action-target-remove-status:" + (action?.DefinitionId ?? ""), action?.targetStatusToRemove, value => value?.DefinitionId ?? "");
            var weights = new List<string>();
            foreach (StatusWeightModifier weight in action.statusModifiers ?? new List<StatusWeightModifier>())
                weights.Add(Pack(weight?.status?.DefinitionId, weight?.multiplier.ToString("R", CultureInfo.InvariantCulture)));
            weights.Sort(StringComparer.Ordinal);
            foreach (string weight in weights) Add(fields, "action-status-weight", action.DefinitionId, weight);
        }
        var selectedStatuses = new HashSet<NpcStatusData>(config.Statuses);
        selectedStatuses.Add(config.freeStatus); selectedStatuses.Add(config.wantedStatus);
        selectedStatuses.Add(config.arrestedStatus); selectedStatuses.Add(config.hiddenStatus);
        foreach (NpcSimulationConfig row in config.Npcs)
            if (row?.npc?.statusPadrao != null) selectedStatuses.UnionWith(row.npc.statusPadrao);
        foreach (NpcActionData action in config.Actions)
        {
            if (action?.statusNecessariosParaFazerAcao != null) selectedStatuses.UnionWith(action.statusNecessariosParaFazerAcao);
            if (action?.statusToAdd != null) selectedStatuses.UnionWith(action.statusToAdd);
            if (action?.statusToRemove != null) selectedStatuses.UnionWith(action.statusToRemove);
            if (action?.targetStatusToAdd != null) selectedStatuses.UnionWith(action.targetStatusToAdd);
            if (action?.targetStatusToRemove != null) selectedStatuses.UnionWith(action.targetStatusToRemove);
            if (action?.statusModifiers != null)
                foreach (StatusWeightModifier weight in action.statusModifiers)
                    if (weight != null) selectedStatuses.Add(weight.status);
        }
        var orderedStatuses = new List<NpcStatusData>(selectedStatuses);
        orderedStatuses.RemoveAll(value => value == null);
        orderedStatuses.Sort((a, b) => StringComparer.Ordinal.Compare(a.DefinitionId, b.DefinitionId));
        foreach (NpcStatusData status in orderedStatuses) Add(fields, "status", status.DefinitionId);
        var selectedJobs = new HashSet<NpcJobData>(config.Jobs);
        foreach (NpcSimulationConfig row in config.Npcs) if (row?.npc?.job != null) selectedJobs.Add(row.npc.job);
        var orderedJobs = new List<NpcJobData>(selectedJobs);
        orderedJobs.Sort((a, b) => StringComparer.Ordinal.Compare(a.DefinitionId, b.DefinitionId));
        foreach (NpcJobData job in orderedJobs)
        {
            Add(fields, "job", job.DefinitionId, job.jobType, job.merchantBehavior,
                job.minimumProfitPerItem.ToString("R", CultureInfo.InvariantCulture), job.workAction?.DefinitionId,
                job.workUtility.ToString("R", CultureInfo.InvariantCulture));
            AddOrdered(fields, "job-preference:" + job.DefinitionId, job.PreferredTradeItems,
                value => Pack(value?.item?.DefinitionId, value?.utilityMultiplier.ToString("R", CultureInfo.InvariantCulture)));
        }
        var selectedItems = new HashSet<ItemData>();
        foreach (CityData city in config.Cities)
        {
            foreach (MarketItemConfig market in city.marketItems) selectedItems.Add(market.item);
            foreach (CityProductionConfig production in city.productionConfigs) selectedItems.Add(production.item);
        }
        foreach (NpcSimulationConfig npc in config.Npcs)
        {
            foreach (NpcInitialInventoryItemConfig item in npc.InitialInventory) selectedItems.Add(item.item);
            if (npc.npc.job != null) foreach (TradeItemPreference item in npc.npc.job.PreferredTradeItems) selectedItems.Add(item.item);
        }
        foreach (NpcJobData job in config.Jobs)
            if (job != null) foreach (TradeItemPreference item in job.PreferredTradeItems) selectedItems.Add(item.item);
        var orderedItems = new List<ItemData>(selectedItems);
        orderedItems.Sort((a, b) => StringComparer.Ordinal.Compare(a?.DefinitionId, b?.DefinitionId));
        foreach (ItemData item in orderedItems)
        {
            Add(fields, "item", item.DefinitionId, item.basePrice.ToString("R", CultureInfo.InvariantCulture));
            var modifiers = new List<string>();
            foreach (CapabilityAttributeModifier modifier in item.CapabilityModifiers)
                modifiers.Add(Pack(modifier?.attribute?.DefinitionId, modifier?.additiveValue.ToString("R", CultureInfo.InvariantCulture)));
            modifiers.Sort(StringComparer.Ordinal);
            foreach (string modifier in modifiers) Add(fields, "item-capability-modifier", item.DefinitionId, modifier);
        }
        var traits = new HashSet<TraitData>();
        var attributes = new HashSet<CapabilityAttributeData>();
        foreach (NpcSimulationConfig npc in config.Npcs)
        {
            foreach (TraitData trait in npc.npc.traits) if (trait != null) traits.Add(trait);
            foreach (CapabilityAttributeValue value in npc.npc.capabilityValues) if (value?.Attribute != null) attributes.Add(value.Attribute);
        }
        foreach (ItemData item in orderedItems)
            foreach (CapabilityAttributeModifier modifier in item.CapabilityModifiers)
                if (modifier?.attribute != null) attributes.Add(modifier.attribute);
        var orderedTraits = new List<TraitData>(traits);
        orderedTraits.Sort((a, b) => StringComparer.Ordinal.Compare(a.DefinitionId, b.DefinitionId));
        foreach (TraitData trait in orderedTraits) Add(fields, "trait", trait.DefinitionId);
        var orderedAttributes = new List<CapabilityAttributeData>(attributes);
        orderedAttributes.Sort((a, b) => StringComparer.Ordinal.Compare(a.DefinitionId, b.DefinitionId));
        foreach (CapabilityAttributeData attribute in orderedAttributes) Add(fields, "capability-attribute", attribute.DefinitionId);
        AddOrdered(fields, "warrant", config.InitialWarrants, value =>
            Pack(value?.target?.DefinitionId, value?.city?.DefinitionId, value?.bounty.ToString("R", CultureInfo.InvariantCulture), value?.sentenceDays));
        AddOrdered(fields, "directive", config.ScheduledDirectives, value =>
            Pack(value?.absoluteDay, value?.mode, value?.operation, value?.actor?.DefinitionId, value?.action?.DefinitionId));
        if (config.useAuthoredGeographyProfile)
        {
            Add(fields, "authored-geography-stage", GeographyStageId, 1);
            Add(fields, "authored-hex", config.authoredHexId, config.authoredHexQ, config.authoredHexR,
                HexCoordinate.ConventionVersion, HexCoordinate.CanonicalOrder, config.authoredTerrainDefinitionId,
                config.authoredTerrainRevisionToken);
            Add(fields, "authored-location", config.authoredLocationId, config.authoredHexId);
            Add(fields, "authored-scale", config.authoredScaleConventionId, config.authoredScaleSourceIdentity,
                config.authoredScaleSourceVersion, config.authoredDistancePerNeighborStep, config.authoredScaleUnit);
            Add(fields, "stage-input", GeographyStageId, "authored-geography-profile/v1");
            Add(fields, "stage-output", GeographyStageId, "SpatialAuthorityStore/geography-v1");
            fields.Add("edge:p9.genesis.resolve-profile/v1>" + GeographyStageId);
            fields.Add("edge:p9.genesis.authored-world/v1>" + GeographyStageId);
            fields.Add("edge:" + GeographyStageId + ">p9.genesis.authored-actors/v1");
            fields.Add("edge:" + GeographyStageId + ">p9.genesis.validate-profile/v1");
        }
        foreach (string stage in ResolveStageOrder(config.useAuthoredGeographyProfile)) fields.Add("stage:" + stage);
        fields.Add("edge:p9.genesis.resolve-profile/v1>p9.genesis.authored-world/v1");
        fields.Add("edge:p9.genesis.resolve-profile/v1>p9.genesis.authored-actors/v1");
        fields.Add("edge:p9.genesis.authored-world/v1>p9.genesis.authored-actors/v1");
        fields.Add("edge:p9.genesis.authored-world/v1>p9.genesis.validate-profile/v1");
        fields.Add("edge:p9.genesis.authored-actors/v1>p9.genesis.validate-profile/v1");
        fields.Add("edge:p9.genesis.validate-profile/v1>p9.genesis.publish/v1");
        foreach (string owner in new[] { "CityRuntime", "MarketCounterpartyRuntime", "PopulationEconomyRuntime", "CityProductionInputs", "SpatialNetworkRuntime", "ExplorableSiteStore", "NpcRuntime", "InventoryRuntime", "InitialKnowledge", "JusticeSystem", "ScheduledDirectiveStore", "SimulationRuntime" })
            Add(fields, "output-owner", owner);
        fields.Add("first-simulated-boundary:day-1");

        canonicalRecords = fields.AsReadOnly();
        return ComputeFingerprint(fields);
    }

    public static string CreateFingerprint(
        SimulationConfigData config,
        EffectiveSimulationConfiguration effectiveConfiguration,
        CalendarDefinition resolvedCalendar,
        out IReadOnlyList<string> canonicalRecords)
    {
        return CreateFingerprint(config, effectiveConfiguration, resolvedCalendar, out canonicalRecords, out _);
    }

    public static string CreateFingerprint(
        SimulationConfigData config,
        EffectiveSimulationConfiguration effectiveConfiguration,
        CalendarDefinition resolvedCalendar,
        out IReadOnlyList<string> canonicalRecords,
        out DailyV1EffectiveDefinitions selectedDefinitions)
    {
        selectedDefinitions = null;
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (effectiveConfiguration == null) throw new ArgumentNullException(nameof(effectiveConfiguration));
        if (resolvedCalendar == null) throw new ArgumentNullException(nameof(resolvedCalendar));
        if (!string.Equals(config.GenesisProfileContractIdentity, ProfileContractIdentity, StringComparison.Ordinal)
            && !string.Equals(config.GenesisProfileContractIdentity, GeographyProfileContractIdentity, StringComparison.Ordinal))
        {
            // Keep legacy profile identity behavior, while making any successfully resolved typed
            // graph available to the same composition path that owns the resulting runtime nodes.
            DailyV1DefinitionAdapter.TryCreate(config, effectiveConfiguration, resolvedCalendar,
                ResolveStageOrder(config.useAuthoredGeographyProfile), HexCoordinate.ConventionVersion,
                HexCoordinate.CanonicalOrder, out selectedDefinitions, out _);
            return CreateUnityFingerprintLegacy(config, effectiveConfiguration, resolvedCalendar, out canonicalRecords);
        }
        if (!DailyV1DefinitionAdapter.TryCreate(config, effectiveConfiguration, resolvedCalendar,
            ResolveStageOrder(config.useAuthoredGeographyProfile), HexCoordinate.ConventionVersion,
            HexCoordinate.CanonicalOrder, out selectedDefinitions, out string diagnostic))
            throw new InvalidOperationException(diagnostic ?? "The selected Daily-v1 definition graph is invalid.");
        return DailyV1DefinitionIdentity.CreateP9Fingerprint(selectedDefinitions, out canonicalRecords);


    }

    public static DailyV1EffectiveDefinitions CreateSelectedDefinitions(SimulationConfigData config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        EffectiveSimulationConfiguration effective = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: config.CreateConfigurationOverrides());
        CalendarDefinition calendar = config.Calendar ?? CalendarDefinition.CreateDefault();
        return CreateSelectedDefinitions(config, effective, calendar);
    }

    public static DailyV1EffectiveDefinitions CreateSelectedDefinitions(
        SimulationConfigData config,
        EffectiveSimulationConfiguration effective,
        CalendarDefinition calendar)
    {
        return CreateSelectedDefinitions(config, effective, calendar, out _);
    }

    internal static DailyV1EffectiveDefinitions CreateSelectedDefinitions(
        SimulationConfigData config,
        EffectiveSimulationConfiguration effective,
        CalendarDefinition calendar,
        out DailyV1DefinitionAdapter.SelectedBindings selectedBindings)
    {
        selectedBindings = null;
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (effective == null) throw new ArgumentNullException(nameof(effective));
        if (calendar == null) throw new ArgumentNullException(nameof(calendar));
        if (!calendar.TryValidate(out string calendarDiagnostic))
            throw new InvalidOperationException("Authored CalendarDefinition is invalid: " + calendarDiagnostic);
        if (!DailyV1DefinitionAdapter.TryCreate(config, effective, calendar,
            ResolveStageOrder(config.useAuthoredGeographyProfile), HexCoordinate.ConventionVersion,
            HexCoordinate.CanonicalOrder, out DailyV1EffectiveDefinitions definitions,
            out selectedBindings, out string diagnostic))
            throw new InvalidOperationException(diagnostic ?? "The selected typed definition graph is invalid.");
        return definitions;
    }

    internal static bool TryValidateSelectedDefinitionBindings(
        DailyV1EffectiveDefinitions definitions,
        out string diagnostic)
    {
        diagnostic = "A selected Daily-v1 definition graph is required.";
        if (definitions == null) return false;

        DailyV1SimulationConfigData config = definitions.Configuration;
        foreach (string id in config.cities ?? System.Array.Empty<string>())
            if (!IsBound(definitions.Cities, id)) return Unbound("configuration.cities", id, "City", out diagnostic);
        foreach (string id in config.actions ?? System.Array.Empty<string>())
            if (!IsBound(definitions.Actions, id)) return Unbound("configuration.actions", id, "NpcAction", out diagnostic);
        foreach (string id in config.statuses ?? System.Array.Empty<string>())
            if (!IsBound(definitions.Statuses, id)) return Unbound("configuration.statuses", id, "NpcStatus", out diagnostic);
        foreach (string id in config.jobs ?? System.Array.Empty<string>())
            if (!IsBound(definitions.Jobs, id)) return Unbound("configuration.jobs", id, "NpcJob", out diagnostic);
        foreach (DailyV1ExplorableSiteConfig row in config.explorableSites ?? System.Array.Empty<DailyV1ExplorableSiteConfig>())
        {
            if (row == null) continue;
            if (!IsBound(definitions.ExplorableSites, row.site)) return Unbound("configuration.explorableSites.site", row.site, "ExplorableSite", out diagnostic);
            if (!IsBound(definitions.Cities, row.anchorCity)) return Unbound("configuration.explorableSites.anchorCity", row.anchorCity, "City", out diagnostic);
        }
        if (!IsBound(definitions.ExplorableSites, config.authoredP10RuinSite))
            return Unbound("configuration.authoredP10RuinSite", config.authoredP10RuinSite, "ExplorableSite", out diagnostic);

        foreach (DailyV1CityDefinition definition in definitions.Cities.Values)
        {
            DailyV1CityData city = definition.Values;
            foreach (DailyV1MarketItemConfig row in city.marketItems ?? System.Array.Empty<DailyV1MarketItemConfig>())
                if (row != null && !IsBound(definitions.Items, row.item)) return Unbound("CityData.marketItems.item", row.item, "Item", out diagnostic);
            foreach (DailyV1CityProductionConfig row in city.productionConfigs ?? System.Array.Empty<DailyV1CityProductionConfig>())
                if (row != null && !IsBound(definitions.Items, row.item)) return Unbound("CityData.productionConfigs.item", row.item, "Item", out diagnostic);
            foreach (DailyV1CityConnection row in city.connections ?? System.Array.Empty<DailyV1CityConnection>())
                if (row != null && !IsBound(definitions.Cities, row.destination)) return Unbound("CityData.connections.destination", row.destination, "City", out diagnostic);
        }

        foreach (DailyV1NpcDefinition definition in definitions.Npcs.Values)
        {
            DailyV1NpcData npc = definition.Values;
            if (!IsBound(definitions.Jobs, npc.job)) return Unbound("NpcData.job", npc.job, "NpcJob", out diagnostic);
            foreach (DailyV1NPCDefaultAction row in npc.acoesPadrao ?? System.Array.Empty<DailyV1NPCDefaultAction>())
                if (row != null && !IsBound(definitions.Actions, row.action)) return Unbound("NpcData.acoesPadrao.action", row.action, "NpcAction", out diagnostic);
            foreach (string id in npc.statusPadrao ?? System.Array.Empty<string>())
                if (!IsBound(definitions.Statuses, id)) return Unbound("NpcData.statusPadrao", id, "NpcStatus", out diagnostic);
            foreach (DailyV1CapabilityAttributeValue row in npc.capabilityValues ?? System.Array.Empty<DailyV1CapabilityAttributeValue>())
                if (row != null && !IsBound(definitions.CapabilityAttributes, row.attribute)) return Unbound("NpcData.capabilityValues.attribute", row.attribute, "CapabilityAttribute", out diagnostic);
            foreach (string id in npc.traits ?? System.Array.Empty<string>())
                if (!IsBound(definitions.Traits, id)) return Unbound("NpcData.traits", id, "Trait", out diagnostic);
        }

        foreach (DailyV1ItemDefinition definition in definitions.Items.Values)
            foreach (DailyV1CapabilityAttributeModifier row in definition.Values.capabilityModifiers ?? System.Array.Empty<DailyV1CapabilityAttributeModifier>())
                if (row != null && !IsBound(definitions.CapabilityAttributes, row.attribute)) return Unbound("ItemData.capabilityModifiers.attribute", row.attribute, "CapabilityAttribute", out diagnostic);

        foreach (DailyV1JobDefinition definition in definitions.Jobs.Values)
        {
            DailyV1NpcJobData job = definition.Values;
            if (!IsBound(definitions.Actions, job.workAction)) return Unbound("NpcJobData.workAction", job.workAction, "NpcAction", out diagnostic);
            foreach (DailyV1TradeItemPreference row in job.preferredTradeItems ?? System.Array.Empty<DailyV1TradeItemPreference>())
                if (row != null && !IsBound(definitions.Items, row.item)) return Unbound("NpcJobData.preferredTradeItems.item", row.item, "Item", out diagnostic);
        }

        foreach (DailyV1ActionDefinition definition in definitions.Actions.Values)
        {
            DailyV1NpcActionData action = definition.Values;
            if (!ValidateStatusReferences(definitions, action.statusNecessariosParaFazerAcao, "NpcActionData.statusNecessariosParaFazerAcao", out diagnostic)
                || !ValidateStatusReferences(definitions, action.statusToAdd, "NpcActionData.statusToAdd", out diagnostic)
                || !ValidateStatusReferences(definitions, action.statusToRemove, "NpcActionData.statusToRemove", out diagnostic)
                || !ValidateStatusReferences(definitions, action.targetStatusToAdd, "NpcActionData.targetStatusToAdd", out diagnostic)
                || !ValidateStatusReferences(definitions, action.targetStatusToRemove, "NpcActionData.targetStatusToRemove", out diagnostic))
                return false;
            foreach (DailyV1StatusWeightModifier row in action.statusModifiers ?? System.Array.Empty<DailyV1StatusWeightModifier>())
                if (row != null && !IsBound(definitions.Statuses, row.status)) return Unbound("NpcActionData.statusModifiers.status", row.status, "NpcStatus", out diagnostic);
        }

        foreach (DailyV1NpcSimulationConfig row in config.npcs ?? System.Array.Empty<DailyV1NpcSimulationConfig>())
        {
            if (row == null) continue;
            if (!IsBound(definitions.Npcs, row.npc)) return Unbound("configuration.npcs", row.npc, "Npc", out diagnostic);
            if (!IsBound(definitions.Cities, row.startingCity)) return Unbound("NpcSimulationConfig.startingCity", row.startingCity, "City", out diagnostic);
            foreach (DailyV1NpcInitialInventoryItemConfig item in row.initialInventory ?? System.Array.Empty<DailyV1NpcInitialInventoryItemConfig>())
                if (item != null && !IsBound(definitions.Items, item.item)) return Unbound("NpcSimulationConfig.initialInventory.item", item.item, "Item", out diagnostic);
            foreach (string id in row.initialKnownExplorableSites ?? System.Array.Empty<string>())
                if (!IsBound(definitions.ExplorableSites, id)) return Unbound("NpcSimulationConfig.initialKnownExplorableSites", id, "ExplorableSite", out diagnostic);
        }

        foreach (DailyV1InitialWantedRecordConfig row in config.initialWarrants ?? System.Array.Empty<DailyV1InitialWantedRecordConfig>())
        {
            if (row == null) continue;
            if (!IsBound(definitions.Npcs, row.target)) return Unbound("InitialWantedRecordConfig.target", row.target, "Npc", out diagnostic);
            if (!IsBound(definitions.Cities, row.city)) return Unbound("InitialWantedRecordConfig.city", row.city, "City", out diagnostic);
        }
        foreach (DailyV1ScheduledDirectiveConfig row in config.scheduledDirectives ?? System.Array.Empty<DailyV1ScheduledDirectiveConfig>())
        {
            if (row == null) continue;
            if (!IsBound(definitions.Npcs, row.actor)) return Unbound("ScheduledDirectiveConfig.actor", row.actor, "Npc", out diagnostic);
            if (!IsBound(definitions.Actions, row.action)) return Unbound("ScheduledDirectiveConfig.action", row.action, "NpcAction", out diagnostic);
        }

        if (!IsBound(definitions.Statuses, config.freeStatus)) return Unbound("SimulationConfigData.freeStatus", config.freeStatus, "NpcStatus", out diagnostic);
        if (!IsBound(definitions.Statuses, config.wantedStatus)) return Unbound("SimulationConfigData.wantedStatus", config.wantedStatus, "NpcStatus", out diagnostic);
        if (!IsBound(definitions.Statuses, config.arrestedStatus)) return Unbound("SimulationConfigData.arrestedStatus", config.arrestedStatus, "NpcStatus", out diagnostic);
        if (!IsBound(definitions.Statuses, config.hiddenStatus)) return Unbound("SimulationConfigData.hiddenStatus", config.hiddenStatus, "NpcStatus", out diagnostic);

        diagnostic = null;
        return true;
    }

    private static bool ValidateStatusReferences(
        DailyV1EffectiveDefinitions definitions,
        System.Collections.Generic.IReadOnlyList<string> ids,
        string source,
        out string diagnostic)
    {
        foreach (string id in ids ?? System.Array.Empty<string>())
            if (!IsBound(definitions.Statuses, id)) return Unbound(source, id, "NpcStatus", out diagnostic);
        diagnostic = null;
        return true;
    }

    private static bool IsBound<T>(System.Collections.Generic.IReadOnlyDictionary<string, T> family, string id)
        where T : class => string.IsNullOrWhiteSpace(id) || family.ContainsKey(id);

    private static bool Unbound(string source, string id, string family, out string diagnostic)
    {
        diagnostic = "Selected reference '" + source + "' does not resolve to a " + family + " definition: '" + id + "'.";
        return false;
    }

    private static void Add(List<string> fields, string kind, params object[] values)
    {
        fields.Add(DailyV1DefinitionIdentity.CreateRecord(kind, values));
    }

    private static string Pack(params object[] values)
    {
        return DailyV1DefinitionIdentity.Pack(values);
    }

    public static void ValidateProfile(SimulationConfigData config)
    {
        if (config == null) throw new InvalidOperationException("Authored bootstrap requires SimulationConfigData.");
        string selectedContractIdentity = config.GenesisProfileContractIdentity;
        if (!string.Equals(selectedContractIdentity, ProfileContractIdentity, StringComparison.Ordinal)
            && !string.Equals(selectedContractIdentity, GeographyProfileContractIdentity, StringComparison.Ordinal)
            && !string.Equals(selectedContractIdentity, P10RuinProfileContractIdentity, StringComparison.Ordinal)
            && !string.Equals(selectedContractIdentity, P10BGeneratedRuinProfileContractIdentity, StringComparison.Ordinal))
            throw new InvalidOperationException("The selected genesis profile contract identity is not supported.");
        if (config.useAuthoredGeographyProfile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(config.authoredHexId)
                    || string.IsNullOrWhiteSpace(config.authoredTerrainDefinitionId)
                    || string.IsNullOrWhiteSpace(config.authoredTerrainRevisionToken)
                    || string.IsNullOrWhiteSpace(config.authoredLocationId)
                    || string.IsNullOrWhiteSpace(config.authoredScaleConventionId)
                    || string.IsNullOrWhiteSpace(config.authoredScaleSourceIdentity)
                    || string.IsNullOrWhiteSpace(config.authoredScaleSourceVersion)
                    || string.IsNullOrWhiteSpace(config.authoredScaleUnit))
                    throw new InvalidOperationException("Authored geography requires stable Hex, terrain, Location, and scale provenance identities.");
                decimal scale = decimal.Parse(config.authoredDistancePerNeighborStep, NumberStyles.Number, CultureInfo.InvariantCulture);
                if (scale <= 0m) throw new InvalidOperationException("Authored geography scale must be positive.");
            }
            catch (Exception exception) when (exception is ArgumentException || exception is FormatException || exception is OverflowException)
            {
                throw new InvalidOperationException("Authored geography scale value or required identity is invalid.", exception);
            }
        }
        var cities = new HashSet<CityData>();
        var cityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (CityData city in config.Cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.DefinitionId) || !cities.Add(city)
                || !cityIds.Add(city.DefinitionId)) throw new InvalidOperationException("City rows must be present and have unique stable DefinitionIds.");
        }
        var routeKeys = new HashSet<RouteKey>();
        foreach (CityData city in config.Cities)
        {
            if (city.connections == null || city.marketItems == null || city.productionConfigs == null || city.initialPopulation < 0)
                throw new InvalidOperationException("Selected city collections and population must be valid.");
            foreach (CityConnection connection in city.connections)
            {
                if (connection == null || connection.destination == null || !cities.Contains(connection.destination)
                    || connection.destination == city || connection.travelDays < 1)
                    throw new InvalidOperationException("Authored city connections must resolve to one distinct selected city.");
                var key = new RouteKey(city.DefinitionId, connection.destination.DefinitionId, connection.travelDays);
                if (!routeKeys.Add(key)) throw new InvalidOperationException("Duplicate authored city route key.");
            }
        }
        var npcIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcSimulationConfig row in config.Npcs)
        {
            if (row == null || row.npc == null || string.IsNullOrWhiteSpace(row.npc.DefinitionId)
                || !npcIds.Add(row.npc.DefinitionId) || !cities.Contains(row.startingCity))
                throw new InvalidOperationException("NPC rows require a unique definition and selected starting city.");
            foreach (NpcInitialInventoryItemConfig item in row.InitialInventory)
                if (item == null || item.item == null || item.amount < 0 || item.averageUnitCost < 0)
                    throw new InvalidOperationException("Selected initial inventory rows must resolve and have non-negative values.");
        }
        var siteIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ExplorableSiteConfig row in config.ExplorableSites)
            if (row == null || row.site == null || string.IsNullOrWhiteSpace(row.site.DefinitionId)
                || !siteIds.Add(row.site.DefinitionId) || !cities.Contains(row.anchorCity) || row.travelDaysFromAnchor < 1)
                throw new InvalidOperationException("Explorable-site rows require a unique definition, selected anchor, and valid travel duration.");
        bool p10b = IsP10BGeneratedRuinEnabled(config);
        bool p10a = P10RuinLocalTopologyGenesis.IsEnabled(config) && !p10b;
        if (string.Equals(selectedContractIdentity, P10RuinProfileContractIdentity, StringComparison.Ordinal) != p10a)
            throw new InvalidOperationException("The selected P10-A profile must match its authored Ruin definition.");
        if (p10b && !P10RuinLocalTopologyGenesis.IsEnabled(config))
            throw new InvalidOperationException("P10-B requires one selected authored Ruin definition.");
        if ((p10a || p10b) && !config.useAuthoredGeographyProfile)
            throw new InvalidOperationException("P10 Ruin topology requires the selected P9-B authored geography profile.");
        if (!p10a && !p10b
            && config.useAuthoredGeographyProfile != string.Equals(selectedContractIdentity, GeographyProfileContractIdentity, StringComparison.Ordinal))
            throw new InvalidOperationException("The selected P9 profile contract identity must match its authored-geography inputs.");
        if (p10b && string.IsNullOrWhiteSpace(config.P10BStableSiteKey))
            throw new InvalidOperationException("P10-B requires an explicit non-empty StableSiteKey.");
        if (config.authoredP10RuinSite != null)
        {
            if (string.IsNullOrWhiteSpace(config.authoredP10RuinSite.DefinitionId)
                || config.authoredP10RuinSite.kind != ExplorableSiteKind.Ruin
                || siteIds.Contains(config.authoredP10RuinSite.DefinitionId))
                throw new InvalidOperationException("The P10 profile requires one unique authored ExplorableSiteKind.Ruin definition.");
            foreach (ExplorableSiteConfig row in config.ExplorableSites)
                if (row?.site != null && row.site.kind == ExplorableSiteKind.Ruin)
                    throw new InvalidOperationException("The P10 profile may compose exactly one Ruin; authored P9 site/route rows cannot add another.");
        }
        foreach (InitialWantedRecordConfig row in config.InitialWarrants)
            if (row == null || row.target == null || !npcIds.Contains(row.target.DefinitionId)
                || row.city == null || !cities.Contains(row.city) || row.bounty < 0f || row.sentenceDays < 1)
                throw new InvalidOperationException("Warrant rows must resolve their selected NPC and city owners and valid values.");
        foreach (ScheduledDirectiveConfig row in config.ScheduledDirectives)
            if (row == null || row.actor == null || !npcIds.Contains(row.actor.DefinitionId)
                || row.action == null || !config.Actions.Contains(row.action) || row.action.actionType != NpcActionType.EscapePrison
                || row.absoluteDay < 1) throw new InvalidOperationException("Directive rows must resolve their selected actor and supported action.");
        var statusIds = new HashSet<string>(StringComparer.Ordinal);
        var statuses = new List<NpcStatusData>(config.Statuses);
        statuses.Add(config.freeStatus);
        statuses.Add(config.wantedStatus);
        statuses.Add(config.arrestedStatus);
        statuses.Add(config.hiddenStatus);
        foreach (NpcSimulationConfig npc in config.Npcs)
            if (npc.npc.statusPadrao != null) statuses.AddRange(npc.npc.statusPadrao);
        foreach (NpcActionData action in config.Actions)
        {
            if (action == null) continue;
            if (action.statusNecessariosParaFazerAcao != null) statuses.AddRange(action.statusNecessariosParaFazerAcao);
            if (action.statusToAdd != null) statuses.AddRange(action.statusToAdd);
            if (action.statusToRemove != null) statuses.AddRange(action.statusToRemove);
            if (action.targetStatusToAdd != null) statuses.AddRange(action.targetStatusToAdd);
            if (action.targetStatusToRemove != null) statuses.AddRange(action.targetStatusToRemove);
            if (action.statusModifiers != null)
                foreach (StatusWeightModifier weight in action.statusModifiers)
                    if (weight != null) statuses.Add(weight.status);
        }
        var uniqueStatuses = new HashSet<NpcStatusData>();
        foreach (NpcStatusData status in statuses)
            if (status != null && uniqueStatuses.Add(status) == false) continue;
        if (config.Statuses.Contains(null)) throw new InvalidOperationException("Selected status definitions cannot contain null rows.");
        foreach (NpcStatusData status in uniqueStatuses)
            if (status != null && (string.IsNullOrWhiteSpace(status.DefinitionId) || !statusIds.Add(status.DefinitionId)))
                throw new InvalidOperationException("Selected status definitions require unique stable DefinitionIds.");
        var jobs = new HashSet<NpcJobData>(config.Jobs);
        if (config.Jobs.Contains(null)) throw new InvalidOperationException("Selected job definitions cannot contain null rows.");
        foreach (NpcSimulationConfig npc in config.Npcs)
            if (npc.npc.job != null) jobs.Add(npc.npc.job);
        var jobIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcJobData job in jobs)
            if (string.IsNullOrWhiteSpace(job.DefinitionId) || !jobIds.Add(job.DefinitionId))
                throw new InvalidOperationException("Selected job definitions require unique stable DefinitionIds.");
            else
            {
                if (job.PreferredTradeItems == null) throw new InvalidOperationException("Selected job preference collections must be present.");
                foreach (TradeItemPreference preference in job.PreferredTradeItems)
                    if (preference == null || preference.item == null || string.IsNullOrWhiteSpace(preference.item.DefinitionId))
                        throw new InvalidOperationException("Selected job preferences must resolve item definitions.");
            }
        var selectedItems = new HashSet<ItemData>();
        foreach (CityData city in config.Cities)
        {
            foreach (MarketItemConfig market in city.marketItems) selectedItems.Add(market.item);
            foreach (CityProductionConfig production in city.productionConfigs) selectedItems.Add(production.item);
        }
        foreach (NpcSimulationConfig npc in config.Npcs)
        {
            foreach (NpcInitialInventoryItemConfig inventory in npc.InitialInventory) selectedItems.Add(inventory.item);
            if (npc.npc.job != null)
                foreach (TradeItemPreference preference in npc.npc.job.PreferredTradeItems) selectedItems.Add(preference.item);
        }
        foreach (NpcJobData job in config.Jobs)
            if (job != null) foreach (TradeItemPreference preference in job.PreferredTradeItems) selectedItems.Add(preference.item);
        var itemIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ItemData item in selectedItems)
            if (item == null || string.IsNullOrWhiteSpace(item.DefinitionId) || !itemIds.Add(item.DefinitionId))
                throw new InvalidOperationException("Selected item definitions require unique stable DefinitionIds.");
        var selectedTraits = new HashSet<TraitData>();
        var selectedAttributes = new HashSet<CapabilityAttributeData>();
        foreach (NpcSimulationConfig npc in config.Npcs)
        {
            foreach (TraitData trait in npc.npc.traits) selectedTraits.Add(trait);
            foreach (CapabilityAttributeValue value in npc.npc.capabilityValues) selectedAttributes.Add(value.Attribute);
        }
        foreach (ItemData item in selectedItems)
            foreach (CapabilityAttributeModifier modifier in item.CapabilityModifiers)
                if (modifier?.attribute != null) selectedAttributes.Add(modifier.attribute);
        var traitIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (TraitData trait in selectedTraits)
            if (trait == null || string.IsNullOrWhiteSpace(trait.DefinitionId) || !traitIds.Add(trait.DefinitionId))
                throw new InvalidOperationException("Selected trait definitions require unique stable DefinitionIds.");
        var attributeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (CapabilityAttributeData attribute in selectedAttributes)
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.DefinitionId) || !attributeIds.Add(attribute.DefinitionId))
                throw new InvalidOperationException("Selected capability attributes require unique stable DefinitionIds.");
        foreach (CityData city in config.Cities)
        {
            var marketKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (MarketItemConfig market in city.marketItems ?? new List<MarketItemConfig>())
                if (market == null || market.item == null || string.IsNullOrWhiteSpace(market.item.DefinitionId)
                    || !marketKeys.Add(market.item.DefinitionId)) throw new InvalidOperationException("City market rows require unique item definitions.");
            foreach (CityProductionConfig input in city.productionConfigs ?? new List<CityProductionConfig>())
                if (input == null || input.item == null || string.IsNullOrWhiteSpace(input.item.DefinitionId))
                    throw new InvalidOperationException("City production rows require item definitions.");
        }
        var actionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcActionData action in config.Actions)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.DefinitionId) || !actionIds.Add(action.DefinitionId))
                throw new InvalidOperationException("Selected action definitions require unique stable DefinitionIds.");
            if (action.statusNecessariosParaFazerAcao == null || action.statusToAdd == null || action.statusToRemove == null
                || action.targetStatusToAdd == null || action.targetStatusToRemove == null || action.statusModifiers == null)
                throw new InvalidOperationException("Selected action status collections must be present.");
            foreach (NpcStatusData status in action.statusNecessariosParaFazerAcao)
                if (status == null || string.IsNullOrWhiteSpace(status.DefinitionId)) throw new InvalidOperationException("Action requirements must resolve stable status definitions.");
            foreach (NpcStatusData status in action.statusToAdd)
                if (status == null || string.IsNullOrWhiteSpace(status.DefinitionId)) throw new InvalidOperationException("Action outputs must resolve stable status definitions.");
            foreach (NpcStatusData status in action.statusToRemove)
                if (status == null || string.IsNullOrWhiteSpace(status.DefinitionId)) throw new InvalidOperationException("Action outputs must resolve stable status definitions.");
            foreach (NpcStatusData status in action.targetStatusToAdd)
                if (status == null || string.IsNullOrWhiteSpace(status.DefinitionId)) throw new InvalidOperationException("Action target outputs must resolve stable status definitions.");
            foreach (NpcStatusData status in action.targetStatusToRemove)
                if (status == null || string.IsNullOrWhiteSpace(status.DefinitionId)) throw new InvalidOperationException("Action target outputs must resolve stable status definitions.");
            foreach (StatusWeightModifier weight in action.statusModifiers)
                if (weight == null || weight.status == null || string.IsNullOrWhiteSpace(weight.status.DefinitionId))
                    throw new InvalidOperationException("Action weights must resolve stable status definitions.");
        }
        foreach (NpcJobData job in jobs)
            if (job.workAction != null && !config.Actions.Contains(job.workAction))
                throw new InvalidOperationException("Selected job work actions must resolve to the exact selected action definition.");
        foreach (NpcSimulationConfig npc in config.Npcs)
        {
            if (npc.npc.acoesPadrao == null || npc.npc.statusPadrao == null || npc.npc.traits == null || npc.npc.capabilityValues == null)
                throw new InvalidOperationException("NPC authored action/status collections must be present.");
            foreach (NPCDefaultAction action in npc.npc.acoesPadrao)
                if (action == null || action.action == null || !config.Actions.Contains(action.action))
                    throw new InvalidOperationException("NPC default actions must resolve to selected action definitions.");
            foreach (NpcStatusData status in npc.npc.statusPadrao)
                if (status == null || string.IsNullOrWhiteSpace(status.DefinitionId))
                    throw new InvalidOperationException("NPC default statuses require stable definitions.");
            foreach (TraitData trait in npc.npc.traits)
                if (trait == null || string.IsNullOrWhiteSpace(trait.DefinitionId)) throw new InvalidOperationException("NPC traits require stable definitions.");
            foreach (CapabilityAttributeValue value in npc.npc.capabilityValues)
                if (value == null || value.Attribute == null || string.IsNullOrWhiteSpace(value.Attribute.DefinitionId))
                    throw new InvalidOperationException("NPC capability values require stable attribute definitions.");
            if (!CapabilityAuthoringValidator.ValidateNpc(npc.npc, out string npcDiagnostic))
                throw new InvalidOperationException("Selected NPC capability authoring is invalid: " + npcDiagnostic);
            foreach (ExplorableSiteData knownSite in npc.InitialKnownExplorableSites)
                if (knownSite == null || !siteIds.Contains(knownSite.DefinitionId))
                    throw new InvalidOperationException("NPC initial Knowledge must resolve to a selected explorable site.");
        }
        foreach (ItemData item in selectedItems)
            if (!CapabilityAuthoringValidator.ValidateItem(item, out string itemDiagnostic))
                throw new InvalidOperationException("Selected item capability authoring is invalid: " + itemDiagnostic);
    }

    private struct RouteKey : IEquatable<RouteKey>
    {
        private readonly string origin;
        private readonly string destination;
        private readonly int travelDays;

        public RouteKey(string origin, string destination, int travelDays)
        {
            this.origin = origin;
            this.destination = destination;
            this.travelDays = travelDays;
        }

        public bool Equals(RouteKey other)
        {
            return StringComparer.Ordinal.Equals(origin, other.origin)
                && StringComparer.Ordinal.Equals(destination, other.destination)
                && travelDays == other.travelDays;
        }

        public override bool Equals(object obj) => obj is RouteKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(origin ?? "");
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(destination ?? "");
                return (hash * 397) ^ travelDays;
            }
        }
    }

    private static void AddDefinitions<T>(List<string> fields, string kind, IEnumerable<T> rows, Func<T, string> key)
    {
        var values = new List<string>();
        foreach (T row in rows) values.Add(key(row) ?? "");
        values.Sort(StringComparer.Ordinal);
        foreach (string value in values) Add(fields, kind, value);
    }

    private static void AddOrdered<T>(List<string> fields, string kind, IEnumerable<T> rows, Func<T, string> value)
    {
        int index = 0;
        foreach (T row in rows) Add(fields, kind, index++, value(row));
    }
}
public sealed class SimulationGenesisManifest
{
    internal SimulationGenesisManifest(
        SimulationConfigData config,
        EffectiveSimulationConfiguration effectiveConfiguration,
        CalendarDefinition calendar,
        string selectedP9Fingerprint,
        string fingerprint,
        IReadOnlyList<string> canonicalRecords,
        bool includeP10RuinLocalTopology,
        bool includeP10BGeneratedRuin = false,
        DailyV1EffectiveDefinitions selectedDefinitions = null)
    {
        DailyV1SimulationConfigData selectedConfiguration = selectedDefinitions?.Configuration;
        if (selectedDefinitions != null && selectedConfiguration == null)
            throw new ArgumentException("The selected Daily-v1 manifest requires its captured configuration.", nameof(selectedDefinitions));
        if (selectedDefinitions != null && (includeP10RuinLocalTopology || includeP10BGeneratedRuin))
            throw new ArgumentException("P10 compositions require their own selected profile manifest, not the Daily-v1 definition graph.", nameof(selectedDefinitions));
        bool useAuthoredGeographyProfile = selectedConfiguration != null
            ? selectedConfiguration.useAuthoredGeographyProfile
            : config.useAuthoredGeographyProfile;
        SchemaVersion = includeP10BGeneratedRuin ? 1 : includeP10RuinLocalTopology
            ? P10RuinLocalTopologyGenesis.ContractSchemaVersion
            : useAuthoredGeographyProfile ? 2 : 1;
        Fingerprint = fingerprint;
        SelectedP9ProfileFingerprint = selectedP9Fingerprint;
        SelectedP9ContractIdentity = useAuthoredGeographyProfile
            ? selectedDefinitions?.GeographyProfileContractIdentity ?? SimulationGenesisPipeline.GeographyProfileContractIdentity
            : selectedDefinitions?.ProfileContractIdentity ?? SimulationGenesisPipeline.ProfileContractIdentity;
        SelectedP9SchemaVersion = useAuthoredGeographyProfile ? 2 : 1;
        EffectiveConfiguration = selectedDefinitions?.EffectiveConfiguration ?? effectiveConfiguration;
        bool useFixedSeed = selectedConfiguration != null
            ? selectedConfiguration.useFixedSimulationSeed
            : config.useFixedSimulationSeed;
        Seed = useFixedSeed
            ? selectedConfiguration != null ? selectedConfiguration.simulationSeed : config.simulationSeed
            : 0;
        SeedSource = useFixedSeed ? "authored-fixed" : "default-zero";
        ContractIdentity = includeP10BGeneratedRuin ? SimulationGenesisPipeline.P10BGeneratedRuinProfileContractIdentity
            : includeP10RuinLocalTopology ? P10RuinLocalTopologyGenesis.ContractIdentity
            : SelectedP9ContractIdentity;
        StageOrder = selectedDefinitions?.StageOrder
            ?? SimulationGenesisPipeline.ResolveStageOrder(useAuthoredGeographyProfile, includeP10RuinLocalTopology, includeP10BGeneratedRuin);
        CanonicalProvenanceRecords = canonicalRecords;
        DailyV1CalendarDefinition selectedCalendar = selectedDefinitions?.Calendar;
        CalendarMonthsPerYear = selectedCalendar != null ? selectedCalendar.monthsPerYear : calendar.MonthsPerYear;
        CalendarWeeksPerMonth = selectedCalendar != null ? selectedCalendar.weeksPerMonth : calendar.WeeksPerMonth;
        CalendarDaysPerWeek = selectedCalendar != null ? selectedCalendar.daysPerWeek : calendar.DaysPerWeek;
        MonthLengths = selectedCalendar != null
            ? Array.AsReadOnly(new List<int>(selectedCalendar.monthLengths ?? Array.Empty<int>()).ToArray())
            : Array.AsReadOnly(new List<int>(calendar.monthLengths ?? new List<int>()).ToArray());
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (selectedDefinitions != null)
        {
            AddSelectedDailyV1DefinitionIds(ids, selectedDefinitions,
                includeP10RuinLocalTopology || includeP10BGeneratedRuin);
        }
        else
        {
            foreach (CityData city in config.Cities)
            {
                ids.Add("city/" + city.DefinitionId);
                foreach (MarketItemConfig market in city.marketItems) AddId(ids, "item", market.item);
                foreach (CityProductionConfig production in city.productionConfigs) AddId(ids, "item", production.item);
            }
            foreach (ExplorableSiteConfig site in config.ExplorableSites) ids.Add("site/" + site.site.DefinitionId);
            if (includeP10RuinLocalTopology || includeP10BGeneratedRuin)
                ids.Add("site/" + config.authoredP10RuinSite.DefinitionId);
            if (useAuthoredGeographyProfile)
            {
                ids.Add("hex/" + config.authoredHexId);
                ids.Add("location/" + config.authoredLocationId);
                ids.Add("terrain/" + config.authoredTerrainDefinitionId);
            }
            foreach (NpcSimulationConfig npc in config.Npcs)
            {
                ids.Add("npc/" + npc.npc.DefinitionId);
                AddId(ids, "job", npc.npc.job);
                foreach (NPCDefaultAction action in npc.npc.acoesPadrao) AddId(ids, "action", action.action);
                foreach (NpcStatusData status in npc.npc.statusPadrao) AddId(ids, "status", status);
                foreach (TraitData trait in npc.npc.traits) if (trait != null) ids.Add("trait/" + trait.DefinitionId);
                foreach (CapabilityAttributeValue value in npc.npc.capabilityValues)
                    if (value?.Attribute != null) ids.Add("capability-attribute/" + value.Attribute.DefinitionId);
                foreach (NpcInitialInventoryItemConfig item in npc.InitialInventory) AddId(ids, "item", item.item);
            }
            foreach (NpcActionData action in config.Actions)
            {
                ids.Add("action/" + action.DefinitionId);
                AddStatusIds(ids, action.statusNecessariosParaFazerAcao);
                AddStatusIds(ids, action.statusToAdd);
                AddStatusIds(ids, action.statusToRemove);
                AddStatusIds(ids, action.targetStatusToAdd);
                AddStatusIds(ids, action.targetStatusToRemove);
                foreach (StatusWeightModifier weight in action.statusModifiers) AddId(ids, "status", weight?.status);
            }
            foreach (NpcStatusData status in config.Statuses) AddId(ids, "status", status);
            AddId(ids, "status", config.freeStatus); AddId(ids, "status", config.wantedStatus);
            AddId(ids, "status", config.arrestedStatus); AddId(ids, "status", config.hiddenStatus);
            foreach (NpcJobData job in config.Jobs) AddJobIds(ids, job);
            foreach (NpcSimulationConfig npc in config.Npcs) AddJobIds(ids, npc.npc.job);
        }

        var orderedIds = new List<string>(ids);
        orderedIds.Sort(StringComparer.Ordinal);
        AuthoredDefinitionIds = orderedIds.AsReadOnly();
        var outputOwners = new List<string>
        {
            "CityRuntime", "MarketCounterpartyRuntime", "PopulationEconomyRuntime", "CityProductionInputs",
            "SpatialNetworkRuntime", "ExplorableSiteStore", "NpcRuntime", "InventoryRuntime", "InitialKnowledge",
            "JusticeSystem", "ScheduledDirectiveStore", "SpatialAuthorityStore", "SimulationRuntime"
        };
        if (includeP10RuinLocalTopology || includeP10BGeneratedRuin)
        {
            outputOwners.Add("LocalTopologyStore");
            outputOwners.Add("LegacySpatialAnchorBindingStore");
        }
        OutputOwners = Array.AsReadOnly(outputOwners.ToArray());
        var dependencies = new List<string>
        {
            "resolve-profile -> authored-world", "resolve-profile -> authored-actors",
            "authored-world -> authored-actors", "authored-world -> validate-profile",
            "authored-actors -> validate-profile", "validate-profile -> publish"
        };
        if (useAuthoredGeographyProfile)
        {
            dependencies.Add("resolve-profile -> authored-geography");
            dependencies.Add("authored-world -> authored-geography");
            dependencies.Add("authored-geography -> authored-actors");
            dependencies.Add("authored-geography -> validate-profile");
        }
        if (includeP10RuinLocalTopology)
        {
            dependencies.Add("authored-world -> " + P10RuinLocalTopologyGenesis.StageId);
            dependencies.Add(SimulationGenesisPipeline.GeographyStageId + " -> " + P10RuinLocalTopologyGenesis.StageId);
            dependencies.Add(P10RuinLocalTopologyGenesis.StageId + " -> p9.genesis.validate-profile/v1");
        }
        if (includeP10BGeneratedRuin)
        {
            dependencies.Add("p9.genesis.authored-world/v1 -> " + SimulationGenesisPipeline.P10BGeneratedRuinStageId);
            dependencies.Add(SimulationGenesisPipeline.GeographyStageId + " -> " + SimulationGenesisPipeline.P10BGeneratedRuinStageId);
            dependencies.Add(SimulationGenesisPipeline.P10BGeneratedRuinStageId + " -> p9.genesis.authored-actors/v1");
            dependencies.Add(SimulationGenesisPipeline.P10BGeneratedRuinStageId + " -> p9.genesis.validate-profile/v1");
        }
        StageDependencyRecords = Array.AsReadOnly(dependencies.ToArray());
        FirstSimulatedBoundary = "advance-day:1";
    }

    // Direct-value restoration is intentionally separate from the config-derived
    // constructor above. It never resolves Unity assets, computes genesis facts, or
    // invokes the stage pipeline.
    private SimulationGenesisManifest(P12CP9GenesisManifestSnapshot snapshot)
    {
        ContractIdentity = snapshot.ContractIdentity;
        SchemaVersion = snapshot.SchemaVersion;
        Fingerprint = snapshot.Fingerprint;
        SelectedP9ProfileFingerprint = snapshot.SelectedP9ProfileFingerprint;
        SelectedP9ContractIdentity = snapshot.SelectedP9ContractIdentity;
        SelectedP9SchemaVersion = snapshot.SelectedP9SchemaVersion;
        EffectiveConfiguration = P12CP9GenesisManifestSnapshot.CloneConfiguration(snapshot.EffectiveConfiguration);
        CanonicalProvenanceRecords = CopySnapshotList(snapshot.CanonicalProvenanceRecords);
        AuthoredDefinitionIds = CopySnapshotList(snapshot.AuthoredDefinitionIds);
        OutputOwners = CopySnapshotList(snapshot.OutputOwners);
        StageDependencyRecords = CopySnapshotList(snapshot.StageDependencyRecords);
        CalendarMonthsPerYear = snapshot.CalendarMonthsPerYear;
        CalendarWeeksPerMonth = snapshot.CalendarWeeksPerMonth;
        CalendarDaysPerWeek = snapshot.CalendarDaysPerWeek;
        MonthLengths = CopySnapshotList(snapshot.MonthLengths);
        Seed = snapshot.Seed;
        SeedSource = snapshot.SeedSource;
        StageOrder = CopySnapshotList(snapshot.StageOrder);
        FirstSimulatedBoundary = snapshot.FirstSimulatedBoundary;
    }

    internal static bool TryCreateFromSnapshot(
        P12CP9GenesisManifestSnapshot snapshot,
        out SimulationGenesisManifest manifest,
        out string diagnostic)
    {
        manifest = null;
        if (snapshot == null)
        {
            diagnostic = "P9-B continuation snapshot is required.";
            return false;
        }

        if (!snapshot.TryValidate(out diagnostic)) return false;

        // Keep the candidate local until every recorded value has been copied.
        SimulationGenesisManifest candidate = new SimulationGenesisManifest(snapshot);
        manifest = candidate;
        diagnostic = null;
        return true;
    }

    private static IReadOnlyList<T> CopySnapshotList<T>(IReadOnlyList<T> source)
    {
        return source == null ? null : Array.AsReadOnly(new List<T>(source).ToArray());
    }

    private static void AddSelectedDailyV1DefinitionIds(
        HashSet<string> ids,
        DailyV1EffectiveDefinitions definitions,
        bool includeP10Site)
    {
        DailyV1SimulationConfigData configuration = definitions.Configuration;
        foreach (string cityId in configuration.cities ?? Array.Empty<string>())
        {
            AddDefinitionId(ids, "city", cityId);
            if (!definitions.Cities.TryGetValue(cityId, out DailyV1CityDefinition cityNode)) continue;
            foreach (DailyV1MarketItemConfig market in cityNode.Values.marketItems ?? Array.Empty<DailyV1MarketItemConfig>())
                AddDefinitionId(ids, "item", market?.item);
            foreach (DailyV1CityProductionConfig production in cityNode.Values.productionConfigs ?? Array.Empty<DailyV1CityProductionConfig>())
                AddDefinitionId(ids, "item", production?.item);
        }

        foreach (DailyV1ExplorableSiteConfig site in configuration.explorableSites ?? Array.Empty<DailyV1ExplorableSiteConfig>())
            AddDefinitionId(ids, "site", site?.site);
        if (includeP10Site && !string.IsNullOrWhiteSpace(configuration.authoredP10RuinSite))
            AddDefinitionId(ids, "site", configuration.authoredP10RuinSite);
        if (configuration.useAuthoredGeographyProfile)
        {
            AddDefinitionId(ids, "hex", configuration.authoredHexId);
            AddDefinitionId(ids, "location", configuration.authoredLocationId);
            AddDefinitionId(ids, "terrain", configuration.authoredTerrainDefinitionId);
        }

        foreach (DailyV1NpcSimulationConfig npcConfig in configuration.npcs ?? Array.Empty<DailyV1NpcSimulationConfig>())
        {
            if (npcConfig == null) continue;
            AddDefinitionId(ids, "npc", npcConfig.npc);
            if (definitions.Npcs.TryGetValue(npcConfig.npc, out DailyV1NpcDefinition npcNode))
            {
                DailyV1NpcData npc = npcNode.Values;
                AddSelectedDailyV1JobIds(ids, npc.job, definitions);
                foreach (DailyV1NPCDefaultAction action in npc.acoesPadrao ?? Array.Empty<DailyV1NPCDefaultAction>())
                    AddDefinitionId(ids, "action", action?.action);
                foreach (string statusId in npc.statusPadrao ?? Array.Empty<string>()) AddDefinitionId(ids, "status", statusId);
                foreach (string traitId in npc.traits ?? Array.Empty<string>()) AddDefinitionId(ids, "trait", traitId);
                foreach (DailyV1CapabilityAttributeValue value in npc.capabilityValues ?? Array.Empty<DailyV1CapabilityAttributeValue>())
                    AddDefinitionId(ids, "capability-attribute", value?.attribute);
            }
            foreach (DailyV1NpcInitialInventoryItemConfig item in
                npcConfig.initialInventory ?? Array.Empty<DailyV1NpcInitialInventoryItemConfig>())
                AddDefinitionId(ids, "item", item?.item);
        }

        foreach (string actionId in configuration.actions ?? Array.Empty<string>())
        {
            AddDefinitionId(ids, "action", actionId);
            if (!definitions.Actions.TryGetValue(actionId, out DailyV1ActionDefinition actionNode)) continue;
            DailyV1NpcActionData action = actionNode.Values;
            AddSelectedStatusIds(ids, action.statusNecessariosParaFazerAcao);
            AddSelectedStatusIds(ids, action.statusToAdd);
            AddSelectedStatusIds(ids, action.statusToRemove);
            AddSelectedStatusIds(ids, action.targetStatusToAdd);
            AddSelectedStatusIds(ids, action.targetStatusToRemove);
            foreach (DailyV1StatusWeightModifier weight in action.statusModifiers ?? Array.Empty<DailyV1StatusWeightModifier>())
                AddDefinitionId(ids, "status", weight?.status);
        }

        foreach (string statusId in configuration.statuses ?? Array.Empty<string>()) AddDefinitionId(ids, "status", statusId);
        AddDefinitionId(ids, "status", configuration.freeStatus);
        AddDefinitionId(ids, "status", configuration.wantedStatus);
        AddDefinitionId(ids, "status", configuration.arrestedStatus);
        AddDefinitionId(ids, "status", configuration.hiddenStatus);
        foreach (string jobId in configuration.jobs ?? Array.Empty<string>()) AddSelectedDailyV1JobIds(ids, jobId, definitions);
        foreach (DailyV1NpcSimulationConfig npcConfig in configuration.npcs ?? Array.Empty<DailyV1NpcSimulationConfig>())
            if (npcConfig != null && definitions.Npcs.TryGetValue(npcConfig.npc, out DailyV1NpcDefinition npcNode))
                AddSelectedDailyV1JobIds(ids, npcNode.Values.job, definitions);
    }

    private static void AddSelectedDailyV1JobIds(HashSet<string> ids, string jobId, DailyV1EffectiveDefinitions definitions)
    {
        AddDefinitionId(ids, "job", jobId);
        if (string.IsNullOrWhiteSpace(jobId) || !definitions.Jobs.TryGetValue(jobId, out DailyV1JobDefinition jobNode)) return;
        AddDefinitionId(ids, "action", jobNode.Values.workAction);
        foreach (DailyV1TradeItemPreference preference in
            jobNode.Values.preferredTradeItems ?? Array.Empty<DailyV1TradeItemPreference>())
            AddDefinitionId(ids, "item", preference?.item);
    }

    private static void AddSelectedStatusIds(HashSet<string> ids, IReadOnlyList<string> values)
    {
        foreach (string value in values ?? Array.Empty<string>()) AddDefinitionId(ids, "status", value);
    }

    private static void AddDefinitionId(HashSet<string> ids, string kind, string definitionId)
    {
        if (!string.IsNullOrWhiteSpace(definitionId)) ids.Add(kind + "/" + definitionId);
    }

    private static void AddId(HashSet<string> ids, string kind, ItemData value)
    {
        if (value != null) ids.Add(kind + "/" + value.DefinitionId);
    }

    private static void AddId(HashSet<string> ids, string kind, NpcActionData value)
    {
        if (value != null) ids.Add(kind + "/" + value.DefinitionId);
    }

    private static void AddId(HashSet<string> ids, string kind, NpcStatusData value)
    {
        if (value != null) ids.Add(kind + "/" + value.DefinitionId);
    }

    private static void AddId(HashSet<string> ids, string kind, NpcJobData value)
    {
        if (value != null) ids.Add(kind + "/" + value.DefinitionId);
    }

    private static void AddStatusIds(HashSet<string> ids, IEnumerable<NpcStatusData> values)
    {
        foreach (NpcStatusData value in values) AddId(ids, "status", value);
    }

    private static void AddJobIds(HashSet<string> ids, NpcJobData value)
    {
        if (value == null) return;
        AddId(ids, "job", value);
        AddId(ids, "action", value.workAction);
        foreach (TradeItemPreference preference in value.PreferredTradeItems) AddId(ids, "item", preference.item);
    }

    public string ContractIdentity { get; }
    public int SchemaVersion { get; }
    public string Fingerprint { get; }
    public string SelectedP9ProfileFingerprint { get; }
    public string SelectedP9ContractIdentity { get; }
    public int SelectedP9SchemaVersion { get; }
    public EffectiveSimulationConfiguration EffectiveConfiguration { get; }
    public IReadOnlyList<string> CanonicalProvenanceRecords { get; }
    public IReadOnlyList<string> AuthoredDefinitionIds { get; }
    public IReadOnlyList<string> OutputOwners { get; }
    public IReadOnlyList<string> StageDependencyRecords { get; }
    public int CalendarMonthsPerYear { get; }
    public int CalendarWeeksPerMonth { get; }
    public int CalendarDaysPerWeek { get; }
    public IReadOnlyList<int> MonthLengths { get; }
    public int Seed { get; }
    public string SeedSource { get; }
    public IReadOnlyList<string> StageOrder { get; }
    public string FirstSimulatedBoundary { get; }
}
