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
        return TryResolveAndApply(conflict, constraints, null, out result, out reason);
    }

    /// <summary>
    /// Resolves and applies a conflict through a world-aware consequence boundary.
    /// The resolver remains pure; the world is used only while applying stateful
    /// consequences such as fatal resident NPC outcomes.
    /// </summary>
    public bool TryResolveAndApply(
        Conflict conflict,
        ConflictResolutionConstraints constraints,
        SimulationRuntime worldRuntime,
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

        return TryApply(conflict, result, worldRuntime, out reason);
    }

    public bool TryApply(
        Conflict conflict,
        ConflictResolutionResult result,
        out string reason)
    {
        return TryApply(conflict, result, null, out reason);
    }

    /// <summary>
    /// Applies a computed result. When a world owner is supplied, fatal resident NPC
    /// consequences are routed through NpcPopulationLifecycleSystem instead of the
    /// low-level resident-death guard.
    /// </summary>
    public bool TryApply(
        Conflict conflict,
        ConflictResolutionResult result,
        SimulationRuntime worldRuntime,
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

        Dictionary<string, NpcPopulationLifecycleTransition> residentDeathTransitions =
            new Dictionary<string, NpcPopulationLifecycleTransition>(StringComparer.Ordinal);
        Dictionary<string, CityRuntime> residentDeathSettlements =
            new Dictionary<string, CityRuntime>(StringComparer.Ordinal);
        Dictionary<string, PersonDeathTransition> residentPersonDeathTransitions =
            new Dictionary<string, PersonDeathTransition>(StringComparer.Ordinal);
        Dictionary<string, PersonDeathTransition> personDeathTransitions =
            new Dictionary<string, PersonDeathTransition>(StringComparer.Ordinal);
        AuthoritativeNpcRoster authoritativeRoster = null;

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

            if (participant.Npc == null || participant.Npc.IsAlive == false)
            {
                reason = "Conflict consequence cannot be applied to an NPC that is not alive or has invalid life state.";
                return false;
            }

            if (consequence.IsDead
                && string.IsNullOrWhiteSpace(participant.Npc.ResidenceSettlementRuntimeId) == false)
            {
                if (worldRuntime == null)
                {
                    reason = "Fatal resident conflict consequence requires an owner-aware world boundary.";
                    return false;
                }

                if (TryFindResidenceSettlement(
                    worldRuntime,
                    participant.Npc,
                    out CityRuntime residenceSettlement,
                    out reason) == false)
                {
                    return false;
                }

                authoritativeRoster = authoritativeRoster ?? worldRuntime.GetAuthoritativeNpcRoster();
                if (NpcPopulationLifecycleSystem.TryProposeResidentDeath(
                    participant.Npc,
                    residenceSettlement,
                    authoritativeRoster,
                    out NpcPopulationLifecycleTransition transition,
                    out NpcPopulationLifecycleFailure lifecycleFailure) == false)
                {
                    reason = "Resident conflict consequence lifecycle proposal was rejected: " + lifecycleFailure + ".";
                    return false;
                }

                if (participant.Npc.BoundPersonRuntime != null)
                {
                    if (worldRuntime.TryProposePersonDeath(
                            participant.Npc.PersonId,
                            out PersonDeathTransition personDeathTransition,
                            out PersonDeathLifecycleFailure personDeathFailure) == false)
                    {
                        reason = "Resident Person-backed conflict death proposal was rejected: " + personDeathFailure + ".";
                        return false;
                    }

                    residentPersonDeathTransitions.Add(consequence.ParticipantId, personDeathTransition);
                }

                residentDeathTransitions.Add(consequence.ParticipantId, transition);
                residentDeathSettlements.Add(consequence.ParticipantId, residenceSettlement);
                continue;
            }

            if (consequence.IsDead && participant.Npc.BoundPersonRuntime != null)
            {
                if (worldRuntime == null)
                {
                    reason = "Fatal non-resident Person-backed conflict consequence requires an owner-aware world boundary.";
                    return false;
                }

                if (worldRuntime.TryProposePersonDeath(
                        participant.Npc.PersonId,
                        out PersonDeathTransition personDeathTransition,
                        out PersonDeathLifecycleFailure personDeathFailure) == false)
                {
                    reason = "Person-backed conflict death proposal was rejected: " + personDeathFailure + ".";
                    return false;
                }

                personDeathTransitions.Add(consequence.ParticipantId, personDeathTransition);
                continue;
            }

            if (participant.Npc.CanApplyConflictConsequence(consequence.InjurySeverity, consequence.IsDead) == false)
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
            if (residentDeathTransitions.ContainsKey(consequence.ParticipantId) == true)
            {
                CityRuntime residenceSettlement = residentDeathSettlements[consequence.ParticipantId];
                if (participant.Npc.IsAlive == false
                    || string.Equals(
                        participant.Npc.ResidenceSettlementRuntimeId,
                        residenceSettlement.RuntimeId,
                        StringComparison.Ordinal) == false)
                {
                    reason = "Resident conflict consequence preconditions changed before lifecycle application.";
                    return false;
                }

                if (residentPersonDeathTransitions.TryGetValue(
                        consequence.ParticipantId,
                        out PersonDeathTransition residentPersonDeathTransition) == true)
                {
                    if (PersonDeathLifecycleSystem.TryValidateDeath(
                            worldRuntime,
                            residentPersonDeathTransition,
                            true,
                            out NpcRuntime materializedNpc,
                            out PersonDeathLifecycleFailure personDeathFailure) == false
                        || ReferenceEquals(materializedNpc, participant.Npc) == false)
                    {
                        reason = "Resident Person-backed conflict consequence preconditions changed before lifecycle application.";
                        return false;
                    }
                }
            }
            else if (personDeathTransitions.ContainsKey(consequence.ParticipantId) == true)
            {
                if (participant.Npc.IsAlive == false
                    || NpcInjuryRules.IsValid(consequence.InjurySeverity) == false)
                {
                    reason = "Person-backed conflict consequence preconditions changed before Person death application.";
                    return false;
                }
            }
            else if (participant.Npc.CanApplyConflictConsequence(consequence.InjurySeverity, consequence.IsDead) == false)
            {
                reason = "Conflict consequence preconditions changed before application.";
                return false;
            }
        }

        HashSet<string> appliedResidentDeathSettlements = new HashSet<string>(StringComparer.Ordinal);
        foreach (ConflictNpcConsequence consequence in result.NpcConsequences)
        {
            ConflictParticipantReference participant = participants[consequence.ParticipantId];
            if (residentDeathTransitions.TryGetValue(
                consequence.ParticipantId,
                out NpcPopulationLifecycleTransition residentDeathTransition) == true)
            {
                CityRuntime residenceSettlement = residentDeathSettlements[consequence.ParticipantId];
                bool usePreparedTransition = appliedResidentDeathSettlements.Add(residenceSettlement.RuntimeId);
                bool lifecycleApplied;
                NpcPopulationLifecycleFailure lifecycleFailure;
                if (usePreparedTransition == true)
                {
                    lifecycleApplied = participant.Npc.BoundPersonRuntime != null
                        ? NpcPopulationLifecycleSystem.TryApplyResidentPersonDeathWithConflictInjury(
                            worldRuntime,
                            participant.Npc,
                            residenceSettlement,
                            authoritativeRoster,
                            consequence.InjurySeverity,
                            residentPersonDeathTransitions[consequence.ParticipantId],
                            residentDeathTransition,
                            out lifecycleFailure)
                        : NpcPopulationLifecycleSystem.TryApplyResidentDeathWithConflictInjury(
                            participant.Npc,
                            residenceSettlement,
                            authoritativeRoster,
                            consequence.InjurySeverity,
                            residentDeathTransition,
                            out lifecycleFailure);
                }
                else
                {
                    lifecycleApplied = participant.Npc.BoundPersonRuntime != null
                        ? NpcPopulationLifecycleSystem.TryApplyResidentPersonDeathWithConflictInjury(
                            worldRuntime,
                            participant.Npc,
                            residenceSettlement,
                            authoritativeRoster,
                            consequence.InjurySeverity,
                            residentPersonDeathTransitions[consequence.ParticipantId],
                            out _,
                            out lifecycleFailure)
                        : NpcPopulationLifecycleSystem.TryApplyResidentDeathWithConflictInjury(
                            participant.Npc,
                            residenceSettlement,
                            authoritativeRoster,
                            consequence.InjurySeverity,
                            out _,
                            out lifecycleFailure);
                }

                if (lifecycleApplied == false)
                {
                    reason = "Resident conflict consequence lifecycle application was rejected: " + lifecycleFailure + ".";
                    return false;
                }

                continue;
            }

            if (personDeathTransitions.TryGetValue(
                    consequence.ParticipantId,
                    out PersonDeathTransition personDeathTransition) == true)
            {
                if (worldRuntime.TryApplyPersonDeathWithConflictInjury(
                        personDeathTransition,
                        consequence.InjurySeverity,
                        out PersonDeathLifecycleFailure personDeathFailure) == false)
                {
                    reason = "Person-backed conflict death application was rejected: " + personDeathFailure + ".";
                    return false;
                }

                continue;
            }

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

    private static bool TryFindResidenceSettlement(
        SimulationRuntime worldRuntime,
        NpcRuntime npc,
        out CityRuntime settlement,
        out string reason)
    {
        settlement = null;
        reason = null;

        if (worldRuntime == null || npc == null)
        {
            reason = "Fatal resident conflict consequence requires an authoritative world and NPC.";
            return false;
        }

        string residenceRuntimeId = npc.ResidenceSettlementRuntimeId;
        if (string.IsNullOrWhiteSpace(residenceRuntimeId) == true)
        {
            reason = "Fatal resident conflict consequence requires a non-empty residence RuntimeId.";
            return false;
        }

        foreach (CityRuntime candidate in worldRuntime.Cities)
        {
            if (candidate != null
                && string.Equals(candidate.RuntimeId, residenceRuntimeId, StringComparison.Ordinal))
            {
                settlement = candidate;
                return true;
            }
        }

        reason = "Fatal resident conflict consequence references a residence settlement that is missing from the authoritative world.";
        return false;
    }
}
