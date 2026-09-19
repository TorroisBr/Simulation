using System;

/// <summary>
/// Explicit downstream command for opening an estate. Person death remains a
/// separate factual transition and does not call this system automatically.
/// </summary>
public static class EstateOpeningSystem
{
    public static bool TryProposeOpening(
        PersonStore personStore,
        EstateStore estateStore,
        EstateId estateId,
        PersonId deceasedPersonId,
        long openingAbsoluteDay,
        out EstateOpeningTransition transition,
        out EstateFoundationFailure failure)
    {
        transition = null;
        failure = EstateFoundationFailure.None;

        if (personStore == null || estateStore == null)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidStore,
                "PersonStore and EstateStore are required.");
            return false;
        }

        if (ReferenceEquals(personStore, estateStore.PersonStoreForWorldBoundary) == false)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidStore,
                "PersonStore and EstateStore must belong to the same world.");
            return false;
        }

        if (estateId == null)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidEstateId,
                "A valid EstateId is required.");
            return false;
        }

        if (deceasedPersonId == null)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidPersonId,
                "A valid deceased PersonId is required.");
            return false;
        }

        if (openingAbsoluteDay < 0L)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidOpeningDay,
                "OpeningAbsoluteDay cannot be negative.");
            return false;
        }

        if (estateStore.Revision == long.MaxValue)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.RevisionOverflow,
                "The estate store revision cannot advance further.");
            return false;
        }

        if (estateStore.TryGet(estateId, out _))
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.DuplicateEstateId,
                "The estate id is already registered.");
            return false;
        }

        if (estateStore.TryGetByDeceasedPerson(deceasedPersonId, out _))
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.EstateAlreadyExistsForPerson,
                "The deceased Person already has an opened estate.");
            return false;
        }

        if (personStore.TryGet(deceasedPersonId, out PersonRuntime person) == false)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.PersonNotRegistered,
                "The deceased Person must be registered in the world.");
            return false;
        }

        if (person.DeathAbsoluteDay.HasValue == false)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.PersonStillLiving,
                "An estate can only be opened for a factual dead Person.");
            return false;
        }

        if (openingAbsoluteDay < person.DeathAbsoluteDay.Value)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidOpeningDay,
                "OpeningAbsoluteDay cannot precede the Person death day.");
            return false;
        }

        transition = new EstateOpeningTransition(
            person,
            estateId,
            openingAbsoluteDay,
            estateStore.Revision);
        return true;
    }

    public static bool TryApplyOpening(
        PersonStore personStore,
        EstateStore estateStore,
        EstateOpeningTransition transition,
        out EstateFoundationFailure failure)
    {
        failure = EstateFoundationFailure.None;

        if (personStore == null || estateStore == null)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidStore,
                "PersonStore and EstateStore are required.");
            return false;
        }

        if (ReferenceEquals(personStore, estateStore.PersonStoreForWorldBoundary) == false)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidStore,
                "PersonStore and EstateStore must belong to the same world.");
            return false;
        }

        if (transition == null
            || transition.EstateId == null
            || transition.DeceasedPersonId == null
            || transition.ExpectedPerson == null
            || transition.ExpectedDeathAbsoluteDay < 0L
            || transition.OpeningAbsoluteDay < 0L
            || transition.ExpectedEstateStoreRevision < 0L)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidTransition,
                "A valid estate opening transition is required.");
            return false;
        }

        if (estateStore.Revision != transition.ExpectedEstateStoreRevision)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.StaleEstateStore,
                "The estate store changed after the transition was proposed.");
            return false;
        }

        if (personStore.TryGet(transition.DeceasedPersonId, out PersonRuntime person) == false)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.PersonNotRegistered,
                "The deceased Person is no longer registered in the world.");
            return false;
        }

        if (ReferenceEquals(person, transition.ExpectedPerson) == false
            || person.DeathAbsoluteDay != transition.ExpectedDeathAbsoluteDay)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.StalePersonRegistration,
                "The Person registration or death fact changed after proposal.");
            return false;
        }

        if (transition.OpeningAbsoluteDay < person.DeathAbsoluteDay.Value)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidOpeningDay,
                "OpeningAbsoluteDay cannot precede the Person death day.");
            return false;
        }

        EstateRecord record = new EstateRecord(
            transition.EstateId,
            transition.DeceasedPersonId,
            transition.OpeningAbsoluteDay);
        return estateStore.TryRegister(record, out failure);
    }

    public static bool TryOpenEstate(
        PersonStore personStore,
        EstateStore estateStore,
        EstateId estateId,
        PersonId deceasedPersonId,
        long openingAbsoluteDay,
        out EstateRecord estate,
        out EstateFoundationFailure failure)
    {
        estate = null;
        if (TryProposeOpening(
                personStore,
                estateStore,
                estateId,
                deceasedPersonId,
                openingAbsoluteDay,
                out EstateOpeningTransition transition,
                out failure) == false
            || TryApplyOpening(personStore, estateStore, transition, out failure) == false)
        {
            return false;
        }

        estateStore.TryGet(estateId, out estate);
        return estate != null;
    }
}
