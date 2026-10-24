using System;
using System.Collections.Generic;

/// <summary>
/// Unity manifest compatibility adapter for the Core-owned detached P9 value.
/// It retains no independent manifest facts.
/// </summary>
public sealed class P12CP9GenesisManifestSnapshot
{
    public const string SnapshotContractIdentity = "p12c.p9-genesis-manifest-snapshot/v1";
    public const int CurrentSchemaVersion = 1;

    private readonly P12CP9GenesisManifestCoreSnapshot coreValue;

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
        coreValue = new P12CP9GenesisManifestCoreSnapshot(
            snapshotContractIdentity,
            snapshotSchemaVersion,
            p12AdmissionProfile,
            contractIdentity,
            schemaVersion,
            fingerprint,
            selectedP9ProfileFingerprint,
            selectedP9ContractIdentity,
            selectedP9SchemaVersion,
            effectiveConfiguration,
            canonicalProvenanceRecords,
            authoredDefinitionIds,
            outputOwners,
            stageDependencyRecords,
            calendarMonthsPerYear,
            calendarWeeksPerMonth,
            calendarDaysPerWeek,
            monthLengths,
            seed,
            seedSource,
            stageOrder,
            firstSimulatedBoundary);
    }

    public string SnapshotContract => coreValue.SnapshotContract;
    public int SnapshotSchemaVersion => coreValue.SnapshotSchemaVersion;
    public SimulationRuntimeAdmissionProfile P12AdmissionProfile => coreValue.P12AdmissionProfile;
    public string ContractIdentity => coreValue.ContractIdentity;
    public int SchemaVersion => coreValue.SchemaVersion;
    public string Fingerprint => coreValue.Fingerprint;
    public string SelectedP9ProfileFingerprint => coreValue.SelectedP9ProfileFingerprint;
    public string SelectedP9ContractIdentity => coreValue.SelectedP9ContractIdentity;
    public int SelectedP9SchemaVersion => coreValue.SelectedP9SchemaVersion;
    public EffectiveSimulationConfiguration EffectiveConfiguration => coreValue.EffectiveConfiguration;
    public IReadOnlyList<string> CanonicalProvenanceRecords => coreValue.CanonicalProvenanceRecords;
    public IReadOnlyList<string> AuthoredDefinitionIds => coreValue.AuthoredDefinitionIds;
    public IReadOnlyList<string> OutputOwners => coreValue.OutputOwners;
    public IReadOnlyList<string> StageDependencyRecords => coreValue.StageDependencyRecords;
    public int CalendarMonthsPerYear => coreValue.CalendarMonthsPerYear;
    public int CalendarWeeksPerMonth => coreValue.CalendarWeeksPerMonth;
    public int CalendarDaysPerWeek => coreValue.CalendarDaysPerWeek;
    public IReadOnlyList<int> MonthLengths => coreValue.MonthLengths;
    public int Seed => coreValue.Seed;
    public string SeedSource => coreValue.SeedSource;
    public IReadOnlyList<string> StageOrder => coreValue.StageOrder;
    public string FirstSimulatedBoundary => coreValue.FirstSimulatedBoundary;

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

    /// <summary>Stages the retained value as a Unity manifest at this host boundary.</summary>
    internal bool TryStageManifest(out SimulationGenesisManifest manifest, out string diagnostic)
    {
        return SimulationGenesisManifest.TryCreateFromSnapshot(this, out manifest, out diagnostic);
    }

    internal bool TryValidate(out string diagnostic) => coreValue.TryValidate(out diagnostic);

    internal static EffectiveSimulationConfiguration CloneConfiguration(
        EffectiveSimulationConfiguration configuration) =>
        P12CP9GenesisManifestCoreSnapshot.CloneConfiguration(configuration);
}
