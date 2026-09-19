using System;

public static class PropertyTransferSystem
{
    public static bool TryProposeTransfer(
        PersonStore personStore,
        PropertyOwnershipStore propertyStore,
        PropertyId propertyId,
        PersonId newOwnerPersonId,
        long transferAbsoluteDay,
        out PropertyOwnershipTransferTransition transition,
        out PropertyTransferFailure failure)
    {
        transition = null;
        failure = PropertyTransferFailure.None;

        if (personStore == null || propertyStore == null)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidStore,
                "PersonStore and PropertyOwnershipStore are required.");
            return false;
        }

        if (propertyStore.PersonStoreForWorldBoundary != null
            && ReferenceEquals(propertyStore.PersonStoreForWorldBoundary, personStore) == false)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidStore,
                "The property store and PersonStore must belong to the same world.");
            return false;
        }

        if (propertyId == null)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidPropertyId,
                "A valid PropertyId is required.");
            return false;
        }

        if (newOwnerPersonId == null)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidNewOwner,
                "A valid new owner PersonId is required.");
            return false;
        }

        if (transferAbsoluteDay < 0L)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidTransferDay,
                "TransferAbsoluteDay cannot be negative.");
            return false;
        }

        if (propertyStore.Revision == long.MaxValue)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.RevisionOverflow,
                "The property ownership store revision cannot advance further.");
            return false;
        }

        if (propertyStore.TryGet(propertyId, out PropertyOwnershipRecord ownership) == false)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.PropertyNotFound,
                "The property must be registered before it can be transferred.");
            return false;
        }

        if (ownership.OwnerPersonId == newOwnerPersonId)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.SameOwner,
                "The new owner must differ from the current owner.");
            return false;
        }

        if (personStore.TryGet(newOwnerPersonId, out PersonRuntime newOwner) == false)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.PersonNotRegistered,
                "The new owner PersonId must be registered in the world.");
            return false;
        }

        if (newOwner.IsDeadAt(transferAbsoluteDay))
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.NewOwnerNotLiving,
                "The new owner must be factually alive on the transfer day.");
            return false;
        }

        transition = new PropertyOwnershipTransferTransition(
            ownership,
            newOwner,
            transferAbsoluteDay,
            propertyStore.Revision);
        return true;
    }

    public static bool TryApplyTransfer(
        PersonStore personStore,
        PropertyOwnershipStore propertyStore,
        PropertyOwnershipTransferTransition transition,
        out PropertyTransferFailure failure)
    {
        failure = PropertyTransferFailure.None;
        if (personStore == null || propertyStore == null)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidStore,
                "PersonStore and PropertyOwnershipStore are required.");
            return false;
        }

        if (propertyStore.PersonStoreForWorldBoundary != null
            && ReferenceEquals(propertyStore.PersonStoreForWorldBoundary, personStore) == false)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidStore,
                "The property store and PersonStore must belong to the same world.");
            return false;
        }

        if (transition == null
            || transition.PropertyId == null
            || transition.ExpectedOwnership == null
            || transition.ExpectedOwnerPersonId == null
            || transition.NewOwnerPersonId == null
            || transition.ExpectedNewOwner == null
            || transition.TransferAbsoluteDay < 0L
            || transition.ExpectedPropertyStoreRevision < 0L)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidTransition,
                "A valid property transfer transition is required.");
            return false;
        }

        if (personStore.TryGet(transition.NewOwnerPersonId, out PersonRuntime currentNewOwner) == false
            || ReferenceEquals(currentNewOwner, transition.ExpectedNewOwner) == false)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.PersonNotRegistered,
                "The new owner registration changed after the transition was proposed.");
            return false;
        }

        if (currentNewOwner.IsDeadAt(transition.TransferAbsoluteDay))
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.NewOwnerNotLiving,
                "The new owner is no longer factually alive on the transfer day.");
            return false;
        }

        PropertyOwnershipRecord nextOwnership = new PropertyOwnershipRecord(
            transition.PropertyId,
            transition.NewOwnerPersonId);
        PropertyOwnershipTransferHistoryRecord history =
            new PropertyOwnershipTransferHistoryRecord(
                transition.PropertyId,
                transition.ExpectedOwnerPersonId,
                transition.NewOwnerPersonId,
                transition.TransferAbsoluteDay);
        return propertyStore.TryApplyTransfer(
            transition,
            nextOwnership,
            history,
            out failure);
    }

    public static bool TryTransfer(
        PersonStore personStore,
        PropertyOwnershipStore propertyStore,
        PropertyId propertyId,
        PersonId newOwnerPersonId,
        long transferAbsoluteDay,
        out PropertyOwnershipRecord ownership,
        out PropertyTransferFailure failure)
    {
        ownership = null;
        if (TryProposeTransfer(
                personStore,
                propertyStore,
                propertyId,
                newOwnerPersonId,
                transferAbsoluteDay,
                out PropertyOwnershipTransferTransition transition,
                out failure) == false
            || TryApplyTransfer(personStore, propertyStore, transition, out failure) == false)
        {
            return false;
        }

        propertyStore.TryGet(propertyId, out ownership);
        return ownership != null;
    }
}
