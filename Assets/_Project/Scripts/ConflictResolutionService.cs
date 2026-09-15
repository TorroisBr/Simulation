using System;
using System.Collections.Generic;

public sealed class ConflictResolutionService
{
    private readonly ConflictResolver resolver;
    private readonly IConflictConsequenceResolver consequenceResolver;
    private readonly DomainEventRecorder domainEventRecorder;
    private readonly SimulationLogger logger;

    public ConflictResolver Resolver => resolver;
    public IConflictConsequenceResolver ConsequenceResolver => consequenceResolver;

    public ConflictResolutionService(
        ConflictResolver resolver,
        IConflictConsequenceResolver consequenceResolver = null,
        DomainEventRecorder domainEventRecorder = null,
        SimulationLogger logger = null)
    {
        this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        this.consequenceResolver = consequenceResolver ?? new DefaultConflictConsequenceResolver(resolver.RandomSource);
        this.domainEventRecorder = domainEventRecorder;
        this.logger = logger ?? new SimulationLogger(null);
    }

    public ConflictResolutionResult Compute(
        Conflict conflict,
        ConflictResolutionConstraints constraints = null)
    {
        ConflictResolutionResult resolution = resolver.Resolve(conflict, constraints);
        ConflictConsequenceComputation consequences = consequenceResolver.Compute(conflict, resolution, constraints);
        ConflictResolutionResult result = resolution.WithConsequences(
            consequences.NpcConsequences,
            consequences.AggregateConsequences);
        ConflictResultValidator.Validate(conflict, result);
        return result;
    }

    public bool TryResolveAndApply(
        Conflict conflict,
        ConflictResolutionConstraints constraints,
        out ConflictResolutionResult result,
        out string reason)
    {
        result = null;
        reason = null;

        try
        {
            result = Compute(conflict, constraints);
        }
        catch (ArgumentException exception)
        {
            reason = exception.Message;
            return false;
        }
        catch (InvalidOperationException exception)
        {
            reason = exception.Message;
            return false;
        }

        return TryApply(conflict, result, out reason);
    }

    public bool TryApply(
        Conflict conflict,
        ConflictResolutionResult result,
        out string reason)
    {
        reason = null;

        if (conflict == null || result == null)
        {
            reason = "Conflict application requires a conflict and computed result.";
            return false;
        }

        if (conflict.TryValidate(out reason) == false)
        {
            return false;
        }

        try
        {
            ConflictResultValidator.Validate(conflict, result);
        }
        catch (InvalidOperationException exception)
        {
            reason = exception.Message;
            return false;
        }

        Dictionary<string, ConflictParticipantReference> participants = CaptureParticipants(conflict, out reason);
        if (participants == null)
        {
            return false;
        }

        HashSet<string> npcConsequenceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictNpcConsequence consequence in result.NpcConsequences)
        {
            if (consequence == null
                || participants.TryGetValue(consequence.ParticipantId, out ConflictParticipantReference participant) == false
                || participant.IsNpc == false
                || string.Equals(participant.Npc.RuntimeId, consequence.RuntimeId, StringComparison.Ordinal) == false
                || npcConsequenceIds.Add(consequence.ParticipantId) == false)
            {
                reason = "Conflict result contains an invalid or duplicate NPC consequence.";
                return false;
            }

            if (participant.Npc.IsAlive == false
                || participant.Npc.CanApplyConflictConsequence(consequence.InjurySeverity, consequence.IsDead) == false)
            {
                reason = "Conflict consequence cannot be applied to an NPC that is not alive or has invalid life state.";
                return false;
            }
        }

        HashSet<string> aggregateConsequenceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictAggregateConsequence consequence in result.AggregateConsequences)
        {
            if (consequence == null
                || participants.TryGetValue(consequence.ParticipantId, out ConflictParticipantReference participant) == false
                || participant.IsAggregate == false
                || string.Equals(participant.Aggregate.SourceId, consequence.SourceId, StringComparison.Ordinal) == false
                || aggregateConsequenceIds.Add(consequence.ParticipantId) == false)
            {
                reason = "Conflict result contains an invalid or duplicate aggregate consequence.";
                return false;
            }
        }

        foreach (ConflictParticipantReference participant in participants.Values)
        {
            if (participant.IsNpc == true && npcConsequenceIds.Contains(participant.ParticipantId) == false)
            {
                reason = "Conflict result is missing an NPC consequence for '" + participant.ParticipantId + "'.";
                return false;
            }

            if (participant.IsAggregate == true && aggregateConsequenceIds.Contains(participant.ParticipantId) == false)
            {
                reason = "Conflict result is missing an aggregate consequence for '" + participant.ParticipantId + "'.";
                return false;
            }
        }

        // Every precondition is checked before the first NPC mutation.
        foreach (ConflictNpcConsequence consequence in result.NpcConsequences)
        {
            ConflictParticipantReference participant = participants[consequence.ParticipantId];
            if (participant.Npc.CanApplyConflictConsequence(consequence.InjurySeverity, consequence.IsDead) == false)
            {
                reason = "Conflict consequence preconditions changed before application.";
                return false;
            }
        }

        foreach (ConflictNpcConsequence consequence in result.NpcConsequences)
        {
            ConflictParticipantReference participant = participants[consequence.ParticipantId];
            if (participant.Npc.ApplyConflictConsequence(consequence.InjurySeverity, consequence.IsDead) == false)
            {
                // With the precondition pass above this is unreachable in the single-threaded simulation.
                reason = "Conflict consequence application was rejected.";
                return false;
            }
        }

        bool eventRecorded = domainEventRecorder == null || domainEventRecorder.Record(
            (eventId, absoluteDay, recordSequence) => new ConflictResolvedEvent(
                eventId,
                absoluteDay,
                recordSequence,
                conflict,
                result));
        if (eventRecorded == false)
        {
            logger.LogWarning("ConflictResolved event could not be recorded for ConflictId '" + conflict.ConflictId + "'.");
        }

        return true;
    }

    private static Dictionary<string, ConflictParticipantReference> CaptureParticipants(
        Conflict conflict,
        out string reason)
    {
        Dictionary<string, ConflictParticipantReference> participants = new Dictionary<string, ConflictParticipantReference>(StringComparer.Ordinal);
        foreach (ConflictSide side in conflict.Sides)
        {
            foreach (ConflictParticipantReference participant in side.Participants)
            {
                if (participant == null || participants.ContainsKey(participant.ParticipantId) == true)
                {
                    reason = "Conflict participants must have unique IDs before application.";
                    return null;
                }

                participants.Add(participant.ParticipantId, participant);
            }
        }

        reason = null;
        return participants;
    }
}
