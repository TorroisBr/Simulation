using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>Detached Person-to-NPC and residence links used by P12-D graph validation.</summary>
internal sealed class DailyV1P12DPersonRelation
{
    internal string PersonId { get; }
    internal string ResidenceCityRuntimeId { get; }
    internal string MaterializedNpcRuntimeId { get; }

    internal DailyV1P12DPersonRelation(string personId, string residenceCityRuntimeId, string materializedNpcRuntimeId)
    {
        PersonId = personId;
        ResidenceCityRuntimeId = residenceCityRuntimeId;
        MaterializedNpcRuntimeId = materializedNpcRuntimeId;
    }
}

/// <summary>One detached parent-to-child identity edge used during P12-D validation.</summary>
internal sealed class DailyV1P12DParentageRelation
{
    internal string ParentPersonId { get; }
    internal string ChildPersonId { get; }

    internal DailyV1P12DParentageRelation(string parentPersonId, string childPersonId)
    {
        ParentPersonId = parentPersonId;
        ChildPersonId = childPersonId;
    }
}

/// <summary>Detached City membership and legacy Location binding used by P12-D validation.</summary>
internal sealed class DailyV1P12DCityRelation
{
    internal string CityRuntimeId { get; }
    internal string LocationRuntimeId { get; }
    internal IReadOnlyList<string> ImportantNpcRuntimeIds { get; }

    internal DailyV1P12DCityRelation(
        string cityRuntimeId,
        string locationRuntimeId,
        IReadOnlyList<string> importantNpcRuntimeIds)
    {
        CityRuntimeId = cityRuntimeId;
        LocationRuntimeId = locationRuntimeId;
        ImportantNpcRuntimeIds = importantNpcRuntimeIds == null
            ? null
            : new ReadOnlyCollection<string>(new List<string>(importantNpcRuntimeIds));
    }
}

/// <summary>Detached NPC identity, Person, City, Location, and residence links used by P12-D validation.</summary>
internal sealed class DailyV1P12DNpcRelation
{
    internal string NpcRuntimeId { get; }
    internal string PersonId { get; }
    internal string ResidenceCityRuntimeId { get; }
    internal string CurrentCityRuntimeId { get; }
    internal string CurrentLocationRuntimeId { get; }
    internal string DestinationCityRuntimeId { get; }
    internal string DestinationLocationRuntimeId { get; }

    internal DailyV1P12DNpcRelation(
        string npcRuntimeId,
        string personId,
        string residenceCityRuntimeId,
        string currentCityRuntimeId,
        string currentLocationRuntimeId,
        string destinationCityRuntimeId,
        string destinationLocationRuntimeId)
    {
        NpcRuntimeId = npcRuntimeId;
        PersonId = personId;
        ResidenceCityRuntimeId = residenceCityRuntimeId;
        CurrentCityRuntimeId = currentCityRuntimeId;
        CurrentLocationRuntimeId = currentLocationRuntimeId;
        DestinationCityRuntimeId = destinationCityRuntimeId;
        DestinationLocationRuntimeId = destinationLocationRuntimeId;
    }
}

/// <summary>
/// Core-owned validation of the accepted P12-D Person/Genealogy/City/NPC and
/// City/Location links. Unity projects its captured rows into these detached
/// values and consumes this result before it constructs host projections.
/// </summary>
internal static class DailyV1P12DRelationshipValidator
{
    /// <summary>
    /// Validates decoded P12-D owner values directly in Core before any host
    /// projections are constructed. This keeps restore relation authority
    /// beside the owner hydrators while retaining the same value-level rules
    /// used by legacy Unity staging.
    /// </summary>
    internal static bool TryValidate(
        PersonStoreOwnerSnapshot persons,
        GenealogyOwnerSnapshot genealogy,
        IReadOnlyList<DailyV1P12DCityCoreOwnerHydrationInput> cities,
        IReadOnlyList<P12DNpcDRow> npcs,
        SpatialNetworkOwnerSnapshot spatial)
    {
        if (persons?.Rows == null || genealogy?.Records == null || cities == null
            || npcs == null || spatial?.LocationRuntimeIds == null)
            return false;

        List<DailyV1P12DPersonRelation> personRelations =
            new List<DailyV1P12DPersonRelation>(persons.Rows.Count);
        foreach (PersonStoreOwnerSnapshotRow person in persons.Rows)
            personRelations.Add(person == null ? null : new DailyV1P12DPersonRelation(
                person.PersonIdValue, person.ResidenceSettlementRuntimeId,
                person.MaterializedNpcRuntimeId));

        List<DailyV1P12DParentageRelation> parentageRelations =
            new List<DailyV1P12DParentageRelation>(genealogy.Records.Count);
        foreach (ParentageRecord edge in genealogy.Records)
            parentageRelations.Add(edge == null ? null : new DailyV1P12DParentageRelation(
                edge.ParentId?.Value, edge.ChildId?.Value));

        List<DailyV1P12DCityRelation> cityRelations =
            new List<DailyV1P12DCityRelation>(cities.Count);
        Dictionary<string, string> cityLocationIds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DailyV1P12DCityCoreOwnerHydrationInput city in cities)
        {
            if (city == null)
            {
                cityRelations.Add(null);
                continue;
            }

            if (string.IsNullOrWhiteSpace(city.CityRuntimeId)
                || !cityLocationIds.TryAdd(city.CityRuntimeId, city.ResolvedLocationRuntimeId))
                return false;
            cityRelations.Add(new DailyV1P12DCityRelation(
                city.CityRuntimeId, city.LegacyLocationRuntimeId,
                city.ImportantNpcRuntimeIds));
        }

        List<DailyV1P12DNpcRelation> npcRelations =
            new List<DailyV1P12DNpcRelation>(npcs.Count);
        foreach (P12DNpcDRow npc in npcs)
            npcRelations.Add(npc == null ? null : new DailyV1P12DNpcRelation(
                npc.RuntimeId, npc.PersonIdValue, npc.ResidenceSettlementRuntimeId,
                npc.CurrentCityRuntimeId, npc.CurrentLocationRuntimeId,
                npc.DestinationCityRuntimeId, npc.DestinationLocationRuntimeId));

        return TryValidate(personRelations, parentageRelations, cityRelations, npcRelations,
            spatial.LocationRuntimeIds, cityLocationIds);
    }

    internal static bool TryValidate(
        IReadOnlyList<DailyV1P12DPersonRelation> persons,
        IReadOnlyList<DailyV1P12DParentageRelation> genealogy,
        IReadOnlyList<DailyV1P12DCityRelation> cities,
        IReadOnlyList<DailyV1P12DNpcRelation> npcs,
        IReadOnlyCollection<string> locationRuntimeIds,
        IReadOnlyDictionary<string, string> cityLocationIds)
    {
        if (persons == null || genealogy == null || cities == null || npcs == null
            || locationRuntimeIds == null || cityLocationIds == null)
            return false;

        HashSet<string> locationIds = new HashSet<string>(locationRuntimeIds, StringComparer.Ordinal);
        HashSet<string> personIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, DailyV1P12DPersonRelation> personsById =
            new Dictionary<string, DailyV1P12DPersonRelation>(StringComparer.Ordinal);
        Dictionary<string, string> personByNpcId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DailyV1P12DPersonRelation person in persons)
        {
            if (person == null || string.IsNullOrWhiteSpace(person.PersonId)
                || !personIds.Add(person.PersonId)
                || (person.ResidenceCityRuntimeId != null
                    && !cityLocationIds.ContainsKey(person.ResidenceCityRuntimeId)))
                return false;
            personsById.Add(person.PersonId, person);
            if (!string.IsNullOrWhiteSpace(person.MaterializedNpcRuntimeId)
                && !personByNpcId.TryAdd(person.MaterializedNpcRuntimeId, person.PersonId))
                return false;
        }

        foreach (DailyV1P12DParentageRelation edge in genealogy)
        {
            if (edge == null || !personIds.Contains(edge.ParentPersonId)
                || !personIds.Contains(edge.ChildPersonId))
                return false;
        }

        Dictionary<string, DailyV1P12DNpcRelation> npcsById =
            new Dictionary<string, DailyV1P12DNpcRelation>(StringComparer.Ordinal);
        foreach (DailyV1P12DNpcRelation npc in npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.NpcRuntimeId)
                || !npcsById.TryAdd(npc.NpcRuntimeId, npc)
                || (npc.CurrentLocationRuntimeId != null && !locationIds.Contains(npc.CurrentLocationRuntimeId))
                || (npc.DestinationLocationRuntimeId != null && !locationIds.Contains(npc.DestinationLocationRuntimeId))
                || (npc.CurrentCityRuntimeId != null && !cityLocationIds.ContainsKey(npc.CurrentCityRuntimeId))
                || (npc.DestinationCityRuntimeId != null && !cityLocationIds.ContainsKey(npc.DestinationCityRuntimeId))
                || (npc.ResidenceCityRuntimeId != null && !cityLocationIds.ContainsKey(npc.ResidenceCityRuntimeId)))
                return false;

            if (npc.CurrentCityRuntimeId != null
                && !string.Equals(npc.CurrentLocationRuntimeId,
                    cityLocationIds[npc.CurrentCityRuntimeId], StringComparison.Ordinal))
                return false;
            if (npc.DestinationCityRuntimeId != null
                && !string.Equals(npc.DestinationLocationRuntimeId,
                    cityLocationIds[npc.DestinationCityRuntimeId], StringComparison.Ordinal))
                return false;

            if (npc.PersonId != null)
            {
                if (!personsById.TryGetValue(npc.PersonId, out DailyV1P12DPersonRelation person)
                    || !string.Equals(person.MaterializedNpcRuntimeId, npc.NpcRuntimeId, StringComparison.Ordinal)
                    || !string.Equals(person.ResidenceCityRuntimeId,
                        npc.ResidenceCityRuntimeId, StringComparison.Ordinal))
                    return false;
            }
            else if (personByNpcId.ContainsKey(npc.NpcRuntimeId))
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, string> binding in personByNpcId)
        {
            if (!npcsById.TryGetValue(binding.Key, out DailyV1P12DNpcRelation npc)
                || !string.Equals(npc.PersonId, binding.Value, StringComparison.Ordinal))
                return false;
        }

        Dictionary<string, int> expectedMemberships = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (DailyV1P12DCityRelation city in cities)
        {
            if (city?.ImportantNpcRuntimeIds == null
                || string.IsNullOrWhiteSpace(city.CityRuntimeId)
                || !cityLocationIds.TryGetValue(city.CityRuntimeId, out string locationId)
                || !string.Equals(city.LocationRuntimeId, locationId, StringComparison.Ordinal)
                || !locationIds.Contains(locationId))
                return false;

            HashSet<string> cityMemberIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string npcId in city.ImportantNpcRuntimeIds)
            {
                if (string.IsNullOrWhiteSpace(npcId) || !cityMemberIds.Add(npcId)
                    || !expectedMemberships.TryAdd(npcId, 1)
                    || !npcsById.TryGetValue(npcId, out DailyV1P12DNpcRelation npc)
                    || !string.Equals(npc.CurrentCityRuntimeId, city.CityRuntimeId, StringComparison.Ordinal)
                    || !string.Equals(npc.CurrentLocationRuntimeId, locationId, StringComparison.Ordinal))
                    return false;
            }
        }

        foreach (DailyV1P12DNpcRelation npc in npcs)
        {
            int count = expectedMemberships.TryGetValue(npc.NpcRuntimeId, out int memberCount) ? memberCount : 0;
            if ((npc.CurrentCityRuntimeId == null && count != 0)
                || (npc.CurrentCityRuntimeId != null && count != 1))
                return false;
        }

        return true;
    }
}
