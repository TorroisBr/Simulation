using System;
using System.Collections.Generic;

/// <summary>
/// Immutable read model for the aggregate population and its materialized living NPCs.
/// Named counts are derived from the supplied NPC roster; no counters are persisted.
/// </summary>
public sealed class SettlementPopulationPresenceSummary
{
    public string SettlementRuntimeId { get; }
    public int ResidentPopulation { get; }
    public int NamedResidentCount { get; }
    public int NamedPresentCount { get; }

    public SettlementPopulationPresenceSummary(
        string settlementRuntimeId,
        int residentPopulation,
        int namedResidentCount,
        int namedPresentCount)
    {
        SettlementRuntimeId = settlementRuntimeId;
        ResidentPopulation = residentPopulation;
        NamedResidentCount = namedResidentCount;
        NamedPresentCount = namedPresentCount;
    }
}

/// <summary>
/// Pure queries for the distinction between resident membership and physical presence.
/// </summary>
public static class SettlementPopulationPresenceQuery
{
    public static SettlementPopulationPresenceSummary BuildSummary(
        CityRuntime city,
        IEnumerable<NpcRuntime> npcs)
    {
        if (city == null)
        {
            return null;
        }

        int namedResidentCount = 0;
        int namedPresentCount = 0;
        HashSet<string> seenNpcRuntimeIds = new HashSet<string>(StringComparer.Ordinal);

        if (npcs != null)
        {
            foreach (NpcRuntime npc in npcs)
            {
                if (npc == null
                    || npc.IsAlive == false
                    || string.IsNullOrWhiteSpace(npc.RuntimeId) == true
                    || seenNpcRuntimeIds.Add(npc.RuntimeId) == false)
                {
                    continue;
                }

                if (string.Equals(npc.ResidenceSettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal))
                {
                    namedResidentCount++;
                }

                if (npc.CurrentCity == city)
                {
                    namedPresentCount++;
                }
            }
        }

        return new SettlementPopulationPresenceSummary(
            city.RuntimeId,
            city.Population.CurrentPopulation,
            namedResidentCount,
            namedPresentCount);
    }

    public static SettlementPopulationPresenceSummary Query(
        CityRuntime city,
        IEnumerable<NpcRuntime> npcs)
    {
        return BuildSummary(city, npcs);
    }
}
