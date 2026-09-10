using System;
using System.Collections.Generic;

[Serializable]
public abstract class DomainEvent
{
    private readonly string eventId;
    private readonly long absoluteDay;

    public string EventId => eventId;
    public long AbsoluteDay => absoluteDay;
    public abstract string EventType { get; }

    protected DomainEvent(string eventId, long absoluteDay)
    {
        if (string.IsNullOrWhiteSpace(eventId) == true)
        {
            throw new ArgumentException("DomainEvent requires a non-empty EventId.", nameof(eventId));
        }

        if (absoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteDay), "DomainEvent AbsoluteDay cannot be negative.");
        }

        this.eventId = eventId;
        this.absoluteDay = absoluteDay;
    }

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

    public override string EventType => nameof(NpcTravelStartedEvent);
    public string ActorRuntimeId => actorRuntimeId;
    public string OriginLocationRuntimeId => originLocationRuntimeId;
    public string DestinationLocationRuntimeId => destinationLocationRuntimeId;
    public string RouteRuntimeId => routeRuntimeId;

    public NpcTravelStartedEvent(
        string eventId,
        long absoluteDay,
        string actorRuntimeId,
        string originLocationRuntimeId,
        string destinationLocationRuntimeId,
        string routeRuntimeId)
        : base(eventId, absoluteDay)
    {
        this.actorRuntimeId = RequireId(actorRuntimeId, nameof(actorRuntimeId));
        this.originLocationRuntimeId = RequireId(originLocationRuntimeId, nameof(originLocationRuntimeId));
        this.destinationLocationRuntimeId = RequireId(destinationLocationRuntimeId, nameof(destinationLocationRuntimeId));
        this.routeRuntimeId = RequireId(routeRuntimeId, nameof(routeRuntimeId));
    }
}

[Serializable]
public sealed class NpcArrivedEvent : DomainEvent
{
    private readonly string actorRuntimeId;
    private readonly string destinationLocationRuntimeId;

    public override string EventType => nameof(NpcArrivedEvent);
    public string ActorRuntimeId => actorRuntimeId;
    public string DestinationLocationRuntimeId => destinationLocationRuntimeId;

    public NpcArrivedEvent(
        string eventId,
        long absoluteDay,
        string actorRuntimeId,
        string destinationLocationRuntimeId)
        : base(eventId, absoluteDay)
    {
        this.actorRuntimeId = RequireId(actorRuntimeId, nameof(actorRuntimeId));
        this.destinationLocationRuntimeId = RequireId(destinationLocationRuntimeId, nameof(destinationLocationRuntimeId));
    }
}

[Serializable]
public sealed class NpcArrestedEvent : DomainEvent
{
    private readonly string actorRuntimeId;
    private readonly string targetRuntimeId;
    private readonly string locationRuntimeId;

    public override string EventType => nameof(NpcArrestedEvent);
    public string ActorRuntimeId => actorRuntimeId;
    public string TargetRuntimeId => targetRuntimeId;
    public string LocationRuntimeId => locationRuntimeId;

    public NpcArrestedEvent(
        string eventId,
        long absoluteDay,
        string actorRuntimeId,
        string targetRuntimeId,
        string locationRuntimeId)
        : base(eventId, absoluteDay)
    {
        this.actorRuntimeId = RequireId(actorRuntimeId, nameof(actorRuntimeId));
        this.targetRuntimeId = RequireId(targetRuntimeId, nameof(targetRuntimeId));
        this.locationRuntimeId = RequireId(locationRuntimeId, nameof(locationRuntimeId));
    }
}

[Serializable]
public sealed class NpcEscapedEvent : DomainEvent
{
    private readonly string actorRuntimeId;
    private readonly string locationRuntimeId;

    public override string EventType => nameof(NpcEscapedEvent);
    public string ActorRuntimeId => actorRuntimeId;
    public string LocationRuntimeId => locationRuntimeId;

    public NpcEscapedEvent(
        string eventId,
        long absoluteDay,
        string actorRuntimeId,
        string locationRuntimeId)
        : base(eventId, absoluteDay)
    {
        this.actorRuntimeId = RequireId(actorRuntimeId, nameof(actorRuntimeId));
        this.locationRuntimeId = RequireId(locationRuntimeId, nameof(locationRuntimeId));
    }
}

public sealed class DomainEventStore
{
    private readonly List<DomainEvent> events = new List<DomainEvent>();
    private readonly IReadOnlyList<DomainEvent> readOnlyEvents;
    private readonly Dictionary<string, DomainEvent> eventsById = new Dictionary<string, DomainEvent>(StringComparer.Ordinal);
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

        if (historyPolicy.ShouldRetain(domainEvent) == true)
        {
            historyStore.Retain(domainEvent);
        }

        return true;
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
    private readonly DomainEventStore eventStore;
    private readonly SimulationLogger logger;

    public DomainEventRecorder(
        RuntimeIdAllocator eventIdAllocator,
        SimulationTime simulationTime,
        DomainEventStore eventStore,
        SimulationLogger logger = null)
    {
        this.eventIdAllocator = eventIdAllocator ?? throw new ArgumentNullException(nameof(eventIdAllocator));
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        this.logger = logger ?? new SimulationLogger(null);
    }

    public bool Record(Func<string, long, DomainEvent> createEvent)
    {
        if (createEvent == null)
        {
            logger.LogError("Cannot record domain event: event factory is null.");
            return false;
        }

        try
        {
            DomainEvent domainEvent = createEvent(eventIdAllocator.AllocateEventId(), simulationTime.AbsoluteDay);
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
