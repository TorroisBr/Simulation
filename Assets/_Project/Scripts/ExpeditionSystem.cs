using System;
using System.Collections.Generic;

public sealed class ExpeditionSystem
{
    private static readonly ActionParticipationRequirements ExpeditionRequirements =
        new ActionParticipationRequirements(
            minPerformers: 1,
            maxPerformers: ActionParticipationRequirements.Unlimited,
            minSupports: 0,
            maxSupports: ActionParticipationRequirements.Unlimited,
            minTargets: 0,
            maxTargets: 0);

    private readonly ExpeditionStore expeditionStore;
    private readonly RuntimeIdAllocator idAllocator;
    private readonly RuntimeIdentityRegistry identityRegistry;
    private readonly ExplorableSiteStore explorableSiteStore;
    private readonly TravelPartySystem travelPartySystem;
    private readonly TravelPartyStore travelPartyStore;
    private readonly ExplorableSiteKnowledgeSystem explorableSiteKnowledgeSystem;
    private readonly LocalTopologyKnowledgeSystem localTopologyKnowledgeSystem;
    private readonly SimulationTime simulationTime;
    private readonly IDomainEventRecorder domainEventRecorder;
    private readonly SimulationLogger logger;
    private readonly PlaceContentStore placeContentStore;
    private readonly LocalTopologyStore localTopologyStore;
    private SimulationRuntime worldRuntime;

    public ExpeditionStore Store => expeditionStore;

    internal void BindWorldRuntime(SimulationRuntime worldRuntime)
    {
        if (worldRuntime == null)
        {
            throw new ArgumentNullException(nameof(worldRuntime));
        }

        if (this.worldRuntime != null && ReferenceEquals(this.worldRuntime, worldRuntime) == false)
        {
            throw new InvalidOperationException("An ExpeditionSystem cannot be bound to more than one SimulationRuntime.");
        }

        this.worldRuntime = worldRuntime;
    }

    public ExpeditionSystem(
        ExpeditionStore expeditionStore,
        RuntimeIdAllocator idAllocator,
        RuntimeIdentityRegistry identityRegistry,
        ExplorableSiteStore explorableSiteStore,
        TravelPartySystem travelPartySystem,
        TravelPartyStore travelPartyStore,
        ExplorableSiteKnowledgeSystem explorableSiteKnowledgeSystem,
        SimulationTime simulationTime,
        IDomainEventRecorder domainEventRecorder = null,
        SimulationLogger logger = null,
        PlaceContentStore placeContentStore = null,
        LocalTopologyStore localTopologyStore = null)
    {
        this.expeditionStore = expeditionStore ?? throw new ArgumentNullException(nameof(expeditionStore));
        this.idAllocator = idAllocator ?? throw new ArgumentNullException(nameof(idAllocator));
        this.identityRegistry = identityRegistry ?? throw new ArgumentNullException(nameof(identityRegistry));
        this.explorableSiteStore = explorableSiteStore ?? throw new ArgumentNullException(nameof(explorableSiteStore));
        this.travelPartySystem = travelPartySystem ?? throw new ArgumentNullException(nameof(travelPartySystem));
        this.travelPartyStore = travelPartyStore ?? throw new ArgumentNullException(nameof(travelPartyStore));
        this.explorableSiteKnowledgeSystem = explorableSiteKnowledgeSystem
            ?? throw new ArgumentNullException(nameof(explorableSiteKnowledgeSystem));
        this.localTopologyKnowledgeSystem = new LocalTopologyKnowledgeSystem();
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        this.domainEventRecorder = domainEventRecorder;
        this.logger = logger ?? new SimulationLogger(null);
        this.placeContentStore = placeContentStore;
        this.localTopologyStore = localTopologyStore;
    }

    public bool TryStartExpedition(
        ExplorableSiteRuntime targetSite,
        ActionExecutionContext context,
        out ExpeditionRuntime expedition)
    {
        return TryStartExpedition(targetSite, context, null, out expedition);
    }

    public bool TryStartExpedition(
        ExplorableSiteRuntime targetSite,
        ActionExecutionContext context,
        ExpeditionObjectiveRuntime objective,
        out ExpeditionRuntime expedition)
    {
        expedition = null;

        if (TryPrepareStart(targetSite, context, out ExpeditionPreparation preparation, out string reason) == false)
        {
            if (string.IsNullOrWhiteSpace(reason) == false)
            {
                logger.LogWarning("Expedition start rejected: " + reason);
            }

            return false;
        }

        string expeditionId;

        try
        {
            expeditionId = idAllocator.AllocateExpeditionId();
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError("Cannot allocate ExpeditionId: " + exception.Message);
            return false;
        }

        ExpeditionRuntime createdExpedition;

        try
        {
            createdExpedition = new ExpeditionRuntime(
                expeditionId,
                targetSite.RuntimeId,
                preparation.OriginLocation.RuntimeId,
                targetSite.Location.RuntimeId,
                preparation.Route.RuntimeId,
                null,
                context.OriginDecisionId,
                preparation.MemberRuntimeIds,
                preparation.PerformerRuntimeIds,
                preparation.SupportRuntimeIds,
                ExpeditionState.Preparing,
                objective);
        }
        catch (ArgumentException exception)
        {
            logger.LogError("Cannot create expedition: " + exception.Message);
            return false;
        }

        if (expeditionStore.Add(createdExpedition) == false)
        {
            return false;
        }

        if (travelPartySystem.TryStartTravelParty(context, out TravelPartyRuntime party) == false)
        {
            expeditionStore.Remove(createdExpedition.ExpeditionId);
            return false;
        }

        if (createdExpedition.TryBeginTravel(party.TravelPartyId) == false)
        {
            // The preparation validation makes this unreachable for a valid party.
            // Keep the store coherent if a future lifecycle change invalidates it.
            expeditionStore.Remove(createdExpedition.ExpeditionId);
            logger.LogError("Expedition could not enter TravelingToSite after TravelParty creation.");
            return false;
        }

        bool eventRecorded = domainEventRecorder == null || domainEventRecorder.Record(
            (eventId, absoluteDay, recordSequence) => new ExpeditionStartedEvent(
                eventId,
                absoluteDay,
                recordSequence,
                createdExpedition.ExpeditionId,
                createdExpedition.TargetSiteRuntimeId,
                createdExpedition.OriginLocationRuntimeId,
                createdExpedition.TargetLocationRuntimeId,
                createdExpedition.TravelPartyId,
                createdExpedition.PerformerRuntimeIds,
                createdExpedition.SupportRuntimeIds,
                createdExpedition.OriginDecisionId));

        if (eventRecorded == false)
        {
            logger.LogWarning("Expedition start event could not be recorded for ExpeditionId '" + createdExpedition.ExpeditionId + "'.");
        }

        expedition = createdExpedition;
        return true;
    }

    public bool IsNpcOnActiveExpedition(string npcRuntimeId)
    {
        return expeditionStore.IsNpcOnActiveExpedition(npcRuntimeId);
    }

    public bool CanBeginExploration(ExpeditionRuntime expedition, out string reason)
    {
        reason = null;
        if (TryValidateActiveExpedition(expedition, out reason) == false)
        {
            return false;
        }

        if (expedition.CanBeginExploration() == false)
        {
            reason = "An expedition can begin exploring only after reaching its target site.";
            return false;
        }

        return true;
    }

    public bool TryBeginExploration(ExpeditionRuntime expedition, out string reason)
    {
        if (CanBeginExploration(expedition, out reason) == false)
        {
            return false;
        }

        if (expedition.TryBeginExploration() == false)
        {
            reason = "Expedition could not enter the Exploring state.";
            return false;
        }

        RecordLifecycleEvent(
            () => domainEventRecorder.Record(
                (eventId, absoluteDay, recordSequence) => new ExpeditionExplorationStartedEvent(
                    eventId,
                    absoluteDay,
                    recordSequence,
                    expedition,
                    expedition.TargetLocationRuntimeId)),
            "exploration-started",
            expedition);

        return true;
    }

    public bool CanContinueExploration(ExpeditionRuntime expedition, out string reason)
    {
        reason = null;
        if (TryValidateActiveExpedition(expedition, out reason) == false)
        {
            return false;
        }

        if (expedition.CanContinueExploration() == false)
        {
            reason = "Expedition is not allowed to continue exploring in its current state.";
            return false;
        }

        return true;
    }

    public bool TryContinueExploration(ExpeditionRuntime expedition, out string reason)
    {
        if (CanContinueExploration(expedition, out reason) == false)
        {
            return false;
        }

        if (localTopologyStore != null
            && localTopologyStore.TryGetTopologyForOwner(expedition.TargetSiteRuntimeId, out LocalTopologyRuntime topology) == true
            && topology != null)
        {
            reason = "A detailed topology requires an explicit LocalPlaceRuntime or connection target.";
            return false;
        }

        int previousProgress = expedition.ExplorationProgress;
        bool objectiveWasCompleted = expedition.IsObjectiveComplete;
        if (expedition.TryAdvanceAbstractProgress() == false)
        {
            reason = "Abstract exploration progress could not advance.";
            return false;
        }

        if (expedition.ExplorationProgress > previousProgress)
        {
            RecordAdvanced(expedition, null);
        }

        RecordObjectiveCompletedIfNeeded(expedition, objectiveWasCompleted, GetObjectiveTargetId(expedition.Objective));

        return true;
    }

    public bool TryContinueExploration(
        ExpeditionRuntime expedition,
        LocalPlaceRuntime place,
        out string reason)
    {
        return TryExploreLocalPlace(expedition, place, out reason);
    }

    public bool TryExploreLocalPlace(
        ExpeditionRuntime expedition,
        LocalPlaceRuntime place,
        out string reason)
    {
        reason = null;
        if (CanContinueExploration(expedition, out reason) == false)
        {
            return false;
        }

        if (place == null
            || TryGetPublishedTargetTopology(expedition, out LocalTopologyRuntime topology) == false
            || topology.ContainsPlace(place) == false)
        {
            reason = "Local exploration target must belong to the published target-site topology.";
            return false;
        }

        if (expedition.CurrentLocalPlaceRuntimeId == null)
        {
            if (topology.IsEntryPoint(place) == false)
            {
                reason = "Detailed exploration must enter through a published topology entry point.";
                return false;
            }
        }
        else if (string.Equals(expedition.CurrentLocalPlaceRuntimeId, place.RuntimeId, StringComparison.Ordinal) == false)
        {
            reason = "Direct local exploration cannot teleport between places; use a directed connection.";
            return false;
        }

        if (TryResolveMembers(expedition, out List<NpcRuntime> members, out reason) == false)
        {
            return false;
        }

        bool objectiveWasCompleted = expedition.IsObjectiveComplete;
        if (expedition.TrySetCurrentLocalPlace(place.RuntimeId, out bool firstVisit) == false)
        {
            reason = "Expedition could not record its local exploration position.";
            return false;
        }

        foreach (NpcRuntime member in members)
        {
            localTopologyKnowledgeSystem.RecordDirectObservation(
                member,
                topology,
                place,
                simulationTime.AbsoluteDay);
        }

        if (firstVisit == true)
        {
            RecordAdvanced(expedition, place.RuntimeId);
        }

        RecordObjectiveCompletedIfNeeded(expedition, objectiveWasCompleted, place.RuntimeId);

        return true;
    }

    public bool TryTraverseLocalConnection(
        ExpeditionRuntime expedition,
        LocalTopologyConnectionRuntime connection,
        out string reason)
    {
        reason = null;
        if (CanContinueExploration(expedition, out reason) == false)
        {
            return false;
        }

        if (connection == null
            || TryGetPublishedTargetTopology(expedition, out LocalTopologyRuntime topology) == false
            || topology.ContainsConnection(connection) == false)
        {
            reason = "Local traversal requires a connection from the published target-site topology.";
            return false;
        }

        if (expedition.CurrentLocalPlaceRuntimeId == null
            || string.Equals(expedition.CurrentLocalPlaceRuntimeId, connection.Origin.RuntimeId, StringComparison.Ordinal) == false)
        {
            reason = "Local traversal must follow a directed connection from the expedition's current place.";
            return false;
        }

        if (TryResolveMembers(expedition, out List<NpcRuntime> members, out reason) == false)
        {
            return false;
        }

        bool objectiveWasCompleted = expedition.IsObjectiveComplete;
        if (expedition.TryTraverseLocalConnection(
            connection.RuntimeId,
            connection.Destination.RuntimeId,
            out bool advanced) == false)
        {
            reason = "Expedition could not record the directed local traversal.";
            return false;
        }

        foreach (NpcRuntime member in members)
        {
            localTopologyKnowledgeSystem.RecordDirectObservation(
                member,
                topology,
                connection,
                simulationTime.AbsoluteDay);
            localTopologyKnowledgeSystem.RecordDirectObservation(
                member,
                topology,
                connection.Destination,
                simulationTime.AbsoluteDay);
        }

        if (advanced == true)
        {
            RecordAdvanced(expedition, connection.RuntimeId);
        }

        RecordObjectiveCompletedIfNeeded(expedition, objectiveWasCompleted, connection.Destination.RuntimeId);

        return true;
    }

    public bool TryRetrieveTargetResource(
        ExpeditionRuntime expedition,
        ItemData item,
        int amount,
        out string reason)
    {
        ExplorableSiteRuntime site = expedition != null
            ? explorableSiteStore.GetByRuntimeId(expedition.TargetSiteRuntimeId)
            : null;
        PlaceContentOwnerReference owner = site != null
            ? PlaceContentOwnerReference.ForExplorableSite(site)
            : null;
        return TryRetrieveTargetResource(
            expedition,
            owner,
            item,
            amount,
            out reason);
    }

    public bool TryRetrieveTargetResource(
        ExpeditionRuntime expedition,
        PlaceContentOwnerReference owner,
        ItemData item,
        int amount,
        out string reason)
    {
        reason = null;
        if (CanContinueExploration(expedition, out reason) == false
            || placeContentStore == null
            || owner == null
            || item == null
            || amount <= 0)
        {
            reason = reason ?? "Retrieval requires an active exploration and a valid content owner, item, and amount.";
            return false;
        }

        if (identityRegistry.TryGetNpc(expedition.PerformerRuntimeIds[0], out NpcRuntime performer) == false
            || performer == null
            || performer.IsAlive == false
            || performer.Inventory.CanAddItem(item, amount) == false
            || TryValidateContentContext(expedition, owner, true, out PlaceContentRuntime content, out reason) == false
            || content.GetAmount(item) < amount
            || content.GetStack(item) == null)
        {
            reason = reason ?? "Target resource is not available in the expedition's current accessible place or the performer cannot carry it.";
            return false;
        }

        bool objectiveWasCompleted = expedition.IsObjectiveComplete;
        float averageUnitCost = content.GetStack(item).AverageUnitCost;
        if (placeContentStore.TryTakeStack(owner, item, amount, out int removedAmount) == false)
        {
            reason = "Target resource could not be removed atomically from the place.";
            return false;
        }

        performer.Inventory.AddItem(item, removedAmount, averageUnitCost);
        if (expedition.Objective.ObjectiveType == ExpeditionObjectiveType.Retrieve
            && expedition.Objective.TargetItemDefinitionId == item.DefinitionId)
        {
            expedition.TryMarkObjectiveComplete();
        }

        RecordObjectiveCompletedIfNeeded(expedition, objectiveWasCompleted, item.DefinitionId);

        return true;
    }

    public bool TryRetrieveNotableItem(
        ExpeditionRuntime expedition,
        string notableItemRuntimeId,
        out NotableItemRuntime notable,
        out string reason)
    {
        notable = null;
        reason = null;
        if (expedition == null || expedition.PerformerRuntimeIds.Count == 0
            || identityRegistry.TryGetNpc(expedition.PerformerRuntimeIds[0], out NpcRuntime performer) == false)
        {
            reason = "Notable retrieval requires a resolvable expedition Performer.";
            return false;
        }

        return TryRetrieveNotableItem(expedition, notableItemRuntimeId, performer, out notable, out reason);
    }

    public bool TryRetrieveNotableItem(
        ExpeditionRuntime expedition,
        string notableItemRuntimeId,
        NpcRuntime destinationPerformer,
        out NotableItemRuntime notable,
        out string reason)
    {
        notable = null;
        reason = null;
        if (CanContinueExploration(expedition, out reason) == false
            || placeContentStore == null
            || string.IsNullOrWhiteSpace(notableItemRuntimeId) == true
            || destinationPerformer == null)
        {
            reason = reason ?? "Notable retrieval requires an active exploration, item RuntimeId, and destination Performer.";
            return false;
        }

        if (ContainsId(expedition.PerformerRuntimeIds, destinationPerformer.RuntimeId) == false
            || destinationPerformer.IsAlive == false
            || identityRegistry.TryGetNpcWithoutLogging(destinationPerformer.RuntimeId, out NpcRuntime registeredPerformer) == false
            || ReferenceEquals(registeredPerformer, destinationPerformer) == false)
        {
            reason = "Notable item destination must be a living registered Performer of this expedition.";
            return false;
        }

        if (placeContentStore.TryGetNotableItem(notableItemRuntimeId, out NotableItemRuntime storedNotable) == false
            || storedNotable.Custody?.CustodyKind != NotableItemCustodyKind.Place
            || storedNotable.Custody.PlaceOwner == null)
        {
            reason = "The exact notable item is not currently held at a place.";
            return false;
        }

        PlaceContentOwnerReference sourceOwner = storedNotable.Custody.PlaceOwner;
        if (TryValidateContentContext(expedition, sourceOwner, true, out _, out reason) == false)
        {
            return false;
        }

        bool objectiveWasCompleted = expedition.IsObjectiveComplete;
        if (placeContentStore.TryTransferNotableFromPlaceToNpc(
            sourceOwner,
            notableItemRuntimeId,
            destinationPerformer,
            out notable,
            out reason) == false)
        {
            return false;
        }

        if (expedition.Objective.ObjectiveType == ExpeditionObjectiveType.Retrieve
            && string.Equals(
                expedition.Objective.TargetNotableItemRuntimeId,
                notableItemRuntimeId,
                StringComparison.Ordinal) == true)
        {
            expedition.TryMarkObjectiveComplete();
        }

        RecordObjectiveCompletedIfNeeded(expedition, objectiveWasCompleted, notableItemRuntimeId);
        return true;
    }

    public bool TryResolvePlaceOpposition(
        ExpeditionRuntime expedition,
        PlaceContentOwnerReference owner,
        PlaceOppositionRuntime opposition,
        Conflict conflict,
        ConflictResolutionService conflictResolutionService,
        out ConflictResolutionResult result,
        out string reason)
    {
        result = null;
        reason = null;
        if (CanContinueExploration(expedition, out reason) == false
            || placeContentStore == null)
        {
            reason = reason ?? "Opposition resolution requires an active exploration and a content store.";
            return false;
        }

        if (TryValidateContentContext(expedition, owner, false, out _, out reason) == false)
        {
            return false;
        }

        if (TryValidateExpeditionConflictBinding(expedition, opposition, conflict, out reason) == false)
        {
            return false;
        }

        bool objectiveWasCompleted = expedition.IsObjectiveComplete;
        if (placeContentStore.TryResolveOpposition(
            owner,
            opposition,
            conflict,
            conflictResolutionService,
            null,
            worldRuntime,
            out result,
            out reason) == false)
        {
            return false;
        }

        if (expedition.Objective.ObjectiveType == ExpeditionObjectiveType.Eliminate
            && expedition.Objective.TargetOppositionRuntimeId == opposition.RuntimeId
            && opposition.IsResolved)
        {
            expedition.TryMarkObjectiveComplete();
        }

        RecordObjectiveCompletedIfNeeded(expedition, objectiveWasCompleted, opposition.RuntimeId);

        return true;
    }

    private bool TryValidateExpeditionConflictBinding(
        ExpeditionRuntime expedition,
        PlaceOppositionRuntime opposition,
        Conflict conflict,
        out string reason)
    {
        reason = null;
        if (expedition == null || opposition == null || conflict == null)
        {
            reason = "Expedition conflict binding requires an expedition, opposition, and conflict.";
            return false;
        }

        if (conflict.TryValidate(out reason) == false)
        {
            return false;
        }

        List<ConflictSide> nonOppositionSides = new List<ConflictSide>();
        foreach (ConflictSide side in conflict.Sides)
        {
            if (side == null)
            {
                reason = "Expedition conflict binding cannot contain a null side.";
                return false;
            }

            if (string.Equals(side.SideId, opposition.OppositionSideId, StringComparison.Ordinal) == false)
            {
                nonOppositionSides.Add(side);
            }
        }

        if (nonOppositionSides.Count != 1)
        {
            reason = "Place opposition integration requires exactly one non-opposition expedition side.";
            return false;
        }

        ConflictSide expeditionSide = nonOppositionSides[0];
        bool hasPerformer = false;
        foreach (ConflictParticipantReference participant in expeditionSide.Participants)
        {
            if (participant == null || participant.IsNpc == false || participant.Npc == null)
            {
                reason = "The expedition conflict side may contain only real expedition NPC participants.";
                return false;
            }

            NpcRuntime participantNpc = participant.Npc;
            if (identityRegistry.TryGetNpcWithoutLogging(participantNpc.RuntimeId, out NpcRuntime registeredNpc) == false
                || ReferenceEquals(registeredNpc, participantNpc) == false)
            {
                reason = "Every expedition conflict NPC must be the registered runtime instance for its RuntimeId.";
                return false;
            }

            if (participantNpc.IsAlive == false)
            {
                reason = "A dead expedition NPC cannot participate in a new conflict.";
                return false;
            }

            if (ContainsId(expedition.MemberRuntimeIds, participantNpc.RuntimeId) == false)
            {
                reason = "Every expedition conflict NPC must belong to the active expedition.";
                return false;
            }

            if (ContainsId(expedition.PerformerRuntimeIds, participantNpc.RuntimeId) == true)
            {
                hasPerformer = true;
            }
        }

        if (hasPerformer == false)
        {
            reason = "An expedition conflict must contain at least one expedition Performer.";
            return false;
        }

        return true;
    }

    public bool TryBeginReturn(ExpeditionRuntime expedition, out string reason)
    {
        reason = null;
        if (TryValidateActiveExpedition(expedition, out reason) == false
            || expedition.CanBeginReturn() == false)
        {
            reason = reason ?? "Expedition can begin returning only from AtSite or Exploring.";
            return false;
        }

        if (identityRegistry.TryFindRouteBetweenLocations(
            expedition.TargetLocationRuntimeId,
            expedition.OriginLocationRuntimeId,
            out SpatialRouteRuntime returnRoute) == false
            || returnRoute == null)
        {
            reason = "No unique real macro return route exists for this expedition.";
            return false;
        }

        List<ActionExecutionParticipant> participants = new List<ActionExecutionParticipant>();
        foreach (string performerRuntimeId in expedition.PerformerRuntimeIds)
        {
            participants.Add(new ActionExecutionParticipant(performerRuntimeId, ActionExecutionParticipantRole.Performer));
        }

        foreach (string supportRuntimeId in expedition.SupportRuntimeIds)
        {
            participants.Add(new ActionExecutionParticipant(supportRuntimeId, ActionExecutionParticipantRole.Support));
        }

        ActionExecutionContext returnContext = new ActionExecutionContext(
            "return-" + expedition.ExpeditionId,
            participants,
            expedition.OriginLocationRuntimeId,
            returnRoute.RuntimeId,
            expedition.OriginDecisionId);

        if (travelPartySystem.CanPlanKnownGroupTravel(returnContext, out reason) == false)
        {
            return false;
        }

        if (expedition.TryBeginReturn() == false)
        {
            reason = "Expedition could not enter the Returning state.";
            return false;
        }

        if (travelPartySystem.TryStartTravelParty(returnContext, out TravelPartyRuntime party) == false)
        {
            expedition.TryCancelReturn();
            reason = "The real macro return TravelParty could not be started.";
            return false;
        }

        if (expedition.TryBeginReturnTravel(party.TravelPartyId) == false)
        {
            expedition.TryCancelReturn();
            travelPartyStore.Remove(party.TravelPartyId);
            reason = "Expedition could not associate its real macro return TravelParty.";
            return false;
        }

        RecordLifecycleEvent(
            () => domainEventRecorder.Record(
                (eventId, absoluteDay, recordSequence) => new ExpeditionReturnStartedEvent(
                    eventId,
                    absoluteDay,
                    recordSequence,
                    expedition,
                    expedition.TargetLocationRuntimeId)),
            "return-started",
            expedition);

        return true;
    }

    private bool TryValidateActiveExpedition(ExpeditionRuntime expedition, out string reason)
    {
        reason = null;
        if (expedition == null
            || expeditionStore.GetById(expedition.ExpeditionId) != expedition
            || expedition.IsActive == false)
        {
            reason = "Expedition is not the active World Truth runtime.";
            return false;
        }

        return true;
    }

    private bool TryGetPublishedTargetTopology(
        ExpeditionRuntime expedition,
        out LocalTopologyRuntime topology)
    {
        topology = null;
        return expedition != null
            && localTopologyStore != null
            && localTopologyStore.TryGetTopologyForOwner(expedition.TargetSiteRuntimeId, out topology) == true
            && topology != null
            && topology.IsPublished == true
            && string.Equals(
                topology.Owner.OwnerRuntimeId,
                expedition.TargetSiteRuntimeId,
                StringComparison.Ordinal) == true;
    }

    private bool TryResolveMembers(
        ExpeditionRuntime expedition,
        out List<NpcRuntime> members,
        out string reason)
    {
        members = new List<NpcRuntime>();
        reason = null;
        if (expedition == null)
        {
            reason = "Expedition is null.";
            return false;
        }

        foreach (string memberRuntimeId in expedition.MemberRuntimeIds)
        {
            if (identityRegistry.TryGetNpcWithoutLogging(memberRuntimeId, out NpcRuntime member) == false
                || member == null)
            {
                members.Clear();
                reason = "Every expedition member must resolve before local exploration mutates state.";
                return false;
            }

            members.Add(member);
        }

        return true;
    }

    private bool TryValidateContentContext(
        ExpeditionRuntime expedition,
        PlaceContentOwnerReference owner,
        bool requireAccessible,
        out PlaceContentRuntime content,
        out string reason)
    {
        content = null;
        reason = null;
        if (expedition == null || owner == null || placeContentStore == null)
        {
            reason = "Content interaction requires an expedition, owner, and content store.";
            return false;
        }

        if (owner.OwnerKind == PlaceContentOwnerKind.ExplorableSite)
        {
            if (string.Equals(owner.OwnerRuntimeId, expedition.TargetSiteRuntimeId, StringComparison.Ordinal) == false
                || string.Equals(owner.MacroLocationRuntimeId, expedition.TargetLocationRuntimeId, StringComparison.Ordinal) == false)
            {
                reason = "Site-level content must belong to this expedition's target site.";
                return false;
            }
        }
        else if (owner.OwnerKind == PlaceContentOwnerKind.LocalPlace)
        {
            if (TryGetPublishedTargetTopology(expedition, out LocalTopologyRuntime topology) == false
                || topology.TryGetPlace(owner.OwnerRuntimeId, out LocalPlaceRuntime localPlace) == false
                || localPlace == null
                || string.Equals(owner.TopologyOwnerRuntimeId, expedition.TargetSiteRuntimeId, StringComparison.Ordinal) == false
                || string.Equals(owner.MacroLocationRuntimeId, expedition.TargetLocationRuntimeId, StringComparison.Ordinal) == false
                || string.Equals(expedition.CurrentLocalPlaceRuntimeId, localPlace.RuntimeId, StringComparison.Ordinal) == false)
            {
                reason = "Local content requires the expedition to be at that LocalPlace in the published target-site topology.";
                return false;
            }
        }
        else
        {
            reason = "An expedition cannot remotely interact with City content while exploring a site.";
            return false;
        }

        if (placeContentStore.TryGet(owner, out content) == false)
        {
            reason = "The requested content owner has no persistent content state.";
            return false;
        }

        if (requireAccessible == true && content.AccessState != PlaceAccessState.Accessible)
        {
            content = null;
            reason = "The requested place content is not accessible.";
            return false;
        }

        return true;
    }

    private void RecordAdvanced(ExpeditionRuntime expedition, string relevantTargetId)
    {
        RecordLifecycleEvent(
            () => domainEventRecorder.Record(
                (eventId, absoluteDay, recordSequence) => new ExpeditionAdvancedEvent(
                    eventId,
                    absoluteDay,
                    recordSequence,
                    expedition,
                    expedition.TargetLocationRuntimeId,
                    relevantTargetId)),
            "advanced",
            expedition);
    }

    private void RecordObjectiveCompletedIfNeeded(
        ExpeditionRuntime expedition,
        bool objectiveWasCompleted,
        string relevantTargetId)
    {
        if (expedition == null || objectiveWasCompleted == true || expedition.IsObjectiveComplete == false)
        {
            return;
        }

        RecordLifecycleEvent(
            () => domainEventRecorder.Record(
                (eventId, absoluteDay, recordSequence) => new ExpeditionObjectiveCompletedEvent(
                    eventId,
                    absoluteDay,
                    recordSequence,
                    expedition,
                    expedition.TargetLocationRuntimeId,
                    relevantTargetId)),
            "objective-completed",
            expedition);
    }

    private void RecordLifecycleEvent(
        Func<bool> record,
        string eventName,
        ExpeditionRuntime expedition)
    {
        if (domainEventRecorder == null)
        {
            return;
        }

        if (record == null || record() == false)
        {
            logger.LogWarning(
                $"Expedition {eventName} event could not be recorded for ExpeditionId '{expedition?.ExpeditionId}'.");
        }
    }

    private static string GetObjectiveTargetId(ExpeditionObjectiveRuntime objective)
    {
        if (objective == null)
        {
            return null;
        }

        return objective.TargetNotableItemRuntimeId
            ?? objective.TargetItemDefinitionId
            ?? objective.TargetOppositionRuntimeId;
    }

    public bool TryGetExpeditionForNpc(string npcRuntimeId, out ExpeditionRuntime expedition)
    {
        return expeditionStore.TryGetExpeditionForNpc(npcRuntimeId, out expedition);
    }

    public IReadOnlyList<ExpeditionRuntime> ReconcileAfterTravel()
    {
        return ReconcileAfterTravel(null);
    }

    public IReadOnlyList<ExpeditionRuntime> ReconcileAfterTravel(IReadOnlyList<NpcRuntime> arrivedNpcs)
    {
        List<ExpeditionRuntime> arrivedExpeditions = new List<ExpeditionRuntime>();
        List<ExpeditionRuntime> activeExpeditions = new List<ExpeditionRuntime>(expeditionStore.ActiveExpeditions);

        foreach (ExpeditionRuntime expedition in activeExpeditions)
        {
            if (expedition == null
                || HasArrivedMember(expedition, arrivedNpcs) == false
                || (expedition.State != ExpeditionState.TravelingToSite
                    && expedition.State != ExpeditionState.Returning))
            {
                continue;
            }

            if (expedition.State == ExpeditionState.TravelingToSite)
            {
                if (CanReconcileArrival(expedition) == false || expedition.TryArriveAtSite() == false)
                {
                    continue;
                }

                bool eventRecorded = domainEventRecorder == null || domainEventRecorder.Record(
                    (eventId, absoluteDay, recordSequence) => new ExpeditionArrivedAtSiteEvent(
                        eventId,
                        absoluteDay,
                        recordSequence,
                        expedition.ExpeditionId,
                        expedition.TargetSiteRuntimeId,
                        expedition.OriginLocationRuntimeId,
                        expedition.TargetLocationRuntimeId,
                        expedition.TravelPartyId,
                        expedition.PerformerRuntimeIds,
                        expedition.SupportRuntimeIds,
                        expedition.OriginDecisionId));

                if (eventRecorded == false)
                {
                    logger.LogWarning("Expedition arrival event could not be recorded for ExpeditionId '" + expedition.ExpeditionId + "'.");
                }

                arrivedExpeditions.Add(expedition);
                continue;
            }

            if (CanReconcileReturn(expedition) == false
                || expedition.TryComplete() == false
                || expeditionStore.Complete(expedition.ExpeditionId) == false)
            {
                continue;
            }

            RecordLifecycleEvent(
                () => domainEventRecorder.Record(
                    (eventId, absoluteDay, recordSequence) => new ExpeditionCompletedEvent(
                        eventId,
                        absoluteDay,
                        recordSequence,
                        expedition,
                        expedition.OriginLocationRuntimeId)),
                "completed",
                expedition);

            arrivedExpeditions.Add(expedition);
        }

        return arrivedExpeditions.AsReadOnly();
    }

    private bool TryPrepareStart(
        ExplorableSiteRuntime targetSite,
        ActionExecutionContext context,
        out ExpeditionPreparation preparation,
        out string reason)
    {
        preparation = null;
        reason = string.Empty;

        if (targetSite == null)
        {
            reason = "Expedition target site is null.";
            return false;
        }

        if (explorableSiteStore.TryGetByRuntimeId(targetSite.RuntimeId, out ExplorableSiteRuntime storedSite) == false
            || storedSite != targetSite)
        {
            reason = "Expedition target site is not the registered World Truth site.";
            return false;
        }

        if (targetSite.Location == null
            || context == null
            || string.Equals(context.TargetLocationRuntimeId, targetSite.Location.RuntimeId, StringComparison.Ordinal) == false)
        {
            reason = "Expedition target LocationRuntimeId must match the target site location.";
            return false;
        }

        if (identityRegistry.TryGetRoute(context.TargetRouteRuntimeId, out SpatialRouteRuntime route) == false
            || route == null)
        {
            reason = "Expedition route could not be resolved from World Truth.";
            return false;
        }

        if (ActionExecutionValidator.TryValidate(context, ExpeditionRequirements, out reason) == false)
        {
            return false;
        }

        List<string> performers = new List<string>();
        List<string> supports = new List<string>();
        List<string> members = new List<string>();
        HashSet<string> memberIds = new HashSet<string>(StringComparer.Ordinal);
        SpatialLocationRuntime originLocation = null;

        foreach (ActionExecutionParticipant participant in context.Participants)
        {
            if (participant.Role != ActionExecutionParticipantRole.Performer
                && participant.Role != ActionExecutionParticipantRole.Support)
            {
                reason = "Expedition members may only use Performer and Support roles.";
                return false;
            }

            if (memberIds.Add(participant.RuntimeId) == false)
            {
                reason = "An expedition member cannot appear in more than one role.";
                return false;
            }

            if (identityRegistry.TryGetNpc(participant.RuntimeId, out NpcRuntime member) == false || member == null)
            {
                reason = "An expedition participant could not be resolved.";
                return false;
            }

            if (member.IsAlive == false
                || member.IsTraveling == true
                || member.CurrentLocation == null
                || string.IsNullOrWhiteSpace(member.ActiveTravelPartyId) == false
                || travelPartyStore.TryGetPartyForNpc(member.RuntimeId, out _)
                || expeditionStore.TryGetExpeditionForNpc(member.RuntimeId, out _))
            {
                reason = "An expedition participant is already traveling or belongs to another activity.";
                return false;
            }

            if (originLocation == null)
            {
                originLocation = member.CurrentLocation;
            }
            else if (member.CurrentLocation != originLocation)
            {
                reason = "Expedition participants must share one CurrentLocation.";
                return false;
            }

            members.Add(member.RuntimeId);

            if (participant.Role == ActionExecutionParticipantRole.Performer)
            {
                performers.Add(member.RuntimeId);
            }
            else
            {
                supports.Add(member.RuntimeId);
            }
        }

        if (originLocation == null
            || route.Origin != originLocation
            || route.Destination != targetSite.Location)
        {
            reason = "Expedition route must connect the common origin to the target site location.";
            return false;
        }

        if (travelPartySystem.CanPlanKnownGroupTravel(context, out reason) == false)
        {
            return false;
        }

        foreach (string performerRuntimeId in performers)
        {
            if (identityRegistry.TryGetNpc(performerRuntimeId, out NpcRuntime performer) == false
                || performer.ExplorableSiteKnowledge.KnowsSite(targetSite.RuntimeId) == false)
            {
                reason = "Every Performer must know the target site before departure.";
                return false;
            }
        }

        preparation = new ExpeditionPreparation(
            originLocation,
            route,
            members,
            performers,
            supports);
        return true;
    }

    private bool CanReconcileArrival(ExpeditionRuntime expedition)
    {
        if (string.IsNullOrWhiteSpace(expedition.TravelPartyId) == true
            || travelPartyStore.GetById(expedition.TravelPartyId) != null)
        {
            return false;
        }

        foreach (string memberRuntimeId in expedition.MemberRuntimeIds)
        {
            if (identityRegistry.TryGetNpc(memberRuntimeId, out NpcRuntime member) == false
                || member == null
                || member.IsTraveling == true
                || string.IsNullOrWhiteSpace(member.ActiveTravelPartyId) == false
                || member.CurrentLocation == null
                || string.Equals(member.CurrentLocation.RuntimeId, expedition.TargetLocationRuntimeId, StringComparison.Ordinal) == false)
            {
                return false;
            }
        }

        return true;
    }

    private bool CanReconcileReturn(ExpeditionRuntime expedition)
    {
        if (string.IsNullOrWhiteSpace(expedition.TravelPartyId) == true
            || travelPartyStore.GetById(expedition.TravelPartyId) != null)
        {
            return false;
        }

        foreach (string memberRuntimeId in expedition.MemberRuntimeIds)
        {
            if (identityRegistry.TryGetNpc(memberRuntimeId, out NpcRuntime member) == false
                || member == null
                || member.IsTraveling == true
                || string.IsNullOrWhiteSpace(member.ActiveTravelPartyId) == false
                || member.CurrentLocation == null
                || string.Equals(member.CurrentLocation.RuntimeId, expedition.OriginLocationRuntimeId, StringComparison.Ordinal) == false)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasArrivedMember(
        ExpeditionRuntime expedition,
        IReadOnlyList<NpcRuntime> arrivedNpcs)
    {
        if (arrivedNpcs == null)
        {
            return true;
        }

        foreach (NpcRuntime arrivedNpc in arrivedNpcs)
        {
            if (arrivedNpc != null && ContainsId(expedition.MemberRuntimeIds, arrivedNpc.RuntimeId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsId(IReadOnlyList<string> ids, string expected)
    {
        if (ids == null)
        {
            return false;
        }

        foreach (string id in ids)
        {
            if (string.Equals(id, expected, StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }

    private sealed class ExpeditionPreparation
    {
        public SpatialLocationRuntime OriginLocation { get; }
        public SpatialRouteRuntime Route { get; }
        public List<string> MemberRuntimeIds { get; }
        public List<string> PerformerRuntimeIds { get; }
        public List<string> SupportRuntimeIds { get; }

        public ExpeditionPreparation(
            SpatialLocationRuntime originLocation,
            SpatialRouteRuntime route,
            List<string> memberRuntimeIds,
            List<string> performerRuntimeIds,
            List<string> supportRuntimeIds)
        {
            OriginLocation = originLocation;
            Route = route;
            MemberRuntimeIds = memberRuntimeIds;
            PerformerRuntimeIds = performerRuntimeIds;
            SupportRuntimeIds = supportRuntimeIds;
        }
    }
}
