using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

/// <summary>Pure, world-authorized transformation of exposed Battle manpower cohorts.</summary>
public interface IBattleDirectConsequenceRule
{
    string RuleKey { get; }
    string ConfigurationIdentity { get; }
    int Version { get; }
    IReadOnlyList<BattleCohortConsequencePartition> Evaluate(BattleDirectConsequenceInput input);
}

/// <summary>
/// Immutable world-composed identity for D6B2. There is deliberately no
/// default rule: a world must explicitly authorize consequence semantics.
/// </summary>
public sealed class BattleDirectConsequencePolicy
{
    private readonly IBattleDirectConsequenceRule rule;

    public string RuleKey { get; }
    public string ConfigurationIdentity { get; }
    public int Version { get; }
    public string SemanticFingerprint { get; }

    public BattleDirectConsequencePolicy(IBattleDirectConsequenceRule rule)
    {
        this.rule = rule ?? throw new ArgumentNullException(nameof(rule));
        RuleKey = RequireIdentity(rule.RuleKey, nameof(rule));
        ConfigurationIdentity = RequireIdentity(rule.ConfigurationIdentity, nameof(rule));
        Version = rule.Version;
        if (Version <= 0)
            throw new ArgumentOutOfRangeException(nameof(rule), "A D6B2 rule version must be positive.");
        SemanticFingerprint = BuildFingerprint(
            RuleKey,
            ConfigurationIdentity,
            Version,
            BattleDirectConsequencePlanningService.PlanSchemaVersion,
            BattleDirectConsequencePlanningService.CoverageVersion);
    }

    internal IBattleDirectConsequenceRule Rule => rule;

    internal bool IdentityRemainsStable()
    {
        try
        {
            return string.Equals(RuleKey, rule.RuleKey, StringComparison.Ordinal)
                && string.Equals(ConfigurationIdentity, rule.ConfigurationIdentity, StringComparison.Ordinal)
                && Version == rule.Version;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildFingerprint(
        string ruleKey,
        string configurationIdentity,
        int version,
        string schema,
        string coverage)
    {
        StringBuilder canonical = new StringBuilder();
        BattleResolutionStableEncoding.Append(canonical, "battle-direct-consequence-policy:v1");
        BattleResolutionStableEncoding.Append(canonical, ruleKey);
        BattleResolutionStableEncoding.Append(canonical, configurationIdentity);
        BattleResolutionStableEncoding.Append(canonical, version.ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(canonical, schema);
        BattleResolutionStableEncoding.Append(canonical, coverage);
        return "battle-direct-consequence-policy:sha256-v1:"
            + BattleResolutionStableEncoding.Sha256Key(canonical.ToString())
                .Substring("battle-causal:sha256-v1:".Length);
    }

    private static string RequireIdentity(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("D6B2 rule identity values must be explicit and non-empty.", parameterName);
        return value;
    }
}

/// <summary>Semantic identity of one exposed input cohort; no mutable store id is used.</summary>
public sealed class BattleDirectConsequenceCohortIdentity : IEquatable<BattleDirectConsequenceCohortIdentity>
{
    public BattleSideId SideId { get; }
    public ArmedForceId ForceId { get; }
    public ContingentId ContingentId { get; }
    public ManpowerInjuryState InjuryState { get; }
    public ManpowerCustodyState CustodyState { get; }
    public ArmedForceId CustodianForceId { get; }
    public ManpowerAvailabilityState AvailabilityState { get; }
    public string StableKey { get; }

    internal BattleDirectConsequenceCohortIdentity(
        BattleSideId sideId,
        ArmedForceId forceId,
        ContingentId contingentId,
        ManpowerInjuryState injuryState,
        ManpowerCustodyState custodyState,
        ArmedForceId custodianForceId,
        ManpowerAvailabilityState availabilityState)
    {
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
        ForceId = forceId ?? throw new ArgumentNullException(nameof(forceId));
        ContingentId = contingentId ?? throw new ArgumentNullException(nameof(contingentId));
        InjuryState = injuryState;
        CustodyState = custodyState;
        CustodianForceId = custodianForceId;
        AvailabilityState = availabilityState;
        StableKey = BuildStableKey(
            SideId.Value,
            ForceId.Value,
            ContingentId.Value,
            InjuryState,
            CustodyState,
            CustodianForceId?.Value,
            AvailabilityState);
    }

    public bool Equals(BattleDirectConsequenceCohortIdentity other)
        => other != null && string.Equals(StableKey, other.StableKey, StringComparison.Ordinal);
    public override bool Equals(object obj) => Equals(obj as BattleDirectConsequenceCohortIdentity);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableKey);

    internal static string BuildStableKey(
        string sideId,
        string forceId,
        string contingentId,
        ManpowerInjuryState injury,
        ManpowerCustodyState custody,
        string custodianForceId,
        ManpowerAvailabilityState availability)
    {
        StringBuilder builder = new StringBuilder();
        BattleResolutionStableEncoding.Append(builder, sideId);
        BattleResolutionStableEncoding.Append(builder, forceId);
        BattleResolutionStableEncoding.Append(builder, contingentId);
        BattleResolutionStableEncoding.Append(builder, ((int)injury).ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(builder, ((int)custody).ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(builder, custodianForceId);
        BattleResolutionStableEncoding.Append(builder, ((int)availability).ToString(CultureInfo.InvariantCulture));
        return builder.ToString();
    }
}

/// <summary>Read-only facts for one D3-exposed free and available cohort.</summary>
public sealed class BattleDirectConsequenceCohortInput
{
    public BattleDirectConsequenceCohortIdentity Identity { get; }
    public long Amount { get; }
    public ManpowerSourceId SourceId { get; }

    internal BattleDirectConsequenceCohortInput(
        BattleDirectConsequenceCohortIdentity identity,
        long amount,
        ManpowerSourceId sourceId)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Amount = amount;
        SourceId = sourceId;
    }
}

/// <summary>Minimal participant-side identity needed to validate explicit captures.</summary>
public sealed class BattleDirectConsequenceParticipant
{
    public BattleSideId SideId { get; }
    public ArmedForceId ForceId { get; }

    internal BattleDirectConsequenceParticipant(BattleSideId sideId, ArmedForceId forceId)
    {
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
        ForceId = forceId ?? throw new ArgumentNullException(nameof(forceId));
    }
}

public sealed class BattleDirectConsequenceContingentBinding
{
    public BattleSideId SideId { get; }
    public ArmedForceId ForceId { get; }
    public ContingentId ContingentId { get; }
    public ManpowerSourceId SourceId { get; }

    internal BattleDirectConsequenceContingentBinding(
        BattleSideId sideId,
        ArmedForceId forceId,
        ContingentId contingentId,
        ManpowerSourceId sourceId)
    {
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
        ForceId = forceId ?? throw new ArgumentNullException(nameof(forceId));
        ContingentId = contingentId ?? throw new ArgumentNullException(nameof(contingentId));
        SourceId = sourceId;
    }
}

/// <summary>
/// Immutable D6B2 rule input. It contains semantic D5 outcome/provenance and
/// D3/D6A aggregate facts, never raw D4 scores, capability, or random swing.
/// </summary>
public sealed class BattleDirectConsequenceInput
{
    private readonly IReadOnlyList<BattleDirectConsequenceCohortInput> cohorts;
    private readonly IReadOnlyList<BattleDirectConsequenceParticipant> participants;
    private readonly IReadOnlyList<BattleSideId> sides;
    private readonly IReadOnlyList<BattleDirectConsequenceContingentBinding> contingents;

    public BattleId BattleId { get; }
    public long AbsoluteDay { get; }
    public BattleOutcomeType OutcomeType { get; }
    public BattleSideId WinningSideId { get; }
    public string D5PolicyFingerprint { get; }
    public string D5CausalFingerprint { get; }
    public string D5SourceContextFingerprint { get; }
    public string D5ProjectionVersion { get; }
    public string D5NumericExecutionProfileKey { get; }
    public string D5CapabilityRuleKey { get; }
    public string D5RandomAuthorityRuleKey { get; }
    public string D5ResolverSettingsIdentity { get; }
    public IReadOnlyList<BattleDirectConsequenceCohortInput> Cohorts => cohorts;
    public IReadOnlyList<BattleDirectConsequenceParticipant> Participants => participants;
    public IReadOnlyList<BattleSideId> Sides => sides;
    public IReadOnlyList<BattleDirectConsequenceContingentBinding> Contingents => contingents;

    internal BattleDirectConsequenceInput(
        BattleId battleId,
        long absoluteDay,
        BattleOutcome outcome,
        IEnumerable<BattleDirectConsequenceCohortInput> cohorts,
        IEnumerable<BattleDirectConsequenceParticipant> participants,
        IEnumerable<BattleSideId> sides,
        IEnumerable<BattleDirectConsequenceContingentBinding> contingents)
    {
        BattleId = battleId ?? throw new ArgumentNullException(nameof(battleId));
        if (outcome == null) throw new ArgumentNullException(nameof(outcome));
        AbsoluteDay = absoluteDay;
        OutcomeType = outcome.OutcomeType;
        WinningSideId = outcome.WinningBattleSideId;
        BattleResolutionProvenance provenance = outcome.Provenance;
        D5PolicyFingerprint = provenance.PolicyFingerprint;
        D5CausalFingerprint = provenance.CausalResolutionFingerprint;
        D5SourceContextFingerprint = provenance.SourceContextFingerprint;
        D5ProjectionVersion = provenance.ProjectionVersion;
        D5NumericExecutionProfileKey = provenance.NumericExecutionProfileKey;
        D5CapabilityRuleKey = provenance.CapabilityRuleKey;
        D5RandomAuthorityRuleKey = provenance.RandomAuthorityRuleKey;
        D5ResolverSettingsIdentity = provenance.ResolverSettingsIdentity;
        List<BattleDirectConsequenceCohortInput> cohortValues = cohorts == null
            ? new List<BattleDirectConsequenceCohortInput>()
            : new List<BattleDirectConsequenceCohortInput>(cohorts);
        List<BattleDirectConsequenceParticipant> participantValues = participants == null
            ? new List<BattleDirectConsequenceParticipant>()
            : new List<BattleDirectConsequenceParticipant>(participants);
        this.cohorts = new ReadOnlyCollection<BattleDirectConsequenceCohortInput>(cohortValues);
        this.participants = new ReadOnlyCollection<BattleDirectConsequenceParticipant>(participantValues);
        List<BattleSideId> sideValues = sides == null ? new List<BattleSideId>() : new List<BattleSideId>(sides);
        List<BattleDirectConsequenceContingentBinding> contingentValues = contingents == null
            ? new List<BattleDirectConsequenceContingentBinding>()
            : new List<BattleDirectConsequenceContingentBinding>(contingents);
        this.sides = new ReadOnlyCollection<BattleSideId>(sideValues);
        this.contingents = new ReadOnlyCollection<BattleDirectConsequenceContingentBinding>(contingentValues);
    }
}

/// <summary>One semantic living destination; output insertion order is normalized by D6B2.</summary>
public sealed class BattleCohortConsequenceLivingDestination
{
    public ManpowerInjuryState InjuryState { get; }
    public ManpowerCustodyState CustodyState { get; }
    public ArmedForceId CustodianForceId { get; }
    public ManpowerAvailabilityState AvailabilityState { get; }
    public long Amount { get; }

    public BattleCohortConsequenceLivingDestination(
        ManpowerInjuryState injuryState,
        ManpowerCustodyState custodyState,
        ArmedForceId custodianForceId,
        ManpowerAvailabilityState availabilityState,
        long amount)
    {
        InjuryState = injuryState;
        CustodyState = custodyState;
        CustodianForceId = custodianForceId;
        AvailabilityState = availabilityState;
        Amount = amount;
    }
}

/// <summary>Complete conservation partition for exactly one exposed cohort.</summary>
public sealed class BattleCohortConsequencePartition
{
    private readonly IReadOnlyList<BattleCohortConsequenceLivingDestination> livingDestinations;

    public BattleDirectConsequenceCohortIdentity InputIdentity { get; }
    public long InputAmount { get; }
    public long DeathAmount { get; }
    public IReadOnlyList<BattleCohortConsequenceLivingDestination> LivingDestinations => livingDestinations;

    public BattleCohortConsequencePartition(
        BattleDirectConsequenceCohortIdentity inputIdentity,
        IEnumerable<BattleCohortConsequenceLivingDestination> livingDestinations,
        long deathAmount)
        : this(inputIdentity, 0L, livingDestinations, deathAmount)
    {
    }

    internal BattleCohortConsequencePartition(
        BattleDirectConsequenceCohortIdentity inputIdentity,
        long inputAmount,
        IEnumerable<BattleCohortConsequenceLivingDestination> livingDestinations,
        long deathAmount)
    {
        InputIdentity = inputIdentity;
        InputAmount = inputAmount;
        DeathAmount = deathAmount;
        List<BattleCohortConsequenceLivingDestination> values = livingDestinations == null
            ? new List<BattleCohortConsequenceLivingDestination>()
            : new List<BattleCohortConsequenceLivingDestination>(livingDestinations);
        this.livingDestinations = new ReadOnlyCollection<BattleCohortConsequenceLivingDestination>(values);
    }
}

public enum BattleDirectConsequenceFailureCode
{
    None = 0,
    InvalidRequest = 1,
    PolicyNotConfigured = 2,
    PolicyIdentityChanged = 3,
    D5PlanningFailed = 4,
    D5PlanInvalid = 5,
    ContextCreationFailed = 6,
    ContextMismatch = 7,
    ContextBecameStale = 8,
    InvalidManpowerState = 9,
    RuleFailed = 10,
    InvalidRuleOutput = 11,
    MissingPartition = 12,
    DuplicatePartition = 13,
    UnknownPartition = 14,
    InvalidAmount = 15,
    ConservationFailure = 16,
    AmountOverflow = 17,
    InvalidTransition = 18,
    InvalidCustodian = 19,
    UnboundDeath = 20,
    SourceConsequencePlanningFailed = 21,
    SourceProposalInvalid = 22,
    SourceProposalStale = 23,
    PlanInvalid = 24,
    PlanStale = 25
}

public sealed class BattleDirectConsequenceFailure
{
    public BattleDirectConsequenceFailureCode Code { get; }
    public string Message { get; }
    public BattleOutcomePlanningFailure D5Failure { get; }
    public BattleExecutionFailure ExecutionFailure { get; }
    public ManpowerSourceConsequenceFailure SourceFailure { get; }
    public bool IsFailure => Code != BattleDirectConsequenceFailureCode.None;

    internal BattleDirectConsequenceFailure(
        BattleDirectConsequenceFailureCode code,
        string message,
        BattleOutcomePlanningFailure d5Failure = null,
        BattleExecutionFailure executionFailure = null,
        ManpowerSourceConsequenceFailure sourceFailure = null)
    {
        Code = code;
        Message = message ?? string.Empty;
        D5Failure = d5Failure;
        ExecutionFailure = executionFailure;
        SourceFailure = sourceFailure;
    }

    public override string ToString() => Code
        + (Message.Length == 0 ? string.Empty : ": " + Message);
}

public enum BattleDirectConsequenceValidationStatus
{
    Current = 0,
    Stale = 1,
    Invalid = 2
}

public enum BattleDirectConsequenceStalenessReason
{
    CurrentDayChanged = 0,
    D5PlanChanged = 1,
    D6B2PolicyChanged = 2,
    ParticipantManpowerChanged = 3,
    SourceProposalChanged = 4,
    CustodianChanged = 5,
    PlanFingerprintChanged = 6
}

public sealed class BattleDirectConsequenceValidationReport
{
    public BattleDirectConsequenceValidationStatus Status { get; }
    public bool IsCurrent => Status == BattleDirectConsequenceValidationStatus.Current;
    public bool IsStale => Status == BattleDirectConsequenceValidationStatus.Stale;
    public bool IsInvalid => Status == BattleDirectConsequenceValidationStatus.Invalid;
    public IReadOnlyList<BattleDirectConsequenceStalenessReason> Reasons { get; }
    public string Message { get; }

    internal BattleDirectConsequenceValidationReport(
        BattleDirectConsequenceValidationStatus status,
        IEnumerable<BattleDirectConsequenceStalenessReason> reasons,
        string message)
    {
        Status = status;
        List<BattleDirectConsequenceStalenessReason> values = reasons == null
            ? new List<BattleDirectConsequenceStalenessReason>()
            : new List<BattleDirectConsequenceStalenessReason>(reasons);
        values.Sort();
        Reasons = new ReadOnlyCollection<BattleDirectConsequenceStalenessReason>(values);
        Message = message ?? string.Empty;
    }
}

public sealed class BattleDirectConsequenceDeathTrace
{
    public BattleDirectConsequenceCohortIdentity CohortIdentity { get; }
    public long DeathAmount { get; }

    internal BattleDirectConsequenceDeathTrace(
        BattleDirectConsequenceCohortIdentity cohortIdentity,
        long deathAmount)
    {
        CohortIdentity = cohortIdentity;
        DeathAmount = deathAmount;
    }
}

public sealed class BattleDirectConsequenceSourceGroup
{
    private readonly IReadOnlyList<BattleDirectConsequenceDeathTrace> traces;

    public ManpowerSourceId SourceId { get; }
    public long DeathAmount { get; }
    public ManpowerSourceConsequenceProposal Proposal { get; }
    public IReadOnlyList<BattleDirectConsequenceDeathTrace> Traces => traces;

    internal BattleDirectConsequenceSourceGroup(
        ManpowerSourceId sourceId,
        long deathAmount,
        IEnumerable<BattleDirectConsequenceDeathTrace> traces,
        ManpowerSourceConsequenceProposal proposal)
    {
        SourceId = sourceId;
        DeathAmount = deathAmount;
        this.traces = new ReadOnlyCollection<BattleDirectConsequenceDeathTrace>(
            traces == null
                ? new List<BattleDirectConsequenceDeathTrace>()
                : new List<BattleDirectConsequenceDeathTrace>(traces));
        Proposal = proposal;
    }
}

public sealed class BattleDirectConsequenceContingentProjection
{
    private readonly IReadOnlyList<ContingentManpowerCohort> cohorts;

    public ContingentId ContingentId { get; }
    public ManpowerSourceId SourceId { get; }
    public IReadOnlyList<ContingentManpowerCohort> Cohorts => cohorts;
    public long LivingRosterAmount { get; }
    public long AvailableAmount { get; }

    internal BattleDirectConsequenceContingentProjection(
        ContingentId contingentId,
        ManpowerSourceId sourceId,
        IEnumerable<ContingentManpowerCohort> cohorts)
    {
        ContingentId = contingentId;
        SourceId = sourceId;
        List<ContingentManpowerCohort> values = cohorts == null
            ? new List<ContingentManpowerCohort>()
            : new List<ContingentManpowerCohort>(cohorts);
        values.Sort(ContingentManpowerState.CompareCohorts);
        this.cohorts = new ReadOnlyCollection<ContingentManpowerCohort>(values);
        long total = 0L;
        long available = 0L;
        foreach (ContingentManpowerCohort cohort in values)
        {
            total = checked(total + cohort.Amount);
            if (cohort.AvailabilityState == ManpowerAvailabilityState.Available)
                available = checked(available + cohort.Amount);
        }
        LivingRosterAmount = total;
        AvailableAmount = available;
    }
}

public sealed class BattleDirectConsequencePlan
{
    private readonly IReadOnlyList<BattleDirectConsequenceCohortInput> inputs;
    private readonly IReadOnlyList<BattleCohortConsequencePartition> partitions;
    private readonly IReadOnlyList<BattleDirectConsequenceSourceGroup> sourceGroups;
    private readonly IReadOnlyList<BattleDirectConsequenceContingentProjection> projections;

    public BattleId BattleId { get; }
    public long AbsoluteDay { get; }
    public BattleOutcomeType OutcomeType { get; }
    public BattleSideId WinningSideId { get; }
    public string D5PolicyFingerprint { get; }
    public string D5CausalFingerprint { get; }
    public string D5SourceContextFingerprint { get; }
    public string D6B2PolicyFingerprint { get; }
    public string Fingerprint { get; }
    public bool IsComplete => true;
    public IReadOnlyList<BattleDirectConsequenceCohortInput> Inputs => inputs;
    public IReadOnlyList<BattleCohortConsequencePartition> Partitions => partitions;
    public IReadOnlyList<BattleDirectConsequenceSourceGroup> SourceGroups => sourceGroups;
    public IReadOnlyList<BattleDirectConsequenceContingentProjection> ContingentProjections => projections;

    internal BattleOutcomeApplicationPlan D5ApplicationPlan { get; }
    internal BattleExecutionContext SourceContext { get; }

    internal BattleDirectConsequencePlan(
        BattleOutcomeApplicationPlan d5ApplicationPlan,
        BattleExecutionContext sourceContext,
        string d6b2PolicyFingerprint,
        IEnumerable<BattleDirectConsequenceCohortInput> inputs,
        IEnumerable<BattleCohortConsequencePartition> partitions,
        IEnumerable<BattleDirectConsequenceSourceGroup> sourceGroups,
        IEnumerable<BattleDirectConsequenceContingentProjection> projections,
        string fingerprint)
    {
        D5ApplicationPlan = d5ApplicationPlan ?? throw new ArgumentNullException(nameof(d5ApplicationPlan));
        SourceContext = sourceContext ?? throw new ArgumentNullException(nameof(sourceContext));
        BattleId = d5ApplicationPlan.BattleId;
        AbsoluteDay = d5ApplicationPlan.Outcome.ResolvedAbsoluteDay;
        OutcomeType = d5ApplicationPlan.Outcome.OutcomeType;
        WinningSideId = d5ApplicationPlan.Outcome.WinningBattleSideId;
        BattleResolutionProvenance provenance = d5ApplicationPlan.Outcome.Provenance;
        D5PolicyFingerprint = provenance.PolicyFingerprint;
        D5CausalFingerprint = provenance.CausalResolutionFingerprint;
        D5SourceContextFingerprint = provenance.SourceContextFingerprint;
        D6B2PolicyFingerprint = d6b2PolicyFingerprint;
        this.inputs = Copy(inputs);
        this.partitions = Copy(partitions);
        this.sourceGroups = Copy(sourceGroups);
        this.projections = Copy(projections);
        Fingerprint = fingerprint;
    }

    private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values)
        => new ReadOnlyCollection<T>(values == null ? new List<T>() : new List<T>(values));
}
