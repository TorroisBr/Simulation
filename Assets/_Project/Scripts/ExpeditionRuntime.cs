using System;
using System.Collections.Generic;

public enum ExpeditionState
{
    Preparing,
    TravelingToSite,
    AtSite
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
    private ExpeditionState state;

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
    public ExpeditionState State => state;
    public bool IsActive => true;

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
        ExpeditionState state = ExpeditionState.Preparing)
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
        if (state != ExpeditionState.Preparing && this.travelPartyId == null)
        {
            throw new ArgumentException(
                "An expedition that has started traveling requires a TravelPartyId.",
                nameof(travelPartyId));
        }

        this.originDecisionId = NormalizeOptionalId(originDecisionId);
        this.state = state;
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
        string originDecisionId = null)
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
            ExpeditionState.Preparing)
    {
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
