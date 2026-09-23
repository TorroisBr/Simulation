using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

public enum ManpowerSourceEffectKind
{
    Death = 0
}

/// <summary>An aggregate source effect. Amount is intentionally wide until a domain adapter validates its range.</summary>
public sealed class ManpowerSourceEffect
{
    public ManpowerSourceEffectKind Kind { get; }
    public long Amount { get; }

    public ManpowerSourceEffect(ManpowerSourceEffectKind kind, long amount)
    {
        Kind = kind;
        Amount = amount;
    }
}

public sealed class ManpowerSourceConsequenceRequest
{
    public ManpowerSourceId SourceId { get; }
    public ManpowerSourceEffect Effect { get; }

    public ManpowerSourceConsequenceRequest(ManpowerSourceId sourceId, ManpowerSourceEffect effect)
    {
        SourceId = sourceId;
        Effect = effect;
    }
}

/// <summary>Stable identity/configuration metadata captured when a source planner is composed.</summary>
public interface IManpowerSourceConsequencePlannerIdentity
{
    string RuleKey { get; }
    string ConfigurationIdentity { get; }
    int Version { get; }
}

public enum ManpowerSourceConsequenceDisposition
{
    DomainTransitionRequired = 0,
    NoExternalMutationRequired = 1
}

public enum ManpowerSourceConsequencePlanningStatus
{
    Planned = 0,
    Unsupported = 1,
    Failed = 2
}

public enum ManpowerSourceConsequenceFailureCode
{
    None = 0,
    InvalidRequest = 1,
    InvalidSourceId = 2,
    InvalidEffect = 3,
    InvalidAmount = 4,
    SourceNotRegistered = 5,
    PlannerNotConfigured = 6,
    UnsupportedEffect = 7,
    AmountNotRepresentable = 8,
    AggregateDemographyRejected = 9,
    InvalidPlannerOutput = 10,
    PlannerIdentityChanged = 11,
    SettlementNotInWorld = 12,
    InvalidWorldPopulationState = 13,
    InvalidProposal = 14
}

public sealed class ManpowerSourceConsequenceFailure
{
    public ManpowerSourceConsequenceFailureCode Code { get; }
    public AggregateDemographyFailure AggregateFailure { get; }
    public string Message { get; }
    public bool IsFailure => Code != ManpowerSourceConsequenceFailureCode.None;

    internal ManpowerSourceConsequenceFailure(
        ManpowerSourceConsequenceFailureCode code,
        AggregateDemographyFailure aggregateFailure,
        string message)
    {
        Code = code;
        AggregateFailure = aggregateFailure;
        Message = message ?? string.Empty;
    }

    public override string ToString()
    {
        return Code + (AggregateFailure == AggregateDemographyFailure.None
            ? string.Empty
            : ":" + AggregateFailure)
            + (Message.Length == 0 ? string.Empty : ": " + Message);
    }
}

public sealed class ManpowerSourceConsequencePlanningResult
{
    public ManpowerSourceConsequencePlanningStatus Status { get; }
    public ManpowerSourceConsequenceProposal Proposal { get; }
    public ManpowerSourceConsequenceFailure Failure { get; }
    public bool IsPlanned => Status == ManpowerSourceConsequencePlanningStatus.Planned;

    internal ManpowerSourceConsequencePlanningResult(
        ManpowerSourceConsequencePlanningStatus status,
        ManpowerSourceConsequenceProposal proposal,
        ManpowerSourceConsequenceFailure failure)
    {
        Status = status;
        Proposal = proposal;
        Failure = failure;
    }
}

public sealed class SettlementPopulationDeathSourceProposal
{
    public ManpowerSourceId SourceId { get; }
    public string SettlementRuntimeId { get; }
    public long ExpectedPopulationRevision { get; }
    public int PopulationBefore { get; }
    public int RepresentedResidentFloor { get; }
    public int Deaths { get; }
    public int PopulationAfter { get; }
    public AggregateDemographyTransition Transition { get; }

    internal SettlementPopulationDeathSourceProposal(
        ManpowerSourceId sourceId,
        AggregateDemographyTransition transition)
    {
        SourceId = sourceId;
        Transition = transition;
        SettlementRuntimeId = transition.SettlementRuntimeId;
        ExpectedPopulationRevision = transition.ExpectedPopulationRevision;
        PopulationBefore = transition.PopulationBefore;
        RepresentedResidentFloor = transition.RepresentedResidentFloor;
        Deaths = transition.Deaths;
        PopulationAfter = transition.PopulationAfter;
    }
}

/// <summary>
/// Immutable, non-applying consequence plan. Domain payloads are explicit typed
/// wrappers; this foundation does not carry arbitrary object payloads.
/// </summary>
public sealed class ManpowerSourceConsequenceProposal
{
    public ManpowerSourceId SourceId { get; }
    public ManpowerSourceEffect Effect { get; }
    public ManpowerSourceConsequenceDisposition Disposition { get; }
    public string PlannerRuleKey { get; }
    public string PlannerConfigurationIdentity { get; }
    public int ProposalVersion { get; }
    public string DependencyFingerprint { get; }
    public string SettlementRuntimeId { get; }
    public long ExpectedPopulationRevision { get; }
    public int PopulationBefore { get; }
    public int RepresentedResidentFloor { get; }
    public SettlementPopulationDeathSourceProposal SettlementPopulationDeath { get; }

    internal SettlementManpowerSourceBinding RegistrationIdentity { get; }

    internal ManpowerSourceConsequenceProposal(
        ManpowerSourceId sourceId,
        ManpowerSourceEffect effect,
        ManpowerSourceConsequenceDisposition disposition,
        string plannerRuleKey,
        string plannerConfigurationIdentity,
        int proposalVersion,
        string dependencyFingerprint,
        string settlementRuntimeId,
        long expectedPopulationRevision,
        int populationBefore,
        int representedResidentFloor,
        SettlementPopulationDeathSourceProposal settlementPopulationDeath,
        SettlementManpowerSourceBinding registrationIdentity)
    {
        SourceId = sourceId;
        Effect = effect;
        Disposition = disposition;
        PlannerRuleKey = plannerRuleKey;
        PlannerConfigurationIdentity = plannerConfigurationIdentity;
        ProposalVersion = proposalVersion;
        DependencyFingerprint = dependencyFingerprint;
        SettlementRuntimeId = settlementRuntimeId;
        ExpectedPopulationRevision = expectedPopulationRevision;
        PopulationBefore = populationBefore;
        RepresentedResidentFloor = representedResidentFloor;
        SettlementPopulationDeath = settlementPopulationDeath;
        RegistrationIdentity = registrationIdentity;
    }
}

public enum ManpowerSourceConsequenceValidationStatus
{
    Current = 0,
    Stale = 1,
    Invalid = 2
}

public enum ManpowerSourceConsequenceValidationReason
{
    None = 0,
    InvalidProposal = 1,
    SourceRegistrationChanged = 2,
    SettlementNotInWorld = 3,
    PlannerIdentityChanged = 4,
    InvalidTransitionShape = 5,
    SettlementPopulationChanged = 6,
    RepresentedResidentFloorChanged = 7,
    DependencyFingerprintChanged = 8
}

public sealed class ManpowerSourceConsequenceValidationResult
{
    public ManpowerSourceConsequenceValidationStatus Status { get; }
    public ManpowerSourceConsequenceValidationReason Reason { get; }
    public bool IsCurrent => Status == ManpowerSourceConsequenceValidationStatus.Current;

    internal ManpowerSourceConsequenceValidationResult(
        ManpowerSourceConsequenceValidationStatus status,
        ManpowerSourceConsequenceValidationReason reason)
    {
        Status = status;
        Reason = reason;
    }
}

/// <summary>Explicit association between a stable source identity and a settlement adapter.</summary>
public sealed class SettlementManpowerSourceRegistration
{
    public ManpowerSourceId SourceId { get; }
    public CityRuntime Settlement { get; }
    public long MilitaryCapacity { get; }
    public IManpowerSourceConsequencePlannerIdentity PlannerIdentity { get; }

    public SettlementManpowerSourceRegistration(
        ManpowerSourceId sourceId,
        CityRuntime settlement,
        long militaryCapacity)
        : this(sourceId, settlement, militaryCapacity, null)
    {
    }

    public SettlementManpowerSourceRegistration(
        ManpowerSourceId sourceId,
        CityRuntime settlement,
        long militaryCapacity,
        IManpowerSourceConsequencePlannerIdentity plannerIdentity)
    {
        SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        Settlement = settlement ?? throw new ArgumentNullException(nameof(settlement));
        if (militaryCapacity < 0L)
            throw new ArgumentOutOfRangeException(nameof(militaryCapacity), "Military capacity cannot be negative.");
        if (plannerIdentity != null
            && (string.IsNullOrWhiteSpace(plannerIdentity.RuleKey)
                || string.IsNullOrWhiteSpace(plannerIdentity.ConfigurationIdentity)
                || plannerIdentity.Version <= 0))
        {
            throw new ArgumentException("Planner identity requires a RuleKey, configuration identity, and positive version.", nameof(plannerIdentity));
        }
        MilitaryCapacity = militaryCapacity;
        PlannerIdentity = plannerIdentity;
    }
}

internal sealed class SettlementPopulationDeathConsequencePlanner : IManpowerSourceConsequencePlannerIdentity
{
    internal const string RuleKeyValue = "settlement.aggregate-population-death";
    internal const string ConfigurationIdentityValue = "deaths:int32;represented-resident-floor:v1";
    internal const int VersionValue = 1;

    public string RuleKey => RuleKeyValue;
    public string ConfigurationIdentity => ConfigurationIdentityValue;
    public int Version => VersionValue;

    internal bool TryPlan(
        SettlementPopulationRuntime population,
        int representedResidentFloor,
        long amount,
        out AggregateDemographyTransition transition,
        out AggregateDemographyFailure failure)
    {
        transition = null;
        failure = AggregateDemographyFailure.None;
        if (amount <= 0L)
        {
            failure = AggregateDemographyFailure.NegativeChange;
            return false;
        }
        if (amount > int.MaxValue)
        {
            failure = AggregateDemographyFailure.WouldOverflow;
            return false;
        }

        return AggregateDemographySystem.TryPropose(
            population,
            representedResidentFloor,
            new AggregateDemographyChange(0, checked((int)amount)),
            out transition,
            out failure);
    }
}

internal sealed class SettlementManpowerSourceBinding
{
    internal ManpowerSourceId SourceId { get; }
    internal CityRuntime Settlement { get; }
    internal SettlementPopulationRuntime Population { get; }
    internal long MilitaryCapacity { get; }
    internal SettlementPopulationDeathConsequencePlanner Planner { get; }
    internal IManpowerSourceConsequencePlannerIdentity PlannerIdentity { get; }
    internal string PlannerRuleKeyAtComposition { get; }
    internal string PlannerConfigurationIdentityAtComposition { get; }
    internal int PlannerVersionAtComposition { get; }

    internal SettlementManpowerSourceBinding(SettlementManpowerSourceRegistration registration)
    {
        SourceId = registration.SourceId;
        Settlement = registration.Settlement;
        Population = registration.Settlement.Population;
        MilitaryCapacity = registration.MilitaryCapacity;
        Planner = new SettlementPopulationDeathConsequencePlanner();
        PlannerIdentity = registration.PlannerIdentity ?? Planner;
        PlannerRuleKeyAtComposition = PlannerIdentity.RuleKey;
        PlannerConfigurationIdentityAtComposition = PlannerIdentity.ConfigurationIdentity;
        PlannerVersionAtComposition = PlannerIdentity.Version;
    }
}

/// <summary>
/// World-composed registry for explicit settlement manpower sources. It is both
/// the D6A snapshot provider and the source-to-planner authority for D6B1.
/// </summary>
public sealed class SettlementManpowerSourceRegistry : IManpowerSourceSnapshotProvider
{
    private readonly Dictionary<string, SettlementManpowerSourceBinding> bindings;
    private readonly object worldCompositionIdentity;

    private SettlementManpowerSourceRegistry(
        Dictionary<string, SettlementManpowerSourceBinding> bindings,
        object worldCompositionIdentity)
    {
        this.bindings = bindings;
        this.worldCompositionIdentity = worldCompositionIdentity;
    }

    internal static bool TryCreate(
        IEnumerable<SettlementManpowerSourceRegistration> registrations,
        IReadOnlyList<CityRuntime> worldCities,
        object worldCompositionIdentity,
        out SettlementManpowerSourceRegistry registry,
        out string failure)
    {
        registry = null;
        failure = string.Empty;
        if (worldCompositionIdentity == null)
        {
            failure = "A settlement manpower source registry requires a world composition identity.";
            return false;
        }
        if (registrations == null)
            return true;

        List<SettlementManpowerSourceRegistration> ordered =
            new List<SettlementManpowerSourceRegistration>(registrations);
        if (ordered.Count == 0)
            return true;
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.SourceId?.Value ?? string.Empty,
            right?.SourceId?.Value ?? string.Empty));

        Dictionary<string, SettlementManpowerSourceBinding> values =
            new Dictionary<string, SettlementManpowerSourceBinding>(StringComparer.Ordinal);
        foreach (SettlementManpowerSourceRegistration registration in ordered)
        {
            if (registration == null || registration.SourceId == null || registration.Settlement == null)
            {
                failure = "A settlement manpower source registration is invalid.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(registration.Settlement.RuntimeId))
            {
                failure = "A settlement manpower source requires a stable settlement RuntimeId.";
                return false;
            }
            if (!ContainsExactCity(worldCities, registration.Settlement))
            {
                failure = "A settlement manpower source must target the exact CityRuntime composed by this world.";
                return false;
            }
            if (values.ContainsKey(registration.SourceId.Value))
            {
                failure = "Duplicate ManpowerSourceId registration: " + registration.SourceId.Value + ".";
                return false;
            }

            foreach (SettlementManpowerSourceBinding existing in values.Values)
            {
                if (ReferenceEquals(existing.Population, registration.Settlement.Population)
                    || ReferenceEquals(existing.Settlement, registration.Settlement)
                    || string.Equals(
                        existing.Settlement.RuntimeId,
                        registration.Settlement.RuntimeId,
                        StringComparison.Ordinal))
                {
                    failure = "Multiple ManpowerSourceIds cannot map to the same settlement population authority.";
                    return false;
                }
            }

            values.Add(registration.SourceId.Value, new SettlementManpowerSourceBinding(registration));
        }

        registry = new SettlementManpowerSourceRegistry(values, worldCompositionIdentity);
        return true;
    }

    public bool TryGetSnapshot(ManpowerSourceId sourceId, out ManpowerSourceCapacitySnapshot snapshot)
    {
        snapshot = null;
        if (sourceId == null || !bindings.TryGetValue(sourceId.Value, out SettlementManpowerSourceBinding binding))
            return false;

        SettlementPopulationRuntime population = binding.Population;
        if (population == null
            || !ReferenceEquals(binding.Settlement.Population, population)
            || !string.Equals(
                population.SettlementRuntimeId,
                binding.Settlement.RuntimeId,
                StringComparison.Ordinal))
        {
            return false;
        }

        long factualLivingAmount = population.CurrentPopulation;
        string fingerprint = ManpowerSourceFingerprint.BuildSnapshotFingerprint(
            binding.SourceId,
            binding.Settlement.RuntimeId,
            binding.MilitaryCapacity,
            population.Revision,
            factualLivingAmount);
        snapshot = new ManpowerSourceCapacitySnapshot(
            binding.SourceId,
            binding.MilitaryCapacity,
            factualLivingAmount,
            fingerprint);
        return true;
    }

    internal bool TryGetBinding(ManpowerSourceId sourceId, out SettlementManpowerSourceBinding binding)
    {
        binding = null;
        return sourceId != null && bindings.TryGetValue(sourceId.Value, out binding);
    }

    internal bool IsComposedFor(object identity)
    {
        return identity != null && ReferenceEquals(worldCompositionIdentity, identity);
    }

    internal static bool ContainsExactCity(IReadOnlyList<CityRuntime> cities, CityRuntime city)
    {
        if (cities == null || city == null)
            return false;
        for (int i = 0; i < cities.Count; i++)
            if (ReferenceEquals(cities[i], city))
                return true;
        return false;
    }
}

internal static class ManpowerSourceFingerprint
{
    internal static string BuildSnapshotFingerprint(
        ManpowerSourceId sourceId,
        string settlementRuntimeId,
        long capacity,
        long populationRevision,
        long factualLivingAmount)
    {
        StringBuilder builder = new StringBuilder();
        Append(builder, sourceId?.Value);
        Append(builder, settlementRuntimeId);
        Append(builder, capacity.ToString(CultureInfo.InvariantCulture));
        Append(builder, populationRevision.ToString(CultureInfo.InvariantCulture));
        Append(builder, factualLivingAmount.ToString(CultureInfo.InvariantCulture));
        return Hash(builder.ToString());
    }

    internal static string BuildConsequenceFingerprint(
        ManpowerSourceId sourceId,
        string settlementRuntimeId,
        long populationRevision,
        int currentPopulation,
        int representedResidentFloor,
        string plannerRuleKey,
        string plannerConfigurationIdentity,
        int proposalVersion,
        ManpowerSourceEffectKind effectKind,
        long effectAmount)
    {
        StringBuilder builder = new StringBuilder();
        Append(builder, sourceId?.Value);
        Append(builder, settlementRuntimeId);
        Append(builder, populationRevision.ToString(CultureInfo.InvariantCulture));
        Append(builder, currentPopulation.ToString(CultureInfo.InvariantCulture));
        Append(builder, representedResidentFloor.ToString(CultureInfo.InvariantCulture));
        Append(builder, plannerRuleKey);
        Append(builder, plannerConfigurationIdentity);
        Append(builder, proposalVersion.ToString(CultureInfo.InvariantCulture));
        Append(builder, ((int)effectKind).ToString(CultureInfo.InvariantCulture));
        Append(builder, effectAmount.ToString(CultureInfo.InvariantCulture));
        return Hash(builder.ToString());
    }

    private static void Append(StringBuilder builder, string value)
    {
        if (value == null)
        {
            builder.Append("-1:");
            return;
        }
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(value);
    }

    private static string Hash(string value)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            StringBuilder result = new StringBuilder(digest.Length * 2);
            for (int i = 0; i < digest.Length; i++)
                result.Append(digest[i].ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }
    }
}

/// <summary>Pure D6B1 planning and freshness checks for sources explicitly composed by a SimulationRuntime.</summary>
public sealed class ManpowerSourceConsequencePlanningService
{
    private readonly SimulationRuntime world;
    private readonly SettlementManpowerSourceRegistry registry;
    private readonly IManpowerSourceSnapshotProvider sourceSnapshotProvider;

    internal ManpowerSourceConsequencePlanningService(
        SimulationRuntime world,
        SettlementManpowerSourceRegistry registry,
        IManpowerSourceSnapshotProvider sourceSnapshotProvider)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.registry = registry;
        this.sourceSnapshotProvider = sourceSnapshotProvider;
    }

    public ManpowerSourceConsequencePlanningResult TryPlan(
        ManpowerSourceConsequenceRequest request)
    {
        if (request == null)
            return Failed(ManpowerSourceConsequenceFailureCode.InvalidRequest, "A consequence request is required.");
        if (request.SourceId == null)
            return Failed(ManpowerSourceConsequenceFailureCode.InvalidSourceId, "A stable ManpowerSourceId is required.");
        if (request.Effect == null)
            return Failed(ManpowerSourceConsequenceFailureCode.InvalidEffect, "A source effect is required.");
        if (!Enum.IsDefined(typeof(ManpowerSourceEffectKind), request.Effect.Kind))
            return Unsupported(ManpowerSourceConsequenceFailureCode.UnsupportedEffect, "The requested source effect is unsupported.");
        if (request.Effect.Kind != ManpowerSourceEffectKind.Death)
            return Unsupported(ManpowerSourceConsequenceFailureCode.UnsupportedEffect, "Only aggregate Death effects are supported by this adapter.");
        if (request.Effect.Amount <= 0L)
            return Failed(ManpowerSourceConsequenceFailureCode.InvalidAmount, "A Death effect amount must be positive.");

        if (registry == null || !registry.TryGetBinding(request.SourceId, out SettlementManpowerSourceBinding binding))
        {
            if (sourceSnapshotProvider != null
                && sourceSnapshotProvider.TryGetSnapshot(request.SourceId, out ManpowerSourceCapacitySnapshot snapshot)
                && snapshot != null
                && snapshot.SourceId == request.SourceId)
            {
                return Unsupported(
                    ManpowerSourceConsequenceFailureCode.PlannerNotConfigured,
                    "A D6A source snapshot exists, but no D6B1 consequence planner is registered.");
            }
            return Unsupported(
                ManpowerSourceConsequenceFailureCode.SourceNotRegistered,
                "No explicit source-to-settlement consequence registration exists.");
        }

        if (!world.TryGetManpowerSourceSettlementContext(
                binding.Settlement,
                binding.Population,
                out int representedResidentFloor))
        {
            return Failed(
                ManpowerSourceConsequenceFailureCode.SettlementNotInWorld,
                "The registered settlement is not the exact live settlement authority of this world.");
        }

        SettlementPopulationRuntime population = binding.Population;
        long expectedRevision = population.Revision;
        int populationBefore = population.CurrentPopulation;
        if (representedResidentFloor < 0 || representedResidentFloor > populationBefore)
        {
            return Failed(
                ManpowerSourceConsequenceFailureCode.InvalidWorldPopulationState,
                "The represented-resident floor is outside current settlement population.");
        }

        string ruleKeyBefore = binding.PlannerRuleKeyAtComposition;
        string configurationBefore = binding.PlannerConfigurationIdentityAtComposition;
        int versionBefore = binding.PlannerVersionAtComposition;
        if (!PlannerIdentityMatches(binding.PlannerIdentity, ruleKeyBefore, configurationBefore, versionBefore))
            return Failed(ManpowerSourceConsequenceFailureCode.PlannerIdentityChanged, "Planner identity changed before planning.");

        bool plannerSucceeded = binding.Planner.TryPlan(
            population,
            representedResidentFloor,
            request.Effect.Amount,
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure aggregateFailure);
        if (!PlannerIdentityMatches(binding.PlannerIdentity, ruleKeyBefore, configurationBefore, versionBefore))
            return Failed(ManpowerSourceConsequenceFailureCode.PlannerIdentityChanged, "Planner identity changed during planning.");
        if (population.Revision != expectedRevision || population.CurrentPopulation != populationBefore)
            return Failed(ManpowerSourceConsequenceFailureCode.InvalidPlannerOutput, "Planning changed settlement population state.");

        if (!plannerSucceeded)
        {
            if (aggregateFailure == AggregateDemographyFailure.WouldOverflow
                && request.Effect.Amount > int.MaxValue)
            {
                return Failed(
                    ManpowerSourceConsequenceFailureCode.AmountNotRepresentable,
                    "The aggregate settlement adapter accepts Death amounts only through Int32.MaxValue.");
            }
            return new ManpowerSourceConsequencePlanningResult(
                ManpowerSourceConsequencePlanningStatus.Failed,
                null,
                new ManpowerSourceConsequenceFailure(
                    ManpowerSourceConsequenceFailureCode.AggregateDemographyRejected,
                    aggregateFailure,
                    "Aggregate demography rejected the proposed Death transition."));
        }

        if (!IsValidTransition(binding, request.Effect.Amount, expectedRevision, populationBefore, representedResidentFloor, transition))
            return Failed(ManpowerSourceConsequenceFailureCode.InvalidPlannerOutput, "The settlement planner returned a transition inconsistent with its captured inputs.");

        string dependencyFingerprint = ManpowerSourceFingerprint.BuildConsequenceFingerprint(
            request.SourceId,
            binding.Settlement.RuntimeId,
            expectedRevision,
            populationBefore,
            representedResidentFloor,
            ruleKeyBefore,
            configurationBefore,
            versionBefore,
            request.Effect.Kind,
            request.Effect.Amount);
        SettlementPopulationDeathSourceProposal typedProposal =
            new SettlementPopulationDeathSourceProposal(request.SourceId, transition);
        ManpowerSourceConsequenceProposal proposal = new ManpowerSourceConsequenceProposal(
            request.SourceId,
            request.Effect,
            ManpowerSourceConsequenceDisposition.DomainTransitionRequired,
            ruleKeyBefore,
            configurationBefore,
            versionBefore,
            dependencyFingerprint,
            binding.Settlement.RuntimeId,
            expectedRevision,
            populationBefore,
            representedResidentFloor,
            typedProposal,
            binding);
        return new ManpowerSourceConsequencePlanningResult(
            ManpowerSourceConsequencePlanningStatus.Planned,
            proposal,
            new ManpowerSourceConsequenceFailure(
                ManpowerSourceConsequenceFailureCode.None,
                AggregateDemographyFailure.None,
                string.Empty));
    }

    public ManpowerSourceConsequenceValidationResult TryValidateCurrent(
        ManpowerSourceConsequenceProposal proposal)
    {
        if (proposal == null || proposal.SourceId == null || proposal.Effect == null
            || string.IsNullOrWhiteSpace(proposal.DependencyFingerprint))
        {
            return Invalid(ManpowerSourceConsequenceValidationReason.InvalidProposal);
        }
        if (registry == null
            || !registry.TryGetBinding(proposal.SourceId, out SettlementManpowerSourceBinding binding)
            || !ReferenceEquals(binding, proposal.RegistrationIdentity))
        {
            return Stale(ManpowerSourceConsequenceValidationReason.SourceRegistrationChanged);
        }
        if (!world.TryGetManpowerSourceSettlementContext(
                binding.Settlement,
                binding.Population,
                out int representedResidentFloor))
        {
            return Invalid(ManpowerSourceConsequenceValidationReason.SettlementNotInWorld);
        }

        string ruleKeyBefore = binding.PlannerRuleKeyAtComposition;
        string configurationBefore = binding.PlannerConfigurationIdentityAtComposition;
        int versionBefore = binding.PlannerVersionAtComposition;
        if (!PlannerIdentityMatches(binding.PlannerIdentity, ruleKeyBefore, configurationBefore, versionBefore))
            return Stale(ManpowerSourceConsequenceValidationReason.PlannerIdentityChanged);
        if (!string.Equals(ruleKeyBefore, proposal.PlannerRuleKey, StringComparison.Ordinal)
            || !string.Equals(configurationBefore, proposal.PlannerConfigurationIdentity, StringComparison.Ordinal)
            || versionBefore != proposal.ProposalVersion)
        {
            return Stale(ManpowerSourceConsequenceValidationReason.PlannerIdentityChanged);
        }

        SettlementPopulationDeathSourceProposal typed = proposal.SettlementPopulationDeath;
        SettlementPopulationRuntime population = binding.Population;
        if (!string.Equals(proposal.SettlementRuntimeId, binding.Settlement.RuntimeId, StringComparison.Ordinal)
            || proposal.ExpectedPopulationRevision < 0L
            || proposal.PopulationBefore < 0
            || proposal.RepresentedResidentFloor < 0
            || proposal.RepresentedResidentFloor > proposal.PopulationBefore
            || (proposal.Disposition == ManpowerSourceConsequenceDisposition.DomainTransitionRequired
                && (typed == null
                    || proposal.Effect.Kind != ManpowerSourceEffectKind.Death
                    || proposal.Effect.Amount <= 0L
                    || !IsValidTransition(
                        binding,
                        proposal.Effect.Amount,
                        proposal.ExpectedPopulationRevision,
                        proposal.PopulationBefore,
                        proposal.RepresentedResidentFloor,
                        typed.Transition)
                    || typed.SourceId != proposal.SourceId
                    || !string.Equals(typed.SettlementRuntimeId, proposal.SettlementRuntimeId, StringComparison.Ordinal)
                    || typed.ExpectedPopulationRevision != proposal.ExpectedPopulationRevision
                    || typed.PopulationBefore != proposal.PopulationBefore
                    || typed.RepresentedResidentFloor != proposal.RepresentedResidentFloor
                    || typed.Deaths != typed.Transition.Deaths
                    || typed.PopulationAfter != typed.Transition.PopulationAfter))
            || (proposal.Disposition == ManpowerSourceConsequenceDisposition.NoExternalMutationRequired
                && typed != null)
            || !Enum.IsDefined(typeof(ManpowerSourceConsequenceDisposition), proposal.Disposition))
        {
            return Invalid(ManpowerSourceConsequenceValidationReason.InvalidTransitionShape);
        }

        long currentRevision = population.Revision;
        int currentPopulation = population.CurrentPopulation;
        if (currentRevision != proposal.ExpectedPopulationRevision || currentPopulation != proposal.PopulationBefore)
            return Stale(ManpowerSourceConsequenceValidationReason.SettlementPopulationChanged);
        if (representedResidentFloor != proposal.RepresentedResidentFloor)
            return Stale(ManpowerSourceConsequenceValidationReason.RepresentedResidentFloorChanged);

        string fingerprint = ManpowerSourceFingerprint.BuildConsequenceFingerprint(
            proposal.SourceId,
            binding.Settlement.RuntimeId,
            currentRevision,
            currentPopulation,
            representedResidentFloor,
            ruleKeyBefore,
            configurationBefore,
            versionBefore,
            proposal.Effect.Kind,
            proposal.Effect.Amount);
        if (!string.Equals(fingerprint, proposal.DependencyFingerprint, StringComparison.Ordinal))
            return Stale(ManpowerSourceConsequenceValidationReason.DependencyFingerprintChanged);
        if (!PlannerIdentityMatches(
                binding.PlannerIdentity,
                binding.PlannerRuleKeyAtComposition,
                binding.PlannerConfigurationIdentityAtComposition,
                binding.PlannerVersionAtComposition))
            return Stale(ManpowerSourceConsequenceValidationReason.PlannerIdentityChanged);

        return new ManpowerSourceConsequenceValidationResult(
            ManpowerSourceConsequenceValidationStatus.Current,
            ManpowerSourceConsequenceValidationReason.None);
    }

    private static bool IsValidTransition(
        SettlementManpowerSourceBinding binding,
        long amount,
        long expectedRevision,
        int populationBefore,
        int representedResidentFloor,
        AggregateDemographyTransition transition)
    {
        if (transition == null || amount <= 0L || amount > int.MaxValue)
            return false;
        int deaths = (int)amount;
        long expectedAfter = (long)populationBefore - deaths;
        return string.Equals(transition.SettlementRuntimeId, binding.Settlement.RuntimeId, StringComparison.Ordinal)
            && transition.ExpectedPopulationRevision == expectedRevision
            && transition.PopulationBefore == populationBefore
            && transition.RepresentedResidentFloor == representedResidentFloor
            && transition.Births == 0
            && transition.Deaths == deaths
            && transition.NetChange == -((long)deaths)
            && expectedAfter >= 0L
            && transition.PopulationAfter == (int)expectedAfter
            && transition.PopulationAfter >= representedResidentFloor;
    }

    private static bool PlannerIdentityMatches(
        IManpowerSourceConsequencePlannerIdentity planner,
        string ruleKey,
        string configurationIdentity,
        int version)
    {
        return planner != null
            && string.Equals(planner.RuleKey, ruleKey, StringComparison.Ordinal)
            && string.Equals(planner.ConfigurationIdentity, configurationIdentity, StringComparison.Ordinal)
            && planner.Version == version;
    }

    private static ManpowerSourceConsequencePlanningResult Failed(
        ManpowerSourceConsequenceFailureCode code,
        string message)
    {
        return new ManpowerSourceConsequencePlanningResult(
            ManpowerSourceConsequencePlanningStatus.Failed,
            null,
            new ManpowerSourceConsequenceFailure(code, AggregateDemographyFailure.None, message));
    }

    private static ManpowerSourceConsequencePlanningResult Unsupported(
        ManpowerSourceConsequenceFailureCode code,
        string message)
    {
        return new ManpowerSourceConsequencePlanningResult(
            ManpowerSourceConsequencePlanningStatus.Unsupported,
            null,
            new ManpowerSourceConsequenceFailure(code, AggregateDemographyFailure.None, message));
    }

    private static ManpowerSourceConsequenceValidationResult Stale(
        ManpowerSourceConsequenceValidationReason reason)
    {
        return new ManpowerSourceConsequenceValidationResult(
            ManpowerSourceConsequenceValidationStatus.Stale,
            reason);
    }

    private static ManpowerSourceConsequenceValidationResult Invalid(
        ManpowerSourceConsequenceValidationReason reason)
    {
        return new ManpowerSourceConsequenceValidationResult(
            ManpowerSourceConsequenceValidationStatus.Invalid,
            reason);
    }
}
