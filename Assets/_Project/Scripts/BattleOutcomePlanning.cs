using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

/// <summary>
/// Explicit host-composition declaration of numeric profiles supported by
/// this runtime. This is not evidence of cross-host numeric equivalence.
/// </summary>
public sealed class BattleNumericExecutionProfileCompatibility
{
    private readonly IReadOnlyList<string> supportedProfileKeys;

    public BattleNumericExecutionProfileCompatibility(IEnumerable<string> supportedProfileKeys)
    {
        if (supportedProfileKeys == null)
        {
            throw new ArgumentNullException(nameof(supportedProfileKeys));
        }

        SortedSet<string> sorted = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string key in supportedProfileKeys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "Supported numeric profile keys must be non-empty semantic identities.",
                    nameof(supportedProfileKeys));
            }

            sorted.Add(key);
        }

        this.supportedProfileKeys = new ReadOnlyCollection<string>(new List<string>(sorted));
    }

    public IReadOnlyList<string> SupportedProfileKeys => supportedProfileKeys;

    public bool Supports(string profileKey)
    {
        if (string.IsNullOrWhiteSpace(profileKey)) return false;
        foreach (string supportedKey in supportedProfileKeys)
        {
            if (string.Equals(supportedKey, profileKey, StringComparison.Ordinal)) return true;
        }

        return false;
    }
}

/// <summary>
/// Immutable world-composed authority for D5 Battle outcome planning. The
/// capability and random implementations must be pure/contextual and keep
/// their RuleKeys stable for the lifetime of the composed runtime.
/// </summary>
public sealed class BattleResolutionPolicy
{
    private readonly IBattleContingentCapabilityProvider capabilityProvider;
    private readonly IBattleContextualConflictRandomSource randomSource;
    private readonly BattleNumericExecutionProfileCompatibility profileCompatibility;

    public string CapabilityRuleKey { get; }
    public string RandomAuthorityRuleKey { get; }
    public BattleResolutionResolverSettings ResolverSettings { get; }
    public string ProjectionVersion { get; }
    public string NumericExecutionProfileKey { get; }
    public bool NumericExecutionProfileSupported =>
        profileCompatibility.Supports(NumericExecutionProfileKey);
    public string ResolverSettingsIdentity { get; }
    public string SemanticFingerprint { get; }

    public BattleResolutionPolicy(
        IBattleContingentCapabilityProvider capabilityProvider,
        IBattleContextualConflictRandomSource randomSource,
        BattleResolutionResolverSettings resolverSettings,
        string projectionVersion,
        string numericExecutionProfileKey,
        BattleNumericExecutionProfileCompatibility profileCompatibility)
    {
        this.capabilityProvider = capabilityProvider
            ?? throw new ArgumentNullException(nameof(capabilityProvider));
        this.randomSource = randomSource
            ?? throw new ArgumentNullException(nameof(randomSource));
        ResolverSettings = resolverSettings
            ?? throw new ArgumentNullException(nameof(resolverSettings));
        this.profileCompatibility = profileCompatibility == null
            ? throw new ArgumentNullException(nameof(profileCompatibility))
            : new BattleNumericExecutionProfileCompatibility(profileCompatibility.SupportedProfileKeys);

        CapabilityRuleKey = RequireKey(capabilityProvider.RuleKey, nameof(capabilityProvider));
        RandomAuthorityRuleKey = RequireKey(randomSource.RuleKey, nameof(randomSource));
        ProjectionVersion = RequireKey(projectionVersion, nameof(projectionVersion));
        NumericExecutionProfileKey = RequireKey(
            numericExecutionProfileKey,
            nameof(numericExecutionProfileKey));
        ResolverSettingsIdentity = BuildResolverSettingsIdentity(resolverSettings);
        SemanticFingerprint = BuildSemanticFingerprint();
    }

    internal bool RuleIdentitiesRemainStable()
    {
        return string.Equals(CapabilityRuleKey, capabilityProvider.RuleKey, StringComparison.Ordinal)
            && string.Equals(RandomAuthorityRuleKey, randomSource.RuleKey, StringComparison.Ordinal);
    }

    internal BattleResolutionComputationService CreateComputationService(
        BattleExecutionContextBuilder contextBuilder)
    {
        return new BattleResolutionComputationService(
            contextBuilder,
            capabilityProvider,
            ResolverSettings,
            randomSource);
    }

    private string BuildSemanticFingerprint()
    {
        StringBuilder canonical = new StringBuilder();
        BattleResolutionStableEncoding.Append(canonical, "battle-resolution-policy:v1");
        BattleResolutionStableEncoding.Append(canonical, "capability-rule-key");
        BattleResolutionStableEncoding.Append(canonical, CapabilityRuleKey);
        BattleResolutionStableEncoding.Append(canonical, "random-authority-rule-key");
        BattleResolutionStableEncoding.Append(canonical, RandomAuthorityRuleKey);
        BattleResolutionStableEncoding.Append(canonical, "max-random-swing-fraction");
        BattleResolutionStableEncoding.Append(
            canonical,
            BattleResolutionStableEncoding.Float(ResolverSettings.MaxRandomSwingFraction));
        BattleResolutionStableEncoding.Append(canonical, "draw-margin-fraction");
        BattleResolutionStableEncoding.Append(
            canonical,
            BattleResolutionStableEncoding.Float(ResolverSettings.DrawMarginFraction));
        BattleResolutionStableEncoding.Append(canonical, "projection-version");
        BattleResolutionStableEncoding.Append(canonical, ProjectionVersion);
        BattleResolutionStableEncoding.Append(canonical, "numeric-profile-key");
        BattleResolutionStableEncoding.Append(canonical, NumericExecutionProfileKey);
        BattleResolutionStableEncoding.Append(canonical, "numeric-profile-supported");
        BattleResolutionStableEncoding.Append(
            canonical,
            NumericExecutionProfileSupported ? "true" : "false");
        return "battle-policy:sha256-v1:"
            + BattleResolutionStableEncoding.Sha256Key(canonical.ToString())
                .Substring("battle-causal:sha256-v1:".Length);
    }

    private static string BuildResolverSettingsIdentity(BattleResolutionResolverSettings settings)
    {
        StringBuilder canonical = new StringBuilder();
        BattleResolutionStableEncoding.Append(canonical, "battle-resolver-settings:v1");
        BattleResolutionStableEncoding.Append(
            canonical,
            BattleResolutionStableEncoding.Float(settings.MaxRandomSwingFraction));
        BattleResolutionStableEncoding.Append(
            canonical,
            BattleResolutionStableEncoding.Float(settings.DrawMarginFraction));
        return "battle-resolver-settings:sha256-v1:"
            + BattleResolutionStableEncoding.Sha256Key(canonical.ToString())
                .Substring("battle-causal:sha256-v1:".Length);
    }

    private static string RequireKey(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Battle policy identities must be explicit and non-empty.", parameterName);
        }

        return value;
    }
}

public enum BattleOutcomeType
{
    Victory = 0,
    Draw = 1
}

/// <summary>
/// Minimal proposed Battle-semantic outcome. It contains no raw scores and is
/// neither accepted nor persistent world truth.
/// </summary>
public sealed class BattleOutcome
{
    internal BattleOutcome(
        BattleId battleId,
        BattleOutcomeType outcomeType,
        BattleSideId winningBattleSideId,
        long resolvedAbsoluteDay,
        BattleResolutionProvenance provenance)
    {
        if (battleId == null) throw new ArgumentNullException(nameof(battleId));
        if (resolvedAbsoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(resolvedAbsoluteDay));
        if (provenance == null) throw new ArgumentNullException(nameof(provenance));
        if (outcomeType == BattleOutcomeType.Victory && winningBattleSideId == null)
        {
            throw new ArgumentException("A Battle victory requires exactly one winning BattleSideId.", nameof(winningBattleSideId));
        }

        if (outcomeType == BattleOutcomeType.Draw && winningBattleSideId != null)
        {
            throw new ArgumentException("A Battle draw cannot have a winning BattleSideId.", nameof(winningBattleSideId));
        }

        if (outcomeType != BattleOutcomeType.Victory && outcomeType != BattleOutcomeType.Draw)
        {
            throw new ArgumentOutOfRangeException(nameof(outcomeType));
        }

        BattleId = battleId;
        OutcomeType = outcomeType;
        WinningBattleSideId = winningBattleSideId;
        ResolvedAbsoluteDay = resolvedAbsoluteDay;
        Provenance = provenance;
    }

    public BattleId BattleId { get; }
    public BattleOutcomeType OutcomeType { get; }
    public BattleSideId WinningBattleSideId { get; }
    public long ResolvedAbsoluteDay { get; }
    public BattleResolutionProvenance Provenance { get; }
}

/// <summary>Minimal immutable audit identities for a proposed outcome.</summary>
public sealed class BattleResolutionProvenance
{
    internal BattleResolutionProvenance(
        string policyFingerprint,
        string numericExecutionProfileKey,
        string projectionVersion,
        string causalResolutionFingerprint,
        string sourceContextFingerprint,
        string capabilityRuleKey,
        string randomAuthorityRuleKey,
        string resolverSettingsIdentity)
    {
        PolicyFingerprint = policyFingerprint;
        NumericExecutionProfileKey = numericExecutionProfileKey;
        ProjectionVersion = projectionVersion;
        CausalResolutionFingerprint = causalResolutionFingerprint;
        SourceContextFingerprint = sourceContextFingerprint;
        CapabilityRuleKey = capabilityRuleKey;
        RandomAuthorityRuleKey = randomAuthorityRuleKey;
        ResolverSettingsIdentity = resolverSettingsIdentity;
    }

    public string PolicyFingerprint { get; }
    public string NumericExecutionProfileKey { get; }
    public string ProjectionVersion { get; }
    public string CausalResolutionFingerprint { get; }
    public string SourceContextFingerprint { get; }
    public string CapabilityRuleKey { get; }
    public string RandomAuthorityRuleKey { get; }
    public string ResolverSettingsIdentity { get; }
}

public enum BattleDirectConsequencePlanStatus
{
    NotProvided = 0
}

/// <summary>
/// Immutable, ephemeral D5 proposal. Direct consequences are deliberately
/// absent; this object is not ready for authoritative application.
/// </summary>
public sealed class BattleOutcomeApplicationPlan
{
    internal BattleOutcomeApplicationPlan(
        BattleExecutionContext sourceContext,
        BattleResolutionComputation computation,
        BattleOutcome outcome,
        string policyFingerprint)
    {
        if (sourceContext == null) throw new ArgumentNullException(nameof(sourceContext));
        ResolutionComputation = computation ?? throw new ArgumentNullException(nameof(computation));
        Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
        AuthorizedPolicyFingerprint = policyFingerprint
            ?? throw new ArgumentNullException(nameof(policyFingerprint));
        BattleId = sourceContext.BattleId;
        SourceContextFingerprint = sourceContext.StableKey;
        SourceDependencies = sourceContext.Dependencies;
        ExecutionPlan = sourceContext.Plan;
        DirectConsequenceStatus = BattleDirectConsequencePlanStatus.NotProvided;
    }

    public BattleId BattleId { get; }
    public BattleOutcome Outcome { get; }
    public string AuthorizedPolicyFingerprint { get; }
    public string SourceContextFingerprint { get; }
    public BattleExecutionDependencyFingerprint SourceDependencies { get; }
    public BattleExecutionPlan ExecutionPlan { get; }
    public BattleResolutionComputation ResolutionComputation { get; }
    public BattleDirectConsequencePlanStatus DirectConsequenceStatus { get; }
    public bool HasCompleteDirectConsequencePlan => false;
    public bool IsCommitReady => false;
}

public enum BattleOutcomePlanningFailureCode
{
    None = 0,
    PolicyNotConfigured = 1,
    NumericProfileUnsupported = 2,
    ProjectionVersionUnsupported = 3,
    PolicyIdentityMismatch = 4,
    ContextCreationFailed = 5,
    RawResolutionFailed = 6,
    ExpectedFingerprintMismatch = 7,
    OutcomeMappingFailed = 8,
    InvalidOutcome = 9,
    InvalidPlan = 10,
    PlanStale = 11,
    ContextBecameStale = 12
}

public sealed class BattleOutcomePlanningFailure
{
    internal BattleOutcomePlanningFailure(
        BattleOutcomePlanningFailureCode code,
        string message,
        BattleExecutionFailure executionFailure = null,
        BattleResolutionComputationFailure computationFailure = null)
    {
        Code = code;
        Message = message ?? string.Empty;
        ExecutionFailure = executionFailure;
        ComputationFailure = computationFailure;
    }

    public BattleOutcomePlanningFailureCode Code { get; }
    public string Message { get; }
    public BattleExecutionFailure ExecutionFailure { get; }
    public BattleResolutionComputationFailure ComputationFailure { get; }
    public bool IsFailure => Code != BattleOutcomePlanningFailureCode.None;
}

public enum BattleOutcomePlanValidationStatus
{
    Current = 0,
    Stale = 1,
    Invalid = 2
}

public enum BattleOutcomePlanStalenessReason
{
    CurrentDayChanged = 0,
    BattleNoLongerEligible = 1,
    ExecutionContextChanged = 2,
    CausalResolutionChanged = 3
}

public sealed class BattleOutcomePlanValidationReport
{
    internal BattleOutcomePlanValidationReport(
        BattleOutcomePlanValidationStatus status,
        IEnumerable<BattleOutcomePlanStalenessReason> reasons,
        string message)
    {
        Status = status;
        List<BattleOutcomePlanStalenessReason> values = reasons == null
            ? new List<BattleOutcomePlanStalenessReason>()
            : new List<BattleOutcomePlanStalenessReason>(reasons);
        values.Sort();
        for (int index = values.Count - 1; index > 0; index--)
        {
            if (values[index] == values[index - 1]) values.RemoveAt(index);
        }

        Reasons = new ReadOnlyCollection<BattleOutcomePlanStalenessReason>(values);
        Message = message ?? string.Empty;
    }

    public BattleOutcomePlanValidationStatus Status { get; }
    public bool IsCurrent => Status == BattleOutcomePlanValidationStatus.Current;
    public bool IsStale => Status == BattleOutcomePlanValidationStatus.Stale;
    public bool IsInvalid => Status == BattleOutcomePlanValidationStatus.Invalid;
    public IReadOnlyList<BattleOutcomePlanStalenessReason> Reasons { get; }
    public string Message { get; }
}

/// <summary>
/// World-bound D5 boundary. It owns its D4 computation service and reads the
/// runtime's current logical day; callers cannot supply resolution authority.
/// </summary>
public sealed class BattleOutcomePlanningService
{
    private readonly BattleExecutionContextBuilder contextBuilder;
    private readonly SimulationTime simulationTime;
    private readonly BattleResolutionPolicy authorizedPolicy;
    private readonly BattleResolutionComputationService computationService;

    internal BattleOutcomePlanningService(
        BattleExecutionContextBuilder contextBuilder,
        SimulationTime simulationTime,
        BattleResolutionPolicy authorizedPolicy)
    {
        this.contextBuilder = contextBuilder
            ?? throw new ArgumentNullException(nameof(contextBuilder));
        this.simulationTime = simulationTime
            ?? throw new ArgumentNullException(nameof(simulationTime));
        this.authorizedPolicy = authorizedPolicy;

        if (authorizedPolicy != null)
        {
            if (!authorizedPolicy.RuleIdentitiesRemainStable())
            {
                throw new ArgumentException(
                    "Battle policy dependency RuleKeys changed before world composition.",
                    nameof(authorizedPolicy));
            }

            computationService = authorizedPolicy.CreateComputationService(contextBuilder);
        }
    }

    public bool IsConfigured => authorizedPolicy != null;
    public string AuthorizedPolicyFingerprint => authorizedPolicy?.SemanticFingerprint;

    public bool TryCreateApplicationPlan(
        BattleId battleId,
        out BattleOutcomeApplicationPlan plan,
        out BattleOutcomePlanningFailure failure)
    {
        return TryCreateApplicationPlan(battleId, null, null, out plan, out failure);
    }

    public bool TryCreateApplicationPlan(
        BattleId battleId,
        BattleExecutionPlan executionPlan,
        string expectedCausalResolutionFingerprint,
        out BattleOutcomeApplicationPlan plan,
        out BattleOutcomePlanningFailure failure)
    {
        plan = null;
        failure = null;

        if (authorizedPolicy == null)
        {
            return Fail(
                BattleOutcomePlanningFailureCode.PolicyNotConfigured,
                "Authoritative Battle outcome planning is unavailable because the world has no BattleResolutionPolicy.",
                out failure);
        }

        if (!authorizedPolicy.RuleIdentitiesRemainStable())
        {
            return Fail(
                BattleOutcomePlanningFailureCode.PolicyIdentityMismatch,
                "A world-authorized Battle policy dependency changed its captured RuleKey.",
                out failure);
        }

        if (!authorizedPolicy.NumericExecutionProfileSupported)
        {
            return Fail(
                BattleOutcomePlanningFailureCode.NumericProfileUnsupported,
                "The configured Battle numeric execution profile is not declared supported by this host composition.",
                out failure);
        }

        if (!string.Equals(
            authorizedPolicy.ProjectionVersion,
            BattleResolutionComputationService.ProjectionVersionValue,
            StringComparison.Ordinal))
        {
            return Fail(
                BattleOutcomePlanningFailureCode.ProjectionVersionUnsupported,
                "The world policy projection version is not supported by the D4 computation boundary.",
                out failure);
        }

        long currentDay = simulationTime.AbsoluteDay;
        if (!contextBuilder.TryCreate(
            battleId,
            currentDay,
            executionPlan,
            out BattleExecutionContext context,
            out BattleExecutionFailure executionFailure))
        {
            failure = new BattleOutcomePlanningFailure(
                BattleOutcomePlanningFailureCode.ContextCreationFailed,
                executionFailure?.Message ?? "The current Battle execution context could not be created.",
                executionFailure);
            return false;
        }

        if (!computationService.TryCompute(
            context,
            currentDay,
            out BattleResolutionComputation computation,
            out BattleResolutionComputationFailure computationFailure))
        {
            failure = new BattleOutcomePlanningFailure(
                BattleOutcomePlanningFailureCode.RawResolutionFailed,
                computationFailure?.Message ?? "Authorized D4 Battle recomputation failed.",
                computationFailure?.ExecutionFailure,
                computationFailure);
            return false;
        }

        long postResolutionDay = simulationTime.AbsoluteDay;
        bool contextStillCurrent = contextBuilder.TryValidateCurrent(
            context,
            postResolutionDay,
            out _,
            out BattleExecutionFailure postResolutionFailure);
        if (postResolutionDay != currentDay
            || !contextStillCurrent)
        {
            failure = new BattleOutcomePlanningFailure(
                BattleOutcomePlanningFailureCode.ContextBecameStale,
                "The Battle execution context changed while the authorized raw resolution was being computed.",
                postResolutionFailure);
            return false;
        }

        if (!authorizedPolicy.RuleIdentitiesRemainStable())
        {
            return Fail(
                BattleOutcomePlanningFailureCode.PolicyIdentityMismatch,
                "A world-authorized Battle policy dependency changed during raw resolution.",
                out failure);
        }

        if (expectedCausalResolutionFingerprint != null
            && !string.Equals(
                expectedCausalResolutionFingerprint,
                computation.CausalFingerprint,
                StringComparison.Ordinal))
        {
            return Fail(
                BattleOutcomePlanningFailureCode.ExpectedFingerprintMismatch,
                "The expected causal resolution fingerprint does not match the current world-authorized recomputation.",
                out failure);
        }

        if (!string.Equals(
            computation.ProjectionVersion,
            authorizedPolicy.ProjectionVersion,
            StringComparison.Ordinal))
        {
            return Fail(
                BattleOutcomePlanningFailureCode.ProjectionVersionUnsupported,
                "The recomputed D4 projection does not match the world-authorized projection version.",
                out failure);
        }

        if (computation.BattleId != battleId
            || computation.ExecutionAbsoluteDay != currentDay
            || computation.SourceContextFingerprint != context.StableKey)
        {
            return Fail(
                BattleOutcomePlanningFailureCode.InvalidOutcome,
                "The authorized D4 computation does not identify the current Battle context and logical day.",
                out failure);
        }

        if (!TryMapOutcome(computation, authorizedPolicy, out BattleOutcome outcome, out failure))
        {
            return false;
        }

        plan = new BattleOutcomeApplicationPlan(
            context,
            computation,
            outcome,
            authorizedPolicy.SemanticFingerprint);
        return true;
    }

    public bool TryValidateCurrent(
        BattleOutcomeApplicationPlan plan,
        out BattleOutcomePlanValidationReport report,
        out BattleOutcomePlanningFailure failure)
    {
        report = null;
        failure = null;
        if (plan == null
            || plan.BattleId == null
            || plan.Outcome == null
            || plan.ResolutionComputation == null
            || plan.SourceDependencies == null
            || plan.Outcome.Provenance == null
            || plan.Outcome.BattleId != plan.BattleId
            || plan.ResolutionComputation.BattleId != plan.BattleId
            || plan.DirectConsequenceStatus != BattleDirectConsequencePlanStatus.NotProvided
            || plan.HasCompleteDirectConsequencePlan
            || plan.IsCommitReady)
        {
            report = new BattleOutcomePlanValidationReport(
                BattleOutcomePlanValidationStatus.Invalid,
                null,
                "The supplied BattleOutcomeApplicationPlan is malformed or has an unsupported consequence state.");
            failure = new BattleOutcomePlanningFailure(
                BattleOutcomePlanningFailureCode.InvalidPlan,
                report.Message);
            return false;
        }

        if (authorizedPolicy == null)
        {
            report = new BattleOutcomePlanValidationReport(
                BattleOutcomePlanValidationStatus.Invalid,
                null,
                "The world has no authorized BattleResolutionPolicy.");
            failure = new BattleOutcomePlanningFailure(
                BattleOutcomePlanningFailureCode.PolicyNotConfigured,
                report.Message);
            return false;
        }

        if (!string.Equals(
                plan.AuthorizedPolicyFingerprint,
                authorizedPolicy.SemanticFingerprint,
                StringComparison.Ordinal)
            || !authorizedPolicy.RuleIdentitiesRemainStable())
        {
            report = new BattleOutcomePlanValidationReport(
                BattleOutcomePlanValidationStatus.Invalid,
                null,
                "The plan policy identity does not match this world's captured policy authority.");
            failure = new BattleOutcomePlanningFailure(
                BattleOutcomePlanningFailureCode.PolicyIdentityMismatch,
                report.Message);
            return false;
        }

        if (!authorizedPolicy.NumericExecutionProfileSupported)
        {
            report = new BattleOutcomePlanValidationReport(
                BattleOutcomePlanValidationStatus.Invalid,
                null,
                "The configured Battle numeric execution profile is unsupported by this host composition.");
            failure = new BattleOutcomePlanningFailure(
                BattleOutcomePlanningFailureCode.NumericProfileUnsupported,
                report.Message);
            return false;
        }

        if (!string.Equals(
            authorizedPolicy.ProjectionVersion,
            BattleResolutionComputationService.ProjectionVersionValue,
            StringComparison.Ordinal))
        {
            report = new BattleOutcomePlanValidationReport(
                BattleOutcomePlanValidationStatus.Invalid,
                null,
                "The configured Battle projection version is unsupported by this D4 boundary.");
            failure = new BattleOutcomePlanningFailure(
                BattleOutcomePlanningFailureCode.ProjectionVersionUnsupported,
                report.Message);
            return false;
        }

        List<BattleOutcomePlanStalenessReason> reasons =
            new List<BattleOutcomePlanStalenessReason>();
        long currentDay = simulationTime.AbsoluteDay;
        if (currentDay != plan.Outcome.ResolvedAbsoluteDay)
        {
            reasons.Add(BattleOutcomePlanStalenessReason.CurrentDayChanged);
        }

        if (!contextBuilder.TryCreate(
            plan.BattleId,
            currentDay,
            plan.ExecutionPlan,
            out BattleExecutionContext currentContext,
            out BattleExecutionFailure executionFailure))
        {
            reasons.Add(BattleOutcomePlanStalenessReason.BattleNoLongerEligible);
            return Stale(
                reasons,
                "The Battle can no longer produce an eligible current execution context.",
                executionFailure,
                null,
                out report,
                out failure);
        }

        if (!string.Equals(
            currentContext.StableKey,
            plan.SourceContextFingerprint,
            StringComparison.Ordinal))
        {
            reasons.Add(BattleOutcomePlanStalenessReason.ExecutionContextChanged);
        }

        if (!computationService.TryCompute(
            currentContext,
            currentDay,
            out BattleResolutionComputation currentComputation,
            out BattleResolutionComputationFailure computationFailure))
        {
            reasons.Add(BattleOutcomePlanStalenessReason.CausalResolutionChanged);
            return Stale(
                reasons,
                "The world-authorized D4 result could not be recomputed for the current Battle state.",
                computationFailure?.ExecutionFailure,
                computationFailure,
                out report,
                out failure);
        }

        long postResolutionDay = simulationTime.AbsoluteDay;
        bool contextStillCurrent = contextBuilder.TryValidateCurrent(
            currentContext,
            postResolutionDay,
            out _,
            out BattleExecutionFailure postResolutionFailure);
        if (postResolutionDay != currentDay
            || !contextStillCurrent)
        {
            if (postResolutionDay != currentDay)
            {
                reasons.Add(BattleOutcomePlanStalenessReason.CurrentDayChanged);
            }

            if (postResolutionFailure != null && postResolutionFailure.IsFailure)
            {
                reasons.Add(BattleOutcomePlanStalenessReason.ExecutionContextChanged);
            }

            return Stale(
                reasons,
                "The current Battle context changed during plan revalidation.",
                postResolutionFailure,
                null,
                out report,
                out failure);
        }

        if (!authorizedPolicy.RuleIdentitiesRemainStable())
        {
            report = new BattleOutcomePlanValidationReport(
                BattleOutcomePlanValidationStatus.Invalid,
                null,
                "A world-authorized Battle policy dependency changed during plan validation.");
            failure = new BattleOutcomePlanningFailure(
                BattleOutcomePlanningFailureCode.PolicyIdentityMismatch,
                report.Message);
            return false;
        }

        if (!string.Equals(
            currentComputation.CausalFingerprint,
            plan.Outcome.Provenance.CausalResolutionFingerprint,
            StringComparison.Ordinal))
        {
            reasons.Add(BattleOutcomePlanStalenessReason.CausalResolutionChanged);
        }

        if (!TryMapOutcome(
            currentComputation,
            authorizedPolicy,
            out BattleOutcome currentOutcome,
            out _)
            || currentOutcome.OutcomeType != plan.Outcome.OutcomeType
            || currentOutcome.WinningBattleSideId != plan.Outcome.WinningBattleSideId
            || currentOutcome.ResolvedAbsoluteDay != plan.Outcome.ResolvedAbsoluteDay)
        {
            reasons.Add(BattleOutcomePlanStalenessReason.CausalResolutionChanged);
        }

        if (reasons.Count > 0)
        {
            return Stale(
                reasons,
                "The Battle outcome plan no longer matches current world truth and authorized recomputation.",
                null,
                null,
                out report,
                out failure);
        }

        report = new BattleOutcomePlanValidationReport(
            BattleOutcomePlanValidationStatus.Current,
            null,
            "The Battle outcome plan matches current context and authorized recomputation.");
        return true;
    }

    private static bool TryMapOutcome(
        BattleResolutionComputation computation,
        BattleResolutionPolicy policy,
        out BattleOutcome outcome,
        out BattleOutcomePlanningFailure failure)
    {
        outcome = null;
        failure = null;
        BattleOutcomeType outcomeType;
        BattleSideId winner = null;
        switch (computation.RawResult.Outcome)
        {
            case ConflictOutcomeType.Victory:
                if (string.IsNullOrWhiteSpace(computation.RawResult.WinningSideId)
                    || !computation.TryGetSideMapping(
                        computation.RawResult.WinningSideId,
                        out BattleResolutionSideMapping winnerMapping)
                    || winnerMapping.SideId == null)
                {
                    return Fail(
                        BattleOutcomePlanningFailureCode.OutcomeMappingFailed,
                        "The raw Victory winner cannot be mapped to exactly one typed BattleSideId.",
                        out failure);
                }

                outcomeType = BattleOutcomeType.Victory;
                winner = winnerMapping.SideId;
                break;

            case ConflictOutcomeType.Draw:
                if (computation.RawResult.WinningSideId != null)
                {
                    return Fail(
                        BattleOutcomePlanningFailureCode.InvalidOutcome,
                        "The raw Draw unexpectedly carries a winning side.",
                        out failure);
                }

                outcomeType = BattleOutcomeType.Draw;
                break;

            default:
                return Fail(
                    BattleOutcomePlanningFailureCode.InvalidOutcome,
                    "The raw D4 computation returned an unsupported outcome type.",
                    out failure);
        }

        BattleResolutionProvenance provenance = new BattleResolutionProvenance(
            policy.SemanticFingerprint,
            policy.NumericExecutionProfileKey,
            computation.ProjectionVersion,
            computation.CausalFingerprint,
            computation.SourceContextFingerprint,
            computation.CapabilityRuleKey,
            computation.RandomAuthorityRuleKey,
            policy.ResolverSettingsIdentity);
        outcome = new BattleOutcome(
            computation.BattleId,
            outcomeType,
            winner,
            computation.ExecutionAbsoluteDay,
            provenance);
        return true;
    }

    private static bool Stale(
        IEnumerable<BattleOutcomePlanStalenessReason> reasons,
        string message,
        BattleExecutionFailure executionFailure,
        BattleResolutionComputationFailure computationFailure,
        out BattleOutcomePlanValidationReport report,
        out BattleOutcomePlanningFailure failure)
    {
        report = new BattleOutcomePlanValidationReport(
            BattleOutcomePlanValidationStatus.Stale,
            reasons,
            message);
        failure = new BattleOutcomePlanningFailure(
            BattleOutcomePlanningFailureCode.PlanStale,
            message,
            executionFailure,
            computationFailure);
        return false;
    }

    private static bool Fail(
        BattleOutcomePlanningFailureCode code,
        string message,
        out BattleOutcomePlanningFailure failure)
    {
        failure = new BattleOutcomePlanningFailure(code, message);
        return false;
    }
}
