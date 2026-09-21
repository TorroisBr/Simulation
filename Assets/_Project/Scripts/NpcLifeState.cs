using System;
using System.Collections.Generic;

public enum NpcLifeState
{
    Alive,
    Dead
}

public enum NpcInjurySeverity
{
    None,
    Hurt,
    Injured,
    SeriouslyInjured,
    Incapacitated
}

public enum ConflictParticipantDisposition
{
    Active,
    Retreated,
    Escaped,
    Surrendered,
    Captured,
    Incapacitated,
    Dead
}

public sealed class ConflictParticipantResolutionConstraint
{
    public string ParticipantId { get; }
    public NpcInjurySeverity? ForcedInjurySeverity { get; set; }
    public bool? ForceDeath { get; set; }
    public bool? ForceAlive { get; set; }
    public ConflictParticipantDisposition? ForcedDisposition { get; set; }

    public bool? ForceDead
    {
        get => ForceDeath;
        set => ForceDeath = value;
    }

    public ConflictParticipantResolutionConstraint(string participantId)
    {
        if (string.IsNullOrWhiteSpace(participantId) == true)
        {
            throw new ArgumentException("Conflict participant constraint requires a ParticipantId.", nameof(participantId));
        }

        ParticipantId = participantId;
    }
}

[Serializable]
public sealed class ConflictNpcConsequence
{
    public string SideId { get; }
    public string ParticipantId { get; }
    public string RuntimeId { get; }
    public NpcInjurySeverity InjurySeverity { get; }
    public bool IsDead { get; }
    public NpcLifeState ResultingLifeState => IsDead ? NpcLifeState.Dead : NpcLifeState.Alive;
    public ConflictParticipantDisposition Disposition { get; }

    public ConflictNpcConsequence(
        string sideId,
        string participantId,
        string runtimeId,
        NpcInjurySeverity injurySeverity,
        bool isDead,
        ConflictParticipantDisposition disposition)
    {
        SideId = RequireId(sideId, nameof(sideId));
        ParticipantId = RequireId(participantId, nameof(participantId));
        RuntimeId = RequireId(runtimeId, nameof(runtimeId));
        if (NpcInjuryRules.IsValid(injurySeverity) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(injurySeverity));
        }

        InjurySeverity = injurySeverity;
        IsDead = isDead;
        Disposition = isDead ? ConflictParticipantDisposition.Dead : disposition;
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Conflict NPC consequence requires stable IDs.", parameterName);
        }

        return value;
    }
}

[Serializable]
public sealed class ConflictAggregateConsequence
{
    public string SideId { get; }
    public string ParticipantId { get; }
    public string SourceId { get; }
    public float OriginalCapability { get; }
    public float RemainingCapability { get; }
    public float LossFraction { get; }
    public ConflictParticipantDisposition Disposition { get; }

    public ConflictAggregateConsequence(
        string sideId,
        string participantId,
        string sourceId,
        float originalCapability,
        float remainingCapability,
        float lossFraction,
        ConflictParticipantDisposition disposition)
    {
        SideId = RequireId(sideId, nameof(sideId));
        ParticipantId = RequireId(participantId, nameof(participantId));
        SourceId = RequireId(sourceId, nameof(sourceId));
        if (float.IsNaN(originalCapability) == true || float.IsInfinity(originalCapability) == true || originalCapability < 0f
            || float.IsNaN(remainingCapability) == true || float.IsInfinity(remainingCapability) == true || remainingCapability < 0f
            || float.IsNaN(lossFraction) == true || float.IsInfinity(lossFraction) == true || lossFraction < 0f || lossFraction > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(originalCapability), "Aggregate consequence values must be finite and non-negative.");
        }

        SideId = sideId;
        ParticipantId = participantId;
        SourceId = sourceId;
        OriginalCapability = originalCapability;
        RemainingCapability = Math.Min(originalCapability, remainingCapability);
        LossFraction = lossFraction;
        Disposition = disposition;
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Conflict aggregate consequence requires stable IDs.", parameterName);
        }

        return value;
    }
}

[Serializable]
public sealed class ConflictConsequenceComputation
{
    public IReadOnlyList<ConflictNpcConsequence> NpcConsequences { get; }
    public IReadOnlyList<ConflictAggregateConsequence> AggregateConsequences { get; }

    public ConflictConsequenceComputation(
        IReadOnlyList<ConflictNpcConsequence> npcConsequences,
        IReadOnlyList<ConflictAggregateConsequence> aggregateConsequences)
    {
        NpcConsequences = new List<ConflictNpcConsequence>(npcConsequences ?? Array.Empty<ConflictNpcConsequence>()).AsReadOnly();
        AggregateConsequences = new List<ConflictAggregateConsequence>(aggregateConsequences ?? Array.Empty<ConflictAggregateConsequence>()).AsReadOnly();
    }
}

public interface IConflictConsequenceResolver
{
    ConflictConsequenceComputation Compute(
        Conflict conflict,
        ConflictResolutionResult resolution,
        ConflictResolutionConstraints constraints = null);
}

public sealed class DefaultConflictConsequenceResolver : IConflictConsequenceResolver
{
    private readonly IConflictRandomSource randomSource;

    public DefaultConflictConsequenceResolver(IConflictRandomSource randomSource)
    {
        this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
    }

    public ConflictConsequenceComputation Compute(
        Conflict conflict,
        ConflictResolutionResult resolution,
        ConflictResolutionConstraints constraints = null)
    {
        if (conflict == null)
        {
            throw new ArgumentNullException(nameof(conflict));
        }

        if (resolution == null)
        {
            throw new ArgumentNullException(nameof(resolution));
        }

        ConflictResultValidator.Validate(conflict, resolution);
        List<ConflictNpcConsequence> npcConsequences = new List<ConflictNpcConsequence>();
        List<ConflictAggregateConsequence> aggregateConsequences = new List<ConflictAggregateConsequence>();

        foreach (ConflictSideResolutionResult sideResult in resolution.SideResults)
        {
            ConflictSide side = FindSide(conflict, sideResult.SideId);
            float strongestOpposingScore = GetStrongestOpposingScore(resolution, sideResult.SideId);
            float scoreMargin = GetScoreMargin(sideResult.FinalScore, strongestOpposingScore);
            int injuryRank = GetBaseInjuryRank(side.Stakes, sideResult.Disposition, scoreMargin, resolution.Outcome);

            foreach (ConflictParticipantCapabilityResult participantResult in sideResult.ParticipantContributions)
            {
                ConflictParticipantReference participant = FindParticipant(conflict, participantResult.ParticipantId);
                if (participant == null)
                {
                    throw new InvalidOperationException("Conflict consequence computation references an unknown participant.");
                }

                ConflictParticipantResolutionConstraint constraint = FindConstraint(constraints, participantResult.ParticipantId);
                if (participantResult.Kind == ConflictParticipantKind.Npc)
                {
                    int participantInjuryRank = ApplyConstraintInjuryRank(injuryRank, constraint);
                    NpcInjurySeverity injurySeverity = (NpcInjurySeverity)participantInjuryRank;
                    ConflictParticipantDisposition disposition = GetParticipantDisposition(
                        sideResult.Disposition,
                        scoreMargin,
                        participantInjuryRank);
                    bool isDead = ShouldDie(
                        conflict,
                        resolution,
                        side,
                        sideResult,
                        participantResult,
                        scoreMargin,
                        participantInjuryRank);

                    if (constraint != null)
                    {
                        if (constraint.ForceAlive == true)
                        {
                            isDead = false;
                        }

                        if (constraint.ForceDeath == true || constraint.ForcedDisposition == ConflictParticipantDisposition.Dead)
                        {
                            isDead = true;
                        }

                        if (constraint.ForcedDisposition.HasValue == true)
                        {
                            disposition = constraint.ForcedDisposition.Value;
                        }
                    }

                    if (isDead == true)
                    {
                        disposition = ConflictParticipantDisposition.Dead;
                    }
                    else if (disposition == ConflictParticipantDisposition.Dead)
                    {
                        disposition = ConflictParticipantDisposition.Active;
                    }

                    npcConsequences.Add(new ConflictNpcConsequence(
                        sideResult.SideId,
                        participantResult.ParticipantId,
                        participantResult.SourceId,
                        injurySeverity,
                        isDead,
                        disposition));
                }
                else
                {
                    float lossFraction = GetAggregateLossFraction(sideResult, scoreMargin, resolution.Outcome, side.Stakes);
                    ConflictParticipantDisposition disposition = GetParticipantDisposition(
                        sideResult.Disposition,
                        scoreMargin,
                        lossFraction >= 0.8f ? (int)NpcInjurySeverity.Incapacitated : (int)NpcInjurySeverity.None);
                    float originalCapability = participantResult.EffectiveCapability;
                    float remainingCapability = originalCapability * (1f - lossFraction);
                    aggregateConsequences.Add(new ConflictAggregateConsequence(
                        sideResult.SideId,
                        participantResult.ParticipantId,
                        participantResult.SourceId,
                        originalCapability,
                        remainingCapability,
                        lossFraction,
                        disposition));
                }
            }
        }

        return new ConflictConsequenceComputation(npcConsequences, aggregateConsequences);
    }

    private bool ShouldDie(
        Conflict conflict,
        ConflictResolutionResult resolution,
        ConflictSide side,
        ConflictSideResolutionResult sideResult,
        ConflictParticipantCapabilityResult participantResult,
        float scoreMargin,
        int injuryRank)
    {
        if (side.Stakes != ConflictStakes.Existential
            || injuryRank < (int)NpcInjurySeverity.SeriouslyInjured)
        {
            return false;
        }

        float deathChance;
        if (sideResult.Disposition == ConflictSideDisposition.Victorious)
        {
            const float decisiveVictoryMargin = 0.25f;
            if (scoreMargin >= decisiveVictoryMargin)
            {
                return false;
            }

            float closeness = 1f - scoreMargin / decisiveVictoryMargin;
            deathChance = 0.02f + 0.08f * closeness;
        }
        else
        {
            deathChance = Math.Min(0.65f, 0.15f + scoreMargin * 0.25f);
        }

        string operationKey = "fatal-consequence"
            + "|conflict|" + conflict.ConflictId
            + "|outcome|" + resolution.Outcome
            + "|winner|" + (resolution.WinningSideId ?? string.Empty)
            + "|side|" + sideResult.SideId
            + "|participant|" + participantResult.ParticipantId
            + "|kind|npc-death";

        float unit = randomSource is IContextualConflictRandomSource contextualRandomSource
            ? contextualRandomSource.NextUnit(operationKey)
            : randomSource.NextUnit();
        return unit < deathChance;
    }

    private static int GetBaseInjuryRank(
        ConflictStakes stakes,
        ConflictSideDisposition disposition,
        float scoreMargin,
        ConflictOutcomeType outcome)
    {
        int rank = 1 + (int)stakes;

        if (outcome == ConflictOutcomeType.Draw)
        {
            rank = Math.Max(1, rank - 1);
        }
        else if (disposition == ConflictSideDisposition.Victorious && scoreMargin >= 0.35f)
        {
            rank = Math.Max(0, rank - 1);
        }
        else if (disposition == ConflictSideDisposition.Defeated && scoreMargin >= 0.35f)
        {
            rank = Math.Min((int)NpcInjurySeverity.Incapacitated, rank + 1);
        }

        return Math.Max(0, Math.Min((int)NpcInjurySeverity.Incapacitated, rank));
    }

    private static int ApplyConstraintInjuryRank(
        int injuryRank,
        ConflictParticipantResolutionConstraint constraint)
    {
        if (constraint == null || constraint.ForcedInjurySeverity.HasValue == false)
        {
            return injuryRank;
        }

        return (int)constraint.ForcedInjurySeverity.Value;
    }

    private static ConflictParticipantDisposition GetParticipantDisposition(
        ConflictSideDisposition sideDisposition,
        float scoreMargin,
        int injuryRank)
    {
        if (injuryRank >= (int)NpcInjurySeverity.Incapacitated)
        {
            return ConflictParticipantDisposition.Incapacitated;
        }

        if (sideDisposition == ConflictSideDisposition.Defeated)
        {
            if (scoreMargin >= 0.4f)
            {
                return ConflictParticipantDisposition.Retreated;
            }

            if (scoreMargin >= 0.2f)
            {
                return ConflictParticipantDisposition.Surrendered;
            }
        }

        return ConflictParticipantDisposition.Active;
    }

    private static float GetAggregateLossFraction(
        ConflictSideResolutionResult sideResult,
        float scoreMargin,
        ConflictOutcomeType outcome,
        ConflictStakes stakes)
    {
        float loss = 0.05f + (int)stakes * 0.05f;
        if (outcome == ConflictOutcomeType.Draw)
        {
            loss += 0.05f;
        }
        else if (sideResult.Disposition == ConflictSideDisposition.Defeated)
        {
            loss += 0.15f + scoreMargin * 0.5f;
        }
        else
        {
            loss = Math.Max(0f, loss - scoreMargin * 0.15f);
        }

        return Math.Max(0f, Math.Min(0.95f, loss));
    }

    private static float GetStrongestOpposingScore(
        ConflictResolutionResult resolution,
        string sideId)
    {
        float strongest = 0f;
        foreach (ConflictSideResolutionResult sideResult in resolution.SideResults)
        {
            if (sideResult != null && string.Equals(sideResult.SideId, sideId, StringComparison.Ordinal) == false)
            {
                strongest = Math.Max(strongest, sideResult.FinalScore);
            }
        }

        return strongest;
    }

    private static float GetScoreMargin(float ownScore, float opposingScore)
    {
        return Math.Abs(ownScore - opposingScore) / Math.Max(1f, Math.Max(ownScore, opposingScore));
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

        throw new InvalidOperationException("Conflict consequence computation references an unknown side.");
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

    private static ConflictParticipantResolutionConstraint FindConstraint(
        ConflictResolutionConstraints constraints,
        string participantId)
    {
        if (constraints == null)
        {
            return null;
        }

        foreach (ConflictParticipantResolutionConstraint constraint in constraints.ParticipantConstraints)
        {
            if (constraint != null && string.Equals(constraint.ParticipantId, participantId, StringComparison.Ordinal) == true)
            {
                return constraint;
            }
        }

        return null;
    }
}

public static class NpcInjuryRules
{
    public static float GetCapabilityMultiplier(NpcInjurySeverity severity)
    {
        switch (severity)
        {
            case NpcInjurySeverity.Hurt:
                return 0.85f;
            case NpcInjurySeverity.Injured:
                return 0.65f;
            case NpcInjurySeverity.SeriouslyInjured:
                return 0.4f;
            case NpcInjurySeverity.Incapacitated:
                return 0.1f;
            case NpcInjurySeverity.None:
                return 1f;
            default:
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown injury severity.");
        }
    }

    public static bool IsValid(NpcInjurySeverity severity)
    {
        return Enum.IsDefined(typeof(NpcInjurySeverity), severity);
    }

    public static bool IsValid(NpcLifeState lifeState)
    {
        return Enum.IsDefined(typeof(NpcLifeState), lifeState);
    }
}
