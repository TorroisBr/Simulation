using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// World-owned registry of explicitly opened estates. Factual Person death is
/// not observed here and never opens an estate implicitly.
/// </summary>
public sealed class EstateStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly PersonStore personStore;
    private readonly Dictionary<string, EstateRecord> recordsById =
        new Dictionary<string, EstateRecord>(StringComparer.Ordinal);
    private readonly Dictionary<PersonId, EstateRecord> recordsByDeceasedPerson =
        new Dictionary<PersonId, EstateRecord>();
    private long revision;

    public EstateStore(PersonStore personStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
    }

    internal static bool TryCreateFromOwnerSnapshot(
        PersonStore stagedPersonStore,
        IReadOnlyList<EstateRecord> records,
        long savedRevision,
        long savedAbsoluteDay,
        out EstateStore store)
    {
        store = null;
        if (stagedPersonStore == null
            || records == null
            || savedAbsoluteDay < 0L
            || savedRevision < 0L
            || savedRevision != records.Count)
            return false;

        EstateStore candidate = new EstateStore(stagedPersonStore);
        foreach (EstateRecord record in records)
        {
            if (record == null
                || record.EstateId == null
                || record.DeceasedPersonId == null
                || record.OpenedAbsoluteDay < 0L
                || record.OpenedAbsoluteDay > savedAbsoluteDay
                || candidate.recordsById.ContainsKey(record.EstateId.Value)
                || candidate.recordsByDeceasedPerson.ContainsKey(record.DeceasedPersonId)
                || !stagedPersonStore.TryGet(record.DeceasedPersonId, out PersonRuntime deceased)
                || !deceased.DeathAbsoluteDay.HasValue
                || record.OpenedAbsoluteDay < deceased.DeathAbsoluteDay.Value)
                return false;

            candidate.recordsById.Add(record.EstateId.Value, record);
            candidate.recordsByDeceasedPerson.Add(record.DeceasedPersonId, record);
        }

        candidate.revision = savedRevision;
        store = candidate;
        return true;
    }

    internal PersonStore PersonStoreForWorldBoundary => personStore;

    public int Count => recordsById.Count;
    public long Revision => revision;

    public IReadOnlyList<EstateRecord> Estates
    {
        get
        {
            List<EstateRecord> snapshot = new List<EstateRecord>(recordsById.Values);
            snapshot.Sort((left, right) => string.CompareOrdinal(
                left.EstateId.Value,
                right.EstateId.Value));
            return new ReadOnlyCollection<EstateRecord>(snapshot);
        }
    }

    public IReadOnlyList<EstateRecord> Records => Estates;

    public bool TryGet(EstateId estateId, out EstateRecord record)
    {
        record = null;
        return estateId != null && recordsById.TryGetValue(estateId.Value, out record);
    }

    public bool TryGetByDeceasedPerson(
        PersonId deceasedPersonId,
        out EstateRecord record)
    {
        record = null;
        return deceasedPersonId != null
            && recordsByDeceasedPerson.TryGetValue(deceasedPersonId, out record);
    }

    internal bool TryRegister(
        EstateRecord record,
        out EstateFoundationFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = EstateFoundationFailure.Create(EstateFoundationFailureCode.RuntimeFaulted, "The runtime is faulted.");
            return false;
        }

        if (record == null || record.EstateId == null || record.DeceasedPersonId == null)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidEstateRecord,
                "An estate record with valid ids is required.");
            return false;
        }

        if (recordsById.ContainsKey(record.EstateId.Value))
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.DuplicateEstateId,
                "The estate id is already registered.");
            return false;
        }

        if (recordsByDeceasedPerson.ContainsKey(record.DeceasedPersonId))
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.EstateAlreadyExistsForPerson,
                "The deceased Person already has an opened estate.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.RevisionOverflow,
                "The estate store revision cannot advance further.");
            return false;
        }

        recordsById.Add(record.EstateId.Value, record);
        recordsByDeceasedPerson.Add(record.DeceasedPersonId, record);
        revision++;
        failure = EstateFoundationFailure.None;
        return true;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard)
            && personStore.CanBindMutationGuard(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard)
            && personStore.TryBindMutationGuard(guard)
            && mutationGuardBinding.TryBindTo(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}
