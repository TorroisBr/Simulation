using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// World-owned registry of explicit property ownership records. This minimal
/// foundation is explicit and world-owned; transfer is an explicit domain
/// transition, not an incidental effect of death or estate opening.
/// </summary>
public sealed class PropertyOwnershipStore
{
    private readonly PersonStore personStore;
    private readonly Dictionary<string, PropertyOwnershipRecord> recordsByPropertyId =
        new Dictionary<string, PropertyOwnershipRecord>(StringComparer.Ordinal);
    private readonly List<PropertyOwnershipTransferHistoryRecord> transferHistory =
        new List<PropertyOwnershipTransferHistoryRecord>();
    private long revision;

    public PropertyOwnershipStore()
    {
    }

    public PropertyOwnershipStore(PersonStore personStore)
    {
        this.personStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
    }

    internal PersonStore PersonStoreForWorldBoundary => personStore;

    public int Count => recordsByPropertyId.Count;
    public long Revision => revision;

    public IReadOnlyList<PropertyOwnershipRecord> OwnershipRecords
    {
        get
        {
            List<PropertyOwnershipRecord> snapshot =
                new List<PropertyOwnershipRecord>(recordsByPropertyId.Values);
            snapshot.Sort(CompareRecords);
            return new ReadOnlyCollection<PropertyOwnershipRecord>(snapshot);
        }
    }

    public IReadOnlyList<PropertyOwnershipRecord> Records => OwnershipRecords;

    public IReadOnlyList<PropertyOwnershipTransferHistoryRecord> TransferHistory
    {
        get
        {
            List<PropertyOwnershipTransferHistoryRecord> snapshot =
                new List<PropertyOwnershipTransferHistoryRecord>(transferHistory);
            snapshot.Sort(PropertyOwnershipTransferHistoryRecord.Compare);
            return new ReadOnlyCollection<PropertyOwnershipTransferHistoryRecord>(snapshot);
        }
    }

    public bool TryRegister(
        PropertyOwnershipRecord record,
        out PropertyFoundationFailure failure)
    {
        if (record == null || record.PropertyId == null || record.OwnerPersonId == null)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.InvalidOwnershipRecord,
                "A property ownership record with valid ids is required.");
            return false;
        }

        if (personStore != null
            && personStore.TryGet(record.OwnerPersonId, out _) == false)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.PersonNotRegistered,
                "The property owner PersonId must be registered in the bound PersonStore.");
            return false;
        }

        if (recordsByPropertyId.ContainsKey(record.PropertyId.Value))
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.DuplicatePropertyId,
                "The property id is already registered.");
            return false;
        }

        recordsByPropertyId.Add(record.PropertyId.Value, record);
        if (revision == long.MaxValue)
        {
            recordsByPropertyId.Remove(record.PropertyId.Value);
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.RevisionOverflow,
                "The property ownership store revision cannot advance further.");
            return false;
        }

        revision++;
        failure = PropertyFoundationFailure.None;
        return true;
    }

    public bool TryGet(PropertyId propertyId, out PropertyOwnershipRecord record)
    {
        record = null;
        return propertyId != null
            && recordsByPropertyId.TryGetValue(propertyId.Value, out record);
    }

    public IReadOnlyList<PropertyOwnershipRecord> GetOwnedBy(PersonId ownerPersonId)
    {
        List<PropertyOwnershipRecord> result = new List<PropertyOwnershipRecord>();
        if (ownerPersonId == null)
        {
            return new ReadOnlyCollection<PropertyOwnershipRecord>(result);
        }

        foreach (PropertyOwnershipRecord record in recordsByPropertyId.Values)
        {
            if (record.OwnerPersonId == ownerPersonId)
            {
                result.Add(record);
            }
        }

        result.Sort(CompareRecords);
        return new ReadOnlyCollection<PropertyOwnershipRecord>(result);
    }

    private static int CompareRecords(
        PropertyOwnershipRecord left,
        PropertyOwnershipRecord right)
    {
        int propertyComparison = string.CompareOrdinal(
            left.PropertyId.Value,
            right.PropertyId.Value);
        return propertyComparison != 0
            ? propertyComparison
            : string.CompareOrdinal(left.OwnerPersonId.Value, right.OwnerPersonId.Value);
    }

    internal bool TryApplyTransfer(
        PropertyOwnershipTransferTransition transition,
        PropertyOwnershipRecord nextOwnership,
        PropertyOwnershipTransferHistoryRecord history,
        out PropertyTransferFailure failure)
    {
        if (transition == null || nextOwnership == null || history == null)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidTransition,
                "A valid property transfer transition is required.");
            return false;
        }

        if (revision != transition.ExpectedPropertyStoreRevision)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.StalePropertyStore,
                "The property ownership store changed after the transition was proposed.");
            return false;
        }

        if (recordsByPropertyId.TryGetValue(transition.PropertyId.Value, out PropertyOwnershipRecord current) == false
            || ReferenceEquals(current, transition.ExpectedOwnership) == false
            || current.OwnerPersonId != transition.ExpectedOwnerPersonId)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.StalePropertyOwnership,
                "The property ownership changed after the transition was proposed.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.RevisionOverflow,
                "The property ownership store revision cannot advance further.");
            return false;
        }

        recordsByPropertyId[transition.PropertyId.Value] = nextOwnership;
        transferHistory.Add(history);
        revision++;
        failure = PropertyTransferFailure.None;
        return true;
    }

    internal bool TryAddHistoricalTransfer(
        PropertyOwnershipTransferHistoryRecord history,
        out PropertyTransferFailure failure)
    {
        if (history == null)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidTransition,
                "A valid property transfer history record is required.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.RevisionOverflow,
                "The property ownership store revision cannot advance further.");
            return false;
        }

        transferHistory.Add(history);
        revision++;
        failure = PropertyTransferFailure.None;
        return true;
    }
}
