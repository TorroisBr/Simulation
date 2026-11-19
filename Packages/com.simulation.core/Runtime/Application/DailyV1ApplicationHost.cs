using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

/// <summary>
/// Opaque host-owned projection retained only so the application can publish
/// or replace the matching host view under the same serialized session gate.
/// It carries no runtime authority or simulation state.
/// </summary>
internal interface IDailyV1ApplicationHostProjection
{
    DailyV1ActiveRuntimeSession CoreRuntimeSession { get; }
    bool TryCreateSelectedTravelPartyStartHostOperation(
        DailyV1SelectedTravelPartyStartPlan plan,
        out IDailyV1SelectedTravelPartyStartHostOperation operation);
}

public enum DailyV1LogSeverity : byte
{
    Log = 0,
    Warning = 1,
    Error = 2
}

/// <summary>Host-captured environment identity for the fixed .NET proving host.</summary>
public sealed class DailyV1HostEnvironmentIdentity
{
    private readonly ReadOnlyCollection<P12AAssemblyIdentityV2> assemblies;

    public DailyV1HostEnvironmentIdentity(ushort hostKind,
        string runtimeFrameworkDescription, string runtimeVersion,
        string operatingSystemDescription, string runtimeIdentifier,
        string processArchitecture, string targetFramework,
        IEnumerable<P12AAssemblyIdentityV2> assemblies,
        string numericProfileId, int numericProfileVersion)
    {
        if (hostKind != 1)
            throw new ArgumentOutOfRangeException(nameof(hostKind), "The Daily-v1 .NET host identity requires HostKind 1.");
        HostKind = hostKind;
        RuntimeFrameworkDescription = NormalizeText(runtimeFrameworkDescription, nameof(runtimeFrameworkDescription));
        RuntimeVersion = CanonicalVersion(runtimeVersion, nameof(runtimeVersion));
        OperatingSystemDescription = NormalizeText(operatingSystemDescription, nameof(operatingSystemDescription));
        RuntimeIdentifier = CanonicalAscii(runtimeIdentifier, nameof(runtimeIdentifier), true);
        ProcessArchitecture = CanonicalAscii(processArchitecture, nameof(processArchitecture), false);
        TargetFramework = NormalizeText(targetFramework, nameof(targetFramework));
        if (!string.Equals(TargetFramework, "net8.0", StringComparison.Ordinal))
            throw new ArgumentException("The selected Daily-v1 host is pinned to net8.0.", nameof(targetFramework));
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));
        List<P12AAssemblyIdentityV2> copy = assemblies.Select(value =>
            value ?? throw new ArgumentException("A host assembly identity is null.", nameof(assemblies))).ToList();
        if (copy.Count < 1 || copy.Count > 4096)
            throw new ArgumentOutOfRangeException(nameof(assemblies));
        copy.Sort(CompareAssembly);
        for (int i = 1; i < copy.Count; i++)
            if (CompareAssembly(copy[i - 1], copy[i]) == 0)
                throw new ArgumentException("The host assembly manifest contains a duplicate identity.", nameof(assemblies));
        this.assemblies = new ReadOnlyCollection<P12AAssemblyIdentityV2>(copy);
        NumericProfileId = Need(numericProfileId, nameof(numericProfileId));
        if (numericProfileVersion <= 0) throw new ArgumentOutOfRangeException(nameof(numericProfileVersion));
        NumericProfileVersion = numericProfileVersion;
    }

    public ushort HostKind { get; }
    public string RuntimeFrameworkDescription { get; }
    public string RuntimeVersion { get; }
    public string OperatingSystemDescription { get; }
    public string RuntimeIdentifier { get; }
    public string ProcessArchitecture { get; }
    public string TargetFramework { get; }
    public IReadOnlyList<P12AAssemblyIdentityV2> Assemblies => assemblies;
    public string NumericProfileId { get; }
    public int NumericProfileVersion { get; }

    private static string Need(string value, string parameter)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A required host identity field is empty.", parameter) : value;

    private static string NormalizeText(string value, string parameter)
        => Need(value, parameter).Trim().Normalize(NormalizationForm.FormC);

    private static string CanonicalVersion(string value, string parameter)
    {
        string[] components = Need(value, parameter).Split('.');
        if (components.Length < 1 || components.Length > 4)
            throw new ArgumentException("The host version must have one to four numeric components.", parameter);
        int[] numbers = new int[4];
        for (int i = 0; i < components.Length; i++)
            if (components[i].Length == 0 || components[i].Any(character => character < '0' || character > '9')
                || !int.TryParse(components[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
                throw new ArgumentException("The host version is not canonical numeric text.", parameter);
        return string.Join(".", numbers.Select(number => number.ToString(CultureInfo.InvariantCulture)));
    }

    private static string CanonicalAscii(string value, string parameter, bool allowHyphens)
    {
        string text = Need(value, parameter).Trim().ToLowerInvariant();
        bool priorHyphen = true;
        foreach (char character in text)
        {
            bool asciiAlphaNumeric = character >= 'a' && character <= 'z'
                || character >= '0' && character <= '9';
            if (asciiAlphaNumeric) { priorHyphen = false; continue; }
            if (!allowHyphens || character != '-' || priorHyphen)
                throw new ArgumentException("The host field must use canonical ASCII text.", parameter);
            priorHyphen = true;
        }
        if (priorHyphen) throw new ArgumentException("The host field must end in an ASCII letter or digit.", parameter);
        return text;
    }

    private static int CompareAssembly(P12AAssemblyIdentityV2 left, P12AAssemblyIdentityV2 right)
    {
        int comparison = StringComparer.Ordinal.Compare(left.SimpleName, right.SimpleName);
        if (comparison != 0) return comparison;
        comparison = Version.Parse(left.Version).CompareTo(Version.Parse(right.Version));
        return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(left.Mvid, right.Mvid);
    }
}

/// <summary>Explicit host ports for one shared Daily-v1 application instance.</summary>
public sealed class DailyV1ApplicationHost
{
    public DailyV1ApplicationHost(
        string persistentDataRoot,
        IAtomicFileSlotOperations fileOperations,
        Action<string> stageCompleted,
        Action<DailyV1LogSeverity, string> logOutput,
        DailyV1HostEnvironmentIdentity dotNetIdentity = null,
        P12ACompatibilityDescriptor unityV1Identity = null)
    {
        if (string.IsNullOrWhiteSpace(persistentDataRoot))
            throw new ArgumentException("An explicit continuation storage root is required.", nameof(persistentDataRoot));
        if (fileOperations == null) throw new ArgumentNullException(nameof(fileOperations));
        if ((dotNetIdentity == null) == (unityV1Identity == null))
            throw new ArgumentException("Exactly one .NET v2 or Unity v1 host identity must be supplied.");

        PersistentDataRoot = persistentDataRoot;
        FileOperations = fileOperations;
        StageCompleted = stageCompleted;
        LogOutput = logOutput;
        DotNetIdentity = dotNetIdentity;
        UnityV1Identity = unityV1Identity;
    }

    internal string PersistentDataRoot { get; }
    internal IAtomicFileSlotOperations FileOperations { get; }
    internal Action<string> StageCompleted { get; }
    internal Action<DailyV1LogSeverity, string> LogOutput { get; }
    internal DailyV1HostEnvironmentIdentity DotNetIdentity { get; }
    internal P12ACompatibilityDescriptor UnityV1Identity { get; }
}

/// <summary>
/// Core-owned host-port composition for the fixed detached runtime. It binds
/// all ports to the exact session graph and deliberately provides no Unity
/// projection or legacy census fallback. The sealed Core census remains the
/// sole selected-profile source for capture and restored-target recapture.
/// </summary>
internal sealed class DailyV1CoreSelectedRuntimeAdvanceHostPorts
    : IDailyV1SelectedRuntimeAdvanceHostPorts
{
    private readonly DailyV1SelectedRuntimeOwnerGraph ownerGraph;
    private readonly SimulationLogger logger;

    private DailyV1CoreSelectedRuntimeAdvanceHostPorts(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        SimulationLogger logger)
    {
        ExecutionState = executionState;
        this.ownerGraph = ownerGraph;
        this.logger = logger;
    }

    public DailyV1ExecutionBoundaryState ExecutionState { get; }
    public object SelectedRuntimeOwnerCensusSourceIdentity => ExecutionState;

    internal static bool TryCreateForApplicationStart(
        DailyV1ApplicationStartPreparation preparation,
        DailyV1ActiveRuntimeSession candidateCoreSession,
        out IDailyV1SelectedRuntimeAdvanceHostPorts candidateHostPorts,
        out string diagnostic)
    {
        candidateHostPorts = null;
        diagnostic = null;
        if (preparation == null || candidateCoreSession == null
            || preparation.Application == null || preparation.Logger == null
            || (preparation.Kind == DailyV1ApplicationStartPreparationKind.GenesisPrepared
                && !ReferenceEquals(preparation.CoreSession, candidateCoreSession))
            || (preparation.Kind == DailyV1ApplicationStartPreparationKind.RestoreRequired
                && preparation.CoreSession != null))
        {
            diagnostic = "The Core runtime ports do not match the exact initial application preparation.";
            return false;
        }

        if (!TryCreate(candidateCoreSession, preparation.Logger,
                out candidateHostPorts, out diagnostic))
            return false;

        if (!DailyV1SelectedActorTurnExecutionContextFactory.TryValidateCoreOnlyStart(
                candidateCoreSession.ExecutionBoundaryState,
                candidateCoreSession.OwnerGraph,
                out diagnostic))
        {
            candidateHostPorts = null;
            if (string.IsNullOrWhiteSpace(diagnostic))
                diagnostic = "The prepared selected profile cannot advance on the detached Core-only path.";
            return false;
        }
        return true;
    }

    internal static bool TryCreate(
        DailyV1ActiveRuntimeSession session,
        SimulationLogger logger,
        out IDailyV1SelectedRuntimeAdvanceHostPorts candidateHostPorts,
        out string diagnostic)
    {
        candidateHostPorts = null;
        diagnostic = null;
        DailyV1ExecutionBoundaryState executionState = session?.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = session?.OwnerGraph;
        if (session == null || logger == null || executionState == null || graph == null
            || !executionState.HasAdmittedSelectedDailyExecution
            || !executionState.SelectedRuntimeExecutionRootsBound
            || !executionState.HasRuntimeProtocolOperationInventory
            || !executionState.HasSealedSelectedOwnerSectionCensusRegistry
            || executionState.IsSelectedRuntimeCensusProtocolFaulted
            || executionState.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !executionState.AdmissionContext.IsOwnedByCurrentThread()
            || !ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, graph)
            || !ReferenceEquals(graph.Definitions, session.Definitions)
            || !graph.MatchesExactOwnerInstances(graph.Cities, graph.Npcs))
        {
            diagnostic = "The detached Core host requires the exact admitted session and sealed selected owner census.";
            return false;
        }

        candidateHostPorts = new DailyV1CoreSelectedRuntimeAdvanceHostPorts(
            executionState, graph, logger);
        return true;
    }

    public bool TryGetExactJusticeRecordsCensusProjectionIdentity(
        DailyV1JusticeRecordsOwner owner,
        out object projectionIdentity,
        out long p18ReceiptRevision)
    {
        projectionIdentity = null;
        p18ReceiptRevision = 0L;
        if (!IsBoundToCurrentSession() || owner == null
            || !ReferenceEquals(owner, ownerGraph.JusticeRecordsOwner))
            return false;

        // P18 has no detached receipt owner in this accepted P12 profile. Its
        // established census witness is the exact-zero sentinel, while the
        // opaque identity remains the canonical P12 Justice projection key.
        projectionIdentity = owner;
        return true;
    }

    public bool TryCreateSelectedDailyExecutionHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDailyExecutionHostPorts hostPorts)
    {
        hostPorts = null;
        if (!IsBoundTo(graph)) return false;
        hostPorts = new DailyV1CoreSelectedDailyExecutionHostPorts(
            ExecutionState, ownerGraph, logger);
        return true;
    }

    public bool TryCaptureOwnerSectionCensus(
        DailyV1CaptureScopeAllowance allowedOuterScope,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure)
    {
        ownerSections = null;
        mutationEpoch = 0L;
        // Selected Daily-v1 must use the sealed Core registry, never a second
        // host-composed owner vector.
        failure = DailyCaptureEligibilityFailure.UnsupportedProfile;
        return false;
    }

    public bool TryBindRestoredCandidateMutationGuard(out string diagnostic)
    {
        diagnostic = null;
        if (!IsBoundToCurrentSession()
            || !ExecutionState.CoreMutationGuardAuthoritiesBound)
        {
            diagnostic = "The restored Core owner mutation authorities were not bound to this exact graph.";
            return false;
        }
        return true;
    }

    public bool TryCaptureUnadmittedRestoredDailyOwnerVector(
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out string diagnostic)
    {
        ownerSections = null;
        mutationEpoch = 0L;
        diagnostic = "Selected Daily-v1 restored recapture is owned by the sealed Core census registry.";
        return false;
    }

    private bool IsBoundTo(DailyV1SelectedRuntimeOwnerGraph graph) =>
        IsBoundToCurrentSession() && ReferenceEquals(ownerGraph, graph)
        && ReferenceEquals(ExecutionState.SelectedRuntimeOwnerGraph, graph);

    private bool IsBoundToCurrentSession() =>
        ExecutionState != null && ownerGraph != null
        && ExecutionState.HasAdmittedSelectedDailyExecution
        && ExecutionState.SelectedRuntimeExecutionRootsBound
        && ExecutionState.HasRuntimeProtocolOperationInventory
        && ExecutionState.HasSealedSelectedOwnerSectionCensusRegistry
        && !ExecutionState.IsSelectedRuntimeCensusProtocolFaulted
        && ExecutionState.AdmissionContext?.Profile
            == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
        && ExecutionState.AdmissionContext.IsOwnedByCurrentThread()
        && ReferenceEquals(ExecutionState.SelectedRuntimeOwnerGraph, ownerGraph);
}

/// <summary>
/// Host effects for an admitted detached Core session. All selected domain
/// execution remains in Core; this adapter supplies only deterministic
/// diagnostics and the no-projection P12 travel/death boundaries. Mixed or
/// legacy execution cannot silently fall back to Unity from this host.
/// </summary>
internal sealed class DailyV1CoreSelectedDailyExecutionHostPorts
    : IDailyV1SelectedDailyExecutionHostPorts
{
    private readonly DailyV1ExecutionBoundaryState executionState;
    private readonly DailyV1SelectedRuntimeOwnerGraph ownerGraph;
    private readonly SimulationLogger logger;

    internal DailyV1CoreSelectedDailyExecutionHostPorts(
        DailyV1ExecutionBoundaryState executionState,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph,
        SimulationLogger logger)
    {
        this.executionState = executionState ?? throw new ArgumentNullException(nameof(executionState));
        this.ownerGraph = ownerGraph ?? throw new ArgumentNullException(nameof(ownerGraph));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void BeginLoggerDay(long absoluteDay)
    {
        EnsureBound();
        logger.BeginDay(absoluteDay);
    }

    public bool HasExactCrimeJusticeProjectionOwnerBindings(
        DailyV1SelectedRuntimeOwnerGraph graph) =>
        IsBoundTo(graph)
        && executionState.TryBindSelectedCrimeJusticeDailyMutationBindingFromCoreCensus(graph);

    public bool TryCreateSelectedDemographyHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDemographyHostPorts hostPorts)
    {
        hostPorts = null;
        if (!IsBoundTo(graph) || graph.PersonStore == null
            || executionState.CalendarIdentity == null)
            return false;
        hostPorts = new DailyV1CoreSelectedDemographyHostPorts(
            null,
            null,
            new DailyV1CoreSelectedPersonDeathBoundary(
                executionState, ownerGraph, projection: null));
        return true;
    }

    public void PublishSelectedDemographyResult(DailyV1SelectedDemographyResult result)
    {
        EnsureBound();
        // The Core PersonStore and population owners are authoritative. The
        // result has no detached projection to publish.
    }

    public void BeginSimulationDay() =>
        throw new InvalidOperationException(
            "The detached Core host does not admit the Unity mixed-roster whole-stage fallback.");

    public void EnsureSelectedEconomyProjectionCanRun(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        EnsureBoundTo(graph);
        if (executionState.CalendarIdentity == null
            && graph.Cities.Any(city => city?.HasLocalDailyMaterialFlow == true))
            throw new InvalidOperationException(
                "The Core economy requires the admitted immutable calendar for local material flow.");
    }

    public void LogSelectedDailyEconomy(
        DailyV1CityRuntimeOwners owner,
        SimulationLogCategory category,
        string message)
    {
        EnsureBound();
        if (owner == null || !ownerGraph.ContainsExactCityOwner(owner))
            throw new InvalidOperationException(
                "The Core economy diagnostic received a City outside the admitted owner graph.");
        logger.Log(category, message);
    }

    public void EnsureSelectedLocalKnowledgeProjectionCanRun(
        DailyV1SelectedRuntimeOwnerGraph graph,
        bool merchantTradeEnabled)
    {
        EnsureBoundTo(graph);
        if (ownerGraph.Cities == null || ownerGraph.Npcs == null
            || !ownerGraph.MatchesExactOwnerInstances(ownerGraph.Cities, ownerGraph.Npcs)
            || merchantTradeEnabled
                != (ownerGraph.Definitions.EffectiveConfiguration.MerchantTrade?.Enabled == true))
            throw new InvalidOperationException(
                "The detached local-knowledge stage does not match the admitted Core owner graph and configuration.");
    }

    public bool TryCreateSelectedActorTurnHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedActorTurnHostPorts hostPorts)
    {
        // The actor-turn factory will attempt its explicitly bounded Core-only
        // path. Unsupported actions, actor choices, or mixed rosters reject
        // before mutation instead of delegating to a Unity adapter.
        hostPorts = null;
        return false;
    }

    public bool TryCreateSelectedTravelDayHostPorts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedTravelDayHostPorts hostPorts)
    {
        hostPorts = null;
        return IsBoundTo(graph)
            && DailyV1CoreSelectedTravelDayHostPorts.TryCreate(
                executionState, ownerGraph, out DailyV1CoreSelectedTravelDayHostPorts corePorts)
            && (hostPorts = corePorts) != null;
    }

    private bool IsBoundTo(DailyV1SelectedRuntimeOwnerGraph graph) =>
        ReferenceEquals(ownerGraph, graph)
        && executionState.HasAdmittedSelectedDailyExecution
        && executionState.SelectedRuntimeExecutionRootsBound
        && executionState.HasRuntimeProtocolOperationInventory
        && executionState.HasSealedSelectedOwnerSectionCensusRegistry
        && !executionState.IsSelectedRuntimeCensusProtocolFaulted
        && executionState.AdmissionContext?.Profile
            == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
        && executionState.AdmissionContext.IsOwnedByCurrentThread()
        && ReferenceEquals(executionState.SelectedRuntimeOwnerGraph, ownerGraph)
        && ReferenceEquals(ownerGraph.Definitions, executionState.SelectedRuntimeOwnerGraph.Definitions);

    private void EnsureBound()
    {
        if (!IsBoundTo(ownerGraph))
            throw new InvalidOperationException(
                "The detached daily execution ports are no longer bound to their exact Core session.");
    }

    private void EnsureBoundTo(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (!IsBoundTo(graph))
            throw new InvalidOperationException(
                "The detached daily execution ports received a different Core owner graph.");
    }
}
