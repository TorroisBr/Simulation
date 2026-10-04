using System;
using System.Collections.Generic;
using System.Linq;

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

    /// <summary>Starts exactly two independently validated Person segments under one position/plan root per store.</summary>
    public bool TryBeginJointCivilLeg(IReadOnlyList<P8EJointTravelParticipant> participants, string sharedSegmentStableKey,
        out P8ETravelFailure failure)
    {
        if (!TryPrepareJointCivilLeg(participants, sharedSegmentStableKey, out PreparedP8EJointCivilLeg prepared, out failure)) return false;
        if (!prepared.TryInstall()) return Fail("Joint travel state changed before the atomic commit.", out failure);
        failure = P8ETravelFailure.None;
        return true;
    }

    internal bool TryPrepareJointCivilLeg(IReadOnlyList<P8EJointTravelParticipant> participants, string sharedSegmentStableKey,
        out PreparedP8EJointCivilLeg prepared, out P8ETravelFailure failure)
    {
        prepared = null;
        if (string.IsNullOrWhiteSpace(sharedSegmentStableKey) || participants == null || participants.Count != 2 || participants.Any(x => x == null)
            || participants.Select(x => x.PersonId?.Value).Distinct(StringComparer.Ordinal).Count() != 2)
            return Fail("Exactly two distinct Persons with separate movement contexts are required.", out failure);

        long expectedPositionRevision = positions.Revision;
        long expectedPlanRevision = plans.Revision;
        List<P8ETransitStart> starts = new List<P8ETransitStart>(2);
        List<PersonRoutePlanStatusChangeRequest> planChanges = new List<PersonRoutePlanStatusChangeRequest>(2);
        HexId sharedOrigin = null;
        HexId sharedDestination = null;
        foreach (P8EJointTravelParticipant participant in participants.OrderBy(x => x.PersonId.Value, StringComparer.Ordinal))
        {
            PersonId actor = participant.PersonId;
            if (participant.MovementContext == null || !plans.TryGetCurrent(actor, out PersonRoutePlan plan)
                || (plan.Status != PersonRoutePlanStatus.Accepted && plan.Status != PersonRoutePlanStatus.Active)
                || !positions.TryGetPosition(actor, out PersonSpatialPosition position) || position.IsInTransit
                || position.Position?.Kind != StablePositionReferenceKind.Hex
                || (plan.Status == PersonRoutePlanStatus.Accepted && plan.Candidate.OriginHexId != position.Position.HexId))
                return Fail("Both Persons need current accepted/active Hex routes from their current positions.", out failure);
            if (plan.Candidate.Legs.Count != 1 || (sharedDestination != null && plan.DestinationHexId != sharedDestination)
                || !string.Equals(plan.Candidate.Legs[0].Segment.StableKey, sharedSegmentStableKey, StringComparison.Ordinal))
                return Fail("Both accepted routes must contain exactly one leg to the same destination.", out failure);
            if (sharedOrigin != null && position.Position.HexId != sharedOrigin)
                return Fail("Both Persons must begin the shared civil leg at the same Hex.", out failure);
            sharedOrigin = position.Position.HexId;
            sharedDestination = plan.DestinationHexId;
            SpatialRouteCandidateLeg leg = plan.Candidate.Legs.FirstOrDefault(x => x.Segment.FromHexId == position.Position.HexId);
            if (leg == null) return Fail("A Person's current position is not a remaining route segment origin.", out failure);
            if (!passage.TryEvaluatePassage(leg.Segment.FromHexId, leg.Segment.ToHexId,
                leg.Segment.Option, participant.MovementContext, out PassageEvaluation evaluation, out SpatialAuthorityFailure passageFailure))
                return Fail(passageFailure?.Message ?? "A Person's current route segment could not be evaluated.", out failure);
            if (!evaluation.IsAvailable) return Fail("A selected traversal option is currently unavailable.", out failure);
            starts.Add(new P8ETransitStart(actor, leg.Segment.Option, leg.Segment.Boundary,
                leg.Segment.FromHexId, leg.Segment.ToHexId));
            if (plan.Status == PersonRoutePlanStatus.Accepted)
                planChanges.Add(new PersonRoutePlanStatusChangeRequest(actor, plan.PlanRevision,
                    PersonRoutePlanStatus.Accepted, PersonRoutePlanStatus.Active));
        }

        if (!positions.TryPrepareBeginTransitBatch(starts, expectedPositionRevision,
            out PreparedPersonSpatialPositionChange preparedPositions, out PersonSpatialPositionFailure positionFailure))
            return Fail(positionFailure.ToString(), out failure);
        PreparedPersonRoutePlanChange preparedPlans = null;
        if (planChanges.Count > 0 && !plans.TryPrepareStatusChangeBatch(planChanges, expectedPlanRevision,
            out preparedPlans, out PersonRoutePlanFailure planFailure))
            return Fail(planFailure.ToString(), out failure);
        prepared = new PreparedP8EJointCivilLeg(positions, plans, preparedPositions, preparedPlans);
        failure = P8ETravelFailure.None;
        return true;
    }

    internal bool TryPrepareFinalArrival(PersonId actor, out PreparedP8EPersonFinalArrival prepared,
        out P8ETravelFailure failure)
    {
        prepared = null;
        if (actor == null || !positions.TryGetPosition(actor, out PersonSpatialPosition current) || !current.IsInTransit
            || !plans.TryGetCurrent(actor, out PersonRoutePlan plan) || plan.Status != PersonRoutePlanStatus.Active
            || current.Transit.ProgressTicks != TraversalProgress.CompleteProgressTicks
            || current.Transit.ToHexId != plan.DestinationHexId || !PlanContainsTransit(plan, current.Transit))
            return Fail("Completed transit to the active plan destination is required for arrival.", out failure);
        if (!positions.TryPrepareArrive(actor, StablePositionReference.ForHex(plan.DestinationHexId), positions.Revision,
            out PreparedPersonSpatialPositionChange preparedPosition, out PersonSpatialPositionFailure positionFailure))
            return Fail(positionFailure.ToString(), out failure);
        if (!plans.TryPrepareStatusChange(actor, plans.Revision, plan.PlanRevision, PersonRoutePlanStatus.Active,
            PersonRoutePlanStatus.Completed, out PreparedPersonRoutePlanChange preparedPlan, out PersonRoutePlanFailure planFailure))
            return Fail(planFailure.ToString(), out failure);
        prepared = new PreparedP8EPersonFinalArrival(positions, plans, preparedPosition, preparedPlan);
        failure = P8ETravelFailure.None;
        return true;
    }

    internal bool TryGetFinalArrival(PersonId actor, out HexId destination)
    {
        destination = null;
        return actor != null && positions.TryGetPosition(actor, out PersonSpatialPosition position)
            && !position.IsInTransit && plans.TryGetCurrent(actor, out PersonRoutePlan plan)
            && plan.Status == PersonRoutePlanStatus.Completed && position.Position?.Kind == StablePositionReferenceKind.Hex
            && position.Position.HexId == plan.DestinationHexId && (destination = plan.DestinationHexId) != null;
    }

    internal bool TryGetPlanDestination(PersonId actor, out HexId destination)
    {
        destination = null;
        return actor != null && plans.TryGetCurrent(actor, out PersonRoutePlan plan)
            && (destination = plan.DestinationHexId) != null;
    }

    internal bool IsFinalArrivalReady(PersonId actor)
    {
        return actor != null && positions.TryGetPosition(actor, out PersonSpatialPosition position)
            && position.IsInTransit && position.Transit.ProgressTicks == TraversalProgress.CompleteProgressTicks
            && plans.TryGetCurrent(actor, out PersonRoutePlan plan) && plan.Status == PersonRoutePlanStatus.Active
            && position.Transit.ToHexId == plan.DestinationHexId && PlanContainsTransit(plan, position.Transit);
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

internal sealed class PreparedP8EJointCivilLeg
{
    private readonly PersonSpatialPositionStore positions;
    private readonly PersonRoutePlanStore plans;
    private readonly PreparedPersonSpatialPositionChange preparedPositions;
    private readonly PreparedPersonRoutePlanChange preparedPlans;
    internal PreparedP8EJointCivilLeg(PersonSpatialPositionStore positions, PersonRoutePlanStore plans,
        PreparedPersonSpatialPositionChange preparedPositions, PreparedPersonRoutePlanChange preparedPlans)
    { this.positions = positions; this.plans = plans; this.preparedPositions = preparedPositions; this.preparedPlans = preparedPlans; }
    internal bool CanInstall => positions.CanInstall(preparedPositions) && (preparedPlans == null || plans.CanInstall(preparedPlans));
    internal bool TryInstall()
    {
        if (!CanInstall) return false;
        InstallPrepared();
        return true;
    }
    internal void InstallPrepared()
    {
        positions.InstallPrepared(preparedPositions);
        if (preparedPlans != null) plans.InstallPrepared(preparedPlans);
    }
}

internal sealed class PreparedP8EPersonFinalArrival
{
    private readonly PersonSpatialPositionStore positions;
    private readonly PersonRoutePlanStore plans;
    private readonly PreparedPersonSpatialPositionChange preparedPosition;
    private readonly PreparedPersonRoutePlanChange preparedPlan;
    internal PreparedP8EPersonFinalArrival(PersonSpatialPositionStore positions, PersonRoutePlanStore plans,
        PreparedPersonSpatialPositionChange preparedPosition, PreparedPersonRoutePlanChange preparedPlan)
    { this.positions = positions; this.plans = plans; this.preparedPosition = preparedPosition; this.preparedPlan = preparedPlan; }
    internal bool CanInstall => positions.CanInstall(preparedPosition) && plans.CanInstall(preparedPlan);
    internal bool TryInstall()
    {
        if (!CanInstall) return false;
        InstallPrepared();
        return true;
    }
    internal void InstallPrepared()
    { positions.InstallPrepared(preparedPosition); plans.InstallPrepared(preparedPlan); }
}

public sealed class P8EJointTravelParticipant
{
    public PersonId PersonId { get; }
    public TraversalCostContext MovementContext { get; }
    public P8EJointTravelParticipant(PersonId personId, TraversalCostContext movementContext)
    { PersonId = personId ?? throw new ArgumentNullException(nameof(personId)); MovementContext = movementContext ?? throw new ArgumentNullException(nameof(movementContext)); }
}

public sealed class P8ETravelFailure
{
    public static P8ETravelFailure None { get; } = new P8ETravelFailure(string.Empty);
    public string Message { get; }
    public bool IsFailure => Message.Length != 0;
    private P8ETravelFailure(string message) { Message = message ?? string.Empty; }
    internal static P8ETravelFailure Create(string message) => new P8ETravelFailure(message);
}
