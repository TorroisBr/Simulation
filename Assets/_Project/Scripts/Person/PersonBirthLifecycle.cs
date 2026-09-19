using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
    PopulationUnderflow = 13,
    InvalidParentCollection = 14,
    InvalidParentId = 15,
    DuplicateParentInput = 16,
    ParentNotRegistered = 17,
    SelfParent = 18,
    ChildAlreadyHasParentage = 19,
    ParentageMutationFailed = 20
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
    public IReadOnlyList<PersonId> ParentIds { get; }
    public bool HasParents => ParentIds.Count > 0;

    internal PersonBirthTransition(
        PersonId personId,
        string settlementRuntimeId,
        long expectedAbsoluteDay,
        long expectedPopulationRevision,
        int populationBefore,
        int populationAfter)
        : this(
            personId,
            settlementRuntimeId,
            expectedAbsoluteDay,
            expectedPopulationRevision,
            populationBefore,
            populationAfter,
            Array.Empty<PersonId>())
    {
    }

    internal PersonBirthTransition(
        PersonId personId,
        string settlementRuntimeId,
        long expectedAbsoluteDay,
        long expectedPopulationRevision,
        int populationBefore,
        int populationAfter,
        IEnumerable<PersonId> parentIds)
    {
        PersonId = personId;
        SettlementRuntimeId = settlementRuntimeId;
        ExpectedAbsoluteDay = expectedAbsoluteDay;
        ExpectedPopulationRevision = expectedPopulationRevision;
        PopulationBefore = populationBefore;
        PopulationAfter = populationAfter;
        ParentIds = new ReadOnlyCollection<PersonId>(
            parentIds != null
                ? new List<PersonId>(parentIds)
                : new List<PersonId>());
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
            && PopulationAfter == other.PopulationAfter
            && ParentIdsEqual(ParentIds, other.ParentIds);
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
            foreach (PersonId parentId in ParentIds)
            {
                hash = (hash * 397) ^ (parentId != null ? parentId.GetHashCode() : 0);
            }

            return hash;
        }
    }

    private static bool ParentIdsEqual(
        IReadOnlyList<PersonId> left,
        IReadOnlyList<PersonId> right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left == null || right == null || left.Count != right.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Count; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
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
        return TryProposeNamedBirthCore(
            world,
            settlement,
            personId,
            null,
            false,
            out transition,
            out failure);
    }

    public static bool TryProposeNamedBirth(
        SimulationRuntime world,
        CityRuntime settlement,
        PersonId personId,
        IEnumerable<PersonId> parentIds,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        return TryProposeNamedBirthCore(
            world,
            settlement,
            personId,
            parentIds,
            true,
            out transition,
            out failure);
    }

    private static bool TryProposeNamedBirthCore(
        SimulationRuntime world,
        CityRuntime settlement,
        PersonId personId,
        IEnumerable<PersonId> parentIds,
        bool parentAware,
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

        if (TryPrepareParentIds(
                world,
                personId,
                parentIds,
                parentAware,
                out IReadOnlyList<PersonId> normalizedParentIds,
                out failure) == false)
        {
            return false;
        }

        if (world.GetGenealogyParents(personId).Count > 0)
        {
            failure = PersonBirthLifecycleFailure.ChildAlreadyHasParentage;
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
            population.CurrentPopulation + 1,
            normalizedParentIds);
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

        if (TryValidateTransitionParents(world, transition, out failure) == false)
        {
            return false;
        }

        if (world.GetGenealogyParents(transition.PersonId).Count > 0)
        {
            failure = PersonBirthLifecycleFailure.ChildAlreadyHasParentage;
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

        List<PersonId> addedParentIds = new List<PersonId>();
        foreach (PersonId parentId in transition.ParentIds)
        {
            if (PersonGenealogySystem.TryStoreAdd(
                    world,
                    parentId,
                    transition.PersonId,
                    out PersonGenealogyFailure genealogyFailure) == false)
            {
                RollbackParentage(world, addedParentIds, transition.PersonId);
                world.PersonStore.TryRollbackRegistration(candidate);
                failure = MapGenealogyFailure(genealogyFailure);
                return false;
            }

            addedParentIds.Add(parentId);
        }

        if (SettlementPopulationSystem.TryApply(
                population,
                populationTransition,
                out populationFailure) == false)
        {
            RollbackParentage(world, addedParentIds, transition.PersonId);
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
        IEnumerable<PersonId> parentIds,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        transition = null;
        if (TryProposeNamedBirth(
                world,
                settlement,
                personId,
                parentIds,
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

    private static bool TryPrepareParentIds(
        SimulationRuntime world,
        PersonId childId,
        IEnumerable<PersonId> parentIds,
        bool parentAware,
        out IReadOnlyList<PersonId> normalizedParentIds,
        out PersonBirthLifecycleFailure failure)
    {
        normalizedParentIds = null;
        failure = PersonBirthLifecycleFailure.None;

        if (parentAware == false)
        {
            normalizedParentIds = new ReadOnlyCollection<PersonId>(new List<PersonId>());
            return true;
        }

        if (parentIds == null)
        {
            failure = PersonBirthLifecycleFailure.InvalidParentCollection;
            return false;
        }

        List<PersonId> collected = new List<PersonId>();
        HashSet<PersonId> seen = new HashSet<PersonId>();
        foreach (PersonId parentId in parentIds)
        {
            if (parentId == null)
            {
                failure = PersonBirthLifecycleFailure.InvalidParentId;
                return false;
            }

            if (parentId == childId)
            {
                failure = PersonBirthLifecycleFailure.SelfParent;
                return false;
            }

            if (seen.Add(parentId) == false)
            {
                failure = PersonBirthLifecycleFailure.DuplicateParentInput;
                return false;
            }

            if (world.PersonStore.TryGet(parentId, out _) == false)
            {
                failure = PersonBirthLifecycleFailure.ParentNotRegistered;
                return false;
            }

            collected.Add(parentId);
        }

        collected.Sort(ComparePersonIds);
        normalizedParentIds = new ReadOnlyCollection<PersonId>(collected);
        return true;
    }

    private static bool TryValidateTransitionParents(
        SimulationRuntime world,
        PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        failure = PersonBirthLifecycleFailure.None;
        if (transition.ParentIds == null)
        {
            failure = PersonBirthLifecycleFailure.InvalidTransition;
            return false;
        }

        PersonId previous = null;
        HashSet<PersonId> seen = new HashSet<PersonId>();
        foreach (PersonId parentId in transition.ParentIds)
        {
            if (parentId == null)
            {
                failure = PersonBirthLifecycleFailure.InvalidParentId;
                return false;
            }

            if (parentId == transition.PersonId)
            {
                failure = PersonBirthLifecycleFailure.SelfParent;
                return false;
            }

            if (seen.Add(parentId) == false)
            {
                failure = PersonBirthLifecycleFailure.DuplicateParentInput;
                return false;
            }

            if (previous != null && ComparePersonIds(previous, parentId) >= 0)
            {
                failure = PersonBirthLifecycleFailure.InvalidTransition;
                return false;
            }

            if (world.PersonStore.TryGet(parentId, out _) == false)
            {
                failure = PersonBirthLifecycleFailure.ParentNotRegistered;
                return false;
            }

            previous = parentId;
        }

        return true;
    }

    private static void RollbackParentage(
        SimulationRuntime world,
        IReadOnlyList<PersonId> addedParentIds,
        PersonId childId)
    {
        for (int index = addedParentIds.Count - 1; index >= 0; index--)
        {
            PersonGenealogySystem.TryStoreRemove(
                world,
                addedParentIds[index],
                childId);
        }
    }

    private static int ComparePersonIds(PersonId left, PersonId right)
    {
        return string.CompareOrdinal(left.Value, right.Value);
    }

    private static PersonBirthLifecycleFailure MapGenealogyFailure(
        PersonGenealogyFailure failure)
    {
        switch (failure)
        {
            case PersonGenealogyFailure.InvalidParent:
                return PersonBirthLifecycleFailure.InvalidParentId;
            case PersonGenealogyFailure.InvalidChild:
                return PersonBirthLifecycleFailure.InvalidPersonId;
            case PersonGenealogyFailure.ParentNotRegistered:
                return PersonBirthLifecycleFailure.ParentNotRegistered;
            case PersonGenealogyFailure.SelfParent:
                return PersonBirthLifecycleFailure.SelfParent;
            case PersonGenealogyFailure.DuplicateParentage:
                return PersonBirthLifecycleFailure.ParentageMutationFailed;
            case PersonGenealogyFailure.WouldCreateCycle:
                return PersonBirthLifecycleFailure.ParentageMutationFailed;
            default:
                return PersonBirthLifecycleFailure.ParentageMutationFailed;
        }
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
