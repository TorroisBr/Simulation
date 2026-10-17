using System;

/// <summary>
/// Narrow Unity-adapter access to PersonRuntime's internal lifecycle and staged
/// owner operations. PersonRuntime remains the sole owner of these values.
/// </summary>
public static class PersonRuntimeOwnerBridge
{
    public static long GetLifeResidenceRevision(PersonRuntime person)
    {
        if (person == null) throw new ArgumentNullException(nameof(person));
        return person.LifeResidenceRevision;
    }

    public static void BindP12LifecycleMutationBoundary(
        PersonRuntime person,
        Func<PersonRuntime, bool> admission,
        Action<PersonRuntime> committed)
    {
        if (person == null) throw new ArgumentNullException(nameof(person));
        person.BindP12LifecycleMutationBoundary(admission, committed);
    }

    public static bool IsP12LifecycleBound(PersonRuntime person)
    {
        if (person == null) throw new ArgumentNullException(nameof(person));
        return person.IsP12LifecycleBound;
    }

    public static bool CanAdvanceP12LifeResidenceRevision(PersonRuntime person, int increments = 1)
    {
        if (person == null) throw new ArgumentNullException(nameof(person));
        return person.CanAdvanceP12LifeResidenceRevision(increments);
    }

    public static bool CanCommitP12LifeResidenceMutation(PersonRuntime person)
    {
        if (person == null) throw new ArgumentNullException(nameof(person));
        return person.CanCommitP12LifeResidenceMutation();
    }

    public static PersonRuntime CreateForOwnerSnapshot(
        PersonId personId,
        long? birthAbsoluteDay,
        long? deathAbsoluteDay,
        string residenceSettlementRuntimeId,
        string materializedNpcRuntimeId,
        long lifeResidenceRevision)
    {
        return PersonRuntime.CreateForOwnerSnapshot(
            personId,
            birthAbsoluteDay,
            deathAbsoluteDay,
            residenceSettlementRuntimeId,
            materializedNpcRuntimeId,
            lifeResidenceRevision);
    }

    public static bool TryRecordDeath(PersonRuntime person, long absoluteDay)
    {
        if (person == null) throw new ArgumentNullException(nameof(person));
        return person.TryRecordDeath(absoluteDay);
    }

    public static bool RecordDeathAfterValidation(PersonRuntime person, long absoluteDay)
    {
        if (person == null) throw new ArgumentNullException(nameof(person));
        return person.RecordDeathAfterValidation(absoluteDay);
    }

    public static bool TryBindMaterializedNpc(PersonRuntime person, string npcRuntimeId)
    {
        if (person == null) throw new ArgumentNullException(nameof(person));
        return person.TryBindMaterializedNpc(npcRuntimeId);
    }

    public static bool TryUnbindMaterializedNpc(PersonRuntime person, string npcRuntimeId)
    {
        if (person == null) throw new ArgumentNullException(nameof(person));
        return person.TryUnbindMaterializedNpc(npcRuntimeId);
    }

    public static bool TrySetResidenceSettlementRuntimeId(PersonRuntime person, string settlementRuntimeId)
    {
        if (person == null) throw new ArgumentNullException(nameof(person));
        return person.TrySetResidenceSettlementRuntimeId(settlementRuntimeId);
    }
}
