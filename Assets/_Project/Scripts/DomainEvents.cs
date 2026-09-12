using System;
using System.Collections.Generic;

public enum DomainEventType
{
    NpcTravelStarted,
    NpcArrived,
    NpcArrested,
    NpcEscaped,
    TravelPartyStarted,
    TravelPartyArrived
}

public enum DomainEventParticipantRole
{
    Actor,
    Support,
    Target,
    Participant
}

[Serializable]
public sealed class DomainEventParticipant
{
    private readonly string runtimeId;
    private readonly DomainEventParticipantRole role;

    public string RuntimeId => runtimeId;
    public DomainEventParticipantRole Role => role;

    public DomainEventParticipant(string runtimeId, DomainEventParticipantRole role)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("Domain event participant requires a RuntimeId.", nameof(runtimeId));
        }

        this.runtimeId = runtimeId;
        this.role = role;
    }
}

[Serializable]
public abstract class DomainEvent
{
    private readonly string eventId;
    private readonly long absoluteDay;
    private readonly long recordSequence;
    private readonly string originDecisionId;

    public string EventId => eventId;
    public long AbsoluteDay => absoluteDay;
    public long RecordSequence => recordSequence;
    public string OriginDecisionId => originDecisionId;
    public abstract DomainEventType EventType { get; }

    protected DomainEvent(string eventId, long absoluteDay, long recordSequence, string originDecisionId)
    {
        if (string.IsNullOrWhiteSpace(eventId) == true)
        {
            throw new ArgumentException("DomainEvent requires a non-empty EventId.", nameof(eventId));
        }

        if (absoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteDay), "DomainEvent AbsoluteDay cannot be negative.");
        }

        if (recordSequence <= 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(recordSequence), "DomainEvent RecordSequence must be positive.");
        }

        this.eventId = eventId;
        this.absoluteDay = absoluteDay;
        this.recordSequence = recordSequence;
        this.originDecisionId = string.IsNullOrWhiteSpace(originDecisionId) == true ? null : originDecisionId;
    }

    public abstract IReadOnlyList<DomainEventParticipant> GetParticipants();

    protected static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Domain event references require non-empty stable IDs.", parameterName);
        }

        return value;
    }
}

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
public sealed class NpcArrivedEvent : DomainEvent
{
    private readonly string actorRuntimeId;
    private readonly string destinationLocationRuntimeId;

    public override DomainEventType EventType => DomainEventType.NpcArrived;
    public string ActorRuntimeId => actorRuntimeId;
    public string DestinationLocationRuntimeId => destinationLocationRuntimeId;

    public NpcArrivedEvent(
        string eventId,
        long absoluteDay,
        long recordSequence,
        string actorRuntimeId,
        string destinationLocationRuntimeId,
        string originDecisionId = null)
        : base(eventId, absoluteDay, recordSequence, originDecisionId)
    {
        this.actorRuntimeId = RequireId(actorRuntimeId, nameof(actorRuntimeId));
        this.destinationLocationRuntimeId = RequireId(destinationLocationRuntimeId, nameof(destinationLocationRuntimeId));
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
public sealed class TravelPartyArrivedEvent : DomainEvent
{
    private readonly string travelPartyId;
    private readonly string originLocationRuntimeId;
    private readonly string destinationLocationRuntimeId;
    private readonly string routeRuntimeId;
    private readonly int travelDaysTotal;
    private readonly IReadOnlyList<string> travelerRuntimeIds;
    private readonly IReadOnlyList<string> escortRuntimeIds;
    private readonly IReadOnlyList<DomainEventParticipant> participants;

    public override DomainEventType EventType => DomainEventType.TravelPartyArrived;
    public string TravelPartyId => travelPartyId;
    public string OriginLocationRuntimeId => originLocationRuntimeId;
    public string DestinationLocationRuntimeId => destinationLocationRuntimeId;
    public string RouteRuntimeId => routeRuntimeId;
    public int TravelDaysTotal => travelDaysTotal;
    public IReadOnlyList<string> TravelerRuntimeIds => travelerRuntimeIds;
    public IReadOnlyList<string> EscortRuntimeIds => escortRuntimeIds;

    public TravelPartyArrivedEvent(
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

public sealed class DomainEventStore
{
    private readonly List<DomainEvent> events = new List<DomainEvent>();
    private readonly IReadOnlyList<DomainEvent> readOnlyEvents;
    private readonly Dictionary<string, DomainEvent> eventsById = new Dictionary<string, DomainEvent>(StringComparer.Ordinal);
    private readonly Dictionary<string, List<DomainEvent>> eventsByParticipant = new Dictionary<string, List<DomainEvent>>(StringComparer.Ordinal);
    private readonly HistoryStore historyStore;
    private readonly HistoryPolicy historyPolicy;
    private readonly SimulationLogger logger;

    public IReadOnlyList<DomainEvent> Events => readOnlyEvents;

    public DomainEventStore(HistoryStore historyStore, HistoryPolicy historyPolicy, SimulationLogger logger = null)
    {
        this.historyStore = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
        this.historyPolicy = historyPolicy ?? throw new ArgumentNullException(nameof(historyPolicy));
        this.logger = logger ?? new SimulationLogger(null);
        readOnlyEvents = events.AsReadOnly();
    }

    public bool Record(DomainEvent domainEvent)
    {
        if (domainEvent == null)
        {
            logger.LogError("Cannot record domain event: event is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(domainEvent.EventId) == true)
        {
            logger.LogError("Cannot record domain event: EventId is empty.");
            return false;
        }

        if (domainEvent.AbsoluteDay < 0L)
        {
            logger.LogError($"Cannot record domain event '{domainEvent.EventId}': AbsoluteDay cannot be negative.");
            return false;
        }

        if (eventsById.ContainsKey(domainEvent.EventId) == true)
        {
            logger.LogError($"Cannot record duplicate domain EventId '{domainEvent.EventId}'.");
            return false;
        }

        eventsById.Add(domainEvent.EventId, domainEvent);
        events.Add(domainEvent);
        IndexParticipants(domainEvent);

        if (historyPolicy.ShouldRetain(domainEvent) == true)
        {
            historyStore.Retain(domainEvent);
        }

        return true;
    }

    public bool TryGetEvent(string eventId, out DomainEvent domainEvent)
    {
        return eventsById.TryGetValue(eventId ?? string.Empty, out domainEvent);
    }

    public IReadOnlyList<DomainEvent> GetEventsForParticipant(string runtimeId)
    {
        return eventsByParticipant.TryGetValue(runtimeId ?? string.Empty, out List<DomainEvent> participantEvents) == true
            ? participantEvents.AsReadOnly()
            : Array.Empty<DomainEvent>();
    }

    private void IndexParticipants(DomainEvent domainEvent)
    {
        HashSet<string> indexedRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<DomainEventParticipant> participants = domainEvent.GetParticipants();

        if (participants == null)
        {
            return;
        }

        foreach (DomainEventParticipant participant in participants)
        {
            if (participant == null
                || string.IsNullOrWhiteSpace(participant.RuntimeId) == true
                || indexedRuntimeIds.Add(participant.RuntimeId) == false)
            {
                continue;
            }

            if (eventsByParticipant.TryGetValue(participant.RuntimeId, out List<DomainEvent> participantEvents) == false)
            {
                participantEvents = new List<DomainEvent>();
                eventsByParticipant.Add(participant.RuntimeId, participantEvents);
            }

            participantEvents.Add(domainEvent);
        }
    }
}

public sealed class HistoryPolicy
{
    public bool ShouldRetain(DomainEvent domainEvent)
    {
        return domainEvent is NpcEscapedEvent;
    }
}

public sealed class HistoryStore
{
    private readonly List<DomainEvent> historicalEvents = new List<DomainEvent>();
    private readonly IReadOnlyList<DomainEvent> readOnlyHistoricalEvents;
    private readonly HashSet<string> retainedEventIds = new HashSet<string>(StringComparer.Ordinal);

    public IReadOnlyList<DomainEvent> HistoricalEvents => readOnlyHistoricalEvents;

    public HistoryStore()
    {
        readOnlyHistoricalEvents = historicalEvents.AsReadOnly();
    }

    internal bool Retain(DomainEvent domainEvent)
    {
        if (domainEvent == null || retainedEventIds.Add(domainEvent.EventId) == false)
        {
            return false;
        }

        historicalEvents.Add(domainEvent);
        return true;
    }
}

public sealed class DomainEventRecorder
{
    private readonly RuntimeIdAllocator eventIdAllocator;
    private readonly SimulationTime simulationTime;
    private readonly SimulationRecordSequence recordSequence;
    private readonly DomainEventStore eventStore;
    private readonly SimulationLogger logger;

    public DomainEventRecorder(
        RuntimeIdAllocator eventIdAllocator,
        SimulationTime simulationTime,
        SimulationRecordSequence recordSequence,
        DomainEventStore eventStore,
        SimulationLogger logger = null)
    {
        this.eventIdAllocator = eventIdAllocator ?? throw new ArgumentNullException(nameof(eventIdAllocator));
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        this.recordSequence = recordSequence ?? throw new ArgumentNullException(nameof(recordSequence));
        this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        this.logger = logger ?? new SimulationLogger(null);
    }

    public bool Record(Func<string, long, long, DomainEvent> createEvent)
    {
        if (createEvent == null)
        {
            logger.LogError("Cannot record domain event: event factory is null.");
            return false;
        }

        try
        {
            DomainEvent domainEvent = createEvent(
                eventIdAllocator.AllocateEventId(),
                simulationTime.AbsoluteDay,
                recordSequence.Allocate());
            return eventStore.Record(domainEvent);
        }
        catch (ArgumentException exception)
        {
            logger.LogError("Cannot create domain event: " + exception.Message);
            return false;
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError("Cannot allocate domain EventId: " + exception.Message);
            return false;
        }
    }
}
