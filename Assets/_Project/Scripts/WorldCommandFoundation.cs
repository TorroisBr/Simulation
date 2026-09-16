using System;
using System.Collections.Generic;
using System.Globalization;

public enum WorldCommandKind
{
    RelocateNpc,
    DeclareStackResource,
    DeclareNotableItem,
    AddLocalPlace,
    AddLocalConnection,
    GrantSiteKnowledge,
    GrantAdventureIntel,
    ResolveConflict,
    PlaceOpposition
}

public enum WorldCommandOrigin
{
    GM,
    Table,
    Scenario,
    ExternalImport,
    System,
    Internal
}

public enum WorldCommandAuthorityMode
{
    Suggest,
    Request,
    Declare,
    ForceOutcome
}

public sealed class WorldCommandIdAllocator
{
    private long nextSequence = 1L;

    public string Allocate()
    {
        if (nextSequence == long.MaxValue)
        {
            throw new InvalidOperationException("World command sequence is exhausted.");
        }

        string id = "world-command-" + nextSequence.ToString("D6", CultureInfo.InvariantCulture);
        nextSequence++;
        return id;
    }
}

public sealed class WorldCommand
{
    public WorldCommandKind Kind { get; }
    public WorldCommandOrigin Origin { get; }
    public WorldCommandAuthorityMode Authority { get; }
    public WorldCommandPayload Payload { get; }

    public WorldCommand(
        WorldCommandKind kind,
        WorldCommandOrigin origin,
        WorldCommandAuthorityMode authority,
        WorldCommandPayload payload)
    {
        if (Enum.IsDefined(typeof(WorldCommandKind), kind) == false
            || Enum.IsDefined(typeof(WorldCommandOrigin), origin) == false
            || Enum.IsDefined(typeof(WorldCommandAuthorityMode), authority) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Kind = kind;
        Origin = origin;
        Authority = authority;
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }
}

public abstract class WorldCommandPayload
{
}

public sealed class WorldContentOwnerReferencePayload
{
    public PlaceContentOwnerKind OwnerKind { get; }
    public string OwnerRuntimeId { get; }
    public string MacroLocationRuntimeId { get; }
    public string TopologyOwnerRuntimeId { get; }

    public WorldContentOwnerReferencePayload(
        PlaceContentOwnerKind ownerKind,
        string ownerRuntimeId,
        string macroLocationRuntimeId,
        string topologyOwnerRuntimeId = null)
    {
        if (Enum.IsDefined(typeof(PlaceContentOwnerKind), ownerKind) == false
            || string.IsNullOrWhiteSpace(ownerRuntimeId) == true
            || string.IsNullOrWhiteSpace(macroLocationRuntimeId) == true)
        {
            throw new ArgumentException("World content owners require a valid kind and stable IDs.");
        }

        OwnerKind = ownerKind;
        OwnerRuntimeId = ownerRuntimeId;
        MacroLocationRuntimeId = macroLocationRuntimeId;
        TopologyOwnerRuntimeId = Normalize(topologyOwnerRuntimeId);
    }

    private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

public sealed class RelocateNpcWorldCommandPayload : WorldCommandPayload
{
    public string NpcRuntimeId { get; }
    public string DestinationMacroLocationRuntimeId { get; }

    public RelocateNpcWorldCommandPayload(string npcRuntimeId, string destinationMacroLocationRuntimeId)
    {
        NpcRuntimeId = RequireId(npcRuntimeId, nameof(npcRuntimeId));
        DestinationMacroLocationRuntimeId = RequireId(destinationMacroLocationRuntimeId, nameof(destinationMacroLocationRuntimeId));
    }

    private static string RequireId(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("RuntimeId is required.", name) : value;
}

public sealed class DeclareStackResourceWorldCommandPayload : WorldCommandPayload
{
    public WorldContentOwnerReferencePayload Owner { get; }
    public string ItemDefinitionId { get; }
    public int Amount { get; }
    public PlaceContentPersistencePolicy PersistencePolicy { get; }
    public int DecayPerDay { get; }
    public float? AverageUnitCost { get; }

    public DeclareStackResourceWorldCommandPayload(
        WorldContentOwnerReferencePayload owner,
        string itemDefinitionId,
        int amount,
        PlaceContentPersistencePolicy persistencePolicy,
        int decayPerDay = 0,
        float? averageUnitCost = null)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        ItemDefinitionId = string.IsNullOrWhiteSpace(itemDefinitionId) ? throw new ArgumentException("Item definition is required.", nameof(itemDefinitionId)) : itemDefinitionId;
        if (amount <= 0 || decayPerDay < 0 || (averageUnitCost.HasValue && (float.IsNaN(averageUnitCost.Value) || float.IsInfinity(averageUnitCost.Value) || averageUnitCost.Value < 0f)))
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Amount = amount;
        PersistencePolicy = persistencePolicy;
        DecayPerDay = decayPerDay;
        AverageUnitCost = averageUnitCost;
    }
}

public sealed class DeclareNotableItemWorldCommandPayload : WorldCommandPayload
{
    public WorldContentOwnerReferencePayload Owner { get; }
    public string ItemDefinitionId { get; }

    public DeclareNotableItemWorldCommandPayload(WorldContentOwnerReferencePayload owner, string itemDefinitionId)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        ItemDefinitionId = string.IsNullOrWhiteSpace(itemDefinitionId) ? throw new ArgumentException("Item definition is required.", nameof(itemDefinitionId)) : itemDefinitionId;
    }
}

public sealed class AddLocalPlaceWorldCommandPayload : WorldCommandPayload
{
    public string TopologyOwnerRuntimeId { get; }
    public string DisplayName { get; }
    public string PlaceTypeDefinitionId { get; }
    public string ParentLocalPlaceRuntimeId { get; }
    public bool IsEntryPoint { get; }

    public AddLocalPlaceWorldCommandPayload(
        string topologyOwnerRuntimeId,
        string displayName,
        string placeTypeDefinitionId = null,
        string parentLocalPlaceRuntimeId = null,
        bool isEntryPoint = false)
    {
        TopologyOwnerRuntimeId = RequireId(topologyOwnerRuntimeId, nameof(topologyOwnerRuntimeId));
        DisplayName = displayName ?? string.Empty;
        PlaceTypeDefinitionId = Normalize(placeTypeDefinitionId);
        ParentLocalPlaceRuntimeId = Normalize(parentLocalPlaceRuntimeId);
        IsEntryPoint = isEntryPoint;
    }

    private static string RequireId(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("RuntimeId is required.", name) : value;
    private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

public sealed class AddLocalConnectionWorldCommandPayload : WorldCommandPayload
{
    public string TopologyOwnerRuntimeId { get; }
    public string OriginLocalPlaceRuntimeId { get; }
    public string DestinationLocalPlaceRuntimeId { get; }
    public float TraversalCost { get; }
    public string ConnectionTypeDefinitionId { get; }

    public AddLocalConnectionWorldCommandPayload(
        string topologyOwnerRuntimeId,
        string originLocalPlaceRuntimeId,
        string destinationLocalPlaceRuntimeId,
        float traversalCost,
        string connectionTypeDefinitionId = null)
    {
        TopologyOwnerRuntimeId = RequireId(topologyOwnerRuntimeId, nameof(topologyOwnerRuntimeId));
        OriginLocalPlaceRuntimeId = RequireId(originLocalPlaceRuntimeId, nameof(originLocalPlaceRuntimeId));
        DestinationLocalPlaceRuntimeId = RequireId(destinationLocalPlaceRuntimeId, nameof(destinationLocalPlaceRuntimeId));
        if (string.Equals(OriginLocalPlaceRuntimeId, DestinationLocalPlaceRuntimeId, StringComparison.Ordinal)
            || LocalTopologyConnectionRuntime.IsValidTraversalCost(traversalCost) == false)
        {
            throw new ArgumentException("A local connection requires distinct places and a positive traversal cost.");
        }

        TraversalCost = traversalCost;
        ConnectionTypeDefinitionId = string.IsNullOrWhiteSpace(connectionTypeDefinitionId) ? null : connectionTypeDefinitionId;
    }

    private static string RequireId(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("RuntimeId is required.", name) : value;
}

public sealed class GrantSiteKnowledgeWorldCommandPayload : WorldCommandPayload
{
    public string NpcRuntimeId { get; }
    public string SiteRuntimeId { get; }
    public ExplorableSiteKnowledgeSource Source { get; }

    public GrantSiteKnowledgeWorldCommandPayload(string npcRuntimeId, string siteRuntimeId, ExplorableSiteKnowledgeSource source = ExplorableSiteKnowledgeSource.InitialScenarioKnowledge)
    {
        NpcRuntimeId = RequireId(npcRuntimeId, nameof(npcRuntimeId));
        SiteRuntimeId = RequireId(siteRuntimeId, nameof(siteRuntimeId));
        Source = source;
    }

    private static string RequireId(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("RuntimeId is required.", name) : value;
}

public enum AdventureIntelDeclarationKind
{
    Opposition,
    NotableItem,
    CommonResource,
    Access
}

public sealed class GrantAdventureIntelWorldCommandPayload : WorldCommandPayload
{
    public string NpcRuntimeId { get; }
    public AdventureIntelDeclarationKind IntelKind { get; }
    public string SiteRuntimeId { get; }
    public string LocalPlaceRuntimeId { get; }
    public string OppositionRuntimeId { get; }
    public AdventureOppositionObservedState? OppositionState { get; }
    public string NotableItemRuntimeId { get; }
    public string ItemDefinitionId { get; }
    public int? ObservedAmount { get; }
    public PlaceAccessState? AccessState { get; }
    public long ObservedDay { get; }
    public long ReceivedDay { get; }
    public AdventureIntelSource Source { get; }

    public GrantAdventureIntelWorldCommandPayload(
        string npcRuntimeId,
        AdventureIntelDeclarationKind intelKind,
        string siteRuntimeId,
        string localPlaceRuntimeId = null,
        string oppositionRuntimeId = null,
        AdventureOppositionObservedState? oppositionState = null,
        string notableItemRuntimeId = null,
        string itemDefinitionId = null,
        int? observedAmount = null,
        PlaceAccessState? accessState = null,
        long observedDay = 0L,
        long receivedDay = 0L,
        AdventureIntelSource source = AdventureIntelSource.InitialScenarioKnowledge)
    {
        NpcRuntimeId = RequireId(npcRuntimeId, nameof(npcRuntimeId));
        SiteRuntimeId = RequireId(siteRuntimeId, nameof(siteRuntimeId));
        if (Enum.IsDefined(typeof(AdventureIntelDeclarationKind), intelKind) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(intelKind));
        }

        if (observedDay < 0L || receivedDay < observedDay || (observedAmount.HasValue && observedAmount.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(observedDay));
        }

        IntelKind = intelKind;
        LocalPlaceRuntimeId = Normalize(localPlaceRuntimeId);
        OppositionRuntimeId = Normalize(oppositionRuntimeId);
        OppositionState = oppositionState;
        NotableItemRuntimeId = Normalize(notableItemRuntimeId);
        ItemDefinitionId = Normalize(itemDefinitionId);
        ObservedAmount = observedAmount;
        AccessState = accessState;
        ObservedDay = observedDay;
        ReceivedDay = receivedDay;
        Source = source;
    }

    private static string RequireId(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("RuntimeId is required.", name) : value;
    private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

[Serializable]
public sealed class WorldConflictParticipantPayload
{
    public string NpcRuntimeId { get; }
    public AggregateParticipantSnapshot Aggregate { get; }

    public bool IsAggregate => Aggregate != null;

    public WorldConflictParticipantPayload(string npcRuntimeId)
    {
        NpcRuntimeId = string.IsNullOrWhiteSpace(npcRuntimeId) ? throw new ArgumentException("NPC RuntimeId is required.", nameof(npcRuntimeId)) : npcRuntimeId;
    }

    public WorldConflictParticipantPayload(AggregateParticipantSnapshot aggregate)
    {
        Aggregate = aggregate ?? throw new ArgumentNullException(nameof(aggregate));
    }
}

[Serializable]
public sealed class WorldConflictSidePayload
{
    private readonly IReadOnlyList<WorldConflictParticipantPayload> participants;
    public string SideId { get; }
    public ConflictObjectiveType Objective { get; }
    public ConflictStakes Stakes { get; }
    public IReadOnlyList<WorldConflictParticipantPayload> Participants => participants;

    public WorldConflictSidePayload(
        string sideId,
        ConflictObjectiveType objective,
        ConflictStakes stakes,
        IEnumerable<WorldConflictParticipantPayload> participants)
    {
        SideId = string.IsNullOrWhiteSpace(sideId) ? throw new ArgumentException("Conflict SideId is required.", nameof(sideId)) : sideId;
        List<WorldConflictParticipantPayload> copy = new List<WorldConflictParticipantPayload>();
        if (participants != null)
        {
            foreach (WorldConflictParticipantPayload participant in participants)
            {
                if (participant != null) copy.Add(participant);
            }
        }

        this.participants = copy.AsReadOnly();
        Objective = objective;
        Stakes = stakes;
    }
}

public sealed class WorldConflictParticipantConstraintPayload
{
    public string ParticipantId { get; }
    public NpcInjurySeverity? ForcedInjurySeverity { get; }
    public bool? ForceDeath { get; }
    public bool? ForceAlive { get; }
    public ConflictParticipantDisposition? ForcedDisposition { get; }

    public WorldConflictParticipantConstraintPayload(
        string participantId,
        NpcInjurySeverity? forcedInjurySeverity = null,
        bool? forceDeath = null,
        bool? forceAlive = null,
        ConflictParticipantDisposition? forcedDisposition = null)
    {
        ParticipantId = string.IsNullOrWhiteSpace(participantId) ? throw new ArgumentException("ParticipantId is required.", nameof(participantId)) : participantId;
        ForcedInjurySeverity = forcedInjurySeverity;
        ForceDeath = forceDeath;
        ForceAlive = forceAlive;
        ForcedDisposition = forcedDisposition;
    }
}

public sealed class ResolveConflictWorldCommandPayload : WorldCommandPayload
{
    private readonly IReadOnlyList<WorldConflictSidePayload> sides;
    private readonly IReadOnlyList<WorldConflictParticipantConstraintPayload> constraints;

    public string LocationRuntimeId { get; }
    public string OppositionRuntimeId { get; }
    public IReadOnlyList<WorldConflictSidePayload> Sides => sides;
    public IReadOnlyList<WorldConflictParticipantConstraintPayload> Constraints => constraints;
    public string ForcedWinningSideId { get; }
    public ConflictOutcomeType? ForcedOverallOutcome { get; }

    public ResolveConflictWorldCommandPayload(
        string locationRuntimeId,
        IEnumerable<WorldConflictSidePayload> sides,
        string oppositionRuntimeId = null,
        string forcedWinningSideId = null,
        ConflictOutcomeType? forcedOverallOutcome = null,
        IEnumerable<WorldConflictParticipantConstraintPayload> constraints = null)
    {
        LocationRuntimeId = string.IsNullOrWhiteSpace(locationRuntimeId) ? null : locationRuntimeId;
        OppositionRuntimeId = string.IsNullOrWhiteSpace(oppositionRuntimeId) ? null : oppositionRuntimeId;
        this.sides = Capture(sides);
        this.constraints = Capture(constraints);
        ForcedWinningSideId = string.IsNullOrWhiteSpace(forcedWinningSideId) ? null : forcedWinningSideId;
        ForcedOverallOutcome = forcedOverallOutcome;
    }

    private static IReadOnlyList<T> Capture<T>(IEnumerable<T> source)
    {
        List<T> copy = new List<T>();
        if (source != null)
        {
            foreach (T item in source)
            {
                if (item != null) copy.Add(item);
            }
        }

        return copy.AsReadOnly();
    }
}

public interface IWorldCommandDefinitionResolver
{
    bool TryResolveItem(string itemDefinitionId, out ItemData item);
    bool TryResolveLocalPlaceType(string definitionId, out LocalPlaceTypeData typeDefinition);
    bool TryResolveLocalConnectionType(string definitionId, out LocalConnectionTypeData typeDefinition);
}

public sealed class WorldCommandPreview
{
    private readonly IReadOnlyList<string> affectedRuntimeIds;
    private readonly IReadOnlyList<string> warnings;

    public WorldCommandKind Kind { get; }
    public WorldCommandAuthorityMode Authority { get; }
    public bool IsValid { get; }
    public IReadOnlyList<string> AffectedRuntimeIds => affectedRuntimeIds;
    public IReadOnlyList<string> Warnings => warnings;
    public string Presentation { get; }
    public string PendingCreatedRuntimeId { get; }

    public WorldCommandPreview(
        WorldCommandKind kind,
        WorldCommandAuthorityMode authority,
        bool isValid,
        IEnumerable<string> affectedRuntimeIds = null,
        string presentation = null,
        IEnumerable<string> warnings = null,
        string pendingCreatedRuntimeId = null)
    {
        Kind = kind;
        Authority = authority;
        IsValid = isValid;
        this.affectedRuntimeIds = CaptureIds(affectedRuntimeIds);
        this.warnings = CaptureIds(warnings);
        Presentation = presentation ?? string.Empty;
        PendingCreatedRuntimeId = string.IsNullOrWhiteSpace(pendingCreatedRuntimeId) ? null : pendingCreatedRuntimeId;
    }

    private static IReadOnlyList<string> CaptureIds(IEnumerable<string> source)
    {
        List<string> copy = new List<string>();
        if (source != null)
        {
            foreach (string id in source)
            {
                if (string.IsNullOrWhiteSpace(id) == false) copy.Add(id);
            }
        }

        return copy.AsReadOnly();
    }
}

public sealed class WorldCommandExecutionContext
{
    public string WorldCommandId { get; }
    public RuntimeIdAllocator RuntimeIdAllocator { get; }
    public RuntimeIdentityRegistry IdentityRegistry { get; }
    public IWorldCommandDefinitionResolver DefinitionResolver { get; }

    public WorldCommandExecutionContext(
        string worldCommandId,
        RuntimeIdAllocator runtimeIdAllocator = null,
        RuntimeIdentityRegistry identityRegistry = null,
        IWorldCommandDefinitionResolver definitionResolver = null)
    {
        WorldCommandId = worldCommandId ?? throw new ArgumentNullException(nameof(worldCommandId));
        RuntimeIdAllocator = runtimeIdAllocator;
        IdentityRegistry = identityRegistry;
        DefinitionResolver = definitionResolver;
    }
}

public sealed class WorldCommandHandlerResult
{
    private readonly IReadOnlyList<string> affectedRuntimeIds;
    private readonly IReadOnlyList<string> createdRuntimeIds;
    private readonly IReadOnlyList<string> eventIds;

    public bool Success { get; }
    public IReadOnlyList<string> AffectedRuntimeIds => affectedRuntimeIds;
    public IReadOnlyList<string> CreatedRuntimeIds => createdRuntimeIds;
    public IReadOnlyList<string> EventIds => eventIds;
    public string Diagnostic { get; }

    public WorldCommandHandlerResult(
        bool success,
        string diagnostic = null,
        IEnumerable<string> affectedRuntimeIds = null,
        IEnumerable<string> createdRuntimeIds = null,
        IEnumerable<string> eventIds = null)
    {
        Success = success;
        Diagnostic = diagnostic;
        affectedRuntimeIds = affectedRuntimeIds ?? Array.Empty<string>();
        createdRuntimeIds = createdRuntimeIds ?? Array.Empty<string>();
        eventIds = eventIds ?? Array.Empty<string>();
        this.affectedRuntimeIds = new List<string>(affectedRuntimeIds).AsReadOnly();
        this.createdRuntimeIds = new List<string>(createdRuntimeIds).AsReadOnly();
        this.eventIds = new List<string>(eventIds).AsReadOnly();
    }
}

public interface IWorldCommandHandler
{
    WorldCommandKind Kind { get; }
    WorldCommandPreview Preview(WorldCommand command);
    WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context);
}

public sealed class WorldCommandResult
{
    private readonly IReadOnlyList<string> affectedRuntimeIds;
    private readonly IReadOnlyList<string> createdRuntimeIds;
    private readonly IReadOnlyList<string> eventIds;

    public string WorldCommandId { get; }
    public WorldCommandKind Kind { get; }
    public bool Success { get; }
    public string Diagnostic { get; }
    public IReadOnlyList<string> AffectedRuntimeIds => affectedRuntimeIds;
    public IReadOnlyList<string> CreatedRuntimeIds => createdRuntimeIds;
    public IReadOnlyList<string> EventIds => eventIds;
    public WorldCommandRecord Record { get; }

    internal WorldCommandResult(
        string worldCommandId,
        WorldCommandKind kind,
        WorldCommandHandlerResult handlerResult,
        WorldCommandRecord record)
    {
        WorldCommandId = worldCommandId;
        Kind = kind;
        Success = handlerResult.Success;
        Diagnostic = handlerResult.Diagnostic;
        affectedRuntimeIds = handlerResult.AffectedRuntimeIds;
        createdRuntimeIds = handlerResult.CreatedRuntimeIds;
        eventIds = handlerResult.EventIds;
        Record = record;
    }
}

public sealed class WorldCommandRecord
{
    private readonly IReadOnlyList<string> affectedRuntimeIds;
    private readonly IReadOnlyList<string> createdRuntimeIds;
    private readonly IReadOnlyList<string> eventIds;

    public string WorldCommandId { get; }
    public long AbsoluteDay { get; }
    public WorldCommandOrigin Origin { get; }
    public WorldCommandAuthorityMode Authority { get; }
    public WorldCommandKind Kind { get; }
    public bool Success { get; }
    public IReadOnlyList<string> AffectedRuntimeIds => affectedRuntimeIds;
    public IReadOnlyList<string> CreatedRuntimeIds => createdRuntimeIds;
    public IReadOnlyList<string> EventIds => eventIds;
    public string Diagnostic { get; }

    public WorldCommandRecord(
        string worldCommandId,
        long absoluteDay,
        WorldCommandOrigin origin,
        WorldCommandAuthorityMode authority,
        WorldCommandKind kind,
        bool success,
        IEnumerable<string> affectedRuntimeIds = null,
        IEnumerable<string> createdRuntimeIds = null,
        IEnumerable<string> eventIds = null,
        string diagnostic = null)
    {
        WorldCommandId = string.IsNullOrWhiteSpace(worldCommandId) ? throw new ArgumentException("WorldCommandId is required.", nameof(worldCommandId)) : worldCommandId;
        if (absoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(absoluteDay));
        AbsoluteDay = absoluteDay;
        Origin = origin;
        Authority = authority;
        Kind = kind;
        Success = success;
        Diagnostic = diagnostic;
        this.affectedRuntimeIds = Capture(affectedRuntimeIds);
        this.createdRuntimeIds = Capture(createdRuntimeIds);
        this.eventIds = Capture(eventIds);
    }

    private static IReadOnlyList<string> Capture(IEnumerable<string> values)
    {
        List<string> copy = new List<string>();
        if (values != null)
        {
            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value) == false) copy.Add(value);
            }
        }

        return copy.AsReadOnly();
    }
}

public sealed class WorldCommandRecordStore
{
    private readonly List<WorldCommandRecord> records = new List<WorldCommandRecord>();
    private readonly IReadOnlyList<WorldCommandRecord> readOnlyRecords;

    public IReadOnlyList<WorldCommandRecord> Records => readOnlyRecords;

    public WorldCommandRecordStore()
    {
        readOnlyRecords = records.AsReadOnly();
    }

    public bool Add(WorldCommandRecord record)
    {
        if (record == null) return false;
        foreach (WorldCommandRecord existing in records)
        {
            if (existing != null && string.Equals(existing.WorldCommandId, record.WorldCommandId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        records.Add(record);
        return true;
    }
}

public sealed class WorldCommandService
{
    private readonly WorldCommandIdAllocator commandIdAllocator;
    private readonly WorldCommandRecordStore recordStore;
    private readonly RuntimeIdAllocator runtimeIdAllocator;
    private readonly RuntimeIdentityRegistry identityRegistry;
    private readonly IWorldCommandDefinitionResolver definitionResolver;
    private readonly SimulationTime simulationTime;
    private readonly Dictionary<WorldCommandKind, IWorldCommandHandler> handlers =
        new Dictionary<WorldCommandKind, IWorldCommandHandler>();

    public WorldCommandRecordStore RecordStore => recordStore;

    public WorldCommandService(
        WorldCommandIdAllocator commandIdAllocator = null,
        WorldCommandRecordStore recordStore = null,
        RuntimeIdAllocator runtimeIdAllocator = null,
        RuntimeIdentityRegistry identityRegistry = null,
        IWorldCommandDefinitionResolver definitionResolver = null,
        SimulationTime simulationTime = null)
    {
        this.commandIdAllocator = commandIdAllocator ?? new WorldCommandIdAllocator();
        this.recordStore = recordStore ?? new WorldCommandRecordStore();
        this.runtimeIdAllocator = runtimeIdAllocator;
        this.identityRegistry = identityRegistry;
        this.definitionResolver = definitionResolver;
        this.simulationTime = simulationTime;
    }

    public bool RegisterHandler(IWorldCommandHandler handler)
    {
        if (handler == null || Enum.IsDefined(typeof(WorldCommandKind), handler.Kind) == false || handlers.ContainsKey(handler.Kind))
        {
            return false;
        }

        handlers.Add(handler.Kind, handler);
        return true;
    }

    public WorldCommandPreview Preview(WorldCommand command)
    {
        if (command == null || handlers.TryGetValue(command.Kind, out IWorldCommandHandler handler) == false)
        {
            return new WorldCommandPreview(
                command == null ? WorldCommandKind.RelocateNpc : command.Kind,
                command == null ? WorldCommandAuthorityMode.Suggest : command.Authority,
                false,
                presentation: "World command handler is unavailable.");
        }

        return handler.Preview(command);
    }

    public WorldCommandResult Execute(WorldCommand command)
    {
        string commandId = commandIdAllocator.Allocate();
        WorldCommandHandlerResult handlerResult;
        if (command == null)
        {
            handlerResult = new WorldCommandHandlerResult(false, "World command is null.");
            return CreateResult(commandId, WorldCommandKind.RelocateNpc, WorldCommandOrigin.ExternalImport, WorldCommandAuthorityMode.Request, handlerResult);
        }

        if (command.Authority == WorldCommandAuthorityMode.Suggest)
        {
            handlerResult = new WorldCommandHandlerResult(false, "Suggest authority is preview-only.");
        }
        else if (handlers.TryGetValue(command.Kind, out IWorldCommandHandler handler) == false)
        {
            handlerResult = new WorldCommandHandlerResult(false, "World command handler is unavailable.");
        }
        else
        {
            handlerResult = handler.Execute(
                command,
                new WorldCommandExecutionContext(commandId, runtimeIdAllocator, identityRegistry, definitionResolver));
        }

        return CreateResult(commandId, command.Kind, command.Origin, command.Authority, handlerResult);
    }

    private WorldCommandResult CreateResult(
        string commandId,
        WorldCommandKind kind,
        WorldCommandOrigin origin,
        WorldCommandAuthorityMode authority,
        WorldCommandHandlerResult handlerResult)
    {
        long day = simulationTime != null ? simulationTime.AbsoluteDay : 0L;
        WorldCommandRecord record = new WorldCommandRecord(
            commandId,
            day,
            origin,
            authority,
            kind,
            handlerResult.Success,
            handlerResult.AffectedRuntimeIds,
            handlerResult.CreatedRuntimeIds,
            handlerResult.EventIds,
            handlerResult.Diagnostic);
        recordStore.Add(record);
        return new WorldCommandResult(commandId, kind, handlerResult, record);
    }
}
