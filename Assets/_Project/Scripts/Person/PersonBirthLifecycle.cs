using System;

public enum PersonBirthLifecycleFailure
{
    None = 0,
    InvalidWorld = 1,
    InvalidPersonId = 2,
    InvalidSettlement = 3,
    SettlementNotInWorld = 4,
    PersonAlreadyExists = 5,
    PopulationOverflow = 6,
    RevisionOverflow = 7,
    StaleWorldDay = 8,
    StalePopulation = 9,
    InvalidTransition = 10,
    RepresentedPopulationInvalid = 11,
    RegistrationFailed = 12,
    PopulationUnderflow = 13
}

/// <summary>
/// Immutable proposal for the creation of one named Person and its aggregate
/// settlement population increment. The transition is produced by the world
/// birth boundary and cannot be constructed by production callers.
/// </summary>
public sealed class PersonBirthTransition : IEquatable<PersonBirthTransition>
{
    public PersonId PersonId { get; }
    public string SettlementRuntimeId { get; }
    public long ExpectedAbsoluteDay { get; }
    public long BirthAbsoluteDay => ExpectedAbsoluteDay;
    public long ExpectedPopulationRevision { get; }
    public int PopulationBefore { get; }
    public int PopulationAfter { get; }

    internal PersonBirthTransition(
        PersonId personId,
        string settlementRuntimeId,
        long expectedAbsoluteDay,
        long expectedPopulationRevision,
        int populationBefore,
        int populationAfter)
    {
        PersonId = personId;
        SettlementRuntimeId = settlementRuntimeId;
        ExpectedAbsoluteDay = expectedAbsoluteDay;
        ExpectedPopulationRevision = expectedPopulationRevision;
        PopulationBefore = populationBefore;
        PopulationAfter = populationAfter;
    }

    public bool Equals(PersonBirthTransition other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        return Equals(PersonId, other.PersonId)
            && string.Equals(SettlementRuntimeId, other.SettlementRuntimeId, StringComparison.Ordinal)
            && ExpectedAbsoluteDay == other.ExpectedAbsoluteDay
            && ExpectedPopulationRevision == other.ExpectedPopulationRevision
            && PopulationBefore == other.PopulationBefore
            && PopulationAfter == other.PopulationAfter;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as PersonBirthTransition);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = PersonId != null ? PersonId.GetHashCode() : 0;
            hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SettlementRuntimeId ?? string.Empty);
            hash = (hash * 397) ^ ExpectedAbsoluteDay.GetHashCode();
            hash = (hash * 397) ^ ExpectedPopulationRevision.GetHashCode();
            hash = (hash * 397) ^ PopulationBefore;
            hash = (hash * 397) ^ PopulationAfter;
            return hash;
        }
    }
}

/// <summary>
/// World-owned boundary for named birth. It creates the lightweight Person
/// identity and the corresponding aggregate population delta atomically. It
/// does not create an NPC, change simulation time, or invoke randomness.
/// </summary>
public static class PersonBirthLifecycleSystem
{
    public static bool TryProposeNamedBirth(
        SimulationRuntime world,
        CityRuntime settlement,
        PersonId personId,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        transition = null;
        failure = PersonBirthLifecycleFailure.None;

        if (world == null)
        {
            failure = PersonBirthLifecycleFailure.InvalidWorld;
            return false;
        }

        if (personId == null)
        {
            failure = PersonBirthLifecycleFailure.InvalidPersonId;
            return false;
        }

        if (settlement == null || string.IsNullOrWhiteSpace(settlement.RuntimeId) == true)
        {
            failure = PersonBirthLifecycleFailure.InvalidSettlement;
            return false;
        }

        if (ContainsSettlement(world, settlement) == false)
        {
            failure = PersonBirthLifecycleFailure.SettlementNotInWorld;
            return false;
        }

        if (world.PersonStore.TryGet(personId, out _))
        {
            failure = PersonBirthLifecycleFailure.PersonAlreadyExists;
            return false;
        }

        SettlementPopulationPresenceSummary summary = GetPresenceSummary(world, settlement);
        if (summary == null || summary.RepresentedResidentCount > summary.ResidentPopulation)
        {
            failure = PersonBirthLifecycleFailure.RepresentedPopulationInvalid;
            return false;
        }

        SettlementPopulationRuntime population = settlement.Population;
        if (population.CurrentPopulation == int.MaxValue)
        {
            failure = PersonBirthLifecycleFailure.PopulationOverflow;
            return false;
        }

        if (population.Revision == long.MaxValue)
        {
            failure = PersonBirthLifecycleFailure.RevisionOverflow;
            return false;
        }

        transition = new PersonBirthTransition(
            personId,
            settlement.RuntimeId,
            world.CurrentDay,
            population.Revision,
            population.CurrentPopulation,
            population.CurrentPopulation + 1);
        return true;
    }

    public static bool TryApplyNamedBirth(
        SimulationRuntime world,
        PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        failure = PersonBirthLifecycleFailure.None;

        if (world == null)
        {
            failure = PersonBirthLifecycleFailure.InvalidWorld;
            return false;
        }

        if (IsValidTransitionShape(transition) == false)
        {
            failure = PersonBirthLifecycleFailure.InvalidTransition;
            return false;
        }

        if (TryFindUniqueSettlement(world, transition.SettlementRuntimeId, out CityRuntime settlement) == false)
        {
            failure = PersonBirthLifecycleFailure.SettlementNotInWorld;
            return false;
        }

        if (world.CurrentDay != transition.ExpectedAbsoluteDay)
        {
            failure = PersonBirthLifecycleFailure.StaleWorldDay;
            return false;
        }

        if (world.PersonStore.TryGet(transition.PersonId, out _))
        {
            failure = PersonBirthLifecycleFailure.PersonAlreadyExists;
            return false;
        }

        SettlementPopulationPresenceSummary summary = GetPresenceSummary(world, settlement);
        if (summary == null || summary.RepresentedResidentCount > summary.ResidentPopulation)
        {
            failure = PersonBirthLifecycleFailure.RepresentedPopulationInvalid;
            return false;
        }

        SettlementPopulationRuntime population = settlement.Population;
        if (population.Revision != transition.ExpectedPopulationRevision
            || population.CurrentPopulation != transition.PopulationBefore)
        {
            failure = PersonBirthLifecycleFailure.StalePopulation;
            return false;
        }

        if (population.CurrentPopulation == int.MaxValue)
        {
            failure = PersonBirthLifecycleFailure.PopulationOverflow;
            return false;
        }

        if (population.Revision == long.MaxValue)
        {
            failure = PersonBirthLifecycleFailure.RevisionOverflow;
            return false;
        }

        SettlementPopulationTransition populationTransition;
        if (SettlementPopulationSystem.TryPropose(
                population,
                new PopulationChangeSet(1, 0, 0, 0),
                out populationTransition,
                out PopulationTransitionFailure populationFailure) == false)
        {
            failure = MapPopulationFailure(populationFailure);
            return false;
        }

        if (populationTransition.ExpectedRevision != transition.ExpectedPopulationRevision
            || populationTransition.PopulationBefore != transition.PopulationBefore
            || populationTransition.PopulationAfter != transition.PopulationAfter
            || populationTransition.Births != 1
            || populationTransition.Deaths != 0
            || populationTransition.Immigrations != 0
            || populationTransition.Emigrations != 0)
        {
            failure = PersonBirthLifecycleFailure.InvalidTransition;
            return false;
        }

        PersonRuntime candidate = new PersonRuntime(
            transition.PersonId,
            transition.BirthAbsoluteDay);
        if (candidate.TrySetResidenceSettlementRuntimeId(settlement.RuntimeId) == false)
        {
            failure = PersonBirthLifecycleFailure.InvalidTransition;
            return false;
        }

        if (world.TryRegisterPerson(candidate, out PersonStoreFailure storeFailure) == false)
        {
            failure = storeFailure == PersonStoreFailure.DuplicatePersonId
                ? PersonBirthLifecycleFailure.PersonAlreadyExists
                : PersonBirthLifecycleFailure.RegistrationFailed;
            return false;
        }

        if (SettlementPopulationSystem.TryApply(
                population,
                populationTransition,
                out populationFailure) == false)
        {
            world.PersonStore.TryRollbackRegistration(candidate);
            failure = MapPopulationFailure(populationFailure);
            return false;
        }

        return true;
    }

    public static bool TryApplyNamedBirth(
        SimulationRuntime world,
        CityRuntime settlement,
        PersonId personId,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        transition = null;
        if (TryProposeNamedBirth(
                world,
                settlement,
                personId,
                out transition,
                out failure) == false)
        {
            return false;
        }

        if (TryApplyNamedBirth(world, transition, out failure) == false)
        {
            transition = null;
            return false;
        }

        return true;
    }

    private static SettlementPopulationPresenceSummary GetPresenceSummary(
        SimulationRuntime world,
        CityRuntime settlement)
    {
        return SettlementPopulationPresenceQuery.BuildSummary(
            settlement,
            world.NpcRuntimes,
            world.PersonStore.Persons);
    }

    private static bool ContainsSettlement(SimulationRuntime world, CityRuntime settlement)
    {
        foreach (CityRuntime candidate in world.Cities)
        {
            if (ReferenceEquals(candidate, settlement))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindUniqueSettlement(
        SimulationRuntime world,
        string settlementRuntimeId,
        out CityRuntime settlement)
    {
        settlement = null;
        foreach (CityRuntime candidate in world.Cities)
        {
            if (candidate == null
                || string.Equals(candidate.RuntimeId, settlementRuntimeId, StringComparison.Ordinal) == false)
            {
                continue;
            }

            if (settlement != null)
            {
                settlement = null;
                return false;
            }

            settlement = candidate;
        }

        return settlement != null;
    }

    private static bool IsValidTransitionShape(PersonBirthTransition transition)
    {
        if (transition == null
            || transition.PersonId == null
            || string.IsNullOrWhiteSpace(transition.SettlementRuntimeId) == true
            || transition.ExpectedAbsoluteDay < 0L
            || transition.BirthAbsoluteDay != transition.ExpectedAbsoluteDay
            || transition.ExpectedPopulationRevision < 0L
            || transition.PopulationBefore < 0
            || transition.PopulationAfter < 0)
        {
            return false;
        }

        return (long)transition.PopulationBefore + 1L == transition.PopulationAfter;
    }

    private static PersonBirthLifecycleFailure MapPopulationFailure(
        PopulationTransitionFailure failure)
    {
        switch (failure)
        {
            case PopulationTransitionFailure.WouldOverflow:
                return PersonBirthLifecycleFailure.PopulationOverflow;
            case PopulationTransitionFailure.WouldUnderflow:
                return PersonBirthLifecycleFailure.PopulationUnderflow;
            case PopulationTransitionFailure.RevisionOverflow:
                return PersonBirthLifecycleFailure.RevisionOverflow;
            case PopulationTransitionFailure.StaleState:
                return PersonBirthLifecycleFailure.StalePopulation;
            case PopulationTransitionFailure.InvalidSettlement:
            case PopulationTransitionFailure.InvalidTransition:
            case PopulationTransitionFailure.NegativeChange:
            default:
                return PersonBirthLifecycleFailure.InvalidTransition;
        }
    }
}
