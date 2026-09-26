using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class SpatialRoutePlanningTests
{
    private const string MetricId = "civil.route.preference";
    private const string MetricUnit = "fixture.preference-units";

    [Test]
    public void SameSubjectResolutionUsesReceiptDayThenOrdinalProvenanceRegardlessOfInsertionOrder()
    {
        Fixture forward = CreateFixture("person.knowledge.forward");
        Fixture reverse = CreateFixture("person.knowledge.reverse");
        SpatialRouteSegment segment = Segment("hex.a", "hex.b", "connection.ab");
        SpatialSubject subject = SpatialSubject.ForTraversalOption(segment);
        SpatialObservation older = OptionObservation(segment, SpatialRouteOptionBelief.KnownUnavailable, "observer", "origin.old", 2L, 2L);
        SpatialObservation tieA = OptionObservation(segment, SpatialRouteOptionBelief.KnownUnavailable, "observer", "origin.aa", 4L, 4L);
        SpatialObservation tieZ = OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable, "observer", "origin.zz", 4L, 4L);

        Record(forward, older, 4L);
        Record(forward, tieA, 4L);
        Record(forward, tieZ, 4L);
        Record(reverse, tieZ, 4L);
        Record(reverse, tieA, 4L);
        Record(reverse, older, 4L);

        Assert.That(forward.Knowledge.TryGetResolvedObservation(forward.Actor, subject, 4L, null, out SpatialResolvedObservation forwardResolved), Is.True);
        Assert.That(reverse.Knowledge.TryGetResolvedObservation(reverse.Actor, subject, 4L, null, out SpatialResolvedObservation reverseResolved), Is.True);
        SpatialObservation expected = StringComparer.Ordinal.Compare(tieZ.Provenance.StableKey, tieA.Provenance.StableKey) > 0 ? tieZ : tieA;
        Assert.That(forwardResolved.Observation.StableIdentity, Is.EqualTo(expected.StableIdentity));
        Assert.That(reverseResolved.Observation.StableIdentity, Is.EqualTo(expected.StableIdentity));
        Assert.That(forwardResolved.Observation.Value.RouteOptionBelief, Is.EqualTo(expected.Value.RouteOptionBelief));
        Assert.That(forward.Knowledge.TryGetResolvedObservation(forward.Actor, subject, 3L, null, out SpatialResolvedObservation historical), Is.True);
        Assert.That(historical.Observation.StableIdentity, Is.EqualTo(older.StableIdentity));
    }

    [Test]
    public void ConflictingSameProvenanceBatchIsRejectedAtomically()
    {
        Fixture fixture = CreateFixture("person.conflict");
        SpatialRouteSegment segment = Segment("hex.a", "hex.b", "connection.ab");
        SpatialObservation first = OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable, "observer", "origin.same", 3L, 3L);
        SpatialObservation conflict = OptionObservation(segment, SpatialRouteOptionBelief.KnownUnavailable, "observer", "origin.same", 3L, 3L);

        Assert.That(fixture.Knowledge.TryRecordObservations(fixture.Actor, new[] { first, conflict }, 3L, out SpatialKnowledgeFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialKnowledgeFailureCode.ConflictingProvenance));
        Assert.That(fixture.Knowledge.Revision, Is.Zero);
        Assert.That(fixture.Knowledge.ObservationCount, Is.Zero);

        Record(fixture, first, 3L);
        long revision = fixture.Knowledge.Revision;
        Assert.That(fixture.Knowledge.TryRecordObservation(fixture.Actor, conflict, 3L, out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(SpatialKnowledgeFailureCode.ConflictingProvenance));
        Assert.That(fixture.Knowledge.Revision, Is.EqualTo(revision));
        Assert.That(fixture.Knowledge.ObservationCount, Is.EqualTo(1));
        Assert.That(fixture.Knowledge.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void KnownUnavailableBeliefExcludesFactuallyOpenOptionAndStaleFalseBeliefStaysKnowledgeOnly()
    {
        Fixture fixture = CreateFixture("person.false-belief");
        SpatialRouteSegment openFact = Segment("hex.a", "hex.b", "connection.ab");
        Assert.That(fixture.Spatial.PassageAuthority.TryGetPassageCondition(
            openFact.Boundary, openFact.Option, out PassageCondition factualCondition, out SpatialAuthorityFailure failure), Is.True, failure.ToString());
        Assert.That(factualCondition, Is.EqualTo(PassageCondition.Available));

        SpatialObservation falseUnavailable = OptionObservation(openFact, SpatialRouteOptionBelief.KnownUnavailable, "scout", "report.old", 4L, 4L);
        Record(fixture, falseUnavailable, 4L);
        SpatialRoutePlanningOutcome excluded = fixture.Planner.BuildKnownCandidates(Request(fixture.Actor, "hex.a", "hex.b", 4L));
        Assert.That(excluded.FailureCode, Is.EqualTo(SpatialRoutePlanningFailureCode.NoKnownRoute));
        Assert.That(fixture.Knowledge.TryGetResolvedObservation(fixture.Actor,
            SpatialSubject.ForTraversalOption(openFact), 6L, 1L, out SpatialResolvedObservation stale), Is.True);
        Assert.That(stale.IsStale, Is.True);
        Assert.That(stale.Observation.Value.RouteOptionBelief, Is.EqualTo(SpatialRouteOptionBelief.KnownUnavailable),
            "A stale report remains the recorded belief; resolving it does not synchronize the open passage fact.");

        SpatialObservation corrected = OptionObservation(openFact, SpatialRouteOptionBelief.KnownAvailable, "scout", "report.corrected", 6L, 6L);
        Record(fixture, corrected, 6L);
        SpatialRoutePlanningOutcome restored = fixture.Planner.BuildKnownCandidates(Request(fixture.Actor, "hex.a", "hex.b", 6L));
        Assert.That(restored.IsSuccess, Is.True, restored.FailureMessage);
        Assert.That(restored.Candidates.Count, Is.EqualTo(1));
        Assert.That(restored.Candidates[0].Segments.Single().Option, Is.EqualTo(openFact.Option));
        Assert.That(fixture.Spatial.PassageAuthority.TryGetPassageCondition(
            openFact.Boundary, openFact.Option, out factualCondition, out failure), Is.True, failure.ToString());
        Assert.That(factualCondition, Is.EqualTo(PassageCondition.Available));
    }

    [Test]
    public void CandidateGenerationUsesOnlyKnownSegmentsAndDoesNotInferGeometryOrMutateStores()
    {
        Fixture fixture = CreateFixture("person.known-routes");
        foreach (SpatialRouteSegment segment in AllRouteSegments())
            Record(fixture, OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable, "map", "route." + segment.Option.ConnectionId.Value, 10L, 10L), 10L);

        long knowledgeRevision = fixture.Knowledge.Revision;
        long spatialRevision = fixture.Spatial.Revision;
        SpatialRoutePlanningOutcome result = fixture.Planner.BuildKnownCandidates(Request(fixture.Actor, "hex.a", "hex.d", 10L));

        Assert.That(result.IsSuccess, Is.True, result.FailureMessage);
        Assert.That(result.Candidates.Count, Is.EqualTo(2), "Only the two explicitly observed route alternatives form candidates.");
        Assert.That(result.Candidates.Select(candidate => candidate.Segments.Count), Is.All.EqualTo(2));
        Assert.That(fixture.Knowledge.Revision, Is.EqualTo(knowledgeRevision));
        Assert.That(fixture.Spatial.Revision, Is.EqualTo(spatialRevision));
        Assert.That(result.Candidates.Select(candidate => candidate.SequenceKey), Is.Ordered);
        Assert.That(result.Candidates.SelectMany(candidate => candidate.Segments).All(segment => segment.Option != null), Is.True);
        Assert.That(fixture.Knowledge.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void GeometryAndReverseKnowledgeDoNotCreateAnUnobservedDirectedRoute()
    {
        Fixture fixture = CreateFixture("person.directional-knowledge");
        SpatialRoutePlanningOutcome noObservation = fixture.Planner.BuildKnownCandidates(Request(fixture.Actor, "hex.a", "hex.b", 0L));
        Assert.That(noObservation.FailureCode, Is.EqualTo(SpatialRoutePlanningFailureCode.NoKnownRoute),
            "An open factual connection and geometric adjacency do not reveal a route to this actor.");

        SpatialRouteSegment reverse = Segment("hex.b", "hex.a", "connection.ab");
        Record(fixture, OptionObservation(reverse, SpatialRouteOptionBelief.KnownAvailable, "map", "reverse-only", 0L, 0L), 0L);
        SpatialRoutePlanningOutcome forwardRequest = fixture.Planner.BuildKnownCandidates(Request(fixture.Actor, "hex.a", "hex.b", 0L));
        SpatialRoutePlanningOutcome reverseRequest = fixture.Planner.BuildKnownCandidates(Request(fixture.Actor, "hex.b", "hex.a", 0L));
        Assert.That(forwardRequest.FailureCode, Is.EqualTo(SpatialRoutePlanningFailureCode.NoKnownRoute));
        Assert.That(reverseRequest.IsSuccess, Is.True, reverseRequest.FailureMessage);
        Assert.That(reverseRequest.Candidates.Single().Segments.Single().FromHexId, Is.EqualTo(new HexId("hex.b")));
    }

    [Test]
    public void SelectorUsesExplicitActorKnownEstimatesAndStableTieBreakAcrossInsertionOrders()
    {
        Fixture forward = CreateFixture("person.selection.forward");
        Fixture reverse = CreateFixture("person.selection.reverse");
        foreach (SpatialRouteSegment segment in AllRouteSegments())
        {
            Record(forward, OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable, "map", "route." + segment.Option.ConnectionId.Value, 10L, 10L), 10L);
        }
        foreach (SpatialRouteSegment segment in AllRouteSegments().Reverse())
            Record(reverse, OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable, "map", "route." + segment.Option.ConnectionId.Value, 10L, 10L), 10L);
        SpatialRoutePlanningRequest forwardRequest = Request(forward.Actor, "hex.a", "hex.d", 10L);
        SpatialRoutePlanningRequest reverseRequest = Request(reverse.Actor, "hex.a", "hex.d", 10L);
        IReadOnlyList<SpatialRouteCandidate> forwardCandidates = forward.Planner.BuildKnownCandidates(forwardRequest).Candidates;
        IReadOnlyList<SpatialRouteCandidate> reverseCandidates = reverse.Planner.BuildKnownCandidates(reverseRequest).Candidates;
        Assert.That(forwardCandidates.Select(value => value.SequenceKey), Is.EqualTo(reverseCandidates.Select(value => value.SequenceKey)));

        for (int i = forwardCandidates.Count - 1; i >= 0; i--)
            Record(forward, EstimateObservation(forwardCandidates[i], MetricId, MetricUnit, 5m, "estimate." + i, 10L), 10L);
        for (int i = 0; i < reverseCandidates.Count; i++)
            Record(reverse, EstimateObservation(reverseCandidates[i], MetricId, MetricUnit, 5m, "estimate." + i, 10L), 10L);

        SpatialRouteSelectionPolicy policy = Policy(preferHigher: false, maxAge: 0L, requireAvailable: true);
        long beforeKnowledge = forward.Knowledge.Revision;
        long beforePlans = forward.Plans.Revision;
        SpatialRoutePlanningOutcome forwardSelection = forward.Planner.SelectKnownRoute(forwardRequest, policy);
        SpatialRoutePlanningOutcome reverseSelection = reverse.Planner.SelectKnownRoute(reverseRequest, policy);

        Assert.That(forwardSelection.IsSuccess, Is.True, forwardSelection.FailureMessage);
        Assert.That(reverseSelection.IsSuccess, Is.True, reverseSelection.FailureMessage);
        Assert.That(forwardSelection.SelectedCandidate.SequenceKey, Is.EqualTo(forwardCandidates.Min(value => value.SequenceKey)));
        Assert.That(reverseSelection.SelectedCandidate.SequenceKey, Is.EqualTo(forwardSelection.SelectedCandidate.SequenceKey));
        Assert.That(forward.Knowledge.Revision, Is.EqualTo(beforeKnowledge));
        Assert.That(forward.Plans.Revision, Is.EqualTo(beforePlans));
    }

    [Test]
    public void UnknownStatusIsDistinctFromUnavailableAndPolicyCanRequireKnownAvailability()
    {
        Fixture fixture = CreateFixture("person.unknown-route");
        SpatialRouteSegment known = Segment("hex.a", "hex.b", "connection.ab");
        Record(fixture, OptionObservation(known, SpatialRouteOptionBelief.KnownAvailable, "map", "known", 7L, 7L), 7L);
        SpatialRoutePlanningOutcome candidates = fixture.Planner.BuildKnownCandidates(Request(fixture.Actor, "hex.a", "hex.d", 7L));
        Assert.That(candidates.FailureCode, Is.EqualTo(SpatialRoutePlanningFailureCode.NoKnownRoute),
            "A single known segment does not complete a route to the destination.");

        SpatialRouteSegment final = Segment("hex.b", "hex.d", "connection.bd");
        Record(fixture, OptionObservation(final, SpatialRouteOptionBelief.Unknown, "map", "final", 7L, 7L), 7L);
        candidates = fixture.Planner.BuildKnownCandidates(Request(fixture.Actor, "hex.a", "hex.d", 7L));
        Assert.That(candidates.IsSuccess, Is.True, candidates.FailureMessage);
        Assert.That(candidates.Candidates[0].Legs.Any(leg => leg.Belief == SpatialRouteOptionBelief.Unknown), Is.True);

        SpatialRouteCandidate candidate = candidates.Candidates[0];
        Record(fixture, EstimateObservation(candidate, MetricId, MetricUnit, 1m, "estimate.unknown", 7L), 7L);
        SpatialRoutePlanningOutcome rejected = fixture.Planner.SelectKnownRoute(Request(fixture.Actor, "hex.a", "hex.d", 7L),
            Policy(preferHigher: false, maxAge: 0L, requireAvailable: true));
        Assert.That(rejected.FailureCode, Is.EqualTo(SpatialRoutePlanningFailureCode.InsufficientKnownOptionBelief));

        SpatialRoutePlanningOutcome accepted = fixture.Planner.SelectKnownRoute(Request(fixture.Actor, "hex.a", "hex.d", 7L),
            Policy(preferHigher: false, maxAge: 0L, requireAvailable: false));
        Assert.That(accepted.IsSuccess, Is.True, accepted.FailureMessage);
    }

    [Test]
    public void MissingOrStaleActorEstimateFailsWithoutTruthFallback()
    {
        Fixture fixture = CreateFixture("person.missing-estimate");
        SpatialRouteSegment segment = Segment("hex.a", "hex.b", "connection.ab");
        Record(fixture, OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable, "map", "route.single", 2L, 2L), 2L);
        SpatialRoutePlanningRequest dayTwo = Request(fixture.Actor, "hex.a", "hex.b", 2L);
        SpatialRoutePlanningOutcome noEstimate = fixture.Planner.SelectKnownRoute(dayTwo, Policy(false, 0L, true));
        Assert.That(noEstimate.FailureCode, Is.EqualTo(SpatialRoutePlanningFailureCode.InsufficientKnownEstimate));
        Assert.That(fixture.Planner.SelectKnownRoute(dayTwo, null).FailureCode, Is.EqualTo(SpatialRoutePlanningFailureCode.PolicyUnavailable));

        SpatialRouteCandidate candidate = fixture.Planner.BuildKnownCandidates(dayTwo).Candidates.Single();
        Record(fixture, EstimateObservation(candidate, MetricId, MetricUnit, 7m, "estimate.old", 2L), 2L);
        SpatialRoutePlanningOutcome fresh = fixture.Planner.SelectKnownRoute(dayTwo, Policy(false, 1L, true));
        Assert.That(fresh.IsSuccess, Is.True, fresh.FailureMessage);
        SpatialRoutePlanningOutcome stale = fixture.Planner.SelectKnownRoute(Request(fixture.Actor, "hex.a", "hex.b", 4L), Policy(false, 1L, true));
        Assert.That(stale.FailureCode, Is.EqualTo(SpatialRoutePlanningFailureCode.InsufficientKnownEstimate));
    }

    [Test]
    public void NonHexAndUnresolvedEndpointsReturnTypedFailuresWithoutAnchorSubstitution()
    {
        Fixture fixture = CreateFixture("person.endpoint-types");
        SpatialRoutePlanningOutcome location = fixture.Planner.BuildKnownCandidates(new SpatialRoutePlanningRequest(
            fixture.Actor, StablePositionReference.ForLocation(new LocationId("location.a")), StablePositionReference.ForHex(new HexId("hex.b")), 0L));
        SpatialRoutePlanningOutcome crossing = fixture.Planner.BuildKnownCandidates(new SpatialRoutePlanningRequest(
            fixture.Actor, StablePositionReference.ForCrossing(new CrossingId("crossing.ab")), StablePositionReference.ForHex(new HexId("hex.b")), 0L));
        SpatialRoutePlanningOutcome missing = fixture.Planner.BuildKnownCandidates(new SpatialRoutePlanningRequest(
            fixture.Actor, StablePositionReference.ForHex(new HexId("hex.missing")), StablePositionReference.ForHex(new HexId("hex.b")), 0L));

        Assert.That(fixture.Spatial.TryGet(new LocationId("location.a"), out LocationRecord locationRecord), Is.True);
        Assert.That(locationRecord.AnchorHexId, Is.EqualTo(new HexId("hex.a")));
        Assert.That(fixture.Spatial.TryGet(new CrossingId("crossing.ab"), out CrossingRecord crossingRecord), Is.True);
        Assert.That(crossingRecord.AnchorHexId, Is.EqualTo(new HexId("hex.a")));
        Assert.That(location.FailureCode, Is.EqualTo(SpatialRoutePlanningFailureCode.UnsupportedRouteEndpoint));
        Assert.That(crossing.FailureCode, Is.EqualTo(SpatialRoutePlanningFailureCode.UnsupportedRouteEndpoint));
        Assert.That(missing.FailureCode, Is.EqualTo(SpatialRoutePlanningFailureCode.UnresolvedHexEndpoint));
    }

    [Test]
    public void PlanAcceptanceReplacementStaleBasisAndCloneAreAtomicAndPersonKeyed()
    {
        Fixture fixture = CreateFixture("person.plan-owner");
        SpatialRouteSegment segment = Segment("hex.a", "hex.b", "connection.ab");
        Record(fixture, OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable, "map", "route.plan", 5L, 5L), 5L);
        SpatialRoutePlanningRequest request = Request(fixture.Actor, "hex.a", "hex.b", 5L);
        SpatialRouteCandidate candidate = fixture.Planner.BuildKnownCandidates(request).Candidates.Single();
        Record(fixture, EstimateObservation(candidate, MetricId, MetricUnit, 4m, "estimate.plan", 5L), 5L);
        SpatialRoutePlanningOutcome selected = fixture.Planner.SelectKnownRoute(request, Policy(false, 0L, true));
        Assert.That(selected.IsSuccess, Is.True, selected.FailureMessage);

        Assert.That(fixture.Plans.TryAcceptPlan(selected, "decision.route.1", 0L, 5L, out PersonRoutePlanFailure failure), Is.True, failure.ToString());
        Assert.That(fixture.Plans.TryGetCurrent(new PersonId(fixture.Actor.Value), out PersonRoutePlan first), Is.True);
        Assert.That(first.Status, Is.EqualTo(PersonRoutePlanStatus.Accepted));
        Assert.That(first.Candidate.Segments.Single().Option, Is.EqualTo(segment.Option));
        Assert.That(fixture.Plans.ValidateInvariants().IsValid, Is.True);

        Assert.That(fixture.Plans.TryAcceptPlan(selected, "decision.route.stale-revision", 0L, 5L, out failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PersonRoutePlanFailureCode.PlanRevisionMismatch));
        Assert.That(fixture.Plans.Revision, Is.EqualTo(1L));

        Assert.That(fixture.Plans.TryAcceptPlan(selected, "decision.route.2", 1L, 5L, out failure), Is.True, failure.ToString());
        Assert.That(fixture.Plans.History.Count, Is.EqualTo(2));
        Assert.That(fixture.Plans.History[0].Status, Is.EqualTo(PersonRoutePlanStatus.Superseded));
        Assert.That(fixture.Plans.History[1].Status, Is.EqualTo(PersonRoutePlanStatus.Accepted));

        PersonStore targetPeople = CreatePeople(fixture.Actor.Value);
        SpatialRouteKnowledgeStore targetKnowledge = CloneKnowledge(fixture.Knowledge, targetPeople);
        PersonRoutePlanStore clone = ClonePlans(fixture.Plans, targetPeople, targetKnowledge);
        Assert.That(clone.Revision, Is.EqualTo(fixture.Plans.Revision));
        Assert.That(clone.History.Select(plan => plan.StableKey), Is.EqualTo(fixture.Plans.History.Select(plan => plan.StableKey)));
        Assert.That(clone.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void StaleKnowledgeBasisAndFaultedMutationGuardRejectPlanWithoutMutation()
    {
        Fixture fixture = CreateFixture("person.plan.stale");
        SpatialRouteSegment segment = Segment("hex.a", "hex.b", "connection.ab");
        Record(fixture, OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable, "map", "route.stale", 3L, 3L), 3L);
        SpatialRoutePlanningRequest request = Request(fixture.Actor, "hex.a", "hex.b", 3L);
        SpatialRouteCandidate candidate = fixture.Planner.BuildKnownCandidates(request).Candidates.Single();
        Record(fixture, EstimateObservation(candidate, MetricId, MetricUnit, 1m, "estimate.stale", 3L), 3L);
        SpatialRoutePlanningOutcome selected = fixture.Planner.SelectKnownRoute(request, Policy(false, 0L, true));
        Record(fixture, OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable, "new-map", "route.new-info", 3L, 3L), 3L);

        Assert.That(fixture.Plans.TryAcceptPlan(selected, "decision.stale", 0L, 3L, out PersonRoutePlanFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PersonRoutePlanFailureCode.StaleKnowledgeBasis));
        Assert.That(fixture.Plans.Revision, Is.Zero);
        Assert.That(fixture.Plans.PlanCount, Is.Zero);

        object guard = CreateFaultedMutationGuard();
        Assert.That(BindGuard(fixture.Knowledge, guard), Is.True);
        Assert.That(fixture.Knowledge.TryRecordObservation(fixture.Actor,
            OptionObservation(Segment("hex.a", "hex.c", "connection.ac"), SpatialRouteOptionBelief.KnownAvailable, "map", "after-fault", 4L, 4L),
            4L, out SpatialKnowledgeFailure knowledgeFailure), Is.False);
        Assert.That(knowledgeFailure.Code, Is.EqualTo(SpatialKnowledgeFailureCode.RuntimeFaulted));
    }

    [Test]
    public void RuntimeClonesAndComposesP8DKnowledgePlannerAndPlansWithDiagnostics()
    {
        Fixture source = CreateFixture("person.runtime-route");
        SpatialRouteSegment segment = Segment("hex.a", "hex.b", "connection.ab");
        Record(source, OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable, "map", "runtime-route", 0L, 0L), 0L);
        SpatialRouteCandidate candidate = source.Planner.BuildKnownCandidates(
            Request(source.Actor, "hex.a", "hex.b", 0L)).Candidates.Single();
        Record(source, EstimateObservation(candidate, MetricId, MetricUnit, 3m, "runtime-estimate", 0L), 0L);

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), null, null,
            personStore: source.People,
            spatialAuthorityStore: source.Spatial,
            spatialRouteKnowledgeStore: source.Knowledge,
            personRoutePlanStore: source.Plans);

        Assert.That(runtime.SpatialAuthorityStore, Is.Not.SameAs(source.Spatial));
        Assert.That(runtime.SpatialRouteKnowledgeStore, Is.Not.SameAs(source.Knowledge));
        Assert.That(runtime.PersonRoutePlanStore, Is.Not.SameAs(source.Plans));
        Assert.That(runtime.SpatialRouteKnowledgeStore.GetObservations(source.Actor).Count, Is.EqualTo(2));
        Assert.That(runtime.ValidateSpatialInvariants().IsValid, Is.True,
            string.Join("; ", runtime.ValidateSpatialInvariants().Violations));

        object secondGuard = CreateFaultedMutationGuard();
        Assert.That(BindGuard(runtime.SpatialRouteKnowledgeStore, secondGuard), Is.False,
            "Runtime-owned Knowledge must already be bound to the runtime mutation guard.");
        Assert.That(BindGuard(runtime.PersonRoutePlanStore, secondGuard), Is.False,
            "Runtime-owned route plans must already be bound to the runtime mutation guard.");

        SpatialRoutePlanningOutcome selected = runtime.SelectKnownSpatialRoute(
            source.Actor,
            StablePositionReference.ForHex(new HexId("hex.a")),
            StablePositionReference.ForHex(new HexId("hex.b")),
            Policy(preferHigher: false, maxAge: 0L, requireAvailable: true));
        Assert.That(selected.IsSuccess, Is.True, selected.FailureMessage);
        Assert.That(runtime.TryAcceptSpatialRoutePlan(selected, "decision.runtime-route", 0L,
            out PersonRoutePlanFailure planFailure), Is.True, planFailure.ToString());
        Assert.That(runtime.PersonRoutePlanStore.History.Single().AcceptedDay, Is.EqualTo(runtime.CurrentDay));

        WorldStateSnapshotContext context = new WorldStateSnapshotContext(
            simulationTime: runtime.SimulationTime,
            personStore: runtime.PersonStore,
            spatialAuthorityStore: runtime.SpatialAuthorityStore,
            legacySpatialAnchorBindingStore: runtime.LegacySpatialAnchorBindingStore,
            personSpatialPositionStore: runtime.PersonSpatialPositionStore,
            spatialRouteKnowledgeStore: runtime.SpatialRouteKnowledgeStore,
            personRoutePlanStore: runtime.PersonRoutePlanStore);
        WorldStateSnapshot before = WorldStateSnapshotBuilder.Capture(context);
        Assert.That(before.Spatial.SpatialRouteObservations.Count, Is.EqualTo(2));
        Assert.That(before.Spatial.PersonRoutePlans.Count, Is.EqualTo(1));
        Assert.That(WorldStateInvariantValidator.Validate(before).IsValid, Is.True,
            string.Join("; ", WorldStateInvariantValidator.Validate(before).Errors));
        string canonicalBefore = WorldStateCanonicalWriter.Write(before);
        Assert.That(canonicalBefore, Does.Contain("SPATIAL_ROUTE_OBSERVATION"));
        Assert.That(canonicalBefore, Does.Contain("PERSON_ROUTE_PLAN_LEG"));
        Assert.That(WorldStateSnapshotFormatter.Format(before), Does.Contain("ROUTE PLAN"));

        SpatialObservation laterFalseBelief = OptionObservation(
            segment, SpatialRouteOptionBelief.KnownUnavailable, "traveler", "runtime-unavailable-report", 0L, 0L);
        Assert.That(runtime.TryRecordSpatialObservations(source.Actor, new[] { laterFalseBelief },
            out SpatialKnowledgeFailure knowledgeFailure), Is.True, knowledgeFailure.ToString());
        WorldStateSnapshot after = WorldStateSnapshotBuilder.Capture(context);
        Assert.That(after.Spatial.SpatialRouteObservations.Count, Is.EqualTo(3));
        Assert.That(WorldStateInvariantValidator.Validate(after).IsValid, Is.True,
            string.Join("; ", WorldStateInvariantValidator.Validate(after).Errors));
        WorldStateDiff diff = WorldStateDiff.Compare(before, after);
        Assert.That(diff.IsEmpty, Is.False);
        Assert.That(diff.Differences.Any(value => value.Section == "SpatialRouteObservation"), Is.True);
        Assert.That(WorldStateCanonicalWriter.Write(after), Does.Contain("KnownUnavailable"));
        Assert.That(runtime.ValidateSpatialInvariants().IsValid, Is.True,
            string.Join("; ", runtime.ValidateSpatialInvariants().Violations));
    }

    [Test]
    public void CivilTravelCommitsDepartureProgressAndArrivalWithDistinctPlanAndMutationRevisions()
    {
        Fixture source = CreateFixture("person.p8e-journey");
        SpatialRouteSegment segment = Segment("hex.a", "hex.b", "connection.ab");
        Record(source, OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable,
            "guide", "journey.option", 0L, 0L), 0L);
        SpatialRouteCandidate candidate = source.Planner.BuildKnownCandidates(
            Request(source.Actor, "hex.a", "hex.b", 0L)).Candidates.Single();
        Record(source, EstimateObservation(candidate, MetricId, MetricUnit, 1m, "journey.estimate", 0L), 0L);
        SpatialRouteSegment reverseSegment = Segment("hex.b", "hex.a", "connection.ab");
        Record(source, OptionObservation(reverseSegment, SpatialRouteOptionBelief.KnownAvailable,
            "guide", "journey.reverse-option", 0L, 0L), 0L);
        SpatialRouteCandidate reverseCandidate = source.Planner.BuildKnownCandidates(
            Request(source.Actor, "hex.b", "hex.a", 0L)).Candidates.Single();
        Record(source, EstimateObservation(reverseCandidate, MetricId, MetricUnit, 1m, "journey.reverse-estimate", 0L), 0L);

        PersonSpatialPositionStore positions = new PersonSpatialPositionStore(source.People, source.Spatial,
            new TestTraversalResolver(source.Spatial.PassageAuthority));
        Assert.That(positions.TrySetAt(source.Actor, StablePositionReference.ForHex(new HexId("hex.a")),
            out PersonSpatialPositionFailure positionFailure), Is.True, positionFailure.ToString());
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null,
            personStore: source.People,
            spatialAuthorityStore: source.Spatial,
            personSpatialPositionStore: positions,
            spatialRouteKnowledgeStore: source.Knowledge);
        SpatialRoutePlanningOutcome selected = runtime.SelectKnownSpatialRoute(source.Actor,
            StablePositionReference.ForHex(new HexId("hex.a")), StablePositionReference.ForHex(new HexId("hex.b")),
            Policy(false, 0L, true));
        Assert.That(selected.IsSuccess, Is.True, selected.FailureMessage);
        Assert.That(runtime.TryAcceptSpatialRoutePlan(selected, "decision.p8e-journey", 0L,
            out PersonRoutePlanFailure planFailure), Is.True, planFailure.ToString());
        Assert.That(runtime.PersonRoutePlanStore.TryGetCurrent(source.Actor, out PersonRoutePlan accepted), Is.True);
        Assert.That(accepted.Status, Is.EqualTo(PersonRoutePlanStatus.Accepted));

        TraversalCostContext context = new TraversalCostContext("civil.profile", "v1", 1m, 1m, 1m);
        Assert.That(runtime.P8ETravelTransactionCoordinator.TryBeginNextSegment(source.Actor, context,
            out P8ETravelFailure travelFailure), Is.True, travelFailure.Message);
        Assert.That(runtime.PersonRoutePlanStore.TryGetCurrent(source.Actor, out PersonRoutePlan active), Is.True);
        Assert.That(active.Status, Is.EqualTo(PersonRoutePlanStatus.Active));
        Assert.That(runtime.PersonRoutePlanStore.Revision, Is.EqualTo(2L));
        Assert.That(runtime.PersonSpatialPositionStore.TryGetPosition(source.Actor, out PersonSpatialPosition transit), Is.True);
        Assert.That(transit.IsInTransit, Is.True);
        long inTransitPositionRevision = runtime.PersonSpatialPositionStore.Revision;
        long inTransitPlanStoreRevision = runtime.PersonRoutePlanStore.Revision;
        Assert.That(runtime.PersonRoutePlanStore.TryAcceptPlan(selected, "decision.illegal-in-transit", 1L,
            runtime.CurrentDay, out PersonRoutePlanFailure transitPlanFailure), Is.False);
        Assert.That(transitPlanFailure.Code, Is.EqualTo(PersonRoutePlanFailureCode.ActorInTransit));
        Assert.That(runtime.PersonSpatialPositionStore.Revision, Is.EqualTo(inTransitPositionRevision));
        Assert.That(runtime.PersonRoutePlanStore.Revision, Is.EqualTo(inTransitPlanStoreRevision));
        Assert.That(runtime.PersonRoutePlanStore.TryGetCurrent(source.Actor, out PersonRoutePlan stillActive), Is.True);
        Assert.That(stillActive.Status, Is.EqualTo(PersonRoutePlanStatus.Active));
        Assert.That(runtime.PersonRoutePlanStore.History.Count, Is.EqualTo(1));

        Assert.That(runtime.P8ETravelTransactionCoordinator.TryAdvanceSegment(source.Actor,
            TraversalProgress.CompleteProgressTicks, out travelFailure), Is.True, travelFailure.Message);
        Assert.That(runtime.P8ETravelTransactionCoordinator.TryArriveAtFinalDestination(source.Actor,
            out travelFailure), Is.True, travelFailure.Message);
        Assert.That(runtime.PersonSpatialPositionStore.TryGetPosition(source.Actor, out PersonSpatialPosition arrived), Is.True);
        Assert.That(arrived.Position.HexId, Is.EqualTo(new HexId("hex.b")));
        Assert.That(runtime.PersonRoutePlanStore.TryGetCurrent(source.Actor, out PersonRoutePlan completed), Is.True);
        Assert.That(completed.Status, Is.EqualTo(PersonRoutePlanStatus.Completed));
        Assert.That(runtime.PersonRoutePlanStore.Revision, Is.EqualTo(3L));
        Assert.That(runtime.PersonRoutePlanStore.PlanCount, Is.EqualTo(1));

        SpatialRoutePlanningOutcome stableReplan = runtime.SelectKnownSpatialRoute(source.Actor,
            StablePositionReference.ForHex(new HexId("hex.b")), StablePositionReference.ForHex(new HexId("hex.a")),
            Policy(false, 0L, true));
        Assert.That(stableReplan.IsSuccess, Is.True, stableReplan.FailureMessage);
        Assert.That(runtime.PersonRoutePlanStore.TryAcceptPlan(stableReplan, "decision.stable-replan", 1L,
            runtime.CurrentDay, out PersonRoutePlanFailure replanFailure), Is.True, replanFailure.ToString());
        Assert.That(runtime.PersonRoutePlanStore.TryGetCurrent(source.Actor, out PersonRoutePlan replanned), Is.True);
        Assert.That(replanned.Status, Is.EqualTo(PersonRoutePlanStatus.Accepted));
        Assert.That(runtime.PersonRoutePlanStore.History[0].Status, Is.EqualTo(PersonRoutePlanStatus.Completed));
        Assert.That(runtime.ValidateSpatialInvariants().IsValid, Is.True,
            string.Join("; ", runtime.ValidateSpatialInvariants().Violations));

        WorldStateSnapshot snapshot = WorldStateSnapshotBuilder.Capture(new WorldStateSnapshotContext(
            simulationTime: runtime.SimulationTime,
            personStore: runtime.PersonStore,
            spatialAuthorityStore: runtime.SpatialAuthorityStore,
            personSpatialPositionStore: runtime.PersonSpatialPositionStore,
            spatialRouteKnowledgeStore: runtime.SpatialRouteKnowledgeStore,
            personRoutePlanStore: runtime.PersonRoutePlanStore));
        Assert.That(WorldStateInvariantValidator.Validate(snapshot).IsValid, Is.True,
            string.Join("; ", WorldStateInvariantValidator.Validate(snapshot).Errors));
    }

    [Test]
    public void RuntimeRejectsSelectionFromAnotherWorldDayThroughFacadeAndExposedPlanStore()
    {
        Fixture source = CreateFixture("person.runtime-stale-day");
        SpatialRoutePlanningOutcome selectionMadeOnDayZero = PrepareSelectableRoute(source, 0L, "stale-day");
        Assert.That(selectionMadeOnDayZero.IsSuccess, Is.True, selectionMadeOnDayZero.FailureMessage);

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(1L), null, null,
            personStore: source.People,
            spatialAuthorityStore: source.Spatial,
            spatialRouteKnowledgeStore: source.Knowledge,
            personRoutePlanStore: source.Plans);
        long planRevision = runtime.PersonRoutePlanStore.Revision;
        int planCount = runtime.PersonRoutePlanStore.PlanCount;

        Assert.That(runtime.TryAcceptSpatialRoutePlan(selectionMadeOnDayZero, "decision.stale-day.facade", 0L,
            out PersonRoutePlanFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(PersonRoutePlanFailureCode.StalePlanningDay));
        Assert.That(runtime.PersonRoutePlanStore.TryAcceptPlan(selectionMadeOnDayZero,
            "decision.stale-day.direct", 0L, 0L, out failure), Is.False,
            "Calling the runtime-owned store directly cannot supply an obsolete world day.");
        Assert.That(failure.Code, Is.EqualTo(PersonRoutePlanFailureCode.StalePlanningDay));
        Assert.That(runtime.PersonRoutePlanStore.Revision, Is.EqualTo(planRevision));
        Assert.That(runtime.PersonRoutePlanStore.PlanCount, Is.EqualTo(planCount));
        Assert.That(runtime.ValidateSpatialInvariants().IsValid, Is.True,
            string.Join("; ", runtime.ValidateSpatialInvariants().Violations));
    }

    [TestCase(2L, 2L)]
    [TestCase(0L, 2L)]
    public void RuntimeRejectsDetachedSpatialKnowledgeDatedAfterTargetWorldDay(
        long observedDay,
        long receivedDay)
    {
        Fixture source = CreateFixture("person.runtime-future-knowledge." + observedDay + "." + receivedDay);
        Record(source, OptionObservation(
            Segment("hex.a", "hex.b", "connection.ab"),
            SpatialRouteOptionBelief.KnownAvailable,
            "map.future",
            "future-knowledge." + observedDay + "." + receivedDay,
            observedDay,
            receivedDay),
            receivedDay);

        ArgumentException failure = Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(1L), null, null,
            personStore: source.People,
            spatialAuthorityStore: source.Spatial,
            spatialRouteKnowledgeStore: source.Knowledge));

        Assert.That(failure.Message, Does.Contain("later than the target SimulationTime.AbsoluteDay"));
    }

    [Test]
    public void RuntimeRejectsDetachedRoutePlanAcceptedAfterTargetWorldDay()
    {
        Fixture source = CreateFixture("person.runtime-future-plan");
        SpatialRouteSegment segment = Segment("hex.a", "hex.b", "connection.ab");
        Record(source, OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable,
            "map", "future-plan.route", 0L, 0L), 0L);
        SpatialRouteCandidate candidate = source.Planner.BuildKnownCandidates(
            Request(source.Actor, "hex.a", "hex.b", 0L)).Candidates.Single();
        Record(source, EstimateObservation(candidate, MetricId, MetricUnit, 2m,
            "future-plan.estimate", 0L), 0L);
        SpatialRoutePlanningOutcome selectedForDayTwo = source.Planner.SelectKnownRoute(
            Request(source.Actor, "hex.a", "hex.b", 2L),
            Policy(preferHigher: false, maxAge: 2L, requireAvailable: true));
        Assert.That(selectedForDayTwo.IsSuccess, Is.True, selectedForDayTwo.FailureMessage);
        Assert.That(source.Plans.TryAcceptPlan(selectedForDayTwo, "decision.future-plan", 0L, 2L,
            out PersonRoutePlanFailure planFailure), Is.True, planFailure.ToString());

        ArgumentException failure = Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(1L), null, null,
            personStore: source.People,
            spatialAuthorityStore: source.Spatial,
            spatialRouteKnowledgeStore: source.Knowledge,
            personRoutePlanStore: source.Plans));

        Assert.That(failure.Message, Does.Contain("accepted later than the target SimulationTime.AbsoluteDay"));
    }

    [Test]
    public void RuntimeAcceptsPastDatedKnowledgeAndPlansAndReportsFutureOwnedKnowledge()
    {
        Fixture source = CreateFixture("person.runtime-past-state");
        SpatialRoutePlanningOutcome selectedOnDayZero = PrepareSelectableRoute(source, 0L, "past-state");
        Assert.That(source.Plans.TryAcceptPlan(selectedOnDayZero, "decision.past-plan", 0L, 0L,
            out PersonRoutePlanFailure planFailure), Is.True, planFailure.ToString());

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(1L), null, null,
            personStore: source.People,
            spatialAuthorityStore: source.Spatial,
            spatialRouteKnowledgeStore: source.Knowledge,
            personRoutePlanStore: source.Plans);

        Assert.That(runtime.SpatialRouteKnowledgeStore.ObservationCount, Is.EqualTo(2));
        Assert.That(runtime.PersonRoutePlanStore.History.Single().AcceptedDay, Is.EqualTo(0L));
        Assert.That(runtime.ValidateSpatialInvariants().IsValid, Is.True,
            string.Join("; ", runtime.ValidateSpatialInvariants().Violations));

        SpatialObservation futureObservation = OptionObservation(
            Segment("hex.a", "hex.b", "connection.ab"),
            SpatialRouteOptionBelief.KnownUnavailable,
            "later-map",
            "runtime-future-observation",
            2L,
            2L);
        Assert.That(runtime.SpatialRouteKnowledgeStore.TryRecordObservation(
            source.Actor, futureObservation, 2L, out SpatialKnowledgeFailure knowledgeFailure),
            Is.True, knowledgeFailure.ToString());

        SimulationRuntimeSpatialInvariantReport report = runtime.ValidateSpatialInvariants();
        Assert.That(report.IsValid, Is.False);
        Assert.That(report.Violations.Any(value => value.Contains("observed after current SimulationTime.AbsoluteDay")), Is.True);
        Assert.That(report.Violations.Any(value => value.Contains("received after current SimulationTime.AbsoluteDay")), Is.True);
    }

    private static SpatialRoutePlanningOutcome PrepareSelectableRoute(Fixture fixture, long day, string identity)
    {
        SpatialRouteSegment segment = Segment("hex.a", "hex.b", "connection.ab");
        Record(fixture, OptionObservation(segment, SpatialRouteOptionBelief.KnownAvailable,
            "map", identity + ".route", day, day), day);
        SpatialRoutePlanningRequest request = Request(fixture.Actor, "hex.a", "hex.b", day);
        SpatialRouteCandidate candidate = fixture.Planner.BuildKnownCandidates(request).Candidates.Single();
        Record(fixture, EstimateObservation(candidate, MetricId, MetricUnit, 2m,
            identity + ".estimate", day), day);
        return fixture.Planner.SelectKnownRoute(request, Policy(false, 0L, true));
    }

    private static Fixture CreateFixture(string actorId)
    {
        PersonStore people = CreatePeople(actorId);
        SpatialAuthorityStore spatial = CreateWorld();
        SpatialRouteKnowledgeStore knowledge = new SpatialRouteKnowledgeStore(people);
        SpatialRoutePlanningSystem planner = new SpatialRoutePlanningSystem(people, spatial, knowledge);
        PersonRoutePlanStore plans = new PersonRoutePlanStore(people, knowledge);
        return new Fixture(new PersonId(actorId), people, spatial, knowledge, planner, plans);
    }

    private static PersonStore CreatePeople(string actorId)
    {
        PersonStore people = new PersonStore();
        Assert.That(people.TryRegister(new PersonRuntime(new PersonId(actorId)), out PersonStoreFailure failure), Is.True, failure.ToString());
        return people;
    }

    private static SpatialAuthorityStore CreateWorld()
    {
        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext("fixture.scale", "fixture", "v1", 1m, "hex-step"),
            new[]
            {
                GeographicHex("hex.a", 0, 0), GeographicHex("hex.b", 1, 0),
                GeographicHex("hex.c", 0, 1), GeographicHex("hex.d", 1, 1)
            },
            new[] { new LocationRecord(new LocationId("location.a"), new HexId("hex.a")) });
        Assert.That(spatial.TryComposeGeography(geography, out SpatialAuthorityFailure failure), Is.True, failure.ToString());

        RegisterFact(spatial, "hex.a", "hex.b", "connection.ab");
        RegisterFact(spatial, "hex.b", "hex.d", "connection.bd");
        RegisterFact(spatial, "hex.a", "hex.c", "connection.ac");
        RegisterFact(spatial, "hex.c", "hex.d", "connection.cd");
        Assert.That(spatial.TryRegisterCrossing(new CrossingRecord(new CrossingId("crossing.ab"),
            new HexBoundaryKey(new HexId("hex.a"), new HexId("hex.b")), new HexId("hex.a"),
            "content.bridge", "bridge.v1", null), out failure), Is.True, failure.ToString());
        return spatial;
    }

    private static void RegisterFact(SpatialAuthorityStore spatial, string from, string to, string optionId)
    {
        HexId fromId = new HexId(from);
        HexId toId = new HexId(to);
        HexBoundaryKey boundary = new HexBoundaryKey(fromId, toId);
        TraversalOptionRef option = TraversalOptionRef.ForConnection(new ConnectionId(optionId));
        Assert.That(spatial.PassageAuthority.TryRegisterConnection(
            new PassageOptionRecord(option, boundary, "content." + optionId, "v1"), PassageCondition.Available,
            out SpatialAuthorityFailure failure), Is.True, failure.ToString());
    }

    private static IEnumerable<SpatialRouteSegment> AllRouteSegments()
    {
        yield return Segment("hex.a", "hex.b", "connection.ab");
        yield return Segment("hex.b", "hex.d", "connection.bd");
        yield return Segment("hex.a", "hex.c", "connection.ac");
        yield return Segment("hex.c", "hex.d", "connection.cd");
    }

    private static SpatialRouteSegment Segment(string from, string to, string optionId)
    {
        HexId fromId = new HexId(from);
        HexId toId = new HexId(to);
        return new SpatialRouteSegment(new HexBoundaryKey(fromId, toId), fromId, toId,
            TraversalOptionRef.ForConnection(new ConnectionId(optionId)));
    }

    private static SpatialObservation OptionObservation(
        SpatialRouteSegment segment,
        SpatialRouteOptionBelief belief,
        string source,
        string origin,
        long observedDay,
        long receivedDay) => new SpatialObservation(
            SpatialSubject.ForTraversalOption(segment),
            SpatialObservationValue.ForRouteOptionBelief(belief),
            new SpatialObservationProvenance(SpatialObservationSourceKind.ExternalReport, source, origin),
            observedDay, receivedDay, 850, "regional-route-report-v1");

    private static SpatialObservation EstimateObservation(
        SpatialRouteCandidate candidate,
        string metricId,
        string unit,
        decimal estimate,
        string origin,
        long day) => new SpatialObservation(
            SpatialSubject.ForRouteEstimate(candidate.Id, metricId),
            SpatialObservationValue.ForEstimate(estimate, unit),
            new SpatialObservationProvenance(SpatialObservationSourceKind.InitialScenarioKnowledge, "scenario", origin),
            day, day, 1000, "authored-estimate-v1");

    private static SpatialRoutePlanningRequest Request(PersonId actor, string origin, string destination, long day) =>
        new SpatialRoutePlanningRequest(actor,
            StablePositionReference.ForHex(new HexId(origin)),
            StablePositionReference.ForHex(new HexId(destination)), day);

    private static SpatialRouteSelectionPolicy Policy(bool preferHigher, long maxAge, bool requireAvailable) =>
        new SpatialRouteSelectionPolicy("policy.fixture.actor-preference", "v1", MetricId, MetricUnit,
            preferHigher, maxAge, requireAvailable);

    private static void Record(Fixture fixture, SpatialObservation observation, long day)
    {
        Assert.That(fixture.Knowledge.TryRecordObservation(fixture.Actor, observation, day, out SpatialKnowledgeFailure failure), Is.True, failure.ToString());
    }

    private static HexRecord GeographicHex(string id, int q, int r) => new HexRecord(new HexId(id), new HexCoordinate(q, r),
        new TerrainReference(new TerrainDefinitionId("terrain.fixture"), "v1"));

    private static SpatialRouteKnowledgeStore CloneKnowledge(SpatialRouteKnowledgeStore source, PersonStore people)
    {
        MethodInfo method = typeof(SpatialRouteKnowledgeStore).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        return (SpatialRouteKnowledgeStore)method.Invoke(source, new object[] { people });
    }

    private static PersonRoutePlanStore ClonePlans(PersonRoutePlanStore source, PersonStore people, SpatialRouteKnowledgeStore knowledge)
    {
        MethodInfo method = typeof(PersonRoutePlanStore).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        return (PersonRoutePlanStore)method.Invoke(source, new object[] { people, knowledge });
    }

    private static object CreateFaultedMutationGuard()
    {
        Type type = typeof(PersonStore).Assembly.GetType("AuthoritativeMutationGuard");
        Assert.That(type, Is.Not.Null);
        object guard = Activator.CreateInstance(type, true);
        MethodInfo markFaulted = type.GetMethod("MarkFaulted", BindingFlags.Instance | BindingFlags.NonPublic);
        Type reasonType = markFaulted.GetParameters()[0].ParameterType;
        object reason = Enum.Parse(reasonType, "IntegrityRestoreFailed");
        markFaulted.Invoke(guard, new[] { reason });
        return guard;
    }

    private static bool BindGuard(object store, object guard)
    {
        MethodInfo method = store.GetType().GetMethod("TryBindMutationGuard", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        return (bool)method.Invoke(store, new object[] { guard });
    }

    private sealed class Fixture
    {
        public PersonId Actor { get; }
        public PersonStore People { get; }
        public SpatialAuthorityStore Spatial { get; }
        public SpatialRouteKnowledgeStore Knowledge { get; }
        public SpatialRoutePlanningSystem Planner { get; }
        public PersonRoutePlanStore Plans { get; }
        public Fixture(PersonId actor, PersonStore people, SpatialAuthorityStore spatial,
            SpatialRouteKnowledgeStore knowledge, SpatialRoutePlanningSystem planner, PersonRoutePlanStore plans)
        { Actor = actor; People = people; Spatial = spatial; Knowledge = knowledge; Planner = planner; Plans = plans; }
    }

    private sealed class TestTraversalResolver : ISpatialTraversalOptionResolver
    {
        private readonly SpatialPassageAuthority passage;
        public TestTraversalResolver(SpatialPassageAuthority passage) { this.passage = passage; }
        public bool TryResolveTraversalOption(TraversalOptionRef option, HexBoundaryKey boundary, out string failure)
        {
            if (passage.TryGetTraversalOptions(boundary, out IReadOnlyList<TraversalOptionRef> options,
                out SpatialAuthorityFailure authorityFailure))
            {
                foreach (TraversalOptionRef candidate in options)
                    if (candidate.Equals(option)) { failure = string.Empty; return true; }
                failure = "Traversal option is not registered for the boundary.";
                return false;
            }
            failure = authorityFailure?.Message ?? "Boundary traversal options could not be read.";
            return false;
        }
    }
}
