using System;
using System.Collections.Generic;

public enum AdventureCandidateKind
{
    Explore,
    Scout,
    RetrieveCommonResource,
    RetrieveNotableItem,
    EliminateOpposition,
    Secure,
    Return
}

[Serializable]
public sealed class AdventureAutonomySettings
{
    public bool enableScout = false;
    public float exploreUtility = 10f;
    public float scoutUtility = 9f;
    public float retrieveUtility = 14f;
    public float eliminateUtility = 12f;
    public float returnUtility = 20f;
}

public sealed class AdventurePartySelection
{
    public NpcRuntime NpcRuntime { get; }
    public NpcDecisionParticipantRole Role { get; }

    public AdventurePartySelection(NpcRuntime npcRuntime, NpcDecisionParticipantRole role)
    {
        NpcRuntime = npcRuntime;
        Role = role;
    }
}

public interface IAdventurePartyAssemblyPolicy
{
    IReadOnlyList<AdventurePartySelection> Assemble(
        NpcRuntime decisionMaker,
        IReadOnlyList<NpcRuntime> availableNpcs);
}

public sealed class SoloAdventurePartyAssemblyPolicy : IAdventurePartyAssemblyPolicy
{
    public IReadOnlyList<AdventurePartySelection> Assemble(
        NpcRuntime decisionMaker,
        IReadOnlyList<NpcRuntime> availableNpcs)
    {
        return decisionMaker == null
            ? Array.Empty<AdventurePartySelection>()
            : new[] { new AdventurePartySelection(decisionMaker, NpcDecisionParticipantRole.Performer) };
    }
}

public sealed class AdventureCandidate
{
    private readonly IReadOnlyList<NpcDecisionParticipant> participants;
    private readonly IReadOnlyList<string> performerRuntimeIds;
    private readonly IReadOnlyList<string> supportRuntimeIds;

    public AdventureCandidateKind Kind { get; }
    public string SiteRuntimeId { get; }
    public string SiteLocationRuntimeId { get; }
    public string LocalPlaceRuntimeId { get; }
    public string TargetRuntimeId { get; }
    public string ItemDefinitionId { get; }
    public float Utility { get; }
    public IReadOnlyList<NpcDecisionParticipant> Participants => participants;
    public IReadOnlyList<string> PerformerRuntimeIds => performerRuntimeIds;
    public IReadOnlyList<string> SupportRuntimeIds => supportRuntimeIds;

    public AdventureCandidate(
        AdventureCandidateKind kind,
        string siteRuntimeId,
        string siteLocationRuntimeId,
        string localPlaceRuntimeId,
        string targetRuntimeId,
        string itemDefinitionId,
        float utility,
        IEnumerable<NpcDecisionParticipant> participants,
        IEnumerable<string> performerRuntimeIds,
        IEnumerable<string> supportRuntimeIds)
    {
        if (string.IsNullOrWhiteSpace(siteRuntimeId) == true
            || string.IsNullOrWhiteSpace(siteLocationRuntimeId) == true
            || float.IsNaN(utility) == true
            || float.IsInfinity(utility) == true)
        {
            throw new ArgumentException("Adventure candidates require stable site identity and finite utility.");
        }

        Kind = kind;
        SiteRuntimeId = siteRuntimeId;
        SiteLocationRuntimeId = siteLocationRuntimeId;
        LocalPlaceRuntimeId = Normalize(localPlaceRuntimeId);
        TargetRuntimeId = Normalize(targetRuntimeId);
        ItemDefinitionId = Normalize(itemDefinitionId);
        Utility = utility;
        this.participants = CaptureParticipants(participants);
        this.performerRuntimeIds = CaptureIds(performerRuntimeIds);
        this.supportRuntimeIds = CaptureIds(supportRuntimeIds);
    }

    public ExpeditionObjectiveRuntime CreateObjective()
    {
        switch (Kind)
        {
            case AdventureCandidateKind.RetrieveCommonResource:
                return ExpeditionObjectiveRuntime.Retrieve(ItemDefinitionId);
            case AdventureCandidateKind.RetrieveNotableItem:
                return ExpeditionObjectiveRuntime.RetrieveNotable(TargetRuntimeId);
            case AdventureCandidateKind.EliminateOpposition:
                return ExpeditionObjectiveRuntime.Eliminate(TargetRuntimeId);
            case AdventureCandidateKind.Scout:
                return new ExpeditionObjectiveRuntime(ExpeditionObjectiveType.Scout);
            case AdventureCandidateKind.Secure:
                throw new InvalidOperationException("Secure autonomous candidates are not supported.");
            default:
                return ExpeditionObjectiveRuntime.Explore();
        }
    }

    private static IReadOnlyList<NpcDecisionParticipant> CaptureParticipants(IEnumerable<NpcDecisionParticipant> source)
    {
        List<NpcDecisionParticipant> snapshot = new List<NpcDecisionParticipant>();
        if (source != null)
        {
            foreach (NpcDecisionParticipant participant in source)
            {
                if (participant != null)
                {
                    snapshot.Add(new NpcDecisionParticipant(participant.RuntimeId, participant.Role));
                }
            }
        }

        return snapshot.AsReadOnly();
    }

    private static IReadOnlyList<string> CaptureIds(IEnumerable<string> source)
    {
        List<string> snapshot = new List<string>();
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        if (source != null)
        {
            foreach (string value in source)
            {
                if (string.IsNullOrWhiteSpace(value) == false && unique.Add(value) == true)
                {
                    snapshot.Add(value);
                }
            }
        }

        return snapshot.AsReadOnly();
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) == true ? null : value;
    }
}

public sealed class AdventureAutonomySystem
{
    private readonly AdventureAutonomySettings settings;
    private readonly IAdventurePartyAssemblyPolicy partyAssemblyPolicy;

    public AdventureAutonomySystem(
        AdventureAutonomySettings settings = null,
        IAdventurePartyAssemblyPolicy partyAssemblyPolicy = null)
    {
        this.settings = settings ?? new AdventureAutonomySettings();
        this.partyAssemblyPolicy = partyAssemblyPolicy ?? new SoloAdventurePartyAssemblyPolicy();
    }

    public IReadOnlyList<AdventureCandidate> BuildCandidates(
        NpcRuntime decisionMaker,
        SpatialNetworkRuntime spatialNetwork = null,
        IReadOnlyList<NpcRuntime> availableNpcs = null)
    {
        List<AdventureCandidate> candidates = new List<AdventureCandidate>();
        if (IsAvailable(decisionMaker, decisionMaker) == false)
        {
            return candidates.AsReadOnly();
        }

        BuildParty(
            decisionMaker,
            availableNpcs,
            out List<NpcDecisionParticipant> participants,
            out List<string> performers,
            out List<string> supports);

        foreach (ExplorableSiteKnowledgeObservation site in decisionMaker.ExplorableSiteKnowledge.Observations)
        {
            if (site == null || HasKnownRouteTo(decisionMaker, site.LocationRuntimeId, spatialNetwork) == false)
            {
                continue;
            }

            candidates.Add(CreateCandidate(
                AdventureCandidateKind.Explore,
                site,
                null,
                null,
                null,
                settings.exploreUtility,
                participants,
                performers,
                supports));

            if (settings.enableScout == true)
            {
                candidates.Add(CreateCandidate(
                    AdventureCandidateKind.Scout,
                    site,
                    null,
                    null,
                    null,
                    settings.scoutUtility,
                    participants,
                    performers,
                    supports));
            }

            AddIntelCandidates(decisionMaker, site, candidates, participants, performers, supports);
        }

        return candidates.AsReadOnly();
    }

    public AdventureCandidate ChooseCandidate(IReadOnlyList<AdventureCandidate> candidates)
    {
        AdventureCandidate selected = null;
        if (candidates == null)
        {
            return null;
        }

        foreach (AdventureCandidate candidate in candidates)
        {
            if (candidate != null && (selected == null || candidate.Utility > selected.Utility))
            {
                selected = candidate;
            }
        }

        return selected;
    }

    public NpcDecisionRecord RecordStartDecision(
        NpcRuntime decisionMaker,
        AdventureCandidate candidate,
        NpcDecisionRecorder recorder)
    {
        if (decisionMaker == null || candidate == null || recorder == null)
        {
            return null;
        }

        List<string> targetRuntimeIds = new List<string> { candidate.SiteRuntimeId };
        if (string.IsNullOrWhiteSpace(candidate.TargetRuntimeId) == false)
        {
            targetRuntimeIds.Add(candidate.TargetRuntimeId);
        }

        return recorder.RecordWithParticipants(
            decisionMaker.RuntimeId,
            NpcDecisionType.ExpeditionStart,
            NpcDecisionOrigin.Autonomous,
            "adventure:" + candidate.Kind,
            candidate.Participants,
            targetRuntimeIds,
            candidate.SiteLocationRuntimeId,
            null);
    }

    public static bool IsAvailable(NpcRuntime npcRuntime, NpcRuntime decisionMaker)
    {
        return npcRuntime != null
            && npcRuntime.IsAlive == true
            && npcRuntime.IsTraveling == false
            && string.IsNullOrWhiteSpace(npcRuntime.ActiveTravelPartyId) == true
            && (decisionMaker == null
                || decisionMaker.CurrentLocation == null
                || npcRuntime.CurrentLocation == decisionMaker.CurrentLocation);
    }

    private void AddIntelCandidates(
        NpcRuntime decisionMaker,
        ExplorableSiteKnowledgeObservation site,
        List<AdventureCandidate> candidates,
        List<NpcDecisionParticipant> participants,
        List<string> performers,
        List<string> supports)
    {
        foreach (AdventureOppositionObservation observation in decisionMaker.AdventureSiteIntelKnowledge.OppositionObservations)
        {
            if (observation != null
                && observation.ObservedState == AdventureOppositionObservedState.Active
                && string.Equals(observation.SiteRuntimeId, site.SiteRuntimeId, StringComparison.Ordinal))
            {
                candidates.Add(CreateCandidate(
                    AdventureCandidateKind.EliminateOpposition,
                    site,
                    observation.LocalPlaceRuntimeId,
                    observation.OppositionRuntimeId,
                    null,
                    settings.eliminateUtility,
                    participants,
                    performers,
                    supports));
            }
        }

        foreach (AdventureNotableItemObservation observation in decisionMaker.AdventureSiteIntelKnowledge.NotableItemObservations)
        {
            if (observation != null && string.Equals(observation.SiteRuntimeId, site.SiteRuntimeId, StringComparison.Ordinal))
            {
                candidates.Add(CreateCandidate(
                    AdventureCandidateKind.RetrieveNotableItem,
                    site,
                    observation.LocalPlaceRuntimeId,
                    observation.NotableItemRuntimeId,
                    observation.ItemDefinitionId,
                    settings.retrieveUtility,
                    participants,
                    performers,
                    supports));
            }
        }

        foreach (AdventureCommonResourceObservation observation in decisionMaker.AdventureSiteIntelKnowledge.CommonResourceObservations)
        {
            if (observation != null
                && observation.ObservedAmount > 0
                && string.Equals(observation.SiteRuntimeId, site.SiteRuntimeId, StringComparison.Ordinal))
            {
                candidates.Add(CreateCandidate(
                    AdventureCandidateKind.RetrieveCommonResource,
                    site,
                    observation.LocalPlaceRuntimeId,
                    observation.ItemDefinitionId,
                    observation.ItemDefinitionId,
                    settings.retrieveUtility,
                    participants,
                    performers,
                    supports));
            }
        }
    }

    private void BuildParty(
        NpcRuntime decisionMaker,
        IReadOnlyList<NpcRuntime> availableNpcs,
        out List<NpcDecisionParticipant> participants,
        out List<string> performers,
        out List<string> supports)
    {
        participants = new List<NpcDecisionParticipant>();
        performers = new List<string>();
        supports = new List<string>();
        HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<AdventurePartySelection> proposed = partyAssemblyPolicy.Assemble(
            decisionMaker,
            availableNpcs ?? Array.Empty<NpcRuntime>());

        if (proposed != null)
        {
            foreach (AdventurePartySelection selection in proposed)
            {
                NpcRuntime npc = selection?.NpcRuntime;
                if (IsAvailable(npc, decisionMaker) == false || selected.Add(npc.RuntimeId) == false)
                {
                    continue;
                }

                NpcDecisionParticipantRole role = selection.Role == NpcDecisionParticipantRole.Support
                    ? NpcDecisionParticipantRole.Support
                    : NpcDecisionParticipantRole.Performer;
                participants.Add(new NpcDecisionParticipant(npc.RuntimeId, role));
                if (role == NpcDecisionParticipantRole.Support)
                {
                    supports.Add(npc.RuntimeId);
                }
                else
                {
                    performers.Add(npc.RuntimeId);
                }
            }
        }

        if (selected.Contains(decisionMaker.RuntimeId) == false)
        {
            participants.Insert(0, new NpcDecisionParticipant(decisionMaker.RuntimeId, NpcDecisionParticipantRole.Performer));
            performers.Insert(0, decisionMaker.RuntimeId);
        }
    }

    private static bool HasKnownRouteTo(
        NpcRuntime decisionMaker,
        string targetLocationRuntimeId,
        SpatialNetworkRuntime spatialNetwork)
    {
        if (decisionMaker.CurrentLocation != null
            && string.Equals(decisionMaker.CurrentLocation.RuntimeId, targetLocationRuntimeId, StringComparison.Ordinal))
        {
            return true;
        }

        if (decisionMaker.CurrentLocation == null || spatialNetwork == null)
        {
            return false;
        }

        foreach (SpatialRouteRuntime route in spatialNetwork.GetOutgoingRoutes(decisionMaker.CurrentLocation))
        {
            if (route != null
                && decisionMaker.SpatialKnowledge.KnowsRoute(route.RuntimeId)
                && string.Equals(route.Destination.RuntimeId, targetLocationRuntimeId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static AdventureCandidate CreateCandidate(
        AdventureCandidateKind kind,
        ExplorableSiteKnowledgeObservation site,
        string localPlaceRuntimeId,
        string targetRuntimeId,
        string itemDefinitionId,
        float utility,
        List<NpcDecisionParticipant> participants,
        List<string> performers,
        List<string> supports)
    {
        return new AdventureCandidate(
            kind,
            site.SiteRuntimeId,
            site.LocationRuntimeId,
            localPlaceRuntimeId,
            targetRuntimeId,
            itemDefinitionId,
            utility,
            participants,
            performers,
            supports);
    }
}
