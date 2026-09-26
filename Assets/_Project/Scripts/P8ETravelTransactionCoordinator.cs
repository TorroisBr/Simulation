using System;

/// <summary>
/// Narrow atomic commit seam for the P8-E civil travel operation. Preparation is
/// performed by each owning authority; this coordinator only rechecks and swaps
/// their precomputed roots while the runtime's synchronous mutation boundary is
/// held by the caller.
/// </summary>
public sealed class P8ETravelTransactionCoordinator
{
    private readonly PersonSpatialPositionStore positions;
    private readonly PersonRoutePlanStore plans;
    private readonly SpatialRouteKnowledgeStore knowledge;
    private readonly SpatialPassageAuthority passage;
    private readonly Func<long> currentWorldDayProvider;

    internal P8ETravelTransactionCoordinator(PersonSpatialPositionStore positions,
        PersonRoutePlanStore plans, SpatialRouteKnowledgeStore knowledge, SpatialPassageAuthority passage,
        Func<long> currentWorldDayProvider)
    {
        this.positions = positions ?? throw new ArgumentNullException(nameof(positions));
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.knowledge = knowledge ?? throw new ArgumentNullException(nameof(knowledge));
        this.passage = passage ?? throw new ArgumentNullException(nameof(passage));
        this.currentWorldDayProvider = currentWorldDayProvider ?? throw new ArgumentNullException(nameof(currentWorldDayProvider));
    }

    public bool TryBeginNextSegment(PersonId actor, TraversalCostContext movementContext,
        out P8ETravelFailure failure)
    {
        if (actor == null || movementContext == null || !plans.TryGetCurrent(actor, out PersonRoutePlan plan)
            || (plan.Status != PersonRoutePlanStatus.Accepted && plan.Status != PersonRoutePlanStatus.Active)
            || !positions.TryGetPosition(actor, out PersonSpatialPosition position) || position.IsInTransit
            || position.Position?.Kind != StablePositionReferenceKind.Hex)
            return Fail("A current accepted/active Hex route and stable Person position are required.", out failure);
        if (plan.Status == PersonRoutePlanStatus.Accepted && plan.Candidate.OriginHexId != position.Position.HexId)
            return Fail("An accepted route may depart only from its planned origin Hex.", out failure);
        SpatialRouteCandidateLeg leg = null;
        foreach (SpatialRouteCandidateLeg candidateLeg in plan.Candidate.Legs)
            if (candidateLeg.Segment.FromHexId == position.Position.HexId) { leg = candidateLeg; break; }
        if (leg == null) return Fail("The current stable position is not a remaining segment origin in the accepted plan.", out failure);
        if (!passage.TryEvaluatePassage(leg.Segment.FromHexId, leg.Segment.ToHexId,
            leg.Segment.Option, movementContext, out PassageEvaluation evaluation, out SpatialAuthorityFailure passageFailure))
            return Fail(passageFailure?.Message ?? "Current passage evaluation failed.", out failure);
        if (!evaluation.IsAvailable) return Fail("The selected traversal option is currently unavailable.", out failure);

        long positionRevision = positions.Revision;
        if (!positions.TryPrepareBeginTransit(actor, leg.Segment.Option, leg.Segment.Boundary,
            leg.Segment.FromHexId, leg.Segment.ToHexId, positionRevision,
            out PreparedPersonSpatialPositionChange preparedPosition, out PersonSpatialPositionFailure positionFailure))
            return Fail(positionFailure.ToString(), out failure);
        PreparedPersonRoutePlanChange preparedPlan = null;
        if (plan.Status == PersonRoutePlanStatus.Accepted)
        {
            if (!plans.TryPrepareStatusChange(actor, plans.Revision, plan.PlanRevision,
                PersonRoutePlanStatus.Accepted, PersonRoutePlanStatus.Active,
                out preparedPlan, out PersonRoutePlanFailure planFailure))
                return Fail(planFailure.ToString(), out failure);
        }
        if (!TryCommit(preparedPosition, preparedPlan, null)) return Fail("Travel state changed before the atomic commit.", out failure);
        failure = P8ETravelFailure.None;
        return true;
    }

    public bool TryAdvanceSegment(PersonId actor, int progressTicks, out P8ETravelFailure failure)
    {
        if (actor == null || !positions.TryGetPosition(actor, out PersonSpatialPosition current) || !current.IsInTransit
            || !plans.TryGetCurrent(actor, out PersonRoutePlan plan) || plan.Status != PersonRoutePlanStatus.Active)
            return Fail("Active transit and its active Person route plan are required.", out failure);
        if (!PlanContainsTransit(plan, current.Transit))
            return Fail("Current transit does not match a segment in the active route plan.", out failure);
        if (!positions.TryPrepareAdvanceTransit(actor, progressTicks, positions.Revision,
            out PreparedPersonSpatialPositionChange preparedPosition, out PersonSpatialPositionFailure positionFailure))
            return Fail(positionFailure.ToString(), out failure);
        if (!TryCommit(preparedPosition, null, null)) return Fail("Travel state changed before the atomic commit.", out failure);
        failure = P8ETravelFailure.None;
        return true;
    }

    public bool TryArriveAtIntermediateHex(PersonId actor, out P8ETravelFailure failure)
    {
        if (actor == null || !positions.TryGetPosition(actor, out PersonSpatialPosition current) || !current.IsInTransit
            || !plans.TryGetCurrent(actor, out PersonRoutePlan plan) || plan.Status != PersonRoutePlanStatus.Active
            || current.Transit.ProgressTicks != TraversalProgress.CompleteProgressTicks
            || current.Transit.ToHexId == plan.DestinationHexId || !PlanContainsTransit(plan, current.Transit))
            return Fail("Completed transit on a non-final active-plan segment is required for intermediate arrival.", out failure);
        if (!positions.TryPrepareArrive(actor, StablePositionReference.ForHex(current.Transit.ToHexId), positions.Revision,
            out PreparedPersonSpatialPositionChange preparedPosition, out PersonSpatialPositionFailure positionFailure))
            return Fail(positionFailure.ToString(), out failure);
        if (!TryCommit(preparedPosition, null, null)) return Fail("Travel state changed before the atomic commit.", out failure);
        failure = P8ETravelFailure.None;
        return true;
    }

    public bool TryInterruptRejectedNextSegment(PersonId actor, TraversalCostContext movementContext,
        SpatialObservation supportedObservation, long currentWorldDay, out P8ETravelFailure failure)
    {
        long authoritativeWorldDay = currentWorldDayProvider();
        if (currentWorldDay != authoritativeWorldDay)
            return Fail("The supplied world day does not match the current SimulationRuntime day.", out failure);

        long expectedPositionRevision = positions.Revision;
        if (actor == null || movementContext == null || authoritativeWorldDay < 0L
            || !positions.TryGetPosition(actor, out PersonSpatialPosition current) || current.IsInTransit
            || current.Position?.Kind != StablePositionReferenceKind.Hex
            || !plans.TryGetCurrent(actor, out PersonRoutePlan plan) || plan.Status != PersonRoutePlanStatus.Active)
            return Fail("An active plan at a stable Hex is required to interrupt a rejected segment.", out failure);
        SpatialRouteCandidateLeg leg = null;
        foreach (SpatialRouteCandidateLeg candidateLeg in plan.Candidate.Legs)
            if (candidateLeg.Segment.FromHexId == current.Position.HexId) { leg = candidateLeg; break; }
        if (leg == null) return Fail("The stable position is not a remaining segment origin in the active plan.", out failure);
        if (!passage.TryEvaluatePassage(leg.Segment.FromHexId, leg.Segment.ToHexId,
            leg.Segment.Option, movementContext, out PassageEvaluation evaluation, out SpatialAuthorityFailure passageFailure))
            return Fail(passageFailure?.Message ?? "Current passage evaluation failed.", out failure);
        if (evaluation.IsAvailable) return Fail("Only a currently rejected next segment can interrupt the active route.", out failure);
        if (supportedObservation != null && (supportedObservation.Subject.Kind != SpatialSubjectKind.TraversalOption
            || !supportedObservation.Subject.Equals(SpatialSubject.ForTraversalOption(leg.Segment))
            || supportedObservation.Value.Kind != SpatialObservationValueKind.RouteOptionBelief
            || supportedObservation.Value.RouteOptionBelief != SpatialRouteOptionBelief.KnownUnavailable
            || supportedObservation.ReceivedDay != authoritativeWorldDay))
            return Fail("Supported rejection evidence must be a same-day KnownUnavailable observation of the attempted traversal option.", out failure);
        if (!plans.TryPrepareStatusChange(actor, plans.Revision, plan.PlanRevision,
            PersonRoutePlanStatus.Active, PersonRoutePlanStatus.Interrupted,
            out PreparedPersonRoutePlanChange preparedPlan, out PersonRoutePlanFailure planFailure))
            return Fail(planFailure.ToString(), out failure);
        PreparedSpatialKnowledgeChange preparedKnowledge = null;
        if (supportedObservation != null && !knowledge.TryPrepareObservation(actor, supportedObservation,
            authoritativeWorldDay, knowledge.Revision, out preparedKnowledge, out SpatialKnowledgeFailure knowledgeFailure))
            return Fail(knowledgeFailure.ToString(), out failure);
        if (!TryCommit(null, preparedPlan, preparedKnowledge, expectedPositionRevision)) return Fail("Travel state changed before the atomic commit.", out failure);
        failure = P8ETravelFailure.None;
        return true;
    }

    public bool TryArriveAtFinalDestination(PersonId actor, out P8ETravelFailure failure)
    {
        if (actor == null || !positions.TryGetPosition(actor, out PersonSpatialPosition current) || !current.IsInTransit
            || !plans.TryGetCurrent(actor, out PersonRoutePlan plan) || plan.Status != PersonRoutePlanStatus.Active
            || current.Transit.ProgressTicks != TraversalProgress.CompleteProgressTicks
            || current.Transit.ToHexId != plan.DestinationHexId || !PlanContainsTransit(plan, current.Transit))
            return Fail("Completed transit to the active plan destination is required for arrival.", out failure);
        if (!positions.TryPrepareArrive(actor, StablePositionReference.ForHex(plan.DestinationHexId), positions.Revision,
            out PreparedPersonSpatialPositionChange preparedPosition, out PersonSpatialPositionFailure positionFailure))
            return Fail(positionFailure.ToString(), out failure);
        if (!plans.TryPrepareStatusChange(actor, plans.Revision, plan.PlanRevision,
            PersonRoutePlanStatus.Active, PersonRoutePlanStatus.Completed,
            out PreparedPersonRoutePlanChange preparedPlan, out PersonRoutePlanFailure planFailure))
            return Fail(planFailure.ToString(), out failure);
        if (!TryCommit(preparedPosition, preparedPlan, null)) return Fail("Travel state changed before the atomic commit.", out failure);
        failure = P8ETravelFailure.None;
        return true;
    }

    internal bool TryCommit(PreparedPersonSpatialPositionChange position,
        PreparedPersonRoutePlanChange plan, PreparedSpatialKnowledgeChange observation,
        long? expectedPositionRevision = null)
    {
        if ((position != null && !positions.CanInstall(position)) || (position == null && plan == null && observation == null)
            || (expectedPositionRevision.HasValue && positions.Revision != expectedPositionRevision.Value)
            || (plan != null && !plans.CanInstall(plan))
            || (observation != null && !knowledge.CanInstall(observation))) return false;

        // No validation, callbacks, allocation, or fallible work after this point.
        if (position != null) positions.InstallPrepared(position);
        if (plan != null) plans.InstallPrepared(plan);
        if (observation != null) knowledge.InstallPrepared(observation);
        return true;
    }

    private static bool Fail(string message, out P8ETravelFailure failure)
    { failure = P8ETravelFailure.Create(message); return false; }

    private static bool PlanContainsTransit(PersonRoutePlan plan, TraversalProgress transit)
    {
        if (plan == null || transit == null) return false;
        foreach (SpatialRouteCandidateLeg leg in plan.Candidate.Legs)
        {
            SpatialRouteSegment segment = leg.Segment;
            if (segment.FromHexId == transit.FromHexId && segment.ToHexId == transit.ToHexId
                && segment.Option.Equals(transit.Option) && segment.Boundary.Equals(transit.Boundary)) return true;
        }
        return false;
    }
}

public sealed class P8ETravelFailure
{
    public static P8ETravelFailure None { get; } = new P8ETravelFailure(string.Empty);
    public string Message { get; }
    public bool IsFailure => Message.Length != 0;
    private P8ETravelFailure(string message) { Message = message ?? string.Empty; }
    internal static P8ETravelFailure Create(string message) => new P8ETravelFailure(message);
}
