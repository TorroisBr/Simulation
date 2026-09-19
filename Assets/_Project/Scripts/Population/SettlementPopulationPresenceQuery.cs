using System;
using System.Collections.Generic;

/// <summary>
/// Immutable read model for aggregate population, individualized residents and
/// their materialized physical presence. No population counters are persisted.
/// </summary>
public sealed class SettlementPopulationPresenceSummary
{
    public string SettlementRuntimeId { get; }
    public int ResidentPopulation { get; }
    public int NamedResidentCount { get; }
    public int NamedPresentCount { get; }
    public int IndividualizedResidentCount { get; }
    public int MaterializedResidentCount { get; }
    public int LegacyResidentNpcCount { get; }
    public int RepresentedResidentCount { get; }
    public int NamedLivingResidentCount => NamedResidentCount;

    public SettlementPopulationPresenceSummary(
        string settlementRuntimeId,
        int residentPopulation,
        int namedResidentCount,
        int namedPresentCount)
        : this(
            settlementRuntimeId,
            residentPopulation,
            0,
            0,
            namedResidentCount,
            namedPresentCount)
    {
    }

    public SettlementPopulationPresenceSummary(
        string settlementRuntimeId,
        int residentPopulation,
        int individualizedResidentCount,
        int materializedResidentCount,
        int legacyResidentNpcCount,
        int namedPresentCount)
    {
        SettlementRuntimeId = settlementRuntimeId;
        ResidentPopulation = residentPopulation;
        IndividualizedResidentCount = individualizedResidentCount;
        MaterializedResidentCount = materializedResidentCount;
        LegacyResidentNpcCount = legacyResidentNpcCount;
        RepresentedResidentCount = individualizedResidentCount + legacyResidentNpcCount;
        NamedResidentCount = RepresentedResidentCount;
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
        return BuildSummary(city, npcs, null);
    }

    public static SettlementPopulationPresenceSummary BuildSummary(
        CityRuntime city,
        IEnumerable<NpcRuntime> npcs,
        IEnumerable<PersonRuntime> persons)
    {
        if (city == null)
        {
            return null;
        }

        int namedPresentCount = 0;
        HashSet<string> individualizedResidentIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> materializedResidentIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> legacyResidentNpcIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, PersonRuntime> personsById = new Dictionary<string, PersonRuntime>(StringComparer.Ordinal);

        if (persons != null)
        {
            foreach (PersonRuntime person in persons)
            {
                if (person == null || person.PersonId == null || person.DeathAbsoluteDay.HasValue)
                {
                    continue;
                }

                string personId = person.PersonId.Value;
                if (personsById.ContainsKey(personId) == true)
                {
                    continue;
                }

                personsById.Add(personId, person);
                if (string.Equals(person.ResidenceSettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal))
                {
                    individualizedResidentIds.Add(personId);
                    if (person.IsMaterialized == true)
                    {
                        materializedResidentIds.Add(personId);
                    }
                }
            }
        }

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

                PersonRuntime boundPerson = npc.BoundPersonRuntime;
                if (boundPerson != null && boundPerson.PersonId != null)
                {
                    string personId = boundPerson.PersonId.Value;
                    if (boundPerson.DeathAbsoluteDay.HasValue)
                    {
                        continue;
                    }

                    if (personsById.ContainsKey(personId) == false)
                    {
                        personsById.Add(personId, boundPerson);
                    }
                    if (string.Equals(boundPerson.ResidenceSettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal))
                    {
                        individualizedResidentIds.Add(personId);
                        if (boundPerson.IsMaterialized == true)
                        {
                            materializedResidentIds.Add(personId);
                        }
                    }
                }
                else if (npc.PersonId != null)
                {
                    string personId = npc.PersonId.Value;
                    if (personsById.TryGetValue(personId, out PersonRuntime knownPerson) == true
                        && knownPerson.DeathAbsoluteDay.HasValue == false)
                    {
                        if (string.Equals(knownPerson.ResidenceSettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal))
                        {
                            individualizedResidentIds.Add(personId);
                            if (knownPerson.IsMaterialized == true)
                            {
                                materializedResidentIds.Add(personId);
                            }
                        }
                    }
                    else if (string.Equals(npc.ResidenceSettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal))
                    {
                        individualizedResidentIds.Add(personId);
                    }
                }
                else if (string.Equals(npc.ResidenceSettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal))
                {
                    legacyResidentNpcIds.Add(npc.RuntimeId);
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
            individualizedResidentIds.Count,
            materializedResidentIds.Count,
            legacyResidentNpcIds.Count,
            namedPresentCount);
    }

    public static SettlementPopulationPresenceSummary Query(
        CityRuntime city,
        IEnumerable<NpcRuntime> npcs)
    {
        return BuildSummary(city, npcs);
    }
}
