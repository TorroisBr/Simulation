using System;
using System.Collections.Generic;

public sealed class WorldCommandDefinitionCatalog : IWorldCommandDefinitionResolver, IWorldCommandDefinitionLookup
{
    private readonly Dictionary<string, ItemData> items = new Dictionary<string, ItemData>(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalPlaceTypeData> placeTypes = new Dictionary<string, LocalPlaceTypeData>(StringComparer.Ordinal);
    private readonly Dictionary<string, LocalConnectionTypeData> connectionTypes = new Dictionary<string, LocalConnectionTypeData>(StringComparer.Ordinal);

    public IReadOnlyCollection<string> ItemDefinitionIds => items.Keys;
    public IReadOnlyCollection<string> LocalPlaceTypeDefinitionIds => placeTypes.Keys;
    public IReadOnlyCollection<string> LocalConnectionTypeDefinitionIds => connectionTypes.Keys;

    public bool RegisterItem(ItemData item)
    {
        return item != null && Register(items, item.DefinitionId, item);
    }

    public bool RegisterLocalPlaceType(LocalPlaceTypeData definition)
    {
        return definition != null && Register(placeTypes, definition.DefinitionId, definition);
    }

    public bool RegisterLocalConnectionType(LocalConnectionTypeData definition)
    {
        return definition != null && Register(connectionTypes, definition.DefinitionId, definition);
    }

    public bool TryResolveItem(string itemDefinitionId, out ItemData item)
    {
        return items.TryGetValue(itemDefinitionId ?? string.Empty, out item);
    }

    public bool TryResolveLocalPlaceType(string definitionId, out LocalPlaceTypeData typeDefinition)
    {
        return placeTypes.TryGetValue(definitionId ?? string.Empty, out typeDefinition);
    }

    public bool TryResolveLocalConnectionType(string definitionId, out LocalConnectionTypeData typeDefinition)
    {
        return connectionTypes.TryGetValue(definitionId ?? string.Empty, out typeDefinition);
    }

    public IReadOnlyList<WorldCommandTranslationEntity> FindItemDefinitions(string reference)
    {
        return WorldCommandTranslationMatching.Resolve(ToEntities(items.Values, WorldCommandTranslationEntityKind.ItemDefinition), reference);
    }

    public IReadOnlyList<WorldCommandTranslationEntity> FindLocalPlaceTypes(string reference)
    {
        return WorldCommandTranslationMatching.Resolve(ToEntities(placeTypes.Values, WorldCommandTranslationEntityKind.LocalPlaceType), reference);
    }

    public IReadOnlyList<WorldCommandTranslationEntity> FindLocalConnectionTypes(string reference)
    {
        return WorldCommandTranslationMatching.Resolve(ToEntities(connectionTypes.Values, WorldCommandTranslationEntityKind.LocalConnectionType), reference);
    }

    private static List<WorldCommandTranslationEntity> ToEntities<T>(
        IEnumerable<T> definitions,
        WorldCommandTranslationEntityKind kind)
        where T : UnityEngine.Object
    {
        List<WorldCommandTranslationEntity> result = new List<WorldCommandTranslationEntity>();
        foreach (T definition in definitions)
        {
            if (definition is ItemData item)
            {
                result.Add(new WorldCommandTranslationEntity(kind, definitionId: item.DefinitionId, displayName: item.itemName));
            }
            else if (definition is LocalPlaceTypeData placeType)
            {
                result.Add(new WorldCommandTranslationEntity(kind, definitionId: placeType.DefinitionId, displayName: placeType.DisplayName));
            }
            else if (definition is LocalConnectionTypeData connectionType)
            {
                result.Add(new WorldCommandTranslationEntity(kind, definitionId: connectionType.DefinitionId, displayName: connectionType.DisplayName));
            }
        }

        return result;
    }

    private static bool Register<T>(Dictionary<string, T> definitions, string definitionId, T definition)
    {
        if (string.IsNullOrWhiteSpace(definitionId) == true || definitions.ContainsKey(definitionId) == true)
        {
            return false;
        }

        definitions.Add(definitionId, definition);
        return true;
    }
}

public static class WorldCommandHandlerRegistration
{
    public static bool RegisterCoreHandlers(
        WorldCommandService service,
        RuntimeIdAllocator runtimeIdAllocator,
        RuntimeIdentityRegistry identityRegistry,
        PlaceContentStore placeContentStore,
        LocalTopologyStore localTopologyStore,
        ConflictResolutionService conflictResolutionService,
        IWorldCommandDefinitionResolver definitionResolver = null,
        ExpeditionSystem expeditionSystem = null,
        TravelPartyStore travelPartyStore = null,
        DomainEventStore domainEventStore = null,
        SimulationRuntime worldRuntime = null)
    {
        if (service == null
            || runtimeIdAllocator == null
            || identityRegistry == null
            || placeContentStore == null
            || localTopologyStore == null
            || conflictResolutionService == null)
        {
            return false;
        }

        CoreWorldCommandDependencies dependencies = new CoreWorldCommandDependencies(
            runtimeIdAllocator,
            identityRegistry,
            placeContentStore,
            localTopologyStore,
            conflictResolutionService,
            definitionResolver,
            expeditionSystem,
            travelPartyStore,
            domainEventStore,
            worldRuntime);

        bool registered = true;
        registered &= service.RegisterHandler(new RelocateNpcWorldCommandHandler(dependencies));
        registered &= service.RegisterHandler(new DeclareStackResourceWorldCommandHandler(dependencies));
        registered &= service.RegisterHandler(new DeclareNotableItemWorldCommandHandler(dependencies));
        registered &= service.RegisterHandler(new AddLocalPlaceWorldCommandHandler(dependencies));
        registered &= service.RegisterHandler(new AddLocalConnectionWorldCommandHandler(dependencies));
        registered &= service.RegisterHandler(new GrantSiteKnowledgeWorldCommandHandler(dependencies));
        registered &= service.RegisterHandler(new GrantAdventureIntelWorldCommandHandler(dependencies));
        registered &= service.RegisterHandler(new ResolveConflictWorldCommandHandler(WorldCommandKind.ResolveConflict, dependencies));
        registered &= service.RegisterHandler(new ResolveConflictWorldCommandHandler(WorldCommandKind.PlaceOpposition, dependencies));
        return registered;
    }
}

public sealed class CoreWorldCommandDependencies
{
    public RuntimeIdAllocator RuntimeIdAllocator { get; }
    public RuntimeIdentityRegistry IdentityRegistry { get; }
    public PlaceContentStore PlaceContentStore { get; }
    public LocalTopologyStore LocalTopologyStore { get; }
    public ConflictResolutionService ConflictResolutionService { get; }
    public IWorldCommandDefinitionResolver DefinitionResolver { get; }
    public ExpeditionSystem ExpeditionSystem { get; }
    public TravelPartyStore TravelPartyStore { get; }
    public DomainEventStore DomainEventStore { get; }
    public SimulationRuntime WorldRuntime { get; }

    public CoreWorldCommandDependencies(
        RuntimeIdAllocator runtimeIdAllocator,
        RuntimeIdentityRegistry identityRegistry,
        PlaceContentStore placeContentStore,
        LocalTopologyStore localTopologyStore,
        ConflictResolutionService conflictResolutionService,
        IWorldCommandDefinitionResolver definitionResolver,
        ExpeditionSystem expeditionSystem,
        TravelPartyStore travelPartyStore,
        DomainEventStore domainEventStore,
        SimulationRuntime worldRuntime)
    {
        RuntimeIdAllocator = runtimeIdAllocator;
        IdentityRegistry = identityRegistry;
        PlaceContentStore = placeContentStore;
        LocalTopologyStore = localTopologyStore;
        ConflictResolutionService = conflictResolutionService;
        DefinitionResolver = definitionResolver;
        ExpeditionSystem = expeditionSystem;
        TravelPartyStore = travelPartyStore;
        DomainEventStore = domainEventStore;
        WorldRuntime = worldRuntime;
    }
}

internal static class WorldCommandAuthorityRules
{
    public static bool IsAllowedForPreview(WorldCommandKind kind, WorldCommandAuthorityMode authority)
    {
        return authority == WorldCommandAuthorityMode.Suggest
            || IsAllowedForExecution(kind, authority);
    }

    public static bool IsAllowedForExecution(WorldCommandKind kind, WorldCommandAuthorityMode authority)
    {
        if (kind == WorldCommandKind.ResolveConflict || kind == WorldCommandKind.PlaceOpposition)
        {
            return authority == WorldCommandAuthorityMode.Request
                || authority == WorldCommandAuthorityMode.Declare
                || authority == WorldCommandAuthorityMode.ForceOutcome;
        }

        return authority == WorldCommandAuthorityMode.Declare;
    }

    public static string GetDiagnostic(WorldCommandKind kind, WorldCommandAuthorityMode authority)
    {
        if (authority == WorldCommandAuthorityMode.Suggest)
        {
            return "Suggest authority is preview-only.";
        }

        if (kind == WorldCommandKind.ResolveConflict || kind == WorldCommandKind.PlaceOpposition)
        {
            return kind + " accepts Request, Declare, or ForceOutcome authority.";
        }

        return kind + " requires Declare authority.";
    }
}

public abstract class CoreWorldCommandHandlerBase : IWorldCommandHandler, IRuntimeMutationGuardSource
{
    protected readonly CoreWorldCommandDependencies Dependencies;

    protected CoreWorldCommandHandlerBase(CoreWorldCommandDependencies dependencies)
    {
        Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
    }

    public abstract WorldCommandKind Kind { get; }
    public abstract WorldCommandPreview Preview(WorldCommand command);
    public abstract WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context);

    internal AuthoritativeMutationGuard RuntimeMutationGuard => Dependencies.WorldRuntime?.MutationGuard;
    AuthoritativeMutationGuard IRuntimeMutationGuardSource.RuntimeMutationGuard => RuntimeMutationGuard;
    protected bool RuntimeIsFaulted => Dependencies.WorldRuntime?.IsMutationFaulted == true;

    protected WorldCommandPreview Invalid(WorldCommand command, string diagnostic, IEnumerable<string> affected = null)
    {
        return new WorldCommandPreview(
            Kind,
            command == null ? WorldCommandAuthorityMode.Request : command.Authority,
            false,
            affected,
            diagnostic,
            new[] { diagnostic });
    }

    protected WorldCommandPreview Valid(
        WorldCommand command,
        string presentation,
        IEnumerable<string> affected = null,
        string pendingCreatedRuntimeId = null)
    {
        return new WorldCommandPreview(
            Kind,
            command.Authority,
            true,
            affected,
            presentation,
            pendingCreatedRuntimeId: pendingCreatedRuntimeId);
    }

    protected static WorldCommandHandlerResult Failure(string diagnostic)
    {
        return new WorldCommandHandlerResult(false, diagnostic);
    }

    protected static WorldCommandHandlerResult Success(
        IEnumerable<string> affected = null,
        IEnumerable<string> created = null,
        IEnumerable<string> events = null)
    {
        return new WorldCommandHandlerResult(true, affectedRuntimeIds: affected, createdRuntimeIds: created, eventIds: events);
    }

    protected bool TryValidatePreviewAuthority(WorldCommand command, out string diagnostic)
    {
        diagnostic = null;
        if (command == null || WorldCommandAuthorityRules.IsAllowedForPreview(Kind, command.Authority))
        {
            return true;
        }

        diagnostic = WorldCommandAuthorityRules.GetDiagnostic(Kind, command.Authority);
        return false;
    }

    protected bool TryValidateExecutionAuthority(WorldCommand command, out string diagnostic)
    {
        diagnostic = null;
        if (command == null || WorldCommandAuthorityRules.IsAllowedForExecution(Kind, command.Authority))
        {
            return true;
        }

        diagnostic = WorldCommandAuthorityRules.GetDiagnostic(Kind, command.Authority);
        return false;
    }

    protected bool TryResolveOwner(
        WorldContentOwnerReferencePayload payload,
        out PlaceContentOwnerReference owner,
        out string diagnostic)
    {
        owner = null;
        diagnostic = null;
        if (payload == null)
        {
            diagnostic = "World content owner is required.";
            return false;
        }

        if (payload.OwnerKind == PlaceContentOwnerKind.City)
        {
            if (Dependencies.IdentityRegistry.TryGetCityWithoutLogging(payload.OwnerRuntimeId, out CityRuntime city) == false
                || city == null
                || city.Location == null
                || string.Equals(city.Location.RuntimeId, payload.MacroLocationRuntimeId, StringComparison.Ordinal) == false)
            {
                diagnostic = "Content owner does not resolve to the declared City macro location.";
                return false;
            }

            owner = PlaceContentOwnerReference.ForCity(city);
            return true;
        }

        if (payload.OwnerKind == PlaceContentOwnerKind.ExplorableSite)
        {
            if (Dependencies.IdentityRegistry.TryGetExplorableSiteWithoutLogging(payload.OwnerRuntimeId, out ExplorableSiteRuntime site) == false
                || site == null
                || site.Location == null
                || string.Equals(site.Location.RuntimeId, payload.MacroLocationRuntimeId, StringComparison.Ordinal) == false)
            {
                diagnostic = "Content owner does not resolve to the declared ExplorableSite macro location.";
                return false;
            }

            owner = PlaceContentOwnerReference.ForExplorableSite(site);
            return true;
        }

        if (payload.OwnerKind == PlaceContentOwnerKind.LocalPlace
            && Dependencies.IdentityRegistry.TryGetLocalPlace(payload.OwnerRuntimeId, out LocalPlaceRuntime localPlace) == true
            && localPlace != null
            && localPlace.OwningTopology != null
            && string.Equals(localPlace.OwningTopology.Owner.MacroLocationRuntimeId, payload.MacroLocationRuntimeId, StringComparison.Ordinal) == true
            && (string.IsNullOrWhiteSpace(payload.TopologyOwnerRuntimeId)
                || string.Equals(localPlace.OwningTopology.Owner.OwnerRuntimeId, payload.TopologyOwnerRuntimeId, StringComparison.Ordinal) == true))
        {
            owner = PlaceContentOwnerReference.ForLocalPlace(localPlace, payload.MacroLocationRuntimeId);
            return true;
        }

        diagnostic = "Content owner does not resolve to a published LocalPlace topology context.";
        return false;
    }

    protected IWorldCommandDefinitionResolver ResolveDefinitions(WorldCommandExecutionContext context)
    {
        return context?.DefinitionResolver ?? Dependencies.DefinitionResolver;
    }

    protected static bool ContainsForcedConflictFields(ResolveConflictWorldCommandPayload payload)
    {
        if (payload == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(payload.ForcedWinningSideId) == false || payload.ForcedOverallOutcome.HasValue)
        {
            return true;
        }

        if (payload.Constraints != null)
        {
            foreach (WorldConflictParticipantConstraintPayload constraint in payload.Constraints)
            {
                if (constraint != null
                    && (constraint.ForcedInjurySeverity.HasValue
                        || constraint.ForceDeath.HasValue
                        || constraint.ForceAlive.HasValue
                        || constraint.ForcedDisposition.HasValue))
                {
                    return true;
                }
            }
        }

        return false;
    }

    protected bool TryBuildConstraints(
        WorldCommand command,
        ResolveConflictWorldCommandPayload payload,
        Conflict conflict,
        out ConflictResolutionConstraints constraints,
        out string diagnostic)
    {
        constraints = null;
        diagnostic = null;
        bool hasForcedFields = ContainsForcedConflictFields(payload);
        if (command.Authority != WorldCommandAuthorityMode.ForceOutcome)
        {
            if (hasForcedFields == true)
            {
                diagnostic = "Forced conflict constraints require ForceOutcome authority.";
                return false;
            }

            return true;
        }

        constraints = new ConflictResolutionConstraints
        {
            ForcedWinningSideId = payload.ForcedWinningSideId,
            ForcedOverallOutcome = payload.ForcedOverallOutcome
        };

        if (payload.Constraints != null)
        {
            foreach (WorldConflictParticipantConstraintPayload source in payload.Constraints)
            {
                if (source == null)
                {
                    continue;
                }

                ConflictParticipantResolutionConstraint target = new ConflictParticipantResolutionConstraint(source.ParticipantId)
                {
                    ForcedInjurySeverity = source.ForcedInjurySeverity,
                    ForceDeath = source.ForceDeath,
                    ForceAlive = source.ForceAlive,
                    ForcedDisposition = source.ForcedDisposition
                };
                constraints.AddParticipantConstraint(target);
            }
        }

        if (constraints.TryValidate(conflict, out diagnostic) == false)
        {
            constraints = null;
            return false;
        }

        return true;
    }

    protected bool TryFindOpposition(
        ResolveConflictWorldCommandPayload payload,
        out PlaceContentOwnerReference owner,
        out PlaceContentRuntime content,
        out PlaceOppositionRuntime opposition,
        out string diagnostic)
    {
        owner = null;
        content = null;
        opposition = null;
        diagnostic = null;

        if (string.IsNullOrWhiteSpace(payload.OppositionRuntimeId) == true)
        {
            diagnostic = "Place opposition resolution requires an OppositionRuntimeId.";
            return false;
        }

        foreach (PlaceContentRuntime candidate in Dependencies.PlaceContentStore.Places)
        {
            if (candidate == null)
            {
                continue;
            }

            PlaceOppositionRuntime found = candidate.GetOpposition(payload.OppositionRuntimeId);
            if (found == null)
            {
                continue;
            }

            if (opposition != null)
            {
                diagnostic = "Opposition RuntimeId is ambiguous across place content owners.";
                return false;
            }

            owner = candidate.Owner;
            content = candidate;
            opposition = found;
        }

        if (opposition == null || owner == null || content == null)
        {
            diagnostic = "Opposition RuntimeId is not an active World Truth member of a place.";
            return false;
        }

        if (opposition.IsActive == false)
        {
            diagnostic = "The targeted opposition has already been resolved.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(payload.LocationRuntimeId) == false
            && string.Equals(payload.LocationRuntimeId, owner.OwnerRuntimeId, StringComparison.Ordinal) == false
            && string.Equals(payload.LocationRuntimeId, owner.MacroLocationRuntimeId, StringComparison.Ordinal) == false
            && string.Equals(payload.LocationRuntimeId, owner.TopologyOwnerRuntimeId, StringComparison.Ordinal) == false)
        {
            diagnostic = "Conflict LocationRuntimeId contradicts the targeted opposition owner.";
            return false;
        }

        return true;
    }

    protected bool TryCaptureNewEventIds(int countBefore, out IReadOnlyList<string> eventIds)
    {
        List<string> captured = new List<string>();
        IReadOnlyList<DomainEvent> events = Dependencies.DomainEventStore?.Events;
        if (events != null)
        {
            for (int i = Math.Max(0, countBefore); i < events.Count; i++)
            {
                if (events[i] != null)
                {
                    captured.Add(events[i].EventId);
                }
            }
        }

        eventIds = captured.AsReadOnly();
        return true;
    }
}

public sealed class RelocateNpcWorldCommandHandler : CoreWorldCommandHandlerBase
{
    public RelocateNpcWorldCommandHandler(CoreWorldCommandDependencies dependencies) : base(dependencies) { }
    public override WorldCommandKind Kind => WorldCommandKind.RelocateNpc;

    public override WorldCommandPreview Preview(WorldCommand command)
    {
        RelocateNpcWorldCommandPayload payload = command?.Payload as RelocateNpcWorldCommandPayload;
        if (payload == null)
        {
            return Invalid(command, "Relocate NPC requires a typed payload.");
        }

        if (TryValidatePreviewAuthority(command, out string authorityDiagnostic) == false)
        {
            return Invalid(command, authorityDiagnostic);
        }

        if (Dependencies.IdentityRegistry.TryGetNpcWithoutLogging(payload.NpcRuntimeId, out NpcRuntime npc) == false
            || Dependencies.IdentityRegistry.TryGetLocation(payload.DestinationMacroLocationRuntimeId, out _) == false)
        {
            return Invalid(command, "Relocate NPC requires registered NPC and destination macro location.", new[] { payload.NpcRuntimeId, payload.DestinationMacroLocationRuntimeId });
        }

        if (Dependencies.ExpeditionSystem != null && Dependencies.ExpeditionSystem.IsNpcOnActiveExpedition(payload.NpcRuntimeId))
        {
            return Invalid(command, "An active expedition member cannot be relocated by this command.", new[] { payload.NpcRuntimeId });
        }

        if (string.IsNullOrWhiteSpace(npc.ActiveTravelPartyId) == false
            || (Dependencies.TravelPartyStore != null && Dependencies.TravelPartyStore.TryGetPartyForNpc(payload.NpcRuntimeId, out _)))
        {
            return Invalid(command, "A TravelParty member cannot be relocated by this command.", new[] { payload.NpcRuntimeId });
        }

        return Valid(command, "Relocate " + payload.NpcRuntimeId + " to " + payload.DestinationMacroLocationRuntimeId + ".", new[] { payload.NpcRuntimeId, payload.DestinationMacroLocationRuntimeId });
    }

    public override WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context)
    {
        if (RuntimeIsFaulted) return Failure("Runtime mutation is faulted.");
        RelocateNpcWorldCommandPayload payload = command?.Payload as RelocateNpcWorldCommandPayload;
        if (payload == null)
        {
            return Failure("Relocate NPC requires a typed payload.");
        }

        if (TryValidateExecutionAuthority(command, out string authorityDiagnostic) == false)
        {
            return Failure(authorityDiagnostic);
        }

        if (Dependencies.IdentityRegistry.TryGetNpcWithoutLogging(payload.NpcRuntimeId, out NpcRuntime npc) == false
            || Dependencies.IdentityRegistry.TryGetLocation(payload.DestinationMacroLocationRuntimeId, out SpatialLocationRuntime destination) == false)
        {
            return Failure("Relocate NPC requires registered NPC and destination macro location.");
        }

        if (Dependencies.ExpeditionSystem != null && Dependencies.ExpeditionSystem.IsNpcOnActiveExpedition(payload.NpcRuntimeId))
        {
            return Failure("An active expedition member cannot be relocated by this command.");
        }

        if (string.IsNullOrWhiteSpace(npc.ActiveTravelPartyId) == false
            || (Dependencies.TravelPartyStore != null && Dependencies.TravelPartyStore.TryGetPartyForNpc(payload.NpcRuntimeId, out _)))
        {
            return Failure("A TravelParty member cannot be relocated by this command.");
        }

        if (npc.IsTraveling)
        {
            npc.CancelTravel(npc.CurrentLocation, npc.CurrentCity);
        }

        if (npc.SetCurrentPresence(destination) == false)
        {
            return Failure("NPC could not assume the declared macro location.");
        }

        return Success(new[] { npc.RuntimeId, destination.RuntimeId });
    }
}

public sealed class DeclareStackResourceWorldCommandHandler : CoreWorldCommandHandlerBase
{
    public DeclareStackResourceWorldCommandHandler(CoreWorldCommandDependencies dependencies) : base(dependencies) { }
    public override WorldCommandKind Kind => WorldCommandKind.DeclareStackResource;

    public override WorldCommandPreview Preview(WorldCommand command)
    {
        DeclareStackResourceWorldCommandPayload payload = command?.Payload as DeclareStackResourceWorldCommandPayload;
        if (payload == null)
        {
            return Invalid(command, "Stack resource declaration requires a typed payload.");
        }

        if (TryValidatePreviewAuthority(command, out string authorityDiagnostic) == false)
        {
            return Invalid(command, authorityDiagnostic);
        }

        if (TryResolveOwner(payload.Owner, out PlaceContentOwnerReference owner, out string ownerDiagnostic) == false)
        {
            return Invalid(command, ownerDiagnostic);
        }

        if (ResolveDefinitions(null)?.TryResolveItem(payload.ItemDefinitionId, out ItemData item) != true || item == null)
        {
            return Invalid(command, "ItemDefinitionId is not registered.");
        }

        return Valid(command, "Declare " + payload.Amount + " " + payload.ItemDefinitionId + " at " + owner.StableKey + ".", new[] { owner.OwnerRuntimeId, payload.ItemDefinitionId });
    }

    public override WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context)
    {
        if (RuntimeIsFaulted) return Failure("Runtime mutation is faulted.");
        DeclareStackResourceWorldCommandPayload payload = command?.Payload as DeclareStackResourceWorldCommandPayload;
        if (payload == null)
        {
            return Failure("Stack resource declaration requires a typed payload.");
        }

        if (TryValidateExecutionAuthority(command, out string authorityDiagnostic) == false)
        {
            return Failure(authorityDiagnostic);
        }

        if (TryResolveOwner(payload.Owner, out PlaceContentOwnerReference owner, out string ownerDiagnostic) == false)
        {
            return Failure(ownerDiagnostic);
        }

        if (ResolveDefinitions(context)?.TryResolveItem(payload.ItemDefinitionId, out ItemData item) != true || item == null)
        {
            return Failure("ItemDefinitionId is not registered.");
        }

        if (Dependencies.PlaceContentStore.TryAddStack(
            owner,
            item,
            payload.Amount,
            payload.PersistencePolicy,
            out _,
            payload.DecayPerDay,
            payload.AverageUnitCost ?? 0f) == false)
        {
            return Failure("Stack resource declaration was rejected by the place content boundary.");
        }

        return Success(new[] { owner.OwnerRuntimeId, payload.ItemDefinitionId });
    }
}

public sealed class DeclareNotableItemWorldCommandHandler : CoreWorldCommandHandlerBase
{
    public DeclareNotableItemWorldCommandHandler(CoreWorldCommandDependencies dependencies) : base(dependencies) { }
    public override WorldCommandKind Kind => WorldCommandKind.DeclareNotableItem;

    public override WorldCommandPreview Preview(WorldCommand command)
    {
        DeclareNotableItemWorldCommandPayload payload = command?.Payload as DeclareNotableItemWorldCommandPayload;
        if (payload == null)
        {
            return Invalid(command, "Notable declaration requires a typed payload.");
        }

        if (TryValidatePreviewAuthority(command, out string authorityDiagnostic) == false)
        {
            return Invalid(command, authorityDiagnostic);
        }

        if (TryResolveOwner(payload.Owner, out PlaceContentOwnerReference owner, out string ownerDiagnostic) == false)
        {
            return Invalid(command, ownerDiagnostic);
        }

        if (ResolveDefinitions(null)?.TryResolveItem(payload.ItemDefinitionId, out ItemData item) != true || item == null)
        {
            return Invalid(command, "ItemDefinitionId is not registered.");
        }

        return Valid(command, "Declare notable " + payload.ItemDefinitionId + " at " + owner.StableKey + ".", new[] { owner.OwnerRuntimeId, payload.ItemDefinitionId });
    }

    public override WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context)
    {
        if (RuntimeIsFaulted) return Failure("Runtime mutation is faulted.");
        DeclareNotableItemWorldCommandPayload payload = command?.Payload as DeclareNotableItemWorldCommandPayload;
        if (payload == null)
        {
            return Failure("Notable declaration requires a typed payload.");
        }

        if (TryValidateExecutionAuthority(command, out string authorityDiagnostic) == false)
        {
            return Failure(authorityDiagnostic);
        }

        if (TryResolveOwner(payload.Owner, out PlaceContentOwnerReference owner, out string ownerDiagnostic) == false)
        {
            return Failure(ownerDiagnostic);
        }

        if (ResolveDefinitions(context)?.TryResolveItem(payload.ItemDefinitionId, out ItemData item) != true || item == null)
        {
            return Failure("ItemDefinitionId is not registered.");
        }

        if (Dependencies.PlaceContentStore.TryCreateNotableItem(item, out NotableItemRuntime notable, out string createDiagnostic) == false)
        {
            return Failure(createDiagnostic);
        }

        if (Dependencies.PlaceContentStore.TryAddNotable(owner, notable, out string addDiagnostic) == false)
        {
            return Failure(addDiagnostic);
        }

        return Success(new[] { owner.OwnerRuntimeId, notable.RuntimeId }, new[] { notable.RuntimeId });
    }
}

public sealed class AddLocalPlaceWorldCommandHandler : CoreWorldCommandHandlerBase
{
    public AddLocalPlaceWorldCommandHandler(CoreWorldCommandDependencies dependencies) : base(dependencies) { }
    public override WorldCommandKind Kind => WorldCommandKind.AddLocalPlace;

    public override WorldCommandPreview Preview(WorldCommand command)
    {
        AddLocalPlaceWorldCommandPayload payload = command?.Payload as AddLocalPlaceWorldCommandPayload;
        if (payload == null)
        {
            return Invalid(command, "Local place creation requires a typed payload.");
        }

        if (TryValidatePreviewAuthority(command, out string authorityDiagnostic) == false)
        {
            return Invalid(command, authorityDiagnostic);
        }

        if (TryResolveTopology(payload.TopologyOwnerRuntimeId, null, payload, out LocalTopologyRuntime topology, out LocalPlaceRuntime parent, out LocalPlaceTypeData typeDefinition, out string diagnostic) == false)
        {
            return Invalid(command, diagnostic);
        }

        return Valid(command, "Add local place '" + payload.DisplayName + "' to " + topology.Owner.OwnerRuntimeId + ".", new[] { topology.Owner.OwnerRuntimeId, parent?.RuntimeId, typeDefinition?.DefinitionId });
    }

    public override WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context)
    {
        if (RuntimeIsFaulted) return Failure("Runtime mutation is faulted.");
        AddLocalPlaceWorldCommandPayload payload = command?.Payload as AddLocalPlaceWorldCommandPayload;
        if (payload == null)
        {
            return Failure("Local place creation requires a typed payload.");
        }

        if (TryValidateExecutionAuthority(command, out string authorityDiagnostic) == false)
        {
            return Failure(authorityDiagnostic);
        }

        if (TryResolveTopology(payload.TopologyOwnerRuntimeId, context, payload, out LocalTopologyRuntime topology, out LocalPlaceRuntime parent, out LocalPlaceTypeData typeDefinition, out string diagnostic) == false)
        {
            return Failure(diagnostic);
        }

        string runtimeId;
        try
        {
            runtimeId = Dependencies.RuntimeIdAllocator.AllocateLocalPlaceId();
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }

        LocalPlaceRuntime place = new LocalPlaceRuntime(runtimeId, payload.DisplayName, typeDefinition);
        if (Dependencies.LocalTopologyStore.TryAddPlace(topology, place, parent, payload.IsEntryPoint, out diagnostic) == false)
        {
            return Failure(diagnostic);
        }

        return Success(new[] { topology.Owner.OwnerRuntimeId, place.RuntimeId }, new[] { place.RuntimeId });
    }

    private bool TryResolveTopology(
        string topologyOwnerRuntimeId,
        WorldCommandExecutionContext context,
        AddLocalPlaceWorldCommandPayload payload,
        out LocalTopologyRuntime topology,
        out LocalPlaceRuntime parent,
        out LocalPlaceTypeData typeDefinition,
        out string diagnostic)
    {
        topology = null;
        parent = null;
        typeDefinition = null;
        diagnostic = null;
        if (Dependencies.LocalTopologyStore.TryGetTopologyForOwner(topologyOwnerRuntimeId, out topology) == false
            || topology == null
            || topology.IsPublished == false)
        {
            diagnostic = "Topology owner does not resolve to a published local topology.";
            return false;
        }

        if (payload == null)
        {
            diagnostic = "Local place payload is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(payload.ParentLocalPlaceRuntimeId) == false
            && topology.TryGetPlace(payload.ParentLocalPlaceRuntimeId, out parent) == false)
        {
            diagnostic = "ParentLocalPlaceRuntimeId must reference an existing place in this topology.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(payload.PlaceTypeDefinitionId) == false
            && (ResolveDefinitions(context)?.TryResolveLocalPlaceType(payload.PlaceTypeDefinitionId, out typeDefinition) != true || typeDefinition == null))
        {
            diagnostic = "PlaceTypeDefinitionId is not registered.";
            return false;
        }

        return true;
    }
}

public sealed class AddLocalConnectionWorldCommandHandler : CoreWorldCommandHandlerBase
{
    public AddLocalConnectionWorldCommandHandler(CoreWorldCommandDependencies dependencies) : base(dependencies) { }
    public override WorldCommandKind Kind => WorldCommandKind.AddLocalConnection;

    public override WorldCommandPreview Preview(WorldCommand command)
    {
        AddLocalConnectionWorldCommandPayload payload = command?.Payload as AddLocalConnectionWorldCommandPayload;
        if (payload == null)
        {
            return Invalid(command, "Local connection creation requires a typed payload.");
        }

        if (TryValidatePreviewAuthority(command, out string authorityDiagnostic) == false)
        {
            return Invalid(command, authorityDiagnostic);
        }

        if (TryResolveConnection(payload, null, out LocalTopologyRuntime topology, out LocalPlaceRuntime origin, out LocalPlaceRuntime destination, out LocalConnectionTypeData typeDefinition, out string diagnostic) == false)
        {
            return Invalid(command, diagnostic);
        }

        return Valid(command, "Add directed local connection " + origin.RuntimeId + " -> " + destination.RuntimeId + ".", new[] { topology.Owner.OwnerRuntimeId, origin.RuntimeId, destination.RuntimeId, typeDefinition?.DefinitionId });
    }

    public override WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context)
    {
        if (RuntimeIsFaulted) return Failure("Runtime mutation is faulted.");
        AddLocalConnectionWorldCommandPayload payload = command?.Payload as AddLocalConnectionWorldCommandPayload;
        if (payload == null)
        {
            return Failure("Local connection creation requires a typed payload.");
        }

        if (TryValidateExecutionAuthority(command, out string authorityDiagnostic) == false)
        {
            return Failure(authorityDiagnostic);
        }

        if (TryResolveConnection(payload, context, out LocalTopologyRuntime topology, out LocalPlaceRuntime origin, out LocalPlaceRuntime destination, out LocalConnectionTypeData typeDefinition, out string diagnostic) == false)
        {
            return Failure(diagnostic);
        }

        string runtimeId;
        try
        {
            runtimeId = Dependencies.RuntimeIdAllocator.AllocateLocalConnectionId();
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }

        LocalTopologyConnectionRuntime connection = new LocalTopologyConnectionRuntime(
            runtimeId,
            origin,
            destination,
            payload.TraversalCost,
            typeDefinition);
        if (Dependencies.LocalTopologyStore.TryAddConnection(topology, connection, out diagnostic) == false)
        {
            return Failure(diagnostic);
        }

        return Success(new[] { topology.Owner.OwnerRuntimeId, connection.RuntimeId }, new[] { connection.RuntimeId });
    }

    private bool TryResolveConnection(
        AddLocalConnectionWorldCommandPayload payload,
        WorldCommandExecutionContext context,
        out LocalTopologyRuntime topology,
        out LocalPlaceRuntime origin,
        out LocalPlaceRuntime destination,
        out LocalConnectionTypeData typeDefinition,
        out string diagnostic)
    {
        topology = null;
        origin = null;
        destination = null;
        typeDefinition = null;
        diagnostic = null;
        if (Dependencies.LocalTopologyStore.TryGetTopologyForOwner(payload.TopologyOwnerRuntimeId, out topology) == false
            || topology == null
            || topology.IsPublished == false
            || topology.TryGetPlace(payload.OriginLocalPlaceRuntimeId, out origin) == false
            || topology.TryGetPlace(payload.DestinationLocalPlaceRuntimeId, out destination) == false
            || LocalTopologyConnectionRuntime.IsValidTraversalCost(payload.TraversalCost) == false)
        {
            diagnostic = "Local connection requires a published topology, two existing places, and a valid traversal cost.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(payload.ConnectionTypeDefinitionId) == false
            && (ResolveDefinitions(context)?.TryResolveLocalConnectionType(payload.ConnectionTypeDefinitionId, out typeDefinition) != true || typeDefinition == null))
        {
            diagnostic = "ConnectionTypeDefinitionId is not registered.";
            return false;
        }

        return true;
    }
}

public sealed class GrantSiteKnowledgeWorldCommandHandler : CoreWorldCommandHandlerBase
{
    public GrantSiteKnowledgeWorldCommandHandler(CoreWorldCommandDependencies dependencies) : base(dependencies) { }
    public override WorldCommandKind Kind => WorldCommandKind.GrantSiteKnowledge;

    public override WorldCommandPreview Preview(WorldCommand command)
    {
        GrantSiteKnowledgeWorldCommandPayload payload = command?.Payload as GrantSiteKnowledgeWorldCommandPayload;
        if (payload == null)
        {
            return Invalid(command, "Site knowledge grant requires a typed payload.");
        }

        if (TryValidatePreviewAuthority(command, out string authorityDiagnostic) == false)
        {
            return Invalid(command, authorityDiagnostic);
        }

        if (Dependencies.IdentityRegistry.TryGetNpcWithoutLogging(payload.NpcRuntimeId, out _) == false
            || Dependencies.IdentityRegistry.TryGetExplorableSiteWithoutLogging(payload.SiteRuntimeId, out ExplorableSiteRuntime site) == false
            || site == null)
        {
            return Invalid(command, "Site knowledge requires registered NPC and site identities.");
        }

        return Valid(command, "Grant site knowledge " + payload.SiteRuntimeId + " to " + payload.NpcRuntimeId + ".", new[] { payload.NpcRuntimeId, payload.SiteRuntimeId });
    }

    public override WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context)
    {
        if (RuntimeIsFaulted) return Failure("Runtime mutation is faulted.");
        GrantSiteKnowledgeWorldCommandPayload payload = command?.Payload as GrantSiteKnowledgeWorldCommandPayload;
        if (payload == null)
        {
            return Failure("Site knowledge grant requires a typed payload.");
        }

        if (TryValidateExecutionAuthority(command, out string authorityDiagnostic) == false)
        {
            return Failure(authorityDiagnostic);
        }

        if (Dependencies.IdentityRegistry.TryGetNpcWithoutLogging(payload.NpcRuntimeId, out NpcRuntime npc) == false
            || Dependencies.IdentityRegistry.TryGetExplorableSiteWithoutLogging(payload.SiteRuntimeId, out ExplorableSiteRuntime site) == false
            || npc == null
            || site == null)
        {
            return Failure("Site knowledge requires registered NPC and site identities.");
        }

        ExplorableSiteKnowledgeObservation observation = new ExplorableSiteKnowledgeObservation(
            site.RuntimeId,
            site.Location.RuntimeId,
            0L,
            0L,
            payload.Source);
        npc.ExplorableSiteKnowledge.RecordObservation(observation);
        npc.SpatialKnowledge.DiscoverLocation(site.Location.RuntimeId);
        return Success(new[] { npc.RuntimeId, site.RuntimeId });
    }
}

public sealed class GrantAdventureIntelWorldCommandHandler : CoreWorldCommandHandlerBase
{
    public GrantAdventureIntelWorldCommandHandler(CoreWorldCommandDependencies dependencies) : base(dependencies) { }
    public override WorldCommandKind Kind => WorldCommandKind.GrantAdventureIntel;

    public override WorldCommandPreview Preview(WorldCommand command)
    {
        GrantAdventureIntelWorldCommandPayload payload = command?.Payload as GrantAdventureIntelWorldCommandPayload;
        if (payload == null)
        {
            return Invalid(command, "Adventure intel grant requires a typed payload.");
        }

        if (TryValidatePreviewAuthority(command, out string authorityDiagnostic) == false)
        {
            return Invalid(command, authorityDiagnostic);
        }

        if (Dependencies.IdentityRegistry.TryGetNpcWithoutLogging(payload.NpcRuntimeId, out _) == false)
        {
            return Invalid(command, "Adventure intel requires a registered NPC identity.");
        }

        return Valid(command, "Grant " + payload.IntelKind + " intel to " + payload.NpcRuntimeId + ".", new[] { payload.NpcRuntimeId, payload.SiteRuntimeId, payload.LocalPlaceRuntimeId, payload.OppositionRuntimeId, payload.NotableItemRuntimeId });
    }

    public override WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context)
    {
        if (RuntimeIsFaulted) return Failure("Runtime mutation is faulted.");
        GrantAdventureIntelWorldCommandPayload payload = command?.Payload as GrantAdventureIntelWorldCommandPayload;
        if (payload == null)
        {
            return Failure("Adventure intel grant requires a typed payload.");
        }

        if (TryValidateExecutionAuthority(command, out string authorityDiagnostic) == false)
        {
            return Failure(authorityDiagnostic);
        }

        if (Dependencies.IdentityRegistry.TryGetNpcWithoutLogging(payload.NpcRuntimeId, out NpcRuntime npc) == false || npc == null)
        {
            return Failure("Adventure intel requires a registered NPC identity.");
        }

        try
        {
            string sourceRuntimeId = payload.Source == AdventureIntelSource.SharedByNpc ? payload.NpcRuntimeId : null;
            switch (payload.IntelKind)
            {
                case AdventureIntelDeclarationKind.Opposition:
                    if (string.IsNullOrWhiteSpace(payload.OppositionRuntimeId) || !payload.OppositionState.HasValue)
                    {
                        return Failure("Opposition intel requires an opposition RuntimeId and observed state.");
                    }

                    npc.AdventureSiteIntelKnowledge.RecordObservation(new AdventureOppositionObservation(
                        payload.SiteRuntimeId,
                        payload.LocalPlaceRuntimeId,
                        payload.OppositionRuntimeId,
                        payload.OppositionState.Value,
                        payload.ObservedDay,
                        payload.ReceivedDay,
                        payload.Source,
                        sourceRuntimeId));
                    break;
                case AdventureIntelDeclarationKind.NotableItem:
                    if (string.IsNullOrWhiteSpace(payload.NotableItemRuntimeId) || string.IsNullOrWhiteSpace(payload.ItemDefinitionId))
                    {
                        return Failure("Notable intel requires notable and item definition IDs.");
                    }

                    npc.AdventureSiteIntelKnowledge.RecordObservation(new AdventureNotableItemObservation(
                        payload.SiteRuntimeId,
                        payload.LocalPlaceRuntimeId,
                        payload.NotableItemRuntimeId,
                        payload.ItemDefinitionId,
                        payload.ObservedDay,
                        payload.ReceivedDay,
                        payload.Source,
                        sourceRuntimeId));
                    break;
                case AdventureIntelDeclarationKind.CommonResource:
                    if (string.IsNullOrWhiteSpace(payload.ItemDefinitionId) || !payload.ObservedAmount.HasValue)
                    {
                        return Failure("Common-resource intel requires an item definition and observed amount.");
                    }

                    npc.AdventureSiteIntelKnowledge.RecordObservation(new AdventureCommonResourceObservation(
                        payload.SiteRuntimeId,
                        payload.LocalPlaceRuntimeId,
                        payload.ItemDefinitionId,
                        payload.ObservedAmount.Value,
                        null,
                        payload.ObservedDay,
                        payload.ReceivedDay,
                        payload.Source,
                        sourceRuntimeId));
                    break;
                case AdventureIntelDeclarationKind.Access:
                    if (!payload.AccessState.HasValue)
                    {
                        return Failure("Access intel requires an observed access state.");
                    }

                    npc.AdventureSiteIntelKnowledge.RecordObservation(new AdventureAccessObservation(
                        payload.SiteRuntimeId,
                        payload.LocalPlaceRuntimeId,
                        payload.AccessState.Value,
                        payload.ObservedDay,
                        payload.ReceivedDay,
                        payload.Source,
                        sourceRuntimeId));
                    break;
                default:
                    return Failure("Adventure intel declaration kind is invalid.");
            }
        }
        catch (ArgumentException exception)
        {
            return Failure(exception.Message);
        }

        return Success(new[] { npc.RuntimeId, payload.SiteRuntimeId, payload.LocalPlaceRuntimeId, payload.OppositionRuntimeId, payload.NotableItemRuntimeId });
    }
}

public sealed class ResolveConflictWorldCommandHandler : CoreWorldCommandHandlerBase
{
    private readonly WorldCommandKind kind;

    public ResolveConflictWorldCommandHandler(WorldCommandKind kind, CoreWorldCommandDependencies dependencies) : base(dependencies)
    {
        if (kind != WorldCommandKind.ResolveConflict && kind != WorldCommandKind.PlaceOpposition)
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        this.kind = kind;
    }

    public override WorldCommandKind Kind => kind;

    public override WorldCommandPreview Preview(WorldCommand command)
    {
        ResolveConflictWorldCommandPayload payload = command?.Payload as ResolveConflictWorldCommandPayload;
        if (payload == null)
        {
            return Invalid(command, "Conflict command requires a typed payload.");
        }

        if (TryValidatePreviewAuthority(command, out string authorityDiagnostic) == false)
        {
            return Invalid(command, authorityDiagnostic);
        }

        if (command.Authority != WorldCommandAuthorityMode.ForceOutcome
            && ContainsForcedConflictFields(payload))
        {
            return Invalid(command, "Forced conflict constraints require ForceOutcome authority.");
        }

        if (kind == WorldCommandKind.PlaceOpposition && string.IsNullOrWhiteSpace(payload.OppositionRuntimeId))
        {
            return Invalid(command, "Place opposition command requires an OppositionRuntimeId.");
        }

        return Valid(command, "Resolve structured conflict" + (payload.OppositionRuntimeId == null ? "." : " against " + payload.OppositionRuntimeId + "."), new[] { payload.LocationRuntimeId, payload.OppositionRuntimeId });
    }

    public override WorldCommandHandlerResult Execute(WorldCommand command, WorldCommandExecutionContext context)
    {
        if (RuntimeIsFaulted) return Failure("Runtime mutation is faulted.");
        ResolveConflictWorldCommandPayload payload = command?.Payload as ResolveConflictWorldCommandPayload;
        if (payload == null)
        {
            return Failure("Conflict command requires a typed payload.");
        }

        if (TryValidateExecutionAuthority(command, out string authorityDiagnostic) == false)
        {
            return Failure(authorityDiagnostic);
        }

        if (kind == WorldCommandKind.PlaceOpposition && string.IsNullOrWhiteSpace(payload.OppositionRuntimeId))
        {
            return Failure("Place opposition command requires an OppositionRuntimeId.");
        }

        if (TryBuildConflict(command, context, payload, out Conflict conflict, out string buildDiagnostic) == false)
        {
            return Failure(buildDiagnostic);
        }

        if (TryBuildConstraints(command, payload, conflict, out ConflictResolutionConstraints constraints, out string constraintDiagnostic) == false)
        {
            return Failure(constraintDiagnostic);
        }

        int eventCountBefore = Dependencies.DomainEventStore?.Events.Count ?? 0;
        bool resolved;
        ConflictResolutionResult result;
        string diagnostic;
        if (string.IsNullOrWhiteSpace(payload.OppositionRuntimeId) == false)
        {
            if (TryFindOpposition(payload, out PlaceContentOwnerReference owner, out _, out PlaceOppositionRuntime opposition, out diagnostic) == false)
            {
                return Failure(diagnostic);
            }

            if (Dependencies.PlaceContentStore.TryResolveOpposition(
                owner,
                opposition,
                conflict,
                Dependencies.ConflictResolutionService,
                constraints,
                Dependencies.WorldRuntime,
                out result,
                out diagnostic) == false)
            {
                return Failure(diagnostic);
            }

            resolved = true;
        }
        else
        {
            resolved = Dependencies.ConflictResolutionService.TryResolveAndApply(
                conflict,
                constraints,
                Dependencies.WorldRuntime,
                out result,
                out diagnostic);
        }

        if (resolved == false)
        {
            return Failure(diagnostic ?? "Conflict resolution was rejected.");
        }

        TryCaptureNewEventIds(eventCountBefore, out IReadOnlyList<string> eventIds);
        return Success(new[] { payload.LocationRuntimeId, payload.OppositionRuntimeId }, events: eventIds);
    }

    private bool TryBuildConflict(
        WorldCommand command,
        WorldCommandExecutionContext context,
        ResolveConflictWorldCommandPayload payload,
        out Conflict conflict,
        out string diagnostic)
    {
        conflict = null;
        diagnostic = null;
        if (payload.Sides == null || payload.Sides.Count < 2)
        {
            diagnostic = "A structured conflict requires at least two sides.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(payload.LocationRuntimeId) == false
            && Dependencies.IdentityRegistry.TryGetLocation(payload.LocationRuntimeId, out _) == false
            && TryFindOpposition(payload, out _, out _, out _, out _) == false)
        {
            diagnostic = "Conflict location is not a registered macro location or targeted place context.";
            return false;
        }

        conflict = new Conflict(context.WorldCommandId, payload.LocationRuntimeId);
        try
        {
            foreach (WorldConflictSidePayload sidePayload in payload.Sides)
            {
                if (sidePayload == null)
                {
                    diagnostic = "Structured conflict contains a null side.";
                    conflict = null;
                    return false;
                }

                ConflictSide side = conflict.AddSide(sidePayload.SideId, sidePayload.Objective, sidePayload.Stakes);
                foreach (WorldConflictParticipantPayload participantPayload in sidePayload.Participants)
                {
                    if (participantPayload == null)
                    {
                        diagnostic = "Structured conflict contains a null participant.";
                        conflict = null;
                        return false;
                    }

                    if (participantPayload.IsAggregate)
                    {
                        side.AddAggregate(participantPayload.Aggregate);
                    }
                    else if (Dependencies.IdentityRegistry.TryGetNpcWithoutLogging(participantPayload.NpcRuntimeId, out NpcRuntime npc) == true
                        && npc != null
                        && npc.IsAlive)
                    {
                        side.AddNpc(npc);
                    }
                    else
                    {
                        diagnostic = "Structured conflict references an unregistered, fake, or dead NPC runtime.";
                        conflict = null;
                        return false;
                    }
                }
            }
        }
        catch (ArgumentException exception)
        {
            diagnostic = exception.Message;
            conflict = null;
            return false;
        }
        catch (InvalidOperationException exception)
        {
            diagnostic = exception.Message;
            conflict = null;
            return false;
        }

        if (conflict.TryValidate(out diagnostic) == false)
        {
            conflict = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(payload.OppositionRuntimeId) == false)
        {
            if (TryFindOpposition(payload, out _, out _, out PlaceOppositionRuntime opposition, out diagnostic) == false
                || opposition.TryValidateConflictBinding(conflict, out diagnostic) == false)
            {
                conflict = null;
                return false;
            }

            if (TryValidateOppositionAggregates(conflict, opposition, out diagnostic) == false)
            {
                conflict = null;
                return false;
            }
        }

        return true;
    }

    private static bool TryValidateOppositionAggregates(
        Conflict conflict,
        PlaceOppositionRuntime opposition,
        out string diagnostic)
    {
        diagnostic = null;
        Dictionary<string, AggregateParticipantSnapshot> expected = new Dictionary<string, AggregateParticipantSnapshot>(StringComparer.Ordinal);
        foreach (AggregateParticipantSnapshot aggregate in opposition.AggregateParticipants)
        {
            if (aggregate == null || expected.ContainsKey(aggregate.SourceId))
            {
                diagnostic = "Place opposition aggregate identity is invalid.";
                return false;
            }

            expected.Add(aggregate.SourceId, aggregate);
        }

        foreach (ConflictSide side in conflict.Sides)
        {
            if (side == null || side.SideId != opposition.OppositionSideId)
            {
                continue;
            }

            foreach (ConflictParticipantReference participant in side.Participants)
            {
                if (participant == null || participant.IsAggregate == false)
                {
                    continue;
                }

                if (expected.TryGetValue(participant.Aggregate.SourceId, out AggregateParticipantSnapshot source) == false
                    || source.BaseCapability != participant.Aggregate.BaseCapability
                    || source.Count != participant.Aggregate.Count
                    || string.Equals(source.DisplayName, participant.Aggregate.DisplayName, StringComparison.Ordinal) == false)
                {
                    diagnostic = "Place opposition aggregate participant does not match the World Truth snapshot.";
                    return false;
                }
            }
        }

        return true;
    }
}
