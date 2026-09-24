using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// PersonId-keyed spatial observations. This store records explicit reports;
/// it never reads passage truth to create or repair an actor belief.
/// </summary>
public sealed class SpatialRouteKnowledgeStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly PersonStore personStore;
    private readonly Dictionary<string, List<SpatialObservation>> observationsByActor = new Dictionary<string, List<SpatialObservation>>(StringComparer.Ordinal);
    private readonly Dictionary<string, long> actorRevisions = new Dictionary<string, long>(StringComparer.Ordinal);
    private long revision;

    public SpatialRouteKnowledgeStore(PersonStore personStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
    }

    public long Revision => revision;
    public int ObservationCount
    {
        get
        {
            int count = 0;
            foreach (List<SpatialObservation> observations in observationsByActor.Values) count += observations.Count;
            return count;
        }
    }

    public long GetActorRevision(PersonId actor)
    {
        return actor != null && actorRevisions.TryGetValue(actor.Value, out long value) ? value : 0L;
    }

    public bool TryRecordObservation(
        PersonId actor,
        SpatialObservation observation,
        long currentWorldDay,
        out SpatialKnowledgeFailure failure)
    {
        return TryRecordObservations(actor, observation == null ? null : new[] { observation }, currentWorldDay, out failure);
    }

    /// <summary>Records an explicit batch atomically; a provenance collision commits nothing.</summary>
    public bool TryRecordObservations(
        PersonId actor,
        IEnumerable<SpatialObservation> observations,
        long currentWorldDay,
        out SpatialKnowledgeFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
            return Fail(SpatialKnowledgeFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        if (actor == null || !personStore.TryGet(actor, out _))
            return Fail(SpatialKnowledgeFailureCode.ActorNotRegistered, "Actor PersonId is not registered.", out failure);
        if (currentWorldDay < 0L || observations == null)
            return Fail(SpatialKnowledgeFailureCode.InvalidObservation, "A non-negative world day and observation batch are required.", out failure);

        List<SpatialObservation> pending = new List<SpatialObservation>();
        foreach (SpatialObservation observation in observations)
        {
            if (!ValidateObservation(observation, currentWorldDay, out failure)) return false;
            pending.Add(observation);
        }
        if (pending.Count == 0)
            return Fail(SpatialKnowledgeFailureCode.InvalidObservation, "At least one explicit spatial observation is required.", out failure);

        observationsByActor.TryGetValue(actor.Value, out List<SpatialObservation> existing);
        List<SpatialObservation> accepted = new List<SpatialObservation>();
        Dictionary<string, SpatialObservation> pendingByIdentity = new Dictionary<string, SpatialObservation>(StringComparer.Ordinal);
        foreach (SpatialObservation incoming in pending)
        {
            if (existing != null)
            {
                foreach (SpatialObservation prior in existing)
                {
                    if (SameSubjectAndProvenance(prior, incoming) && !prior.HasSameOriginEvidence(incoming))
                        return Fail(SpatialKnowledgeFailureCode.ConflictingProvenance,
                            "Conflicting values from one stable provenance for the same subject are rejected atomically.", out failure);
                }
            }

            foreach (SpatialObservation prior in pending)
            {
                if (ReferenceEquals(prior, incoming)) break;
                if (SameSubjectAndProvenance(prior, incoming) && !prior.HasSameOriginEvidence(incoming))
                    return Fail(SpatialKnowledgeFailureCode.ConflictingProvenance,
                        "Conflicting values from one stable provenance for the same subject are rejected atomically.", out failure);
            }

            if (pendingByIdentity.TryGetValue(incoming.StableIdentity, out SpatialObservation samePending))
            {
                if (!samePending.HasSameOriginEvidence(incoming) || samePending.ReceivedDay != incoming.ReceivedDay)
                    return Fail(SpatialKnowledgeFailureCode.ConflictingProvenance, "Observation identity collision rejected atomically.", out failure);
                continue;
            }
            if (existing != null && ContainsIdentity(existing, incoming.StableIdentity)) continue;

            pendingByIdentity.Add(incoming.StableIdentity, incoming);
            accepted.Add(incoming);
        }

        if (accepted.Count == 0)
        {
            failure = SpatialKnowledgeFailure.None;
            return true;
        }
        if (revision == long.MaxValue || GetActorRevision(actor) == long.MaxValue)
            return Fail(SpatialKnowledgeFailureCode.RevisionOverflow, "Spatial Knowledge revision cannot advance.", out failure);

        if (existing == null)
        {
            existing = new List<SpatialObservation>();
            observationsByActor.Add(actor.Value, existing);
        }
        foreach (SpatialObservation observation in accepted) existing.Add(observation.Clone());
        revision++;
        actorRevisions[actor.Value] = GetActorRevision(actor) + 1L;
        failure = SpatialKnowledgeFailure.None;
        return true;
    }

    public IReadOnlyList<SpatialObservation> GetObservations(PersonId actor)
    {
        if (actor == null || !observationsByActor.TryGetValue(actor.Value, out List<SpatialObservation> values))
            return new ReadOnlyCollection<SpatialObservation>(new List<SpatialObservation>());
        List<SpatialObservation> result = CloneAndSort(values);
        return new ReadOnlyCollection<SpatialObservation>(result);
    }

    public IReadOnlyList<SpatialObservation> GetObservations(PersonId actor, SpatialSubject subject)
    {
        List<SpatialObservation> result = new List<SpatialObservation>();
        if (actor == null || subject == null || !observationsByActor.TryGetValue(actor.Value, out List<SpatialObservation> values))
            return new ReadOnlyCollection<SpatialObservation>(result);
        foreach (SpatialObservation observation in values)
            if (observation.Subject.Equals(subject)) result.Add(observation.Clone());
        result.Sort(CompareEvidenceOrder);
        return new ReadOnlyCollection<SpatialObservation>(result);
    }

    public IReadOnlyList<SpatialSubject> GetSubjects(PersonId actor, SpatialSubjectKind kind)
    {
        List<SpatialSubject> result = new List<SpatialSubject>();
        if (actor == null || !observationsByActor.TryGetValue(actor.Value, out List<SpatialObservation> values))
            return new ReadOnlyCollection<SpatialSubject>(result);
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (SpatialObservation observation in values)
            if (observation.Subject.Kind == kind && seen.Add(observation.Subject.StableKey)) result.Add(CloneSubject(observation.Subject));
        result.Sort((left, right) => left.CompareTo(right));
        return new ReadOnlyCollection<SpatialSubject>(result);
    }

    /// <summary>
    /// Resolves actor belief as of an explicit day. Freshness is a read policy;
    /// old reports remain stored and are never refreshed from World Truth.
    /// </summary>
    public bool TryGetResolvedObservation(
        PersonId actor,
        SpatialSubject subject,
        long asOfDay,
        long? maximumAgeDays,
        out SpatialResolvedObservation resolved)
    {
        resolved = null;
        if (actor == null || subject == null || asOfDay < 0L || (maximumAgeDays.HasValue && maximumAgeDays.Value < 0L)
            || !observationsByActor.TryGetValue(actor.Value, out List<SpatialObservation> values)) return false;

        SpatialObservation winner = null;
        foreach (SpatialObservation candidate in values)
        {
            if (!candidate.Subject.Equals(subject) || candidate.ReceivedDay > asOfDay) continue;
            if (winner == null || CompareResolutionPriority(candidate, winner) > 0) winner = candidate;
        }
        if (winner == null) return false;

        bool stale = maximumAgeDays.HasValue && asOfDay - winner.ReceivedDay > maximumAgeDays.Value;
        resolved = new SpatialResolvedObservation(winner.Clone(), !stale, stale);
        return true;
    }

    public SpatialKnowledgeBasis CreateBasis(PersonId actor, IEnumerable<string> consultedObservationIdentities)
    {
        return new SpatialKnowledgeBasis(actor, GetActorRevision(actor), consultedObservationIdentities);
    }

    public bool IsBasisCurrent(SpatialKnowledgeBasis basis)
    {
        return ContainsBasis(basis)
            && GetActorRevision(basis.ActorPersonId) == basis.ActorKnowledgeRevision;
    }

    internal bool ContainsBasis(SpatialKnowledgeBasis basis)
    {
        if (basis == null || basis.ActorPersonId == null || !personStore.TryGet(basis.ActorPersonId, out _)) return false;
        HashSet<string> current = new HashSet<string>(StringComparer.Ordinal);
        if (observationsByActor.TryGetValue(basis.ActorPersonId.Value, out List<SpatialObservation> values))
            foreach (SpatialObservation observation in values) current.Add(observation.StableIdentity);
        foreach (string identity in basis.ObservationIdentities)
            if (!current.Contains(identity)) return false;
        return true;
    }

    public SpatialKnowledgeInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();
        if (revision < 0L) violations.Add("Spatial Knowledge revision is negative.");
        long summedActorRevisions = 0L;
        foreach (KeyValuePair<string, List<SpatialObservation>> entry in observationsByActor)
        {
            PersonId actor = new PersonId(entry.Key);
            if (!personStore.TryGet(actor, out _)) violations.Add("Spatial Knowledge references an unregistered actor PersonId: " + entry.Key + ".");
            if (entry.Value == null) { violations.Add("Spatial Knowledge observation collection is null for " + entry.Key + "."); continue; }
            HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < entry.Value.Count; i++)
            {
                SpatialObservation value = entry.Value[i];
                if (!ValidateObservation(value, long.MaxValue, out _)) violations.Add("Spatial Knowledge contains an invalid observation for " + entry.Key + ".");
                if (value != null && !identities.Add(value.StableIdentity)) violations.Add("Spatial Knowledge contains duplicate observation identity " + value.StableIdentity + ".");
                if (value?.Provenance?.TransmittingPersonId != null
                    && !personStore.TryGet(value.Provenance.TransmittingPersonId, out _))
                    violations.Add("Spatial Knowledge provenance references an unregistered transmitting PersonId: " + value.Provenance.TransmittingPersonId.Value + ".");
                for (int j = 0; j < i; j++)
                {
                    SpatialObservation prior = entry.Value[j];
                    if (value != null && prior != null && SameSubjectAndProvenance(prior, value)
                        && !prior.HasSameOriginEvidence(value))
                        violations.Add("Spatial Knowledge contains conflicting values for one subject/provenance pair: " + value.Subject.StableKey + ".");
                }
            }
            if (!actorRevisions.TryGetValue(entry.Key, out long actorRevision) || actorRevision < 0L)
                violations.Add("Spatial Knowledge actor revision is absent or invalid for " + entry.Key + ".");
            else if (summedActorRevisions <= long.MaxValue - actorRevision) summedActorRevisions += actorRevision;
            else violations.Add("Spatial Knowledge actor revision total overflows.");
        }
        foreach (KeyValuePair<string, long> entry in actorRevisions)
            if (entry.Value < 0L || !observationsByActor.ContainsKey(entry.Key))
                violations.Add("Spatial Knowledge actor revision has no matching observation owner: " + entry.Key + ".");
        if (summedActorRevisions != revision) violations.Add("Spatial Knowledge global revision does not match its actor revision total.");
        return new SpatialKnowledgeInvariantReport(violations);
    }

    internal SpatialRouteKnowledgeStore Clone(PersonStore targetPersons)
    {
        if (targetPersons == null) throw new ArgumentNullException(nameof(targetPersons));
        SpatialRouteKnowledgeStore copy = new SpatialRouteKnowledgeStore(targetPersons);
        foreach (KeyValuePair<string, List<SpatialObservation>> entry in observationsByActor)
        {
            PersonId actor = new PersonId(entry.Key);
            if (!targetPersons.TryGet(actor, out _)) throw new ArgumentException("Spatial Knowledge actor is absent from target PersonStore.", nameof(targetPersons));
            List<SpatialObservation> cloned = new List<SpatialObservation>();
            foreach (SpatialObservation observation in entry.Value)
            {
                if (observation.Provenance.TransmittingPersonId != null
                    && !targetPersons.TryGet(observation.Provenance.TransmittingPersonId, out _))
                    throw new ArgumentException("Spatial Knowledge transmitter is absent from target PersonStore.", nameof(targetPersons));
                cloned.Add(observation.Clone());
            }
            copy.observationsByActor.Add(entry.Key, cloned);
            copy.actorRevisions.Add(entry.Key, actorRevisions[entry.Key]);
        }
        copy.revision = revision;
        return copy;
    }

    private bool ValidateObservation(SpatialObservation observation, long currentWorldDay, out SpatialKnowledgeFailure failure)
    {
        if (observation == null || observation.Subject == null || observation.Value == null || observation.Provenance == null
            || observation.ObservedDay < 0L || observation.ReceivedDay < observation.ObservedDay
            || observation.ReceivedDay > currentWorldDay || observation.ConfidencePermille < 0 || observation.ConfidencePermille > 1000
            || string.IsNullOrWhiteSpace(observation.PrecisionIdentity))
            return Fail(observation != null && observation.ReceivedDay > currentWorldDay
                    ? SpatialKnowledgeFailureCode.FutureObservation : SpatialKnowledgeFailureCode.InvalidObservation,
                "Spatial observation values, times, confidence, and precision must be valid at the supplied world day.", out failure);
        if (observation.Provenance.TransmittingPersonId != null
            && !personStore.TryGet(observation.Provenance.TransmittingPersonId, out _))
            return Fail(SpatialKnowledgeFailureCode.SourcePersonNotRegistered, "Observation provenance transmitter PersonId is not registered.", out failure);
        failure = SpatialKnowledgeFailure.None;
        return true;
    }

    private static int CompareResolutionPriority(SpatialObservation left, SpatialObservation right)
    {
        int day = left.ReceivedDay.CompareTo(right.ReceivedDay);
        if (day != 0) return day;
        int provenance = StringComparer.Ordinal.Compare(left.Provenance.StableKey, right.Provenance.StableKey);
        return provenance != 0 ? provenance : StringComparer.Ordinal.Compare(left.StableIdentity, right.StableIdentity);
    }

    private static int CompareEvidenceOrder(SpatialObservation left, SpatialObservation right)
    {
        int subject = left.Subject.CompareTo(right.Subject);
        if (subject != 0) return subject;
        int observed = left.ObservedDay.CompareTo(right.ObservedDay);
        if (observed != 0) return observed;
        int received = left.ReceivedDay.CompareTo(right.ReceivedDay);
        if (received != 0) return received;
        int provenance = StringComparer.Ordinal.Compare(left.Provenance.StableKey, right.Provenance.StableKey);
        return provenance != 0 ? provenance : StringComparer.Ordinal.Compare(left.StableIdentity, right.StableIdentity);
    }

    private static List<SpatialObservation> CloneAndSort(List<SpatialObservation> values)
    {
        List<SpatialObservation> result = new List<SpatialObservation>(values.Count);
        foreach (SpatialObservation observation in values) result.Add(observation.Clone());
        result.Sort(CompareEvidenceOrder);
        return result;
    }

    private static SpatialSubject CloneSubject(SpatialSubject value)
    {
        if (value.Kind != SpatialSubjectKind.TraversalOption) return value;
        SpatialRouteSegment segment = value.RouteSegment;
        return SpatialSubject.ForTraversalOption(new SpatialRouteSegment(
            new HexBoundaryKey(new HexId(segment.Boundary.FirstHexId.Value), new HexId(segment.Boundary.SecondHexId.Value)),
            new HexId(segment.FromHexId.Value), new HexId(segment.ToHexId.Value), segment.Option));
    }

    private static bool SameSubjectAndProvenance(SpatialObservation left, SpatialObservation right) =>
        left.Subject.Equals(right.Subject) && left.Provenance.Equals(right.Provenance);

    private static bool ContainsIdentity(List<SpatialObservation> values, string identity)
    {
        foreach (SpatialObservation observation in values)
            if (string.Equals(observation.StableIdentity, identity, StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool Fail(SpatialKnowledgeFailureCode code, string message, out SpatialKnowledgeFailure failure)
    { failure = SpatialKnowledgeFailure.Create(code, message); return false; }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}

public sealed class SpatialKnowledgeInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;
    internal SpatialKnowledgeInvariantReport(IEnumerable<string> values)
    {
        List<string> result = values == null ? new List<string>() : new List<string>(values);
        result.Sort(StringComparer.Ordinal);
        Violations = new ReadOnlyCollection<string>(result);
    }
}
