using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// World-owned registry of explicitly opened estates.
///
/// Factual Person death is intentionally not observed here. Estate opening is
/// controlled by EstateCreationSystem so callers must issue an explicit
/// downstream transition after validating the deceased Person.
/// </summary>
public sealed class EstateStore
{
    private readonly Dictionary<string, EstateRecord> recordsById =
        new Dictionary<string, EstateRecord>(StringComparer.Ordinal);
    private readonly Dictionary<PersonId, EstateRecord> recordsByDeceasedPerson =
        new Dictionary<PersonId, EstateRecord>();
    private long revision;

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

    public bool TryGetForDeceasedPerson(
        PersonId deceasedPersonId,
        out EstateRecord record)
    {
        return TryGetByDeceasedPerson(deceasedPersonId, out record);
    }

    internal bool TryRegister(
        EstateRecord record,
        out EstateFoundationFailure failure)
    {
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
}
