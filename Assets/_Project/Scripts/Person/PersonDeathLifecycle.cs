using System;

public enum PersonDeathLifecycleFailure
{
    None = 0,
    InvalidWorld = 1,
    InvalidPersonId = 2,
    PersonNotRegistered = 3,
    PersonAlreadyDead = 4,
    StaleWorldDay = 5,
    StaleMaterialization = 6,
    MaterializedNpcMissing = 7,
    PersonNpcBindingMismatch = 8,
    ExecutionMirrorAlreadyDead = 9,
    InvalidTransition = 10,
    StalePersonRegistration = 11
}

/// <summary>
/// Immutable proposal for one factual Person death. The proposal captures the
/// world day and optional materialized execution mirror so apply can reject
/// stale state without relying on a mutable Person life-state enum.
/// </summary>
public sealed class PersonDeathTransition : IEquatable<PersonDeathTransition>
{
    internal PersonRuntime ExpectedPerson { get; }
    public PersonId PersonId { get; }
    public long ExpectedAbsoluteDay { get; }
    public long DeathAbsoluteDay => ExpectedAbsoluteDay;
    public string ExpectedMaterializedNpcRuntimeId { get; }
    public bool ExpectsMaterializedNpc =>
        string.IsNullOrWhiteSpace(ExpectedMaterializedNpcRuntimeId) == false;

    internal PersonDeathTransition(
        PersonRuntime expectedPerson,
        long expectedAbsoluteDay,
        string expectedMaterializedNpcRuntimeId)
    {
        ExpectedPerson = expectedPerson;
        PersonId = expectedPerson?.PersonId;
        ExpectedAbsoluteDay = expectedAbsoluteDay;
        ExpectedMaterializedNpcRuntimeId = expectedMaterializedNpcRuntimeId;
    }

    public bool Equals(PersonDeathTransition other)
    {
        return ReferenceEquals(other, null) == false
            && Equals(PersonId, other.PersonId)
            && ExpectedAbsoluteDay == other.ExpectedAbsoluteDay
            && string.Equals(
                ExpectedMaterializedNpcRuntimeId,
                other.ExpectedMaterializedNpcRuntimeId,
                StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PersonDeathTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = PersonId != null ? PersonId.GetHashCode() : 0;
            hash = (hash * 397) ^ ExpectedAbsoluteDay.GetHashCode();
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(
                ExpectedMaterializedNpcRuntimeId ?? string.Empty);
            return hash;
        }
    }
}

/// <summary>
/// World-owned mutation boundary for factual Person death. It does not change
/// aggregate population, residence, institutions, genealogy, property, or time.
/// A bound NpcRuntime is updated in the same validated commit as an execution
/// mirror only.
/// </summary>
public static class PersonDeathLifecycleSystem
{
    public static bool TryProposeDeath(
        SimulationRuntime world,
        PersonId personId,
        out PersonDeathTransition transition,
        out PersonDeathLifecycleFailure failure)
    {
        transition = null;
        if (TryResolveLivingPerson(
                world,
                personId,
                out PersonRuntime person,
                out NpcRuntime materializedNpc,
                out failure) == false)
        {
            return false;
        }

        transition = new PersonDeathTransition(
            person,
            world.CurrentDay,
            materializedNpc?.RuntimeId);
        return true;
    }

    public static bool TryApplyDeath(
        SimulationRuntime world,
        PersonId personId,
        out PersonDeathTransition transition,
        out PersonDeathLifecycleFailure failure)
    {
        if (TryProposeDeath(world, personId, out transition, out failure) == false)
        {
            return false;
        }

        return TryApplyDeath(world, transition, out failure);
    }

    public static bool TryApplyDeath(
        SimulationRuntime world,
        PersonDeathTransition transition,
        out PersonDeathLifecycleFailure failure)
    {
        failure = PersonDeathLifecycleFailure.None;
        if (world == null)
        {
            failure = PersonDeathLifecycleFailure.InvalidWorld;
            return false;
        }

        if (transition == null
            || transition.PersonId == null
            || transition.ExpectedPerson == null
            || transition.ExpectedAbsoluteDay < 0L)
        {
            failure = PersonDeathLifecycleFailure.InvalidTransition;
            return false;
        }

        if (world.CurrentDay != transition.ExpectedAbsoluteDay)
        {
            failure = PersonDeathLifecycleFailure.StaleWorldDay;
            return false;
        }

        if (TryResolveLivingPerson(
                world,
                transition.PersonId,
                out PersonRuntime person,
                out NpcRuntime materializedNpc,
                out failure) == false)
        {
            return false;
        }

        if (ReferenceEquals(person, transition.ExpectedPerson) == false)
        {
            failure = PersonDeathLifecycleFailure.StalePersonRegistration;
            return false;
        }

        string currentNpcRuntimeId = materializedNpc?.RuntimeId;
        if (string.Equals(
                currentNpcRuntimeId,
                transition.ExpectedMaterializedNpcRuntimeId,
                StringComparison.Ordinal) == false)
        {
            failure = PersonDeathLifecycleFailure.StaleMaterialization;
            return false;
        }

        // Every fallible check is complete before either representation mutates.
        if (person.TryRecordDeath(transition.DeathAbsoluteDay) == false)
        {
            failure = PersonDeathLifecycleFailure.InvalidTransition;
            return false;
        }

        materializedNpc?.ApplyPersonDeathAfterValidation();
        return true;
    }

    private static bool TryResolveLivingPerson(
        SimulationRuntime world,
        PersonId personId,
        out PersonRuntime person,
        out NpcRuntime materializedNpc,
        out PersonDeathLifecycleFailure failure)
    {
        person = null;
        materializedNpc = null;
        failure = PersonDeathLifecycleFailure.None;

        if (world == null)
        {
            failure = PersonDeathLifecycleFailure.InvalidWorld;
            return false;
        }

        if (personId == null)
        {
            failure = PersonDeathLifecycleFailure.InvalidPersonId;
            return false;
        }

        if (world.PersonStore.TryGet(personId, out person) == false)
        {
            failure = PersonDeathLifecycleFailure.PersonNotRegistered;
            return false;
        }

        if (person.DeathAbsoluteDay.HasValue)
        {
            failure = PersonDeathLifecycleFailure.PersonAlreadyDead;
            return false;
        }

        if (person.IsMaterialized == false)
        {
            return true;
        }

        if (world.TryGetNpcRuntime(person.MaterializedNpcRuntimeId, out materializedNpc) == false)
        {
            failure = PersonDeathLifecycleFailure.MaterializedNpcMissing;
            return false;
        }

        if (materializedNpc.PersonId != person.PersonId
            || ReferenceEquals(materializedNpc.BoundPersonRuntime, person) == false)
        {
            failure = PersonDeathLifecycleFailure.PersonNpcBindingMismatch;
            return false;
        }

        if (materializedNpc.IsAlive == false)
        {
            failure = PersonDeathLifecycleFailure.ExecutionMirrorAlreadyDead;
            return false;
        }

        return true;
    }
}
