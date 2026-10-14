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

    public static string CreateFingerprint(
        SimulationConfigData config,
        EffectiveSimulationConfiguration effectiveConfiguration,
        CalendarDefinition resolvedCalendar,
        out IReadOnlyList<string> canonicalRecords)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (effectiveConfiguration == null) throw new ArgumentNullException(nameof(effectiveConfiguration));
        if (resolvedCalendar == null) throw new ArgumentNullException(nameof(resolvedCalendar));
        if (!DailyV1DefinitionAdapter.TryCreate(config, effectiveConfiguration, resolvedCalendar,
            ResolveStageOrder(config.useAuthoredGeographyProfile), HexCoordinate.ConventionVersion,
            HexCoordinate.CanonicalOrder, out DailyV1EffectiveDefinitions definitions, out string diagnostic))
            throw new InvalidOperationException(diagnostic ?? "The selected Daily-v1 definition graph is invalid.");
        return DailyV1DefinitionIdentity.CreateP9Fingerprint(definitions, out canonicalRecords);


    }

    private static void Add(List<string> fields, string kind, params object[] values)
    {
        var record = new StringBuilder(kind);
        foreach (object value in values)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            record.Append('|').Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text);
        }
        fields.Add(record.ToString());
    }

    private static string Pack(params object[] values)
    {
        var record = new StringBuilder();
        foreach (object value in values)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            record.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text);
        }
        return record.ToString();
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
        bool includeP10BGeneratedRuin = false)
    {
        SchemaVersion = includeP10BGeneratedRuin ? 1 : includeP10RuinLocalTopology
            ? P10RuinLocalTopologyGenesis.ContractSchemaVersion
            : config.useAuthoredGeographyProfile ? 2 : 1;
        Fingerprint = fingerprint;
        SelectedP9ProfileFingerprint = selectedP9Fingerprint;
        SelectedP9ContractIdentity = config.useAuthoredGeographyProfile
            ? SimulationGenesisPipeline.GeographyProfileContractIdentity
            : SimulationGenesisPipeline.ProfileContractIdentity;
        SelectedP9SchemaVersion = config.useAuthoredGeographyProfile ? 2 : 1;
        EffectiveConfiguration = effectiveConfiguration;
        Seed = config.useFixedSimulationSeed ? config.simulationSeed : 0;
        SeedSource = config.useFixedSimulationSeed ? "authored-fixed" : "default-zero";
        ContractIdentity = includeP10BGeneratedRuin ? SimulationGenesisPipeline.P10BGeneratedRuinProfileContractIdentity
            : includeP10RuinLocalTopology ? P10RuinLocalTopologyGenesis.ContractIdentity
            : SelectedP9ContractIdentity;
        StageOrder = SimulationGenesisPipeline.ResolveStageOrder(config.useAuthoredGeographyProfile, includeP10RuinLocalTopology, includeP10BGeneratedRuin);
        CanonicalProvenanceRecords = canonicalRecords;
        CalendarMonthsPerYear = calendar.MonthsPerYear;
        CalendarWeeksPerMonth = calendar.WeeksPerMonth;
        CalendarDaysPerWeek = calendar.DaysPerWeek;
        MonthLengths = Array.AsReadOnly(new List<int>(calendar.monthLengths ?? new List<int>()).ToArray());
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (CityData city in config.Cities)
        {
            ids.Add("city/" + city.DefinitionId);
            foreach (MarketItemConfig market in city.marketItems) AddId(ids, "item", market.item);
            foreach (CityProductionConfig production in city.productionConfigs) AddId(ids, "item", production.item);
        }
        foreach (ExplorableSiteConfig site in config.ExplorableSites) ids.Add("site/" + site.site.DefinitionId);
        if (includeP10RuinLocalTopology || includeP10BGeneratedRuin)
            ids.Add("site/" + config.authoredP10RuinSite.DefinitionId);
        if (config.useAuthoredGeographyProfile)
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
        if (config.useAuthoredGeographyProfile)
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
