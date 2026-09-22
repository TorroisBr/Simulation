using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

public interface IBattleContingentCapabilityProvider
{
    /// <summary>
    /// Stable semantic identity for both the rule and all configuration that
    /// can affect its output. This must not be based on runtime object identity.
    /// Implementations composed into a world policy must remain immutable and
    /// pure for that policy's lifetime; changing rule behavior/configuration
    /// requires a new provider and a new RuleKey.
    /// </summary>
    string RuleKey { get; }

    /// <summary>
    /// Purely projects one captured contingent. Implementations must not read
    /// world stores, inspect runtime Persons, consume RNG, or mutate state.
    /// The computation service invokes this only for captured contingents with
    /// positive AvailableAmount; zero-availability direct contingents remain
    /// projected with zero capability without calling the provider. Availability
    /// is not an implicit capability formula: each rule defines its use under
    /// its stable RuleKey.
    /// </summary>
    bool TryEvaluate(
        BattleExecutionContingentSnapshot contingent,
        out float capability,
        out string failureReason);
}

/// <summary>
/// A keyed, stateless random authority used only by raw Battle computation.
/// Identical RuleKey/operationKey pairs must return identical values,
/// independent of call count and evaluation order. RuleKey must identify the
/// seed/authority and algorithm semantics. A source composed into a world
/// policy must remain immutable and stateless for that policy's lifetime;
/// changing its authority or algorithm requires a new source and RuleKey.
/// </summary>
public interface IBattleContextualConflictRandomSource : IContextualConflictRandomSource
{
    string RuleKey { get; }
}

/// <summary>
/// Context-keyed adapter over the project's deterministic random foundation.
/// No shared draw sequence is advanced by a Battle resolution.
/// </summary>
public sealed class DeterministicBattleConflictRandomSource : IBattleContextualConflictRandomSource
{
    private readonly DeterministicRandomSource source;

    public string RuleKey { get; }

    public DeterministicBattleConflictRandomSource(int seed, string authorityKey)
    {
        if (string.IsNullOrWhiteSpace(authorityKey))
        {
            throw new ArgumentException("Battle random authority requires a stable semantic key.", nameof(authorityKey));
        }

        source = new DeterministicRandomSource(seed);
        RuleKey = "battle-contextual-fnv1a64-v1|seed="
            + seed.ToString(CultureInfo.InvariantCulture)
            + "|authority=" + BattleResolutionStableEncoding.Segment(authorityKey);
    }

    public float NextUnit(string operationKey)
    {
        if (string.IsNullOrWhiteSpace(operationKey))
        {
            throw new ArgumentException("A contextual Battle random draw requires an operation key.", nameof(operationKey));
        }

        return source.NextUnit("battle-resolution|" + RuleKey + "|" + operationKey);
    }

    public float NextUnit()
    {
        throw new InvalidOperationException("Battle resolution requires a contextual random operation key.");
    }
}

/// <summary>
/// Explicit, immutable snapshot of the two lower-level resolver parameters
/// that can change a raw Battle result. No resolver defaults are inherited.
/// </summary>
public sealed class BattleResolutionResolverSettings
{
    public float MaxRandomSwingFraction { get; }
    public float DrawMarginFraction { get; }

    public BattleResolutionResolverSettings(
        float maxRandomSwingFraction,
        float drawMarginFraction)
    {
        ValidateFraction(maxRandomSwingFraction, nameof(maxRandomSwingFraction));
        ValidateFraction(drawMarginFraction, nameof(drawMarginFraction));
        MaxRandomSwingFraction = maxRandomSwingFraction;
        DrawMarginFraction = drawMarginFraction;
    }

    internal ConflictResolverSettings CreateLowerLevelCopy()
    {
        ConflictResolverSettings result = new ConflictResolverSettings
        {
            MaxRandomSwingFraction = MaxRandomSwingFraction,
            DrawMarginFraction = DrawMarginFraction
        };
        result.Validate();
        return result;
    }

    private static void ValidateFraction(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value >= 1f)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Resolver fractions must be finite values in [0, 1).");
        }
    }
}

public enum BattleResolutionComputationFailureCode
{
    None = 0,
    ContextInvalid = 1,
    ContextStale = 2,
    RuleIdentityChanged = 3,
    CapabilityProjectionFailed = 4,
    InvalidCapability = 5,
    SideCapabilityOverflow = 6,
    InvalidConflictProjection = 7,
    RawResolutionFailed = 8
}

public sealed class BattleResolutionComputationFailure
{
    internal BattleResolutionComputationFailure(
        BattleResolutionComputationFailureCode code,
        string message,
        BattleExecutionValidationReport validationReport = null,
        BattleExecutionFailure executionFailure = null)
    {
        Code = code;
        Message = message ?? string.Empty;
        ValidationReport = validationReport;
        ExecutionFailure = executionFailure;
    }

    public BattleResolutionComputationFailureCode Code { get; }
    public string Message { get; }
    public BattleExecutionValidationReport ValidationReport { get; }
    public BattleExecutionFailure ExecutionFailure { get; }
    public bool IsFailure => Code != BattleResolutionComputationFailureCode.None;
}

public sealed class BattleResolutionSideMapping
{
    internal BattleResolutionSideMapping(
        string lowerSideId,
        BattleSideId sideId,
        ConflictObjectiveType transitionalObjective,
        ConflictStakes transitionalStakes)
    {
        LowerSideId = lowerSideId ?? throw new ArgumentNullException(nameof(lowerSideId));
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
        TransitionalObjective = transitionalObjective;
        TransitionalStakes = transitionalStakes;
    }

    public string LowerSideId { get; }
    public BattleSideId SideId { get; }
    public ConflictObjectiveType TransitionalObjective { get; }
    public ConflictStakes TransitionalStakes { get; }
}

/// <summary>
/// Typed correspondence for exactly one projected direct contingent.
/// Lower-level IDs are retained as data; consumers never need to parse them.
/// </summary>
public sealed class BattleResolutionParticipantMapping
{
    internal BattleResolutionParticipantMapping(
        string lowerParticipantId,
        string aggregateSourceId,
        BattleSideId sideId,
        ArmedForceId forceId,
        ContingentId contingentId,
        long amount,
        int? aggregateCount,
        float capability)
    {
        LowerParticipantId = lowerParticipantId ?? throw new ArgumentNullException(nameof(lowerParticipantId));
        AggregateSourceId = aggregateSourceId ?? throw new ArgumentNullException(nameof(aggregateSourceId));
        SideId = sideId ?? throw new ArgumentNullException(nameof(sideId));
        ForceId = forceId ?? throw new ArgumentNullException(nameof(forceId));
        ContingentId = contingentId ?? throw new ArgumentNullException(nameof(contingentId));
        Amount = amount;
        AggregateCount = aggregateCount;
        Capability = capability;
    }

    public string LowerParticipantId { get; }
    public string AggregateSourceId { get; }
    public BattleSideId SideId { get; }
    public ArmedForceId ForceId { get; }
    public ContingentId ContingentId { get; }
    public long Amount { get; }
    public int? AggregateCount { get; }
    public float Capability { get; }
}

/// <summary>
/// Immutable, ephemeral output of one validated raw Battle computation.
/// This is not persistent Battle state and does not imply an applied outcome.
/// </summary>
public sealed class BattleResolutionComputation
{
    private readonly IReadOnlyList<BattleResolutionSideMapping> sideMappings;
    private readonly IReadOnlyList<BattleResolutionParticipantMapping> participantMappings;
    private readonly IReadOnlyDictionary<string, BattleResolutionSideMapping> sideMappingsByLowerId;
    private readonly IReadOnlyDictionary<string, BattleResolutionParticipantMapping> participantMappingsByLowerId;

    internal BattleResolutionComputation(
        BattleExecutionContext sourceContext,
        string causalFingerprint,
        string causalFingerprintCanonicalInputs,
        string capabilityRuleKey,
        string randomAuthorityRuleKey,
        BattleResolutionResolverSettings resolverSettings,
        string adaptedConflictId,
        ConflictResolutionResult rawResult,
        IEnumerable<BattleResolutionSideMapping> sides,
        IEnumerable<BattleResolutionParticipantMapping> participants)
    {
        if (sourceContext == null) throw new ArgumentNullException(nameof(sourceContext));
        BattleId = sourceContext.BattleId;
        ExecutionAbsoluteDay = sourceContext.ExecutionAbsoluteDay;
        SourceContextFingerprint = sourceContext.StableKey;
        CausalFingerprint = causalFingerprint ?? throw new ArgumentNullException(nameof(causalFingerprint));
        CausalFingerprintCanonicalInputs = causalFingerprintCanonicalInputs
            ?? throw new ArgumentNullException(nameof(causalFingerprintCanonicalInputs));
        ProjectionVersion = BattleResolutionComputationService.ProjectionVersionValue;
        CapabilityRuleKey = capabilityRuleKey ?? throw new ArgumentNullException(nameof(capabilityRuleKey));
        RandomAuthorityRuleKey = randomAuthorityRuleKey ?? throw new ArgumentNullException(nameof(randomAuthorityRuleKey));
        ResolverSettings = resolverSettings ?? throw new ArgumentNullException(nameof(resolverSettings));
        AdaptedConflictId = adaptedConflictId ?? throw new ArgumentNullException(nameof(adaptedConflictId));
        RawResult = rawResult ?? throw new ArgumentNullException(nameof(rawResult));
        ProjectedLocationRuntimeId = null;
        ProjectedModifierCount = 0;

        List<BattleResolutionSideMapping> sideValues = sides == null
            ? new List<BattleResolutionSideMapping>()
            : new List<BattleResolutionSideMapping>(sides);
        sideValues.Sort((left, right) => StringComparer.Ordinal.Compare(left?.LowerSideId, right?.LowerSideId));
        List<BattleResolutionParticipantMapping> participantValues = participants == null
            ? new List<BattleResolutionParticipantMapping>()
            : new List<BattleResolutionParticipantMapping>(participants);
        participantValues.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.LowerParticipantId,
            right?.LowerParticipantId));

        sideMappings = new ReadOnlyCollection<BattleResolutionSideMapping>(sideValues);
        participantMappings = new ReadOnlyCollection<BattleResolutionParticipantMapping>(participantValues);
        Dictionary<string, BattleResolutionSideMapping> sideMap =
            new Dictionary<string, BattleResolutionSideMapping>(StringComparer.Ordinal);
        foreach (BattleResolutionSideMapping mapping in sideValues)
        {
            sideMap.Add(mapping.LowerSideId, mapping);
        }

        Dictionary<string, BattleResolutionParticipantMapping> participantMap =
            new Dictionary<string, BattleResolutionParticipantMapping>(StringComparer.Ordinal);
        foreach (BattleResolutionParticipantMapping mapping in participantValues)
        {
            participantMap.Add(mapping.LowerParticipantId, mapping);
        }

        sideMappingsByLowerId = new ReadOnlyDictionary<string, BattleResolutionSideMapping>(sideMap);
        participantMappingsByLowerId = new ReadOnlyDictionary<string, BattleResolutionParticipantMapping>(participantMap);
    }

    public BattleId BattleId { get; }
    public long ExecutionAbsoluteDay { get; }
    public string SourceContextFingerprint { get; }
    public string CausalFingerprint { get; }
    public string CausalFingerprintCanonicalInputs { get; }
    public string ProjectionVersion { get; }
    public string CapabilityRuleKey { get; }
    public string RandomAuthorityRuleKey { get; }
    public BattleResolutionResolverSettings ResolverSettings { get; }
    public string AdaptedConflictId { get; }
    public ConflictResolutionResult RawResult { get; }
    public string ProjectedLocationRuntimeId { get; }
    public int ProjectedModifierCount { get; }
    public IReadOnlyList<BattleResolutionSideMapping> SideMappings => sideMappings;
    public IReadOnlyList<BattleResolutionParticipantMapping> ParticipantMappings => participantMappings;

    public bool TryGetSideMapping(string lowerSideId, out BattleResolutionSideMapping mapping)
    {
        return sideMappingsByLowerId.TryGetValue(lowerSideId ?? string.Empty, out mapping);
    }

    public bool TryGetParticipantMapping(
        string lowerParticipantId,
        out BattleResolutionParticipantMapping mapping)
    {
        return participantMappingsByLowerId.TryGetValue(lowerParticipantId ?? string.Empty, out mapping);
    }
}

/// <summary>
/// Validates a D3 context, projects every direct contingent to one lower-level
/// aggregate, and computes a raw ConflictFoundation result. It has no world
/// mutation, lifecycle, event, consequence, or persistence authority.
/// </summary>
public sealed class BattleResolutionComputationService
{
    public const string ProjectionVersionValue = "battle-conflict-projection:v1";

    private readonly BattleExecutionContextBuilder contextBuilder;
    private readonly IBattleContingentCapabilityProvider capabilityProvider;
    private readonly BattleResolutionResolverSettings resolverSettings;
    private readonly IBattleContextualConflictRandomSource randomSource;
    private readonly string capabilityRuleKey;
    private readonly string randomAuthorityRuleKey;

    public BattleResolutionComputationService(
        BattleExecutionContextBuilder contextBuilder,
        IBattleContingentCapabilityProvider capabilityProvider,
        BattleResolutionResolverSettings resolverSettings,
        IBattleContextualConflictRandomSource randomSource)
    {
        this.contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
        this.capabilityProvider = capabilityProvider ?? throw new ArgumentNullException(nameof(capabilityProvider));
        this.resolverSettings = resolverSettings ?? throw new ArgumentNullException(nameof(resolverSettings));
        this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        capabilityRuleKey = RequireRuleKey(capabilityProvider.RuleKey, nameof(capabilityProvider));
        randomAuthorityRuleKey = RequireRuleKey(randomSource.RuleKey, nameof(randomSource));
    }

    public bool TryCompute(
        BattleExecutionContext context,
        long currentAbsoluteDay,
        out BattleResolutionComputation computation,
        out BattleResolutionComputationFailure failure)
    {
        computation = null;
        failure = null;

        // This must remain the first operation: stale or malformed input cannot
        // invoke capability code or reach the random authority.
        if (!contextBuilder.TryValidateCurrent(
            context,
            currentAbsoluteDay,
            out BattleExecutionValidationReport validationReport,
            out BattleExecutionFailure executionFailure))
        {
            BattleResolutionComputationFailureCode code = validationReport != null && validationReport.IsInvalid
                ? BattleResolutionComputationFailureCode.ContextInvalid
                : BattleResolutionComputationFailureCode.ContextStale;
            failure = new BattleResolutionComputationFailure(
                code,
                validationReport?.Message ?? executionFailure?.Message ?? "The Battle execution context is invalid.",
                validationReport,
                executionFailure);
            return false;
        }

        if (!RuleIdentitiesRemainStable())
        {
            failure = Fail(
                BattleResolutionComputationFailureCode.RuleIdentityChanged,
                "A capability or random authority RuleKey changed after service construction.");
            return false;
        }

        List<ProjectedParticipant> projectedParticipants = new List<ProjectedParticipant>();
        Dictionary<string, float> sideTotals = new Dictionary<string, float>(StringComparer.Ordinal);
        List<BattleExecutionSideContext> orderedSides = new List<BattleExecutionSideContext>(context.Sides);
        orderedSides.Sort((left, right) => StringComparer.Ordinal.Compare(
            left?.SideId?.Value,
            right?.SideId?.Value));

        foreach (BattleExecutionSideContext side in orderedSides)
        {
            if (side == null || side.SideId == null)
            {
                failure = Fail(BattleResolutionComputationFailureCode.InvalidConflictProjection, "The validated context contains an invalid Battle side.");
                return false;
            }

            sideTotals.Add(side.SideId.Value, 0f);
            List<BattleExecutionForceContext> orderedForces = new List<BattleExecutionForceContext>(side.Forces);
            orderedForces.Sort((left, right) => StringComparer.Ordinal.Compare(
                left?.ForceId?.Value,
                right?.ForceId?.Value));

            foreach (BattleExecutionForceContext force in orderedForces)
            {
                if (force == null || force.ForceId == null)
                {
                    failure = Fail(BattleResolutionComputationFailureCode.InvalidConflictProjection, "The validated context contains an invalid participant force.");
                    return false;
                }

                List<BattleExecutionContingentSnapshot> orderedContingents =
                    new List<BattleExecutionContingentSnapshot>(force.DirectContingents);
                orderedContingents.Sort((left, right) => StringComparer.Ordinal.Compare(
                    left?.ContingentId?.Value,
                    right?.ContingentId?.Value));

                foreach (BattleExecutionContingentSnapshot contingent in orderedContingents)
                {
                    if (contingent == null || contingent.ContingentId == null || contingent.ForceId == null)
                    {
                        failure = Fail(BattleResolutionComputationFailureCode.InvalidConflictProjection, "The validated context contains an invalid direct contingent.");
                        return false;
                    }

                    float capability = 0f;
                    if (contingent.AvailableAmount > 0L)
                    {
                        string providerFailure;
                        try
                        {
                            if (!capabilityProvider.TryEvaluate(contingent, out capability, out providerFailure))
                            {
                                failure = Fail(
                                    BattleResolutionComputationFailureCode.CapabilityProjectionFailed,
                                    string.IsNullOrWhiteSpace(providerFailure)
                                        ? "The capability provider rejected a contingent projection."
                                        : providerFailure);
                                return false;
                            }
                        }
                        catch (Exception exception)
                        {
                            failure = Fail(
                                BattleResolutionComputationFailureCode.CapabilityProjectionFailed,
                                "The capability provider failed before random evaluation: " + exception.Message);
                            return false;
                        }
                    }

                    if (float.IsNaN(capability) || float.IsInfinity(capability) || capability < 0f)
                    {
                        failure = Fail(
                            BattleResolutionComputationFailureCode.InvalidCapability,
                            "Capability values must be finite and non-negative.");
                        return false;
                    }

                    if (capability == 0f) capability = 0f;
                    float newSideTotal = sideTotals[side.SideId.Value] + capability;
                    if (float.IsNaN(newSideTotal) || float.IsInfinity(newSideTotal))
                    {
                        failure = Fail(
                            BattleResolutionComputationFailureCode.SideCapabilityOverflow,
                            "Projected aggregate capability overflowed the lower-level float representation.");
                        return false;
                    }

                    sideTotals[side.SideId.Value] = newSideTotal;
                    string sourceId = BuildAggregateSourceId(context.BattleId, force.ForceId, contingent.ContingentId);
                    projectedParticipants.Add(new ProjectedParticipant(
                        side.SideId,
                        force.ForceId,
                        contingent,
                        capability,
                        sourceId,
                        "aggregate:" + sourceId));
                }
            }
        }

        if (!RuleIdentitiesRemainStable())
        {
            failure = Fail(
                BattleResolutionComputationFailureCode.RuleIdentityChanged,
                "A capability or random authority RuleKey changed during projection.");
            return false;
        }

        float maximumRandomFactor = 1f + resolverSettings.MaxRandomSwingFraction;
        foreach (KeyValuePair<string, float> sideTotal in sideTotals)
        {
            float maximumPossibleScore = sideTotal.Value * maximumRandomFactor;
            if (float.IsNaN(maximumPossibleScore) || float.IsInfinity(maximumPossibleScore))
            {
                failure = Fail(
                    BattleResolutionComputationFailureCode.SideCapabilityOverflow,
                    "A side's capability could overflow the lower-level score under the captured random swing setting.");
                return false;
            }
        }

        projectedParticipants.Sort(ProjectedParticipant.CompareStable);
        string canonicalInputs = BuildCausalCanonicalInputs(
            context,
            projectedParticipants,
            capabilityRuleKey,
            randomAuthorityRuleKey,
            resolverSettings);
        string causalFingerprint = BattleResolutionStableEncoding.Sha256Key(canonicalInputs);
        string adaptedConflictId = BuildAdaptedConflictId(context, causalFingerprint);
        Conflict adaptedConflict = new Conflict(adaptedConflictId, locationRuntimeId: null);
        List<BattleResolutionSideMapping> sideMappings = new List<BattleResolutionSideMapping>();
        List<BattleResolutionParticipantMapping> participantMappings = new List<BattleResolutionParticipantMapping>();

        foreach (BattleExecutionSideContext side in orderedSides)
        {
            ConflictSide lowerSide = adaptedConflict.AddSide(
                side.SideId.Value,
                ConflictObjectiveType.Other,
                ConflictStakes.Low);
            sideMappings.Add(new BattleResolutionSideMapping(
                lowerSide.SideId,
                side.SideId,
                lowerSide.Objective,
                lowerSide.Stakes));

            foreach (ProjectedParticipant participant in projectedParticipants)
            {
                if (participant.SideId != side.SideId) continue;
                AggregateParticipantSnapshot aggregate = new AggregateParticipantSnapshot(
                    participant.SourceId,
                    participant.Capability,
                    count: null,
                    displayName: participant.SourceId);
                if (!lowerSide.TryAddAggregate(aggregate, out string diagnostic))
                {
                    failure = Fail(
                        BattleResolutionComputationFailureCode.InvalidConflictProjection,
                        diagnostic ?? "The lower-level aggregate participant could not be added.");
                    return false;
                }

                participantMappings.Add(new BattleResolutionParticipantMapping(
                    participant.LowerParticipantId,
                    participant.SourceId,
                    participant.SideId,
                    participant.ForceId,
                    participant.Contingent.ContingentId,
                    participant.Contingent.Amount,
                    aggregate.Count,
                    participant.Capability));
            }
        }

        string conflictDiagnostic = null;
        bool conflictValid = adaptedConflict.TryValidate(out conflictDiagnostic);
        if (adaptedConflict.Modifiers.Count != 0
            || adaptedConflict.LocationRuntimeId != null
            || !conflictValid)
        {
            failure = Fail(
                BattleResolutionComputationFailureCode.InvalidConflictProjection,
                conflictDiagnostic ?? "The lower-level Battle projection violates its transitional boundary.");
            return false;
        }

        ConflictResolver resolver = new ConflictResolver(
            new AggregateOnlyCapabilityModelGuard(),
            randomSource,
            resolverSettings.CreateLowerLevelCopy());
        try
        {
            ConflictResolutionResult rawResult = resolver.Resolve(adaptedConflict, constraints: null);
            computation = new BattleResolutionComputation(
                context,
                causalFingerprint,
                canonicalInputs,
                capabilityRuleKey,
                randomAuthorityRuleKey,
                resolverSettings,
                adaptedConflictId,
                rawResult,
                sideMappings,
                participantMappings);
            failure = null;
            return true;
        }
        catch (Exception exception)
        {
            failure = Fail(
                BattleResolutionComputationFailureCode.RawResolutionFailed,
                "Raw ConflictFoundation resolution failed: " + exception.Message);
            return false;
        }
    }

    private bool RuleIdentitiesRemainStable()
    {
        return string.Equals(capabilityRuleKey, capabilityProvider.RuleKey, StringComparison.Ordinal)
            && string.Equals(randomAuthorityRuleKey, randomSource.RuleKey, StringComparison.Ordinal);
    }

    private static string BuildAggregateSourceId(
        BattleId battleId,
        ArmedForceId forceId,
        ContingentId contingentId)
    {
        return "battle-contingent:v1|b=" + BattleResolutionStableEncoding.Segment(battleId.Value)
            + "|f=" + BattleResolutionStableEncoding.Segment(forceId.Value)
            + "|c=" + BattleResolutionStableEncoding.Segment(contingentId.Value);
    }

    private static string BuildAdaptedConflictId(BattleExecutionContext context, string causalFingerprint)
    {
        return "battle-conflict:v1|b=" + BattleResolutionStableEncoding.Segment(context.BattleId.Value)
            + "|day=" + context.ExecutionAbsoluteDay.ToString(CultureInfo.InvariantCulture)
            + "|causal=" + causalFingerprint;
    }

    private static string BuildCausalCanonicalInputs(
        BattleExecutionContext context,
        IReadOnlyList<ProjectedParticipant> participants,
        string capabilityRuleKey,
        string randomAuthorityRuleKey,
        BattleResolutionResolverSettings settings)
    {
        StringBuilder builder = new StringBuilder();
        BattleResolutionStableEncoding.Append(builder, "projection-version");
        BattleResolutionStableEncoding.Append(builder, ProjectionVersionValue);
        BattleResolutionStableEncoding.Append(builder, "battle-id");
        BattleResolutionStableEncoding.Append(builder, context.BattleId.Value);
        BattleResolutionStableEncoding.Append(builder, "execution-day");
        BattleResolutionStableEncoding.Append(builder, context.ExecutionAbsoluteDay.ToString(CultureInfo.InvariantCulture));
        BattleResolutionStableEncoding.Append(builder, "capability-rule-key");
        BattleResolutionStableEncoding.Append(builder, capabilityRuleKey);
        BattleResolutionStableEncoding.Append(builder, "random-authority-rule-key");
        BattleResolutionStableEncoding.Append(builder, randomAuthorityRuleKey);
        BattleResolutionStableEncoding.Append(builder, "max-random-swing");
        BattleResolutionStableEncoding.Append(builder, BattleResolutionStableEncoding.Float(settings.MaxRandomSwingFraction));
        BattleResolutionStableEncoding.Append(builder, "draw-margin");
        BattleResolutionStableEncoding.Append(builder, BattleResolutionStableEncoding.Float(settings.DrawMarginFraction));

        string lastSideId = null;
        foreach (ProjectedParticipant participant in participants)
        {
            if (!string.Equals(lastSideId, participant.SideId.Value, StringComparison.Ordinal))
            {
                BattleResolutionStableEncoding.Append(builder, "side");
                BattleResolutionStableEncoding.Append(builder, participant.SideId.Value);
                lastSideId = participant.SideId.Value;
            }

            BattleResolutionStableEncoding.Append(builder, "force-id");
            BattleResolutionStableEncoding.Append(builder, participant.ForceId.Value);
            BattleResolutionStableEncoding.Append(builder, "contingent-id");
            BattleResolutionStableEncoding.Append(builder, participant.Contingent.ContingentId.Value);
            BattleResolutionStableEncoding.Append(builder, "contingent-force-id");
            BattleResolutionStableEncoding.Append(builder, participant.Contingent.ForceId.Value);
            BattleResolutionStableEncoding.Append(builder, "amount");
            BattleResolutionStableEncoding.Append(builder, participant.Contingent.Amount.ToString(CultureInfo.InvariantCulture));
            BattleResolutionStableEncoding.Append(builder, "origin-domain");
            BattleResolutionStableEncoding.Append(builder, participant.Contingent.Origin?.Domain);
            BattleResolutionStableEncoding.Append(builder, "origin-value");
            BattleResolutionStableEncoding.Append(builder, participant.Contingent.Origin?.Value);
            BattleResolutionStableEncoding.Append(builder, "service-type");
            BattleResolutionStableEncoding.Append(builder, participant.Contingent.ServiceType);
            foreach (ArmedForceCharacteristic characteristic in participant.Contingent.Characteristics)
            {
                BattleResolutionStableEncoding.Append(builder, "characteristic-key");
                BattleResolutionStableEncoding.Append(builder, characteristic?.Key);
                BattleResolutionStableEncoding.Append(builder, "characteristic-value");
                BattleResolutionStableEncoding.Append(builder, characteristic?.Value);
            }

            BattleResolutionStableEncoding.Append(builder, "projected-capability");
            BattleResolutionStableEncoding.Append(builder, BattleResolutionStableEncoding.Float(participant.Capability));
        }

        return builder.ToString();
    }

    private static string RequireRuleKey(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Battle computation dependencies require stable non-empty RuleKeys.", parameterName);
        }

        return value;
    }

    private static BattleResolutionComputationFailure Fail(
        BattleResolutionComputationFailureCode code,
        string message)
    {
        return new BattleResolutionComputationFailure(code, message);
    }

    private sealed class ProjectedParticipant
    {
        public BattleSideId SideId { get; }
        public ArmedForceId ForceId { get; }
        public BattleExecutionContingentSnapshot Contingent { get; }
        public float Capability { get; }
        public string SourceId { get; }
        public string LowerParticipantId { get; }

        public ProjectedParticipant(
            BattleSideId sideId,
            ArmedForceId forceId,
            BattleExecutionContingentSnapshot contingent,
            float capability,
            string sourceId,
            string lowerParticipantId)
        {
            SideId = sideId;
            ForceId = forceId;
            Contingent = contingent;
            Capability = capability;
            SourceId = sourceId;
            LowerParticipantId = lowerParticipantId;
        }

        public static int CompareStable(ProjectedParticipant left, ProjectedParticipant right)
        {
            int side = StringComparer.Ordinal.Compare(left?.SideId?.Value, right?.SideId?.Value);
            if (side != 0) return side;
            int force = StringComparer.Ordinal.Compare(left?.ForceId?.Value, right?.ForceId?.Value);
            return force != 0
                ? force
                : StringComparer.Ordinal.Compare(left?.Contingent?.ContingentId?.Value, right?.Contingent?.ContingentId?.Value);
        }
    }
}

/// <summary>
/// The raw Battle adapter is aggregate-only. Accidentally adding an NPC to its
/// projection fails immediately instead of invoking a person-level model.
/// </summary>
internal sealed class AggregateOnlyCapabilityModelGuard : ICapabilityModel
{
    public CapabilityEvaluationResult Evaluate(
        NpcRuntime participant,
        CapabilityEvaluationContext context = null)
    {
        throw new InvalidOperationException("Battle raw projection permits aggregate participants only.");
    }
}

internal static class BattleResolutionStableEncoding
{
    public static string Segment(string value)
    {
        if (value == null) return "-1:";
        return value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
    }

    public static void Append(StringBuilder builder, string value)
    {
        builder.Append(Segment(value)).Append(';');
    }

    public static string Float(float value)
    {
        if (value == 0f) return "0";
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    public static string Sha256Key(string canonicalInputs)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(canonicalInputs ?? string.Empty);
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] digest = sha256.ComputeHash(bytes);
            StringBuilder result = new StringBuilder(digest.Length * 2);
            foreach (byte value in digest)
            {
                result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            }

            return "battle-causal:sha256-v1:" + result;
        }
    }
}
