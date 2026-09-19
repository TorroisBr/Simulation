using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// World-owned legal ownership registry. It is deliberately independent of
/// NpcRuntime containers and therefore survives dormant/materialized changes.
/// </summary>
public sealed class PropertyOwnershipStore
{
    private readonly Dictionary<string, PropertyOwnershipRecord> recordsById =
        new Dictionary<string, PropertyOwnershipRecord>(StringComparer.Ordinal);
    private long revision;

    public int Count => recordsById.Count;
    public long Revision => revision;

    public IReadOnlyList<PropertyOwnershipRecord> OwnershipRecords
    {
        get
        {
            List<PropertyOwnershipRecord> snapshot = new List<PropertyOwnershipRecord>(recordsById.Values);
            snapshot.Sort((left, right) => string.CompareOrdinal(
                left.PropertyId.Value,
                right.PropertyId.Value));
            return new ReadOnlyCollection<PropertyOwnershipRecord>(snapshot);
        }
    }

    public IReadOnlyList<PropertyOwnershipRecord> Records => OwnershipRecords;

    public bool TryGet(PropertyId propertyId, out PropertyOwnershipRecord record)
    {
        record = null;
        return propertyId != null && recordsById.TryGetValue(propertyId.Value, out record);
    }

    public bool TryRegister(
        PropertyOwnershipRecord record,
        out PropertyFoundationFailure failure)
    {
        if (record == null || record.PropertyId == null || record.Owner == null)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.InvalidOwnershipRecord,
                "A property ownership record with a valid id and owner is required.");
            return false;
        }

        if (recordsById.ContainsKey(record.PropertyId.Value))
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.DuplicatePropertyId,
                "The property id is already registered.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.RevisionOverflow,
                "The property ownership revision cannot advance further.");
            return false;
        }

        recordsById.Add(record.PropertyId.Value, record);
        revision++;
        failure = PropertyFoundationFailure.None;
        return true;
    }

    public IReadOnlyList<PropertyOwnershipRecord> GetOwnedBy(PersonId owner)
    {
        return GetOwnedBy(PropertyOwnerReference.ForPerson(owner));
    }

    public IReadOnlyList<PropertyOwnershipRecord> GetOwnedBy(EstateId owner)
    {
        return GetOwnedBy(PropertyOwnerReference.ForEstate(owner));
    }

    public IReadOnlyList<PropertyOwnershipRecord> GetOwnedBy(PropertyOwnerReference owner)
    {
        List<PropertyOwnershipRecord> result = new List<PropertyOwnershipRecord>();
        if (owner == null)
        {
            return new ReadOnlyCollection<PropertyOwnershipRecord>(result);
        }

        foreach (PropertyOwnershipRecord record in recordsById.Values)
        {
            if (record.Owner.Equals(owner))
            {
                result.Add(record);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(
            left.PropertyId.Value,
            right.PropertyId.Value));
        return new ReadOnlyCollection<PropertyOwnershipRecord>(result);
    }

    public bool TryProposeTransfer(
        PropertyId propertyId,
        PropertyOwnerReference newOwner,
        out PropertyOwnershipTransition transition,
        out PropertyFoundationFailure failure)
    {
        transition = null;
        if (propertyId == null || newOwner == null)
        {
            failure = PropertyFoundationFailure.Create(
                propertyId == null
                    ? PropertyFoundationFailureCode.InvalidPropertyId
                    : PropertyFoundationFailureCode.InvalidOwner,
                "A property id and new owner are required.");
            return false;
        }

        if (TryGet(propertyId, out PropertyOwnershipRecord record) == false)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.PropertyNotFound,
                "The property must be registered before it can be transferred.");
            return false;
        }

        if (record.Owner.Equals(newOwner))
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.SameOwner,
                "The property already has the requested owner.");
            return false;
        }

        transition = new PropertyOwnershipTransition(record, newOwner, revision);
        failure = PropertyFoundationFailure.None;
        return true;
    }

    public bool TryApplyTransfer(
        PropertyOwnershipTransition transition,
        out PropertyFoundationFailure failure)
    {
        failure = PropertyFoundationFailure.None;
        if (transition == null
            || transition.PropertyId == null
            || transition.ExpectedOwner == null
            || transition.NewOwner == null
            || transition.ExpectedStoreRevision < 0L)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.InvalidTransition,
                "A valid property ownership transition is required.");
            return false;
        }

        if (revision != transition.ExpectedStoreRevision
            || TryGet(transition.PropertyId, out PropertyOwnershipRecord current) == false
            || current.Equals(transition.ExpectedRecord) == false)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.StaleOwnership,
                "The property ownership changed after the transition was proposed.");
            return false;
        }

        if (current.Owner.Equals(transition.NewOwner))
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.SameOwner,
                "The property already has the requested owner.");
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.RevisionOverflow,
                "The property ownership revision cannot advance further.");
            return false;
        }

        recordsById[transition.PropertyId.Value] = new PropertyOwnershipRecord(
            transition.PropertyId,
            transition.NewOwner);
        revision++;
        return true;
    }
}
