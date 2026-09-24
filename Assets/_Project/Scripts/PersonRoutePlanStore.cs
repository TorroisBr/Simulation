using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

public enum PersonRoutePlanStatus
{
    Active = 0,
    Superseded = 1,
    Interrupted = 2
}

/// <summary>PersonId-owned travel intent. It contains no position or progress state.</summary>
public sealed class PersonRoutePlan
{
    public PersonId ActorPersonId { get; }
    public HexId DestinationHexId { get; }
    public SpatialRouteCandidate Candidate { get; }
    public SpatialRouteSelectionPolicy SelectionPolicy { get; }
    public SpatialKnowledgeBasis KnowledgeBasis { get; }
    public string DecisionIdentity { get; }
    public long AcceptedDay { get; }
    public long PlanRevision { get; }
    public PersonRoutePlanStatus Status { get; }
    public string StableKey => SpatialStableKey.Encode(
        ActorPersonId.Value, PlanRevision.ToString(CultureInfo.InvariantCulture),
        DecisionIdentity, Candidate.Id.StableKey);

    internal PersonRoutePlan(
        PersonId actor,
        HexId destination,
        SpatialRouteCandidate candidate,
        SpatialRouteSelectionPolicy policy,
        SpatialKnowledgeBasis basis,
        string decisionIdentity,
        long acceptedDay,
        long revision,
        PersonRoutePlanStatus status)
    {
        ActorPersonId = actor ?? throw new ArgumentNullException(nameof(actor));
        DestinationHexId = destination ?? throw new ArgumentNullException(nameof(destination));
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        SelectionPolicy = policy ?? throw new ArgumentNullException(nameof(policy));
        KnowledgeBasis = basis ?? throw new ArgumentNullException(nameof(basis));
        DecisionIdentity = decisionIdentity ?? throw new ArgumentNullException(nameof(decisionIdentity));
        AcceptedDay = acceptedDay;
        PlanRevision = revision;
        Status = status;
    }
}

public enum PersonRoutePlanFailureCode
{
    None = 0,
    RuntimeFaulted = 1,
    InvalidPlan = 2,
    ActorNotRegistered = 3,
    InvalidDecisionIdentity = 4,
    StaleKnowledgeBasis = 5,
    PlanRevisionMismatch = 6,
    RevisionOverflow = 7
}

public sealed class PersonRoutePlanFailure
{
    private PersonRoutePlanFailure(PersonRoutePlanFailureCode code, string message)
    { Code = code; Message = message ?? string.Empty; }
    public static PersonRoutePlanFailure None { get; } = new PersonRoutePlanFailure(PersonRoutePlanFailureCode.None, string.Empty);
    public PersonRoutePlanFailureCode Code { get; }
    public string Message { get; }
    public bool IsFailure => Code != PersonRoutePlanFailureCode.None;
    internal static PersonRoutePlanFailure Create(PersonRoutePlanFailureCode code, string message) => new PersonRoutePlanFailure(code, message);
    public override string ToString() => Code + (Message.Length == 0 ? string.Empty : ": " + Message);
}

/// <summary>
/// Intent-plan authority keyed by persistent PersonId. Plan replacement is an
/// explicit atomic mutation guarded by actor revision and current Knowledge basis.
/// </summary>
public sealed class PersonRoutePlanStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly PersonStore personStore;
    private readonly SpatialRouteKnowledgeStore knowledgeStore;
    private readonly Dictionary<string, List<PersonRoutePlan>> plansByActor = new Dictionary<string, List<PersonRoutePlan>>(StringComparer.Ordinal);
    private long revision;

    public PersonRoutePlanStore(PersonStore personStore, SpatialRouteKnowledgeStore knowledgeStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        this.knowledgeStore = knowledgeStore ?? throw new ArgumentNullException(nameof(knowledgeStore));
    }

    public long Revision => revision;
    public int PlanCount
    {
        get
        {
            int count = 0;
            foreach (List<PersonRoutePlan> plans in plansByActor.Values) count += plans.Count;
            return count;
        }
    }

    public IReadOnlyList<PersonRoutePlan> Plans
    {
        get
        {
            List<PersonRoutePlan> result = new List<PersonRoutePlan>();
            foreach (List<PersonRoutePlan> values in plansByActor.Values)
                if (values.Count > 0) result.Add(values[values.Count - 1]);
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.ActorPersonId.Value, right.ActorPersonId.Value));
            return new ReadOnlyCollection<PersonRoutePlan>(result);
        }
    }

    public IReadOnlyList<PersonRoutePlan> History
    {
        get
        {
            List<PersonRoutePlan> result = new List<PersonRoutePlan>();
            foreach (List<PersonRoutePlan> values in plansByActor.Values) result.AddRange(values);
            result.Sort((left, right) =>
            {
                int actor = StringComparer.Ordinal.Compare(left.ActorPersonId.Value, right.ActorPersonId.Value);
                return actor != 0 ? actor : left.PlanRevision.CompareTo(right.PlanRevision);
            });
            return new ReadOnlyCollection<PersonRoutePlan>(result);
        }
    }

    public long GetActorPlanRevision(PersonId actor)
    {
        return actor != null && plansByActor.TryGetValue(actor.Value, out List<PersonRoutePlan> values) && values.Count > 0
            ? values[values.Count - 1].PlanRevision : 0L;
    }

    public bool TryGetCurrent(PersonId actor, out PersonRoutePlan plan)
    {
        plan = null;
        if (actor == null || !plansByActor.TryGetValue(actor.Value, out List<PersonRoutePlan> values) || values.Count == 0) return false;
        plan = values[values.Count - 1];
        return true;
    }

    public bool TryAcceptPlan(
        SpatialRoutePlanningOutcome selectedOutcome,
        string decisionIdentity,
        long expectedActorPlanRevision,
        long acceptedDay,
        out PersonRoutePlanFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
            return Fail(PersonRoutePlanFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        if (selectedOutcome == null || !selectedOutcome.IsSuccess || selectedOutcome.SelectedCandidate == null
            || selectedOutcome.Request == null || selectedOutcome.Request.ActorPersonId == null
            || selectedOutcome.Policy == null || !selectedOutcome.Policy.IsValid
            || selectedOutcome.KnowledgeBasis == null || string.IsNullOrWhiteSpace(decisionIdentity)
            || acceptedDay < 0L || selectedOutcome.PlanningDay != acceptedDay
            || !IsCandidateConsistent(selectedOutcome))
            return Fail(string.IsNullOrWhiteSpace(decisionIdentity)
                    ? PersonRoutePlanFailureCode.InvalidDecisionIdentity : PersonRoutePlanFailureCode.InvalidPlan,
                "A successful selected route, explicit policy, current-day basis, and stable decision identity are required.", out failure);

        PersonId actor = selectedOutcome.Request.ActorPersonId;
        if (!personStore.TryGet(actor, out _))
            return Fail(PersonRoutePlanFailureCode.ActorNotRegistered, "Plan actor PersonId is not registered.", out failure);
        if (selectedOutcome.KnowledgeBasis.ActorPersonId != actor
            || !knowledgeStore.IsBasisCurrent(selectedOutcome.KnowledgeBasis))
            return Fail(PersonRoutePlanFailureCode.StaleKnowledgeBasis, "The actor Knowledge basis changed after selection.", out failure);
        if (expectedActorPlanRevision != GetActorPlanRevision(actor))
            return Fail(PersonRoutePlanFailureCode.PlanRevisionMismatch, "The current Person route-plan revision does not match the caller's expected revision.", out failure);
        long actorRevision = GetActorPlanRevision(actor);
        if (revision == long.MaxValue || actorRevision == long.MaxValue)
            return Fail(PersonRoutePlanFailureCode.RevisionOverflow, "Person route-plan revision cannot advance.", out failure);

        List<PersonRoutePlan> nextPlans = plansByActor.TryGetValue(actor.Value, out List<PersonRoutePlan> current)
            ? new List<PersonRoutePlan>(current) : new List<PersonRoutePlan>();
        if (nextPlans.Count > 0 && nextPlans[nextPlans.Count - 1].Status == PersonRoutePlanStatus.Active)
        {
            PersonRoutePlan previous = nextPlans[nextPlans.Count - 1];
            nextPlans[nextPlans.Count - 1] = CreatePlan(
                previous.ActorPersonId, previous.DestinationHexId, previous.Candidate,
                previous.SelectionPolicy, previous.KnowledgeBasis, previous.DecisionIdentity,
                previous.AcceptedDay, previous.PlanRevision, PersonRoutePlanStatus.Superseded);
        }

        PersonRoutePlan created = CreatePlan(actor, selectedOutcome.SelectedCandidate.DestinationHexId,
            selectedOutcome.SelectedCandidate, selectedOutcome.Policy, selectedOutcome.KnowledgeBasis,
            decisionIdentity, acceptedDay, actorRevision + 1L, PersonRoutePlanStatus.Active);
        nextPlans.Add(created);
        plansByActor[actor.Value] = nextPlans;
        revision++;
        failure = PersonRoutePlanFailure.None;
        return true;
    }

    public PersonRoutePlanInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();
        if (revision < 0L) violations.Add("Person route-plan store revision is negative.");
        long count = 0L;
        foreach (KeyValuePair<string, List<PersonRoutePlan>> entry in plansByActor)
        {
            PersonId actor = new PersonId(entry.Key);
            if (!personStore.TryGet(actor, out _)) violations.Add("Person route plan references an unregistered PersonId: " + entry.Key + ".");
            if (entry.Value == null || entry.Value.Count == 0) { violations.Add("Person route-plan history is empty for " + entry.Key + "."); continue; }
            count += entry.Value.Count;
            long expectedPlanRevision = 1L;
            int activeCount = 0;
            foreach (PersonRoutePlan plan in entry.Value)
            {
                if (plan == null || plan.ActorPersonId == null || plan.ActorPersonId != actor
                    || plan.DestinationHexId == null || plan.Candidate == null || plan.SelectionPolicy == null
                    || !plan.SelectionPolicy.IsValid || plan.KnowledgeBasis == null
                    || plan.KnowledgeBasis.ActorPersonId != actor || string.IsNullOrWhiteSpace(plan.DecisionIdentity)
                    || plan.AcceptedDay < 0L || plan.PlanRevision != expectedPlanRevision
                    || !knowledgeStore.ContainsBasis(plan.KnowledgeBasis)
                    || !Enum.IsDefined(typeof(PersonRoutePlanStatus), plan.Status)
                    || !IsValidCandidate(plan.Candidate, actor, plan.DestinationHexId))
                    violations.Add("Person route plan is structurally invalid for " + entry.Key + ".");
                if (plan != null && plan.Status == PersonRoutePlanStatus.Active) activeCount++;
                expectedPlanRevision++;
            }
            if (activeCount > 1) violations.Add("Person has multiple active route plans: " + entry.Key + ".");
            if (entry.Value[entry.Value.Count - 1].Status == PersonRoutePlanStatus.Superseded)
                violations.Add("Latest Person route plan cannot be superseded: " + entry.Key + ".");
        }
        if (count != revision) violations.Add("Person route-plan revision does not match accepted plan history count.");
        return new PersonRoutePlanInvariantReport(violations);
    }

    internal PersonRoutePlanStore Clone(PersonStore targetPersons, SpatialRouteKnowledgeStore targetKnowledge)
    {
        if (targetPersons == null) throw new ArgumentNullException(nameof(targetPersons));
        if (targetKnowledge == null) throw new ArgumentNullException(nameof(targetKnowledge));
        PersonRoutePlanStore copy = new PersonRoutePlanStore(targetPersons, targetKnowledge);
        foreach (KeyValuePair<string, List<PersonRoutePlan>> entry in plansByActor)
        {
            PersonId actor = new PersonId(entry.Key);
            if (!targetPersons.TryGet(actor, out _)) throw new ArgumentException("Route-plan actor is absent from target PersonStore.", nameof(targetPersons));
            List<PersonRoutePlan> cloned = new List<PersonRoutePlan>();
            foreach (PersonRoutePlan plan in entry.Value)
            {
                if (!targetKnowledge.ContainsBasis(plan.KnowledgeBasis))
                    throw new ArgumentException("Route-plan Knowledge basis is not present in the target Knowledge store.", nameof(targetKnowledge));
                cloned.Add(CreatePlan(plan.ActorPersonId, plan.DestinationHexId, plan.Candidate,
                    plan.SelectionPolicy, plan.KnowledgeBasis, plan.DecisionIdentity,
                    plan.AcceptedDay, plan.PlanRevision, plan.Status));
            }
            copy.plansByActor.Add(entry.Key, cloned);
        }
        copy.revision = revision;
        return copy;
    }

    private static bool IsCandidateConsistent(SpatialRoutePlanningOutcome outcome)
    {
        SpatialRoutePlanningRequest request = outcome.Request;
        SpatialRouteCandidate selected = outcome.SelectedCandidate;
        if (request.Origin.Kind != StablePositionReferenceKind.Hex
            || request.Destination.Kind != StablePositionReferenceKind.Hex
            || selected.OriginHexId != request.Origin.HexId
            || selected.DestinationHexId != request.Destination.HexId
            || !outcome.Candidates.Contains(selected)
            || !IsValidCandidate(selected, request.ActorPersonId, request.Destination.HexId)) return false;
        return true;
    }

    private static bool IsValidCandidate(SpatialRouteCandidate candidate, PersonId actor, HexId destination)
    {
        if (candidate == null || actor == null || destination == null || candidate.DestinationHexId != destination) return false;
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal) { candidate.OriginHexId.Value };
        HexId current = candidate.OriginHexId;
        foreach (SpatialRouteCandidateLeg leg in candidate.Legs)
        {
            if (leg == null || leg.Segment == null || leg.BeliefObservationIdentity == null
                || leg.Segment.FromHexId != current || !visited.Add(leg.Segment.ToHexId.Value)) return false;
            current = leg.Segment.ToHexId;
        }
        return current == candidate.DestinationHexId
            && string.Equals(candidate.Id.StableKey, RecomputeCandidateKey(candidate), StringComparison.Ordinal);
    }

    private static string RecomputeCandidateKey(SpatialRouteCandidate candidate)
    {
        List<string> parts = new List<string> { "route-candidate-v1", candidate.OriginHexId.Value, candidate.DestinationHexId.Value };
        foreach (SpatialRouteSegment segment in candidate.Segments) parts.Add(segment.StableKey);
        return SpatialStableKey.Encode(parts.ToArray());
    }

    private static PersonRoutePlan CreatePlan(
        PersonId actor,
        HexId destination,
        SpatialRouteCandidate candidate,
        SpatialRouteSelectionPolicy policy,
        SpatialKnowledgeBasis basis,
        string decisionIdentity,
        long acceptedDay,
        long planRevision,
        PersonRoutePlanStatus status)
    {
        SpatialRouteCandidate copiedCandidate = CloneCandidate(candidate);
        SpatialRouteSelectionPolicy copiedPolicy = new SpatialRouteSelectionPolicy(
            policy.PolicyId, policy.Version, policy.EstimateMetricId, policy.EstimateUnitIdentity,
            policy.PreferHigherEstimate, policy.MaximumEstimateAgeDays, policy.RequireKnownAvailableOptions);
        SpatialKnowledgeBasis copiedBasis = new SpatialKnowledgeBasis(
            new PersonId(basis.ActorPersonId.Value), basis.ActorKnowledgeRevision, basis.ObservationIdentities);
        return new PersonRoutePlan(new PersonId(actor.Value), new HexId(destination.Value), copiedCandidate,
            copiedPolicy, copiedBasis, decisionIdentity, acceptedDay, planRevision, status);
    }

    private static SpatialRouteCandidate CloneCandidate(SpatialRouteCandidate candidate)
    {
        List<SpatialRouteCandidateLeg> legs = new List<SpatialRouteCandidateLeg>();
        foreach (SpatialRouteCandidateLeg leg in candidate.Legs)
        {
            SpatialRouteSegment segment = leg.Segment;
            SpatialRouteSegment copy = new SpatialRouteSegment(
                new HexBoundaryKey(new HexId(segment.Boundary.FirstHexId.Value), new HexId(segment.Boundary.SecondHexId.Value)),
                new HexId(segment.FromHexId.Value), new HexId(segment.ToHexId.Value), segment.Option);
            legs.Add(new SpatialRouteCandidateLeg(copy, leg.Belief, leg.IsBeliefStale, leg.BeliefObservationIdentity));
        }
        return new SpatialRouteCandidate(new HexId(candidate.OriginHexId.Value), new HexId(candidate.DestinationHexId.Value), legs);
    }

    private static bool Fail(PersonRoutePlanFailureCode code, string message, out PersonRoutePlanFailure failure)
    { failure = PersonRoutePlanFailure.Create(code, message); return false; }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}

public sealed class PersonRoutePlanInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;
    internal PersonRoutePlanInvariantReport(IEnumerable<string> values)
    {
        List<string> result = values == null ? new List<string>() : new List<string>(values);
        result.Sort(StringComparer.Ordinal);
        Violations = new ReadOnlyCollection<string>(result);
    }
}
