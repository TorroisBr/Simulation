using System;
using System.Collections.Generic;

/// <summary>Passive per-City witnesses for the ImportantNpcs presence projection.</summary>
public static class CityNpcPresenceCensusProvider
{
    public const string SectionIdPrefix = "p12b.city.important-npcs/";
    public const int SchemaVersion = 1;

    private sealed class SectionProvider : IOwnerSectionCensusProvider
    {
        private readonly CityRuntime owner;
        private readonly IReadOnlyList<NpcRuntime> installedNpcRoster;
        private readonly string sectionId;

        public SectionProvider(CityRuntime owner, IReadOnlyList<NpcRuntime> installedNpcRoster)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.installedNpcRoster = installedNpcRoster
                ?? throw new ArgumentNullException(nameof(installedNpcRoster));
            if (string.IsNullOrWhiteSpace(owner.RuntimeId))
                throw new ArgumentException("City presence census requires a City RuntimeId.", nameof(owner));
            sectionId = SectionIdFor(owner.RuntimeId);
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            ValidateReciprocalPresenceProjection();
            return new OwnerSectionCensusWitness(
                sectionId,
                SchemaVersion,
                owner,
                owner.ImportantNpcs.Count,
                owner.ImportantNpcRevision);
        }

        private void ValidateReciprocalPresenceProjection()
        {
            HashSet<NpcRuntime> rosterMembers = new HashSet<NpcRuntime>();
            HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < installedNpcRoster.Count; i++)
            {
                NpcRuntime npc = installedNpcRoster[i];
                if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                    || !rosterMembers.Add(npc) || !runtimeIds.Add(npc.RuntimeId))
                {
                    throw new InvalidOperationException(
                        "City presence census cannot validate a null or duplicate installed NPC owner.");
                }
            }

            IReadOnlyList<NpcRuntime> projection = owner.ImportantNpcs;
            HashSet<NpcRuntime> projectedMembers = new HashSet<NpcRuntime>();
            for (int i = 0; i < projection.Count; i++)
            {
                NpcRuntime npc = projection[i];
                if (npc == null || !projectedMembers.Add(npc)
                    || !rosterMembers.Contains(npc)
                    || !ReferenceEquals(npc.CurrentCity, owner)
                    || !ReferenceEquals(npc.CurrentLocation, owner.Location))
                {
                    throw new InvalidOperationException(
                        "City presence census found a duplicate or non-reciprocal City projection member.");
                }
            }

            foreach (NpcRuntime npc in rosterMembers)
            {
                bool claimsThisCity = ReferenceEquals(npc.CurrentCity, owner);
                bool isProjected = projectedMembers.Contains(npc);
                if (claimsThisCity != isProjected
                    || (claimsThisCity && !ReferenceEquals(npc.CurrentLocation, owner.Location)))
                {
                    throw new InvalidOperationException(
                        "City presence census found a non-reciprocal NPC presence owner.");
                }
            }
        }
    }

    public static string SectionIdFor(string cityRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(cityRuntimeId))
            throw new ArgumentException("City presence section requires a City RuntimeId.", nameof(cityRuntimeId));
        return SectionIdPrefix + cityRuntimeId;
    }

    /// <summary>Creates one stable, ordinally ordered section for each installed City.</summary>
    public static IReadOnlyList<IOwnerSectionCensusProvider> CreateProviders(
        IReadOnlyList<CityRuntime> cities,
        IReadOnlyList<NpcRuntime> installedNpcRoster)
    {
        if (cities == null) throw new ArgumentNullException(nameof(cities));
        if (installedNpcRoster == null) throw new ArgumentNullException(nameof(installedNpcRoster));

        List<CityRuntime> orderedCities = new List<CityRuntime>(cities.Count);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < cities.Count; i++)
        {
            CityRuntime city = cities[i];
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                || !runtimeIds.Add(city.RuntimeId))
            {
                throw new ArgumentException(
                    "City presence census requires non-null Cities with unique RuntimeIds.",
                    nameof(cities));
            }
            orderedCities.Add(city);
        }

        orderedCities.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        IOwnerSectionCensusProvider[] providers = new IOwnerSectionCensusProvider[orderedCities.Count];
        for (int i = 0; i < orderedCities.Count; i++)
            providers[i] = new SectionProvider(orderedCities[i], installedNpcRoster);
        return Array.AsReadOnly(providers);
    }
}
