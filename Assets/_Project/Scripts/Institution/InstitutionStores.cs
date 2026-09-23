using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// In-memory registry for institution definitions within one world boundary.
/// </summary>
public sealed class InstitutionStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly Dictionary<string, InstitutionRecord> records =
        new Dictionary<string, InstitutionRecord>(StringComparer.Ordinal);

    public IReadOnlyList<InstitutionRecord> Institutions
    {
        get
        {
            List<InstitutionRecord> snapshot = new List<InstitutionRecord>(records.Values);
            snapshot.Sort((left, right) => string.CompareOrdinal(left.Id.Value, right.Id.Value));
            return new ReadOnlyCollection<InstitutionRecord>(snapshot);
        }
    }

    public bool TryRegister(InstitutionRecord record, out InstitutionFoundationFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = InstitutionFoundationFailure.Create(InstitutionFoundationFailureCode.RuntimeFaulted, "The runtime is faulted.");
            return false;
        }

        if (record == null || record.Id == null)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.InvalidInstitutionRecord,
                "An institution record with a valid id is required.");
            return false;
        }

        if (records.ContainsKey(record.Id.Value))
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.DuplicateInstitutionId,
                "The institution id is already registered.");
            return false;
        }

        records.Add(record.Id.Value, record);
        failure = InstitutionFoundationFailure.None;
        return true;
    }

    public bool TryGet(InstitutionId id, out InstitutionRecord record)
    {
        record = null;
        return id != null && records.TryGetValue(id.Value, out record);
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}

/// <summary>
/// In-memory office registry and controlled incumbency boundary for one world.
/// It deliberately has no dependency on mutable simulation runtime adapters.
/// </summary>
public sealed class OfficeStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly InstitutionStore institutionStore;
    private readonly Dictionary<string, OfficeRecord> records =
        new Dictionary<string, OfficeRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, OfficeIncumbency> incumbencies =
        new Dictionary<string, OfficeIncumbency>(StringComparer.Ordinal);
    private readonly List<OfficeTenureRecord> tenureHistory = new List<OfficeTenureRecord>();

    public OfficeStore(InstitutionStore institutionStore)
    {
        this.institutionStore = institutionStore ?? throw new ArgumentNullException(nameof(institutionStore));
    }

    internal InstitutionStore InstitutionStoreForWorldBoundary => institutionStore;

    public IReadOnlyList<OfficeRecord> Offices
    {
        get
        {
            return CreateSortedOfficeSnapshot(records.Values);
        }
    }

    public IReadOnlyList<OfficeIncumbency> Incumbencies
    {
        get
        {
            List<OfficeIncumbency> snapshot = new List<OfficeIncumbency>(incumbencies.Values);
            snapshot.Sort((left, right) => string.CompareOrdinal(left.OfficeId.Value, right.OfficeId.Value));
            return new ReadOnlyCollection<OfficeIncumbency>(snapshot);
        }
    }

    public IReadOnlyList<OfficeTenureRecord> TenureHistory
    {
        get
        {
            List<OfficeTenureRecord> snapshot = new List<OfficeTenureRecord>(tenureHistory);
            snapshot.Sort((left, right) =>
            {
                int office = string.CompareOrdinal(left.OfficeId.Value, right.OfficeId.Value);
                if (office != 0)
                {
                    return office;
                }

                int start = Nullable.Compare(left.StartAbsoluteDay, right.StartAbsoluteDay);
                if (start != 0)
                {
                    return start;
                }

                int incumbent = string.CompareOrdinal(left.Incumbent.Value, right.Incumbent.Value);
                if (incumbent != 0)
                {
                    return incumbent;
                }

                int end = Nullable.Compare(left.EndAbsoluteDay, right.EndAbsoluteDay);
                if (end != 0)
                {
                    return end;
                }

                int reason = Nullable.Compare(
                    left.EndReason.HasValue ? (int?)left.EndReason.Value : null,
                    right.EndReason.HasValue ? (int?)right.EndReason.Value : null);
                return reason != 0 ? reason : left.IsClosed.CompareTo(right.IsClosed);
            });
            return new ReadOnlyCollection<OfficeTenureRecord>(snapshot);
        }
    }

    internal IReadOnlyList<OfficeTenureRecord> TenureHistoryInMutationOrder
    {
        get
        {
            return new ReadOnlyCollection<OfficeTenureRecord>(
                new List<OfficeTenureRecord>(tenureHistory));
        }
    }

    public bool TryRegister(OfficeRecord record, out InstitutionFoundationFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = InstitutionFoundationFailure.Create(InstitutionFoundationFailureCode.RuntimeFaulted, "The runtime is faulted.");
            return false;
        }

        if (record == null || record.Id == null || record.InstitutionId == null)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.InvalidOfficeRecord,
                "An office record with valid ids is required.");
            return false;
        }

        if (records.ContainsKey(record.Id.Value))
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.DuplicateOfficeId,
                "The office id is already registered.");
            return false;
        }

        if (institutionStore.TryGet(record.InstitutionId, out _) == false)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.InstitutionNotFoundForOffice,
                "The office institution must be registered before the office.");
            return false;
        }

        records.Add(record.Id.Value, record);
        failure = InstitutionFoundationFailure.None;
        return true;
    }

    public bool TryGet(OfficeId id, out OfficeRecord record)
    {
        record = null;
        return id != null && records.TryGetValue(id.Value, out record);
    }

    public bool TryGetIncumbency(OfficeId officeId, out OfficeIncumbency incumbency)
    {
        incumbency = null;
        return officeId != null && incumbencies.TryGetValue(officeId.Value, out incumbency);
    }

    public bool TryGetCurrentIncumbent(OfficeId officeId, out PersonId incumbent)
    {
        incumbent = null;
        if (TryGetIncumbency(officeId, out OfficeIncumbency incumbency) == false)
        {
            return false;
        }

        incumbent = incumbency.Incumbent;
        return true;
    }

    public bool IsVacant(OfficeId officeId)
    {
        return officeId != null
            && records.ContainsKey(officeId.Value)
            && incumbencies.ContainsKey(officeId.Value) == false;
    }

    internal bool TryGetLatestClosedTenure(
        OfficeId officeId,
        out OfficeTenureRecord tenure)
    {
        tenure = null;
        if (officeId == null)
        {
            return false;
        }

        for (int index = tenureHistory.Count - 1; index >= 0; index--)
        {
            OfficeTenureRecord candidate = tenureHistory[index];
            if (candidate != null
                && candidate.OfficeId == officeId
                && candidate.IsClosed)
            {
                tenure = candidate;
                return true;
            }
        }

        return false;
    }

    public bool TryAssignIncumbent(
        OfficeId officeId,
        PersonId incumbent,
        long? startAbsoluteDay,
        out InstitutionFoundationFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = InstitutionFoundationFailure.Create(InstitutionFoundationFailureCode.RuntimeFaulted, "The runtime is faulted.");
            return false;
        }

        if (TryGet(officeId, out _) == false)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.OfficeNotFound,
                "The office must be registered before assigning an incumbent.");
            return false;
        }

        if (incumbent == null)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.InvalidPersonId,
                "A valid PersonId is required for an incumbent.");
            return false;
        }

        if (startAbsoluteDay.HasValue && startAbsoluteDay.Value < 0L)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.InvalidStartAbsoluteDay,
                "StartAbsoluteDay cannot be negative.");
            return false;
        }

        if (incumbencies.ContainsKey(officeId.Value))
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.OfficeAlreadyOccupied,
                "An occupied office must be vacated before it can receive another incumbent.");
            return false;
        }

        OfficeIncumbency next = new OfficeIncumbency(officeId, incumbent, startAbsoluteDay);
        incumbencies.Add(officeId.Value, next);
        tenureHistory.Add(new OfficeTenureRecord(
            officeId,
            incumbent,
            startAbsoluteDay,
            null,
            null));
        failure = InstitutionFoundationFailure.None;
        return true;
    }

    public bool TryVacateOffice(OfficeId officeId, out InstitutionFoundationFailure failure)
    {
        return TryVacateOffice(
            officeId,
            null,
            InstitutionalVacancyRecognitionReason.ExplicitDecision,
            out failure);
    }

    public bool TryVacateOffice(
        OfficeId officeId,
        long? endAbsoluteDay,
        InstitutionalVacancyRecognitionReason endReason,
        out InstitutionFoundationFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = InstitutionFoundationFailure.Create(InstitutionFoundationFailureCode.RuntimeFaulted, "The runtime is faulted.");
            return false;
        }

        if (TryGet(officeId, out _) == false)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.OfficeNotFound,
                "The office must be registered before it can be vacated.");
            return false;
        }

        if (incumbencies.ContainsKey(officeId.Value) == false)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.OfficeAlreadyVacant,
                "The office is already vacant.");
            return false;
        }

        if (endAbsoluteDay.HasValue == false
            && endReason != InstitutionalVacancyRecognitionReason.ExplicitDecision)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.InvalidVacancyRecognitionReason,
                "A non-default vacancy recognition reason requires an end day.");
            return false;
        }

        if (Enum.IsDefined(typeof(InstitutionalVacancyRecognitionReason), endReason) == false)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.InvalidVacancyRecognitionReason,
                "The vacancy recognition reason is not supported.");
            return false;
        }

        OfficeIncumbency current = incumbencies[officeId.Value];
        if (endAbsoluteDay.HasValue
            && current.StartAbsoluteDay.HasValue
            && endAbsoluteDay.Value < current.StartAbsoluteDay.Value)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.InvalidEndAbsoluteDay,
                "EndAbsoluteDay cannot be earlier than StartAbsoluteDay.");
            return false;
        }

        OfficeTenureRecord closed = new OfficeTenureRecord(
            current.OfficeId,
            current.Incumbent,
            current.StartAbsoluteDay,
            endAbsoluteDay,
            endReason,
            true);

        if (incumbencies.Remove(officeId.Value) == false)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.OfficeAlreadyVacant,
                "The office is already vacant.");
            return false;
        }

        for (int index = tenureHistory.Count - 1; index >= 0; index--)
        {
            OfficeTenureRecord candidate = tenureHistory[index];
            if (candidate.IsOpen
                && candidate.OfficeId == current.OfficeId
                && candidate.Incumbent == current.Incumbent
                && candidate.StartAbsoluteDay == current.StartAbsoluteDay)
            {
                tenureHistory[index] = closed;
                break;
            }
        }

        failure = InstitutionFoundationFailure.None;
        return true;
    }

    internal bool TryAddHistoricalTenure(
        OfficeTenureRecord record,
        out InstitutionFoundationFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = InstitutionFoundationFailure.Create(InstitutionFoundationFailureCode.RuntimeFaulted, "The runtime is faulted.");
            return false;
        }

        if (record == null || record.OfficeId == null || record.Incumbent == null || record.IsOpen)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.InvalidOfficeRecord,
                "A closed historical office tenure is required.");
            return false;
        }

        if (TryGet(record.OfficeId, out _) == false)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.OfficeNotFound,
                "The historical tenure office must be registered first.");
            return false;
        }

        foreach (OfficeTenureRecord existing in tenureHistory)
        {
            if (existing.Equals(record))
            {
                failure = InstitutionFoundationFailure.Create(
                    InstitutionFoundationFailureCode.StaleIncumbency,
                    "The historical office tenure is already registered.");
                return false;
            }
        }

        tenureHistory.Add(record);
        failure = InstitutionFoundationFailure.None;
        return true;
    }

    public IReadOnlyList<OfficeRecord> GetVacantOffices()
    {
        List<OfficeRecord> vacant = new List<OfficeRecord>();
        foreach (OfficeRecord record in records.Values)
        {
            if (incumbencies.ContainsKey(record.Id.Value) == false)
            {
                vacant.Add(record);
            }
        }

        vacant.Sort((left, right) => string.CompareOrdinal(left.Id.Value, right.Id.Value));
        return new ReadOnlyCollection<OfficeRecord>(vacant);
    }

    public IReadOnlyList<OfficeRecord> GetOfficesForInstitution(InstitutionId institutionId)
    {
        List<OfficeRecord> result = new List<OfficeRecord>();
        if (institutionId == null)
        {
            return new ReadOnlyCollection<OfficeRecord>(result);
        }

        foreach (OfficeRecord record in records.Values)
        {
            if (record.InstitutionId == institutionId)
            {
                result.Add(record);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(left.Id.Value, right.Id.Value));
        return new ReadOnlyCollection<OfficeRecord>(result);
    }

    public IReadOnlyList<OfficeRecord> GetOfficesHeldBy(PersonId personId)
    {
        List<OfficeRecord> result = new List<OfficeRecord>();
        if (personId == null)
        {
            return new ReadOnlyCollection<OfficeRecord>(result);
        }

        foreach (OfficeIncumbency incumbency in incumbencies.Values)
        {
            if (incumbency.Incumbent == personId
                && records.TryGetValue(incumbency.OfficeId.Value, out OfficeRecord record))
            {
                result.Add(record);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(left.Id.Value, right.Id.Value));
        return new ReadOnlyCollection<OfficeRecord>(result);
    }

    private static IReadOnlyList<OfficeRecord> CreateSortedOfficeSnapshot(
        ICollection<OfficeRecord> source)
    {
        List<OfficeRecord> snapshot = new List<OfficeRecord>(source);
        snapshot.Sort((left, right) => string.CompareOrdinal(left.Id.Value, right.Id.Value));
        return new ReadOnlyCollection<OfficeRecord>(snapshot);
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard)
            && institutionStore.CanBindMutationGuard(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard)
            && institutionStore.TryBindMutationGuard(guard)
            && mutationGuardBinding.TryBindTo(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}
