using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

public sealed class PoliticalSupportStore
{
    private readonly PersonStore personStore;
    private readonly FactionStore factionStore;
    private readonly PoliticalClaimStore politicalClaimStore;
    private readonly object ownerToken = new object();
    private readonly Dictionary<string, PoliticalSupportRelationRecord> recordsById =
        new Dictionary<string, PoliticalSupportRelationRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, PoliticalSupportRelationRecord> activeByPair =
        new Dictionary<string, PoliticalSupportRelationRecord>(StringComparer.Ordinal);
    private long revision;

    public PoliticalSupportStore(
        PersonStore personStore,
        FactionStore factionStore,
        PoliticalClaimStore politicalClaimStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        this.factionStore = factionStore ?? throw new ArgumentNullException(nameof(factionStore));
        this.politicalClaimStore = politicalClaimStore ?? throw new ArgumentNullException(nameof(politicalClaimStore));
    }

    internal object OwnerToken => ownerToken;

    public int Count => recordsById.Count;
    public long Revision => revision;

    public IReadOnlyList<PoliticalSupportRelationRecord> Records
    {
        get
        {
            List<PoliticalSupportRelationRecord> snapshot = new List<PoliticalSupportRelationRecord>(recordsById.Values);
            snapshot.Sort(CompareRecords);
            return new ReadOnlyCollection<PoliticalSupportRelationRecord>(snapshot);
        }
    }

    public bool TryRegister(
        PoliticalSupportRelationRecord record,
        out PoliticalSupportFailure failure)
    {
        if (record == null || record.RelationId == null || record.Source == null || record.Target == null)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.InvalidRelation,
                "A political support relation with a stable id, source, and target is required.");
            return false;
        }

        if (Enum.IsDefined(typeof(PoliticalSupportDisposition), record.Disposition) == false)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.InvalidDisposition,
                "The political support relation disposition is invalid.");
            return false;
        }

        if (record.StartedAbsoluteDay < 0L)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.InvalidStartedAbsoluteDay,
                "Relation start must not be negative.");
            return false;
        }

        if (record.EndedAbsoluteDay.HasValue
            && (record.EndedAbsoluteDay.Value < record.StartedAbsoluteDay
                || record.EndedAbsoluteDay.Value < 0L))
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.InvalidEndAbsoluteDay,
                "Relation end must be on or after relation start.");
            return false;
        }

        if (recordsById.ContainsKey(record.RelationId.Value))
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.DuplicateRelationId,
                "The political support relation id is already registered.");
            return false;
        }

        if (ValidateEndpoints(record, out failure) == false)
        {
            return false;
        }

        if (record.IsActive && activeByPair.ContainsKey(PairKey(record.Source, record.Target)))
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.DuplicateActiveRelation,
                "An active political support relation already exists for the source and target.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.RevisionOverflow,
                "The political support store revision cannot advance further.");
            return false;
        }

        recordsById.Add(record.RelationId.Value, record);
        if (record.IsActive)
        {
            activeByPair.Add(PairKey(record.Source, record.Target), record);
        }

        revision++;
        failure = PoliticalSupportFailure.None;
        return true;
    }

    public bool TryGet(
        PoliticalSupportRelationId relationId,
        out PoliticalSupportRelationRecord record)
    {
        record = null;
        return relationId != null && recordsById.TryGetValue(relationId.Value, out record);
    }

    public bool TryGetActive(
        PoliticalSupportSource source,
        PoliticalSupportTarget target,
        out PoliticalSupportRelationRecord record)
    {
        record = null;
        if (source == null || target == null)
        {
            return false;
        }

        return activeByPair.TryGetValue(PairKey(source, target), out record);
    }

    public IReadOnlyList<PoliticalSupportRelationRecord> GetForSource(PoliticalSupportSource source)
    {
        return GetRecords(record => record.Source.Equals(source));
    }

    public IReadOnlyList<PoliticalSupportRelationRecord> GetForSource(PersonId personId)
    {
        return personId == null
            ? EmptyRecords()
            : GetForSource(PoliticalSupportSource.ForPerson(personId));
    }

    public IReadOnlyList<PoliticalSupportRelationRecord> GetForSource(FactionId factionId)
    {
        return factionId == null
            ? EmptyRecords()
            : GetForSource(PoliticalSupportSource.ForFaction(factionId));
    }

    public IReadOnlyList<PoliticalSupportRelationRecord> GetForTarget(PoliticalSupportTarget target)
    {
        return GetRecords(record => record.Target.Equals(target));
    }

    public IReadOnlyList<PoliticalSupportRelationRecord> GetForTarget(PoliticalClaimId politicalClaimId)
    {
        return politicalClaimId == null
            ? EmptyRecords()
            : GetForTarget(PoliticalSupportTarget.ForPoliticalClaim(politicalClaimId));
    }

    public IReadOnlyList<PoliticalSupportRelationRecord> GetForTarget(PersonId successionCandidatePersonId)
    {
        return successionCandidatePersonId == null
            ? EmptyRecords()
            : GetForTarget(PoliticalSupportTarget.ForSuccessionCandidate(successionCandidatePersonId));
    }

    public IReadOnlyList<PoliticalSupportRelationRecord> GetForPair(
        PoliticalSupportSource source,
        PoliticalSupportTarget target)
    {
        return source == null || target == null
            ? EmptyRecords()
            : GetRecords(record => record.Source.Equals(source) && record.Target.Equals(target));
    }

    public bool TryProposeEnd(
        PoliticalSupportRelationId relationId,
        long expectedWorldDay,
        out PoliticalSupportEndTransition transition,
        out PoliticalSupportFailure failure)
    {
        transition = null;
        if (relationId == null || expectedWorldDay < 0L)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.InvalidTransition,
                "A relation id and non-negative expected world day are required.");
            return false;
        }

        if (TryGet(relationId, out PoliticalSupportRelationRecord existing) == false)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.RelationNotRegistered,
                "The political support relation is not registered.");
            return false;
        }

        if (existing.IsActive == false)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.RelationAlreadyEnded,
                "The political support relation has already ended.");
            return false;
        }

        if (expectedWorldDay < existing.StartedAbsoluteDay)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.InvalidEndAbsoluteDay,
                "Relation end must not precede relation start.");
            return false;
        }

        transition = new PoliticalSupportEndTransition(
            this,
            existing,
            revision,
            expectedWorldDay,
            expectedWorldDay);
        failure = PoliticalSupportFailure.None;
        return true;
    }

    public bool TryProposeEnd(
        PoliticalSupportSource source,
        PoliticalSupportTarget target,
        long expectedWorldDay,
        out PoliticalSupportEndTransition transition,
        out PoliticalSupportFailure failure)
    {
        transition = null;
        if (source == null || target == null)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.InvalidTransition,
                "A source and target are required.");
            return false;
        }

        if (TryGetActive(source, target, out PoliticalSupportRelationRecord active) == false)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.RelationNotRegistered,
                "No active political support relation exists for the source and target.");
            return false;
        }

        return TryProposeEnd(active.RelationId, expectedWorldDay, out transition, out failure);
    }

    public bool TryApplyEnd(
        PoliticalSupportEndTransition transition,
        long currentWorldDay,
        out PoliticalSupportFailure failure)
    {
        if (transition == null || transition.ExpectedRelation == null)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.InvalidTransition,
                "A valid political support relation end transition is required.");
            return false;
        }

        if (ReferenceEquals(transition.ExpectedStore, this) == false)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.WrongSupportStore,
                "The political support transition belongs to another support store.");
            return false;
        }

        if (currentWorldDay < 0L || currentWorldDay != transition.ExpectedWorldDay)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.StaleWorldDay,
                "The political support relation end proposal was created for a different world day.");
            return false;
        }

        if (revision != transition.ExpectedStoreRevision)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.StaleRelation,
                "The political support store changed after the end proposal was created.");
            return false;
        }

        if (recordsById.TryGetValue(transition.RelationId.Value, out PoliticalSupportRelationRecord current) == false
            || ReferenceEquals(current, transition.ExpectedRelation) == false
            || current.IsActive == false)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.StaleRelation,
                "The political support relation changed after the end proposal was created.");
            return false;
        }

        if (transition.EndedAbsoluteDay < current.StartedAbsoluteDay
            || transition.EndedAbsoluteDay != currentWorldDay)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.InvalidEndAbsoluteDay,
                "Relation end must be within the current world timeline and on or after relation start.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.RevisionOverflow,
                "The political support store revision cannot advance further.");
            return false;
        }

        PoliticalSupportRelationRecord ended = current.WithEnd(transition.EndedAbsoluteDay);
        recordsById[transition.RelationId.Value] = ended;
        activeByPair.Remove(PairKey(current.Source, current.Target));
        revision++;
        failure = PoliticalSupportFailure.None;
        return true;
    }

    private bool ValidateEndpoints(
        PoliticalSupportRelationRecord record,
        out PoliticalSupportFailure failure)
    {
        if (record.Source.Kind == PoliticalSupportSourceKind.Person
            && personStore.TryGet(record.Source.PersonId, out _) == false)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.SourcePersonNotRegistered,
                "The political support source PersonId is not registered.");
            return false;
        }

        if (record.Source.Kind == PoliticalSupportSourceKind.Faction
            && factionStore.TryGet(record.Source.FactionId, out _) == false)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.SourceFactionNotRegistered,
                "The political support source FactionId is not registered.");
            return false;
        }

        if (record.Target.Kind == PoliticalSupportTargetKind.PoliticalClaim
            && politicalClaimStore.TryGet(record.Target.PoliticalClaimId, out _) == false)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.TargetClaimNotRegistered,
                "The political support target PoliticalClaimId is not registered.");
            return false;
        }

        if (record.Target.Kind == PoliticalSupportTargetKind.SuccessionCandidate
            && personStore.TryGet(record.Target.SuccessionCandidatePersonId, out _) == false)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.TargetCandidateNotRegistered,
                "The succession-candidate PersonId is not registered.");
            return false;
        }

        failure = PoliticalSupportFailure.None;
        return true;
    }

    private IReadOnlyList<PoliticalSupportRelationRecord> GetRecords(
        Func<PoliticalSupportRelationRecord, bool> predicate)
    {
        List<PoliticalSupportRelationRecord> result = new List<PoliticalSupportRelationRecord>();
        if (predicate != null)
        {
            foreach (PoliticalSupportRelationRecord record in recordsById.Values)
            {
                if (predicate(record))
                {
                    result.Add(record);
                }
            }
        }

        result.Sort(CompareRecords);
        return new ReadOnlyCollection<PoliticalSupportRelationRecord>(result);
    }

    private static IReadOnlyList<PoliticalSupportRelationRecord> EmptyRecords()
    {
        return new ReadOnlyCollection<PoliticalSupportRelationRecord>(
            new List<PoliticalSupportRelationRecord>());
    }

    private static string PairKey(PoliticalSupportSource source, PoliticalSupportTarget target)
    {
        return ((int)source.Kind).ToString(CultureInfo.InvariantCulture)
            + ":" + source.Value.Length.ToString(CultureInfo.InvariantCulture)
            + ":" + source.Value
            + ":" + ((int)target.Kind).ToString(CultureInfo.InvariantCulture)
            + ":" + target.Value.Length.ToString(CultureInfo.InvariantCulture)
            + ":" + target.Value;
    }

    private static int CompareRecords(
        PoliticalSupportRelationRecord left,
        PoliticalSupportRelationRecord right)
    {
        int sourceKind = left.Source.Kind.CompareTo(right.Source.Kind);
        if (sourceKind != 0) return sourceKind;

        int source = StringComparer.Ordinal.Compare(left.Source.Value, right.Source.Value);
        if (source != 0) return source;

        int targetKind = left.Target.Kind.CompareTo(right.Target.Kind);
        if (targetKind != 0) return targetKind;

        int target = StringComparer.Ordinal.Compare(left.Target.Value, right.Target.Value);
        if (target != 0) return target;

        int disposition = left.Disposition.CompareTo(right.Disposition);
        if (disposition != 0) return disposition;

        int started = left.StartedAbsoluteDay.CompareTo(right.StartedAbsoluteDay);
        if (started != 0) return started;

        return StringComparer.Ordinal.Compare(left.RelationId.Value, right.RelationId.Value);
    }
}
