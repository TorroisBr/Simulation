using System;
using System.Collections.Generic;

[Serializable]
public sealed class NpcTravelStartedEvent : DomainEvent
{
    private readonly string actorRuntimeId;
    private readonly string originLocationRuntimeId;
    private readonly string destinationLocationRuntimeId;
    private readonly string routeRuntimeId;

    public override DomainEventType EventType => DomainEventType.NpcTravelStarted;
    public string ActorRuntimeId => actorRuntimeId;
    public string OriginLocationRuntimeId => originLocationRuntimeId;
    public string DestinationLocationRuntimeId => destinationLocationRuntimeId;
    public string RouteRuntimeId => routeRuntimeId;

    public NpcTravelStartedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        string actorRuntimeId,
        string originLocationRuntimeId,
        string destinationLocationRuntimeId,
        string routeRuntimeId,
        string originDecisionId = null)
        : base(eventId, absoluteDay, recordSequence, originDecisionId)
    {
        this.actorRuntimeId = RequireId(actorRuntimeId, nameof(actorRuntimeId));
        this.originLocationRuntimeId = RequireId(originLocationRuntimeId, nameof(originLocationRuntimeId));
        this.destinationLocationRuntimeId = RequireId(destinationLocationRuntimeId, nameof(destinationLocationRuntimeId));
        this.routeRuntimeId = RequireId(routeRuntimeId, nameof(routeRuntimeId));
    }

    public override IReadOnlyList<DomainEventParticipant> GetParticipants()
    {
        return new[] { new DomainEventParticipant(actorRuntimeId, DomainEventParticipantRole.Actor) };
    }
}

[Serializable]
public sealed class NpcArrestedEvent : DomainEvent
{
    private readonly string actorRuntimeId;
    private readonly string targetRuntimeId;
    private readonly string locationRuntimeId;

    public override DomainEventType EventType => DomainEventType.NpcArrested;
    public string ActorRuntimeId => actorRuntimeId;
    public string TargetRuntimeId => targetRuntimeId;
    public string LocationRuntimeId => locationRuntimeId;

    public NpcArrestedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        string actorRuntimeId,
        string targetRuntimeId,
        string locationRuntimeId,
        string originDecisionId = null)
        : base(eventId, absoluteDay, recordSequence, originDecisionId)
    {
        this.actorRuntimeId = RequireId(actorRuntimeId, nameof(actorRuntimeId));
        this.targetRuntimeId = RequireId(targetRuntimeId, nameof(targetRuntimeId));
        this.locationRuntimeId = RequireId(locationRuntimeId, nameof(locationRuntimeId));
    }

    public override IReadOnlyList<DomainEventParticipant> GetParticipants()
    {
        return new[]
        {
            new DomainEventParticipant(actorRuntimeId, DomainEventParticipantRole.Actor),
            new DomainEventParticipant(targetRuntimeId, DomainEventParticipantRole.Target)
        };
    }
}

[Serializable]
public sealed class NpcEscapedEvent : DomainEvent
{
    private readonly string actorRuntimeId;
    private readonly string locationRuntimeId;

    public override DomainEventType EventType => DomainEventType.NpcEscaped;
    public string ActorRuntimeId => actorRuntimeId;
    public string LocationRuntimeId => locationRuntimeId;

    public NpcEscapedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        string actorRuntimeId,
        string locationRuntimeId,
        string originDecisionId = null)
        : base(eventId, absoluteDay, recordSequence, originDecisionId)
    {
        this.actorRuntimeId = RequireId(actorRuntimeId, nameof(actorRuntimeId));
        this.locationRuntimeId = RequireId(locationRuntimeId, nameof(locationRuntimeId));
    }

    public override IReadOnlyList<DomainEventParticipant> GetParticipants()
    {
        return new[] { new DomainEventParticipant(actorRuntimeId, DomainEventParticipantRole.Actor) };
    }
}

[Serializable]
public sealed class TravelPartyStartedEvent : DomainEvent
{
    private readonly string travelPartyId;
    private readonly string originLocationRuntimeId;
    private readonly string destinationLocationRuntimeId;
    private readonly string routeRuntimeId;
    private readonly int travelDaysTotal;
    private readonly IReadOnlyList<string> travelerRuntimeIds;
    private readonly IReadOnlyList<string> escortRuntimeIds;
    private readonly IReadOnlyList<DomainEventParticipant> participants;

    public override DomainEventType EventType => DomainEventType.TravelPartyStarted;
    public string TravelPartyId => travelPartyId;
    public string OriginLocationRuntimeId => originLocationRuntimeId;
    public string DestinationLocationRuntimeId => destinationLocationRuntimeId;
    public string RouteRuntimeId => routeRuntimeId;
    public int TravelDaysTotal => travelDaysTotal;
    public IReadOnlyList<string> TravelerRuntimeIds => travelerRuntimeIds;
    public IReadOnlyList<string> EscortRuntimeIds => escortRuntimeIds;

    public TravelPartyStartedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        string travelPartyId,
        string originLocationRuntimeId,
        string destinationLocationRuntimeId,
        string routeRuntimeId,
        int travelDaysTotal,
        IEnumerable<string> travelerRuntimeIds,
        IEnumerable<string> escortRuntimeIds,
        string originDecisionId = null)
        : base(eventId, absoluteDay, recordSequence, originDecisionId)
    {
        this.travelPartyId = TravelPartyEventData.RequireId(travelPartyId, nameof(travelPartyId));
        this.originLocationRuntimeId = TravelPartyEventData.RequireId(originLocationRuntimeId, nameof(originLocationRuntimeId));
        this.destinationLocationRuntimeId = TravelPartyEventData.RequireId(destinationLocationRuntimeId, nameof(destinationLocationRuntimeId));
        this.routeRuntimeId = TravelPartyEventData.RequireId(routeRuntimeId, nameof(routeRuntimeId));

        if (travelDaysTotal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(travelDaysTotal));
        }

        this.travelDaysTotal = travelDaysTotal;
        this.travelerRuntimeIds = TravelPartyEventData.CaptureIds(travelerRuntimeIds, nameof(travelerRuntimeIds));
        this.escortRuntimeIds = TravelPartyEventData.CaptureIds(escortRuntimeIds, nameof(escortRuntimeIds));
        if (this.travelerRuntimeIds.Count == 0)
        {
            throw new ArgumentException("Travel party event requires at least one traveler.", nameof(travelerRuntimeIds));
        }
        participants = TravelPartyEventData.BuildParticipants(this.travelerRuntimeIds, this.escortRuntimeIds);
    }

    public override IReadOnlyList<DomainEventParticipant> GetParticipants()
    {
        return participants;
    }
}

[Serializable]
public sealed class ExpeditionStartedEvent : DomainEvent
{
    private readonly string expeditionId;
    private readonly string targetSiteRuntimeId;
    private readonly string originLocationRuntimeId;
    private readonly string targetLocationRuntimeId;
    private readonly string travelPartyId;
    private readonly IReadOnlyList<string> performerRuntimeIds;
    private readonly IReadOnlyList<string> supportRuntimeIds;
    private readonly IReadOnlyList<DomainEventParticipant> participants;

    public override DomainEventType EventType => DomainEventType.ExpeditionStarted;
    public string ExpeditionId => expeditionId;
    public string TargetSiteRuntimeId => targetSiteRuntimeId;
    public string OriginLocationRuntimeId => originLocationRuntimeId;
    public string TargetLocationRuntimeId => targetLocationRuntimeId;
    public string TravelPartyId => travelPartyId;
    public IReadOnlyList<string> PerformerRuntimeIds => performerRuntimeIds;
    public IReadOnlyList<string> SupportRuntimeIds => supportRuntimeIds;

    public ExpeditionStartedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        string expeditionId,
        string targetSiteRuntimeId,
        string originLocationRuntimeId,
        string targetLocationRuntimeId,
        string travelPartyId,
        IEnumerable<string> performerRuntimeIds,
        IEnumerable<string> supportRuntimeIds,
        string originDecisionId = null)
        : base(eventId, absoluteDay, recordSequence, originDecisionId)
    {
        this.expeditionId = TravelPartyEventData.RequireId(expeditionId, nameof(expeditionId));
        this.targetSiteRuntimeId = TravelPartyEventData.RequireId(targetSiteRuntimeId, nameof(targetSiteRuntimeId));
        this.originLocationRuntimeId = TravelPartyEventData.RequireId(originLocationRuntimeId, nameof(originLocationRuntimeId));
        this.targetLocationRuntimeId = TravelPartyEventData.RequireId(targetLocationRuntimeId, nameof(targetLocationRuntimeId));
        this.travelPartyId = TravelPartyEventData.RequireId(travelPartyId, nameof(travelPartyId));
        this.performerRuntimeIds = TravelPartyEventData.CaptureIds(performerRuntimeIds, nameof(performerRuntimeIds));
        this.supportRuntimeIds = TravelPartyEventData.CaptureIds(supportRuntimeIds, nameof(supportRuntimeIds));
        ValidateDistinctRoles(this.performerRuntimeIds, this.supportRuntimeIds);
        if (this.performerRuntimeIds.Count == 0)
        {
            throw new ArgumentException("Expedition event requires at least one Performer.", nameof(performerRuntimeIds));
        }

        participants = TravelPartyEventData.BuildParticipants(this.performerRuntimeIds, this.supportRuntimeIds);
    }

    public override IReadOnlyList<DomainEventParticipant> GetParticipants()
    {
        return participants;
    }

    private static void ValidateDistinctRoles(
        IReadOnlyList<string> performers,
        IReadOnlyList<string> supports)
    {
        HashSet<string> ids = new HashSet<string>(performers, StringComparer.Ordinal);

        foreach (string supportRuntimeId in supports)
        {
            if (ids.Add(supportRuntimeId) == false)
            {
                throw new ArgumentException("Expedition event participant IDs must be unique across roles.", nameof(supports));
            }
        }
    }
}

[Serializable]
public sealed class ExpeditionArrivedAtSiteEvent : DomainEvent
{
    private readonly string expeditionId;
    private readonly string targetSiteRuntimeId;
    private readonly string originLocationRuntimeId;
    private readonly string targetLocationRuntimeId;
    private readonly string travelPartyId;
    private readonly IReadOnlyList<string> performerRuntimeIds;
    private readonly IReadOnlyList<string> supportRuntimeIds;
    private readonly IReadOnlyList<DomainEventParticipant> participants;

    public override DomainEventType EventType => DomainEventType.ExpeditionArrivedAtSite;
    public string ExpeditionId => expeditionId;
    public string TargetSiteRuntimeId => targetSiteRuntimeId;
    public string OriginLocationRuntimeId => originLocationRuntimeId;
    public string TargetLocationRuntimeId => targetLocationRuntimeId;
    public string TravelPartyId => travelPartyId;
    public IReadOnlyList<string> PerformerRuntimeIds => performerRuntimeIds;
    public IReadOnlyList<string> SupportRuntimeIds => supportRuntimeIds;

    public ExpeditionArrivedAtSiteEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        string expeditionId,
        string targetSiteRuntimeId,
        string originLocationRuntimeId,
        string targetLocationRuntimeId,
        string travelPartyId,
        IEnumerable<string> performerRuntimeIds,
        IEnumerable<string> supportRuntimeIds,
        string originDecisionId = null)
        : base(eventId, absoluteDay, recordSequence, originDecisionId)
    {
        this.expeditionId = TravelPartyEventData.RequireId(expeditionId, nameof(expeditionId));
        this.targetSiteRuntimeId = TravelPartyEventData.RequireId(targetSiteRuntimeId, nameof(targetSiteRuntimeId));
        this.originLocationRuntimeId = TravelPartyEventData.RequireId(originLocationRuntimeId, nameof(originLocationRuntimeId));
        this.targetLocationRuntimeId = TravelPartyEventData.RequireId(targetLocationRuntimeId, nameof(targetLocationRuntimeId));
        this.travelPartyId = TravelPartyEventData.RequireId(travelPartyId, nameof(travelPartyId));
        this.performerRuntimeIds = TravelPartyEventData.CaptureIds(performerRuntimeIds, nameof(performerRuntimeIds));
        this.supportRuntimeIds = TravelPartyEventData.CaptureIds(supportRuntimeIds, nameof(supportRuntimeIds));
        ValidateDistinctRoles(this.performerRuntimeIds, this.supportRuntimeIds);
        if (this.performerRuntimeIds.Count == 0)
        {
            throw new ArgumentException("Expedition event requires at least one Performer.", nameof(performerRuntimeIds));
        }

        participants = TravelPartyEventData.BuildParticipants(this.performerRuntimeIds, this.supportRuntimeIds);
    }

    public override IReadOnlyList<DomainEventParticipant> GetParticipants()
    {
        return participants;
    }

    private static void ValidateDistinctRoles(
        IReadOnlyList<string> performers,
        IReadOnlyList<string> supports)
    {
        HashSet<string> ids = new HashSet<string>(performers, StringComparer.Ordinal);

        foreach (string supportRuntimeId in supports)
        {
            if (ids.Add(supportRuntimeId) == false)
            {
                throw new ArgumentException("Expedition event participant IDs must be unique across roles.", nameof(supports));
            }
        }
    }
}

internal static class TravelPartyEventData
{
    public static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Travel party event requires stable IDs.", parameterName);
        }

        return value;
    }

    public static IReadOnlyList<string> CaptureIds(IEnumerable<string> source, string parameterName)
    {
        List<string> snapshot = new List<string>();
        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);

        if (source != null)
        {
            foreach (string runtimeId in source)
            {
                if (string.IsNullOrWhiteSpace(runtimeId) == true || uniqueIds.Add(runtimeId) == false)
                {
                    throw new ArgumentException("Travel party event participant IDs must be non-empty and unique.", parameterName);
                }

                snapshot.Add(runtimeId);
            }
        }

        return snapshot.AsReadOnly();
    }

    public static IReadOnlyList<DomainEventParticipant> BuildParticipants(
        IReadOnlyList<string> travelerRuntimeIds,
        IReadOnlyList<string> escortRuntimeIds)
    {
        List<DomainEventParticipant> snapshot = new List<DomainEventParticipant>();

        foreach (string runtimeId in travelerRuntimeIds)
        {
            snapshot.Add(new DomainEventParticipant(runtimeId, DomainEventParticipantRole.Actor));
        }

        foreach (string runtimeId in escortRuntimeIds)
        {
            snapshot.Add(new DomainEventParticipant(runtimeId, DomainEventParticipantRole.Support));
        }

        return snapshot.AsReadOnly();
    }
}

[Serializable]
public abstract class ExpeditionLifecycleEvent : DomainEvent
{
    private readonly string expeditionId;
    private readonly string targetSiteRuntimeId;
    private readonly string macroLocationRuntimeId;
    private readonly string currentLocalPlaceRuntimeId;
    private readonly ExpeditionObjectiveType objectiveType;
    private readonly int progress;
    private readonly int requiredProgress;
    private readonly string relevantTargetId;
    private readonly IReadOnlyList<string> performerRuntimeIds;
    private readonly IReadOnlyList<string> supportRuntimeIds;
    private readonly IReadOnlyList<DomainEventParticipant> participants;

    public string ExpeditionId => expeditionId;
    public string TargetSiteRuntimeId => targetSiteRuntimeId;
    public string MacroLocationRuntimeId => macroLocationRuntimeId;
    public string CurrentLocalPlaceRuntimeId => currentLocalPlaceRuntimeId;
    public ExpeditionObjectiveType ObjectiveType => objectiveType;
    public int Progress => progress;
    public int RequiredProgress => requiredProgress;
    public string RelevantTargetId => relevantTargetId;
    public IReadOnlyList<string> PerformerRuntimeIds => performerRuntimeIds;
    public IReadOnlyList<string> SupportRuntimeIds => supportRuntimeIds;

    protected ExpeditionLifecycleEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        ExpeditionRuntime expedition,
        string macroLocationRuntimeId,
        string relevantTargetId = null)
        : base(
            eventId,
            absoluteDay,
            recordSequence,
            expedition != null ? expedition.OriginDecisionId : null)
    {
        if (expedition == null)
        {
            throw new ArgumentNullException(nameof(expedition));
        }

        expeditionId = RequireId(expedition.ExpeditionId, nameof(expedition.ExpeditionId));
        targetSiteRuntimeId = RequireId(expedition.TargetSiteRuntimeId, nameof(expedition.TargetSiteRuntimeId));
        this.macroLocationRuntimeId = RequireId(macroLocationRuntimeId, nameof(macroLocationRuntimeId));
        currentLocalPlaceRuntimeId = string.IsNullOrWhiteSpace(expedition.CurrentLocalPlaceRuntimeId)
            ? null
            : expedition.CurrentLocalPlaceRuntimeId;
        objectiveType = expedition.Objective.ObjectiveType;
        progress = expedition.Objective.Progress;
        requiredProgress = expedition.Objective.RequiredProgress;
        this.relevantTargetId = string.IsNullOrWhiteSpace(relevantTargetId) ? null : relevantTargetId;
        performerRuntimeIds = TravelPartyEventData.CaptureIds(
            expedition.PerformerRuntimeIds,
            nameof(expedition.PerformerRuntimeIds));
        supportRuntimeIds = TravelPartyEventData.CaptureIds(
            expedition.SupportRuntimeIds,
            nameof(expedition.SupportRuntimeIds));
        participants = TravelPartyEventData.BuildParticipants(performerRuntimeIds, supportRuntimeIds);
    }

    public override IReadOnlyList<DomainEventParticipant> GetParticipants()
    {
        return participants;
    }
}

[Serializable]
public sealed class ExpeditionExplorationStartedEvent : ExpeditionLifecycleEvent
{
    public override DomainEventType EventType => DomainEventType.ExpeditionExplorationStarted;

    public ExpeditionExplorationStartedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        ExpeditionRuntime expedition,
        string macroLocationRuntimeId)
        : base(eventId, absoluteDay, recordSequence, expedition, macroLocationRuntimeId)
    {
    }
}

[Serializable]
public sealed class ExpeditionAdvancedEvent : ExpeditionLifecycleEvent
{
    public override DomainEventType EventType => DomainEventType.ExpeditionAdvanced;

    public ExpeditionAdvancedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        ExpeditionRuntime expedition,
        string macroLocationRuntimeId,
        string relevantTargetId = null)
        : base(eventId, absoluteDay, recordSequence, expedition, macroLocationRuntimeId, relevantTargetId)
    {
    }
}

[Serializable]
public sealed class ExpeditionObjectiveCompletedEvent : ExpeditionLifecycleEvent
{
    public override DomainEventType EventType => DomainEventType.ExpeditionObjectiveCompleted;

    public ExpeditionObjectiveCompletedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        ExpeditionRuntime expedition,
        string macroLocationRuntimeId,
        string relevantTargetId = null)
        : base(eventId, absoluteDay, recordSequence, expedition, macroLocationRuntimeId, relevantTargetId)
    {
    }
}

[Serializable]
public sealed class ExpeditionReturnStartedEvent : ExpeditionLifecycleEvent
{
    public override DomainEventType EventType => DomainEventType.ExpeditionReturnStarted;

    public ExpeditionReturnStartedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        ExpeditionRuntime expedition,
        string macroLocationRuntimeId)
        : base(eventId, absoluteDay, recordSequence, expedition, macroLocationRuntimeId)
    {
    }
}

[Serializable]
public sealed class ExpeditionCompletedEvent : ExpeditionLifecycleEvent
{
    public override DomainEventType EventType => DomainEventType.ExpeditionCompleted;

    public ExpeditionCompletedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        ExpeditionRuntime expedition,
        string macroLocationRuntimeId)
        : base(eventId, absoluteDay, recordSequence, expedition, macroLocationRuntimeId)
    {
    }
}

public sealed class ConflictResolvedEvent : DomainEvent
{
    private readonly string conflictId;
    private readonly string locationRuntimeId;
    private readonly ConflictResolutionResult resolution;

    public override DomainEventType EventType => DomainEventType.ConflictResolved;
    public string ConflictId => conflictId;
    public string LocationRuntimeId => locationRuntimeId;
    public ConflictResolutionResult Resolution => resolution;
    public string WinningSideId => resolution.WinningSideId;
    public ConflictOutcomeType Outcome => resolution.Outcome;
    public ConflictOutcomeSource OutcomeSource => resolution.OutcomeSource;
    public bool ConsequencesWereExternallyConstrained => resolution.ConsequencesWereExternallyConstrained;
    public IReadOnlyList<ConflictSideResolutionResult> SideResults => resolution.SideResults;
    public IReadOnlyList<ConflictNpcConsequence> NpcConsequences => resolution.NpcConsequences;
    public IReadOnlyList<ConflictAggregateConsequence> AggregateConsequences => resolution.AggregateConsequences;

    public ConflictResolvedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        Conflict conflict,
        ConflictResolutionResult resolution)
        : base(
            eventId,
            absoluteDay,
            recordSequence,
            conflict != null ? conflict.OriginDecisionId : null)
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
        conflictId = conflict.ConflictId;
        locationRuntimeId = conflict.LocationRuntimeId;
        this.resolution = resolution;
    }

    public override IReadOnlyList<DomainEventParticipant> GetParticipants()
    {
        List<DomainEventParticipant> participants = new List<DomainEventParticipant>();
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (ConflictSideResolutionResult sideResult in resolution.SideResults)
        {
            foreach (ConflictParticipantCapabilityResult participant in sideResult.ParticipantContributions)
            {
                if (participant.Kind != ConflictParticipantKind.Npc
                    || runtimeIds.Add(participant.SourceId) == false)
                {
                    continue;
                }

                participants.Add(new DomainEventParticipant(
                    participant.SourceId,
                    DomainEventParticipantRole.Participant));
            }
        }

        return participants.AsReadOnly();
    }
}

[Serializable]
public sealed class BattleResolvedEvent : DomainEvent
{
    public override DomainEventType EventType => DomainEventType.BattleResolved;
    public BattleId BattleId { get; }
    public BattleOutcomeType OutcomeType { get; }
    public BattleSideId WinningBattleSideId { get; }
    public string D5PolicyFingerprint { get; }
    public string D5CausalResolutionFingerprint { get; }
    public string D6B2PolicyFingerprint { get; }
    public string D6B2PlanFingerprint { get; }

    public BattleResolvedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        BattleId battleId,
        BattleOutcomeType outcomeType,
        BattleSideId winningBattleSideId,
        string d5PolicyFingerprint,
        string d5CausalResolutionFingerprint,
        string d6b2PolicyFingerprint,
        string d6b2PlanFingerprint)
        : base(eventId, absoluteDay, recordSequence, null)
    {
        BattleId = battleId ?? throw new ArgumentNullException(nameof(battleId));
        if (!Enum.IsDefined(typeof(BattleOutcomeType), outcomeType))
            throw new ArgumentOutOfRangeException(nameof(outcomeType));
        if (outcomeType == BattleOutcomeType.Victory && winningBattleSideId == null)
            throw new ArgumentException("A BattleResolved victory requires a winning side.", nameof(winningBattleSideId));
        if (outcomeType == BattleOutcomeType.Draw && winningBattleSideId != null)
            throw new ArgumentException("A BattleResolved draw cannot have a winning side.", nameof(winningBattleSideId));
        OutcomeType = outcomeType;
        WinningBattleSideId = winningBattleSideId;
        D5PolicyFingerprint = RequireId(d5PolicyFingerprint, nameof(d5PolicyFingerprint));
        D5CausalResolutionFingerprint = RequireId(d5CausalResolutionFingerprint, nameof(d5CausalResolutionFingerprint));
        D6B2PolicyFingerprint = RequireId(d6b2PolicyFingerprint, nameof(d6b2PolicyFingerprint));
        D6B2PlanFingerprint = RequireId(d6b2PlanFingerprint, nameof(d6b2PlanFingerprint));
    }

    // ArmedForceId and BattleSideId are not NpcRuntimeIds; do not misrepresent
    // military participants through the generic runtime-participant index.
    public override IReadOnlyList<DomainEventParticipant> GetParticipants()
        => Array.Empty<DomainEventParticipant>();
}
