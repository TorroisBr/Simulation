using System;

public enum PersonMaterializationFailure
{
    None = 0,
    InvalidWorld = 1,
    InvalidPerson = 2,
    PersonNotRegistered = 3,
    AlreadyMaterialized = 4,
    InvalidNpcDefinition = 5,
    InvalidRuntimeId = 6,
    DuplicateNpcRuntimeId = 7,
    NpcAlreadyBoundToAnotherPerson = 8,
    NpcAlreadyBoundToDifferentPerson = 9,
    RosterRegistrationFailed = 10,
    InvalidStartingContext = 11,
    NpcNotRegistered = 12,
    ResidenceConflict = 13
}

/// <summary>
/// Explicit atomic boundary for Person to NpcRuntime materialization and legacy adoption.
/// </summary>
public static class PersonMaterializationSystem
{
    public static bool TryMaterializePerson(
        SimulationRuntime world,
        PersonId personId,
        NpcData npcData,
        string runtimeId,
        CityRuntime startingCity,
        float initialMoney,
        out NpcRuntime npcRuntime,
        out PersonMaterializationFailure failure)
    {
        npcRuntime = null;
        failure = PersonMaterializationFailure.None;

        if (world == null)
        {
            failure = PersonMaterializationFailure.InvalidWorld;
            return false;
        }

        if (personId == null)
        {
            failure = PersonMaterializationFailure.InvalidPerson;
            return false;
        }

        if (npcData == null)
        {
            failure = PersonMaterializationFailure.InvalidNpcDefinition;
            return false;
        }

        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            failure = PersonMaterializationFailure.InvalidRuntimeId;
            return false;
        }

        if (world.PersonStore.TryGet(personId, out PersonRuntime person) == false)
        {
            failure = PersonMaterializationFailure.PersonNotRegistered;
            return false;
        }

        if (person.IsMaterialized == true)
        {
            failure = PersonMaterializationFailure.AlreadyMaterialized;
            return false;
        }

        if (world.TryGetNpcRuntime(runtimeId, out _))
        {
            failure = PersonMaterializationFailure.DuplicateNpcRuntimeId;
            return false;
        }

        NpcRuntime candidate = new NpcRuntime(runtimeId, npcData, null, initialMoney);
        if (candidate.TryAssignPersonId(personId) == false)
        {
            failure = PersonMaterializationFailure.NpcAlreadyBoundToDifferentPerson;
            return false;
        }

        if (candidate.TryBindPersonRuntime(person) == false)
        {
            candidate.ClearPersonId();
            failure = PersonMaterializationFailure.NpcAlreadyBoundToDifferentPerson;
            return false;
        }

        if (world.PersonStore.TryBindMaterializedNpc(
                personId,
                runtimeId,
                out PersonStoreFailure storeFailure) == false)
        {
            failure = MapStoreFailure(storeFailure);
            return false;
        }

        if (world.TryRegisterNpc(candidate, out WorldNpcRegistryFailure registryFailure) == false)
        {
            world.PersonStore.TryUnbindMaterializedNpc(personId, runtimeId);
            candidate.ClearPersonRuntime();
            candidate.ClearPersonId();
            failure = registryFailure == WorldNpcRegistryFailure.DuplicateRuntimeId
                ? PersonMaterializationFailure.DuplicateNpcRuntimeId
                : PersonMaterializationFailure.RosterRegistrationFailed;
            return false;
        }

        if (startingCity != null)
        {
            startingCity.AddImportantNpc(candidate);
            if (candidate.CurrentCity != startingCity)
            {
                world.PersonStore.TryUnbindMaterializedNpc(personId, runtimeId);
                candidate.ClearPersonRuntime();
                candidate.ClearPersonId();
                world.TryUnregisterNpc(runtimeId, out _);
                failure = PersonMaterializationFailure.InvalidStartingContext;
                return false;
            }
        }

        npcRuntime = candidate;
        return true;
    }

    public static bool TryBindExistingNpcToPerson(
        SimulationRuntime world,
        PersonId personId,
        string npcRuntimeId,
        out PersonMaterializationFailure failure)
    {
        failure = PersonMaterializationFailure.None;

        if (world == null)
        {
            failure = PersonMaterializationFailure.InvalidWorld;
            return false;
        }

        if (personId == null)
        {
            failure = PersonMaterializationFailure.InvalidPerson;
            return false;
        }

        if (world.TryGetNpcRuntime(npcRuntimeId, out NpcRuntime npcRuntime) == false)
        {
            failure = PersonMaterializationFailure.NpcNotRegistered;
            return false;
        }

        if (npcRuntime.PersonId != null)
        {
            failure = npcRuntime.PersonId == personId
                ? PersonMaterializationFailure.AlreadyMaterialized
                : PersonMaterializationFailure.NpcAlreadyBoundToDifferentPerson;
            return false;
        }

        if (world.PersonStore.TryGet(personId, out PersonRuntime person) == false)
        {
            failure = PersonMaterializationFailure.PersonNotRegistered;
            return false;
        }

        string npcResidence = npcRuntime.ResidenceSettlementRuntimeId;
        string personResidence = person.ResidenceSettlementRuntimeId;
        if (string.IsNullOrWhiteSpace(npcResidence) == false
            && string.IsNullOrWhiteSpace(personResidence) == false
            && string.Equals(npcResidence, personResidence, StringComparison.Ordinal) == false)
        {
            failure = PersonMaterializationFailure.ResidenceConflict;
            return false;
        }

        if (npcRuntime.TryAssignPersonId(personId) == false)
        {
            failure = PersonMaterializationFailure.NpcAlreadyBoundToDifferentPerson;
            return false;
        }

        if (npcRuntime.TryBindPersonRuntime(person) == false)
        {
            npcRuntime.ClearPersonId();
            failure = PersonMaterializationFailure.NpcAlreadyBoundToDifferentPerson;
            return false;
        }

        if (world.PersonStore.TryBindMaterializedNpc(
                personId,
                npcRuntimeId,
                out PersonStoreFailure storeFailure) == false)
        {
            npcRuntime.ClearPersonRuntime();
            npcRuntime.ClearPersonId();
            failure = MapStoreFailure(storeFailure);
            return false;
        }

        if (string.IsNullOrWhiteSpace(personResidence) == true
            && string.IsNullOrWhiteSpace(npcResidence) == false
            && person.TrySetResidenceSettlementRuntimeId(npcResidence) == false)
        {
            world.PersonStore.TryUnbindMaterializedNpc(personId, npcRuntimeId);
            npcRuntime.ClearPersonRuntime();
            npcRuntime.ClearPersonId();
            failure = PersonMaterializationFailure.ResidenceConflict;
            return false;
        }

        return true;
    }

    private static PersonMaterializationFailure MapStoreFailure(PersonStoreFailure failure)
    {
        switch (failure)
        {
            case PersonStoreFailure.PersonNotRegistered:
                return PersonMaterializationFailure.PersonNotRegistered;
            case PersonStoreFailure.AlreadyMaterialized:
                return PersonMaterializationFailure.AlreadyMaterialized;
            case PersonStoreFailure.NpcAlreadyBoundToAnotherPerson:
                return PersonMaterializationFailure.NpcAlreadyBoundToAnotherPerson;
            default:
                return PersonMaterializationFailure.RosterRegistrationFailed;
        }
    }
}
