using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

public sealed class SpatialRoutePlanningRequest
{
    public PersonId ActorPersonId { get; }
    public StablePositionReference Origin { get; }
    public StablePositionReference Destination { get; }
    public long PlanningDay { get; }
    public long? MaximumOptionBeliefAgeDays { get; }

    public SpatialRoutePlanningRequest(
        PersonId actorPersonId,
        StablePositionReference origin,
        StablePositionReference destination,
        long planningDay,
        long? maximumOptionBeliefAgeDays = null)
    {
        ActorPersonId = actorPersonId;
        Origin = origin;
        Destination = destination;
        PlanningDay = planningDay;
        MaximumOptionBeliefAgeDays = maximumOptionBeliefAgeDays;
    }
}

/// <summary>One candidate leg together with the belief used to include it.</summary>
public sealed class SpatialRouteCandidateLeg
{
    public SpatialRouteSegment Segment { get; }
    public SpatialRouteOptionBelief Belief { get; }
    public bool IsBeliefStale { get; }
    public string BeliefObservationIdentity { get; }
    internal SpatialRouteCandidateLeg(SpatialRouteSegment segment, SpatialRouteOptionBelief belief, bool stale, string identity)
    { Segment = segment; Belief = belief; IsBeliefStale = stale; BeliefObservationIdentity = identity; }
}

/// <summary>An immutable simple route assembled only from actor-known segments.</summary>
public sealed class SpatialRouteCandidate
{
    private readonly ReadOnlyCollection<SpatialRouteCandidateLeg> legs;
    private readonly ReadOnlyCollection<SpatialRouteSegment> segments;
    public HexId OriginHexId { get; }
    public HexId DestinationHexId { get; }
    public IReadOnlyList<SpatialRouteCandidateLeg> Legs => legs;
    public IReadOnlyList<SpatialRouteSegment> Segments => segments;
    public SpatialRouteCandidateId Id { get; }
    public string SequenceKey => Id.StableKey;

    internal SpatialRouteCandidate(HexId origin, HexId destination, IEnumerable<SpatialRouteCandidateLeg> candidateLegs)
    {
        OriginHexId = new HexId(origin.Value);
        DestinationHexId = new HexId(destination.Value);
        List<SpatialRouteCandidateLeg> legCopies = candidateLegs == null
            ? new List<SpatialRouteCandidateLeg>() : new List<SpatialRouteCandidateLeg>(candidateLegs);
        List<SpatialRouteSegment> segmentCopies = new List<SpatialRouteSegment>(legCopies.Count);
        List<string> parts = new List<string> { "route-candidate-v1", OriginHexId.Value, DestinationHexId.Value };
        foreach (SpatialRouteCandidateLeg leg in legCopies)
        {
            segmentCopies.Add(leg.Segment);
            parts.Add(leg.Segment.StableKey);
        }
        legs = new ReadOnlyCollection<SpatialRouteCandidateLeg>(legCopies);
        segments = new ReadOnlyCollection<SpatialRouteSegment>(segmentCopies);
        Id = new SpatialRouteCandidateId(SpatialStableKey.Encode(parts.ToArray()));
    }
}

/// <summary>
/// Explicit comparison semantics for actor-known estimates. Metric/unit and
/// version are supplied by the caller; the selector invents no route objective.
/// </summary>
public sealed class SpatialRouteSelectionPolicy
{
    public string PolicyId { get; }
    public string Version { get; }
    public string EstimateMetricId { get; }
    public string EstimateUnitIdentity { get; }
    public bool PreferHigherEstimate { get; }
    public long MaximumEstimateAgeDays { get; }
    public bool RequireKnownAvailableOptions { get; }

    public SpatialRouteSelectionPolicy(
        string policyId,
        string version,
        string estimateMetricId,
        string estimateUnitIdentity,
        bool preferHigherEstimate,
        long maximumEstimateAgeDays,
        bool requireKnownAvailableOptions)
    {
        PolicyId = policyId;
        Version = version;
        EstimateMetricId = estimateMetricId;
        EstimateUnitIdentity = estimateUnitIdentity;
        PreferHigherEstimate = preferHigherEstimate;
        MaximumEstimateAgeDays = maximumEstimateAgeDays;
        RequireKnownAvailableOptions = requireKnownAvailableOptions;
    }

    public string StableKey => SpatialStableKey.Encode(
        PolicyId ?? string.Empty, Version ?? string.Empty, EstimateMetricId ?? string.Empty,
        EstimateUnitIdentity ?? string.Empty, PreferHigherEstimate ? "higher" : "lower",
        MaximumEstimateAgeDays.ToString(CultureInfo.InvariantCulture),
        RequireKnownAvailableOptions ? "known-available-required" : "unknown-status-allowed");

    public bool IsValid => !string.IsNullOrWhiteSpace(PolicyId)
        && !string.IsNullOrWhiteSpace(Version)
        && !string.IsNullOrWhiteSpace(EstimateMetricId)
        && !string.IsNullOrWhiteSpace(EstimateUnitIdentity)
        && MaximumEstimateAgeDays >= 0L;
}

public enum SpatialRoutePlanningFailureCode
{
    None = 0,
    InvalidRequest = 1,
    ActorNotRegistered = 2,
    UnsupportedRouteEndpoint = 3,
    UnresolvedHexEndpoint = 4,
    NoKnownRoute = 5,
    PolicyUnavailable = 6,
    InsufficientKnownEstimate = 7,
    InsufficientKnownOptionBelief = 8
}

public sealed class SpatialRoutePlanningOutcome
{
    private readonly ReadOnlyCollection<SpatialRouteCandidate> candidates;
    public SpatialRoutePlanningFailureCode FailureCode { get; }
    public string FailureMessage { get; }
    public bool IsSuccess => FailureCode == SpatialRoutePlanningFailureCode.None;
    public IReadOnlyList<SpatialRouteCandidate> Candidates => candidates;
    public SpatialRouteCandidate SelectedCandidate { get; }
    public SpatialRoutePlanningRequest Request { get; }
    public SpatialRouteSelectionPolicy Policy { get; }
    public SpatialKnowledgeBasis KnowledgeBasis { get; }
    public long PlanningDay => Request == null ? -1L : Request.PlanningDay;

    internal SpatialRoutePlanningOutcome(
        SpatialRoutePlanningFailureCode failureCode,
        string failureMessage,
        SpatialRoutePlanningRequest request,
        SpatialRouteSelectionPolicy policy,
        IEnumerable<SpatialRouteCandidate> values,
        SpatialRouteCandidate selected,
        SpatialKnowledgeBasis basis)
    {
        FailureCode = failureCode;
        FailureMessage = failureMessage ?? string.Empty;
        Request = request;
        Policy = policy;
        List<SpatialRouteCandidate> ordered = values == null ? new List<SpatialRouteCandidate>() : new List<SpatialRouteCandidate>(values);
        ordered.Sort((left, right) => StringComparer.Ordinal.Compare(left.SequenceKey, right.SequenceKey));
        candidates = new ReadOnlyCollection<SpatialRouteCandidate>(ordered);
        SelectedCandidate = selected;
        KnowledgeBasis = basis;
    }
}

/// <summary>
/// Pure route preview and selection over PersonId-owned Knowledge. The only
/// World Truth lookup is endpoint identity validation; passage authority is
/// never queried and geometric adjacency never creates an edge.
/// </summary>
public sealed class SpatialRoutePlanningSystem
{
    private readonly PersonStore personStore;
    private readonly SpatialAuthorityStore spatialAuthorityStore;
    private readonly SpatialRouteKnowledgeStore knowledgeStore;

    public SpatialRoutePlanningSystem(
        PersonStore personStore,
        SpatialAuthorityStore spatialAuthorityStore,
        SpatialRouteKnowledgeStore knowledgeStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        this.spatialAuthorityStore = spatialAuthorityStore ?? throw new ArgumentNullException(nameof(spatialAuthorityStore));
        this.knowledgeStore = knowledgeStore ?? throw new ArgumentNullException(nameof(knowledgeStore));
    }

    public SpatialRoutePlanningOutcome BuildKnownCandidates(SpatialRoutePlanningRequest request)
    {
        if (!ValidateRequest(request, out SpatialRoutePlanningFailureCode failure, out string message))
            return Failure(failure, message, request, null, null, null);

        List<string> consulted = new List<string>();
        List<SpatialRouteCandidateLeg> knownLegs = new List<SpatialRouteCandidateLeg>();
        foreach (SpatialSubject subject in knowledgeStore.GetSubjects(request.ActorPersonId, SpatialSubjectKind.TraversalOption))
        {
            if (!knowledgeStore.TryGetResolvedObservation(request.ActorPersonId, subject, request.PlanningDay,
                request.MaximumOptionBeliefAgeDays, out SpatialResolvedObservation resolved)) continue;

            SpatialObservation observation = resolved.Observation;
            consulted.Add(observation.StableIdentity);
            SpatialRouteOptionBelief belief = resolved.IsKnown
                ? observation.Value.RouteOptionBelief : SpatialRouteOptionBelief.Unknown;
            if (belief == SpatialRouteOptionBelief.KnownUnavailable) continue;

            SpatialRouteSegment segment = CloneSegment(subject.RouteSegment);
            knownLegs.Add(new SpatialRouteCandidateLeg(segment, belief, resolved.IsStale, observation.StableIdentity));
        }
        knownLegs.Sort((left, right) => CompareSegments(left.Segment, right.Segment));

        List<SpatialRouteCandidate> candidates = new List<SpatialRouteCandidate>();
        HexId origin = request.Origin.HexId;
        HexId destination = request.Destination.HexId;
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal) { origin.Value };
        List<SpatialRouteCandidateLeg> path = new List<SpatialRouteCandidateLeg>();
        BuildDepthFirst(origin, destination, knownLegs, visited, path, candidates);
        candidates.Sort((left, right) => StringComparer.Ordinal.Compare(left.SequenceKey, right.SequenceKey));
        SpatialKnowledgeBasis basis = knowledgeStore.CreateBasis(request.ActorPersonId, consulted);
        if (candidates.Count == 0)
            return Failure(SpatialRoutePlanningFailureCode.NoKnownRoute,
                "Actor Knowledge contains no eligible known route between the requested Hex endpoints.", request, null, candidates, basis);
        return Success(request, null, candidates, null, basis);
    }

    public SpatialRoutePlanningOutcome SelectKnownRoute(
        SpatialRoutePlanningRequest request,
        SpatialRouteSelectionPolicy policy)
    {
        if (policy == null || !policy.IsValid)
            return Failure(SpatialRoutePlanningFailureCode.PolicyUnavailable,
                "Route selection requires an explicit stable policy identity, version, metric, unit, and freshness window.", request, policy, null, null);

        SpatialRoutePlanningOutcome candidateOutcome = BuildKnownCandidates(request);
        if (!candidateOutcome.IsSuccess) return candidateOutcome;

        List<string> consulted = new List<string>(candidateOutcome.KnowledgeBasis.ObservationIdentities);
        List<SpatialRouteCandidate> candidates = new List<SpatialRouteCandidate>(candidateOutcome.Candidates);
        Dictionary<string, decimal> estimateByCandidate = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (SpatialRouteCandidate candidate in candidates)
        {
            if (policy.RequireKnownAvailableOptions)
            {
                foreach (SpatialRouteCandidateLeg leg in candidate.Legs)
                {
                    if (leg.Belief != SpatialRouteOptionBelief.KnownAvailable)
                        return Failure(SpatialRoutePlanningFailureCode.InsufficientKnownOptionBelief,
                            "The explicit selection policy requires a fresh KnownAvailable belief for every candidate segment.",
                            request, policy, candidates,
                            knowledgeStore.CreateBasis(request.ActorPersonId, consulted));
                }
            }

            SpatialSubject estimateSubject = SpatialSubject.ForRouteEstimate(candidate.Id, policy.EstimateMetricId);
            if (!knowledgeStore.TryGetResolvedObservation(request.ActorPersonId, estimateSubject, request.PlanningDay,
                    policy.MaximumEstimateAgeDays, out SpatialResolvedObservation resolved)
                || !resolved.IsKnown
                || resolved.Observation.Value.Kind != SpatialObservationValueKind.RouteEstimate
                || !string.Equals(resolved.Observation.Value.EstimateUnit, policy.EstimateUnitIdentity, StringComparison.Ordinal))
            {
                return Failure(SpatialRoutePlanningFailureCode.InsufficientKnownEstimate,
                    "A candidate lacks a fresh actor-known estimate matching the explicit policy metric and unit.",
                    request, policy, candidates,
                    knowledgeStore.CreateBasis(request.ActorPersonId, consulted));
            }
            consulted.Add(resolved.Observation.StableIdentity);
            estimateByCandidate.Add(candidate.SequenceKey, resolved.Observation.Value.Estimate);
        }

        SpatialRouteCandidate selected = null;
        decimal selectedEstimate = 0m;
        foreach (SpatialRouteCandidate candidate in candidates)
        {
            decimal estimate = estimateByCandidate[candidate.SequenceKey];
            if (selected == null
                || (policy.PreferHigherEstimate ? estimate > selectedEstimate : estimate < selectedEstimate)
                || (estimate == selectedEstimate && StringComparer.Ordinal.Compare(candidate.SequenceKey, selected.SequenceKey) < 0))
            {
                selected = candidate;
                selectedEstimate = estimate;
            }
        }

        SpatialKnowledgeBasis basis = knowledgeStore.CreateBasis(request.ActorPersonId, consulted);
        return Success(request, policy, candidates, selected, basis);
    }

    private bool ValidateRequest(
        SpatialRoutePlanningRequest request,
        out SpatialRoutePlanningFailureCode failure,
        out string message)
    {
        failure = SpatialRoutePlanningFailureCode.None;
        message = string.Empty;
        if (request == null || request.ActorPersonId == null || request.Origin == null || request.Destination == null
            || request.PlanningDay < 0L
            || (request.MaximumOptionBeliefAgeDays.HasValue && request.MaximumOptionBeliefAgeDays.Value < 0L))
        {
            failure = SpatialRoutePlanningFailureCode.InvalidRequest;
            message = "A valid actor, endpoint pair, planning day, and optional freshness window are required.";
            return false;
        }
        if (!personStore.TryGet(request.ActorPersonId, out _))
        {
            failure = SpatialRoutePlanningFailureCode.ActorNotRegistered;
            message = "Actor PersonId is not registered.";
            return false;
        }
        if (request.Origin.Kind != StablePositionReferenceKind.Hex
            || request.Destination.Kind != StablePositionReferenceKind.Hex)
        {
            failure = SpatialRoutePlanningFailureCode.UnsupportedRouteEndpoint;
            message = "P8-D regional route endpoints must be Hex references; Location and Crossing anchors do not imply connectors.";
            return false;
        }
        if (!spatialAuthorityStore.TryGet(request.Origin.HexId, out _)
            || !spatialAuthorityStore.TryGet(request.Destination.HexId, out _))
        {
            failure = SpatialRoutePlanningFailureCode.UnresolvedHexEndpoint;
            message = "Origin and destination Hex identities must resolve in the spatial authority.";
            return false;
        }
        return true;
    }

    private static void BuildDepthFirst(
        HexId current,
        HexId destination,
        IReadOnlyList<SpatialRouteCandidateLeg> knownLegs,
        HashSet<string> visited,
        List<SpatialRouteCandidateLeg> path,
        List<SpatialRouteCandidate> candidates)
    {
        if (current == destination)
        {
            candidates.Add(new SpatialRouteCandidate(FindOrigin(path, current), destination, path));
            return;
        }

        foreach (SpatialRouteCandidateLeg leg in knownLegs)
        {
            if (leg.Segment.FromHexId != current || visited.Contains(leg.Segment.ToHexId.Value)) continue;
            visited.Add(leg.Segment.ToHexId.Value);
            path.Add(leg);
            if (leg.Segment.ToHexId == destination)
            {
                candidates.Add(new SpatialRouteCandidate(FindOrigin(path, current), destination, path));
            }
            else
            {
                BuildDepthFirst(leg.Segment.ToHexId, destination, knownLegs, visited, path, candidates);
            }
            path.RemoveAt(path.Count - 1);
            visited.Remove(leg.Segment.ToHexId.Value);
        }
    }

    private static HexId FindOrigin(List<SpatialRouteCandidateLeg> path, HexId current)
    {
        return path.Count == 0 ? current : path[0].Segment.FromHexId;
    }

    private static int CompareSegments(SpatialRouteSegment left, SpatialRouteSegment right)
    {
        int boundary = left.Boundary.CompareTo(right.Boundary);
        if (boundary != 0) return boundary;
        int from = StringComparer.Ordinal.Compare(left.FromHexId.Value, right.FromHexId.Value);
        if (from != 0) return from;
        int to = StringComparer.Ordinal.Compare(left.ToHexId.Value, right.ToHexId.Value);
        return to != 0 ? to : left.Option.CompareTo(right.Option);
    }

    private static SpatialRouteSegment CloneSegment(SpatialRouteSegment value) => new SpatialRouteSegment(
        new HexBoundaryKey(new HexId(value.Boundary.FirstHexId.Value), new HexId(value.Boundary.SecondHexId.Value)),
        new HexId(value.FromHexId.Value), new HexId(value.ToHexId.Value), value.Option);

    private static SpatialRoutePlanningOutcome Success(
        SpatialRoutePlanningRequest request,
        SpatialRouteSelectionPolicy policy,
        IEnumerable<SpatialRouteCandidate> candidates,
        SpatialRouteCandidate selected,
        SpatialKnowledgeBasis basis) => new SpatialRoutePlanningOutcome(
            SpatialRoutePlanningFailureCode.None, string.Empty, request, policy, candidates, selected, basis);

    private static SpatialRoutePlanningOutcome Failure(
        SpatialRoutePlanningFailureCode code,
        string message,
        SpatialRoutePlanningRequest request,
        SpatialRouteSelectionPolicy policy,
        IEnumerable<SpatialRouteCandidate> candidates,
        SpatialKnowledgeBasis basis) => new SpatialRoutePlanningOutcome(code, message, request, policy, candidates, null, basis);

}
