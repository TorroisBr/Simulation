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
    private readonly SimulationTime simulationTime;
    private readonly DomainEventRecorder domainEventRecorder;
    private readonly SimulationLogger logger;

    public ExpeditionStore Store => expeditionStore;

    public ExpeditionSystem(
        ExpeditionStore expeditionStore,
        RuntimeIdAllocator idAllocator,
        RuntimeIdentityRegistry identityRegistry,
        ExplorableSiteStore explorableSiteStore,
        TravelPartySystem travelPartySystem,
        TravelPartyStore travelPartyStore,
        ExplorableSiteKnowledgeSystem explorableSiteKnowledgeSystem,
        SimulationTime simulationTime,
        DomainEventRecorder domainEventRecorder = null,
        SimulationLogger logger = null)
    {
        this.expeditionStore = expeditionStore ?? throw new ArgumentNullException(nameof(expeditionStore));
        this.idAllocator = idAllocator ?? throw new ArgumentNullException(nameof(idAllocator));
        this.identityRegistry = identityRegistry ?? throw new ArgumentNullException(nameof(identityRegistry));
        this.explorableSiteStore = explorableSiteStore ?? throw new ArgumentNullException(nameof(explorableSiteStore));
        this.travelPartySystem = travelPartySystem ?? throw new ArgumentNullException(nameof(travelPartySystem));
        this.travelPartyStore = travelPartyStore ?? throw new ArgumentNullException(nameof(travelPartyStore));
        this.explorableSiteKnowledgeSystem = explorableSiteKnowledgeSystem
            ?? throw new ArgumentNullException(nameof(explorableSiteKnowledgeSystem));
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        this.domainEventRecorder = domainEventRecorder;
        this.logger = logger ?? new SimulationLogger(null);
    }

    public bool TryStartExpedition(
        ExplorableSiteRuntime targetSite,
        ActionExecutionContext context,
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
                preparation.SupportRuntimeIds);
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
                || expedition.State != ExpeditionState.TravelingToSite
                || HasArrivedMember(expedition, arrivedNpcs) == false
                || CanReconcileArrival(expedition) == false)
            {
                continue;
            }

            if (expedition.TryArriveAtSite() == false)
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
