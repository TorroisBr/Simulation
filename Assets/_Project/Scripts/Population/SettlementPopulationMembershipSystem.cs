using System;
using System.Collections.Generic;

public enum PopulationMembershipFailure
{
    None = 0,
    InvalidNpc = 1,
    DeadNpc = 2,
    InvalidSettlement = 3,
    ResidenceAlreadyAssigned = 4,
    AggregateCapacityExceeded = 5
}

/// <summary>
/// Explicit membership boundary for materializing an already aggregated resident.
/// Binding changes only the NPC identity; it is not an immigration and does not change population or revision.
/// </summary>
public static class SettlementPopulationMembershipSystem
{
    public static bool TryBindExistingResident(
        CityRuntime city,
        NpcRuntime npc,
        IEnumerable<NpcRuntime> knownNpcs,
        out PopulationMembershipFailure failure)
    {
        failure = PopulationMembershipFailure.None;

        if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId) == true)
        {
            failure = PopulationMembershipFailure.InvalidSettlement;
            return false;
        }

        if (npc == null)
        {
            failure = PopulationMembershipFailure.InvalidNpc;
            return false;
        }

        if (npc.IsAlive == false)
        {
            failure = PopulationMembershipFailure.DeadNpc;
            return false;
        }

        if (string.Equals(npc.ResidenceSettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(npc.ResidenceSettlementRuntimeId) == false)
        {
            failure = PopulationMembershipFailure.ResidenceAlreadyAssigned;
            return false;
        }

        List<NpcRuntime> rosterWithCandidate = new List<NpcRuntime>();
        if (knownNpcs != null)
        {
            foreach (NpcRuntime knownNpc in knownNpcs)
            {
                rosterWithCandidate.Add(knownNpc);
            }
        }

        rosterWithCandidate.Add(npc);
        SettlementPopulationPresenceSummary summary = SettlementPopulationPresenceQuery.BuildSummary(
            city,
            rosterWithCandidate);
        if (summary.NamedResidentCount >= summary.ResidentPopulation)
        {
            failure = PopulationMembershipFailure.AggregateCapacityExceeded;
            return false;
        }

        npc.SetResidenceSettlementRuntimeId(city.RuntimeId);
        return true;
    }
}
