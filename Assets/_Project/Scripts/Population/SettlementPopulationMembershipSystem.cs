using System;
using System.Collections.Generic;

/// <summary>
/// Explicit snapshot of the complete named-NPC roster known to the world boundary.
/// Membership operations require this type so an arbitrary partial IEnumerable cannot
/// silently be treated as authoritative for aggregate-capacity validation.
/// </summary>
public sealed class AuthoritativeNpcRoster
{
    private readonly IReadOnlyList<NpcRuntime> npcs;

    public IReadOnlyList<NpcRuntime> Npcs => npcs;

    private AuthoritativeNpcRoster(IReadOnlyList<NpcRuntime> npcs)
    {
        if (npcs == null)
        {
            throw new ArgumentNullException(nameof(npcs));
        }

        List<NpcRuntime> snapshot = new List<NpcRuntime>();
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (NpcRuntime npc in npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId) == true)
            {
                throw new ArgumentException(
                    "An authoritative NPC roster cannot contain null or unidentified NPCs.",
                    nameof(npcs));
            }

            if (runtimeIds.Add(npc.RuntimeId) == false)
            {
                throw new ArgumentException(
                    "An authoritative NPC roster cannot contain duplicate RuntimeIds.",
                    nameof(npcs));
            }

            snapshot.Add(npc);
        }

        this.npcs = snapshot.AsReadOnly();
    }
}

public enum PopulationMembershipFailure
{
    None = 0,
    InvalidNpc = 1,
    DeadNpc = 2,
    InvalidSettlement = 3,
    ResidenceAlreadyAssigned = 4,
    AggregateCapacityExceeded = 5,
    AuthoritativeRosterRequired = 6
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
        AuthoritativeNpcRoster authoritativeRoster,
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

        if (authoritativeRoster == null)
        {
            failure = PopulationMembershipFailure.AuthoritativeRosterRequired;
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
        foreach (NpcRuntime knownNpc in authoritativeRoster.Npcs)
        {
            rosterWithCandidate.Add(knownNpc);
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

    public static bool TryBindExistingResident(
        CityRuntime city,
        NpcRuntime npc,
        IEnumerable<NpcRuntime> nonAuthoritativeRoster,
        out PopulationMembershipFailure failure)
    {
        failure = PopulationMembershipFailure.AuthoritativeRosterRequired;
        return false;
    }
}
