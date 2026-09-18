using System;
using System.Collections.Generic;

public enum PersonStoreFailure
{
    None = 0,
    InvalidPerson = 1,
    DuplicatePersonId = 2,
    PersonNotRegistered = 3,
    AlreadyMaterialized = 4,
    InvalidNpcRuntimeId = 5,
    NpcAlreadyBoundToAnotherPerson = 6,
    BirthAbsoluteDayInFuture = 7
}

/// <summary>
/// World-owned registry of lightweight Persons and their optional materialization binding.
/// </summary>
public sealed class PersonStore
{
    private readonly Dictionary<PersonId, PersonRuntime> personsById =
        new Dictionary<PersonId, PersonRuntime>();
    private readonly Dictionary<string, PersonRuntime> personsByNpcRuntimeId =
        new Dictionary<string, PersonRuntime>(StringComparer.Ordinal);
    private readonly List<PersonRuntime> persons = new List<PersonRuntime>();
    private readonly IReadOnlyList<PersonRuntime> personSnapshot;

    public IReadOnlyList<PersonRuntime> Persons => personSnapshot;

    public PersonStore()
    {
        personSnapshot = persons.AsReadOnly();
    }

    public bool TryRegister(PersonRuntime person, out PersonStoreFailure failure)
    {
        failure = PersonStoreFailure.None;

        if (person == null || person.PersonId == null)
        {
            failure = PersonStoreFailure.InvalidPerson;
            return false;
        }

        if (personsById.ContainsKey(person.PersonId) == true)
        {
            failure = PersonStoreFailure.DuplicatePersonId;
            return false;
        }

        personsById.Add(person.PersonId, person);
        persons.Add(person);
        return true;
    }

    public bool TryGet(PersonId personId, out PersonRuntime person)
    {
        person = null;
        return personId != null && personsById.TryGetValue(personId, out person);
    }

    public bool TryGetByMaterializedNpcRuntimeId(
        string npcRuntimeId,
        out PersonRuntime person)
    {
        person = null;
        return string.IsNullOrWhiteSpace(npcRuntimeId) == false
            && personsByNpcRuntimeId.TryGetValue(npcRuntimeId, out person);
    }

    internal bool TryBindMaterializedNpc(
        PersonId personId,
        string npcRuntimeId,
        out PersonStoreFailure failure)
    {
        failure = PersonStoreFailure.None;

        if (personId == null)
        {
            failure = PersonStoreFailure.InvalidPerson;
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            failure = PersonStoreFailure.InvalidNpcRuntimeId;
            return false;
        }

        if (personsById.TryGetValue(personId, out PersonRuntime person) == false)
        {
            failure = PersonStoreFailure.PersonNotRegistered;
            return false;
        }

        if (person.IsMaterialized == true)
        {
            failure = PersonStoreFailure.AlreadyMaterialized;
            return false;
        }

        if (personsByNpcRuntimeId.ContainsKey(npcRuntimeId) == true)
        {
            failure = PersonStoreFailure.NpcAlreadyBoundToAnotherPerson;
            return false;
        }

        if (person.TryBindMaterializedNpc(npcRuntimeId) == false)
        {
            failure = PersonStoreFailure.AlreadyMaterialized;
            return false;
        }

        personsByNpcRuntimeId.Add(npcRuntimeId, person);
        return true;
    }

    internal bool TryUnbindMaterializedNpc(PersonId personId, string npcRuntimeId)
    {
        if (personId == null || string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            return false;
        }

        if (personsById.TryGetValue(personId, out PersonRuntime person) == false
            || person.TryUnbindMaterializedNpc(npcRuntimeId) == false)
        {
            return false;
        }

        personsByNpcRuntimeId.Remove(npcRuntimeId);
        return true;
    }
}
