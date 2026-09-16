using System;
using System.Collections.Generic;

public enum ExpeditionState
{
    Preparing,
    TravelingToSite,
    AtSite,
    Exploring,
    Returning,
    Completed
}

public enum ExpeditionObjectiveType
{
    Explore,
    Retrieve,
    Eliminate,
    Rescue,
    Scout,
    Escort,
    Secure
}

[Serializable]
public sealed class ExpeditionObjectiveRuntime
{
    private readonly ExpeditionObjectiveType objectiveType;
    private readonly string targetItemDefinitionId;
    private readonly string targetOppositionRuntimeId;
    private readonly int requiredProgress;
    private readonly bool allowContinueAfterCompletion;
    private int progress;
    private bool completed;

    public ExpeditionObjectiveType ObjectiveType => objectiveType;
    public ExpeditionObjectiveType Type => objectiveType;
    public string TargetItemDefinitionId => targetItemDefinitionId;
    public string TargetOppositionRuntimeId => targetOppositionRuntimeId;
    public int RequiredProgress => requiredProgress;
    public int Progress => progress;
    public bool IsCompleted => completed;
    public bool AllowContinueAfterCompletion => allowContinueAfterCompletion;

    public ExpeditionObjectiveRuntime(
        ExpeditionObjectiveType objectiveType,
        string targetItemDefinitionId = null,
        string targetOppositionRuntimeId = null,
        int requiredProgress = 1,
        bool allowContinueAfterCompletion = true)
    {
        if (Enum.IsDefined(typeof(ExpeditionObjectiveType), objectiveType) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(objectiveType));
        }

        if (requiredProgress <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredProgress));
        }

        this.objectiveType = objectiveType;
        this.targetItemDefinitionId = NormalizeOptionalId(targetItemDefinitionId);
        this.targetOppositionRuntimeId = NormalizeOptionalId(targetOppositionRuntimeId);
        this.requiredProgress = requiredProgress;
        this.allowContinueAfterCompletion = allowContinueAfterCompletion;
    }

    public static ExpeditionObjectiveRuntime Explore(int requiredProgress = 1)
    {
        return new ExpeditionObjectiveRuntime(ExpeditionObjectiveType.Explore, requiredProgress: requiredProgress);
    }

    public static ExpeditionObjectiveRuntime Retrieve(string itemDefinitionId)
    {
        return new ExpeditionObjectiveRuntime(ExpeditionObjectiveType.Retrieve, targetItemDefinitionId: itemDefinitionId);
    }

    public static ExpeditionObjectiveRuntime Eliminate(string oppositionRuntimeId)
    {
        return new ExpeditionObjectiveRuntime(ExpeditionObjectiveType.Eliminate, targetOppositionRuntimeId: oppositionRuntimeId);
    }

    internal void AddProgress(int progressDelta)
    {
        if (progressDelta <= 0 || completed == true)
        {
            return;
        }

        progress = progress >= requiredProgress - progressDelta
            ? requiredProgress
            : progress + progressDelta;

        if (progress >= requiredProgress)
        {
            completed = true;
        }
    }

    internal void MarkCompleted()
    {
        progress = requiredProgress;
        completed = true;
    }

    private static string NormalizeOptionalId(string value)
    {
        return string.IsNullOrWhiteSpace(value) == true ? null : value;
    }
}

[Serializable]
public sealed class ExpeditionRuntime
{
    private readonly string expeditionId;
    private readonly string targetSiteRuntimeId;
    private readonly string originLocationRuntimeId;
    private readonly string targetLocationRuntimeId;
    private readonly string outboundRouteRuntimeId;
    private string travelPartyId;
    private readonly string originDecisionId;
    private readonly IReadOnlyList<string> memberRuntimeIds;
    private readonly IReadOnlyList<string> performerRuntimeIds;
    private readonly IReadOnlyList<string> supportRuntimeIds;
    private readonly ExpeditionObjectiveRuntime objective;
    private readonly List<string> visitedLocalPlaceRuntimeIds = new List<string>();
    private readonly List<string> observedLocalConnectionRuntimeIds = new List<string>();
    private readonly IReadOnlyList<string> readOnlyVisitedLocalPlaceRuntimeIds;
    private readonly IReadOnlyList<string> readOnlyObservedLocalConnectionRuntimeIds;
    private ExpeditionState state;
    private string currentLocalPlaceRuntimeId;

    public string ExpeditionId => expeditionId;
    public string TargetSiteRuntimeId => targetSiteRuntimeId;
    public string OriginLocationRuntimeId => originLocationRuntimeId;
    public string TargetLocationRuntimeId => targetLocationRuntimeId;
    public string OutboundRouteRuntimeId => outboundRouteRuntimeId;
    public string TravelPartyId => travelPartyId;
    public string OriginDecisionId => originDecisionId;
    public IReadOnlyList<string> MemberRuntimeIds => memberRuntimeIds;
    public IReadOnlyList<string> PerformerRuntimeIds => performerRuntimeIds;
    public IReadOnlyList<string> SupportRuntimeIds => supportRuntimeIds;
    public ExpeditionObjectiveRuntime Objective => objective;
    public ExpeditionObjectiveRuntime ObjectiveRuntime => objective;
    public ExpeditionState State => state;
    public bool IsActive => state != ExpeditionState.Completed;
    public bool IsObjectiveComplete => objective.IsCompleted;
    public string CurrentLocalPlaceRuntimeId => currentLocalPlaceRuntimeId;
    public IReadOnlyList<string> VisitedLocalPlaceRuntimeIds => readOnlyVisitedLocalPlaceRuntimeIds;
    public IReadOnlyList<string> ObservedLocalConnectionRuntimeIds => readOnlyObservedLocalConnectionRuntimeIds;
    public int ExplorationProgress => objective.Progress;
    public int ExplorationProgressRequired => objective.RequiredProgress;

    public ExpeditionRuntime(
        string expeditionId,
        string targetSiteRuntimeId,
        string originLocationRuntimeId,
        string targetLocationRuntimeId,
        string outboundRouteRuntimeId,
        string travelPartyId,
        string originDecisionId,
        IEnumerable<string> memberRuntimeIds,
        IEnumerable<string> performerRuntimeIds,
        IEnumerable<string> supportRuntimeIds,
        ExpeditionState state = ExpeditionState.Preparing,
        ExpeditionObjectiveRuntime objective = null)
    {
        this.expeditionId = RequireId(expeditionId, nameof(expeditionId));
        this.targetSiteRuntimeId = RequireId(targetSiteRuntimeId, nameof(targetSiteRuntimeId));
        this.originLocationRuntimeId = RequireId(originLocationRuntimeId, nameof(originLocationRuntimeId));
        this.targetLocationRuntimeId = RequireId(targetLocationRuntimeId, nameof(targetLocationRuntimeId));
        this.outboundRouteRuntimeId = RequireId(outboundRouteRuntimeId, nameof(outboundRouteRuntimeId));

        if (Enum.IsDefined(typeof(ExpeditionState), state) == false)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        this.memberRuntimeIds = CaptureIds(memberRuntimeIds, nameof(memberRuntimeIds));
        this.performerRuntimeIds = CaptureIds(performerRuntimeIds, nameof(performerRuntimeIds));
        this.supportRuntimeIds = CaptureIds(supportRuntimeIds, nameof(supportRuntimeIds));
        ValidateParticipants(this.memberRuntimeIds, this.performerRuntimeIds, this.supportRuntimeIds);

        this.travelPartyId = NormalizeOptionalId(travelPartyId);
        if ((state == ExpeditionState.TravelingToSite || state == ExpeditionState.Returning) && this.travelPartyId == null)
        {
            throw new ArgumentException(
                "An expedition that has started traveling requires a TravelPartyId.",
                nameof(travelPartyId));
        }

        this.originDecisionId = NormalizeOptionalId(originDecisionId);
        this.objective = objective ?? ExpeditionObjectiveRuntime.Explore();
        this.state = state;
        readOnlyVisitedLocalPlaceRuntimeIds = visitedLocalPlaceRuntimeIds.AsReadOnly();
        readOnlyObservedLocalConnectionRuntimeIds = observedLocalConnectionRuntimeIds.AsReadOnly();
    }

    public ExpeditionRuntime(
        string expeditionId,
        string targetSiteRuntimeId,
        string originLocationRuntimeId,
        string targetLocationRuntimeId,
        string outboundRouteRuntimeId,
        IEnumerable<string> memberRuntimeIds,
        IEnumerable<string> performerRuntimeIds,
        IEnumerable<string> supportRuntimeIds,
        string originDecisionId = null,
        ExpeditionObjectiveRuntime objective = null)
        : this(
            expeditionId,
            targetSiteRuntimeId,
            originLocationRuntimeId,
            targetLocationRuntimeId,
            outboundRouteRuntimeId,
            null,
            originDecisionId,
            memberRuntimeIds,
            performerRuntimeIds,
            supportRuntimeIds,
            ExpeditionState.Preparing,
            objective)
    {
    }

    public bool CanBeginExploration()
    {
        return state == ExpeditionState.AtSite;
    }

    public bool CanContinueExploration()
    {
        return state == ExpeditionState.Exploring
            && (objective.IsCompleted == false || objective.AllowContinueAfterCompletion == true);
    }

    public bool CanBeginReturn()
    {
        return state == ExpeditionState.AtSite || state == ExpeditionState.Exploring;
    }

    public bool TryBeginExploration()
    {
        if (CanBeginExploration() == false)
        {
            return false;
        }

        state = ExpeditionState.Exploring;
        return true;
    }

    public bool TryAdvanceAbstractProgress(int progressDelta = 1)
    {
        if (CanContinueExploration() == false || progressDelta <= 0)
        {
            return false;
        }

        if (objective.ObjectiveType == ExpeditionObjectiveType.Explore
            || objective.ObjectiveType == ExpeditionObjectiveType.Scout)
        {
            objective.AddProgress(progressDelta);
        }
        return true;
    }

    public bool TrySetCurrentLocalPlace(string localPlaceRuntimeId)
    {
        if (CanContinueExploration() == false || string.IsNullOrWhiteSpace(localPlaceRuntimeId) == true)
        {
            return false;
        }

        currentLocalPlaceRuntimeId = localPlaceRuntimeId;
        if (visitedLocalPlaceRuntimeIds.Contains(localPlaceRuntimeId) == false)
        {
            visitedLocalPlaceRuntimeIds.Add(localPlaceRuntimeId);
        }

        if (objective.ObjectiveType == ExpeditionObjectiveType.Explore
            || objective.ObjectiveType == ExpeditionObjectiveType.Scout)
        {
            objective.AddProgress(1);
        }
        return true;
    }

    public bool TryRecordObservedConnection(string localConnectionRuntimeId)
    {
        if (CanContinueExploration() == false || string.IsNullOrWhiteSpace(localConnectionRuntimeId) == true)
        {
            return false;
        }

        if (observedLocalConnectionRuntimeIds.Contains(localConnectionRuntimeId) == false)
        {
            observedLocalConnectionRuntimeIds.Add(localConnectionRuntimeId);
        }

        return true;
    }

    public bool TryMarkObjectiveComplete()
    {
        if (state != ExpeditionState.Exploring && state != ExpeditionState.AtSite)
        {
            return false;
        }

        objective.MarkCompleted();
        return true;
    }

    public bool TryBeginReturn()
    {
        if (CanBeginReturn() == false)
        {
            return false;
        }

        state = ExpeditionState.Returning;
        return true;
    }

    public bool TryComplete()
    {
        if (state != ExpeditionState.Returning)
        {
            return false;
        }

        state = ExpeditionState.Completed;
        return true;
    }

    internal bool TryBeginTravel(string newTravelPartyId)
    {
        if (state != ExpeditionState.Preparing || string.IsNullOrWhiteSpace(newTravelPartyId) == true)
        {
            return false;
        }

        travelPartyId = newTravelPartyId;
        state = ExpeditionState.TravelingToSite;
        return true;
    }

    internal bool TryArriveAtSite()
    {
        if (state != ExpeditionState.TravelingToSite)
        {
            return false;
        }

        state = ExpeditionState.AtSite;
        return true;
    }

    internal bool TryBeginReturnTravel(string newTravelPartyId)
    {
        if (state != ExpeditionState.Returning || string.IsNullOrWhiteSpace(newTravelPartyId) == true)
        {
            return false;
        }

        travelPartyId = newTravelPartyId;
        return true;
    }

    internal bool TryCancelReturn()
    {
        if (state != ExpeditionState.Returning)
        {
            return false;
        }

        state = currentLocalPlaceRuntimeId == null ? ExpeditionState.AtSite : ExpeditionState.Exploring;
        return true;
    }

    private static void ValidateParticipants(
        IReadOnlyList<string> members,
        IReadOnlyList<string> performers,
        IReadOnlyList<string> supports)
    {
        if (members.Count == 0)
        {
            throw new ArgumentException("An expedition requires at least one member.", nameof(members));
        }

        if (performers.Count == 0)
        {
            throw new ArgumentException("An expedition requires at least one Performer.", nameof(performers));
        }

        HashSet<string> memberIds = new HashSet<string>(members, StringComparer.Ordinal);
        HashSet<string> roleIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (string runtimeId in performers)
        {
            if (memberIds.Contains(runtimeId) == false || roleIds.Add(runtimeId) == false)
            {
                throw new ArgumentException("Expedition Performer IDs must be unique members.", nameof(performers));
            }
        }

        foreach (string runtimeId in supports)
        {
            if (memberIds.Contains(runtimeId) == false || roleIds.Add(runtimeId) == false)
            {
                throw new ArgumentException("Expedition Support IDs must be unique members.", nameof(supports));
            }
        }

        if (roleIds.Count != memberIds.Count)
        {
            throw new ArgumentException("Every expedition member must have exactly one supported role.", nameof(members));
        }
    }

    private static IReadOnlyList<string> CaptureIds(IEnumerable<string> source, string parameterName)
    {
        List<string> snapshot = new List<string>();
        HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);

        if (source != null)
        {
            foreach (string runtimeId in source)
            {
                if (string.IsNullOrWhiteSpace(runtimeId) == true || uniqueIds.Add(runtimeId) == false)
                {
                    throw new ArgumentException("Expedition participant IDs must be non-empty and unique.", parameterName);
                }

                snapshot.Add(runtimeId);
            }
        }

        return snapshot.AsReadOnly();
    }

    private static string RequireId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Expedition requires stable IDs.", parameterName);
        }

        return value;
    }

    private static string NormalizeOptionalId(string value)
    {
        return string.IsNullOrWhiteSpace(value) == true ? null : value;
    }
}
