using System;
using System.Collections.Generic;

/// <summary>The selected Daily-v1 immigration or emigration operation.</summary>
internal enum DailyV1SelectedPopulationChange
{
    Immigration = 1,
    Emigration = 2
}

internal enum DailyV1SelectedPopulationChangeFailure
{
    None = 0,
    InvalidNpc = 1,
    DeadNpc = 2,
    InvalidSettlement = 3,
    ResidenceAlreadyAssigned = 4,
    ResidenceMismatch = 5,
    AggregateCapacityExceeded = 6,
    PopulationUnderflow = 7,
    PopulationOverflow = 8,
    RevisionOverflow = 9,
    StaleState = 10,
    InvalidTransition = 11,
    RuntimeFaulted = 12
}

/// <summary>
/// Prepared selected-owner immigration/emigration. It binds one exact City
/// population owner and exactly one residence authority: PersonStore for a
/// materialized Person or the NPC-only residence owner.
/// </summary>
internal sealed class DailyV1SelectedPopulationChangePlan
{
    internal DailyV1NpcRuntimeOwners Npc { get; }
    internal DailyV1CityRuntimeOwners City { get; }
    internal PersonRuntime Person { get; }
    internal DailyV1NpcResidenceOwner NpcResidence { get; }
    internal DailyV1SelectedPopulationChange Operation { get; }
    internal SettlementPopulationTransition PopulationTransition { get; }
    internal int NamedResidentsBefore { get; }
    internal long ResidenceRevision { get; }
    internal string ResidenceBefore { get; }
    internal string ResidenceAfter { get; }
    internal long PopulationRevision => PopulationTransition.ExpectedRevision;
    internal int PopulationBefore => PopulationTransition.PopulationBefore;
    internal int PopulationAfter => PopulationTransition.PopulationAfter;
    internal bool IsPersonBacked => Person != null;

    internal DailyV1SelectedPopulationChangePlan(
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners city,
        PersonRuntime person,
        DailyV1NpcResidenceOwner npcResidence,
        DailyV1SelectedPopulationChange operation,
        SettlementPopulationTransition populationTransition,
        int namedResidentsBefore,
        long residenceRevision,
        string residenceBefore,
        string residenceAfter)
    {
        Npc = npc ?? throw new ArgumentNullException(nameof(npc));
        City = city ?? throw new ArgumentNullException(nameof(city));
        Person = person;
        NpcResidence = npcResidence;
        Operation = operation;
        PopulationTransition = populationTransition
            ?? throw new ArgumentNullException(nameof(populationTransition));
        NamedResidentsBefore = namedResidentsBefore;
        ResidenceRevision = residenceRevision;
        ResidenceBefore = residenceBefore;
        ResidenceAfter = residenceAfter;
    }
}

/// <summary>
/// Core-owned selected Daily-v1 immigration/emigration preparation and commit.
/// P12 admission remains the host boundary; owner selection, population
/// semantics, residence mutation, and City mutation are bound to Core owners.
/// </summary>
internal static class DailyV1SelectedPopulationChangeOperation
{
    internal static bool TryPrepare(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners city,
        DailyV1SelectedPopulationChange operation,
        out DailyV1SelectedPopulationChangePlan plan,
        out DailyV1SelectedPopulationChangeFailure failure)
    {
        plan = null;
        failure = DailyV1SelectedPopulationChangeFailure.InvalidNpc;
        if (graph == null || npc == null || !graph.ContainsExactNpcOwner(npc))
            return false;
        if (npc.Lifecycle?.LifeState != NpcLifeState.Alive)
        {
            failure = DailyV1SelectedPopulationChangeFailure.DeadNpc;
            return false;
        }
        if (operation != DailyV1SelectedPopulationChange.Immigration
            && operation != DailyV1SelectedPopulationChange.Emigration)
        {
            failure = DailyV1SelectedPopulationChangeFailure.InvalidTransition;
            return false;
        }
        if (city == null || !graph.ContainsExactCityOwner(city)
            || city.Population == null
            || !string.Equals(city.Population.SettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal))
        {
            failure = DailyV1SelectedPopulationChangeFailure.InvalidSettlement;
            return false;
        }
        if (!ReferenceEquals(npc.Definitions, city.Definitions))
        {
            failure = DailyV1SelectedPopulationChangeFailure.RuntimeFaulted;
            return false;
        }

        if (!TryGetResidenceAuthority(
                graph,
                npc,
                out PersonRuntime person,
                out DailyV1NpcResidenceOwner npcResidence,
                out string residence,
                out long residenceRevision))
        {
            failure = DailyV1SelectedPopulationChangeFailure.RuntimeFaulted;
            return false;
        }
        if (operation == DailyV1SelectedPopulationChange.Immigration)
        {
            if (!string.IsNullOrWhiteSpace(residence))
            {
                failure = DailyV1SelectedPopulationChangeFailure.ResidenceAlreadyAssigned;
                return false;
            }
        }
        else if (!string.Equals(residence, city.RuntimeId, StringComparison.Ordinal))
        {
            failure = DailyV1SelectedPopulationChangeFailure.ResidenceMismatch;
            return false;
        }

        if (!TryCountNamedResidents(graph, city.RuntimeId, out int namedResidents))
        {
            failure = DailyV1SelectedPopulationChangeFailure.RuntimeFaulted;
            return false;
        }
        if (namedResidents > city.Population.CurrentPopulation)
        {
            failure = DailyV1SelectedPopulationChangeFailure.AggregateCapacityExceeded;
            return false;
        }

        if (operation == DailyV1SelectedPopulationChange.Immigration
            && city.Population.CurrentPopulation == int.MaxValue)
        {
            failure = DailyV1SelectedPopulationChangeFailure.PopulationOverflow;
            return false;
        }
        if (operation == DailyV1SelectedPopulationChange.Emigration
            && city.Population.CurrentPopulation <= 0)
        {
            failure = DailyV1SelectedPopulationChangeFailure.PopulationUnderflow;
            return false;
        }
        if (!PopulationOwnerBridge.CanAdvanceP12AggregateRevision(city.Population))
        {
            failure = DailyV1SelectedPopulationChangeFailure.RevisionOverflow;
            return false;
        }
        if (residenceRevision == long.MaxValue)
        {
            failure = DailyV1SelectedPopulationChangeFailure.RevisionOverflow;
            return false;
        }
        if (operation == DailyV1SelectedPopulationChange.Emigration
            && namedResidents <= 0)
        {
            failure = DailyV1SelectedPopulationChangeFailure.AggregateCapacityExceeded;
            return false;
        }

        PopulationChangeSet change = operation == DailyV1SelectedPopulationChange.Immigration
            ? new PopulationChangeSet(0, 0, 1, 0)
            : new PopulationChangeSet(0, 0, 0, 1);
        if (!SettlementPopulationSystem.TryPropose(
                city.Population,
                change,
                out SettlementPopulationTransition populationTransition,
                out PopulationTransitionFailure populationFailure))
        {
            failure = MapPopulationFailure(populationFailure);
            return false;
        }

        int namedResidentsAfter = operation == DailyV1SelectedPopulationChange.Immigration
            ? namedResidents + 1
            : namedResidents - 1;
        if (namedResidentsAfter < 0 || namedResidentsAfter > populationTransition.PopulationAfter)
        {
            failure = DailyV1SelectedPopulationChangeFailure.AggregateCapacityExceeded;
            return false;
        }

        plan = new DailyV1SelectedPopulationChangePlan(
            npc,
            city,
            person,
            npcResidence,
            operation,
            populationTransition,
            namedResidents,
            residenceRevision,
            residence,
            operation == DailyV1SelectedPopulationChange.Immigration ? city.RuntimeId : null);
        failure = DailyV1SelectedPopulationChangeFailure.None;
        return true;
    }

    internal static bool TryCommit(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1SelectedPopulationChangePlan plan,
        Func<bool> npcResidenceMutationAdmission,
        Action notifyNpcResidenceCommitted,
        out DailyV1SelectedPopulationChangeFailure failure)
    {
        failure = DailyV1SelectedPopulationChangeFailure.RuntimeFaulted;
        if (graph == null || plan == null
            || !graph.ContainsExactNpcOwner(plan.Npc)
            || !graph.ContainsExactCityOwner(plan.City)
            || plan.Npc.Lifecycle?.LifeState != NpcLifeState.Alive
            || !ReferenceEquals(plan.Npc.Definitions, plan.City.Definitions))
            return false;

        SettlementPopulationRuntime population = plan.City.Population;
        if (population == null)
            return false;
        if (population.Revision != plan.PopulationRevision
            || population.CurrentPopulation != plan.PopulationBefore)
        {
            failure = DailyV1SelectedPopulationChangeFailure.StaleState;
            return false;
        }
        if (!PopulationOwnerBridge.CanAdvanceP12AggregateRevision(population))
        {
            failure = DailyV1SelectedPopulationChangeFailure.RevisionOverflow;
            return false;
        }
        if (!TryCountNamedResidents(graph, plan.City.RuntimeId, out int currentNamedResidents)
            || currentNamedResidents != plan.NamedResidentsBefore)
        {
            failure = DailyV1SelectedPopulationChangeFailure.StaleState;
            return false;
        }

        if (plan.Person != null)
        {
            if (graph.PersonStore == null
                || plan.Person.PersonId == null
                || !graph.PersonStore.TryGet(plan.Person.PersonId, out PersonRuntime registeredPerson)
                || !ReferenceEquals(registeredPerson, plan.Person)
                || !graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    plan.Npc.RuntimeId, out PersonRuntime materializedPerson)
                || !ReferenceEquals(materializedPerson, plan.Person)
                || !string.Equals(plan.Person.MaterializedNpcRuntimeId,
                    plan.Npc.RuntimeId, StringComparison.Ordinal)
                || plan.Person.DeathAbsoluteDay.HasValue
                || !string.Equals(plan.Person.ResidenceSettlementRuntimeId,
                    plan.ResidenceBefore, StringComparison.Ordinal)
                || PersonRuntimeOwnerBridge.GetLifeResidenceRevision(plan.Person)
                    != plan.ResidenceRevision
                || !PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(plan.Person, 1)
                || !PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(plan.Person))
                return false;
        }
        else if (plan.NpcResidence == null
            || !ReferenceEquals(plan.NpcResidence, plan.Npc.Residence)
            || !plan.NpcResidence.Matches(plan.ResidenceBefore, plan.ResidenceRevision)
            || !plan.NpcResidence.CanAdvanceRevision(1)
            || npcResidenceMutationAdmission == null)
        {
            return false;
        }

        int expectedPopulationAfter = plan.Operation == DailyV1SelectedPopulationChange.Immigration
            ? plan.PopulationBefore + 1
            : plan.PopulationBefore - 1;
        int expectedResidentsAfter = plan.Operation == DailyV1SelectedPopulationChange.Immigration
            ? currentNamedResidents + 1
            : currentNamedResidents - 1;
        if (plan.PopulationTransition.PopulationAfter != expectedPopulationAfter
            || expectedResidentsAfter < 0
            || expectedResidentsAfter > expectedPopulationAfter
            || !string.Equals(
                plan.ResidenceAfter,
                plan.Operation == DailyV1SelectedPopulationChange.Immigration
                    ? plan.City.RuntimeId
                    : null,
                StringComparison.Ordinal))
        {
            failure = DailyV1SelectedPopulationChangeFailure.InvalidTransition;
            return false;
        }

        bool CommitResidence()
        {
            if (plan.Person != null)
                return PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
                    plan.Person, plan.ResidenceAfter);

            bool admitted;
            try { admitted = npcResidenceMutationAdmission(); }
            catch { admitted = false; }
            if (!admitted
                || !plan.NpcResidence.TrySetValue(plan.ResidenceAfter))
                return false;
            plan.NpcResidence.NotifyMutationCommitted();
            try { notifyNpcResidenceCommitted?.Invoke(); }
            catch { }
            return true;
        }

        if (!PopulationOwnerBridge.TryApplyWithResidenceCommit(
                population,
                plan.PopulationTransition,
                CommitResidence,
                out PopulationTransitionFailure applyFailure))
        {
            failure = MapPopulationFailure(applyFailure);
            return false;
        }

        failure = DailyV1SelectedPopulationChangeFailure.None;
        return true;
    }

    private static bool TryGetResidenceAuthority(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners npc,
        out PersonRuntime person,
        out DailyV1NpcResidenceOwner npcResidence,
        out string residence,
        out long revision)
    {
        person = null;
        npcResidence = null;
        residence = null;
        revision = 0L;
        if (graph.PersonStore != null
            && graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                npc.RuntimeId, out PersonRuntime materializedPerson))
        {
            if (materializedPerson == null
                || materializedPerson.PersonId == null
                || !graph.PersonStore.TryGet(materializedPerson.PersonId, out PersonRuntime registered)
                || !ReferenceEquals(registered, materializedPerson)
                || !string.Equals(materializedPerson.MaterializedNpcRuntimeId,
                    npc.RuntimeId, StringComparison.Ordinal)
                || materializedPerson.DeathAbsoluteDay.HasValue)
                return false;
            person = materializedPerson;
            residence = person.ResidenceSettlementRuntimeId;
            revision = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
            return true;
        }

        npcResidence = npc.Residence;
        if (npcResidence == null
            || !string.Equals(npcResidence.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
            return false;
        residence = npcResidence.SettlementRuntimeId;
        revision = npcResidence.Revision;
        return true;
    }

    private static bool TryCountNamedResidents(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string cityRuntimeId,
        out int residentCount)
    {
        residentCount = 0;
        if (graph?.PersonStore == null || string.IsNullOrWhiteSpace(cityRuntimeId))
            return false;

        foreach (PersonRuntime person in graph.PersonStore.Persons)
        {
            if (person == null || person.PersonId == null)
                return false;
            if (!person.DeathAbsoluteDay.HasValue
                && string.Equals(person.ResidenceSettlementRuntimeId, cityRuntimeId, StringComparison.Ordinal))
            {
                if (residentCount == int.MaxValue) return false;
                residentCount++;
            }
        }

        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || npc.Lifecycle == null || npc.Residence == null
                || !string.Equals(npc.Residence.OwnerRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
                return false;
            if (graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    npc.RuntimeId, out PersonRuntime materializedPerson))
            {
                if (materializedPerson == null
                    || materializedPerson.PersonId == null
                    || !ReferenceEquals(
                        materializedPerson,
                        graph.PersonStore.TryGet(materializedPerson.PersonId, out PersonRuntime registered)
                            ? registered
                            : null)
                    || !string.Equals(materializedPerson.MaterializedNpcRuntimeId,
                        npc.RuntimeId, StringComparison.Ordinal))
                    return false;
                continue;
            }
            if (npc.Lifecycle.LifeState == NpcLifeState.Alive
                && string.Equals(npc.Residence.SettlementRuntimeId, cityRuntimeId, StringComparison.Ordinal))
            {
                if (residentCount == int.MaxValue) return false;
                residentCount++;
            }
        }

        return true;
    }

    private static DailyV1SelectedPopulationChangeFailure MapPopulationFailure(
        PopulationTransitionFailure failure)
    {
        switch (failure)
        {
            case PopulationTransitionFailure.None:
                return DailyV1SelectedPopulationChangeFailure.None;
            case PopulationTransitionFailure.WouldUnderflow:
                return DailyV1SelectedPopulationChangeFailure.PopulationUnderflow;
            case PopulationTransitionFailure.WouldOverflow:
                return DailyV1SelectedPopulationChangeFailure.PopulationOverflow;
            case PopulationTransitionFailure.RevisionOverflow:
                return DailyV1SelectedPopulationChangeFailure.RevisionOverflow;
            case PopulationTransitionFailure.StaleState:
                return DailyV1SelectedPopulationChangeFailure.StaleState;
            default:
                return DailyV1SelectedPopulationChangeFailure.InvalidTransition;
        }
    }
}
