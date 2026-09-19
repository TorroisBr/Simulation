using System;

/// <summary>
/// Explicit downstream command boundary for creating one estate after factual
/// Person death. Person death itself never calls this system.
/// </summary>
public static class EstateCreationSystem
{
    public static bool TryPropose(
        PersonStore personStore,
        EstateStore estateStore,
        PersonId deceasedPersonId,
        EstateId estateId,
        long creationAbsoluteDay,
        out EstateCreationTransition transition,
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

        if (deceasedPersonId == null)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidDeceasedPersonId,
                "A deceased PersonId is required.");
            return false;
        }

        if (estateId == null)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidEstateId,
                "A valid EstateId is required.");
            return false;
        }

        if (creationAbsoluteDay < 0L)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidAbsoluteDay,
                "CreationAbsoluteDay cannot be negative.");
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
                EstateFoundationFailureCode.PersonNotDeceased,
                "An estate can be created only for a factually deceased Person.");
            return false;
        }

        if (person.DeathAbsoluteDay.Value > creationAbsoluteDay)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidAbsoluteDay,
                "CreationAbsoluteDay cannot precede factual Person death.");
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
                "The deceased Person already has an estate record.");
            return false;
        }

        if (estateStore.Revision == long.MaxValue)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.RevisionOverflow,
                "The estate store revision cannot advance further.");
            return false;
        }

        transition = new EstateCreationTransition(
            person,
            estateId,
            person.DeathAbsoluteDay.Value,
            creationAbsoluteDay,
            estateStore.Revision);
        return true;
    }

    public static bool TryPropose(
        PersonStore personStore,
        EstateStore estateStore,
        EstateId estateId,
        PersonId deceasedPersonId,
        long creationAbsoluteDay,
        out EstateCreationTransition transition,
        out EstateFoundationFailure failure)
    {
        return TryPropose(
            personStore,
            estateStore,
            deceasedPersonId,
            estateId,
            creationAbsoluteDay,
            out transition,
            out failure);
    }

    public static bool TryApply(
        PersonStore personStore,
        EstateStore estateStore,
        EstateCreationTransition transition,
        long currentAbsoluteDay,
        out EstateRecord estate,
        out EstateFoundationFailure failure)
    {
        estate = null;
        failure = EstateFoundationFailure.None;

        if (personStore == null || estateStore == null || transition == null)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidStore,
                "PersonStore, EstateStore, and transition are required.");
            return false;
        }

        if (transition.ExpectedPerson == null
            || transition.EstateId == null
            || transition.DeceasedPersonId == null
            || transition.ExpectedDeathAbsoluteDay < 0L
            || transition.CreatedAbsoluteDay < transition.ExpectedDeathAbsoluteDay
            || transition.ExpectedEstateStoreRevision < 0L)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidTransition,
                "A valid estate creation transition is required.");
            return false;
        }

        if (currentAbsoluteDay != transition.CreatedAbsoluteDay)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.StaleAbsoluteDay,
                "The world day changed after the estate transition was proposed.");
            return false;
        }

        if (estateStore.Revision != transition.ExpectedEstateStoreRevision)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.StaleEstateStore,
                "The estate store changed after the transition was proposed.");
            return false;
        }

        if (personStore.TryGet(
                transition.DeceasedPersonId,
                out PersonRuntime person) == false)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.PersonNotRegistered,
                "The deceased Person must remain registered while applying the transition.");
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

        EstateRecord candidate = new EstateRecord(
            transition.EstateId,
            transition.DeceasedPersonId,
            transition.ExpectedDeathAbsoluteDay,
            transition.CreatedAbsoluteDay);
        if (estateStore.TryRegister(candidate, out failure) == false)
        {
            return false;
        }

        estate = candidate;
        return true;
    }

    public static bool TryApply(
        PersonStore personStore,
        EstateStore estateStore,
        EstateCreationTransition transition,
        long currentAbsoluteDay,
        out EstateFoundationFailure failure)
    {
        return TryApply(
            personStore,
            estateStore,
            transition,
            currentAbsoluteDay,
            out _,
            out failure);
    }

    public static bool TryCreate(
        PersonStore personStore,
        EstateStore estateStore,
        PersonId deceasedPersonId,
        EstateId estateId,
        long creationAbsoluteDay,
        out EstateRecord estate,
        out EstateFoundationFailure failure)
    {
        estate = null;
        if (TryPropose(
                personStore,
                estateStore,
                deceasedPersonId,
                estateId,
                creationAbsoluteDay,
                out EstateCreationTransition transition,
                out failure) == false)
        {
            return false;
        }

        return TryApply(
            personStore,
            estateStore,
            transition,
            creationAbsoluteDay,
            out estate,
            out failure);
    }
}

/// <summary>
/// Explicit opening transition retained as a domain synonym for estate
/// creation. It carries the same stale Person and store guards.
/// </summary>
public sealed class EstateOpeningTransition : IEquatable<EstateOpeningTransition>
{
    internal PersonRuntime ExpectedPerson { get; }

    public EstateId EstateId { get; }

    public PersonId DeceasedPersonId { get; }

    public long ExpectedDeathAbsoluteDay { get; }

    public long OpeningAbsoluteDay { get; }

    public long ExpectedEstateStoreRevision { get; }

    internal EstateOpeningTransition(
        PersonRuntime expectedPerson,
        EstateId estateId,
        long openingAbsoluteDay,
        long expectedEstateStoreRevision)
    {
        ExpectedPerson = expectedPerson;
        EstateId = estateId;
        DeceasedPersonId = expectedPerson?.PersonId;
        ExpectedDeathAbsoluteDay = expectedPerson?.DeathAbsoluteDay ?? -1L;
        OpeningAbsoluteDay = openingAbsoluteDay;
        ExpectedEstateStoreRevision = expectedEstateStoreRevision;
    }

    public bool Equals(EstateOpeningTransition other)
    {
        return other != null
            && EstateId == other.EstateId
            && DeceasedPersonId == other.DeceasedPersonId
            && ExpectedDeathAbsoluteDay == other.ExpectedDeathAbsoluteDay
            && OpeningAbsoluteDay == other.OpeningAbsoluteDay
            && ExpectedEstateStoreRevision == other.ExpectedEstateStoreRevision;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as EstateOpeningTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = EstateId != null ? EstateId.GetHashCode() : 0;
            hash = (hash * 397) ^ (DeceasedPersonId != null ? DeceasedPersonId.GetHashCode() : 0);
            hash = (hash * 397) ^ ExpectedDeathAbsoluteDay.GetHashCode();
            hash = (hash * 397) ^ OpeningAbsoluteDay.GetHashCode();
            return (hash * 397) ^ ExpectedEstateStoreRevision.GetHashCode();
        }
    }
}

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
                EstateFoundationFailureCode.InvalidDeceasedPersonId,
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
                "An estate can only be opened for a factually dead Person.");
            return false;
        }

        if (openingAbsoluteDay < person.DeathAbsoluteDay.Value)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidOpeningDay,
                "OpeningAbsoluteDay cannot precede DeathAbsoluteDay.");
            return false;
        }

        if (estateStore.TryGet(estateId, out _)
            || estateStore.TryGetByDeceasedPerson(deceasedPersonId, out _))
        {
            failure = EstateFoundationFailure.Create(
                estateStore.TryGet(estateId, out _)
                    ? EstateFoundationFailureCode.DuplicateEstateId
                    : EstateFoundationFailureCode.EstateAlreadyExistsForPerson,
                "The estate identity is already registered.");
            return false;
        }

        if (estateStore.Revision == long.MaxValue)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.RevisionOverflow,
                "The estate store revision cannot advance further.");
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
        if (personStore == null || estateStore == null || transition == null)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidTransition,
                "A valid estate opening transition is required.");
            return false;
        }

        if (transition.ExpectedPerson == null
            || transition.EstateId == null
            || transition.DeceasedPersonId == null
            || transition.ExpectedDeathAbsoluteDay < 0L
            || transition.OpeningAbsoluteDay < transition.ExpectedDeathAbsoluteDay
            || transition.ExpectedEstateStoreRevision < 0L)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidTransition,
                "The estate opening transition is malformed.");
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
                "The deceased Person must remain registered while applying the transition.");
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

        EstateRecord record = new EstateRecord(
            transition.EstateId,
            transition.DeceasedPersonId,
            transition.ExpectedDeathAbsoluteDay,
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

        return estateStore.TryGet(estateId, out estate);
    }
}
