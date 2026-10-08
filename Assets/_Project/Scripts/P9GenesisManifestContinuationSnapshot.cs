using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Detached P12-C continuation value for the selected P9-B genesis manifest.
/// It preserves the already-produced manifest and never resolves current assets.
/// </summary>
public sealed class P12CP9GenesisManifestSnapshot
{
    public const string SnapshotContractIdentity = "p12c.p9-genesis-manifest-snapshot/v1";
    public const int CurrentSchemaVersion = 1;

    private static readonly string[] ExpectedStageOrder =
    {
        "p9.genesis.resolve-profile/v1",
        "p9.genesis.authored-world/v1",
        SimulationGenesisPipeline.GeographyStageId,
        "p9.genesis.authored-actors/v1",
        "p9.genesis.validate-profile/v1",
        "p9.genesis.publish/v1"
    };

    private static readonly string[] ExpectedOutputOwners =
    {
        "CityRuntime", "MarketCounterpartyRuntime", "PopulationEconomyRuntime", "CityProductionInputs",
        "SpatialNetworkRuntime", "ExplorableSiteStore", "NpcRuntime", "InventoryRuntime", "InitialKnowledge",
        "JusticeSystem", "ScheduledDirectiveStore", "SpatialAuthorityStore", "SimulationRuntime"
    };

    private static readonly string[] ExpectedStageDependencies =
    {
        "resolve-profile -> authored-world",
        "resolve-profile -> authored-actors",
        "authored-world -> authored-actors",
        "authored-world -> validate-profile",
        "authored-actors -> validate-profile",
        "validate-profile -> publish",
        "resolve-profile -> authored-geography",
        "authored-world -> authored-geography",
        "authored-geography -> authored-actors",
        "authored-geography -> validate-profile"
    };

    private static readonly string[] ExpectedCanonicalEdges =
    {
        "edge:p9.genesis.resolve-profile/v1>p9.genesis.authored-geography/v1",
        "edge:p9.genesis.authored-world/v1>p9.genesis.authored-geography/v1",
        "edge:p9.genesis.authored-geography/v1>p9.genesis.authored-actors/v1",
        "edge:p9.genesis.authored-geography/v1>p9.genesis.validate-profile/v1",
        "edge:p9.genesis.resolve-profile/v1>p9.genesis.authored-world/v1",
        "edge:p9.genesis.resolve-profile/v1>p9.genesis.authored-actors/v1",
        "edge:p9.genesis.authored-world/v1>p9.genesis.authored-actors/v1",
        "edge:p9.genesis.authored-world/v1>p9.genesis.validate-profile/v1",
        "edge:p9.genesis.authored-actors/v1>p9.genesis.validate-profile/v1",
        "edge:p9.genesis.validate-profile/v1>p9.genesis.publish/v1"
    };

    internal P12CP9GenesisManifestSnapshot(
        string snapshotContractIdentity,
        int snapshotSchemaVersion,
        SimulationRuntimeAdmissionProfile p12AdmissionProfile,
        string contractIdentity,
        int schemaVersion,
        string fingerprint,
        string selectedP9ProfileFingerprint,
        string selectedP9ContractIdentity,
        int selectedP9SchemaVersion,
        EffectiveSimulationConfiguration effectiveConfiguration,
        IReadOnlyList<string> canonicalProvenanceRecords,
        IReadOnlyList<string> authoredDefinitionIds,
        IReadOnlyList<string> outputOwners,
        IReadOnlyList<string> stageDependencyRecords,
        int calendarMonthsPerYear,
        int calendarWeeksPerMonth,
        int calendarDaysPerWeek,
        IReadOnlyList<int> monthLengths,
        int seed,
        string seedSource,
        IReadOnlyList<string> stageOrder,
        string firstSimulatedBoundary)
    {
        SnapshotContract = snapshotContractIdentity;
        SnapshotSchemaVersion = snapshotSchemaVersion;
        P12AdmissionProfile = p12AdmissionProfile;
        ContractIdentity = contractIdentity;
        SchemaVersion = schemaVersion;
        Fingerprint = fingerprint;
        SelectedP9ProfileFingerprint = selectedP9ProfileFingerprint;
        SelectedP9ContractIdentity = selectedP9ContractIdentity;
        SelectedP9SchemaVersion = selectedP9SchemaVersion;
        EffectiveConfiguration = CloneConfiguration(effectiveConfiguration);
        CanonicalProvenanceRecords = CopyList(canonicalProvenanceRecords);
        AuthoredDefinitionIds = CopyList(authoredDefinitionIds);
        OutputOwners = CopyList(outputOwners);
        StageDependencyRecords = CopyList(stageDependencyRecords);
        CalendarMonthsPerYear = calendarMonthsPerYear;
        CalendarWeeksPerMonth = calendarWeeksPerMonth;
        CalendarDaysPerWeek = calendarDaysPerWeek;
        MonthLengths = CopyList(monthLengths);
        Seed = seed;
        SeedSource = seedSource;
        StageOrder = CopyList(stageOrder);
        FirstSimulatedBoundary = firstSimulatedBoundary;
    }

    public string SnapshotContract { get; }
    public int SnapshotSchemaVersion { get; }
    public SimulationRuntimeAdmissionProfile P12AdmissionProfile { get; }
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

    /// <summary>Captures the exact already-produced P9-B manifest for P12 Daily-v1.</summary>
    public static bool TryCapture(
        SimulationRuntimeAdmissionProfile p12AdmissionProfile,
        SimulationGenesisManifest manifest,
        out P12CP9GenesisManifestSnapshot snapshot,
        out string diagnostic)
    {
        snapshot = null;
        if (p12AdmissionProfile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
        {
            diagnostic = "P12 admission profile must be UnityBootstrapDailyV1.";
            return false;
        }

        if (manifest == null)
        {
            diagnostic = "P9-B genesis manifest is required.";
            return false;
        }

        P12CP9GenesisManifestSnapshot candidate = new P12CP9GenesisManifestSnapshot(
            SnapshotContractIdentity,
            CurrentSchemaVersion,
            p12AdmissionProfile,
            manifest.ContractIdentity,
            manifest.SchemaVersion,
            manifest.Fingerprint,
            manifest.SelectedP9ProfileFingerprint,
            manifest.SelectedP9ContractIdentity,
            manifest.SelectedP9SchemaVersion,
            manifest.EffectiveConfiguration,
            manifest.CanonicalProvenanceRecords,
            manifest.AuthoredDefinitionIds,
            manifest.OutputOwners,
            manifest.StageDependencyRecords,
            manifest.CalendarMonthsPerYear,
            manifest.CalendarWeeksPerMonth,
            manifest.CalendarDaysPerWeek,
            manifest.MonthLengths,
            manifest.Seed,
            manifest.SeedSource,
            manifest.StageOrder,
            manifest.FirstSimulatedBoundary);
        if (!candidate.TryValidate(out diagnostic))
        {
            return false;
        }

        snapshot = candidate;
        return true;
    }

    /// <summary>Validates this retained value and privately stages a direct manifest reconstruction.</summary>
    internal bool TryStageManifest(out SimulationGenesisManifest manifest, out string diagnostic)
    {
        return SimulationGenesisManifest.TryCreateFromSnapshot(this, out manifest, out diagnostic);
    }

    internal bool TryValidate(out string diagnostic)
    {
        if (!string.Equals(SnapshotContract, SnapshotContractIdentity, StringComparison.Ordinal)
            || SnapshotSchemaVersion != CurrentSchemaVersion)
        {
            diagnostic = "Snapshot contract identity or schema version is unsupported.";
            return false;
        }

        if (P12AdmissionProfile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
        {
            diagnostic = "P12 admission profile must be UnityBootstrapDailyV1.";
            return false;
        }

        if (!string.Equals(ContractIdentity, SimulationGenesisPipeline.GeographyProfileContractIdentity, StringComparison.Ordinal)
            || SchemaVersion != 2
            || !string.Equals(SelectedP9ContractIdentity, SimulationGenesisPipeline.GeographyProfileContractIdentity, StringComparison.Ordinal)
            || SelectedP9SchemaVersion != 2)
        {
            diagnostic = "Manifest and selected P9 identities must describe P9-B authored geography schema 2.";
            return false;
        }

        if (!IsNonBlank(Fingerprint) || !IsNonBlank(SelectedP9ProfileFingerprint)
            || !IsSha256(Fingerprint) || !IsSha256(SelectedP9ProfileFingerprint))
        {
            diagnostic = "Both P9-B fingerprints must be non-blank lowercase SHA-256 values.";
            return false;
        }

        if (!string.Equals(Fingerprint, SelectedP9ProfileFingerprint, StringComparison.Ordinal))
        {
            diagnostic = "P9-B full-profile and selected-P9 fingerprints must be equal.";
            return false;
        }

        if (CanonicalProvenanceRecords == null || CanonicalProvenanceRecords.Count == 0
            || !HasOnlyNonBlankEntries(CanonicalProvenanceRecords))
        {
            diagnostic = "Canonical P9 provenance records must be present and non-blank.";
            return false;
        }

        if (!HeaderRecordsMatchSnapshot())
        {
            diagnostic = "P9 profile identity, schema, seed source, or seed value differs from retained provenance.";
            return false;
        }

        foreach (string record in CanonicalProvenanceRecords)
        {
            if (record.StartsWith("p10-record|", StringComparison.Ordinal)
                || record.StartsWith("p10b-record|", StringComparison.Ordinal)
                || ContainsExcludedTopologyMarker(record))
            {
                diagnostic = "P9-B provenance cannot contain P10 topology records.";
                return false;
            }
        }

        string retainedFingerprint = SimulationGenesisPipeline.ComputeFingerprint(CanonicalProvenanceRecords);
        if (!string.Equals(retainedFingerprint, Fingerprint, StringComparison.Ordinal)
            || !string.Equals(retainedFingerprint, SelectedP9ProfileFingerprint, StringComparison.Ordinal))
        {
            diagnostic = "Retained P9 provenance records do not match both recorded fingerprints.";
            return false;
        }

        if (!HasExactOrderedRecords(CanonicalProvenanceRecords, "stage:", ExpectedStageRecords())
            || !HasExactOrderedRecords(CanonicalProvenanceRecords, "edge:", ExpectedCanonicalEdges)
            || !HasExactOrderedRecords(CanonicalProvenanceRecords, "output-owner|", ExpectedCanonicalOutputOwners())
            || !HasExactlyOne(CanonicalProvenanceRecords, CanonicalGeographyStageRecord())
            || !HasExactlyOne(CanonicalProvenanceRecords, CanonicalGeographyInputRecord())
            || !HasExactlyOne(CanonicalProvenanceRecords, CanonicalGeographyOutputRecord())
            || !HasExactlyOne(CanonicalProvenanceRecords, "first-simulated-boundary:day-1"))
        {
            diagnostic = "Canonical provenance must retain the exact P9-A/P9-B stage, dependency, output-owner, geography, and day-one evidence.";
            return false;
        }

        if (!string.Equals(SeedSource, "authored-fixed", StringComparison.Ordinal)
            && !string.Equals(SeedSource, "default-zero", StringComparison.Ordinal))
        {
            diagnostic = "Seed source must match a supported P9 genesis manifest value.";
            return false;
        }

        if (string.Equals(SeedSource, "default-zero", StringComparison.Ordinal) && Seed != 0)
        {
            diagnostic = "The default-zero seed source must retain seed zero.";
            return false;
        }

        if (!string.Equals(FirstSimulatedBoundary, "advance-day:1", StringComparison.Ordinal))
        {
            diagnostic = "P9-B first simulated boundary must be advance-day:1.";
            return false;
        }

        if (!HasUniqueNonBlankEntries(AuthoredDefinitionIds))
        {
            diagnostic = "Authored definition IDs must be present, non-blank, and unique.";
            return false;
        }

        if (!SequenceEquals(StageOrder, ExpectedStageOrder))
        {
            diagnostic = "Stage order must be the exact P9-A plus one P9-B authored-geography lineage.";
            return false;
        }

        if (!SequenceEquals(StageDependencyRecords, ExpectedStageDependencies))
        {
            diagnostic = "Stage dependencies must be the exact P9-B dependency graph without P10 stages.";
            return false;
        }

        if (!SequenceEquals(OutputOwners, ExpectedOutputOwners))
        {
            diagnostic = "Output owners must be the P9-B profile owner set without P10 topology owners.";
            return false;
        }

        if (EffectiveConfiguration == null)
        {
            diagnostic = "Effective simulation configuration is required.";
            return false;
        }

        SimulationConfigurationValidationResult configurationValidation =
            SimulationConfigurationValidator.Validate(EffectiveConfiguration);
        if (!configurationValidation.IsValid)
        {
            diagnostic = "Effective simulation configuration is invalid: "
                + string.Join("; ", configurationValidation.Errors);
            return false;
        }

        if (!EffectiveConfigurationMatchesRecords())
        {
            diagnostic = "Effective simulation configuration differs from retained provenance records.";
            return false;
        }

        if (MonthLengths == null)
        {
            diagnostic = "Calendar month-length values are required (an empty list represents uniform months).";
            return false;
        }

        CalendarDefinition calendar = new CalendarDefinition
        {
            monthsPerYear = CalendarMonthsPerYear,
            weeksPerMonth = CalendarWeeksPerMonth,
            daysPerWeek = CalendarDaysPerWeek,
            monthLengths = new List<int>(MonthLengths)
        };
        if (!calendar.TryValidate(out diagnostic))
        {
            diagnostic = "Recorded calendar is invalid: " + diagnostic;
            return false;
        }

        if (!CalendarMatchesRecords())
        {
            diagnostic = "Calendar dimensions or month lengths differ from retained provenance records.";
            return false;
        }

        if (!AuthoredDefinitionIdsMatchRecords())
        {
            diagnostic = "Authored definition IDs differ from the IDs encoded by retained provenance records.";
            return false;
        }

        diagnostic = null;
        return true;
    }

    internal static EffectiveSimulationConfiguration CloneConfiguration(
        EffectiveSimulationConfiguration value)
    {
        if (value == null) return null;
        // EffectiveSimulationConfiguration supplies defaults when these optional
        // constructor arguments are null. A continuation copy must not turn a
        // malformed recorded value into those defaults.
        if (value.MerchantTrade == null
            || value.CommercialKnowledge == null
            || value.NaturalMortality == null
            || value.AggregateDemography == null)
        {
            return null;
        }

        EffectivePopulationConfiguration population = value.Population == null ? null
            : new EffectivePopulationConfiguration(value.Population.RepresentationMode,
                value.Population.DecisionScope, value.Population.MaturityAgeYears);
        EffectiveEconomyConfiguration economy = value.Economy == null ? null
            : new EffectiveEconomyConfiguration(value.Economy.Enabled);
        EffectiveTravelConfiguration travel = value.Travel == null ? null
            : new EffectiveTravelConfiguration(value.Travel.TravelCostPerDay);
        EffectiveCrimeConfiguration crime = value.Crime == null ? null
            : new EffectiveCrimeConfiguration(value.Crime.Enabled, value.Crime.AutonomousEnabled);
        EffectiveGuardCrimeConfiguration guardCrime = value.GuardCrime == null ? null
            : new EffectiveGuardCrimeConfiguration(value.GuardCrime.Enabled);
        EffectiveMerchantTradeConfiguration merchantTrade = value.MerchantTrade == null ? null
            : new EffectiveMerchantTradeConfiguration(value.MerchantTrade.Enabled,
                value.MerchantTrade.AllowAutonomousTradeRepositioning, value.MerchantTrade.MaxTradeAmount,
                value.MerchantTrade.LocalWholesalePriceMultiplier, value.MerchantTrade.LocalReserveRatio,
                value.MerchantTrade.MaxUnprofitablePlanWaitDays);
        EffectiveCommercialKnowledgeConfiguration commercialKnowledge = value.CommercialKnowledge == null ? null
            : new EffectiveCommercialKnowledgeConfiguration(value.CommercialKnowledge.FreshForDays,
                value.CommercialKnowledge.MaxUsefulAgeDays,
                value.CommercialKnowledge.MaxSharedObservationsPerInteraction);
        EffectiveNaturalMortalityConfiguration naturalMortality = value.NaturalMortality == null ? null
            : new EffectiveNaturalMortalityConfiguration(value.NaturalMortality.Policy,
                value.NaturalMortality.AnnualProbability);
        EffectiveAggregateDemographyConfiguration aggregateDemography = value.AggregateDemography == null ? null
            : new EffectiveAggregateDemographyConfiguration(value.AggregateDemography.Policy,
                value.AggregateDemography.AnnualBirthRate, value.AggregateDemography.AnnualDeathRate);

        // Supplying every nested value prevents constructor defaults from changing a
        // historical effective configuration while it crosses the snapshot boundary.
        return new EffectiveSimulationConfiguration(population, economy, travel, crime, guardCrime,
            naturalMortality, aggregateDemography, merchantTrade, commercialKnowledge);
    }

    private static IReadOnlyList<T> CopyList<T>(IReadOnlyList<T> source)
    {
        return source == null ? null : Array.AsReadOnly(new List<T>(source).ToArray());
    }

    private static bool IsNonBlank(string value) => !string.IsNullOrWhiteSpace(value);

    private static bool ContainsExcludedTopologyMarker(string record)
    {
        return record.IndexOf(P10RuinLocalTopologyGenesis.StageId, StringComparison.Ordinal) >= 0
            || record.IndexOf(SimulationGenesisPipeline.P10BGeneratedRuinStageId, StringComparison.Ordinal) >= 0
            || record.IndexOf("LocalTopologyStore", StringComparison.Ordinal) >= 0
            || record.IndexOf("LegacySpatialAnchorBindingStore", StringComparison.Ordinal) >= 0;
    }

    private static string[] ExpectedStageRecords()
    {
        string[] records = new string[ExpectedStageOrder.Length];
        for (int i = 0; i < ExpectedStageOrder.Length; i++) records[i] = "stage:" + ExpectedStageOrder[i];
        return records;
    }

    private static string[] ExpectedCanonicalOutputOwners()
    {
        string[] records = new string[ExpectedOutputOwners.Length];
        for (int i = 0; i < ExpectedOutputOwners.Length; i++)
            records[i] = "output-owner|" + ExpectedOutputOwners[i].Length + ":" + ExpectedOutputOwners[i];
        return records;
    }

    private static string CanonicalGeographyStageRecord()
    {
        string stageId = SimulationGenesisPipeline.GeographyStageId;
        return "authored-geography-stage|" + stageId.Length + ":" + stageId + "|1:1";
    }

    private static string CanonicalGeographyInputRecord()
    {
        string stageId = SimulationGenesisPipeline.GeographyStageId;
        const string contract = "authored-geography-profile/v1";
        return "stage-input|" + stageId.Length + ":" + stageId + "|" + contract.Length + ":" + contract;
    }

    private static string CanonicalGeographyOutputRecord()
    {
        string stageId = SimulationGenesisPipeline.GeographyStageId;
        const string output = "SpatialAuthorityStore/geography-v1";
        return "stage-output|" + stageId.Length + ":" + stageId + "|" + output.Length + ":" + output;
    }

    private static bool IsSha256(string value)
    {
        if (value == null || value.Length != 64) return false;
        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];
            if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
                return false;
        }
        return true;
    }

    private static bool HasOnlyNonBlankEntries(IReadOnlyList<string> values)
    {
        if (values == null) return false;
        for (int i = 0; i < values.Count; i++)
            if (!IsNonBlank(values[i])) return false;
        return true;
    }

    private static bool HasUniqueNonBlankEntries(IReadOnlyList<string> values)
    {
        if (!HasOnlyNonBlankEntries(values) || values.Count == 0) return false;
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < values.Count; i++)
            if (!seen.Add(values[i])) return false;
        return true;
    }

    private static bool SequenceEquals(IReadOnlyList<string> actual, IReadOnlyList<string> expected)
    {
        if (actual == null || actual.Count != expected.Length) return false;
        for (int i = 0; i < expected.Length; i++)
            if (!string.Equals(actual[i], expected[i], StringComparison.Ordinal)) return false;
        return true;
    }

    private static bool HasExactlyOne(IReadOnlyList<string> values, string expected)
    {
        int count = 0;
        for (int i = 0; i < values.Count; i++)
            if (string.Equals(values[i], expected, StringComparison.Ordinal)) count++;
        return count == 1;
    }

    private bool HeaderRecordsMatchSnapshot()
    {
        if (CanonicalProvenanceRecords.Count < 4) return false;
        string expectedSeedMode = string.Equals(SeedSource, "authored-fixed", StringComparison.Ordinal)
            ? "fixed-seed"
            : string.Equals(SeedSource, "default-zero", StringComparison.Ordinal) ? "default-seed" : null;
        return string.Equals(CanonicalProvenanceRecords[0], ContractIdentity, StringComparison.Ordinal)
            && string.Equals(CanonicalProvenanceRecords[1], "schema=" + SchemaVersion.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            && expectedSeedMode != null
            && string.Equals(CanonicalProvenanceRecords[2], expectedSeedMode, StringComparison.Ordinal)
            && string.Equals(CanonicalProvenanceRecords[3], Seed.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private bool EffectiveConfigurationMatchesRecords()
    {
        EffectivePopulationConfiguration population = EffectiveConfiguration.Population;
        EffectiveEconomyConfiguration economy = EffectiveConfiguration.Economy;
        EffectiveTravelConfiguration travel = EffectiveConfiguration.Travel;
        EffectiveCrimeConfiguration crime = EffectiveConfiguration.Crime;
        EffectiveGuardCrimeConfiguration guardCrime = EffectiveConfiguration.GuardCrime;
        EffectiveMerchantTradeConfiguration merchant = EffectiveConfiguration.MerchantTrade;
        EffectiveCommercialKnowledgeConfiguration knowledge = EffectiveConfiguration.CommercialKnowledge;
        EffectiveNaturalMortalityConfiguration mortality = EffectiveConfiguration.NaturalMortality;
        EffectiveAggregateDemographyConfiguration demography = EffectiveConfiguration.AggregateDemography;

        return HasExactlyOne(CanonicalProvenanceRecords, EncodeTaggedRecord("effective.population",
                population.RepresentationMode, population.DecisionScope, population.MaturityAgeYears))
            && HasExactlyOne(CanonicalProvenanceRecords, EncodeTaggedRecord("effective.economy", economy.Enabled))
            && HasExactlyOne(CanonicalProvenanceRecords, EncodeTaggedRecord("effective.travel",
                travel.TravelCostPerDay.ToString("R", CultureInfo.InvariantCulture)))
            && HasExactlyOne(CanonicalProvenanceRecords, EncodeTaggedRecord("effective.crime",
                crime.Enabled, crime.AutonomousEnabled))
            && HasExactlyOne(CanonicalProvenanceRecords, EncodeTaggedRecord("effective.guard-crime",
                guardCrime.Enabled))
            && HasExactlyOne(CanonicalProvenanceRecords, EncodeTaggedRecord("effective.merchant-trade",
                merchant.Enabled, merchant.AllowAutonomousTradeRepositioning, merchant.MaxTradeAmount,
                merchant.LocalWholesalePriceMultiplier.ToString("R", CultureInfo.InvariantCulture),
                merchant.LocalReserveRatio.ToString("R", CultureInfo.InvariantCulture),
                merchant.MaxUnprofitablePlanWaitDays))
            && HasExactlyOne(CanonicalProvenanceRecords, EncodeTaggedRecord("effective.commercial-knowledge",
                knowledge.FreshForDays, knowledge.MaxUsefulAgeDays,
                knowledge.MaxSharedObservationsPerInteraction))
            && HasExactlyOne(CanonicalProvenanceRecords, EncodeTaggedRecord("effective.natural-mortality",
                mortality.Policy, mortality.AnnualProbability.ToString("R", CultureInfo.InvariantCulture)))
            && HasExactlyOne(CanonicalProvenanceRecords, EncodeTaggedRecord("effective.aggregate-demography",
                demography.Policy, demography.AnnualBirthRate.ToString("R", CultureInfo.InvariantCulture),
                demography.AnnualDeathRate.ToString("R", CultureInfo.InvariantCulture)));
    }

    private bool CalendarMatchesRecords()
    {
        if (!HasExactlyOne(CanonicalProvenanceRecords, EncodeTaggedRecord("calendar",
            CalendarMonthsPerYear, CalendarWeeksPerMonth, CalendarDaysPerWeek)))
        {
            return false;
        }

        List<string> expectedMonthRecords = new List<string>(MonthLengths.Count);
        for (int i = 0; i < MonthLengths.Count; i++)
            expectedMonthRecords.Add(EncodeTaggedRecord("calendar-month-length", i, MonthLengths[i]));
        return HasExactOrderedRecords(CanonicalProvenanceRecords, "calendar-month-length|", expectedMonthRecords);
    }

    private bool AuthoredDefinitionIdsMatchRecords()
    {
        HashSet<string> expectedIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < CanonicalProvenanceRecords.Count; i++)
        {
            string record = CanonicalProvenanceRecords[i];
            if (!TryParseTaggedRecord(record, out string kind, out List<string> values))
            {
                if (IsDefinitionRecordKind(kind)) return false;
                continue;
            }

            if (kind == "city")
            {
                if (!AddDefinitionId(expectedIds, "city", values, 0)) return false;
            }
            else if (kind == "market")
            {
                if (!AddDefinitionId(expectedIds, "item", values, 1)) return false;
            }
            else if (kind == "production")
            {
                if (!AddDefinitionId(expectedIds, "item", values, 2)) return false;
            }
            else if (kind == "site")
            {
                if (!AddDefinitionId(expectedIds, "site", values, 0)) return false;
            }
            else if (kind == "authored-hex")
            {
                if (!AddDefinitionId(expectedIds, "hex", values, 0)
                    || !AddDefinitionId(expectedIds, "terrain", values, 5)) return false;
            }
            else if (kind == "authored-location")
            {
                if (!AddDefinitionId(expectedIds, "location", values, 0)
                    || !AddDefinitionId(expectedIds, "hex", values, 1)) return false;
            }
            else if (kind == "npc")
            {
                if (!AddDefinitionId(expectedIds, "npc", values, 0)
                    || !AddDefinitionId(expectedIds, "job", values, 3)) return false;
            }
            else if (kind == "npc-capability")
            {
                if (!AddPackedDefinitionId(expectedIds, "capability-attribute", values, 1, 0)) return false;
            }
            else if (kind == "action")
            {
                if (!AddDefinitionId(expectedIds, "action", values, 0)) return false;
            }
            else if (kind == "action-status-weight")
            {
                if (!AddPackedDefinitionId(expectedIds, "status", values, 1, 0)) return false;
            }
            else if (kind == "status")
            {
                if (!AddDefinitionId(expectedIds, "status", values, 0)) return false;
            }
            else if (kind == "job")
            {
                if (!AddDefinitionId(expectedIds, "job", values, 0)
                    || !AddDefinitionId(expectedIds, "action", values, 4)) return false;
            }
            else if (kind == "item")
            {
                if (!AddDefinitionId(expectedIds, "item", values, 0)) return false;
            }
            else if (kind == "item-capability-modifier")
            {
                if (!AddDefinitionId(expectedIds, "item", values, 0)
                    || !AddPackedDefinitionId(expectedIds, "capability-attribute", values, 1, 0)) return false;
            }
            else if (kind == "trait")
            {
                if (!AddDefinitionId(expectedIds, "trait", values, 0)) return false;
            }
            else if (kind == "capability-attribute")
            {
                if (!AddDefinitionId(expectedIds, "capability-attribute", values, 0)) return false;
            }
            else if (kind.StartsWith("npc-default-action:", StringComparison.Ordinal))
            {
                if (!AddPackedDefinitionId(expectedIds, "action", values, 1, 0)) return false;
            }
            else if (kind.StartsWith("npc-default-status:", StringComparison.Ordinal))
            {
                if (!AddDefinitionId(expectedIds, "status", values, 1)) return false;
            }
            else if (kind.StartsWith("npc-trait:", StringComparison.Ordinal))
            {
                if (!AddDefinitionId(expectedIds, "trait", values, 1)) return false;
            }
            else if (kind.StartsWith("inventory:", StringComparison.Ordinal)
                || kind.StartsWith("job-preference:", StringComparison.Ordinal))
            {
                if (!AddPackedDefinitionId(expectedIds, "item", values, 1, 0)) return false;
            }
            else if (IsActionStatusRecordKind(kind))
            {
                if (!AddPackedDefinitionId(expectedIds, "status", values, 1, 0)) return false;
            }
        }

        List<string> orderedIds = new List<string>(expectedIds);
        orderedIds.Sort(StringComparer.Ordinal);
        if (orderedIds.Count != AuthoredDefinitionIds.Count) return false;
        for (int i = 0; i < orderedIds.Count; i++)
            if (!string.Equals(orderedIds[i], AuthoredDefinitionIds[i], StringComparison.Ordinal)) return false;
        return true;
    }

    private static bool IsActionStatusRecordKind(string kind)
    {
        return kind.StartsWith("action-required-status:", StringComparison.Ordinal)
            || kind.StartsWith("action-add-status:", StringComparison.Ordinal)
            || kind.StartsWith("action-remove-status:", StringComparison.Ordinal)
            || kind.StartsWith("action-target-add-status:", StringComparison.Ordinal)
            || kind.StartsWith("action-target-remove-status:", StringComparison.Ordinal);
    }

    private static bool IsDefinitionRecordKind(string kind)
    {
        return kind == "city" || kind == "market" || kind == "production" || kind == "site"
            || kind == "authored-hex" || kind == "authored-location" || kind == "npc"
            || kind == "npc-capability" || kind == "action" || kind == "action-status-weight"
            || kind == "status" || kind == "job" || kind == "item" || kind == "item-capability-modifier"
            || kind == "trait" || kind == "capability-attribute"
            || (kind != null && (kind.StartsWith("npc-default-action:", StringComparison.Ordinal)
                || kind.StartsWith("npc-default-status:", StringComparison.Ordinal)
                || kind.StartsWith("npc-trait:", StringComparison.Ordinal)
                || kind.StartsWith("inventory:", StringComparison.Ordinal)
                || kind.StartsWith("job-preference:", StringComparison.Ordinal)
                || IsActionStatusRecordKind(kind)));
    }

    private static bool AddDefinitionId(HashSet<string> ids, string type, IReadOnlyList<string> values, int index)
    {
        if (index < 0 || index >= values.Count) return false;
        if (values[index].Length > 0) ids.Add(type + "/" + values[index]);
        return true;
    }

    private static bool AddPackedDefinitionId(
        HashSet<string> ids,
        string type,
        IReadOnlyList<string> values,
        int packedIndex,
        int valueIndex)
    {
        if (packedIndex < 0 || packedIndex >= values.Count
            || !TryParsePackedValues(values[packedIndex], out List<string> packedValues)
            || valueIndex < 0 || valueIndex >= packedValues.Count) return false;
        if (packedValues[valueIndex].Length > 0) ids.Add(type + "/" + packedValues[valueIndex]);
        return true;
    }

    private static bool TryParseTaggedRecord(string record, out string kind, out List<string> values)
    {
        kind = null;
        values = null;
        int separator = record.IndexOf('|');
        if (separator < 0) return false;
        kind = record.Substring(0, separator);
        values = new List<string>();
        return TryParseLengthPrefixedValues(record, separator + 1, true, values);
    }

    private static bool TryParsePackedValues(string packed, out List<string> values)
    {
        values = new List<string>();
        return TryParseLengthPrefixedValues(packed, 0, false, values);
    }

    private static bool TryParseLengthPrefixedValues(
        string input,
        int offset,
        bool separated,
        List<string> values)
    {
        if (offset >= input.Length) return false;
        while (offset < input.Length)
        {
            int colon = input.IndexOf(':', offset);
            if (colon <= offset
                || !int.TryParse(input.Substring(offset, colon - offset), NumberStyles.None,
                    CultureInfo.InvariantCulture, out int valueLength)
                || valueLength < 0)
            {
                return false;
            }

            int valueStart = colon + 1;
            if (valueLength > input.Length - valueStart) return false;
            values.Add(input.Substring(valueStart, valueLength));
            offset = valueStart + valueLength;
            if (offset == input.Length) return true;
            if (separated)
            {
                if (input[offset] != '|') return false;
                offset++;
                if (offset == input.Length) return false;
            }
        }
        return values.Count > 0;
    }

    private static string EncodeTaggedRecord(string kind, params object[] values)
    {
        string record = kind;
        for (int i = 0; i < values.Length; i++)
        {
            string value = Convert.ToString(values[i], CultureInfo.InvariantCulture) ?? string.Empty;
            record += "|" + value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
        }
        return record;
    }

    private static bool HasExactOrderedRecords(
        IReadOnlyList<string> allRecords,
        string prefix,
        IReadOnlyList<string> expectedRecords)
    {
        List<string> actual = new List<string>();
        for (int i = 0; i < allRecords.Count; i++)
            if (allRecords[i].StartsWith(prefix, StringComparison.Ordinal)) actual.Add(allRecords[i]);
        if (actual.Count != expectedRecords.Count) return false;
        for (int i = 0; i < actual.Count; i++)
            if (!string.Equals(actual[i], expectedRecords[i], StringComparison.Ordinal)) return false;
        return true;
    }
}
