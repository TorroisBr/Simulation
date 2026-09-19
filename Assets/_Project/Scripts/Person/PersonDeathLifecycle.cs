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
    StalePersonRegistration = 11,
    InvalidInjury = 12,
    InvalidDeathDay = 13,
    ResidenceSettlementMissing = 14
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
    public string ExpectedResidenceSettlementRuntimeId { get; }
    public long ExpectedResidencePopulationRevision { get; }
    public int ExpectedResidencePopulation { get; }
    public bool ExpectsResident =>
        string.IsNullOrWhiteSpace(ExpectedResidenceSettlementRuntimeId) == false;

    internal PersonDeathTransition(
        PersonRuntime expectedPerson,
        long expectedAbsoluteDay,
        string expectedMaterializedNpcRuntimeId,
        string expectedResidenceSettlementRuntimeId,
        long expectedResidencePopulationRevision,
        int expectedResidencePopulation)
    {
        ExpectedPerson = expectedPerson;
        PersonId = expectedPerson?.PersonId;
        ExpectedAbsoluteDay = expectedAbsoluteDay;
        ExpectedMaterializedNpcRuntimeId = expectedMaterializedNpcRuntimeId;
        ExpectedResidenceSettlementRuntimeId = expectedResidenceSettlementRuntimeId;
        ExpectedResidencePopulationRevision = expectedResidencePopulationRevision;
        ExpectedResidencePopulation = expectedResidencePopulation;
    }

    public bool Equals(PersonDeathTransition other)
    {
        return ReferenceEquals(other, null) == false
            && Equals(PersonId, other.PersonId)
            && ExpectedAbsoluteDay == other.ExpectedAbsoluteDay
            && string.Equals(
                ExpectedMaterializedNpcRuntimeId,
                other.ExpectedMaterializedNpcRuntimeId,
                StringComparison.Ordinal)
            && string.Equals(
                ExpectedResidenceSettlementRuntimeId,
                other.ExpectedResidenceSettlementRuntimeId,
                StringComparison.Ordinal)
            && ExpectedResidencePopulationRevision == other.ExpectedResidencePopulationRevision
            && ExpectedResidencePopulation == other.ExpectedResidencePopulation;
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
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(
                ExpectedResidenceSettlementRuntimeId ?? string.Empty);
            hash = (hash * 397) ^ ExpectedResidencePopulationRevision.GetHashCode();
            hash = (hash * 397) ^ ExpectedResidencePopulation.GetHashCode();
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

        if (TryResolveResidenceSnapshot(
                world,
                person,
                out string residenceSettlementRuntimeId,
                out long residencePopulationRevision,
                out int residencePopulation,
                out failure) == false)
        {
            return false;
        }

        transition = new PersonDeathTransition(
            person,
            world.CurrentDay,
            materializedNpc?.RuntimeId,
            residenceSettlementRuntimeId,
            residencePopulationRevision,
            residencePopulation);
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
        return TryApplyDeathInternal(
            world,
            transition,
            NpcInjurySeverity.None,
            false,
            out failure);
    }

    public static bool TryApplyDeathWithConflictInjury(
        SimulationRuntime world,
        PersonDeathTransition transition,
        NpcInjurySeverity injurySeverity,
        out PersonDeathLifecycleFailure failure)
    {
        if (NpcInjuryRules.IsValid(injurySeverity) == false)
        {
            failure = PersonDeathLifecycleFailure.InvalidInjury;
            return false;
        }

        return TryApplyDeathInternal(
            world,
            transition,
            injurySeverity,
            true,
            out failure);
    }

    internal static bool TryValidateDeath(
        SimulationRuntime world,
        PersonDeathTransition transition,
        bool requireMaterializedNpc,
        out NpcRuntime materializedNpc,
        out PersonDeathLifecycleFailure failure)
    {
        materializedNpc = null;
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
                out materializedNpc,
                out failure) == false)
        {
            return false;
        }

        if (ReferenceEquals(person, transition.ExpectedPerson) == false)
        {
            failure = PersonDeathLifecycleFailure.StalePersonRegistration;
            return false;
        }

        if (string.Equals(
                materializedNpc?.RuntimeId,
                transition.ExpectedMaterializedNpcRuntimeId,
                StringComparison.Ordinal) == false)
        {
            failure = PersonDeathLifecycleFailure.StaleMaterialization;
            return false;
        }

        if (person.BirthAbsoluteDay.HasValue
            && transition.DeathAbsoluteDay < person.BirthAbsoluteDay.Value)
        {
            failure = PersonDeathLifecycleFailure.InvalidDeathDay;
            return false;
        }

        if (string.Equals(
                person.ResidenceSettlementRuntimeId,
                transition.ExpectedResidenceSettlementRuntimeId,
                StringComparison.Ordinal) == false)
        {
            failure = PersonDeathLifecycleFailure.StaleMaterialization;
            return false;
        }

        if (transition.ExpectsResident)
        {
            if (TryResolveSettlement(
                    world,
                    transition.ExpectedResidenceSettlementRuntimeId,
                    out CityRuntime settlement) == false)
            {
                failure = PersonDeathLifecycleFailure.ResidenceSettlementMissing;
                return false;
            }

            if (settlement.Population.Revision != transition.ExpectedResidencePopulationRevision
                || settlement.CurrentPopulation != transition.ExpectedResidencePopulation
                || transition.ExpectedResidencePopulation <= 0)
            {
                failure = PersonDeathLifecycleFailure.StalePersonRegistration;
                return false;
            }
        }

        if (requireMaterializedNpc && materializedNpc == null)
        {
            failure = PersonDeathLifecycleFailure.InvalidTransition;
            return false;
        }

        return true;
    }

    private static bool TryApplyDeathInternal(
        SimulationRuntime world,
        PersonDeathTransition transition,
        NpcInjurySeverity injurySeverity,
        bool applyConflictInjury,
        out PersonDeathLifecycleFailure failure)
    {
        failure = PersonDeathLifecycleFailure.None;
        if (TryValidateDeath(
                world,
                transition,
                applyConflictInjury,
                out NpcRuntime materializedNpc,
                out failure) == false)
        {
            return false;
        }

        SettlementPopulationRuntime residentPopulation = null;
        if (transition.ExpectsResident)
        {
            if (TryResolveSettlement(
                    world,
                    transition.ExpectedResidenceSettlementRuntimeId,
                    out CityRuntime settlement) == false
                || SettlementPopulationSystem.TryPropose(
                    settlement.Population,
                    new PopulationChangeSet(0, 1, 0, 0),
                    out SettlementPopulationTransition aggregateTransition,
                    out PopulationTransitionFailure aggregateFailure) == false
                || aggregateTransition.ExpectedRevision != transition.ExpectedResidencePopulationRevision
                || aggregateTransition.PopulationBefore != transition.ExpectedResidencePopulation
                || SettlementPopulationSystem.TryApply(
                    settlement.Population,
                    aggregateTransition,
                    out aggregateFailure) == false)
            {
                failure = PersonDeathLifecycleFailure.StalePersonRegistration;
                return false;
            }

            residentPopulation = settlement.Population;
        }

        // Every fallible check is complete before either representation mutates.
        if (materializedNpc != null && transition.ExpectsResident)
        {
            materializedNpc.ApplyPersonBackedResidentDeathAfterPopulationValidation(
                applyConflictInjury ? injurySeverity : NpcInjurySeverity.None,
                transition);
        }
        else
        {
            transition.ExpectedPerson.RecordDeathAfterValidation(transition.DeathAbsoluteDay);
            materializedNpc?.ApplyPersonDeathAfterValidation(
                applyConflictInjury ? injurySeverity : NpcInjurySeverity.None);
            if (residentPopulation != null)
            {
                transition.ExpectedPerson.TrySetResidenceSettlementRuntimeId(null);
            }
        }
        return true;
    }

    private static bool TryResolveResidenceSnapshot(
        SimulationRuntime world,
        PersonRuntime person,
        out string settlementRuntimeId,
        out long populationRevision,
        out int population,
        out PersonDeathLifecycleFailure failure)
    {
        settlementRuntimeId = person?.ResidenceSettlementRuntimeId;
        populationRevision = 0L;
        population = 0;
        failure = PersonDeathLifecycleFailure.None;

        if (string.IsNullOrWhiteSpace(settlementRuntimeId))
        {
            return true;
        }

        if (TryResolveSettlement(world, settlementRuntimeId, out CityRuntime settlement) == false)
        {
            failure = PersonDeathLifecycleFailure.ResidenceSettlementMissing;
            return false;
        }

        populationRevision = settlement.Population.Revision;
        population = settlement.CurrentPopulation;
        return true;
    }

    private static bool TryResolveSettlement(
        SimulationRuntime world,
        string settlementRuntimeId,
        out CityRuntime settlement)
    {
        settlement = null;
        if (world == null || string.IsNullOrWhiteSpace(settlementRuntimeId))
        {
            return false;
        }

        foreach (CityRuntime candidate in world.Cities)
        {
            if (candidate != null
                && string.Equals(candidate.RuntimeId, settlementRuntimeId, StringComparison.Ordinal))
            {
                settlement = candidate;
                return true;
            }
        }

        return false;
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
