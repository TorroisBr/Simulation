using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Read-only join of the exact selected City and NPC owner compositions held
/// by one runtime. The graph indexes the existing authorities and owns no
/// mutable simulation state itself.
/// </summary>
internal sealed class DailyV1SelectedRuntimeOwnerGraph
{
    private readonly ReadOnlyCollection<DailyV1CityRuntimeOwners> cities;
    private readonly ReadOnlyCollection<DailyV1NpcRuntimeOwners> npcs;

    internal DailyV1EffectiveDefinitions Definitions { get; }
    internal IReadOnlyList<DailyV1CityRuntimeOwners> Cities => cities;
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Npcs => npcs;

    private DailyV1SelectedRuntimeOwnerGraph(
        DailyV1EffectiveDefinitions definitions,
        List<DailyV1CityRuntimeOwners> cities,
        List<DailyV1NpcRuntimeOwners> npcs)
    {
        Definitions = definitions;
        this.cities = cities.AsReadOnly();
        this.npcs = npcs.AsReadOnly();
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (definitions == null || cityOwners == null || npcOwners == null) return false;

        List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>();
        List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>();
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners owner in cityOwners)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || owner.Definition == null || !ReferenceEquals(owner.Definitions, definitions)
                || !definitions.Cities.TryGetValue(owner.Definition.DefinitionId, out DailyV1CityDefinition city)
                || !ReferenceEquals(city, owner.Definition)
                || !runtimeIds.Add(owner.RuntimeId)) return false;
            cities.Add(owner);
        }
        foreach (DailyV1NpcRuntimeOwners owner in npcOwners)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || owner.Definition == null || !ReferenceEquals(owner.Definitions, definitions)
                || !definitions.Npcs.TryGetValue(owner.Definition.DefinitionId, out DailyV1NpcDefinition npc)
                || !ReferenceEquals(npc, owner.Definition)
                || !runtimeIds.Add(owner.RuntimeId)) return false;
            npcs.Add(owner);
        }

        graph = new DailyV1SelectedRuntimeOwnerGraph(definitions, cities, npcs);
        return true;
    }
}
