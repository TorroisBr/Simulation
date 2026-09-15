using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class ConflictIdAllocator
{
    private long nextSequence = 1L;

    public string AllocateConflictId()
    {
        if (nextSequence == long.MaxValue)
        {
            throw new InvalidOperationException("ConflictId sequence is exhausted.");
        }

        string conflictId = "conflict-" + nextSequence.ToString("D6", CultureInfo.InvariantCulture);
        nextSequence++;
        return conflictId;
    }
}

public enum ConflictObjectiveType
{
    Defeat,
    Defend,
    Seize,
    Protect,
    Escape,
    Capture,
    Retrieve,
    Other
}

public enum ConflictStakes
{
    Low,
    Meaningful,
    Critical,
    Existential
}

public enum ConflictParticipantKind
{
    Npc,
    Aggregate
}

public enum ConflictOutcomeType
{
    Victory,
    Draw
}

public enum ConflictSideDisposition
{
    Victorious,
    Defeated,
    Stalemate
}

public enum ConflictOutcomeSource
{
    Simulated,
    ExternallyConstrained
}

[Serializable]
public sealed class AggregateParticipantSnapshot
{
    private readonly string sourceId;
    private readonly string displayName;
    private readonly float baseCapability;
    private readonly int? count;

    public string SourceId => sourceId;
    public string DisplayName => displayName;
    public float BaseCapability => baseCapability;
    public int? Count => count;
    public int? OptionalCount => count;

    public AggregateParticipantSnapshot(
        string sourceId,
        float baseCapability,
        int? count = null,
        string displayName = null)
    {
        if (string.IsNullOrWhiteSpace(sourceId) == true)
        {
            throw new ArgumentException("Aggregate participant requires a non-empty SourceId.", nameof(sourceId));
        }

        if (float.IsNaN(baseCapability) == true || float.IsInfinity(baseCapability) == true || baseCapability < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(baseCapability), "Aggregate participant capability must be finite and non-negative.");
        }

        if (count.HasValue == true && count.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Aggregate participant count cannot be negative.");
        }

        this.sourceId = sourceId;
        this.baseCapability = baseCapability;
        this.count = count;
        this.displayName = string.IsNullOrWhiteSpace(displayName) == true ? sourceId : displayName;
    }
}

[Serializable]
public sealed class ConflictParticipantReference
{
    private readonly string participantId;
    private readonly ConflictParticipantKind kind;
    private readonly NpcRuntime npc;
    private readonly AggregateParticipantSnapshot aggregate;

    public string ParticipantId => participantId;
    public ConflictParticipantKind Kind => kind;
    public NpcRuntime Npc => npc;
    public AggregateParticipantSnapshot Aggregate => aggregate;
    public string SourceId => kind == ConflictParticipantKind.Npc ? npc.RuntimeId : aggregate.SourceId;
    public bool IsNpc => kind == ConflictParticipantKind.Npc;
    public bool IsAggregate => kind == ConflictParticipantKind.Aggregate;

    private ConflictParticipantReference(
        string participantId,
        ConflictParticipantKind kind,
        NpcRuntime npc,
        AggregateParticipantSnapshot aggregate)
    {
        this.participantId = RequireId(participantId, nameof(participantId));
        this.kind = kind;
        this.npc = npc;
        this.aggregate = aggregate;
    }

    public static ConflictParticipantReference CreateNpc(NpcRuntime npc)
    {
        if (npc == null)
        {
            throw new ArgumentNullException(nameof(npc));
        }

        return new ConflictParticipantReference(
            npc.RuntimeId,
            ConflictParticipantKind.Npc,
            npc,
            null);
    }

    public static ConflictParticipantReference CreateAggregate(AggregateParticipantSnapshot aggregate)
    {
        if (aggregate == null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        return new ConflictParticipantReference(
            "aggregate:" + aggregate.SourceId,
            ConflictParticipantKind.Aggregate,
            null,
            aggregate);
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Conflict participant requires a non-empty stable ID.", parameterName);
        }

        return value;
    }
}

[Serializable]
public sealed class ConflictSide
{
    private readonly string sideId;
    private readonly ConflictObjectiveType objective;
    private readonly ConflictStakes stakes;
    private readonly List<ConflictParticipantReference> participants = new List<ConflictParticipantReference>();
    private readonly IReadOnlyList<ConflictParticipantReference> readOnlyParticipants;

    public string SideId => sideId;
    public ConflictObjectiveType Objective => objective;
    public ConflictStakes Stakes => stakes;
    public IReadOnlyList<ConflictParticipantReference> Participants => readOnlyParticipants;

    public ConflictSide(
        string sideId,
        ConflictObjectiveType objective,
        ConflictStakes stakes)
    {
        if (string.IsNullOrWhiteSpace(sideId) == true)
        {
            throw new ArgumentException("Conflict side requires a non-empty SideId.", nameof(sideId));
        }

        this.sideId = sideId;
        this.objective = objective;
        this.stakes = stakes;
        readOnlyParticipants = participants.AsReadOnly();
    }

    public void AddNpc(NpcRuntime npc)
    {
        if (TryAddNpc(npc, out string diagnostic) == false)
        {
            throw new InvalidOperationException(diagnostic);
        }
    }

    public bool TryAddNpc(NpcRuntime npc, out string diagnostic)
    {
        if (npc == null)
        {
            diagnostic = "Conflict NPC participant is null.";
            return false;
        }

        return TryAddParticipant(ConflictParticipantReference.CreateNpc(npc), out diagnostic);
    }

    public void AddAggregate(AggregateParticipantSnapshot aggregate)
    {
        if (TryAddAggregate(aggregate, out string diagnostic) == false)
        {
            throw new InvalidOperationException(diagnostic);
        }
    }

    public bool TryAddAggregate(AggregateParticipantSnapshot aggregate, out string diagnostic)
    {
        if (aggregate == null)
        {
            diagnostic = "Conflict aggregate participant is null.";
            return false;
        }

        return TryAddParticipant(ConflictParticipantReference.CreateAggregate(aggregate), out diagnostic);
    }

    public bool TryAddParticipant(ConflictParticipantReference participant, out string diagnostic)
    {
        if (participant == null)
        {
            diagnostic = "Conflict participant is null.";
            return false;
        }

        foreach (ConflictParticipantReference existing in participants)
        {
            if (existing != null && string.Equals(existing.ParticipantId, participant.ParticipantId, StringComparison.Ordinal) == true)
            {
                diagnostic = "Duplicate participant '" + participant.ParticipantId + "' in conflict side '" + sideId + "'.";
                return false;
            }
        }

        participants.Add(participant);
        diagnostic = null;
        return true;
    }
}

[Serializable]
public sealed class ConflictModifier
{
    private readonly string modifierId;
    private readonly string sideId;
    private readonly float additiveContribution;
    private readonly float multiplier;

    public string ModifierId => modifierId;
    public string SideId => sideId;
    public float AdditiveContribution => additiveContribution;
    public float Multiplier => multiplier;

    public ConflictModifier(
        string modifierId,
        string sideId,
        float additiveContribution = 0f,
        float multiplier = 1f)
    {
        if (string.IsNullOrWhiteSpace(modifierId) == true)
        {
            throw new ArgumentException("Conflict modifier requires a non-empty ModifierId.", nameof(modifierId));
        }

        if (string.IsNullOrWhiteSpace(sideId) == false
            && (sideId.IndexOf('\u001f') >= 0 || sideId.IndexOf('\u0000') >= 0))
        {
            throw new ArgumentException("Conflict modifier SideId contains an invalid separator.", nameof(sideId));
        }

        if (float.IsNaN(additiveContribution) == true || float.IsInfinity(additiveContribution) == true
            || float.IsNaN(multiplier) == true || float.IsInfinity(multiplier) == true || multiplier < 0f)
        {
            throw new ArgumentException("Conflict modifier values must be finite and multiplier must be non-negative.");
        }

        this.modifierId = modifierId;
        this.sideId = string.IsNullOrWhiteSpace(sideId) == true ? null : sideId;
        this.additiveContribution = additiveContribution;
        this.multiplier = multiplier;
    }
}

[Serializable]
public sealed class Conflict
{
    private readonly string conflictId;
    private readonly string locationRuntimeId;
    private readonly string originDecisionId;
    private readonly List<ConflictSide> sides = new List<ConflictSide>();
    private readonly List<ConflictModifier> modifiers = new List<ConflictModifier>();
    private readonly IReadOnlyList<ConflictSide> readOnlySides;
    private readonly IReadOnlyList<ConflictModifier> readOnlyModifiers;

    public string ConflictId => conflictId;
    public string LocationRuntimeId => locationRuntimeId;
    public string OriginDecisionId => originDecisionId;
    public IReadOnlyList<ConflictSide> Sides => readOnlySides;
    public IReadOnlyList<ConflictModifier> Modifiers => readOnlyModifiers;

    public Conflict(
        string conflictId,
        string locationRuntimeId = null,
        string originDecisionId = null)
    {
        if (string.IsNullOrWhiteSpace(conflictId) == true)
        {
            throw new ArgumentException("Conflict requires a non-empty ConflictId.", nameof(conflictId));
        }

        this.conflictId = conflictId;
        this.locationRuntimeId = NormalizeOptionalId(locationRuntimeId);
        this.originDecisionId = NormalizeOptionalId(originDecisionId);
        readOnlySides = sides.AsReadOnly();
        readOnlyModifiers = modifiers.AsReadOnly();
    }

    public ConflictSide AddSide(
        string sideId,
        ConflictObjectiveType objective,
        ConflictStakes stakes)
    {
        if (TryAddSide(sideId, objective, stakes, out ConflictSide side, out string diagnostic) == false)
        {
            throw new InvalidOperationException(diagnostic);
        }

        return side;
    }

    public bool TryAddSide(
        string sideId,
        ConflictObjectiveType objective,
        ConflictStakes stakes,
        out ConflictSide side,
        out string diagnostic)
    {
        side = null;
        if (string.IsNullOrWhiteSpace(sideId) == true)
        {
            diagnostic = "Conflict side requires a non-empty SideId.";
            return false;
        }

        foreach (ConflictSide existing in sides)
        {
            if (existing != null && string.Equals(existing.SideId, sideId, StringComparison.Ordinal) == true)
            {
                diagnostic = "Duplicate conflict SideId '" + sideId + "'.";
                return false;
            }
        }

        side = new ConflictSide(sideId, objective, stakes);
        sides.Add(side);
        diagnostic = null;
        return true;
    }

    public void AddModifier(ConflictModifier modifier)
    {
        if (modifier == null)
        {
            throw new ArgumentNullException(nameof(modifier));
        }

        modifiers.Add(modifier);
    }

    public bool TryValidate(out string diagnostic)
    {
        if (string.IsNullOrWhiteSpace(conflictId) == true)
        {
            diagnostic = "ConflictId is empty.";
            return false;
        }

        if (sides.Count < 2)
        {
            diagnostic = "Conflict requires at least two sides.";
            return false;
        }

        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> participantIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, string> npcSideByRuntimeId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ConflictSide side in sides)
        {
            if (side == null || sideIds.Add(side.SideId) == false)
            {
                diagnostic = "Conflict contains a null or duplicate side.";
                return false;
            }

            foreach (ConflictParticipantReference participant in side.Participants)
            {
                if (participant == null)
                {
                    diagnostic = "Conflict contains a null participant.";
                    return false;
                }

                if (participant.IsNpc == true)
                {
                    if (participant.Npc == null || string.IsNullOrWhiteSpace(participant.Npc.RuntimeId) == true)
                    {
                        diagnostic = "Conflict NPC participant must have a valid RuntimeId.";
                        return false;
                    }

                    if (participant.Npc.IsAlive == false)
                    {
                        diagnostic = "Dead NPC '" + participant.Npc.RuntimeId + "' cannot join a new conflict.";
                        return false;
                    }

                    if (npcSideByRuntimeId.TryGetValue(participant.Npc.RuntimeId, out string previousSideId) == true)
                    {
                        diagnostic = "NPC '" + participant.Npc.RuntimeId + "' cannot belong to multiple conflict sides ('"
                            + previousSideId + "' and '" + side.SideId + "').";
                        return false;
                    }
                }
                else if (participant.Aggregate == null)
                {
                    diagnostic = "Conflict aggregate participant must have a snapshot.";
                    return false;
                }

                if (participantIds.Add(participant.ParticipantId) == false)
                {
                    diagnostic = "Conflict participant IDs must be unique.";
                    return false;
                }

                if (participant.IsNpc == true)
                {
                    npcSideByRuntimeId.Add(participant.Npc.RuntimeId, side.SideId);
                }
            }
        }

        foreach (ConflictModifier modifier in modifiers)
        {
            if (modifier == null)
            {
                diagnostic = "Conflict contains a null modifier.";
                return false;
            }

            if (modifier.SideId != null && sideIds.Contains(modifier.SideId) == false)
            {
                diagnostic = "Conflict modifier '" + modifier.ModifierId + "' references an unknown side '" + modifier.SideId + "'.";
                return false;
            }
        }

        diagnostic = null;
        return true;
    }

    private static string NormalizeOptionalId(string value)
    {
        return string.IsNullOrWhiteSpace(value) == true ? null : value;
    }
}

public sealed class ConflictResolutionConstraints
{
    private readonly List<ConflictParticipantResolutionConstraint> participantConstraints = new List<ConflictParticipantResolutionConstraint>();

    public string ForcedWinningSideId { get; set; }
    public ConflictOutcomeType? ForcedOverallOutcome { get; set; }
    public IReadOnlyList<ConflictParticipantResolutionConstraint> ParticipantConstraints => participantConstraints.AsReadOnly();

    public bool HasExternalConstraints => string.IsNullOrWhiteSpace(ForcedWinningSideId) == false
        || ForcedOverallOutcome.HasValue
        || participantConstraints.Count > 0;

    public void AddParticipantConstraint(ConflictParticipantResolutionConstraint constraint)
    {
        if (constraint == null)
        {
            throw new ArgumentNullException(nameof(constraint));
        }

        participantConstraints.Add(constraint);
    }

    public bool TryValidate(Conflict conflict, out string diagnostic)
    {
        if (conflict == null)
        {
            diagnostic = "Conflict constraints require a conflict.";
            return false;
        }

        if (ForcedWinningSideId != null && FindSide(conflict, ForcedWinningSideId) == null)
        {
            diagnostic = "Forced winning side '" + ForcedWinningSideId + "' does not exist in the conflict.";
            return false;
        }

        if (ForcedOverallOutcome == ConflictOutcomeType.Victory && string.IsNullOrWhiteSpace(ForcedWinningSideId) == true)
        {
            diagnostic = "A forced victory requires ForcedWinningSideId.";
            return false;
        }

        if (ForcedOverallOutcome == ConflictOutcomeType.Draw && string.IsNullOrWhiteSpace(ForcedWinningSideId) == false)
        {
            diagnostic = "A forced draw cannot also specify ForcedWinningSideId.";
            return false;
        }

        HashSet<string> participantIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictParticipantResolutionConstraint participantConstraint in participantConstraints)
        {
            if (participantConstraint == null || string.IsNullOrWhiteSpace(participantConstraint.ParticipantId) == true)
            {
                diagnostic = "Conflict participant constraints require a ParticipantId.";
                return false;
            }

            if (participantIds.Add(participantConstraint.ParticipantId) == false)
            {
                diagnostic = "Conflict participant constraints contain duplicate ParticipantId '" + participantConstraint.ParticipantId + "'.";
                return false;
            }

            ConflictParticipantReference participant = FindParticipant(conflict, participantConstraint.ParticipantId);
            if (participant == null)
            {
                diagnostic = "Conflict participant constraint references unknown participant '" + participantConstraint.ParticipantId + "'.";
                return false;
            }

            if (participant.IsAggregate == true
                && (participantConstraint.ForceDeath.HasValue
                    || participantConstraint.ForceAlive.HasValue
                    || participantConstraint.ForcedInjurySeverity.HasValue))
            {
                diagnostic = "Life and injury constraints can only target NPC participants.";
                return false;
            }

            if (participantConstraint.ForceDeath == true && participantConstraint.ForceAlive == true)
            {
                diagnostic = "Conflict participant constraint cannot force both death and life for '" + participantConstraint.ParticipantId + "'.";
                return false;
            }

            if (participantConstraint.ForcedInjurySeverity.HasValue
                && NpcInjuryRules.IsValid(participantConstraint.ForcedInjurySeverity.Value) == false)
            {
                diagnostic = "Conflict participant constraint contains an invalid injury severity.";
                return false;
            }

            if (participantConstraint.ForcedDisposition == ConflictParticipantDisposition.Dead
                && participantConstraint.ForceAlive == true)
            {
                diagnostic = "Conflict participant constraint cannot force a dead disposition and ForceAlive for '" + participantConstraint.ParticipantId + "'.";
                return false;
            }
        }

        diagnostic = null;
        return true;
    }

    private static ConflictSide FindSide(Conflict conflict, string sideId)
    {
        foreach (ConflictSide side in conflict.Sides)
        {
            if (side != null && string.Equals(side.SideId, sideId, StringComparison.Ordinal) == true)
            {
                return side;
            }
        }

        return null;
    }

    private static ConflictParticipantReference FindParticipant(Conflict conflict, string participantId)
    {
        foreach (ConflictSide side in conflict.Sides)
        {
            foreach (ConflictParticipantReference participant in side.Participants)
            {
                if (participant != null && string.Equals(participant.ParticipantId, participantId, StringComparison.Ordinal) == true)
                {
                    return participant;
                }
            }
        }

        return null;
    }
}

public interface IConflictRandomSource
{
    float NextUnit();
}

public sealed class SequenceConflictRandomSource : IConflictRandomSource
{
    private readonly Queue<float> values;
    private readonly float fallbackValue;

    public int ConsumedCount { get; private set; }

    public SequenceConflictRandomSource(IEnumerable<float> values, float fallbackValue = 0.5f)
    {
        this.values = values != null ? new Queue<float>(values) : new Queue<float>();
        if (float.IsNaN(fallbackValue) == true || float.IsInfinity(fallbackValue) == true)
        {
            throw new ArgumentException("Conflict random fallback must be finite.", nameof(fallbackValue));
        }

        this.fallbackValue = ClampUnit(fallbackValue);
    }

    public SequenceConflictRandomSource(params float[] values)
        : this((IEnumerable<float>)values)
    {
    }

    public float NextUnit()
    {
        ConsumedCount++;
        return values.Count > 0 ? ClampUnit(values.Dequeue()) : fallbackValue;
    }

    private static float ClampUnit(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }
}

public sealed class SeededConflictRandomSource : IConflictRandomSource
{
    private readonly System.Random random;

    public SeededConflictRandomSource(int seed)
    {
        random = new System.Random(seed);
    }

    public float NextUnit()
    {
        return (float)random.NextDouble();
    }
}

public sealed class ConflictResolverSettings
{
    public float MaxRandomSwingFraction { get; set; } = 0.2f;
    public float DrawMarginFraction { get; set; } = 0.01f;

    public void Validate()
    {
        if (float.IsNaN(MaxRandomSwingFraction) == true || float.IsInfinity(MaxRandomSwingFraction) == true
            || MaxRandomSwingFraction < 0f || MaxRandomSwingFraction >= 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxRandomSwingFraction), "Random swing must be in [0, 1).");
        }

        if (float.IsNaN(DrawMarginFraction) == true || float.IsInfinity(DrawMarginFraction) == true
            || DrawMarginFraction < 0f || DrawMarginFraction >= 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(DrawMarginFraction), "Draw margin must be in [0, 1).");
        }
    }
}

[Serializable]
public sealed class ConflictParticipantCapabilityResult
{
    private readonly string participantId;
    private readonly string sourceId;
    private readonly ConflictParticipantKind kind;
    private readonly float rawCapability;
    private readonly float effectiveCapability;
    private readonly CapabilityEvaluationResult capabilityEvaluation;

    public string ParticipantId => participantId;
    public string SourceId => sourceId;
    public ConflictParticipantKind Kind => kind;
    public float RawCapability => rawCapability;
    public float EffectiveCapability => effectiveCapability;
    public CapabilityEvaluationResult CapabilityEvaluation => capabilityEvaluation;
    public IReadOnlyList<CapabilityBreakdownEntry> CapabilityBreakdown => capabilityEvaluation?.Breakdown ?? Array.Empty<CapabilityBreakdownEntry>();

    public ConflictParticipantCapabilityResult(
        string participantId,
        string sourceId,
        ConflictParticipantKind kind,
        float rawCapability,
        float effectiveCapability,
        CapabilityEvaluationResult capabilityEvaluation = null)
    {
        this.participantId = RequireId(participantId, nameof(participantId));
        this.sourceId = RequireId(sourceId, nameof(sourceId));
        if (float.IsNaN(rawCapability) == true || float.IsInfinity(rawCapability) == true || rawCapability < 0f
            || float.IsNaN(effectiveCapability) == true || float.IsInfinity(effectiveCapability) == true || effectiveCapability < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(rawCapability), "Conflict participant capability must be finite and non-negative.");
        }

        this.kind = kind;
        this.rawCapability = rawCapability;
        this.effectiveCapability = effectiveCapability;
        this.capabilityEvaluation = capabilityEvaluation;
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Conflict participant capability requires stable IDs.", parameterName);
        }

        return value;
    }
}

[Serializable]
public sealed class ConflictModifierResult
{
    public string ModifierId { get; }
    public string SideId { get; }
    public float AdditiveContribution { get; }
    public float Multiplier { get; }

    public ConflictModifierResult(ConflictModifier modifier)
    {
        if (modifier == null)
        {
            throw new ArgumentNullException(nameof(modifier));
        }

        ModifierId = modifier.ModifierId;
        SideId = modifier.SideId;
        AdditiveContribution = modifier.AdditiveContribution;
        Multiplier = modifier.Multiplier;
    }
}

[Serializable]
public sealed class ConflictSideResolutionResult
{
    private readonly string sideId;
    private readonly float rawCapability;
    private readonly float modifierAdjustedCapability;
    private readonly float randomFactor;
    private readonly float finalScore;
    private readonly ConflictSideDisposition disposition;
    private readonly IReadOnlyList<ConflictParticipantCapabilityResult> participantContributions;
    private readonly IReadOnlyList<ConflictModifierResult> appliedModifiers;

    public string SideId => sideId;
    public float RawCapability => rawCapability;
    public float RawScore => rawCapability;
    public float ModifierAdjustedCapability => modifierAdjustedCapability;
    public float RandomFactor => randomFactor;
    public float FinalScore => finalScore;
    public ConflictSideDisposition Disposition => disposition;
    public IReadOnlyList<ConflictParticipantCapabilityResult> ParticipantContributions => participantContributions;
    public IReadOnlyList<ConflictModifierResult> AppliedModifiers => appliedModifiers;

    public ConflictSideResolutionResult(
        string sideId,
        float rawCapability,
        float modifierAdjustedCapability,
        float randomFactor,
        float finalScore,
        ConflictSideDisposition disposition,
        IReadOnlyList<ConflictParticipantCapabilityResult> participantContributions,
        IReadOnlyList<ConflictModifierResult> appliedModifiers)
    {
        this.sideId = RequireId(sideId, nameof(sideId));
        this.rawCapability = ValidateScore(rawCapability, nameof(rawCapability));
        this.modifierAdjustedCapability = ValidateScore(modifierAdjustedCapability, nameof(modifierAdjustedCapability));
        this.randomFactor = ValidateScore(randomFactor, nameof(randomFactor));
        this.finalScore = ValidateScore(finalScore, nameof(finalScore));
        this.disposition = disposition;
        this.participantContributions = new List<ConflictParticipantCapabilityResult>(participantContributions ?? Array.Empty<ConflictParticipantCapabilityResult>()).AsReadOnly();
        this.appliedModifiers = new List<ConflictModifierResult>(appliedModifiers ?? Array.Empty<ConflictModifierResult>()).AsReadOnly();
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Conflict side result requires a stable SideId.", parameterName);
        }

        return value;
    }

    private static float ValidateScore(float value, string parameterName)
    {
        if (float.IsNaN(value) == true || float.IsInfinity(value) == true || value < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Conflict scores must be finite and non-negative.");
        }

        return value;
    }
}

[Serializable]
public sealed class ConflictResolutionResult
{
    private readonly string conflictId;
    private readonly string winningSideId;
    private readonly ConflictOutcomeType outcome;
    private readonly ConflictOutcomeSource outcomeSource;
    private readonly IReadOnlyList<ConflictSideResolutionResult> sideResults;
    private readonly IReadOnlyList<ConflictNpcConsequence> npcConsequences;
    private readonly IReadOnlyList<ConflictAggregateConsequence> aggregateConsequences;

    public string ConflictId => conflictId;
    public string WinningSideId => winningSideId;
    public ConflictOutcomeType Outcome => outcome;
    public ConflictOutcomeSource OutcomeSource => outcomeSource;
    public bool OutcomeWasExternallyConstrained => outcomeSource == ConflictOutcomeSource.ExternallyConstrained;
    public bool WasSimulated => outcomeSource == ConflictOutcomeSource.Simulated;
    public IReadOnlyList<ConflictSideResolutionResult> SideResults => sideResults;
    public IReadOnlyList<ConflictNpcConsequence> NpcConsequences => npcConsequences;
    public IReadOnlyList<ConflictAggregateConsequence> AggregateConsequences => aggregateConsequences;

    public ConflictResolutionResult(
        string conflictId,
        string winningSideId,
        ConflictOutcomeType outcome,
        ConflictOutcomeSource outcomeSource,
        IReadOnlyList<ConflictSideResolutionResult> sideResults,
        IReadOnlyList<ConflictNpcConsequence> npcConsequences = null,
        IReadOnlyList<ConflictAggregateConsequence> aggregateConsequences = null)
    {
        if (string.IsNullOrWhiteSpace(conflictId) == true)
        {
            throw new ArgumentException("Conflict resolution requires a ConflictId.", nameof(conflictId));
        }

        if (sideResults == null || sideResults.Count < 2)
        {
            throw new ArgumentException("Conflict resolution requires at least two side results.", nameof(sideResults));
        }

        if (outcome == ConflictOutcomeType.Victory && string.IsNullOrWhiteSpace(winningSideId) == true)
        {
            throw new ArgumentException("Conflict victory requires a winning SideId.", nameof(winningSideId));
        }

        if (outcome == ConflictOutcomeType.Draw && string.IsNullOrWhiteSpace(winningSideId) == false)
        {
            throw new ArgumentException("Conflict draw cannot have a winning SideId.", nameof(winningSideId));
        }

        this.conflictId = conflictId;
        this.winningSideId = string.IsNullOrWhiteSpace(winningSideId) == true ? null : winningSideId;
        this.outcome = outcome;
        this.outcomeSource = outcomeSource;
        this.sideResults = new List<ConflictSideResolutionResult>(sideResults).AsReadOnly();
        this.npcConsequences = new List<ConflictNpcConsequence>(npcConsequences ?? Array.Empty<ConflictNpcConsequence>()).AsReadOnly();
        this.aggregateConsequences = new List<ConflictAggregateConsequence>(aggregateConsequences ?? Array.Empty<ConflictAggregateConsequence>()).AsReadOnly();
    }

    public ConflictResolutionResult WithConsequences(
        IReadOnlyList<ConflictNpcConsequence> npcConsequences,
        IReadOnlyList<ConflictAggregateConsequence> aggregateConsequences)
    {
        return new ConflictResolutionResult(
            conflictId,
            winningSideId,
            outcome,
            outcomeSource,
            sideResults,
            npcConsequences,
            aggregateConsequences);
    }
}

public sealed class ConflictResolver
{
    private const float DefaultMaxRandomSwingFraction = 0.2f;
    private readonly ICapabilityModel capabilityModel;
    private readonly IConflictRandomSource randomSource;
    private readonly ConflictResolverSettings settings;

    public ICapabilityModel CapabilityModel => capabilityModel;
    public IConflictRandomSource RandomSource => randomSource;
    public ConflictResolverSettings Settings => settings;

    public ConflictResolver(
        ICapabilityModel capabilityModel,
        IConflictRandomSource randomSource,
        ConflictResolverSettings settings = null)
    {
        this.capabilityModel = capabilityModel ?? throw new ArgumentNullException(nameof(capabilityModel));
        this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        this.settings = settings ?? new ConflictResolverSettings
        {
            MaxRandomSwingFraction = DefaultMaxRandomSwingFraction
        };
        this.settings.Validate();
    }

    public ConflictResolutionResult Resolve(
        Conflict conflict,
        ConflictResolutionConstraints constraints = null)
    {
        if (conflict == null)
        {
            throw new ArgumentNullException(nameof(conflict));
        }

        if (conflict.TryValidate(out string conflictDiagnostic) == false)
        {
            throw new InvalidOperationException(conflictDiagnostic);
        }

        constraints = constraints ?? new ConflictResolutionConstraints();
        if (constraints.TryValidate(conflict, out string constraintsDiagnostic) == false)
        {
            throw new InvalidOperationException(constraintsDiagnostic);
        }

        List<SideComputation> computations = new List<SideComputation>();
        foreach (ConflictSide side in conflict.Sides)
        {
            computations.Add(ComputeSide(conflict, side));
        }

        foreach (SideComputation computation in computations)
        {
            computation.RandomFactor = CalculateRandomFactor();
            computation.FinalScore = computation.ModifierAdjustedCapability * computation.RandomFactor;
        }

        string winningSideId = DetermineSimulatedWinner(computations);
        ConflictOutcomeType outcome = string.IsNullOrWhiteSpace(winningSideId) == true
            ? ConflictOutcomeType.Draw
            : ConflictOutcomeType.Victory;

        if (constraints.ForcedOverallOutcome.HasValue == true)
        {
            outcome = constraints.ForcedOverallOutcome.Value;
            winningSideId = outcome == ConflictOutcomeType.Victory ? constraints.ForcedWinningSideId : null;
        }
        else if (string.IsNullOrWhiteSpace(constraints.ForcedWinningSideId) == false)
        {
            outcome = ConflictOutcomeType.Victory;
            winningSideId = constraints.ForcedWinningSideId;
        }

        ConflictOutcomeSource outcomeSource = constraints.HasExternalConstraints
            ? ConflictOutcomeSource.ExternallyConstrained
            : ConflictOutcomeSource.Simulated;
        List<ConflictSideResolutionResult> sideResults = new List<ConflictSideResolutionResult>();
        foreach (SideComputation computation in computations)
        {
            ConflictSideDisposition disposition;
            if (outcome == ConflictOutcomeType.Draw)
            {
                disposition = ConflictSideDisposition.Stalemate;
            }
            else
            {
                disposition = string.Equals(computation.Side.SideId, winningSideId, StringComparison.Ordinal) == true
                    ? ConflictSideDisposition.Victorious
                    : ConflictSideDisposition.Defeated;
            }

            sideResults.Add(new ConflictSideResolutionResult(
                computation.Side.SideId,
                computation.RawCapability,
                computation.ModifierAdjustedCapability,
                computation.RandomFactor,
                computation.FinalScore,
                disposition,
                computation.ParticipantContributions,
                computation.AppliedModifiers));
        }

        ConflictResolutionResult result = new ConflictResolutionResult(
            conflict.ConflictId,
            winningSideId,
            outcome,
            outcomeSource,
            sideResults);
        ConflictResultValidator.Validate(conflict, result);
        return result;
    }

    private SideComputation ComputeSide(Conflict conflict, ConflictSide side)
    {
        SideComputation computation = new SideComputation(side);
        foreach (ConflictParticipantReference participant in side.Participants)
        {
            if (participant.IsNpc == true)
            {
                CapabilityEvaluationResult evaluation = capabilityModel.Evaluate(participant.Npc);
                if (evaluation == null)
                {
                    throw new InvalidOperationException("Capability model returned a null result for NPC '" + participant.Npc.RuntimeId + "'.");
                }

                computation.ParticipantContributions.Add(new ConflictParticipantCapabilityResult(
                    participant.ParticipantId,
                    participant.Npc.RuntimeId,
                    ConflictParticipantKind.Npc,
                    evaluation.BaseCapability,
                    evaluation.EffectiveCapability,
                    evaluation));
            }
            else
            {
                computation.ParticipantContributions.Add(new ConflictParticipantCapabilityResult(
                    participant.ParticipantId,
                    participant.Aggregate.SourceId,
                    ConflictParticipantKind.Aggregate,
                    participant.Aggregate.BaseCapability,
                    participant.Aggregate.BaseCapability));
            }
        }

        foreach (ConflictParticipantCapabilityResult participantResult in computation.ParticipantContributions)
        {
            computation.RawCapability += participantResult.EffectiveCapability;
        }

        float additiveContribution = 0f;
        float multiplier = 1f;
        foreach (ConflictModifier modifier in conflict.Modifiers)
        {
            if (modifier.SideId != null && string.Equals(modifier.SideId, side.SideId, StringComparison.Ordinal) == false)
            {
                continue;
            }

            additiveContribution += modifier.AdditiveContribution;
            multiplier *= modifier.Multiplier;
            computation.AppliedModifiers.Add(new ConflictModifierResult(modifier));
        }

        computation.ModifierAdjustedCapability = Math.Max(0f, computation.RawCapability + additiveContribution) * Math.Max(0f, multiplier);
        return computation;
    }

    private float CalculateRandomFactor()
    {
        float unit = Math.Max(0f, Math.Min(1f, randomSource.NextUnit()));
        return 1f - settings.MaxRandomSwingFraction + (2f * settings.MaxRandomSwingFraction * unit);
    }

    private string DetermineSimulatedWinner(IReadOnlyList<SideComputation> computations)
    {
        SideComputation best = null;
        SideComputation second = null;
        foreach (SideComputation computation in computations)
        {
            if (best == null || computation.FinalScore > best.FinalScore)
            {
                second = best;
                best = computation;
            }
            else if (second == null || computation.FinalScore > second.FinalScore)
            {
                second = computation;
            }
        }

        if (best == null || second == null)
        {
            return null;
        }

        float scale = Math.Max(1f, Math.Max(best.FinalScore, second.FinalScore));
        float relativeMargin = Math.Abs(best.FinalScore - second.FinalScore) / scale;
        return relativeMargin <= settings.DrawMarginFraction ? null : best.Side.SideId;
    }

    private sealed class SideComputation
    {
        public ConflictSide Side { get; }
        public float RawCapability { get; set; }
        public float ModifierAdjustedCapability { get; set; }
        public float RandomFactor { get; set; }
        public float FinalScore { get; set; }
        public List<ConflictParticipantCapabilityResult> ParticipantContributions { get; } = new List<ConflictParticipantCapabilityResult>();
        public List<ConflictModifierResult> AppliedModifiers { get; } = new List<ConflictModifierResult>();

        public SideComputation(ConflictSide side)
        {
            Side = side;
        }
    }
}

public static class ConflictResultValidator
{
    public static void Validate(Conflict conflict, ConflictResolutionResult result)
    {
        if (conflict == null)
        {
            throw new ArgumentNullException(nameof(conflict));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (string.Equals(conflict.ConflictId, result.ConflictId, StringComparison.Ordinal) == false)
        {
            throw new InvalidOperationException("Conflict result references a different ConflictId.");
        }

        HashSet<string> sideIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictSide side in conflict.Sides)
        {
            sideIds.Add(side.SideId);
        }

        if (result.Outcome == ConflictOutcomeType.Victory && sideIds.Contains(result.WinningSideId) == false)
        {
            throw new InvalidOperationException("Conflict result winner does not reference a conflict side.");
        }

        if (result.Outcome == ConflictOutcomeType.Draw && result.WinningSideId != null)
        {
            throw new InvalidOperationException("Conflict draw cannot reference a winner.");
        }

        HashSet<string> resultSideIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, ConflictParticipantReference> participantsById = new Dictionary<string, ConflictParticipantReference>(StringComparer.Ordinal);
        Dictionary<string, string> participantSideById = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ConflictSide side in conflict.Sides)
        {
            foreach (ConflictParticipantReference participant in side.Participants)
            {
                participantsById[participant.ParticipantId] = participant;
                participantSideById[participant.ParticipantId] = side.SideId;
            }
        }

        foreach (ConflictSideResolutionResult sideResult in result.SideResults)
        {
            if (sideResult == null || sideIds.Contains(sideResult.SideId) == false || resultSideIds.Add(sideResult.SideId) == false)
            {
                throw new InvalidOperationException("Conflict result side IDs must be unique references to the conflict.");
            }

            if (sideResult.Disposition == ConflictSideDisposition.Victorious
                && (result.Outcome != ConflictOutcomeType.Victory || sideResult.SideId != result.WinningSideId))
            {
                throw new InvalidOperationException("Conflict result has an incompatible victorious side disposition.");
            }

            if (sideResult.Disposition == ConflictSideDisposition.Stalemate && result.Outcome != ConflictOutcomeType.Draw)
            {
                throw new InvalidOperationException("Conflict result has a stalemate side disposition without a draw.");
            }

            HashSet<string> sideParticipantIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ConflictParticipantCapabilityResult participant in sideResult.ParticipantContributions)
            {
                if (participant == null
                    || participantsById.TryGetValue(participant.ParticipantId, out ConflictParticipantReference reference) == false
                    || string.Equals(participantSideById[participant.ParticipantId], sideResult.SideId, StringComparison.Ordinal) == false
                    || reference.Kind != participant.Kind
                    || string.Equals(reference.SourceId, participant.SourceId, StringComparison.Ordinal) == false
                    || sideParticipantIds.Add(participant.ParticipantId) == false)
                {
                    throw new InvalidOperationException("Conflict result participant contributions must reference their original participants.");
                }
            }
        }

        if (resultSideIds.Count != sideIds.Count)
        {
            throw new InvalidOperationException("Conflict result must contain every conflict side exactly once.");
        }

        HashSet<string> consequenceParticipantIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictNpcConsequence consequence in result.NpcConsequences)
        {
            if (consequence == null
                || participantsById.TryGetValue(consequence.ParticipantId, out ConflictParticipantReference participant) == false
                || participant.IsNpc == false
                || string.Equals(participantSideById[consequence.ParticipantId], consequence.SideId, StringComparison.Ordinal) == false
                || string.Equals(participant.Npc.RuntimeId, consequence.RuntimeId, StringComparison.Ordinal) == false
                || consequenceParticipantIds.Add(consequence.ParticipantId) == false)
            {
                throw new InvalidOperationException("Conflict result NPC consequences must reference their original participants.");
            }
        }

        foreach (ConflictAggregateConsequence consequence in result.AggregateConsequences)
        {
            if (consequence == null
                || participantsById.TryGetValue(consequence.ParticipantId, out ConflictParticipantReference participant) == false
                || participant.IsAggregate == false
                || string.Equals(participantSideById[consequence.ParticipantId], consequence.SideId, StringComparison.Ordinal) == false
                || string.Equals(participant.Aggregate.SourceId, consequence.SourceId, StringComparison.Ordinal) == false
                || consequenceParticipantIds.Add(consequence.ParticipantId) == false)
            {
                throw new InvalidOperationException("Conflict result aggregate consequences must reference their original participants.");
            }
        }
    }
}
