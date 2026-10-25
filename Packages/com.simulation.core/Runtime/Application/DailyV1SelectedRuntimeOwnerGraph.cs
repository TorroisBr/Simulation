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
    internal SpatialNetworkOwner SpatialNetwork { get; }
    internal DailyV1JusticeRecordsOwner JusticeRecordsOwner { get; }
    internal TravelPartyStore TravelParties { get; }
    internal DailyV1RuntimeIdentityIndex RuntimeIdentityIndex { get; }
    internal ExpeditionStore Expeditions { get; private set; }
    internal PersonStore PersonStore { get; private set; }
    internal PersonSpatialPositionStore PersonSpatialPositionStore { get; private set; }
    internal LegacySpatialAnchorBindingStore LegacySpatialAnchors { get; private set; }
    internal ScheduledDirectiveStore ScheduledDirectives { get; private set; }

    private DailyV1SelectedRuntimeOwnerGraph(
        DailyV1EffectiveDefinitions definitions,
        List<DailyV1CityRuntimeOwners> cities,
        List<DailyV1NpcRuntimeOwners> npcs,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        PersonStore personStore)
    {
        Definitions = definitions;
        this.cities = cities.AsReadOnly();
        this.npcs = npcs.AsReadOnly();
        SpatialNetwork = spatialNetwork;
        JusticeRecordsOwner = justiceRecordsOwner;
        TravelParties = travelParties;
        RuntimeIdentityIndex = runtimeIdentityIndex;
        PersonStore = personStore;
    }

    /// <summary>
    /// Completes the unpublished genesis graph with the exact selected
    /// PersonStore authority after the runtime constructor has bound NPC
    /// memberships. The binding is one-time; a published graph never changes
    /// owner identity.
    /// </summary>
    internal bool TryBindPersonStore(PersonStore personStore)
    {
        if (personStore == null) return false;
        if (PersonStore != null) return ReferenceEquals(PersonStore, personStore);
        if (!MatchesPersonStore(personStore, npcs)) return false;
        PersonStore = personStore;
        return true;
    }

    /// <summary>Binds the exact P8-C position owner installed by the runtime.</summary>
    internal bool TryBindPersonSpatialPositionStore(PersonSpatialPositionStore store)
    {
        if (store == null || PersonStore == null
            || !ReferenceEquals(store.PersonStore, PersonStore)
            || (LegacySpatialAnchors != null
                && !ReferenceEquals(LegacySpatialAnchors.SpatialLocationLookup,
                    store.SpatialAuthorityStore))) return false;
        if (PersonSpatialPositionStore != null)
            return ReferenceEquals(PersonSpatialPositionStore, store);
        PersonSpatialPositionStore = store;
        return true;
    }

    /// <summary>
    /// Binds the exact P8 anchor authority used by the runtime spatial owner.
    /// Selected action eligibility reads this Core-owned relation instead of
    /// asking a Unity runtime projection to infer current-city location.
    /// </summary>
    internal bool TryBindLegacySpatialAnchorBindingStore(LegacySpatialAnchorBindingStore store)
    {
        PersonSpatialPositionStore positions = PersonSpatialPositionStore;
        if (store == null || positions == null
            || !ReferenceEquals(store.SpatialLocationLookup, positions.SpatialAuthorityStore))
            return false;
        if (LegacySpatialAnchors != null)
            return ReferenceEquals(LegacySpatialAnchors, store);
        LegacySpatialAnchors = store;
        return true;
    }

    /// <summary>
    /// Resolves current-city location from the selected Core owner graph and
    /// its exact P8 position/anchor authorities. An absent Person position
    /// retains the legacy no-position behavior; transit and unresolved or
    /// mismatched Location facts fail closed.
    /// </summary>
    internal bool IsActorAtCurrentCityLocation(DailyV1NpcRuntimeOwners actor)
    {
        if (actor == null || !ContainsExactNpcOwner(actor)
            || string.IsNullOrWhiteSpace(actor.Travel?.CurrentCityRuntimeId)
            || !TryFindCity(actor.Travel.CurrentCityRuntimeId,
                out DailyV1CityRuntimeOwners city))
            return false;

        PersonSpatialPositionStore positions = PersonSpatialPositionStore;
        if (positions == null || PersonStore == null
            || !PersonStore.TryGetByMaterializedNpcRuntimeId(
                actor.RuntimeId, out PersonRuntime person)
            || !positions.TryGetPosition(person.PersonId,
                out PersonSpatialPosition position))
            return true;

        if (position == null || position.IsInTransit)
            return false;

        LegacySpatialAnchorBindingStore anchors = LegacySpatialAnchors;
        if (anchors == null
            || !anchors.TryGet(new SpatialAnchorOwnerId(
                SpatialAnchorOwnerKind.City, city.RuntimeId),
                out LocationId cityLocationId)
            || cityLocationId == null
            || !positions.SpatialAuthorityStore.TryGet(cityLocationId, out _))
            return false;

        return position.Position != null
            && position.Position.Kind == StablePositionReferenceKind.Location
            && position.Position.LocationId.Equals(cityLocationId);
    }

    internal bool IsPersonInSpatialTransit(PersonId personId)
    {
        PersonSpatialPositionStore store = PersonSpatialPositionStore;
        return personId != null
            && store != null
            && store.TryGetPosition(personId, out PersonSpatialPosition position)
            && position.IsInTransit;
    }

    /// <summary>
    /// Binds the exact configured directive store after Core genesis. Restored
    /// graphs may leave this unset until the persisted store is staged.
    /// </summary>
    internal bool TryBindScheduledDirectives(ScheduledDirectiveStore store)
    {
        if (store == null) return false;
        if (ScheduledDirectives != null) return ReferenceEquals(ScheduledDirectives, store);
        ScheduledDirectives = store;
        return true;
    }

    /// <summary>
    /// Binds the exact expedition owner used by the runtime after genesis.
    /// Actor eligibility reads this owner from the selected graph so a host
    /// cannot substitute a parallel expedition store at the turn boundary.
    /// </summary>
    internal bool TryBindExpeditionStore(ExpeditionStore store)
    {
        if (store == null) return false;
        if (Expeditions != null) return ReferenceEquals(Expeditions, store);
        Expeditions = store;
        return true;
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreate(definitions, cityOwners, npcOwners, null, null, out graph);
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreate(definitions, cityOwners, npcOwners, spatialNetwork, null, out graph);
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreate(definitions, cityOwners, npcOwners, spatialNetwork,
            justiceRecordsOwner, null, out graph);
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreate(definitions, cityOwners, npcOwners, spatialNetwork,
            justiceRecordsOwner, travelParties, null, out graph);
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreateCore(definitions, cityOwners, npcOwners, spatialNetwork,
            justiceRecordsOwner, travelParties, runtimeIdentityIndex, null, null, 0, out graph);
    }

    internal static bool TryCreate(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        PersonStore personStore,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreateCore(definitions, cityOwners, npcOwners, spatialNetwork,
            justiceRecordsOwner, travelParties, runtimeIdentityIndex, personStore, null, 0, out graph);
    }

    private static bool TryCreateCore(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        PersonStore personStore,
        string pendingNpcIdentityId,
        int pendingNpcMembershipDelta,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (definitions == null || cityOwners == null || npcOwners == null) return false;

        List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>();
        List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>();
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
        if (spatialNetwork != null)
        {
            foreach (SpatialLocationRuntime location in spatialNetwork.Locations)
                if (location == null || string.IsNullOrWhiteSpace(location.RuntimeId)
                    || !runtimeIds.Add(location.RuntimeId)) return false;
            foreach (SpatialRouteRuntime route in spatialNetwork.Routes)
                if (route == null || string.IsNullOrWhiteSpace(route.RuntimeId)
                    || !runtimeIds.Add(route.RuntimeId)) return false;
        }
        foreach (DailyV1CityRuntimeOwners owner in cityOwners)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || string.IsNullOrWhiteSpace(owner.LocationRuntimeId)
                || owner.Definition == null || !ReferenceEquals(owner.Definitions, definitions)
                || !definitions.Cities.TryGetValue(owner.Definition.DefinitionId, out DailyV1CityDefinition city)
                || !ReferenceEquals(city, owner.Definition)
                || (spatialNetwork != null && !spatialNetwork.ContainsLocationId(owner.LocationRuntimeId))
                || !runtimeIds.Add(owner.RuntimeId)
                || !locationIds.Add(owner.LocationRuntimeId)
                || (spatialNetwork == null && !runtimeIds.Add(owner.LocationRuntimeId))) return false;
            cities.Add(owner);
        }
        foreach (DailyV1NpcRuntimeOwners owner in npcOwners)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId)
                || owner.Definition == null || !ReferenceEquals(owner.Definitions, definitions)
                || !definitions.Npcs.TryGetValue(owner.Definition.DefinitionId, out DailyV1NpcDefinition npc)
                || !ReferenceEquals(npc, owner.Definition)
                || !runtimeIds.Add(owner.RuntimeId)
                || !MatchesCity(cities, owner.MerchantTradePlan?.OriginCityRuntimeId)
                || !MatchesCity(cities, owner.MerchantTradePlan?.TargetCityRuntimeId)
                || !MatchesCityLocation(cities, owner.Travel?.CurrentCityRuntimeId,
                    owner.Travel?.CurrentLocationRuntimeId)
                || !MatchesCityLocation(cities, owner.Travel?.DestinationCityRuntimeId,
                    owner.Travel?.DestinationLocationRuntimeId)
                || (spatialNetwork != null
                    && (!ContainsLocationWhenPresent(spatialNetwork, owner.Travel?.CurrentLocationRuntimeId)
                        || !ContainsLocationWhenPresent(spatialNetwork, owner.Travel?.DestinationLocationRuntimeId)))) return false;
            npcs.Add(owner);
        }

        if (justiceRecordsOwner != null
            && !MatchesJusticeRecords(cities, npcs, justiceRecordsOwner)) return false;

        if (personStore != null && !MatchesPersonStore(personStore, npcs)) return false;

        if (runtimeIdentityIndex != null
            && !MatchesRuntimeIdentityIndex(runtimeIdentityIndex, cities, npcs, spatialNetwork,
                pendingNpcIdentityId, pendingNpcMembershipDelta)) return false;

        graph = new DailyV1SelectedRuntimeOwnerGraph(
            definitions, cities, npcs, spatialNetwork, justiceRecordsOwner,
            travelParties, runtimeIdentityIndex, personStore);
        return true;
    }

    private static bool MatchesPersonStore(
        PersonStore personStore,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcs)
    {
        HashSet<string> boundNpcRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        int bindingCount = 0;
        foreach (PersonRuntime person in personStore.Persons)
        {
            if (person == null || person.PersonId == null) return false;
            string npcRuntimeId = person.MaterializedNpcRuntimeId;
            if (npcRuntimeId == null) continue;
            if (string.IsNullOrWhiteSpace(npcRuntimeId)
                || !boundNpcRuntimeIds.Add(npcRuntimeId)
                || !ContainsNpcRuntimeId(npcs, npcRuntimeId)
                || !personStore.TryGetByMaterializedNpcRuntimeId(
                    npcRuntimeId, out PersonRuntime indexedPerson)
                || !ReferenceEquals(person, indexedPerson)) return false;
            bindingCount++;
        }

        return bindingCount == personStore.MaterializedBindingCount;
    }

    private static bool MatchesRuntimeIdentityIndex(
        DailyV1RuntimeIdentityIndex identityIndex,
        IReadOnlyList<DailyV1CityRuntimeOwners> cities,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcs,
        SpatialNetworkOwner spatialNetwork,
        string pendingNpcIdentityId,
        int pendingNpcMembershipDelta)
    {
        if (pendingNpcMembershipDelta < -1 || pendingNpcMembershipDelta > 1
            || (pendingNpcMembershipDelta == 0) != (pendingNpcIdentityId == null)) return false;
        if (pendingNpcMembershipDelta == 1
            && !identityIndex.CanRegister(pendingNpcIdentityId)) return false;
        if (pendingNpcMembershipDelta == -1
            && !identityIndex.HasIdentity(pendingNpcIdentityId, DailyV1RuntimeIdentityKind.Npc)) return false;

        foreach (DailyV1CityRuntimeOwners city in cities)
            if (!identityIndex.HasIdentity(city.RuntimeId, DailyV1RuntimeIdentityKind.City)
                || !identityIndex.HasIdentity(city.LocationRuntimeId, DailyV1RuntimeIdentityKind.Location)) return false;
        foreach (DailyV1NpcRuntimeOwners npc in npcs)
            if (string.Equals(npc.RuntimeId, pendingNpcIdentityId, StringComparison.Ordinal)
                ? !identityIndex.CanRegister(npc.RuntimeId)
                : !identityIndex.HasIdentity(npc.RuntimeId, DailyV1RuntimeIdentityKind.Npc)) return false;
        if (spatialNetwork != null)
        {
            foreach (SpatialLocationRuntime location in spatialNetwork.Locations)
                if (!identityIndex.HasIdentity(location.RuntimeId, DailyV1RuntimeIdentityKind.Location)) return false;
            foreach (SpatialRouteRuntime route in spatialNetwork.Routes)
                if (!identityIndex.HasIdentity(route.RuntimeId, DailyV1RuntimeIdentityKind.Route)) return false;
        }

        int locationCount = spatialNetwork != null
            ? Count(spatialNetwork.Locations)
            : CountDistinctCityLocations(cities);
        int routeCount = spatialNetwork != null ? spatialNetwork.RouteCount : 0;
        return identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.City) == cities.Count
            && identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Npc) == npcs.Count - pendingNpcMembershipDelta
            && identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Location) == locationCount
            && identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.Route) == routeCount
            && identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.ExplorableSite) == 0
            && identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.LocalPlace) == 0
            && identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.LocalConnection) == 0
            && identityIndex.GetCardinality(DailyV1RuntimeIdentityKind.NotableItem) == 0;
    }

    private static int Count<T>(IEnumerable<T> values)
    {
        int count = 0;
        foreach (T _ in values) count++;
        return count;
    }

    private static int CountDistinctCityLocations(IReadOnlyList<DailyV1CityRuntimeOwners> cities)
    {
        HashSet<string> locations = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in cities) locations.Add(city.LocationRuntimeId);
        return locations.Count;
    }

    private static bool MatchesJusticeRecords(
        IReadOnlyList<DailyV1CityRuntimeOwners> cities,
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcs,
        DailyV1JusticeRecordsOwner justiceRecordsOwner)
    {
        foreach (DailyV1JusticeWantedRecordOwner row in justiceRecordsOwner.WantedRecords)
            if (row == null
                || !ContainsNpcRuntimeId(npcs, row.TargetRuntimeId)
                || !ContainsCityRuntimeId(cities, row.CityRuntimeId)) return false;

        foreach (DailyV1JusticeSentenceOwner sentence in justiceRecordsOwner.Sentences)
            if (sentence == null
                || !ContainsNpcRuntimeId(npcs, sentence.TargetRuntimeId)
                || !ContainsCityRuntimeId(cities, sentence.CityRuntimeId)
                || sentence.Warrant == null
                || !justiceRecordsOwner.ContainsWantedRecord(sentence.Warrant)
                || !string.Equals(sentence.TargetRuntimeId, sentence.Warrant.TargetRuntimeId, StringComparison.Ordinal)
                || !string.Equals(sentence.CityRuntimeId, sentence.Warrant.CityRuntimeId, StringComparison.Ordinal)) return false;

        return true;
    }

    private static bool ContainsNpcRuntimeId(
        IReadOnlyList<DailyV1NpcRuntimeOwners> npcs,
        string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId)) return false;
        foreach (DailyV1NpcRuntimeOwners npc in npcs)
            if (string.Equals(npc.RuntimeId, runtimeId, StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool ContainsCityRuntimeId(
        IReadOnlyList<DailyV1CityRuntimeOwners> cities,
        string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId)) return false;
        foreach (DailyV1CityRuntimeOwners city in cities)
            if (string.Equals(city.RuntimeId, runtimeId, StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool ContainsLocationWhenPresent(SpatialNetworkOwner spatialNetwork, string locationRuntimeId)
    {
        return locationRuntimeId == null
            || (!string.IsNullOrWhiteSpace(locationRuntimeId)
                && spatialNetwork.ContainsLocationId(locationRuntimeId));
    }

    internal static bool TryCreateFromExactOwners(
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreateFromExactOwners(cityOwners, npcOwners, null, null, out graph);
    }

    internal static bool TryCreateFromExactOwners(
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreateFromExactOwners(cityOwners, npcOwners, spatialNetwork, null, out graph);
    }

    internal static bool TryCreateFromExactOwners(
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreateFromExactOwners(cityOwners, npcOwners, spatialNetwork,
            justiceRecordsOwner, null, out graph);
    }

    internal static bool TryCreateFromExactOwners(
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreateFromExactOwners(cityOwners, npcOwners, spatialNetwork,
            justiceRecordsOwner, travelParties, null, out graph);
    }

    internal static bool TryCreateFromExactOwners(
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryCreateFromExactOwners(cityOwners, npcOwners, spatialNetwork,
            justiceRecordsOwner, travelParties, runtimeIdentityIndex, null, out graph);
    }

    internal static bool TryCreateFromExactOwners(
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        DailyV1RuntimeIdentityIndex runtimeIdentityIndex,
        PersonStore personStore,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (cityOwners == null || npcOwners == null) return false;

        List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>();
        List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>();
        DailyV1EffectiveDefinitions definitions = null;
        foreach (DailyV1CityRuntimeOwners owner in cityOwners)
        {
            if (owner == null || owner.Definitions == null) return false;
            if (definitions == null) definitions = owner.Definitions;
            else if (!ReferenceEquals(definitions, owner.Definitions)) return false;
            cities.Add(owner);
        }
        foreach (DailyV1NpcRuntimeOwners owner in npcOwners)
        {
            if (owner == null || owner.Definitions == null) return false;
            if (definitions == null) definitions = owner.Definitions;
            else if (!ReferenceEquals(definitions, owner.Definitions)) return false;
            npcs.Add(owner);
        }

        return definitions != null
            && TryCreate(definitions, cities, npcs, spatialNetwork,
                justiceRecordsOwner, travelParties, runtimeIdentityIndex, personStore, out graph);
    }

    internal static bool TryCreateInitialCityOwner(
        string runtimeId,
        string locationRuntimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        MarketCounterpartyRuntime suppliedCounterparty,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (!DailyV1CityRuntimeOwners.TryCreateInitial(
                runtimeId, locationRuntimeId, definitions, definition, suppliedCounterparty,
                out DailyV1CityRuntimeOwners candidate)
            || !TryCreate(definitions, new[] { candidate }, Array.Empty<DailyV1NpcRuntimeOwners>(), out _))
            return false;

        owners = candidate;
        return true;
    }

    internal static bool TryCreateBoundCityOwner(
        string runtimeId,
        string locationRuntimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1CityDefinition definition,
        SettlementPopulationRuntime population,
        DailyV1MarketOwner market,
        MarketCounterpartyRuntime counterparty,
        PopulationEconomyRuntime populationEconomy,
        DailyV1CityNpcPresenceOwner npcPresence,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (!DailyV1CityRuntimeOwners.TryCreateBound(
                runtimeId, locationRuntimeId, definitions, definition, population, market, counterparty,
                populationEconomy, npcPresence, out DailyV1CityRuntimeOwners candidate)
            || !TryCreate(definitions, new[] { candidate }, Array.Empty<DailyV1NpcRuntimeOwners>(), out _))
            return false;

        owners = candidate;
        return true;
    }

    private static bool MatchesCityLocation(
        IReadOnlyList<DailyV1CityRuntimeOwners> cities,
        string cityRuntimeId,
        string locationRuntimeId)
    {
        if (cityRuntimeId == null) return true;
        if (string.IsNullOrWhiteSpace(cityRuntimeId) || string.IsNullOrWhiteSpace(locationRuntimeId)) return false;
        foreach (DailyV1CityRuntimeOwners city in cities)
            if (string.Equals(city.RuntimeId, cityRuntimeId, StringComparison.Ordinal))
                return string.Equals(city.LocationRuntimeId, locationRuntimeId, StringComparison.Ordinal);
        return false;
    }

    private static bool MatchesCity(
        IReadOnlyList<DailyV1CityRuntimeOwners> cities,
        string cityRuntimeId)
    {
        if (cityRuntimeId == null) return true;
        if (string.IsNullOrWhiteSpace(cityRuntimeId)) return false;
        foreach (DailyV1CityRuntimeOwners city in cities)
            if (string.Equals(city.RuntimeId, cityRuntimeId, StringComparison.Ordinal)) return true;
        return false;
    }

    internal static bool TryCreateInitialNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        float initialMoney,
        out DailyV1NpcRuntimeOwners owners)
    {
        return TryCreateInitialNpcOwner(runtimeId, definitions, definition, initialMoney,
            string.IsNullOrWhiteSpace(runtimeId) ? null : new SpatialKnowledgeRuntime(runtimeId), out owners);
    }

    internal static bool TryCreateInitialNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        float initialMoney,
        SpatialKnowledgeRuntime spatialKnowledge,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (!DailyV1NpcRuntimeOwners.TryCreateInitial(
                runtimeId, definitions, definition, initialMoney, spatialKnowledge,
                out DailyV1NpcRuntimeOwners candidate)
            || !TryCreate(definitions, Array.Empty<DailyV1CityRuntimeOwners>(), new[] { candidate }, out _))
            return false;

        owners = candidate;
        return true;
    }

    internal static bool TryCreateBoundNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        DailyV1CommercialKnowledgeOwner commercialKnowledge,
        out DailyV1NpcRuntimeOwners owners)
    {
        return TryCreateBoundNpcOwner(runtimeId, definitions, definition, statuses,
            inventory, moneyAccount, commercialKnowledge, null, out owners);
    }

    internal static bool TryCreateBoundNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        DailyV1CommercialKnowledgeOwner commercialKnowledge,
        DailyV1NpcMerchantTradePlanOwner merchantTradePlan,
        out DailyV1NpcRuntimeOwners owners)
    {
        return TryCreateBoundNpcOwner(runtimeId, definitions, definition, statuses,
            inventory, moneyAccount, commercialKnowledge, merchantTradePlan,
            string.IsNullOrWhiteSpace(runtimeId) ? null : new SpatialKnowledgeRuntime(runtimeId), out owners);
    }

    internal static bool TryCreateBoundNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition definition,
        IEnumerable<DailyV1StatusDefinition> statuses,
        DailyV1InventoryOwner inventory,
        MoneyAccountRuntime moneyAccount,
        DailyV1CommercialKnowledgeOwner commercialKnowledge,
        DailyV1NpcMerchantTradePlanOwner merchantTradePlan,
        SpatialKnowledgeRuntime spatialKnowledge,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (spatialKnowledge == null) return false;
        if (merchantTradePlan == null
            && (definitions == null || !DailyV1NpcMerchantTradePlanOwner.TryCreateInitial(
                definitions, out merchantTradePlan))) return false;
        bool created = DailyV1NpcRuntimeOwners.TryCreateBound(
            runtimeId, definitions, definition, statuses, inventory, moneyAccount,
            commercialKnowledge, merchantTradePlan, spatialKnowledge,
            out DailyV1NpcRuntimeOwners candidate);
        // A bound NPC can be staged before its City owners are composed (notably
        // during detached P12 restore). Cross-owner City joins are validated by
        // the complete runtime graph after all City authorities are available.
        if (!created || candidate == null || !ReferenceEquals(candidate.Definitions, definitions))
            return false;

        owners = candidate;
        return true;
    }

    internal bool MatchesExactOwnerInstances(
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners)
    {
        return MatchesExactOwnerInstances(cities, cityOwners)
            && MatchesExactOwnerInstances(npcs, npcOwners);
    }

    /// <summary>Installs a selected merchant plan into its exact NPC owner.</summary>
    internal bool TryInstallMerchantTradePlan(
        DailyV1NpcRuntimeOwners npc,
        DailyV1ItemDefinition item,
        string originCityRuntimeId,
        string targetCityRuntimeId,
        int plannedAmount,
        float purchasePricePerItem,
        string originDecisionId)
    {
        if (npc == null || item == null || !ContainsExactNpc(npc)
            || !Definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admittedItem)
            || !ReferenceEquals(admittedItem, item)
            || !ContainsCityId(originCityRuntimeId) || !ContainsCityId(targetCityRuntimeId)
            || plannedAmount <= 0 || float.IsNaN(purchasePricePerItem)
            || float.IsInfinity(purchasePricePerItem) || purchasePricePerItem < 0f)
            return false;

        return npc.MerchantTradePlan.TryInstallState(item, originCityRuntimeId,
            targetCityRuntimeId, plannedAmount, plannedAmount, purchasePricePerItem, 0, 0,
            originDecisionId);
    }

    /// <summary>Plans and installs the selected merchant's direct trade route.</summary>
    internal bool TryCommitMerchantTradeTravelPlan(
        DailyV1NpcRuntimeOwners npc,
        string targetCityRuntimeId,
        float utility,
        string originDecisionId,
        Func<bool> admitMutation,
        out float expectedCost)
    {
        expectedCost = -1f;
        if (npc == null || !ContainsExactNpc(npc) || SpatialNetwork == null
            || float.IsNaN(utility) || float.IsInfinity(utility) || utility < 0f
            || string.IsNullOrWhiteSpace(targetCityRuntimeId)
            || !string.Equals(targetCityRuntimeId,
                npc.MerchantTradePlan?.TargetCityRuntimeId, StringComparison.Ordinal)
            || !SpatialNetwork.TryGetLocation(npc.Travel?.CurrentLocationRuntimeId,
                out SpatialLocationRuntime origin)
            || !TryFindCity(targetCityRuntimeId, out DailyV1CityRuntimeOwners target)
            || !SpatialNetwork.TryGetLocation(target.LocationRuntimeId,
                out SpatialLocationRuntime destination)
            || !SpatialNetwork.TryGetSingleDirectRoute(origin, destination,
                out SpatialRouteRuntime route, out _)) return false;

        if (!TryGetMerchantTradeTravelCost(route, out float cost)
            || !npc.TravelPlan.CanCommit(destination.RuntimeId, target.RuntimeId,
                NpcTravelReason.Trade, utility, cost, originDecisionId)
            || (!npc.TravelPlan.Matches(destination.RuntimeId, target.RuntimeId,
                    NpcTravelReason.Trade, utility, cost, originDecisionId)
                && (admitMutation == null || !admitMutation()))
            || !npc.TravelPlan.TryCommit(destination.RuntimeId, target.RuntimeId,
                NpcTravelReason.Trade, utility, cost, originDecisionId)) return false;
        expectedCost = cost;
        return true;
    }

    /// <summary>
    /// Commits the selected FleeCity action's pending travel plan against the
    /// exact selected NPC and destination owners. The Unity host may preflight
    /// and later refresh its City/Location projections, but it does not own the
    /// TravelPlan mutation. P12 admission and publication run only for a real
    /// owner transition and remain adjacent to that leaf write.
    /// </summary>
    internal bool TryCommitSelectedFleeCityTravelPlan(
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners destination,
        float expectedCost,
        string originDecisionId,
        Func<bool> admitMutation,
        Action mutationCommitted,
        out bool changed)
    {
        changed = false;
        if (npc == null || !ContainsExactNpc(npc)
            || destination == null || !ContainsExactCityOwner(destination)
            || !ReferenceEquals(npc.Definitions, destination.Definitions)
            || npc.Lifecycle?.LifeState != NpcLifeState.Alive
            || npc.Travel == null || npc.Travel.IsTraveling
            || npc.TravelPlan == null
            || SpatialNetwork == null
            || !SpatialNetwork.TryGetLocation(destination.LocationRuntimeId,
                out SpatialLocationRuntime destinationLocation)
            || !ContainsCityRuntimeLocation(destination.RuntimeId, destinationLocation.RuntimeId))
            return false;

        string normalizedDecisionId = string.IsNullOrWhiteSpace(originDecisionId)
            ? null
            : originDecisionId;
        DailyV1NpcTravelPlanOwner travelPlan = npc.TravelPlan;
        if (!travelPlan.CanCommit(destinationLocation.RuntimeId, destination.RuntimeId,
                NpcTravelReason.Flee, 80f, expectedCost, normalizedDecisionId))
            return false;

        changed = !travelPlan.Matches(destinationLocation.RuntimeId, destination.RuntimeId,
            NpcTravelReason.Flee, 80f, expectedCost, normalizedDecisionId);
        if (changed
            && (admitMutation == null || mutationCommitted == null || !admitMutation()))
        {
            changed = false;
            return false;
        }

        if (!travelPlan.TryCommit(destinationLocation.RuntimeId, destination.RuntimeId,
                NpcTravelReason.Flee, 80f, expectedCost, normalizedDecisionId))
        {
            changed = false;
            return false;
        }

        if (changed) mutationCommitted();
        return true;
    }

    /// <summary>
    /// Clears the exact selected NPC TravelPlan owner after a successful Core
    /// travel action. Admission and publication are required only for a real
    /// owner transition, preserving the Unity owner's prior no-op semantics.
    /// </summary>
    internal bool TryClearSelectedTravelPlan(
        DailyV1NpcRuntimeOwners npc,
        Func<bool> admitMutation,
        Action mutationCommitted,
        out bool changed)
    {
        changed = false;
        if (npc == null || !ContainsExactNpcOwner(npc) || npc.TravelPlan == null)
            return false;

        DailyV1NpcTravelPlanOwner travelPlan = npc.TravelPlan;
        if (!travelPlan.CanCommit(null, null, NpcTravelReason.None, 0f, 0f, null))
            return false;

        changed = !travelPlan.Matches(null, null, NpcTravelReason.None, 0f, 0f, null);
        if (changed && (admitMutation == null || mutationCommitted == null || !admitMutation()))
        {
            changed = false;
            return false;
        }

        if (!travelPlan.TryCommit(null, null, NpcTravelReason.None, 0f, 0f, null))
        {
            changed = false;
            return false;
        }

        if (changed) mutationCommitted();
        return true;
    }

    internal bool TryGetMerchantTradeTravelCost(SpatialRouteRuntime route,
        out float travelCost)
    {
        travelCost = -1f;
        if (route == null) return false;
        float candidate = Math.Max(1, route.TravelDays)
            * (Definitions.EffectiveConfiguration?.Travel?.TravelCostPerDay ?? 1f);
        if (float.IsNaN(candidate) || float.IsInfinity(candidate) || candidate < 0f)
            return false;
        travelCost = candidate;
        return true;
    }

    private bool TryFindCity(string runtimeId, out DailyV1CityRuntimeOwners city)
    {
        city = null;
        for (int i = 0; i < cities.Count; i++)
            if (string.Equals(cities[i].RuntimeId, runtimeId, StringComparison.Ordinal))
            { city = cities[i]; return true; }
        return false;
    }

    private bool ContainsExactNpc(DailyV1NpcRuntimeOwners candidate)
    {
        for (int i = 0; i < npcs.Count; i++)
            if (ReferenceEquals(npcs[i], candidate)) return true;
        return false;
    }

    private bool ContainsCityId(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId)) return false;
        for (int i = 0; i < cities.Count; i++)
            if (string.Equals(cities[i].RuntimeId, runtimeId, StringComparison.Ordinal)) return true;
        return false;
    }

    internal bool TryGetCityLocationRuntimeId(string cityRuntimeId, out string locationRuntimeId)
    {
        locationRuntimeId = null;
        if (!TryFindCity(cityRuntimeId, out DailyV1CityRuntimeOwners city)) return false;
        locationRuntimeId = city.LocationRuntimeId;
        return !string.IsNullOrWhiteSpace(locationRuntimeId);
    }

    internal bool ContainsNpcRuntimeId(string npcRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(npcRuntimeId)) return false;
        for (int i = 0; i < npcs.Count; i++)
            if (string.Equals(npcs[i].RuntimeId, npcRuntimeId, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>Checks party exclusion against the same authoritative P8 store
    /// used by the selected runtime, and fails closed if its membership does
    /// not agree with the exact NPC owner's active-party identity.</summary>
    internal bool IsManagedByTravelParty(
        DailyV1NpcRuntimeOwners npc,
        TravelPartyStore partyStore)
    {
        if (!ContainsExactNpcOwner(npc)) return true;
        bool ownerHasParty = !string.IsNullOrWhiteSpace(npc.Travel?.ActiveTravelPartyId);
        TravelPartyRuntime party = null;
        bool storeHasParty = partyStore != null
            && partyStore.TryGetPartyForNpc(npc.RuntimeId, out party);
        if (ownerHasParty != storeHasParty) return true;
        if (!ownerHasParty) return false;
        return true;
    }

    /// <summary>
    /// Creates the exact selected-owner graph for a roster that has admitted
    /// one additional NPC. The existing graph and all owner authorities stay
    /// immutable; only the runtime's graph reference changes at the membership
    /// boundary.
    /// </summary>
    internal bool TryCreateWithAddedNpc(
        DailyV1NpcRuntimeOwners owner,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (owner == null || !ReferenceEquals(owner.Definitions, Definitions)) return false;

        List<DailyV1NpcRuntimeOwners> updatedNpcs = new List<DailyV1NpcRuntimeOwners>(npcs)
        {
            owner
        };
        return TryCreateMembershipGraph(updatedNpcs, owner.RuntimeId, 1, out graph);
    }

    /// <summary>
    /// Creates the exact selected-owner graph for a roster after one NPC has
    /// been removed. Removal is by owner-instance identity, matching the
    /// runtime registry and alias rules.
    /// </summary>
    internal bool TryCreateWithoutNpc(
        DailyV1NpcRuntimeOwners owner,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (owner == null || !ReferenceEquals(owner.Definitions, Definitions)) return false;

        bool removed = false;
        List<DailyV1NpcRuntimeOwners> updatedNpcs = new List<DailyV1NpcRuntimeOwners>(npcs.Count);
        foreach (DailyV1NpcRuntimeOwners current in npcs)
        {
            if (!removed && ReferenceEquals(current, owner))
            {
                removed = true;
                continue;
            }
            updatedNpcs.Add(current);
        }

        return removed && TryCreateMembershipGraph(updatedNpcs, owner.RuntimeId, -1, out graph);
    }

    private bool TryCreateMembershipGraph(
        IReadOnlyList<DailyV1NpcRuntimeOwners> updatedNpcs,
        string pendingNpcIdentityId,
        int pendingNpcMembershipDelta,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (!TryCreateCore(Definitions, cities, updatedNpcs, SpatialNetwork,
                JusticeRecordsOwner, TravelParties, RuntimeIdentityIndex, PersonStore,
                pendingNpcIdentityId, pendingNpcMembershipDelta, out graph))
            return false;

        if ((ScheduledDirectives != null && !graph.TryBindScheduledDirectives(ScheduledDirectives))
            || (Expeditions != null && !graph.TryBindExpeditionStore(Expeditions)))
        {
            graph = null;
            return false;
        }
        if (PersonSpatialPositionStore != null
            && !graph.TryBindPersonSpatialPositionStore(PersonSpatialPositionStore))
        {
            graph = null;
            return false;
        }
        if (LegacySpatialAnchors != null
            && !graph.TryBindLegacySpatialAnchorBindingStore(LegacySpatialAnchors))
        {
            graph = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Resolves an admitted action's exact status effects against this runtime's
    /// selected definition graph. Target application is deliberately deferred
    /// until after the actor callback, matching the existing actuation order.
    /// </summary>
    internal bool TryCreateActionStatusPlan(
        DailyV1NpcRuntimeOwners actorOwner,
        DailyV1ActionDefinition action,
        bool targetExpected,
        DailyV1NpcRuntimeOwners targetOwner,
        out DailyV1SelectedActionStatusPlan plan)
    {
        plan = null;
        if (actorOwner == null
            || !ReferenceEquals(actorOwner.Definitions, Definitions)
            || !ContainsExactNpcOwner(actorOwner)
            || (!targetExpected && targetOwner != null)
            || !DailyV1NpcDefinitionRules.TryGetActionStatusChanges(
                Definitions, action,
                out IReadOnlyList<DailyV1StatusDefinition> actorStatusToRemove,
                out IReadOnlyList<DailyV1StatusDefinition> actorStatusToAdd,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove,
                out IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd))
            return false;

        bool targetMutationExpected = targetExpected
            && (targetStatusToRemove.Count > 0 || targetStatusToAdd.Count > 0);
        if (targetMutationExpected
            && (targetOwner == null
                || !ReferenceEquals(targetOwner.Definitions, Definitions)
                || !ContainsExactNpcOwner(targetOwner)))
            return false;

        plan = new DailyV1SelectedActionStatusPlan(
            actorOwner,
            actorStatusToRemove,
            actorStatusToAdd,
            targetMutationExpected,
            targetMutationExpected ? targetOwner : null,
            targetStatusToRemove,
            targetStatusToAdd);
        return true;
    }

    internal bool ContainsExactCityOwner(DailyV1CityRuntimeOwners candidate)
    {
        if (candidate == null || !ReferenceEquals(candidate.Definitions, Definitions)) return false;
        foreach (DailyV1CityRuntimeOwners owner in cities)
            if (ReferenceEquals(owner, candidate)) return true;
        return false;
    }

    internal bool ContainsExactNpcOwner(DailyV1NpcRuntimeOwners candidate)
    {
        if (candidate == null || !ReferenceEquals(candidate.Definitions, Definitions)) return false;
        foreach (DailyV1NpcRuntimeOwners owner in npcs)
            if (ReferenceEquals(owner, candidate)) return true;
        return false;
    }

    internal bool TryGetNpcOwner(string runtimeId, out DailyV1NpcRuntimeOwners owner)
    {
        owner = null;
        if (string.IsNullOrWhiteSpace(runtimeId)) return false;
        foreach (DailyV1NpcRuntimeOwners candidate in npcs)
            if (string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
            { owner = candidate; return true; }
        return false;
    }

    /// <summary>
    /// Prepares the ordinary, unkeyed open-market sale against the exact City
    /// and NPC authorities in this graph. The host supplies the P12 operation
    /// admission only when the prepared plan is ready to commit.
    /// </summary>
    internal bool TryPrepareOpenMarketSale(
        DailyV1NpcRuntimeOwners npc,
        DailyV1MarketOwner market,
        DailyV1ItemDefinition item,
        int requestedQuantity,
        out DailyV1SelectedOpenMarketSalePlan plan,
        out DailyV1SelectedOpenMarketSaleFailure failure)
    {
        plan = null;
        failure = DailyV1SelectedOpenMarketSaleFailure.InvalidInput;
        if (!ContainsExactNpcOwner(npc) || market == null)
            return false;

        DailyV1CityRuntimeOwners city = null;
        foreach (DailyV1CityRuntimeOwners candidate in cities)
        {
            if (candidate != null && ReferenceEquals(candidate.Market, market))
            {
                city = candidate;
                break;
            }
        }

        return DailyV1SelectedOpenMarketSalePlan.TryPrepare(
            this, npc, city, market, item, requestedQuantity, out plan, out failure);
    }

    /// <summary>Prepares an ordinary selected open-market purchase on exact Core owners.</summary>
    internal bool TryPrepareOpenMarketPurchase(
        DailyV1NpcRuntimeOwners npc,
        DailyV1MarketOwner market,
        DailyV1ItemDefinition item,
        int requestedQuantity,
        bool allowAccountBacked,
        out DailyV1SelectedOpenMarketPurchasePlan plan,
        out DailyV1SelectedOpenMarketPurchaseFailure failure)
    {
        plan = null;
        failure = DailyV1SelectedOpenMarketPurchaseFailure.InvalidInput;
        if (!ContainsExactNpcOwner(npc) || market == null) return false;
        DailyV1CityRuntimeOwners city = null;
        foreach (DailyV1CityRuntimeOwners candidate in cities)
            if (candidate != null && ReferenceEquals(candidate.Market, market))
            { city = candidate; break; }
        return DailyV1SelectedOpenMarketPurchasePlan.TryPrepare(
            this, npc, city, market, item, requestedQuantity, allowAccountBacked,
            out plan, out failure);
    }

    internal bool ContainsCityRuntimeLocation(string cityRuntimeId, string locationRuntimeId)
    {
        if (cityRuntimeId == null) return true;
        if (string.IsNullOrWhiteSpace(cityRuntimeId) || string.IsNullOrWhiteSpace(locationRuntimeId))
            return false;
        foreach (DailyV1CityRuntimeOwners city in cities)
            if (string.Equals(city.RuntimeId, cityRuntimeId, StringComparison.Ordinal))
                return string.Equals(city.LocationRuntimeId, locationRuntimeId, StringComparison.Ordinal);
        return false;
    }

    /// <summary>
    /// Commits one prepared travel/presence operation only when its NPC and all
    /// affected City authorities are exact members of this selected runtime.
    /// The host admission callback runs before any owner revision or value changes.
    /// </summary>
    internal bool TryCommitTravelTransition(
        DailyV1NpcRuntimeOwners npc,
        DailyV1SelectedTravelTransitionPlan plan,
        Func<bool> preCommitAdmission)
    {
        if (!ContainsExactNpcOwner(npc) || plan == null || !plan.IsPreparedFor(npc, this)) return false;
        return plan.TryCommit(preCommitAdmission);
    }

    private bool TryCommitTravelTransitionAfterPreparedPresenceAdds(
        DailyV1NpcRuntimeOwners npc,
        DailyV1SelectedTravelTransitionPlan plan,
        DailyV1CityNpcPresenceOwner sharedPresenceOwner,
        long earlierAdds)
    {
        if (!ContainsExactNpcOwner(npc) || plan == null || !plan.IsPreparedFor(npc, this)) return false;
        return earlierAdds == 0L
            ? plan.TryCommit()
            : plan.TryCommitAfterPreparedPresenceAdds(sharedPresenceOwner, earlierAdds);
    }

    /// <summary>Resolves the admitted selected NPC's exact direct route and
    /// effective Daily-v1 travel cost without consulting Unity projections.</summary>
    internal bool TryPlanSelectedSoloTravel(
        DailyV1NpcRuntimeOwners npc,
        string originLocationRuntimeId,
        string destinationLocationRuntimeId,
        string destinationCityRuntimeId,
        out SpatialRouteRuntime route,
        out float travelCost)
    {
        route = null;
        travelCost = -1f;
        if (!ContainsExactNpcOwner(npc) || SpatialNetwork == null
            || !string.Equals(npc.Travel?.CurrentLocationRuntimeId,
                originLocationRuntimeId, StringComparison.Ordinal)
            || !SpatialNetwork.TryGetLocation(originLocationRuntimeId, out SpatialLocationRuntime origin)
            || !SpatialNetwork.TryGetLocation(destinationLocationRuntimeId, out SpatialLocationRuntime destination)
            || (destinationCityRuntimeId != null
                && !ContainsCityRuntimeLocation(destinationCityRuntimeId, destinationLocationRuntimeId))
            || !SpatialNetwork.TryGetSingleDirectRoute(origin, destination, out route, out _))
        {
            route = null;
            return false;
        }

        int days = Math.Max(1, route.TravelDays);
        travelCost = days * (Definitions.EffectiveConfiguration?.Travel?.TravelCostPerDay ?? 0f);
        if (float.IsNaN(travelCost) || float.IsInfinity(travelCost) || travelCost < 0f)
        {
            route = null;
            travelCost = -1f;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Advances the existing P8 TravelParty owner in its captured store order.
    /// Core resolves each exact selected NPC owner, checks synchronized travel,
    /// and performs the travel, discovery, party-identity, and store-completion
    /// mutations. The two host callbacks are restricted to the established
    /// pre-commit P12/projection preflight and publishing a committed Core
    /// owner into the existing Unity projection before the next member is read.
    /// The event callback publishes the noncausal event read model at its
    /// existing position before active-party identities are cleared.
    /// </summary>
    internal bool TryAdvanceTravelParties(
        TravelPartyStore partyStore,
        Func<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan, bool, bool> preCommitAdmission,
        Action<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan, bool> publishCommitted,
        Func<TravelPartyRuntime, bool> publishArrivalEvent,
        out IReadOnlyList<DailyV1SelectedTravelPartyArrivalResult> arrivals)
    {
        arrivals = null;
        if (partyStore == null || preCommitAdmission == null || publishCommitted == null)
            return false;

        using (partyStore.EnterMutationWindow())
        {
            List<DailyV1SelectedTravelPartyArrivalResult> completed =
                new List<DailyV1SelectedTravelPartyArrivalResult>();
            List<TravelPartyRuntime> activeParties = new List<TravelPartyRuntime>(partyStore.ActiveParties);
            Dictionary<string, DailyV1NpcRuntimeOwners> npcsById =
                new Dictionary<string, DailyV1NpcRuntimeOwners>(StringComparer.Ordinal);
            foreach (DailyV1NpcRuntimeOwners npc in npcs)
                if (npc == null || !npcsById.TryAdd(npc.RuntimeId, npc)) return false;

            foreach (TravelPartyRuntime party in activeParties)
            {
                if (party == null || !party.IsActive
                    || !TryResolveSynchronizedPartyMembers(party, npcsById,
                        out List<DailyV1NpcRuntimeOwners> members))
                    continue;

                bool startedToday = false;
                foreach (DailyV1NpcRuntimeOwners member in members)
                    startedToday |= member.Travel.TravelStartedToday;

                if (startedToday)
                {
                    List<DailyV1SelectedTravelTransitionPlan> clearPlans =
                        new List<DailyV1SelectedTravelTransitionPlan>(members.Count);
                    bool canClear = true;
                    foreach (DailyV1NpcRuntimeOwners member in members)
                    {
                        if (!DailyV1SelectedTravelTransitionPlan.TryClearTravelStartedToday(
                                member, out DailyV1SelectedTravelTransitionPlan clearPlan)
                            || !preCommitAdmission(member, clearPlan, false))
                        {
                            canClear = false;
                            break;
                        }
                        clearPlans.Add(clearPlan);
                    }
                    if (!canClear) continue;
                    foreach (DailyV1SelectedTravelTransitionPlan clearPlan in clearPlans)
                    {
                        DailyV1NpcRuntimeOwners member = clearPlan.Npc;
                        if (!TryCommitTravelTransition(member, clearPlan, null))
                            throw new InvalidOperationException(
                                "A preflighted TravelParty start-day flag could not commit.");
                        publishCommitted(member, clearPlan, false);
                    }
                    continue;
                }

                bool willArrive = members[0].Travel.TravelDaysRemaining <= 1;
                DailyV1CityRuntimeOwners destinationCity = null;
                string destinationCityId = members[0].Travel.DestinationCityRuntimeId;
                if (destinationCityId != null)
                {
                    foreach (DailyV1CityRuntimeOwners city in cities)
                        if (string.Equals(city.RuntimeId, destinationCityId, StringComparison.Ordinal))
                        {
                            destinationCity = city;
                            break;
                        }
                    if (destinationCity == null) continue;
                }

                if (willArrive && !partyStore.CanCommitMutations(1)) continue;

                List<DailyV1SelectedTravelTransitionPlan> advancePlans =
                    new List<DailyV1SelectedTravelTransitionPlan>(members.Count);
                int destinationPresenceAdds = 0;
                bool canAdvance = true;
                foreach (DailyV1NpcRuntimeOwners member in members)
                {
                    if (!DailyV1SelectedTravelTransitionPlan.TryAdvanceTravelDay(
                            member, destinationCity,
                            out DailyV1SelectedTravelTransitionPlan advancePlan,
                            out bool memberArrives)
                        || memberArrives != willArrive
                        || !preCommitAdmission(member, advancePlan, willArrive))
                    {
                        canAdvance = false;
                        break;
                    }
                    if (willArrive && destinationCity != null
                        && !destinationCity.NpcPresence.Contains(member.RuntimeId))
                        destinationPresenceAdds++;
                    advancePlans.Add(advancePlan);
                }
                if (!canAdvance || (destinationCity != null
                    && !destinationCity.NpcPresence.CanApplyRevisionIncrements(destinationPresenceAdds)))
                    continue;

                foreach (DailyV1SelectedTravelTransitionPlan advancePlan in advancePlans)
                {
                    if (!ContainsExactNpcOwner(advancePlan.Npc)
                        || !advancePlan.IsPreparedFor(advancePlan.Npc, this)
                        || !advancePlan.CanCommit())
                    {
                        canAdvance = false;
                        break;
                    }
                }
                if (!canAdvance) continue;

                DailyV1CityNpcPresenceOwner sharedDestinationPresence = destinationCity?.NpcPresence;
                long committedPresenceAdds = 0L;
                foreach (DailyV1SelectedTravelTransitionPlan advancePlan in advancePlans)
                {
                    DailyV1NpcRuntimeOwners member = advancePlan.Npc;
                    bool addsDestinationPresence = sharedDestinationPresence != null
                        && advancePlan.TryGetPresenceChange(destinationCity, out bool adds)
                        && adds;
                    if (!TryCommitTravelTransitionAfterPreparedPresenceAdds(
                            member,
                            advancePlan,
                            sharedDestinationPresence,
                            addsDestinationPresence ? committedPresenceAdds : 0L))
                        throw new InvalidOperationException(
                            "A fully preflighted TravelParty member transition could not commit in its serialized batch.");
                    publishCommitted(member, advancePlan, willArrive);
                    if (addsDestinationPresence) committedPresenceAdds++;
                }
                if (!willArrive) continue;

                foreach (DailyV1NpcRuntimeOwners member in members)
                    member.SpatialKnowledge.DiscoverLocation(party.DestinationLocationRuntimeId);

                bool eventRecorded = publishArrivalEvent == null || publishArrivalEvent(party);
                foreach (DailyV1NpcRuntimeOwners member in members)
                {
                    if (!DailyV1SelectedTravelTransitionPlan.TrySetActiveTravelParty(
                            member, null, out DailyV1SelectedTravelTransitionPlan clearPartyPlan)
                        || !preCommitAdmission(member, clearPartyPlan, false)
                        || !TryCommitTravelTransition(member, clearPartyPlan, null))
                        throw new InvalidOperationException(
                            "A preflighted arrived TravelParty member could not clear its party identity.");
                    publishCommitted(member, clearPartyPlan, false);
                }

                if (!partyStore.Complete(party.TravelPartyId))
                    throw new InvalidOperationException(
                        "A preflighted TravelParty arrival could not complete its Core owner.");

                completed.Add(new DailyV1SelectedTravelPartyArrivalResult(
                    party, members.AsReadOnly(), eventRecorded));
            }

            arrivals = completed.AsReadOnly();
            return true;
        }
    }

    private static bool TryResolveSynchronizedPartyMembers(
        TravelPartyRuntime party,
        IReadOnlyDictionary<string, DailyV1NpcRuntimeOwners> npcsById,
        out List<DailyV1NpcRuntimeOwners> members)
    {
        members = new List<DailyV1NpcRuntimeOwners>();
        if (party == null || npcsById == null || party.MemberRuntimeIds == null
            || party.MemberRuntimeIds.Count == 0)
            return false;

        foreach (string runtimeId in party.MemberRuntimeIds)
        {
            if (string.IsNullOrWhiteSpace(runtimeId)
                || !npcsById.TryGetValue(runtimeId, out DailyV1NpcRuntimeOwners member)
                || member == null || member.Lifecycle.LifeState != NpcLifeState.Alive)
                return false;
            members.Add(member);
        }

        DailyV1NpcRuntimeOwners first = members[0];
        if (!first.Travel.IsTraveling
            || string.IsNullOrWhiteSpace(first.Travel.DestinationLocationRuntimeId)
            || first.Travel.TravelDaysRemaining <= 0
            || !string.Equals(first.Travel.ActiveTravelPartyId, party.TravelPartyId, StringComparison.Ordinal)
            || !string.Equals(first.Travel.DestinationLocationRuntimeId,
                party.DestinationLocationRuntimeId, StringComparison.Ordinal)
            || !string.Equals(first.Travel.TravelOriginDecisionId,
                party.OriginDecisionId, StringComparison.Ordinal))
            return false;

        foreach (DailyV1NpcRuntimeOwners member in members)
            if (!member.Travel.IsTraveling
                || !string.Equals(member.Travel.DestinationLocationRuntimeId,
                    first.Travel.DestinationLocationRuntimeId, StringComparison.Ordinal)
                || !string.Equals(member.Travel.DestinationCityRuntimeId,
                    first.Travel.DestinationCityRuntimeId, StringComparison.Ordinal)
                || member.Travel.TravelDaysRemaining != first.Travel.TravelDaysRemaining
                || !string.Equals(member.Travel.ActiveTravelPartyId,
                    party.TravelPartyId, StringComparison.Ordinal)
                || !string.Equals(member.Travel.TravelOriginDecisionId,
                    party.OriginDecisionId, StringComparison.Ordinal))
                return false;
        return true;
    }

    /// <summary>
    /// Commits the exact selected NPC action definition/payload to its Core
    /// owner slot after host admission. The host still projects its existing
    /// action object and records the P12 epoch after this authoritative write.
    /// </summary>
    internal bool TryCommitCurrentActionSlot(
        DailyV1NpcRuntimeOwners npc,
        DailyV1ActionDefinition action,
        DailyV1NpcActionPayloadOwner payload,
        Func<bool> preCommitAdmission)
    {
        DailyV1NpcActionSlotOwner slot = npc?.ActionSlot;
        if (!ContainsExactNpcOwner(npc)
            || slot == null
            || !slot.CanSetCurrentAction(action, payload))
            return false;

        if (preCommitAdmission != null)
        {
            try
            {
                if (!preCommitAdmission()) return false;
            }
            catch
            {
                return false;
            }
        }

        // Admission executes on the runtime owner thread. The exact slot and
        // admitted payload were validated above, so no fallible preparation
        // remains after the boundary accepts the mutation.
        if (!slot.TrySetCurrentAction(action, payload)) return false;
        slot.NotifyCurrentActionMutationCommitted();
        return true;
    }

    /// <summary>
    /// Runs the selected solo-travel portion of one daily turn in stable
    /// RuntimeId order. Core selects eligible owners, prepares each existing
    /// travel plan, and installs it into the exact travel/presence owners. The
    /// host supplies a read-only party-membership snapshot, a compatibility
    /// boundary preflight, and a per-owner projection/invalidation publisher.
    /// Core chooses and commits each travel result; the publisher reconciles its
    /// Unity projection and P12 mutation boundary before the next owner is read.
    /// </summary>
    internal bool TryAdvanceSoloTravelDay(
        IEnumerable<DailyV1NpcRuntimeOwners> exactNpcOwners,
        IReadOnlyCollection<string> managedTravelPartyNpcRuntimeIds,
        Func<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan, bool, bool> preCommitAdmission,
        Action<DailyV1NpcRuntimeOwners, DailyV1SelectedTravelTransitionPlan, bool> publishCommitted,
        Action<DailyV1SelectedSoloTravelDayResult> publishResult,
        out IReadOnlyList<DailyV1SelectedSoloTravelDayResult> results)
    {
        results = null;
        if (!MatchesExactOwnerInstances(npcs, exactNpcOwners)
            || managedTravelPartyNpcRuntimeIds == null
            || preCommitAdmission == null
            || publishCommitted == null
            || publishResult == null)
            return false;

        HashSet<string> managedIds = new HashSet<string>(managedTravelPartyNpcRuntimeIds, StringComparer.Ordinal);
        List<DailyV1NpcRuntimeOwners> ordered = new List<DailyV1NpcRuntimeOwners>(npcs);
        ordered.Sort((left, right) => string.CompareOrdinal(left.RuntimeId, right.RuntimeId));
        List<DailyV1SelectedSoloTravelDayResult> completed = new List<DailyV1SelectedSoloTravelDayResult>();

        foreach (DailyV1NpcRuntimeOwners npc in ordered)
        {
            if (npc.Lifecycle.LifeState != NpcLifeState.Alive
                || !npc.Travel.IsTraveling
                || !string.IsNullOrWhiteSpace(npc.Travel.ActiveTravelPartyId)
                || managedIds.Contains(npc.RuntimeId))
                continue;

            if (npc.Travel.TravelStartedToday)
            {
                if (DailyV1SelectedTravelTransitionPlan.TryClearTravelStartedToday(
                        npc, out DailyV1SelectedTravelTransitionPlan clearStartedPlan))
                {
                    bool committed = preCommitAdmission(npc, clearStartedPlan, false)
                        && TryCommitTravelTransition(npc, clearStartedPlan, null);
                    if (committed) publishCommitted(npc, clearStartedPlan, false);
                    DailyV1SelectedSoloTravelDayResult result = new DailyV1SelectedSoloTravelDayResult(
                        npc, clearStartedPlan, false, committed, false,
                        npc.Travel.TravelOriginDecisionId);
                    completed.Add(result);
                    publishResult(result);
                }
                continue;
            }

            string originDecisionId = npc.Travel.TravelOriginDecisionId;
            DailyV1CityRuntimeOwners destination = null;
            if (npc.Travel.DestinationCityRuntimeId != null)
                foreach (DailyV1CityRuntimeOwners city in cities)
                    if (string.Equals(city.RuntimeId, npc.Travel.DestinationCityRuntimeId,
                            StringComparison.Ordinal))
                    {
                        destination = city;
                        break;
                    }

            DailyV1SelectedTravelTransitionPlan advancePlan = null;
            bool willArrive = false;
            bool committedAdvance = false;
            if (DailyV1SelectedTravelTransitionPlan.TryAdvanceTravelDay(
                    npc, destination,
                    out advancePlan,
                    out willArrive))
            {
                committedAdvance = preCommitAdmission(npc, advancePlan, willArrive)
                    && TryCommitTravelTransition(npc, advancePlan, null);
                if (committedAdvance) publishCommitted(npc, advancePlan, willArrive);
            }

            DailyV1SelectedSoloTravelDayResult advanceResult = new DailyV1SelectedSoloTravelDayResult(
                npc, advancePlan, shouldReport: true, committedAdvance,
                arrived: willArrive && committedAdvance, originDecisionId);
            completed.Add(advanceResult);
            if (advanceResult.Arrived)
            {
                // Arrival discovery is part of the selected domain transition,
                // not a Unity-side consequence callback. TravelPlan has already
                // installed the authoritative location on this exact NPC owner;
                // the SpatialKnowledge owner retains its existing P12 mutation
                // boundary and revision behavior.
                npc.SpatialKnowledge.DiscoverLocation(npc.Travel.CurrentLocationRuntimeId);
            }
            publishResult(advanceResult);
        }

        results = completed.AsReadOnly();
        return true;
    }

    /// <summary>
    /// Executes the existing selected-City daily economy in its established
    /// global pass order: local material flow, ordinary production, then
    /// consumption and price refresh per City. The host callbacks are projections
    /// only; all selected mutable owners and definition reads remain in Core.
    /// Returns false only during preflight, before any owner is mutated.
    /// </summary>
    internal bool TrySimulateDailyEconomyDay(
        IEnumerable<DailyV1CityRuntimeOwners> exactCityOwners,
        long absoluteDay,
        string calendarVersion,
        Action<DailyV1CityRuntimeOwners, SimulationLogCategory, string> logOutput)
    {
        if (!MatchesExactOwnerInstances(cities, exactCityOwners)) return false;

        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (!city.HasLocalDailyMaterialFlow) continue;
            LocalDailyMaterialFlowResult result = city.SimulateSelectedLocalDailyMaterialFlow(
                city.Definition.Values.materialFlowLocationId,
                absoluteDay,
                calendarVersion,
                logOutput == null ? null : (category, message) => logOutput(city, category, message));
            city.RecordSelectedMaterialFlow(result);
        }

        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (!city.HasLocalDailyMaterialFlow)
                city.SimulateSelectedProductionDay(
                    logOutput == null ? null : (category, message) => logOutput(city, category, message));
        }

        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (!city.HasLocalDailyMaterialFlow)
                city.SimulateSelectedConsumptionDay(
                    logOutput == null ? null : (category, message) => logOutput(city, category, message));
            city.UpdateSelectedMarketPrices(
                logOutput == null ? null : (category, message) => logOutput(city, category, message));
        }

        return true;
    }

    private static bool MatchesExactOwnerInstances<T>(
        IReadOnlyList<T> expected,
        IEnumerable<T> actual)
        where T : class
    {
        if (expected == null || actual == null) return false;
        int index = 0;
        foreach (T owner in actual)
        {
            if (index >= expected.Count || !ReferenceEquals(expected[index], owner)) return false;
            index++;
        }

        return index == expected.Count;
    }
}

/// <summary>
/// Immutable host-projection receipt for one selected NPC's solo-travel turn.
/// A receipt is emitted only after Core has selected and evaluated that NPC;
/// its plan is committed when <see cref="WasCommitted"/> is true.
/// </summary>
internal sealed class DailyV1SelectedTravelPartyArrivalResult
{
    internal TravelPartyRuntime Party { get; }
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Members { get; }
    internal bool EventRecorded { get; }

    internal DailyV1SelectedTravelPartyArrivalResult(
        TravelPartyRuntime party,
        IReadOnlyList<DailyV1NpcRuntimeOwners> members,
        bool eventRecorded)
    {
        Party = party ?? throw new ArgumentNullException(nameof(party));
        Members = members ?? throw new ArgumentNullException(nameof(members));
        EventRecorded = eventRecorded;
    }
}

internal sealed class DailyV1SelectedSoloTravelDayResult
{
    internal DailyV1NpcRuntimeOwners Npc { get; }
    internal DailyV1SelectedTravelTransitionPlan Plan { get; }
    internal bool ShouldReport { get; }
    internal bool WasCommitted { get; }
    internal bool Arrived { get; }
    internal string OriginDecisionId { get; }

    internal DailyV1SelectedSoloTravelDayResult(
        DailyV1NpcRuntimeOwners npc,
        DailyV1SelectedTravelTransitionPlan plan,
        bool shouldReport,
        bool wasCommitted,
        bool arrived,
        string originDecisionId)
    {
        Npc = npc ?? throw new ArgumentNullException(nameof(npc));
        Plan = plan;
        ShouldReport = shouldReport;
        WasCommitted = wasCommitted;
        Arrived = arrived;
        OriginDecisionId = originDecisionId;
    }
}

/// <summary>
/// Builds the initial selected Daily-v1 City/NPC authorities from the admitted
/// immutable definition graph and the runtime identities allocated by genesis.
/// Unity supplies identity values and later projects the resulting owner
/// instances; it does not construct a parallel selected owner graph.
/// </summary>
internal sealed class DailyV1SelectedRuntimeOwnerGraphBuilder
{
    private readonly DailyV1EffectiveDefinitions definitions;
    private readonly SpatialNetworkOwner spatialNetwork;
    private readonly DailyV1RuntimeIdentityIndex identityIndex;
    private readonly List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>();
    private readonly List<DailyV1NpcRuntimeOwners> npcs = new List<DailyV1NpcRuntimeOwners>();
    private readonly Dictionary<string, List<DailyV1CityRuntimeOwners>> citiesByDefinition =
        new Dictionary<string, List<DailyV1CityRuntimeOwners>>(StringComparer.Ordinal);
    private readonly HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
    private bool built;
    private bool initialJusticeStateCreated;
    private bool initialCommercialKnowledgeCreated;
    internal string LastInitialJusticeStateFailure { get; private set; }

    internal DailyV1SelectedRuntimeOwnerGraphBuilder(
        DailyV1EffectiveDefinitions definitions,
        SpatialNetworkOwner spatialNetwork = null,
        DailyV1RuntimeIdentityIndex identityIndex = null)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.spatialNetwork = spatialNetwork ?? new SpatialNetworkOwner();
        this.identityIndex = identityIndex ?? new DailyV1RuntimeIdentityIndex();
    }

    internal IReadOnlyList<DailyV1CityRuntimeOwners> Cities => cities.AsReadOnly();
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> Npcs => npcs.AsReadOnly();
    internal SpatialNetworkOwner SpatialNetwork => spatialNetwork;
    internal DailyV1RuntimeIdentityIndex RuntimeIdentityIndex => identityIndex;

    /// <summary>
    /// Builds the configured selected owner graph and its supported day-zero
    /// spatial, commercial, and Justice genesis owners without Unity
    /// projection callbacks. A failed build is a discarded genesis candidate;
    /// callers must not publish its allocator or any partial owner projection.
    /// </summary>
    internal static bool TryBuildConfigured(
        DailyV1EffectiveDefinitions definitions,
        RuntimeIdAllocator runtimeIds,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (definitions == null || runtimeIds == null) return false;

        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        SimulationTime simulationTime = new SimulationTime();
        ScheduledDirectiveStore scheduledDirectives = new ScheduledDirectiveStore(simulationTime);
        DailyV1JusticeRecordsOwner justice = new DailyV1JusticeRecordsOwner();
        return builder.TryCreateConfiguredCities(runtimeIds)
            && builder.TryCreateConfiguredCityRoutes(runtimeIds)
            && builder.TryCreateConfiguredNpcs(runtimeIds, null)
            && builder.TryCreateConfiguredScheduledDirectives(runtimeIds, scheduledDirectives)
            && builder.TryBootstrapInitialSpatialKnowledge()
            && builder.TryBootstrapInitialCommercialKnowledge(0L)
            && builder.TryCreateConfiguredInitialJusticeState(justice)
            && builder.TryBuild(justice, null, scheduledDirectives, out graph);
    }

    /// <summary>
    /// Performs selected Location identity registration and authoritative
    /// owner construction in Core. Optional callbacks only project or roll
    /// back the existing identity in a host-owned lookup.
    /// </summary>
    internal bool TryCreateConfiguredCities(
        RuntimeIdAllocator runtimeIds,
        Func<SpatialLocationRuntime, bool> tryRegisterLocationIdentity,
        Action<SpatialLocationRuntime> rollbackLocationIdentity = null)
    {
        if (built || runtimeIds == null || definitions.Configuration == null)
            return false;

        List<string> selectedCityIds = new List<string>(
            definitions.Configuration.cities ?? Array.Empty<string>());
        selectedCityIds.Sort(StringComparer.Ordinal);
        foreach (string cityId in selectedCityIds)
        {
            if (string.IsNullOrWhiteSpace(cityId)) continue;
            if (!definitions.Cities.TryGetValue(cityId, out DailyV1CityDefinition definition)
                || definition == null
                || !string.Equals(definition.DefinitionId, cityId, StringComparison.Ordinal))
                return false;

            string locationRuntimeId = runtimeIds.AllocateLocationId();
            SpatialLocationRuntime location = new SpatialLocationRuntime(locationRuntimeId);
            bool identityStaged = false;
            bool identityRevisionAdvanced = false;
            Func<bool> registerIdentity = () =>
            {
                if (!identityIndex.TryStageRegister(locationRuntimeId, DailyV1RuntimeIdentityKind.Location))
                    return false;
                identityStaged = true;
                try
                {
                    if (tryRegisterLocationIdentity != null && !tryRegisterLocationIdentity(location))
                    {
                        identityIndex.TryRemoveStaged(locationRuntimeId, DailyV1RuntimeIdentityKind.Location);
                        identityStaged = false;
                        return false;
                    }
                    if (!identityIndex.TryAdvanceRevision())
                        throw new InvalidOperationException("Core runtime identity revision is exhausted during Location genesis.");
                    identityRevisionAdvanced = true;
                    return true;
                }
                catch
                {
                    if (identityRevisionAdvanced) identityIndex.TryRollbackRevision();
                    if (identityStaged) identityIndex.TryRemoveStaged(locationRuntimeId, DailyV1RuntimeIdentityKind.Location);
                    identityRevisionAdvanced = false;
                    identityStaged = false;
                    throw;
                }
            };
            Action rollbackIdentity = () =>
            {
                rollbackLocationIdentity?.Invoke(location);
                if (identityRevisionAdvanced && !identityIndex.TryRollbackRevision())
                    throw new InvalidOperationException("Core Location identity revision rollback failed.");
                if (identityStaged && !identityIndex.TryRemoveStaged(locationRuntimeId, DailyV1RuntimeIdentityKind.Location))
                    throw new InvalidOperationException("Core Location identity rollback failed.");
                identityRevisionAdvanced = false;
                identityStaged = false;
            };
            if (!spatialNetwork.TryRegisterLocation(
                    location,
                    registerIdentity,
                    rollbackIdentity,
                    null))
                continue;

            string cityRuntimeId = runtimeIds.AllocateCityId();
            if (!TryAddInitialCity(cityRuntimeId, locationRuntimeId, definition, out _))
            {
                if (!spatialNetwork.TryRollbackGenesisLocation(location, rollbackIdentity))
                    throw new InvalidOperationException(
                        "A failed selected City genesis insertion could not roll back its spatial location.");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Constructs selected Cities and their location identities entirely in
    /// Core. The Unity adapter uses the overload above when it must also
    /// project the same allocated location into its read-only compatibility
    /// view; a detached host has no such projection to register.
    /// </summary>
    internal bool TryCreateConfiguredCities(RuntimeIdAllocator runtimeIds) =>
        TryCreateConfiguredCities(runtimeIds, null, null);

    /// <summary>
    /// Allocates selected NPC identities and builds their exact definition,
    /// inventory, travel, and City-presence owners in Core. The optional
    /// resolver is a host projection check used to preserve existing Unity
    /// missing/ambiguous City diagnostics before each NPC identity is issued.
    /// </summary>
    internal bool TryCreateConfiguredNpcs(
        RuntimeIdAllocator runtimeIds,
        Func<string, string> resolveStartingCityProjection)
    {
        if (built || runtimeIds == null || definitions.Configuration == null) return false;

        Dictionary<string, DailyV1NpcSimulationConfig> selectedRows =
            new Dictionary<string, DailyV1NpcSimulationConfig>(StringComparer.Ordinal);
        foreach (DailyV1NpcSimulationConfig row in
            definitions.Configuration.npcs ?? Array.Empty<DailyV1NpcSimulationConfig>())
        {
            if (row == null || string.IsNullOrWhiteSpace(row.npc)) continue;
            if (!selectedRows.TryAdd(row.npc, row)) return false;
        }

        List<string> selectedNpcIds = new List<string>(selectedRows.Keys);
        selectedNpcIds.Sort(StringComparer.Ordinal);
        foreach (string npcId in selectedNpcIds)
        {
            if (!definitions.Npcs.TryGetValue(npcId, out DailyV1NpcDefinition definition)
                || definition == null
                || !string.Equals(definition.DefinitionId, npcId, StringComparison.Ordinal))
                return false;

            DailyV1NpcSimulationConfig configuration = selectedRows[npcId];
            DailyV1CityRuntimeOwners startingCity = null;
            if (!string.IsNullOrWhiteSpace(configuration.startingCity))
            {
                if (!definitions.Cities.ContainsKey(configuration.startingCity)) return false;
                startingCity = ResolveUniqueStartingCity(configuration.startingCity);
                string projectedCityRuntimeId = resolveStartingCityProjection == null
                    ? startingCity?.RuntimeId
                    : resolveStartingCityProjection(configuration.startingCity);
                if (!string.Equals(projectedCityRuntimeId, startingCity?.RuntimeId, StringComparison.Ordinal))
                    return false;
            }

            string npcRuntimeId = runtimeIds.AllocateNpcId();
            if (!TryAddInitialNpc(npcRuntimeId, definition, configuration, out _)) return false;
        }

        return true;
    }

    /// <summary>
    /// Creates the selected profile's scheduled directives against the exact
    /// Core NPC owners and admitted action definitions. Unity may project the
    /// resulting stores for compatibility, but does not resolve or construct
    /// selected directive authority.
    /// </summary>
    internal bool TryCreateConfiguredScheduledDirectives(
        RuntimeIdAllocator runtimeIds,
        ScheduledDirectiveStore directiveStore,
        Action<string> logWarning = null,
        Action<string> logError = null)
    {
        if (built || runtimeIds == null || directiveStore == null
            || definitions.Configuration == null)
            return false;

        foreach (DailyV1ScheduledDirectiveConfig selectedDirective in
            definitions.Configuration.scheduledDirectives ?? Array.Empty<DailyV1ScheduledDirectiveConfig>())
        {
            if (selectedDirective == null)
            {
                logWarning?.Invoke("Skipping null selected scheduled directive configuration.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(selectedDirective.actor))
            {
                logWarning?.Invoke("Skipping selected scheduled directive configuration: actor definition is null.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(selectedDirective.action)
                || !definitions.Actions.TryGetValue(selectedDirective.action,
                    out DailyV1ActionDefinition action)
                || action?.Values?.actionType?.Value != DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue)
            {
                logWarning?.Invoke("Skipping selected scheduled directive configuration: EscapePrison requires a matching admitted action definition.");
                continue;
            }

            DailyV1NpcRuntimeOwners actor = null;
            int actorMatches = 0;
            foreach (DailyV1NpcRuntimeOwners candidate in npcs)
            {
                if (candidate?.Definition == null
                    || !string.Equals(candidate.Definition.DefinitionId, selectedDirective.actor,
                        StringComparison.Ordinal))
                    continue;
                actor = candidate;
                actorMatches++;
                if (actorMatches > 1) break;
            }
            if (actorMatches != 1) continue;

            try
            {
                ScheduledDirective directive = new ScheduledDirective(
                    runtimeIds.AllocateDirectiveId(),
                    selectedDirective.absoluteDay,
                    (ScheduledDirectiveMode)(selectedDirective.mode?.Value ?? 0),
                    (ScheduledDirectiveOperation)(selectedDirective.operation?.Value ?? 0),
                    actor.RuntimeId,
                    action.DefinitionId,
                    action.Values.actionType.Value);
                directiveStore.Add(directive);
            }
            catch (ArgumentException exception)
            {
                logError?.Invoke("Cannot create scheduled directive: " + exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                logError?.Invoke("Cannot allocate DirectiveId: " + exception.Message);
            }
        }

        return true;
    }

    /// <summary>
    /// Creates the selected profile's initial warrant rows and wanted-status
    /// memberships on their Core owners before the graph is admitted to a
    /// runtime. Unity later binds reference projections for those exact rows.
    /// </summary>
    internal bool TryCreateConfiguredInitialJusticeState(DailyV1JusticeRecordsOwner justiceRecordsOwner)
    {
        LastInitialJusticeStateFailure = null;
        if (built || initialJusticeStateCreated || justiceRecordsOwner == null
            || definitions.Configuration == null || justiceRecordsOwner.HasP12MutationBoundary
            || justiceRecordsOwner.Revision != 0L || justiceRecordsOwner.WantedRecords.Count != 0
            || justiceRecordsOwner.Sentences.Count != 0)
        {
            LastInitialJusticeStateFailure = "builder or initial owner is not at genesis";
            return false;
        }

        DailyV1StatusDefinition wantedStatus = null;
        string wantedStatusId = definitions.Configuration.wantedStatus;
        bool hasApplicableWarrant = false;
        foreach (DailyV1InitialWantedRecordConfig selectedWarrant in
            definitions.Configuration.initialWarrants ?? Array.Empty<DailyV1InitialWantedRecordConfig>())
        {
            if (selectedWarrant == null
                || string.IsNullOrWhiteSpace(selectedWarrant.target)
                || string.IsNullOrWhiteSpace(selectedWarrant.city))
                continue;

            if (ResolveUniqueNpc(selectedWarrant.target) != null
                && ResolveUniqueStartingCity(selectedWarrant.city) != null)
            {
                hasApplicableWarrant = true;
                break;
            }
        }

        // A wanted-status definition is required only when this profile will
        // create a selected initial warrant. Non-Justice proving profiles can
        // carry the shared configuration object without admitting that status.
        if (!string.IsNullOrWhiteSpace(wantedStatusId)
            && (!definitions.Statuses.TryGetValue(wantedStatusId, out wantedStatus)
                || wantedStatus == null))
        {
            if (hasApplicableWarrant)
            {
                LastInitialJusticeStateFailure = "configured wanted status is absent while an initial warrant applies";
                return false;
            }
            wantedStatus = null;
        }

        foreach (DailyV1InitialWantedRecordConfig selectedWarrant in
            definitions.Configuration.initialWarrants ?? Array.Empty<DailyV1InitialWantedRecordConfig>())
        {
            if (selectedWarrant == null
                || string.IsNullOrWhiteSpace(selectedWarrant.target)
                || string.IsNullOrWhiteSpace(selectedWarrant.city))
                continue;

            DailyV1NpcRuntimeOwners target = ResolveUniqueNpc(selectedWarrant.target);
            DailyV1CityRuntimeOwners city = ResolveUniqueStartingCity(selectedWarrant.city);
            if (target == null || city == null) continue;

            DailyV1JusticeWantedRecordOwner record = justiceRecordsOwner.FindActiveWantedRecord(
                target.RuntimeId, city.RuntimeId);
            if (record == null)
            {
                record = new DailyV1JusticeWantedRecordOwner(
                    target.RuntimeId, city.RuntimeId, selectedWarrant.bounty, selectedWarrant.sentenceDays);
                if (!justiceRecordsOwner.TryAddWantedRecord(record))
                {
                    LastInitialJusticeStateFailure = "initial warrant row could not be added";
                    return false;
                }
            }
            else if (!record.TryAddPenalty(selectedWarrant.bounty, selectedWarrant.sentenceDays))
            {
                LastInitialJusticeStateFailure = "initial warrant penalty could not be added";
                return false;
            }

            // JusticeSystem.CreateOrIncreaseWarrant synchronizes this target
            // immediately after each accepted configuration row. Preserve that
            // ordered insertion behavior before the final roster-wide pass.
            if (wantedStatus != null && !target.Status.Contains(wantedStatus)
                && !target.Status.TryAdd(wantedStatus))
            {
                LastInitialJusticeStateFailure = "target wanted-status owner rejected an initial warrant membership";
                return false;
            }
        }

        if (wantedStatus != null)
        {
            // Match JusticeSystem.SyncWantedStatuses' roster order and exact
            // active-warrant predicate, including removal of authored stale
            // wanted membership for actors without an active warrant.
            foreach (DailyV1NpcRuntimeOwners target in npcs)
            {
                if (target == null)
                {
                    LastInitialJusticeStateFailure = "selected NPC owner is null during wanted-status synchronization";
                    return false;
                }
                bool hasActiveWarrant = justiceRecordsOwner.FindActiveWantedRecords(target.RuntimeId).Count > 0;
                bool hasWantedStatus = target.Status.Contains(wantedStatus);
                if (hasActiveWarrant && !hasWantedStatus && !target.Status.TryAdd(wantedStatus))
                {
                    LastInitialJusticeStateFailure = "wanted-status owner rejected an active-warrant membership";
                    return false;
                }
                if (!hasActiveWarrant && hasWantedStatus && !target.Status.TryRemove(wantedStatus))
                {
                    LastInitialJusticeStateFailure = "wanted-status owner rejected stale-membership removal";
                    return false;
                }
            }
        }

        initialJusticeStateCreated = true;
        LastInitialJusticeStateFailure = null;
        return true;
    }

    /// <summary>
    /// Seeds selected actors' initial local spatial knowledge from the exact
    /// Core City/Location/Route graph. The caller uses this path only when no
    /// selected explorable-site content requires the host's existing site-aware
    /// bootstrap behavior.
    /// </summary>
    internal bool TryBootstrapInitialSpatialKnowledge()
    {
        if (built || definitions.Configuration == null) return false;

        foreach (DailyV1NpcRuntimeOwners npc in npcs)
        {
            string startingLocationId = npc?.Travel?.CurrentLocationRuntimeId;
            if (string.IsNullOrWhiteSpace(startingLocationId)
                || !spatialNetwork.TryGetLocation(startingLocationId, out SpatialLocationRuntime startingLocation))
                continue;

            npc.SpatialKnowledge.DiscoverLocation(startingLocation.RuntimeId);
            foreach (SpatialRouteRuntime route in spatialNetwork.GetOutgoingRoutes(startingLocation))
            {
                if (route == null) continue;
                npc.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
                npc.SpatialKnowledge.DiscoverLocation(route.Destination?.RuntimeId);
            }
        }

        return true;
    }

    /// <summary>
    /// Seeds the accepted selected-profile traveling Merchants from the exact
    /// Core Market, Counterparty, Travel, SpatialKnowledge, and CommercialKnowledge
    /// owners. This mirrors the existing day-zero scenario-knowledge bootstrap;
    /// it does not add site/topology facts or mutate spatial knowledge.
    /// </summary>
    internal bool TryBootstrapInitialCommercialKnowledge(long absoluteDay)
    {
        if (built || initialCommercialKnowledgeCreated || definitions.Configuration == null)
            return false;
        if (absoluteDay != 0L) return true;

        foreach (DailyV1NpcRuntimeOwners npc in npcs)
        {
            if (npc == null) return false;
            if (npc.Lifecycle?.LifeState != NpcLifeState.Alive
                || !DailyV1NpcDefinitionRules.HasJobType(npc.Definition, definitions,
                    DailyV1NpcDefinitionRules.MerchantJobTypeValue)
                || !DailyV1NpcDefinitionRules.HasMerchantBehavior(npc.Definition, definitions,
                    DailyV1NpcDefinitionRules.TravelingMerchantBehaviorValue))
                continue;

            if (!TryFindCityByRuntimeId(npc.Travel?.CurrentCityRuntimeId,
                    out DailyV1CityRuntimeOwners originCity))
                continue;

            if (npc.SpatialKnowledge.KnowsLocation(originCity.LocationRuntimeId))
                ObserveInitialMarket(npc, originCity, absoluteDay);

            if (!spatialNetwork.TryGetLocation(originCity.LocationRuntimeId,
                    out SpatialLocationRuntime originLocation)
                || !npc.SpatialKnowledge.KnowsLocation(originLocation.RuntimeId))
                continue;

            List<SpatialRouteRuntime> knownDirectRoutes = new List<SpatialRouteRuntime>();
            foreach (SpatialRouteRuntime route in spatialNetwork.GetOutgoingRoutes(originLocation))
            {
                if (route == null || !ReferenceEquals(route.Origin, originLocation)
                    || !npc.SpatialKnowledge.KnowsRoute(route.RuntimeId)
                    || route.Destination == null
                    || !npc.SpatialKnowledge.KnowsLocation(route.Destination.RuntimeId)
                    || !spatialNetwork.TryGetSingleDirectRoute(originLocation,
                        route.Destination, out SpatialRouteRuntime directRoute, out _)
                    || !ReferenceEquals(directRoute, route))
                    continue;
                knownDirectRoutes.Add(route);
            }
            knownDirectRoutes.Sort((left, right) => StringComparer.Ordinal.Compare(
                left?.RuntimeId ?? string.Empty, right?.RuntimeId ?? string.Empty));

            foreach (SpatialRouteRuntime route in knownDirectRoutes)
                if (TryFindCityByLocation(route.Destination.RuntimeId,
                        out DailyV1CityRuntimeOwners destinationCity))
                    ObserveInitialMarket(npc, destinationCity, absoluteDay);
        }

        initialCommercialKnowledgeCreated = true;
        return true;
    }

    private static void ObserveInitialMarket(
        DailyV1NpcRuntimeOwners npc,
        DailyV1CityRuntimeOwners city,
        long absoluteDay)
    {
        if (npc?.CommercialKnowledge == null || city?.Market == null || city.Counterparty == null)
            return;

        foreach (DailyV1MarketItemState marketItem in city.Market.Items)
        {
            if (marketItem == null || marketItem.Item == null
                || string.IsNullOrWhiteSpace(marketItem.Item.DefinitionId))
                continue;

            npc.CommercialKnowledge.RecordObservation(new DailyV1CommercialMarketObservation(
                city.LocationRuntimeId, marketItem.Item, marketItem.CurrentPrice,
                marketItem.Amount, absoluteDay, absoluteDay,
                DailyV1CommercialKnowledgeSource.InitialScenarioKnowledge));
        }

        MarketCounterpartyRuntime counterparty = city.Counterparty;
        float observedPurchasingPower = counterparty.LiquidityMode == MarketLiquidityMode.AccountBacked
            ? counterparty.MoneyAccount.Balance
            : 0f;
        npc.CommercialKnowledge.RecordLiquidityObservation(new DailyV1CommercialLiquidityObservation(
            city.LocationRuntimeId, counterparty.LiquidityMode, observedPurchasingPower,
            absoluteDay, absoluteDay,
            DailyV1CommercialKnowledgeSource.InitialScenarioKnowledge));
    }

    private bool TryFindCityByLocation(
        string locationRuntimeId,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        if (string.IsNullOrWhiteSpace(locationRuntimeId)) return false;
        foreach (DailyV1CityRuntimeOwners candidate in cities)
        {
            if (!string.Equals(candidate?.LocationRuntimeId, locationRuntimeId,
                    StringComparison.Ordinal))
                continue;
            if (city != null) return false;
            city = candidate;
        }
        return city != null;
    }

    private bool TryFindCityByRuntimeId(
        string cityRuntimeId,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        if (string.IsNullOrWhiteSpace(cityRuntimeId)) return false;
        foreach (DailyV1CityRuntimeOwners candidate in cities)
        {
            if (!string.Equals(candidate?.RuntimeId, cityRuntimeId,
                    StringComparison.Ordinal))
                continue;
            if (city != null) return false;
            city = candidate;
        }
        return city != null;
    }

    private DailyV1NpcRuntimeOwners ResolveUniqueNpc(string definitionId)
    {
        DailyV1NpcRuntimeOwners match = null;
        foreach (DailyV1NpcRuntimeOwners candidate in npcs)
        {
            if (candidate?.Definition == null
                || !string.Equals(candidate.Definition.DefinitionId, definitionId, StringComparison.Ordinal))
                continue;
            if (match != null) return null;
            match = candidate;
        }
        return match;
    }

    /// <summary>
    /// Constructs selected City routes from the admitted immutable definition
    /// graph and registers them in the Core spatial owner. The optional
    /// callback only binds the already-admitted Route object into a host lookup;
    /// Core owns route validity, identity, ordering, allocation and topology.
    /// </summary>
    internal bool TryCreateConfiguredCityRoutes(
        RuntimeIdAllocator runtimeIds,
        Func<SpatialRouteRuntime, bool> tryBindRouteProjection = null,
        Action<SpatialRouteRuntime> rollbackRouteProjection = null,
        Action<string> logWarning = null,
        Action<string> logError = null)
    {
        if (built || runtimeIds == null
            || ((tryBindRouteProjection == null) != (rollbackRouteProjection == null)))
            return false;

        foreach (DailyV1CityRuntimeOwners origin in cities)
        {
            if (origin?.Definition?.Values?.connections == null) continue;

            List<DailyV1CityConnection> connections = new List<DailyV1CityConnection>(
                origin.Definition.Values.connections);
            connections.Sort((left, right) =>
            {
                int destination = StringComparer.Ordinal.Compare(left?.destination, right?.destination);
                return destination != 0
                    ? destination
                    : Nullable.Compare(left != null ? (int?)left.travelDays : null,
                        right != null ? (int?)right.travelDays : null);
            });

            foreach (DailyV1CityConnection connection in connections)
            {
                if (connection == null)
                {
                    logWarning?.Invoke($"Skipping invalid spatial route from city '{origin.Definition.Values.cityName}': connection is null.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(connection.destination))
                {
                    logWarning?.Invoke($"Skipping invalid spatial route from city '{origin.Definition.Values.cityName}': destination definition is null.");
                    continue;
                }

                if (!citiesByDefinition.TryGetValue(connection.destination,
                        out List<DailyV1CityRuntimeOwners> destinations)
                    || destinations.Count == 0)
                {
                    logWarning?.Invoke($"City definition '{connection.destination}' has no runtime instance.");
                    continue;
                }
                if (destinations.Count > 1)
                {
                    logError?.Invoke($"City definition '{connection.destination}' is ambiguous: {destinations.Count} runtime instances exist. Resolve by RuntimeId instead.");
                    continue;
                }

                DailyV1CityRuntimeOwners destination = destinations[0];
                if (ReferenceEquals(origin, destination))
                {
                    logWarning?.Invoke($"Skipping invalid self route for city '{origin.Definition.Values.cityName}'.");
                    continue;
                }
                if (!spatialNetwork.TryGetLocation(origin.LocationRuntimeId,
                        out SpatialLocationRuntime originLocation)
                    || !spatialNetwork.TryGetLocation(destination.LocationRuntimeId,
                        out SpatialLocationRuntime destinationLocation))
                {
                    logError?.Invoke("Cannot register spatial route: origin and destination must both be registered locations.");
                    continue;
                }

                string routeRuntimeId = runtimeIds.AllocateRouteId();
                SpatialRouteRuntime route = new SpatialRouteRuntime(
                    routeRuntimeId, originLocation, destinationLocation, connection.travelDays);
                if (!spatialNetwork.CanAdvanceRevision)
                {
                    logError?.Invoke("Cannot register spatial route: spatial network census revision is exhausted.");
                    continue;
                }
                if (spatialNetwork.ContainsLocationId(routeRuntimeId)
                    || spatialNetwork.ContainsRouteId(routeRuntimeId))
                {
                    if (identityIndex.TryGetKind(routeRuntimeId, out DailyV1RuntimeIdentityKind existingKind))
                        logError?.Invoke($"Duplicate RuntimeId '{routeRuntimeId}' while registering Route; it is already registered as {GetIdentityKindName(existingKind)}.");
                    else
                        logError?.Invoke($"Duplicate RuntimeId '{routeRuntimeId}' while registering Route.");
                    continue;
                }
                if (!identityIndex.TryStageRegister(routeRuntimeId, DailyV1RuntimeIdentityKind.Route))
                {
                    LogRouteIdentityRejection(routeRuntimeId, logError);
                    continue;
                }
                if (!identityIndex.TryAdvanceRevision())
                {
                    identityIndex.TryRemoveStaged(routeRuntimeId, DailyV1RuntimeIdentityKind.Route);
                    logError?.Invoke("Cannot register Route runtime identity: census revision is exhausted.");
                    continue;
                }

                bool routeProjectionBound = false;
                try
                {
                    if (tryBindRouteProjection != null)
                    {
                        if (!tryBindRouteProjection(route))
                        {
                            RollbackRouteIdentity(routeRuntimeId);
                            continue;
                        }
                        routeProjectionBound = true;
                    }

                    if (!spatialNetwork.TryRegisterRoute(route, () => true))
                    {
                        if (routeProjectionBound) rollbackRouteProjection(route);
                        RollbackRouteIdentity(routeRuntimeId);
                    }
                }
                catch
                {
                    if (routeProjectionBound) rollbackRouteProjection(route);
                    RollbackRouteIdentity(routeRuntimeId);
                    throw;
                }
            }
        }

        return true;
    }

    private void LogRouteIdentityRejection(string runtimeId, Action<string> logError)
    {
        if (logError == null) return;
        if (identityIndex.CensusRevision == long.MaxValue)
        {
            logError("Cannot register Route runtime identity: census revision is exhausted.");
            return;
        }
        if (identityIndex.TryGetKind(runtimeId, out DailyV1RuntimeIdentityKind existingKind))
        {
            logError($"Duplicate RuntimeId '{runtimeId}' while registering Route; it is already registered as {GetIdentityKindName(existingKind)}.");
            return;
        }
        logError($"Cannot register Route runtime identity: Core rejected RuntimeId '{runtimeId}'.");
    }

    private void RollbackRouteIdentity(string runtimeId)
    {
        if (!identityIndex.TryRemoveStaged(runtimeId, DailyV1RuntimeIdentityKind.Route))
            throw new InvalidOperationException("Core Route identity rollback failed.");
        if (!identityIndex.TryRollbackRevision())
            throw new InvalidOperationException("Core Route identity revision rollback failed.");
    }

    private static string GetIdentityKindName(DailyV1RuntimeIdentityKind kind)
    {
        switch (kind)
        {
            case DailyV1RuntimeIdentityKind.Npc: return "NPC";
            case DailyV1RuntimeIdentityKind.City: return "City";
            case DailyV1RuntimeIdentityKind.Location: return "Location";
            case DailyV1RuntimeIdentityKind.Route: return "Route";
            case DailyV1RuntimeIdentityKind.ExplorableSite: return "ExplorableSite";
            case DailyV1RuntimeIdentityKind.LocalPlace: return "LocalPlace";
            case DailyV1RuntimeIdentityKind.LocalConnection: return "LocalConnection";
            case DailyV1RuntimeIdentityKind.NotableItem: return "NotableItem";
            default: return "unknown";
        }
    }

    internal bool TryAddInitialCity(
        string runtimeId,
        string locationRuntimeId,
        DailyV1CityDefinition definition,
        out DailyV1CityRuntimeOwners owners)
    {
        owners = null;
        if (built
            || string.IsNullOrWhiteSpace(runtimeId)
            || string.IsNullOrWhiteSpace(locationRuntimeId)
            || !runtimeIds.Add(runtimeId)) return false;
        bool locationIdentityAdded = false;
        if (!TryEnsureIdentity(locationRuntimeId, DailyV1RuntimeIdentityKind.Location,
                out locationIdentityAdded))
        {
            runtimeIds.Remove(runtimeId);
            return false;
        }
        if (!locationIds.Add(locationRuntimeId))
        {
            runtimeIds.Remove(runtimeId);
            if (locationIdentityAdded) RollbackIdentity(locationRuntimeId, DailyV1RuntimeIdentityKind.Location);
            return false;
        }
        SpatialLocationRuntime registeredLocationHere = null;
        if (!spatialNetwork.ContainsLocationId(locationRuntimeId))
        {
            SpatialLocationRuntime location = new SpatialLocationRuntime(locationRuntimeId);
            if (!spatialNetwork.TryRegisterLocation(location, () => true, () => { }, null))
            {
                runtimeIds.Remove(runtimeId);
                locationIds.Remove(locationRuntimeId);
                if (locationIdentityAdded) RollbackIdentity(locationRuntimeId, DailyV1RuntimeIdentityKind.Location);
                return false;
            }
            registeredLocationHere = location;
        }
        if (!DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
                runtimeId, locationRuntimeId, definitions, definition, null,
                out DailyV1CityRuntimeOwners candidate))
        {
            runtimeIds.Remove(runtimeId);
            locationIds.Remove(locationRuntimeId);
            if (registeredLocationHere != null
                && !spatialNetwork.TryRollbackGenesisLocation(registeredLocationHere, () => { }))
                throw new InvalidOperationException(
                    "A failed selected City owner creation could not roll back its spatial location.");
            if (locationIdentityAdded) RollbackIdentity(locationRuntimeId, DailyV1RuntimeIdentityKind.Location);
            return false;
        }

        if (!identityIndex.TryStageRegister(runtimeId, DailyV1RuntimeIdentityKind.City))
        {
            runtimeIds.Remove(runtimeId);
            locationIds.Remove(locationRuntimeId);
            if (registeredLocationHere != null
                && !spatialNetwork.TryRollbackGenesisLocation(registeredLocationHere, () => { }))
                throw new InvalidOperationException("A rejected City identity could not roll back its staged Location.");
            if (locationIdentityAdded) RollbackIdentity(locationRuntimeId, DailyV1RuntimeIdentityKind.Location);
            return false;
        }
        if (!identityIndex.TryAdvanceRevision())
        {
            identityIndex.TryRemoveStaged(runtimeId, DailyV1RuntimeIdentityKind.City);
            runtimeIds.Remove(runtimeId);
            locationIds.Remove(locationRuntimeId);
            if (registeredLocationHere != null
                && !spatialNetwork.TryRollbackGenesisLocation(registeredLocationHere, () => { }))
                throw new InvalidOperationException("A rejected City revision could not roll back its staged Location.");
            if (locationIdentityAdded) RollbackIdentity(locationRuntimeId, DailyV1RuntimeIdentityKind.Location);
            return false;
        }

        cities.Add(candidate);
        if (!citiesByDefinition.TryGetValue(definition.DefinitionId, out List<DailyV1CityRuntimeOwners> matches))
        {
            matches = new List<DailyV1CityRuntimeOwners>();
            citiesByDefinition.Add(definition.DefinitionId, matches);
        }
        matches.Add(candidate);
        owners = candidate;
        return true;
    }

    internal bool TryRemoveInitialCity(DailyV1CityRuntimeOwners owners)
    {
        if (built || owners == null || !cities.Remove(owners)) return false;
        if (!identityIndex.TryRemoveStaged(owners.RuntimeId, DailyV1RuntimeIdentityKind.City)
            || !identityIndex.TryRollbackRevision())
            throw new InvalidOperationException("Cannot roll back the Core City identity committed at genesis.");
        locationIds.Remove(owners.LocationRuntimeId);
        runtimeIds.Remove(owners.RuntimeId);
        if (citiesByDefinition.TryGetValue(owners.Definition.DefinitionId, out List<DailyV1CityRuntimeOwners> matches))
        {
            matches.Remove(owners);
            if (matches.Count == 0) citiesByDefinition.Remove(owners.Definition.DefinitionId);
        }
        return true;
    }

    internal bool TryAddInitialNpc(
        string runtimeId,
        DailyV1NpcDefinition definition,
        DailyV1NpcSimulationConfig configuration,
        out DailyV1NpcRuntimeOwners owners)
    {
        owners = null;
        if (built || string.IsNullOrWhiteSpace(runtimeId) || runtimeIds.Contains(runtimeId)
            || !identityIndex.CanRegister(runtimeId)) return false;
        runtimeIds.Add(runtimeId);
        if (!IsExactConfiguredNpc(definition, configuration)
            || (!string.IsNullOrWhiteSpace(configuration.startingCity)
                && !definitions.Cities.ContainsKey(configuration.startingCity))
            || !DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
                runtimeId, definitions, definition, configuration.initialMoney,
                out DailyV1NpcRuntimeOwners candidate)
            || !ApplyInitialInventory(candidate.Inventory, configuration))
        {
            runtimeIds.Remove(runtimeId);
            return false;
        }

        DailyV1CityRuntimeOwners startingCity = ResolveUniqueStartingCity(configuration.startingCity);
        if (startingCity != null)
        {
            if (!startingCity.NpcPresence.TryAdd(runtimeId)
                || !candidate.Travel.TryCommitState(
                    startingCity.LocationRuntimeId, startingCity.RuntimeId,
                    null, null, 0, 0, null, false, null, null, false))
            {
                runtimeIds.Remove(runtimeId);
                return false;
            }
        }

        if (!identityIndex.TryStageRegister(runtimeId, DailyV1RuntimeIdentityKind.Npc))
        {
            runtimeIds.Remove(runtimeId);
            return false;
        }
        if (!identityIndex.TryAdvanceRevision())
        {
            identityIndex.TryRemoveStaged(runtimeId, DailyV1RuntimeIdentityKind.Npc);
            runtimeIds.Remove(runtimeId);
            return false;
        }

        npcs.Add(candidate);
        owners = candidate;
        return true;
    }

    internal bool TryRemoveInitialNpc(DailyV1NpcRuntimeOwners owners)
    {
        if (built || owners == null || !npcs.Remove(owners)) return false;
        if (!identityIndex.TryRemoveStaged(owners.RuntimeId, DailyV1RuntimeIdentityKind.Npc)
            || !identityIndex.TryRollbackRevision())
            throw new InvalidOperationException("Cannot roll back the Core NPC identity committed at genesis.");
        runtimeIds.Remove(owners.RuntimeId);
        return true;
    }

    internal DailyV1CityRuntimeOwners ResolveUniqueStartingCity(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId)
            || !citiesByDefinition.TryGetValue(definitionId, out List<DailyV1CityRuntimeOwners> matches)
            || matches.Count != 1)
            return null;
        return matches[0];
    }

    internal bool TryGetLocationRuntimeId(DailyV1CityRuntimeOwners city, out string runtimeId)
    {
        runtimeId = null;
        if (city == null || !cities.Contains(city)) return false;
        runtimeId = city.LocationRuntimeId;
        return !string.IsNullOrWhiteSpace(runtimeId);
    }

    internal bool TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryBuild(null, out graph);
    }

    internal bool TryBuild(
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryBuild(justiceRecordsOwner, null, out graph);
    }

    internal bool TryBuild(
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryBuild(justiceRecordsOwner, travelParties, null, out graph);
    }

    internal bool TryBuild(
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        ScheduledDirectiveStore scheduledDirectives,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        return TryBuild(justiceRecordsOwner, travelParties, scheduledDirectives,
            null, out graph);
    }

    internal bool TryBuild(
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        ScheduledDirectiveStore scheduledDirectives,
        ExpeditionStore expeditions,
        out DailyV1SelectedRuntimeOwnerGraph graph)
    {
        graph = null;
        if (built || !MatchesConfiguredCityRows() || !MatchesConfiguredNpcRows()
            || !HasConsistentInitialPresence()) return false;
        if (!TryBindSpatialIdentities()) return false;
        if (!DailyV1SelectedRuntimeOwnerGraph.TryCreate(
                definitions, cities, npcs, spatialNetwork, justiceRecordsOwner,
                travelParties, identityIndex, out graph))
            return false;
        if ((scheduledDirectives != null && !graph.TryBindScheduledDirectives(scheduledDirectives))
            || (expeditions != null && !graph.TryBindExpeditionStore(expeditions)))
        {
            graph = null;
            return false;
        }
        built = true;
        return true;
    }

    private bool TryBindSpatialIdentities()
    {
        List<KeyValuePair<string, DailyV1RuntimeIdentityKind>> added =
            new List<KeyValuePair<string, DailyV1RuntimeIdentityKind>>();
        foreach (SpatialLocationRuntime location in spatialNetwork.Locations)
        {
            if (location == null || !TryEnsureIdentity(location.RuntimeId,
                    DailyV1RuntimeIdentityKind.Location, out bool wasAdded))
            {
                RollbackIdentities(added);
                return false;
            }
            if (wasAdded) added.Add(new KeyValuePair<string, DailyV1RuntimeIdentityKind>(
                location.RuntimeId, DailyV1RuntimeIdentityKind.Location));
        }
        foreach (SpatialRouteRuntime route in spatialNetwork.Routes)
        {
            if (route == null || !TryEnsureIdentity(route.RuntimeId,
                    DailyV1RuntimeIdentityKind.Route, out bool wasAdded))
            {
                RollbackIdentities(added);
                return false;
            }
            if (wasAdded) added.Add(new KeyValuePair<string, DailyV1RuntimeIdentityKind>(
                route.RuntimeId, DailyV1RuntimeIdentityKind.Route));
        }
        return true;
    }

    private bool TryEnsureIdentity(
        string runtimeId,
        DailyV1RuntimeIdentityKind kind,
        out bool added)
    {
        added = false;
        if (identityIndex.HasIdentity(runtimeId, kind)) return true;
        if (!identityIndex.TryStageRegister(runtimeId, kind)) return false;
        if (!identityIndex.TryAdvanceRevision())
        {
            if (!identityIndex.TryRemoveStaged(runtimeId, kind))
                throw new InvalidOperationException("A rejected Core runtime identity could not be removed.");
            return false;
        }
        added = true;
        return true;
    }

    private void RollbackIdentity(string runtimeId, DailyV1RuntimeIdentityKind kind)
    {
        if (!identityIndex.TryRemoveStaged(runtimeId, kind)
            || !identityIndex.TryRollbackRevision())
            throw new InvalidOperationException("A staged Core runtime identity could not be rolled back.");
    }

    private void RollbackIdentities(
        List<KeyValuePair<string, DailyV1RuntimeIdentityKind>> identities)
    {
        for (int i = identities.Count - 1; i >= 0; i--)
            RollbackIdentity(identities[i].Key, identities[i].Value);
    }

    private bool IsExactConfiguredNpc(
        DailyV1NpcDefinition definition,
        DailyV1NpcSimulationConfig configuration)
    {
        if (definition == null || configuration == null
            || !string.Equals(configuration.npc, definition.DefinitionId, StringComparison.Ordinal)
            || !definitions.Npcs.TryGetValue(definition.DefinitionId, out DailyV1NpcDefinition admitted)
            || !ReferenceEquals(definition, admitted)) return false;

        DailyV1NpcSimulationConfig exact = null;
        foreach (DailyV1NpcSimulationConfig row in definitions.Configuration?.npcs
            ?? Array.Empty<DailyV1NpcSimulationConfig>())
        {
            if (row == null || !string.Equals(row.npc, definition.DefinitionId, StringComparison.Ordinal)) continue;
            if (exact != null) return false;
            exact = row;
        }
        return ReferenceEquals(configuration, exact);
    }

    private bool ApplyInitialInventory(
        DailyV1InventoryOwner inventory,
        DailyV1NpcSimulationConfig configuration)
    {
        if (inventory == null || configuration == null
            || !ReferenceEquals(inventory.Definitions, definitions)) return false;
        foreach (DailyV1NpcInitialInventoryItemConfig row in
            configuration.initialInventory ?? Array.Empty<DailyV1NpcInitialInventoryItemConfig>())
        {
            if (row == null || string.IsNullOrWhiteSpace(row.item)) continue;
            if (!definitions.Items.TryGetValue(row.item, out DailyV1ItemDefinition item)
                || !inventory.TryAddItem(item, row.amount, row.averageUnitCost)) return false;
        }
        return true;
    }

    private bool MatchesConfiguredCityRows()
    {
        List<string> expected = new List<string>(
            definitions.Configuration?.cities ?? Array.Empty<string>());
        expected.RemoveAll(string.IsNullOrWhiteSpace);
        expected.Sort(StringComparer.Ordinal);
        if (expected.Count != cities.Count) return false;
        for (int index = 0; index < expected.Count; index++)
            if (!string.Equals(expected[index], cities[index].Definition.DefinitionId, StringComparison.Ordinal))
                return false;
        return true;
    }

    private bool MatchesConfiguredNpcRows()
    {
        Dictionary<string, DailyV1NpcSimulationConfig> expected =
            new Dictionary<string, DailyV1NpcSimulationConfig>(StringComparer.Ordinal);
        foreach (DailyV1NpcSimulationConfig row in definitions.Configuration?.npcs
            ?? Array.Empty<DailyV1NpcSimulationConfig>())
        {
            if (row == null || string.IsNullOrWhiteSpace(row.npc)) continue;
            if (expected.ContainsKey(row.npc)) return false;
            expected.Add(row.npc, row);
        }
        List<string> ids = new List<string>(expected.Keys);
        ids.Sort(StringComparer.Ordinal);
        if (ids.Count != npcs.Count) return false;
        for (int index = 0; index < ids.Count; index++)
            if (!string.Equals(ids[index], npcs[index].Definition.DefinitionId, StringComparison.Ordinal))
                return false;
        return true;
    }

    private bool HasConsistentInitialPresence()
    {
        HashSet<string> seenNpcIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (string.IsNullOrWhiteSpace(city.LocationRuntimeId)) return false;
            foreach (string npcRuntimeId in city.NpcPresence.OrderedNpcRuntimeIds)
            {
                if (!seenNpcIds.Add(npcRuntimeId)) return false;
                DailyV1NpcRuntimeOwners npc = npcs.Find(owner =>
                    string.Equals(owner.RuntimeId, npcRuntimeId, StringComparison.Ordinal));
                if (npc == null
                    || !string.Equals(npc.Travel.CurrentCityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
                    || !string.Equals(npc.Travel.CurrentLocationRuntimeId,
                        city.LocationRuntimeId, StringComparison.Ordinal))
                    return false;
            }
        }

        foreach (DailyV1NpcRuntimeOwners npc in npcs)
        {
            if (npc.Travel.CurrentCityRuntimeId == null)
            {
                if (npc.Travel.CurrentLocationRuntimeId != null || seenNpcIds.Contains(npc.RuntimeId)) return false;
                continue;
            }
            if (!seenNpcIds.Contains(npc.RuntimeId)) return false;
        }
        return true;
    }
}

internal enum DailyV1SelectedActionStatusPlanResult
{
    ActionDidNotSucceed,
    Applied,
    ActorRejected,
    TargetRejected
}

/// <summary>
/// Immutable actor-first/target-second status mutation plan. The adapter
/// callbacks still own host-specific admission, revision, epoch and
/// compatibility projection behavior through each existing NPC owner writer.
/// </summary>
internal sealed class DailyV1SelectedActionStatusPlan
{
    internal delegate bool ApplyOwnerStatusChanges(
        DailyV1NpcRuntimeOwners owner,
        IReadOnlyList<DailyV1StatusDefinition> statusToRemove,
        IReadOnlyList<DailyV1StatusDefinition> statusToAdd);

    private readonly DailyV1NpcRuntimeOwners actorOwner;
    private readonly IReadOnlyList<DailyV1StatusDefinition> actorStatusToRemove;
    private readonly IReadOnlyList<DailyV1StatusDefinition> actorStatusToAdd;
    private readonly bool targetExpected;
    private readonly DailyV1NpcRuntimeOwners targetOwner;
    private readonly IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove;
    private readonly IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd;

    internal DailyV1SelectedActionStatusPlan(
        DailyV1NpcRuntimeOwners actorOwner,
        IReadOnlyList<DailyV1StatusDefinition> actorStatusToRemove,
        IReadOnlyList<DailyV1StatusDefinition> actorStatusToAdd,
        bool targetExpected,
        DailyV1NpcRuntimeOwners targetOwner,
        IReadOnlyList<DailyV1StatusDefinition> targetStatusToRemove,
        IReadOnlyList<DailyV1StatusDefinition> targetStatusToAdd)
    {
        this.actorOwner = actorOwner ?? throw new ArgumentNullException(nameof(actorOwner));
        this.actorStatusToRemove = actorStatusToRemove ?? throw new ArgumentNullException(nameof(actorStatusToRemove));
        this.actorStatusToAdd = actorStatusToAdd ?? throw new ArgumentNullException(nameof(actorStatusToAdd));
        this.targetExpected = targetExpected;
        this.targetOwner = targetOwner;
        this.targetStatusToRemove = targetStatusToRemove ?? throw new ArgumentNullException(nameof(targetStatusToRemove));
        this.targetStatusToAdd = targetStatusToAdd ?? throw new ArgumentNullException(nameof(targetStatusToAdd));
    }

    internal DailyV1SelectedActionStatusPlanResult Apply(
        ApplyOwnerStatusChanges applyActor,
        ApplyOwnerStatusChanges applyTarget)
    {
        if (applyActor == null
            || !applyActor(actorOwner, actorStatusToRemove, actorStatusToAdd))
            return DailyV1SelectedActionStatusPlanResult.ActorRejected;

        if (!targetExpected) return DailyV1SelectedActionStatusPlanResult.Applied;

        return applyTarget != null
            && applyTarget(targetOwner, targetStatusToRemove, targetStatusToAdd)
            ? DailyV1SelectedActionStatusPlanResult.Applied
            : DailyV1SelectedActionStatusPlanResult.TargetRejected;
    }
}
