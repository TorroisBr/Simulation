using System;
using System.Collections.Generic;

public interface IAdventureItemDefinitionResolver
{
    bool TryResolveItem(string itemDefinitionId, out ItemData item);
}

[Serializable]
public sealed class AdventureExpeditionAutonomySettings
{
    public NpcInjurySeverity ReturnAtOrAboveInjury = NpcInjurySeverity.SeriouslyInjured;
    public bool IncludeLivingSupportsInConflict = true;
    public int CommonResourceRetrievalAmount = 1;
}

public sealed class AdventureExpeditionAutonomySystem
{
    private readonly AdventureAutonomySystem candidateSystem;
    private readonly AdventureSiteIntelKnowledgeSystem intelSystem;
    private readonly ExpeditionSystem expeditionSystem;
    private readonly ExpeditionStore expeditionStore;
    private readonly ExplorableSiteStore siteStore;
    private readonly LocalTopologyStore topologyStore;
    private readonly PlaceContentStore contentStore;
    private readonly RuntimeIdentityRegistry identityRegistry;
    private readonly SpatialNetworkRuntime spatialNetwork;
    private readonly NpcDecisionRecorder decisionRecorder;
    private readonly ConflictResolutionService conflictResolutionService;
    private readonly ConflictIdAllocator conflictIdAllocator;
    private readonly IAdventureItemDefinitionResolver itemResolver;
    private readonly SimulationTime simulationTime;
    private readonly AdventureExpeditionAutonomySettings settings;
    private readonly HashSet<string> reservedToday = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> failedExecutionKeys = new HashSet<string>(StringComparer.Ordinal);
    private long reservationDay = -1L;

    public Conflict LastConflict { get; private set; }
    public ConflictResolutionResult LastConflictResult { get; private set; }
    public ConflictResolutionConstraints LastConflictConstraints { get; private set; }

    public AdventureExpeditionAutonomySystem(
        AdventureAutonomySystem candidateSystem,
        ExpeditionSystem expeditionSystem,
        ExpeditionStore expeditionStore,
        ExplorableSiteStore siteStore,
        LocalTopologyStore topologyStore,
        PlaceContentStore contentStore,
        RuntimeIdentityRegistry identityRegistry,
        SpatialNetworkRuntime spatialNetwork,
        NpcDecisionRecorder decisionRecorder,
        SimulationTime simulationTime,
        ConflictResolutionService conflictResolutionService = null,
        ConflictIdAllocator conflictIdAllocator = null,
        IAdventureItemDefinitionResolver itemResolver = null,
        AdventureExpeditionAutonomySettings settings = null)
    {
        this.candidateSystem = candidateSystem ?? throw new ArgumentNullException(nameof(candidateSystem));
        this.expeditionSystem = expeditionSystem ?? throw new ArgumentNullException(nameof(expeditionSystem));
        this.expeditionStore = expeditionStore ?? throw new ArgumentNullException(nameof(expeditionStore));
        this.siteStore = siteStore ?? throw new ArgumentNullException(nameof(siteStore));
        this.topologyStore = topologyStore;
        this.contentStore = contentStore;
        this.identityRegistry = identityRegistry ?? throw new ArgumentNullException(nameof(identityRegistry));
        this.spatialNetwork = spatialNetwork ?? throw new ArgumentNullException(nameof(spatialNetwork));
        this.decisionRecorder = decisionRecorder ?? throw new ArgumentNullException(nameof(decisionRecorder));
        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        this.conflictResolutionService = conflictResolutionService;
        this.conflictIdAllocator = conflictIdAllocator ?? new ConflictIdAllocator();
        this.itemResolver = itemResolver;
        this.settings = settings ?? new AdventureExpeditionAutonomySettings();
        intelSystem = new AdventureSiteIntelKnowledgeSystem();
    }

    public void BeginDay()
    {
        if (reservationDay == simulationTime.AbsoluteDay)
        {
            return;
        }

        reservationDay = simulationTime.AbsoluteDay;
        reservedToday.Clear();
    }

    public bool IsReservedToday(string npcRuntimeId)
    {
        BeginDay();
        return string.IsNullOrWhiteSpace(npcRuntimeId) == false && reservedToday.Contains(npcRuntimeId);
    }

    public bool TryStartAutonomousExpedition(NpcRuntime decisionMaker, IReadOnlyList<NpcRuntime> availableNpcs)
    {
        BeginDay();
        if (decisionMaker == null
            || reservedToday.Contains(decisionMaker.RuntimeId)
            || expeditionStore.IsNpcOnActiveExpedition(decisionMaker.RuntimeId))
        {
            return false;
        }

        IReadOnlyList<AdventureCandidate> candidates = candidateSystem.BuildCandidates(
            decisionMaker,
            spatialNetwork,
            availableNpcs);
        AdventureCandidate candidate = candidateSystem.ChooseCandidate(candidates);
        if (candidate == null || failedExecutionKeys.Contains(GetStartKey(decisionMaker, candidate)))
        {
            return false;
        }

        NpcDecisionRecord decision = candidateSystem.RecordStartDecision(decisionMaker, candidate, decisionRecorder);
        if (decision == null)
        {
            return false;
        }

        if (siteStore.TryGetByRuntimeId(candidate.SiteRuntimeId, out ExplorableSiteRuntime site) == false
            || site == null
            || site.Location == null
            || string.Equals(site.Location.RuntimeId, candidate.SiteLocationRuntimeId, StringComparison.Ordinal) == false
            || TryGetKnownDirectRoute(decisionMaker, candidate.SiteLocationRuntimeId, out SpatialRouteRuntime route) == false)
        {
            failedExecutionKeys.Add(GetStartKey(decisionMaker, candidate));
            return false;
        }

        List<ActionExecutionParticipant> participants = new List<ActionExecutionParticipant>();
        foreach (string performerRuntimeId in candidate.PerformerRuntimeIds)
        {
            participants.Add(new ActionExecutionParticipant(performerRuntimeId, ActionExecutionParticipantRole.Performer));
        }

        foreach (string supportRuntimeId in candidate.SupportRuntimeIds)
        {
            participants.Add(new ActionExecutionParticipant(supportRuntimeId, ActionExecutionParticipantRole.Support));
        }

        ActionExecutionContext context = new ActionExecutionContext(
            "autonomous-expedition:" + candidate.Kind,
            participants,
            site.Location.RuntimeId,
            route.RuntimeId,
            decision.DecisionId);
        if (expeditionSystem.TryStartExpedition(site, context, candidate.CreateObjective(), out ExpeditionRuntime expedition) == false)
        {
            failedExecutionKeys.Add(GetStartKey(decisionMaker, candidate));
            return false;
        }

        Reserve(expedition);
        return true;
    }

    public void AdvanceActiveExpeditions()
    {
        BeginDay();
        List<ExpeditionRuntime> active = new List<ExpeditionRuntime>(expeditionStore.ActiveExpeditions);
        foreach (ExpeditionRuntime expedition in active)
        {
            if (expedition == null || expedition.IsActive == false)
            {
                continue;
            }

            Reserve(expedition);
            if (expedition.State == ExpeditionState.AtSite)
            {
                TryBeginExploration(expedition);
            }
            else if (expedition.State == ExpeditionState.Exploring)
            {
                TryAdvanceExploration(expedition);
            }
        }
    }

    private void TryBeginExploration(ExpeditionRuntime expedition)
    {
        NpcDecisionRecord decision = RecordExpeditionDecision(
            expedition,
            NpcDecisionType.ExpeditionExplore,
            "adventure:begin-exploration",
            expedition.TargetSiteRuntimeId);
        if (decision == null)
        {
            return;
        }

        if (expeditionSystem.TryBeginExploration(expedition, out _) == true)
        {
            ObserveSiteLevel(expedition);
        }
        else
        {
            failedExecutionKeys.Add(GetProgressKey(expedition, "begin", expedition.TargetSiteRuntimeId));
        }
    }

    private void TryAdvanceExploration(ExpeditionRuntime expedition)
    {
        if (expedition.IsObjectiveComplete == true || ShouldReturnForInjury(expedition))
        {
            TryReturn(expedition);
            return;
        }

        if (TryPerformKnownObjectiveAtCurrentContext(expedition) == true)
        {
            return;
        }

        if (topologyStore == null
            || topologyStore.TryGetTopologyForOwner(expedition.TargetSiteRuntimeId, out LocalTopologyRuntime topology) == false
            || topology == null)
        {
            TryAdvanceAbstract(expedition);
            return;
        }

        string desiredLocalPlaceRuntimeId = GetKnownObjectiveLocalPlace(expedition);
        if (TryAdvanceDetailed(expedition, topology, desiredLocalPlaceRuntimeId) == false)
        {
            TryReturn(expedition);
        }
    }

    private void TryAdvanceAbstract(ExpeditionRuntime expedition)
    {
        string key = GetProgressKey(expedition, "abstract", expedition.ExplorationProgress.ToString());
        if (failedExecutionKeys.Contains(key))
        {
            TryReturn(expedition);
            return;
        }

        NpcDecisionRecord decision = RecordExpeditionDecision(
            expedition,
            NpcDecisionType.ExpeditionExplore,
            "adventure:continue-exploration",
            expedition.TargetSiteRuntimeId);
        if (decision == null || expeditionSystem.TryContinueExploration(expedition, out _) == false)
        {
            failedExecutionKeys.Add(key);
            TryReturn(expedition);
        }
    }

    private bool TryAdvanceDetailed(
        ExpeditionRuntime expedition,
        LocalTopologyRuntime topology,
        string desiredLocalPlaceRuntimeId)
    {
        NpcRuntime decisionMaker = GetDecisionMaker(expedition);
        if (decisionMaker == null)
        {
            return false;
        }

        LocalTopologyKnowledgeRuntime knowledge = decisionMaker.LocalTopologyKnowledge;
        if (expedition.CurrentLocalPlaceRuntimeId == null)
        {
            foreach (LocalPlaceKnowledgeObservation place in knowledge.PlaceObservations)
            {
                if (place != null
                    && place.IsEntryPoint
                    && string.Equals(place.TopologyOwnerRuntimeId, expedition.TargetSiteRuntimeId, StringComparison.Ordinal)
                    && Contains(expedition.VisitedLocalPlaceRuntimeIds, place.LocalPlaceRuntimeId) == false
                    && topology.TryGetPlace(place.LocalPlaceRuntimeId, out LocalPlaceRuntime truthPlace))
                {
                    return TryExploreKnownPlace(expedition, topology, truthPlace);
                }
            }

            return false;
        }

        if (string.IsNullOrWhiteSpace(desiredLocalPlaceRuntimeId) == false
            && string.Equals(expedition.CurrentLocalPlaceRuntimeId, desiredLocalPlaceRuntimeId, StringComparison.Ordinal) == false
            && knowledge.TryFindKnownPath(
                expedition.TargetSiteRuntimeId,
                expedition.CurrentLocalPlaceRuntimeId,
                desiredLocalPlaceRuntimeId,
                out LocalTopologyPath path)
            && path.LocalConnectionRuntimeIds.Count > 0
            && topology.TryGetConnection(path.LocalConnectionRuntimeIds[0], out LocalTopologyConnectionRuntime targetConnection))
        {
            return TryTraverseKnownConnection(expedition, topology, targetConnection);
        }

        foreach (LocalConnectionKnowledgeObservation connection in knowledge.ConnectionObservations)
        {
            if (connection != null
                && string.Equals(connection.TopologyOwnerRuntimeId, expedition.TargetSiteRuntimeId, StringComparison.Ordinal)
                && string.Equals(connection.OriginLocalPlaceRuntimeId, expedition.CurrentLocalPlaceRuntimeId, StringComparison.Ordinal)
                && Contains(expedition.VisitedLocalPlaceRuntimeIds, connection.DestinationLocalPlaceRuntimeId) == false
                && topology.TryGetConnection(connection.LocalConnectionRuntimeId, out LocalTopologyConnectionRuntime truthConnection))
            {
                return TryTraverseKnownConnection(expedition, topology, truthConnection);
            }
        }

        return false;
    }

    private bool TryExploreKnownPlace(
        ExpeditionRuntime expedition,
        LocalTopologyRuntime topology,
        LocalPlaceRuntime place)
    {
        string key = GetProgressKey(expedition, "explore", place.RuntimeId);
        if (failedExecutionKeys.Contains(key))
        {
            return false;
        }

        NpcDecisionRecord decision = RecordExpeditionDecision(
            expedition,
            NpcDecisionType.ExpeditionExplore,
            "adventure:explore-local-place",
            place.RuntimeId);
        if (decision == null || expeditionSystem.TryExploreLocalPlace(expedition, place, out _) == false)
        {
            failedExecutionKeys.Add(key);
            return false;
        }

        ObserveLocalPlace(expedition, topology, place);
        return true;
    }

    private bool TryTraverseKnownConnection(
        ExpeditionRuntime expedition,
        LocalTopologyRuntime topology,
        LocalTopologyConnectionRuntime connection)
    {
        string key = GetProgressKey(expedition, "traverse", connection.RuntimeId);
        if (failedExecutionKeys.Contains(key))
        {
            return false;
        }

        NpcDecisionRecord decision = RecordExpeditionDecision(
            expedition,
            NpcDecisionType.ExpeditionTraverse,
            "adventure:traverse-local-connection",
            connection.RuntimeId);
        if (decision == null || expeditionSystem.TryTraverseLocalConnection(expedition, connection, out _) == false)
        {
            failedExecutionKeys.Add(key);
            return false;
        }

        ObserveLocalPlace(expedition, topology, connection.Destination);
        return true;
    }

    private bool TryPerformKnownObjectiveAtCurrentContext(ExpeditionRuntime expedition)
    {
        if (HasKnownObjectiveIntel(expedition) == false)
        {
            return false;
        }

        if (expedition.Objective.ObjectiveType == ExpeditionObjectiveType.Retrieve)
        {
            return TryRetrieve(expedition);
        }

        if (expedition.Objective.ObjectiveType == ExpeditionObjectiveType.Eliminate)
        {
            return TryResolveOpposition(expedition);
        }

        return false;
    }

    private bool HasKnownObjectiveIntel(ExpeditionRuntime expedition)
    {
        NpcRuntime decisionMaker = GetDecisionMaker(expedition);
        if (decisionMaker == null)
        {
            return false;
        }

        if (expedition.Objective.ObjectiveType == ExpeditionObjectiveType.Eliminate)
        {
            return decisionMaker.AdventureSiteIntelKnowledge.KnowsOpposition(
                expedition.TargetSiteRuntimeId,
                expedition.Objective.TargetOppositionRuntimeId);
        }

        if (expedition.Objective.ObjectiveType != ExpeditionObjectiveType.Retrieve)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(expedition.Objective.TargetNotableItemRuntimeId) == false)
        {
            return decisionMaker.AdventureSiteIntelKnowledge.KnowsNotableItem(
                expedition.TargetSiteRuntimeId,
                expedition.Objective.TargetNotableItemRuntimeId);
        }

        return decisionMaker.AdventureSiteIntelKnowledge.KnowsCommonResource(
            expedition.TargetSiteRuntimeId,
            expedition.Objective.TargetItemDefinitionId);
    }

    private bool TryRetrieve(ExpeditionRuntime expedition)
    {
        string expectedLocalPlaceRuntimeId = GetKnownObjectiveLocalPlace(expedition);
        if (string.IsNullOrWhiteSpace(expectedLocalPlaceRuntimeId) == false
            && string.Equals(expectedLocalPlaceRuntimeId, expedition.CurrentLocalPlaceRuntimeId, StringComparison.Ordinal) == false)
        {
            return false;
        }

        string targetId = expedition.Objective.TargetNotableItemRuntimeId
            ?? expedition.Objective.TargetItemDefinitionId;
        string key = GetProgressKey(expedition, "retrieve", targetId);
        if (failedExecutionKeys.Contains(key))
        {
            return true;
        }

        NpcDecisionRecord decision = RecordExpeditionDecision(
            expedition,
            NpcDecisionType.ExpeditionRetrieve,
            "adventure:retrieve",
            targetId);
        bool success = false;
        if (decision != null && string.IsNullOrWhiteSpace(expedition.Objective.TargetNotableItemRuntimeId) == false)
        {
            success = expeditionSystem.TryRetrieveNotableItem(
                expedition,
                expedition.Objective.TargetNotableItemRuntimeId,
                out _,
                out _);
        }
        else if (decision != null
            && itemResolver != null
            && itemResolver.TryResolveItem(expedition.Objective.TargetItemDefinitionId, out ItemData item))
        {
            PlaceContentOwnerReference owner = ResolveKnownObjectiveOwner(expedition);
            success = expeditionSystem.TryRetrieveTargetResource(
                expedition,
                owner,
                item,
                Math.Max(1, settings.CommonResourceRetrievalAmount),
                out _);
        }

        if (success == false)
        {
            failedExecutionKeys.Add(key);
            TryReturn(expedition);
        }

        return true;
    }

    private bool TryResolveOpposition(ExpeditionRuntime expedition)
    {
        string expectedLocalPlaceRuntimeId = GetKnownObjectiveLocalPlace(expedition);
        if (string.IsNullOrWhiteSpace(expectedLocalPlaceRuntimeId) == false
            && string.Equals(expectedLocalPlaceRuntimeId, expedition.CurrentLocalPlaceRuntimeId, StringComparison.Ordinal) == false)
        {
            return false;
        }

        string targetId = expedition.Objective.TargetOppositionRuntimeId;
        string key = GetProgressKey(expedition, "opposition", targetId);
        if (failedExecutionKeys.Contains(key))
        {
            return true;
        }

        PlaceContentOwnerReference owner = ResolveKnownObjectiveOwner(expedition);
        if (owner == null
            || contentStore == null
            || contentStore.TryGet(owner, out PlaceContentRuntime content) == false)
        {
            failedExecutionKeys.Add(key);
            TryReturn(expedition);
            return true;
        }

        PlaceOppositionRuntime opposition = content.GetOpposition(targetId);
        if (opposition == null || opposition.IsActive == false || conflictResolutionService == null)
        {
            failedExecutionKeys.Add(key);
            TryReturn(expedition);
            return true;
        }

        NpcDecisionRecord decision = RecordExpeditionDecision(
            expedition,
            NpcDecisionType.ExpeditionResolveOpposition,
            "adventure:resolve-opposition",
            targetId);
        if (decision == null)
        {
            return true;
        }

        List<NpcRuntime> participants = ResolveLivingConflictParticipants(expedition);
        if (participants.Count == 0)
        {
            failedExecutionKeys.Add(key);
            TryReturn(expedition);
            return true;
        }

        Conflict conflict = opposition.CreateConflict(
            conflictIdAllocator.AllocateConflictId(),
            participants,
            "expedition",
            ConflictObjectiveType.Defeat,
            ConflictStakes.Meaningful,
            owner.OwnerRuntimeId,
            decision.DecisionId);
        LastConflict = conflict;
        LastConflictConstraints = null;
        if (expeditionSystem.TryResolvePlaceOpposition(
            expedition,
            owner,
            opposition,
            conflict,
            conflictResolutionService,
            out ConflictResolutionResult result,
            out _) == false)
        {
            failedExecutionKeys.Add(key);
            TryReturn(expedition);
            return true;
        }

        LastConflictResult = result;
        if (expedition.IsObjectiveComplete == false)
        {
            TryReturn(expedition);
        }

        return true;
    }

    private void TryReturn(ExpeditionRuntime expedition)
    {
        if (expedition == null || expedition.CanBeginReturn() == false)
        {
            return;
        }

        string key = GetProgressKey(expedition, "return", expedition.OriginLocationRuntimeId);
        if (failedExecutionKeys.Contains(key))
        {
            return;
        }

        NpcDecisionRecord decision = RecordExpeditionDecision(
            expedition,
            NpcDecisionType.ExpeditionReturn,
            "adventure:return",
            expedition.OriginLocationRuntimeId);
        if (decision == null || expeditionSystem.TryBeginReturn(expedition, out _) == false)
        {
            failedExecutionKeys.Add(key);
        }
    }

    private NpcDecisionRecord RecordExpeditionDecision(
        ExpeditionRuntime expedition,
        NpcDecisionType decisionType,
        string actionDefinitionId,
        string targetRuntimeId)
    {
        NpcRuntime decisionMaker = GetDecisionMaker(expedition);
        if (decisionMaker == null)
        {
            return null;
        }

        List<NpcDecisionParticipant> participants = new List<NpcDecisionParticipant>();
        foreach (string performerRuntimeId in expedition.PerformerRuntimeIds)
        {
            participants.Add(new NpcDecisionParticipant(performerRuntimeId, NpcDecisionParticipantRole.Performer));
        }

        foreach (string supportRuntimeId in expedition.SupportRuntimeIds)
        {
            participants.Add(new NpcDecisionParticipant(supportRuntimeId, NpcDecisionParticipantRole.Support));
        }

        return decisionRecorder.RecordWithParticipants(
            decisionMaker.RuntimeId,
            decisionType,
            NpcDecisionOrigin.Autonomous,
            actionDefinitionId,
            participants,
            string.IsNullOrWhiteSpace(targetRuntimeId) ? null : new[] { targetRuntimeId },
            expedition.TargetLocationRuntimeId,
            null);
    }

    private void ObserveSiteLevel(ExpeditionRuntime expedition)
    {
        if (siteStore.TryGetByRuntimeId(expedition.TargetSiteRuntimeId, out ExplorableSiteRuntime site) == false)
        {
            return;
        }

        foreach (NpcRuntime member in ResolveMembers(expedition))
        {
            intelSystem.RecordDirectObservation(member, site, null, null, contentStore, simulationTime.AbsoluteDay);
        }
    }

    private void ObserveLocalPlace(
        ExpeditionRuntime expedition,
        LocalTopologyRuntime topology,
        LocalPlaceRuntime place)
    {
        if (siteStore.TryGetByRuntimeId(expedition.TargetSiteRuntimeId, out ExplorableSiteRuntime site) == false)
        {
            return;
        }

        foreach (NpcRuntime member in ResolveMembers(expedition))
        {
            intelSystem.RecordDirectObservation(member, site, place, topology, contentStore, simulationTime.AbsoluteDay);
        }
    }

    private string GetKnownObjectiveLocalPlace(ExpeditionRuntime expedition)
    {
        NpcRuntime decisionMaker = GetDecisionMaker(expedition);
        if (decisionMaker == null)
        {
            return null;
        }

        if (expedition.Objective.ObjectiveType == ExpeditionObjectiveType.Eliminate)
        {
            foreach (AdventureOppositionObservation observation in decisionMaker.AdventureSiteIntelKnowledge.OppositionObservations)
            {
                if (observation != null
                    && string.Equals(observation.SiteRuntimeId, expedition.TargetSiteRuntimeId, StringComparison.Ordinal)
                    && string.Equals(observation.OppositionRuntimeId, expedition.Objective.TargetOppositionRuntimeId, StringComparison.Ordinal))
                {
                    return observation.LocalPlaceRuntimeId;
                }
            }
        }
        else if (string.IsNullOrWhiteSpace(expedition.Objective.TargetNotableItemRuntimeId) == false)
        {
            foreach (AdventureNotableItemObservation observation in decisionMaker.AdventureSiteIntelKnowledge.NotableItemObservations)
            {
                if (observation != null
                    && string.Equals(observation.SiteRuntimeId, expedition.TargetSiteRuntimeId, StringComparison.Ordinal)
                    && string.Equals(observation.NotableItemRuntimeId, expedition.Objective.TargetNotableItemRuntimeId, StringComparison.Ordinal))
                {
                    return observation.LocalPlaceRuntimeId;
                }
            }
        }
        else if (string.IsNullOrWhiteSpace(expedition.Objective.TargetItemDefinitionId) == false)
        {
            foreach (AdventureCommonResourceObservation observation in decisionMaker.AdventureSiteIntelKnowledge.CommonResourceObservations)
            {
                if (observation != null
                    && string.Equals(observation.SiteRuntimeId, expedition.TargetSiteRuntimeId, StringComparison.Ordinal)
                    && string.Equals(observation.ItemDefinitionId, expedition.Objective.TargetItemDefinitionId, StringComparison.Ordinal))
                {
                    return observation.LocalPlaceRuntimeId;
                }
            }
        }

        return null;
    }

    private PlaceContentOwnerReference ResolveKnownObjectiveOwner(ExpeditionRuntime expedition)
    {
        string localPlaceRuntimeId = GetKnownObjectiveLocalPlace(expedition);
        if (string.IsNullOrWhiteSpace(localPlaceRuntimeId) == false
            && topologyStore != null
            && topologyStore.TryGetTopologyForOwner(expedition.TargetSiteRuntimeId, out LocalTopologyRuntime topology)
            && topology.TryGetPlace(localPlaceRuntimeId, out LocalPlaceRuntime place))
        {
            return PlaceContentOwnerReference.ForLocalPlace(place);
        }

        return siteStore.TryGetByRuntimeId(expedition.TargetSiteRuntimeId, out ExplorableSiteRuntime site)
            ? PlaceContentOwnerReference.ForExplorableSite(site)
            : null;
    }

    private List<NpcRuntime> ResolveLivingConflictParticipants(ExpeditionRuntime expedition)
    {
        List<NpcRuntime> participants = new List<NpcRuntime>();
        foreach (string runtimeId in expedition.PerformerRuntimeIds)
        {
            AddLivingRegisteredNpc(runtimeId, participants);
        }

        if (settings.IncludeLivingSupportsInConflict)
        {
            foreach (string runtimeId in expedition.SupportRuntimeIds)
            {
                AddLivingRegisteredNpc(runtimeId, participants);
            }
        }

        return participants;
    }

    private void AddLivingRegisteredNpc(string runtimeId, List<NpcRuntime> participants)
    {
        if (identityRegistry.TryGetNpcWithoutLogging(runtimeId, out NpcRuntime npc)
            && npc != null
            && npc.IsAlive
            && participants.Contains(npc) == false)
        {
            participants.Add(npc);
        }
    }

    private List<NpcRuntime> ResolveMembers(ExpeditionRuntime expedition)
    {
        List<NpcRuntime> members = new List<NpcRuntime>();
        foreach (string runtimeId in expedition.MemberRuntimeIds)
        {
            if (identityRegistry.TryGetNpcWithoutLogging(runtimeId, out NpcRuntime npc) && npc != null)
            {
                members.Add(npc);
            }
        }

        return members;
    }

    private NpcRuntime GetDecisionMaker(ExpeditionRuntime expedition)
    {
        if (expedition == null || expedition.PerformerRuntimeIds.Count == 0)
        {
            return null;
        }

        return identityRegistry.TryGetNpcWithoutLogging(expedition.PerformerRuntimeIds[0], out NpcRuntime decisionMaker)
            && decisionMaker != null
            && decisionMaker.IsAlive
            ? decisionMaker
            : null;
    }

    private bool ShouldReturnForInjury(ExpeditionRuntime expedition)
    {
        foreach (string runtimeId in expedition.PerformerRuntimeIds)
        {
            if (identityRegistry.TryGetNpcWithoutLogging(runtimeId, out NpcRuntime npc)
                && npc != null
                && npc.IsAlive
                && npc.InjurySeverity >= settings.ReturnAtOrAboveInjury)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetKnownDirectRoute(
        NpcRuntime decisionMaker,
        string destinationLocationRuntimeId,
        out SpatialRouteRuntime route)
    {
        route = null;
        if (decisionMaker?.CurrentLocation == null)
        {
            return false;
        }

        foreach (SpatialRouteRuntime candidate in spatialNetwork.GetOutgoingRoutes(decisionMaker.CurrentLocation))
        {
            if (candidate != null
                && string.Equals(candidate.Destination.RuntimeId, destinationLocationRuntimeId, StringComparison.Ordinal)
                && decisionMaker.SpatialKnowledge.KnowsRoute(candidate.RuntimeId))
            {
                if (route != null)
                {
                    route = null;
                    return false;
                }

                route = candidate;
            }
        }

        return route != null;
    }

    private void Reserve(ExpeditionRuntime expedition)
    {
        foreach (string runtimeId in expedition.MemberRuntimeIds)
        {
            reservedToday.Add(runtimeId);
        }
    }

    private static bool Contains(IReadOnlyList<string> values, string expected)
    {
        if (values != null)
        {
            foreach (string value in values)
            {
                if (string.Equals(value, expected, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string GetStartKey(NpcRuntime decisionMaker, AdventureCandidate candidate)
    {
        return "start:" + decisionMaker.RuntimeId + ":" + candidate.Kind + ":" + candidate.SiteRuntimeId + ":" + candidate.TargetRuntimeId;
    }

    private static string GetProgressKey(ExpeditionRuntime expedition, string operation, string target)
    {
        return expedition.ExpeditionId + ":" + operation + ":" + target;
    }
}
