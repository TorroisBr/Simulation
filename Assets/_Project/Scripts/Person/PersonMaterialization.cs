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
    ResidenceConflict = 13,
    PersonDead = 14,
    LifeStateConflict = 15,
    RuntimeFaulted = 16,
    RevisionOverflow = 17
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

        if (world.IsMutationFaulted)
        {
            failure = PersonMaterializationFailure.RuntimeFaulted;
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

        if (person.IsDeadAt(world.CurrentDay))
        {
            failure = PersonMaterializationFailure.PersonDead;
            return false;
        }

        if (world.TryGetNpcRuntime(runtimeId, out _))
        {
            failure = PersonMaterializationFailure.DuplicateNpcRuntimeId;
            return false;
        }

        if (world.TryCreateNpcMaterializationCandidate(
                npcData,
                runtimeId,
                initialMoney,
                out NpcRuntime candidate,
                out failure) == false)
        {
            return false;
        }
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
        world.MarkNpcMembershipPersonStoreRevisionCommitted();

        if (world.TryRegisterNpc(candidate, out WorldNpcRegistryFailure registryFailure) == false)
        {
            bool bindingRestored = world.PersonStore.TryRollbackMaterializedNpcBinding(personId, runtimeId);
            if (bindingRestored) world.MarkNpcMembershipPersonStoreRevisionCommitted();
            candidate.ClearPersonRuntime();
            candidate.ClearPersonId();
            if (!bindingRestored) world.MarkNpcMembershipCensusCompensationFailed();
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
                bool bindingRestored = world.PersonStore.TryRollbackMaterializedNpcBinding(personId, runtimeId);
                if (bindingRestored) world.MarkNpcMembershipPersonStoreRevisionCommitted();
                candidate.ClearPersonRuntime();
                candidate.ClearPersonId();
                bool npcUnregistered = world.TryUnregisterNpc(runtimeId, out _);
                if (!bindingRestored || !npcUnregistered)
                    world.MarkNpcMembershipCensusCompensationFailed();
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

        if (world.IsMutationFaulted)
        {
            failure = PersonMaterializationFailure.RuntimeFaulted;
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

        if (person.IsDeadAt(world.CurrentDay) != npcRuntime.IsDead)
        {
            failure = PersonMaterializationFailure.LifeStateConflict;
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
        world.MarkNpcMembershipPersonStoreRevisionCommitted();

        if (string.IsNullOrWhiteSpace(personResidence) == true
            && string.IsNullOrWhiteSpace(npcResidence) == false
            && PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(person, npcResidence) == false)
        {
            bool bindingRestored = world.PersonStore.TryRollbackMaterializedNpcBinding(personId, npcRuntimeId);
            if (bindingRestored) world.MarkNpcMembershipPersonStoreRevisionCommitted();
            npcRuntime.ClearPersonRuntime();
            npcRuntime.ClearPersonId();
            if (!bindingRestored) world.MarkNpcMembershipCensusCompensationFailed();
            failure = PersonMaterializationFailure.ResidenceConflict;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies the selected Daily-v1 Core binding operation while retaining
    /// only the Unity NpcRuntime identity/reference projection in this host
    /// adapter. The caller owns the enclosing P12 membership reservation and
    /// publishes the derived actor-roster projection before invoking this.
    /// </summary>
    internal static bool TryBindExistingNpcToPersonSelectedCore(
        SimulationRuntime world,
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonId personId,
        string npcRuntimeId,
        out PersonMaterializationFailure failure)
    {
        failure = PersonMaterializationFailure.RuntimeFaulted;
        if (world == null || graph == null
            || !ReferenceEquals(graph.PersonStore, world.PersonStore))
            return false;
        if (world.IsMutationFaulted)
            return false;
        if (personId == null)
        {
            failure = PersonMaterializationFailure.InvalidPerson;
            return false;
        }
        if (!world.TryGetNpcRuntime(npcRuntimeId, out NpcRuntime npcRuntime))
        {
            failure = PersonMaterializationFailure.NpcNotRegistered;
            return false;
        }
        if (!npcRuntime.UsesSelectedDailyV1Definitions
            || !ReferenceEquals(npcRuntime.SelectedRuntimeOwnerGraph, graph)
            || !graph.TryGetNpcOwner(npcRuntimeId, out DailyV1NpcRuntimeOwners npcOwner)
            || !ReferenceEquals(npcOwner, npcRuntime.SelectedRuntimeOwners))
            return false;

        if (npcRuntime.PersonId != null)
        {
            failure = npcRuntime.PersonId == personId
                ? PersonMaterializationFailure.AlreadyMaterialized
                : PersonMaterializationFailure.NpcAlreadyBoundToDifferentPerson;
            return false;
        }

        if (!DailyV1SelectedNpcPersonBindingOperation.TryPrepare(
                graph, world.PersonStore, personId, npcRuntimeId, world.CurrentDay,
                out DailyV1SelectedNpcPersonBindingPlan plan,
                out DailyV1SelectedNpcPersonBindingFailure prepareFailure))
        {
            failure = MapSelectedBindingFailure(prepareFailure);
            return false;
        }

        if (!npcRuntime.TryAssignPersonId(personId))
        {
            failure = PersonMaterializationFailure.NpcAlreadyBoundToDifferentPerson;
            return false;
        }
        if (!npcRuntime.TryBindPersonRuntime(plan.Person))
        {
            npcRuntime.ClearPersonId();
            failure = PersonMaterializationFailure.NpcAlreadyBoundToDifferentPerson;
            return false;
        }

        bool committed = DailyV1SelectedNpcPersonBindingOperation.TryCommit(
            graph, world.PersonStore, plan, world.CurrentDay,
            out DailyV1SelectedNpcPersonBindingFailure commitFailure,
            out int personStoreRevisionDelta,
            out bool compensationFailed);
        for (int i = 0; i < personStoreRevisionDelta; i++)
            world.MarkNpcMembershipPersonStoreRevisionCommitted();

        if (!committed)
        {
            npcRuntime.ClearPersonRuntime();
            npcRuntime.ClearPersonId();
            if (compensationFailed)
                world.MarkNpcMembershipCensusCompensationFailed();
            failure = MapSelectedBindingFailure(commitFailure);
            return false;
        }

        failure = PersonMaterializationFailure.None;
        return true;
    }

    private static PersonMaterializationFailure MapSelectedBindingFailure(
        DailyV1SelectedNpcPersonBindingFailure failure)
    {
        switch (failure)
        {
            case DailyV1SelectedNpcPersonBindingFailure.None:
                return PersonMaterializationFailure.None;
            case DailyV1SelectedNpcPersonBindingFailure.InvalidPerson:
                return PersonMaterializationFailure.InvalidPerson;
            case DailyV1SelectedNpcPersonBindingFailure.InvalidNpcRuntimeId:
                return PersonMaterializationFailure.InvalidRuntimeId;
            case DailyV1SelectedNpcPersonBindingFailure.NpcNotRegistered:
                return PersonMaterializationFailure.NpcNotRegistered;
            case DailyV1SelectedNpcPersonBindingFailure.PersonNotRegistered:
                return PersonMaterializationFailure.PersonNotRegistered;
            case DailyV1SelectedNpcPersonBindingFailure.AlreadyMaterialized:
                return PersonMaterializationFailure.AlreadyMaterialized;
            case DailyV1SelectedNpcPersonBindingFailure.NpcAlreadyBoundToDifferentPerson:
                return PersonMaterializationFailure.NpcAlreadyBoundToDifferentPerson;
            case DailyV1SelectedNpcPersonBindingFailure.LifeStateConflict:
                return PersonMaterializationFailure.LifeStateConflict;
            case DailyV1SelectedNpcPersonBindingFailure.ResidenceConflict:
                return PersonMaterializationFailure.ResidenceConflict;
            case DailyV1SelectedNpcPersonBindingFailure.RevisionOverflow:
                return PersonMaterializationFailure.RevisionOverflow;
            default:
                return PersonMaterializationFailure.RuntimeFaulted;
        }
    }

    private static PersonMaterializationFailure MapStoreFailure(PersonStoreFailure failure)
    {
        switch (failure)
        {
            case PersonStoreFailure.RuntimeFaulted:
                return PersonMaterializationFailure.RuntimeFaulted;
            case PersonStoreFailure.PersonNotRegistered:
                return PersonMaterializationFailure.PersonNotRegistered;
            case PersonStoreFailure.AlreadyMaterialized:
                return PersonMaterializationFailure.AlreadyMaterialized;
            case PersonStoreFailure.NpcAlreadyBoundToAnotherPerson:
                return PersonMaterializationFailure.NpcAlreadyBoundToAnotherPerson;
            case PersonStoreFailure.RevisionOverflow:
                return PersonMaterializationFailure.RevisionOverflow;
            default:
                return PersonMaterializationFailure.RosterRegistrationFailed;
        }
    }
}
