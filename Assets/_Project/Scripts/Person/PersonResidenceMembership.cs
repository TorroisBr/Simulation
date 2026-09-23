using System;

public enum PersonResidenceMembershipFailure
{
    None = 0,
    InvalidWorld = 1,
    InvalidPerson = 2,
    PersonNotRegistered = 3,
    InvalidSettlement = 4,
    SettlementNotInWorld = 5,
    ResidenceAlreadyAssigned = 6,
    AggregateCapacityExceeded = 7,
    PersonDead = 8,
    RuntimeFaulted = 9
}

/// <summary>
/// World-owned boundary for assigning an existing Person to a settlement.
/// The operation changes only Person residence authority; it does not mutate
/// aggregate population or revision values.
/// </summary>
public static class PersonResidenceMembershipSystem
{
    public static bool TryBindExistingResident(
        PersonRuntime person,
        CityRuntime settlement,
        SimulationRuntime world,
        out PersonResidenceMembershipFailure failure)
    {
        failure = PersonResidenceMembershipFailure.None;

        if (world == null)
        {
            failure = PersonResidenceMembershipFailure.InvalidWorld;
            return false;
        }

        if (world.IsMutationFaulted)
        {
            failure = PersonResidenceMembershipFailure.RuntimeFaulted;
            return false;
        }

        if (person == null || person.PersonId == null)
        {
            failure = PersonResidenceMembershipFailure.InvalidPerson;
            return false;
        }

        if (settlement == null || string.IsNullOrWhiteSpace(settlement.RuntimeId) == true)
        {
            failure = PersonResidenceMembershipFailure.InvalidSettlement;
            return false;
        }

        if (ContainsSettlement(world, settlement) == false)
        {
            failure = PersonResidenceMembershipFailure.SettlementNotInWorld;
            return false;
        }

        if (world.PersonStore.TryGet(person.PersonId, out PersonRuntime registeredPerson) == false
            || ReferenceEquals(registeredPerson, person) == false)
        {
            failure = PersonResidenceMembershipFailure.PersonNotRegistered;
            return false;
        }

        if (person.DeathAbsoluteDay.HasValue)
        {
            failure = PersonResidenceMembershipFailure.PersonDead;
            return false;
        }

        if (string.IsNullOrWhiteSpace(person.ResidenceSettlementRuntimeId) == false)
        {
            failure = string.Equals(
                person.ResidenceSettlementRuntimeId,
                settlement.RuntimeId,
                StringComparison.Ordinal)
                ? PersonResidenceMembershipFailure.None
                : PersonResidenceMembershipFailure.ResidenceAlreadyAssigned;
            return failure == PersonResidenceMembershipFailure.None;
        }

        SettlementPopulationPresenceSummary summary =
            SettlementPopulationPresenceQuery.BuildSummary(
                settlement,
                world.NpcRuntimes,
                world.PersonStore.Persons);
        if (summary.RepresentedResidentCount >= summary.ResidentPopulation)
        {
            failure = PersonResidenceMembershipFailure.AggregateCapacityExceeded;
            return false;
        }

        return person.TrySetResidenceSettlementRuntimeId(settlement.RuntimeId);
    }

    public static bool TryBindExistingResident(
        SimulationRuntime world,
        PersonRuntime person,
        CityRuntime settlement,
        out PersonResidenceMembershipFailure failure)
    {
        return TryBindExistingResident(person, settlement, world, out failure);
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
}
