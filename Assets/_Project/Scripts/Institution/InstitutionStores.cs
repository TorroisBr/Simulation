using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// In-memory registry for institution definitions within one world boundary.
/// </summary>
public sealed class InstitutionStore
{
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
}

/// <summary>
/// In-memory office registry and controlled incumbency boundary for one world.
/// It deliberately has no dependency on mutable simulation runtime adapters.
/// </summary>
public sealed class OfficeStore
{
    private readonly InstitutionStore institutionStore;
    private readonly Dictionary<string, OfficeRecord> records =
        new Dictionary<string, OfficeRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, OfficeIncumbency> incumbencies =
        new Dictionary<string, OfficeIncumbency>(StringComparer.Ordinal);

    public OfficeStore(InstitutionStore institutionStore)
    {
        this.institutionStore = institutionStore ?? throw new ArgumentNullException(nameof(institutionStore));
    }

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

    public bool TryRegister(OfficeRecord record, out InstitutionFoundationFailure failure)
    {
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

    public bool TryAssignIncumbent(
        OfficeId officeId,
        PersonId incumbent,
        long? startAbsoluteDay,
        out InstitutionFoundationFailure failure)
    {
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

        incumbencies.Add(officeId.Value, new OfficeIncumbency(officeId, incumbent, startAbsoluteDay));
        failure = InstitutionFoundationFailure.None;
        return true;
    }

    public bool TryVacateOffice(OfficeId officeId, out InstitutionFoundationFailure failure)
    {
        if (TryGet(officeId, out _) == false)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.OfficeNotFound,
                "The office must be registered before it can be vacated.");
            return false;
        }

        if (incumbencies.Remove(officeId.Value) == false)
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.OfficeAlreadyVacant,
                "The office is already vacant.");
            return false;
        }

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
}
