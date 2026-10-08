using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class P12CP9GenesisManifestSnapshotTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = simulationObjects.Count - 1; i >= 0; i--)
        {
            if (simulationObjects[i] != null) UnityEngine.Object.DestroyImmediate(simulationObjects[i]);
        }
        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void CaptureDetachesTheCompleteP9BManifestAndStagesItWithoutLiveAssets()
    {
        SimulationGenesisManifest source = CreateP9BManifest();
        Assert.That(P12CP9GenesisManifestSnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            source,
            out P12CP9GenesisManifestSnapshot snapshot,
            out string captureDiagnostic), Is.True, captureDiagnostic);

        Assert.That(snapshot.SnapshotContract, Is.EqualTo(P12CP9GenesisManifestSnapshot.SnapshotContractIdentity));
        Assert.That(snapshot.SnapshotSchemaVersion, Is.EqualTo(P12CP9GenesisManifestSnapshot.CurrentSchemaVersion));
        Assert.That(snapshot.P12AdmissionProfile, Is.EqualTo(SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1));
        Assert.That(HasRecordWithPrefixAndText(snapshot.CanonicalProvenanceRecords,
            "action-required-status:", "Status-Livre"), Is.True,
            "Daily-v1 Viajar has a required status recorded as a raw DefinitionId.");
        AssertManifestMatchesSnapshot(source, snapshot);
        Assert.That(snapshot.CanonicalProvenanceRecords, Is.Not.SameAs(source.CanonicalProvenanceRecords));
        Assert.That(snapshot.AuthoredDefinitionIds, Is.Not.SameAs(source.AuthoredDefinitionIds));
        Assert.That(snapshot.StageOrder, Is.Not.SameAs(source.StageOrder));
        Assert.That(snapshot.EffectiveConfiguration, Is.Not.SameAs(source.EffectiveConfiguration));
        Assert.That(snapshot.EffectiveConfiguration.Population, Is.Not.SameAs(source.EffectiveConfiguration.Population));

        for (int i = simulationObjects.Count - 1; i >= 0; i--)
        {
            if (simulationObjects[i] != null) UnityEngine.Object.DestroyImmediate(simulationObjects[i]);
        }
        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();

        Assert.That(snapshot.TryStageManifest(out SimulationGenesisManifest staged, out string stageDiagnostic),
            Is.True, stageDiagnostic);
        Assert.That(staged, Is.Not.Null);
        AssertManifestMatchesSnapshot(staged, snapshot);
        Assert.That(staged.CanonicalProvenanceRecords, Is.Not.SameAs(snapshot.CanonicalProvenanceRecords));
        Assert.That(staged.EffectiveConfiguration, Is.Not.SameAs(snapshot.EffectiveConfiguration));
        Assert.That(staged.EffectiveConfiguration, Is.EqualTo(snapshot.EffectiveConfiguration));
        Assert.That(staged.StageOrder, Is.EqualTo(new[]
        {
            "p9.genesis.resolve-profile/v1",
            "p9.genesis.authored-world/v1",
            SimulationGenesisPipeline.GeographyStageId,
            "p9.genesis.authored-actors/v1",
            "p9.genesis.validate-profile/v1",
            "p9.genesis.publish/v1"
        }));
        Assert.That(staged.OutputOwners, Does.Not.Contain("LocalTopologyStore"));
        Assert.That(staged.OutputOwners, Does.Not.Contain("LegacySpatialAnchorBindingStore"));
        AssertNoRecordsWithPrefix(staged.CanonicalProvenanceRecords, "p10-record|");
        AssertNoRecordsWithPrefix(staged.CanonicalProvenanceRecords, "p10b-record|");
        Assert.That(staged.Fingerprint, Is.EqualTo(staged.SelectedP9ProfileFingerprint));
    }

    [Test]
    public void SnapshotCopiesMutableInputsAndRejectsIndependentProfileAndLineageMismatches()
    {
        SimulationGenesisManifest source = CreateP9BManifest();
        P12CP9GenesisManifestSnapshot snapshot = Capture(source);

        List<string> mutableRecords = new List<string>(snapshot.CanonicalProvenanceRecords);
        P12CP9GenesisManifestSnapshot detached = CopySnapshot(
            snapshot, canonicalRecords: mutableRecords, replaceCanonicalRecords: true);
        string retainedFirstRecord = detached.CanonicalProvenanceRecords[0];
        mutableRecords[0] = "mutated-after-snapshot";
        Assert.That(detached.CanonicalProvenanceRecords[0], Is.EqualTo(retainedFirstRecord));

        AssertRejected(CopySnapshot(snapshot, snapshotSchema: P12CP9GenesisManifestSnapshot.CurrentSchemaVersion + 1));
        AssertRejected(CopySnapshot(snapshot, snapshotContract: "unknown-snapshot-contract"));
        AssertRejected(CopySnapshot(snapshot, admissionProfile: SimulationRuntimeAdmissionProfile.None));
        AssertRejected(CopySnapshot(snapshot, contractIdentity: SimulationGenesisPipeline.ProfileContractIdentity));
        AssertRejected(CopySnapshot(snapshot, selectedContractIdentity: SimulationGenesisPipeline.ProfileContractIdentity));
        AssertRejected(CopySnapshot(snapshot, manifestSchema: 1));
        AssertRejected(CopySnapshot(snapshot, selectedP9Schema: 1));
        AssertRejected(CopySnapshot(snapshot, fingerprint: new string('0', 64)));
        AssertRejected(CopySnapshot(snapshot, fingerprint: string.Empty));
        AssertRejected(CopySnapshot(snapshot, firstBoundary: "advance-day:2"));

        List<string> reorderedStages = new List<string>(snapshot.StageOrder);
        string first = reorderedStages[0];
        reorderedStages[0] = reorderedStages[1];
        reorderedStages[1] = first;
        AssertRejected(CopySnapshot(snapshot, stageOrder: reorderedStages, replaceStageOrder: true));

        List<string> withP10Owner = new List<string>(snapshot.OutputOwners) { "LocalTopologyStore" };
        AssertRejected(CopySnapshot(snapshot, outputOwners: withP10Owner, replaceOutputOwners: true));

        List<string> changedDependencies = new List<string>(snapshot.StageDependencyRecords);
        changedDependencies[0] = "resolve-profile -> unrelated-stage";
        AssertRejected(CopySnapshot(snapshot, dependencies: changedDependencies, replaceDependencies: true));

        List<string> duplicateIds = new List<string>(snapshot.AuthoredDefinitionIds)
        {
            snapshot.AuthoredDefinitionIds[0]
        };
        AssertRejected(CopySnapshot(snapshot, authoredDefinitionIds: duplicateIds, replaceAuthoredDefinitionIds: true));

        List<string> p10Records = new List<string>(snapshot.CanonicalProvenanceRecords)
        {
            "stage:p10b.genesis.ruin-topology/v1"
        };
        string p10Fingerprint = SimulationGenesisPipeline.ComputeFingerprint(p10Records);
        AssertRejected(CopySnapshot(snapshot, fingerprint: p10Fingerprint,
            selectedFingerprint: p10Fingerprint, canonicalRecords: p10Records,
            replaceCanonicalRecords: true));

        List<string> missingGeographyStage = new List<string>(snapshot.CanonicalProvenanceRecords);
        Assert.That(missingGeographyStage.Remove("stage:" + SimulationGenesisPipeline.GeographyStageId), Is.True);
        string missingStageFingerprint = SimulationGenesisPipeline.ComputeFingerprint(missingGeographyStage);
        AssertRejected(CopySnapshot(snapshot, fingerprint: missingStageFingerprint,
            selectedFingerprint: missingStageFingerprint, canonicalRecords: missingGeographyStage,
            replaceCanonicalRecords: true));

        EffectivePopulationConfiguration invalidPopulation = new EffectivePopulationConfiguration(
            (PopulationRepresentationMode)99,
            snapshot.EffectiveConfiguration.Population.DecisionScope,
            snapshot.EffectiveConfiguration.Population.MaturityAgeYears);
        EffectiveSimulationConfiguration invalidConfiguration = new EffectiveSimulationConfiguration(
            invalidPopulation,
            snapshot.EffectiveConfiguration.Economy,
            snapshot.EffectiveConfiguration.Travel,
            snapshot.EffectiveConfiguration.Crime,
            snapshot.EffectiveConfiguration.GuardCrime,
            snapshot.EffectiveConfiguration.NaturalMortality,
            snapshot.EffectiveConfiguration.AggregateDemography,
            snapshot.EffectiveConfiguration.MerchantTrade,
            snapshot.EffectiveConfiguration.CommercialKnowledge);
        AssertRejected(CopySnapshot(snapshot, effectiveConfiguration: invalidConfiguration,
            replaceEffectiveConfiguration: true));

        List<int> invalidMonthLengths = new List<int> { -1 };
        AssertRejected(CopySnapshot(snapshot, monthLengths: invalidMonthLengths, replaceMonthLengths: true));

        Assert.That(P12CP9GenesisManifestSnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.None,
            source,
            out P12CP9GenesisManifestSnapshot rejectedCapture,
            out string captureDiagnostic), Is.False);
        Assert.That(rejectedCapture, Is.Null);
        Assert.That(captureDiagnostic, Is.Not.Empty);
        AssertManifestMatchesSnapshot(source, snapshot);
    }

    [Test]
    public void RejectsValidCopiedFieldsThatDifferFromRetainedP9Records()
    {
        P12CP9GenesisManifestSnapshot snapshot = Capture(CreateP9BManifest());

        EffectiveSimulationConfiguration changedTravel = new EffectiveSimulationConfiguration(
            snapshot.EffectiveConfiguration.Population,
            snapshot.EffectiveConfiguration.Economy,
            new EffectiveTravelConfiguration(snapshot.EffectiveConfiguration.Travel.TravelCostPerDay + 1f),
            snapshot.EffectiveConfiguration.Crime,
            snapshot.EffectiveConfiguration.GuardCrime,
            snapshot.EffectiveConfiguration.NaturalMortality,
            snapshot.EffectiveConfiguration.AggregateDemography,
            snapshot.EffectiveConfiguration.MerchantTrade,
            snapshot.EffectiveConfiguration.CommercialKnowledge);
        Assert.That(SimulationConfigurationValidator.Validate(changedTravel).IsValid, Is.True);
        AssertRejected(CopySnapshot(snapshot, effectiveConfiguration: changedTravel,
            replaceEffectiveConfiguration: true));

        int changedDaysPerWeek = snapshot.CalendarDaysPerWeek + 1;
        CalendarDefinition changedCalendar = new CalendarDefinition
        {
            monthsPerYear = snapshot.CalendarMonthsPerYear,
            weeksPerMonth = snapshot.CalendarWeeksPerMonth,
            daysPerWeek = changedDaysPerWeek,
            monthLengths = new List<int>(snapshot.MonthLengths)
        };
        Assert.That(changedCalendar.TryValidate(out _), Is.True);
        AssertRejected(CopySnapshot(snapshot, calendarDaysPerWeek: changedDaysPerWeek));

        int changedSeed = snapshot.Seed == int.MaxValue ? int.MaxValue - 1 : snapshot.Seed + 1;
        string changedSeedSource = snapshot.SeedSource == "default-zero" ? "authored-fixed" : snapshot.SeedSource;
        AssertRejected(CopySnapshot(snapshot, seed: changedSeed, seedSource: changedSeedSource));

        List<string> changedIds = new List<string>(snapshot.AuthoredDefinitionIds);
        int hexIdIndex = changedIds.FindIndex(value => value.StartsWith("hex/", StringComparison.Ordinal));
        Assert.That(hexIdIndex, Is.GreaterThanOrEqualTo(0), "P9-B must retain its authored hex ID.");
        changedIds[hexIdIndex] = "hex/changed-but-well-formed";
        changedIds.Sort(StringComparer.Ordinal);
        Assert.That(changedIds.TrueForAll(value => !string.IsNullOrWhiteSpace(value)), Is.True);
        Assert.That(new HashSet<string>(changedIds, StringComparer.Ordinal).Count, Is.EqualTo(changedIds.Count));
        AssertRejected(CopySnapshot(snapshot, authoredDefinitionIds: changedIds,
            replaceAuthoredDefinitionIds: true));
    }

    private SimulationGenesisManifest CreateP9BManifest()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(config, Is.Not.Null);
        Assert.That(config.useAuthoredGeographyProfile, Is.True);
        Assert.That(config.authoredP10RuinSite, Is.Null);

        GameObject gameObject = new GameObject("p12c-p9b-genesis-manifest-snapshot-test");
        simulationObjects.Add(gameObject);
        TesteSimulacao simulation = gameObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();
        Assert.That(simulation.Bootstrap, Is.Not.Null);
        Assert.That(simulation.Bootstrap.Manifest.ContractIdentity,
            Is.EqualTo(SimulationGenesisPipeline.GeographyProfileContractIdentity));
        return simulation.Bootstrap.Manifest;
    }

    private static P12CP9GenesisManifestSnapshot Capture(SimulationGenesisManifest manifest)
    {
        Assert.That(P12CP9GenesisManifestSnapshot.TryCapture(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            manifest,
            out P12CP9GenesisManifestSnapshot snapshot,
            out string diagnostic), Is.True, diagnostic);
        return snapshot;
    }

    private static P12CP9GenesisManifestSnapshot CopySnapshot(
        P12CP9GenesisManifestSnapshot source,
        string snapshotContract = null,
        int? snapshotSchema = null,
        SimulationRuntimeAdmissionProfile? admissionProfile = null,
        string contractIdentity = null,
        int? manifestSchema = null,
        string fingerprint = null,
        string selectedFingerprint = null,
        string selectedContractIdentity = null,
        int? selectedP9Schema = null,
        EffectiveSimulationConfiguration effectiveConfiguration = null,
        bool replaceEffectiveConfiguration = false,
        IReadOnlyList<string> canonicalRecords = null,
        bool replaceCanonicalRecords = false,
        IReadOnlyList<string> authoredDefinitionIds = null,
        bool replaceAuthoredDefinitionIds = false,
        IReadOnlyList<string> outputOwners = null,
        bool replaceOutputOwners = false,
        IReadOnlyList<string> dependencies = null,
        bool replaceDependencies = false,
        int? calendarMonthsPerYear = null,
        int? calendarWeeksPerMonth = null,
        int? calendarDaysPerWeek = null,
        IReadOnlyList<int> monthLengths = null,
        bool replaceMonthLengths = false,
        int? seed = null,
        string seedSource = null,
        IReadOnlyList<string> stageOrder = null,
        bool replaceStageOrder = false,
        string firstBoundary = null)
    {
        return new P12CP9GenesisManifestSnapshot(
            snapshotContract ?? source.SnapshotContract,
            snapshotSchema ?? source.SnapshotSchemaVersion,
            admissionProfile ?? source.P12AdmissionProfile,
            contractIdentity ?? source.ContractIdentity,
            manifestSchema ?? source.SchemaVersion,
            fingerprint ?? source.Fingerprint,
            selectedFingerprint ?? source.SelectedP9ProfileFingerprint,
            selectedContractIdentity ?? source.SelectedP9ContractIdentity,
            selectedP9Schema ?? source.SelectedP9SchemaVersion,
            replaceEffectiveConfiguration ? effectiveConfiguration : source.EffectiveConfiguration,
            replaceCanonicalRecords ? canonicalRecords : source.CanonicalProvenanceRecords,
            replaceAuthoredDefinitionIds ? authoredDefinitionIds : source.AuthoredDefinitionIds,
            replaceOutputOwners ? outputOwners : source.OutputOwners,
            replaceDependencies ? dependencies : source.StageDependencyRecords,
            calendarMonthsPerYear ?? source.CalendarMonthsPerYear,
            calendarWeeksPerMonth ?? source.CalendarWeeksPerMonth,
            calendarDaysPerWeek ?? source.CalendarDaysPerWeek,
            replaceMonthLengths ? monthLengths : source.MonthLengths,
            seed ?? source.Seed,
            seedSource ?? source.SeedSource,
            replaceStageOrder ? stageOrder : source.StageOrder,
            firstBoundary ?? source.FirstSimulatedBoundary);
    }

    private static void AssertRejected(P12CP9GenesisManifestSnapshot snapshot)
    {
        Assert.That(snapshot.TryStageManifest(out SimulationGenesisManifest manifest, out string diagnostic), Is.False);
        Assert.That(manifest, Is.Null);
        Assert.That(diagnostic, Is.Not.Empty);
    }

    private static void AssertNoRecordsWithPrefix(IReadOnlyList<string> records, string prefix)
    {
        for (int i = 0; i < records.Count; i++)
            Assert.That(records[i].StartsWith(prefix, StringComparison.Ordinal), Is.False, records[i]);
    }

    private static bool HasRecordWithPrefixAndText(IReadOnlyList<string> records, string prefix, string text)
    {
        for (int i = 0; i < records.Count; i++)
            if (records[i].StartsWith(prefix, StringComparison.Ordinal)
                && records[i].IndexOf(text, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    private static void AssertManifestMatchesSnapshot(
        SimulationGenesisManifest manifest,
        P12CP9GenesisManifestSnapshot snapshot)
    {
        Assert.That(manifest.ContractIdentity, Is.EqualTo(snapshot.ContractIdentity));
        Assert.That(manifest.SchemaVersion, Is.EqualTo(snapshot.SchemaVersion));
        Assert.That(manifest.Fingerprint, Is.EqualTo(snapshot.Fingerprint));
        Assert.That(manifest.SelectedP9ProfileFingerprint, Is.EqualTo(snapshot.SelectedP9ProfileFingerprint));
        Assert.That(manifest.SelectedP9ContractIdentity, Is.EqualTo(snapshot.SelectedP9ContractIdentity));
        Assert.That(manifest.SelectedP9SchemaVersion, Is.EqualTo(snapshot.SelectedP9SchemaVersion));
        Assert.That(manifest.EffectiveConfiguration, Is.EqualTo(snapshot.EffectiveConfiguration));
        Assert.That(manifest.CanonicalProvenanceRecords, Is.EqualTo(snapshot.CanonicalProvenanceRecords));
        Assert.That(manifest.AuthoredDefinitionIds, Is.EqualTo(snapshot.AuthoredDefinitionIds));
        Assert.That(manifest.OutputOwners, Is.EqualTo(snapshot.OutputOwners));
        Assert.That(manifest.StageDependencyRecords, Is.EqualTo(snapshot.StageDependencyRecords));
        Assert.That(manifest.CalendarMonthsPerYear, Is.EqualTo(snapshot.CalendarMonthsPerYear));
        Assert.That(manifest.CalendarWeeksPerMonth, Is.EqualTo(snapshot.CalendarWeeksPerMonth));
        Assert.That(manifest.CalendarDaysPerWeek, Is.EqualTo(snapshot.CalendarDaysPerWeek));
        Assert.That(manifest.MonthLengths, Is.EqualTo(snapshot.MonthLengths));
        Assert.That(manifest.Seed, Is.EqualTo(snapshot.Seed));
        Assert.That(manifest.SeedSource, Is.EqualTo(snapshot.SeedSource));
        Assert.That(manifest.StageOrder, Is.EqualTo(snapshot.StageOrder));
        Assert.That(manifest.FirstSimulatedBoundary, Is.EqualTo(snapshot.FirstSimulatedBoundary));
    }
}
