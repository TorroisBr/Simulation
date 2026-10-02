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
    BirthAbsoluteDayInFuture = 7,
    DeathAbsoluteDayInFuture = 8,
    RuntimeFaulted = 9,
    RevisionOverflow = 10
}

/// <summary>
/// World-owned registry of lightweight Persons and their optional materialization binding.
/// </summary>
public sealed class PersonStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private FactualReadAdmission factualReadAdmission;
    private readonly Dictionary<PersonId, PersonRuntime> personsById =
        new Dictionary<PersonId, PersonRuntime>();
    private readonly Dictionary<string, PersonRuntime> personsByNpcRuntimeId =
        new Dictionary<string, PersonRuntime>(StringComparer.Ordinal);
    private readonly List<PersonRuntime> persons = new List<PersonRuntime>();
    private readonly IReadOnlyList<PersonRuntime> personSnapshot;
    private long revision;

    public IReadOnlyList<PersonRuntime> Persons => personSnapshot;
    public int MaterializedBindingCount => personsByNpcRuntimeId.Count;
    public long Revision => revision;

    public PersonStore()
    {
        personSnapshot = persons.AsReadOnly();
    }

    public bool TryRegister(PersonRuntime person, out PersonStoreFailure failure)
    {
        failure = PersonStoreFailure.None;

        if (!mutationGuardBinding.CanMutate || !CanMutateThroughFactualReadAdmission())
        {
            failure = PersonStoreFailure.RuntimeFaulted;
            return false;
        }

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

        if (CanAdvanceRevisionForForwardWrite() == false)
        {
            failure = PersonStoreFailure.RevisionOverflow;
            return false;
        }

        personsById.Add(person.PersonId, person);
        persons.Add(person);
        revision++;
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

    /// <summary>
    /// Removes exactly the registration created for an operation that has not
    /// yet become externally observable. This is intentionally internal: the
    /// world must not expose arbitrary Person deletion as a public mutation.
    /// </summary>
    internal bool TryRollbackRegistration(PersonRuntime person)
    {
        if (!CanMutateThroughFactualReadAdmission()
            || person == null
            || person.IsMaterialized == true
            || personsById.TryGetValue(person.PersonId, out PersonRuntime registeredPerson) == false
            || ReferenceEquals(registeredPerson, person) == false)
        {
            return false;
        }

        if (CanAdvanceRevision() == false)
        {
            return false;
        }

        personsById.Remove(person.PersonId);
        persons.Remove(person);
        revision++;
        return true;
    }

    public int CountResidents(string settlementRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(settlementRuntimeId) == true)
        {
            return 0;
        }

        int count = 0;
        foreach (PersonRuntime person in persons)
        {
            if (person != null
                && string.Equals(
                    person.ResidenceSettlementRuntimeId,
                    settlementRuntimeId,
                    StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    public IReadOnlyList<PersonRuntime> GetResidents(string settlementRuntimeId)
    {
        List<PersonRuntime> residents = new List<PersonRuntime>();
        if (string.IsNullOrWhiteSpace(settlementRuntimeId) == true)
        {
            return residents.AsReadOnly();
        }

        foreach (PersonRuntime person in persons)
        {
            if (person != null
                && string.Equals(
                    person.ResidenceSettlementRuntimeId,
                    settlementRuntimeId,
                    StringComparison.Ordinal))
            {
                residents.Add(person);
            }
        }

        return residents.AsReadOnly();
    }

    internal bool TryBindMaterializedNpc(
        PersonId personId,
        string npcRuntimeId,
        out PersonStoreFailure failure)
    {
        failure = PersonStoreFailure.None;

        if (!mutationGuardBinding.CanMutate || !CanMutateThroughFactualReadAdmission())
        {
            failure = PersonStoreFailure.RuntimeFaulted;
            return false;
        }

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

        if (CanAdvanceRevisionForForwardWrite() == false)
        {
            failure = PersonStoreFailure.RevisionOverflow;
            return false;
        }

        if (person.TryBindMaterializedNpc(npcRuntimeId) == false)
        {
            failure = PersonStoreFailure.AlreadyMaterialized;
            return false;
        }

        personsByNpcRuntimeId.Add(npcRuntimeId, person);
        revision++;
        return true;
    }

    internal bool TryRollbackMaterializedNpcBinding(PersonId personId, string npcRuntimeId)
    {
        if (!mutationGuardBinding.CanMutate || !CanMutateThroughFactualReadAdmission())
        {
            return false;
        }

        if (personId == null || string.IsNullOrWhiteSpace(npcRuntimeId) == true)
        {
            return false;
        }

        if (personsById.TryGetValue(personId, out PersonRuntime person) == false
            || personsByNpcRuntimeId.TryGetValue(npcRuntimeId, out PersonRuntime indexedPerson) == false
            || ReferenceEquals(indexedPerson, person) == false
            || CanAdvanceRevision() == false
            || person.TryUnbindMaterializedNpc(npcRuntimeId) == false)
        {
            return false;
        }

        personsByNpcRuntimeId.Remove(npcRuntimeId);
        revision++;
        return true;
    }

    private bool CanAdvanceRevisionForForwardWrite()
    {
        return revision < long.MaxValue - 1L;
    }

    private bool CanAdvanceRevision()
    {
        return revision < long.MaxValue;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.TryBindTo(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }

    internal bool CanBindFactualReadAdmission(FactualReadAdmission admission)
    {
        return admission != null
            && (factualReadAdmission == null || ReferenceEquals(factualReadAdmission, admission));
    }

    internal bool TryBindFactualReadAdmission(FactualReadAdmission admission)
    {
        if (!CanBindFactualReadAdmission(admission))
            return false;
        if (factualReadAdmission == null)
            factualReadAdmission = admission;
        return true;
    }

    internal void TryUnbindFactualReadAdmission(FactualReadAdmission admission)
    {
        if (ReferenceEquals(factualReadAdmission, admission))
            factualReadAdmission = null;
    }

    private bool CanMutateThroughFactualReadAdmission()
    {
        return factualReadAdmission == null
            || factualReadAdmission.CanMutatePersonStore(this);
    }
}
