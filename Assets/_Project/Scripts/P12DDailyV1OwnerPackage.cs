using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

internal enum P12DDailyV1OwnerPackageFailure
{
    None = 0,
    InvalidCaptureContext,
    InvalidOwnerEvidence,
    InvalidOwnerSnapshot,
    InvalidDefinition,
    InvalidRelation,
    InvalidIdentityRoot,
    StageFailed,
    StaleBoundary
}

/// <summary>
/// A private, unpublished Daily-v1 D owner package. Its references are valid
/// only inside the containing staged composition; P12-G owns whole-profile
/// validation and final publication.
/// </summary>
internal sealed class P12DDailyV1OwnerPackage
{
    internal DailyCaptureStagingAttempt StagingAttempt { get; }
    internal WorldId WorldId { get; }
    internal RuntimeIdentityRegistry RuntimeIdentities { get; }
    internal SpatialNetworkRuntime SpatialNetwork { get; }
    internal IReadOnlyList<CityRuntime> Cities { get; }
    internal IReadOnlyList<NpcRuntime> Npcs { get; }
    internal IReadOnlyList<P12DNpcFRow> NpcFRows { get; }
    internal PersonStore Persons { get; }
    internal GenealogyStore Genealogy { get; }
    internal ExplorableSiteStore EmptyExplorableSites { get; }

    private P12DDailyV1OwnerPackage(
        DailyCaptureStagingAttempt stagingAttempt,
        WorldId worldId,
        RuntimeIdentityRegistry runtimeIdentities,
        SpatialNetworkRuntime spatialNetwork,
        IReadOnlyList<CityRuntime> cities,
        IReadOnlyList<NpcRuntime> npcs,
        IReadOnlyList<P12DNpcFRow> npcFRows,
        PersonStore persons,
        GenealogyStore genealogy,
        ExplorableSiteStore emptyExplorableSites)
    {
        StagingAttempt = stagingAttempt;
        WorldId = worldId;
        RuntimeIdentities = runtimeIdentities;
        SpatialNetwork = spatialNetwork;
        Cities = new ReadOnlyCollection<CityRuntime>(new List<CityRuntime>(cities));
        Npcs = new ReadOnlyCollection<NpcRuntime>(new List<NpcRuntime>(npcs));
        NpcFRows = new ReadOnlyCollection<P12DNpcFRow>(new List<P12DNpcFRow>(npcFRows));
        Persons = persons;
        Genealogy = genealogy;
        EmptyExplorableSites = emptyExplorableSites;
    }

    /// <summary>
    /// Captures and privately stages the accepted Daily-v1 D owners at one
    /// already completed P12-B boundary. The supplied identity registry must
    /// be the still-private registry from the staged P12-C roots. This method
    /// may add D identities to that private registry; the caller must discard
    /// the entire P12-C/D composition if this method returns false.
    /// </summary>
    internal static bool TryCaptureAndStage(
        SimulationRuntime sourceRuntime,
        DailyCaptureEligibilityToken token,
        DailyCaptureStagingAttempt stagingAttempt,
        IReadOnlyList<OwnerSectionCensusSnapshot> exactOwnerSectionVector,
        RuntimeIdentityRegistry sourceRuntimeIdentities,
        SpatialNetworkRuntime sourceSpatialNetwork,
        ExplorableSiteStore sourceExplorableSites,
        RuntimeIdentityRegistry stagedRuntimeIdentities,
        WorldId stagedWorldId,
        IReadOnlyList<CityData> cityDefinitions,
        IReadOnlyList<ItemData> itemDefinitions,
        IReadOnlyList<NpcData> npcDefinitions,
        IReadOnlyList<NpcActionData> actionDefinitions,
        IReadOnlyList<NpcStatusData> statusDefinitions,
        IReadOnlyList<ExplorableSiteData> admittedDailySiteDefinitions,
        IReadOnlyList<string> stagedTravelPartyIds,
        out P12DDailyV1OwnerPackage package,
        out P12DDailyV1OwnerPackageFailure failure,
        Action<P12GDailyV1RestoreStage> stageObserver = null)
    {
        package = null;
        failure = P12DDailyV1OwnerPackageFailure.InvalidCaptureContext;
        if (sourceRuntime == null || token == null || stagingAttempt == null
            || exactOwnerSectionVector == null || sourceRuntimeIdentities == null
            || sourceSpatialNetwork == null || sourceExplorableSites == null
            || stagedRuntimeIdentities == null || stagedWorldId == null
            || cityDefinitions == null || itemDefinitions == null
            || npcDefinitions == null || actionDefinitions == null
            || statusDefinitions == null || admittedDailySiteDefinitions == null
            || stagedTravelPartyIds == null
            || !ReferenceEquals(token.OwnerSections, exactOwnerSectionVector)
            || !stagingAttempt.IsCurrentFor(sourceRuntime, token, exactOwnerSectionVector)
            || !ReferenceEquals(token.WorldId, sourceRuntime.WorldId)
            || !token.WorldId.Equals(stagedWorldId)
            || !sourceRuntime.TryValidateCompletedDailyCaptureToken(token, out _)
            || !sourceRuntime.HasSameP12RuntimeIdentitySpatialOwners(
                sourceRuntimeIdentities, sourceSpatialNetwork, sourceExplorableSites))
        {
            return false;
        }

        PersonStore sourcePersons = sourceRuntime.PersonStore;
        GenealogyStore sourceGenealogy = sourceRuntime.GenealogyStoreForWorldBoundary;
        if (sourcePersons == null || sourceGenealogy == null
            || !TryValidateStagedIdentityRegistryIsEmpty(stagedRuntimeIdentities)
            || !TryValidateSourceOwnerSections(
                token, sourceRuntimeIdentities, sourceSpatialNetwork,
                sourceExplorableSites, sourcePersons, sourceGenealogy))
        {
            failure = P12DDailyV1OwnerPackageFailure.InvalidOwnerEvidence;
            return false;
        }

        object sharedStamp = stagingAttempt;
        IReadOnlyList<OwnerSectionCensusSnapshot> vector = exactOwnerSectionVector;
        List<P12DCityRootOwnerSnapshot.StagingCaptureEnvelope> cityCaptures =
            new List<P12DCityRootOwnerSnapshot.StagingCaptureEnvelope>(sourceRuntime.Cities.Count);
        for (int i = 0; i < sourceRuntime.Cities.Count; i++)
        {
            CityRuntime sourceCity = sourceRuntime.Cities[i];
            if (sourceCity == null
                || !P12DCityRootOwnerSnapshot.TryCaptureForStaging(
                    sourceCity, token, sharedStamp, vector,
                    out P12DCityRootOwnerSnapshot.StagingCaptureEnvelope cityCapture, out _)
                || cityCapture == null)
            {
                failure = P12DDailyV1OwnerPackageFailure.InvalidOwnerSnapshot;
                return false;
            }
            cityCaptures.Add(cityCapture);
        }

        if (!P12DNpcRootOwnerSnapshot.TryCapture(
                sourceRuntime, token, sharedStamp, vector,
                out P12DNpcDProjection dProjection,
                out P12DNpcFProjection fProjection,
                out _)
            || dProjection == null || fProjection == null
            || dProjection.Rows.Count != sourceRuntime.NpcRuntimes.Count
            || fProjection.Rows.Count != sourceRuntime.NpcRuntimes.Count)
        {
            failure = P12DDailyV1OwnerPackageFailure.InvalidOwnerSnapshot;
            return false;
        }

        PersonStoreOwnerSnapshot personSnapshot = sourcePersons.CaptureOwnerSnapshot();
        GenealogyOwnerSnapshot genealogySnapshot = sourceGenealogy.CaptureOwnerSnapshot();
        SpatialNetworkOwnerSnapshot spatialSnapshot = sourceSpatialNetwork.CaptureOwnerSnapshot();
        if (!sourceExplorableSites.TryCaptureOwnerSnapshot(
                out ExplorableSiteOwnerSnapshot siteSnapshot, out _)
            || siteSnapshot == null
            || siteSnapshot.Sites == null || siteSnapshot.Sites.Count != 0 || siteSnapshot.Revision != 0L
            || admittedDailySiteDefinitions.Count != 0)
        {
            failure = P12DDailyV1OwnerPackageFailure.InvalidOwnerSnapshot;
            return false;
        }

        if (!sourceRuntime.TryValidateCompletedDailyCaptureToken(token, out _)
            || !TryValidateCapturedOwnerCardinalities(
                token, sourcePersons, personSnapshot, sourceGenealogy, genealogySnapshot,
                sourceSpatialNetwork, spatialSnapshot, sourceExplorableSites, siteSnapshot))
        {
            failure = P12DDailyV1OwnerPackageFailure.StaleBoundary;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.DSnapshotsCaptured);

        if (!SpatialNetworkRuntime.TryCreateFromOwnerSnapshot(
                spatialSnapshot, stagedRuntimeIdentities,
                out SpatialNetworkRuntime stagedSpatialNetwork, out _)
            || stagedSpatialNetwork == null)
        {
            failure = P12DDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.DSpatialNetworkStaged);

        if (!PersonStore.TryCreateFromOwnerSnapshot(
                personSnapshot, out PersonStore stagedPersons, out _)
            || stagedPersons == null)
        {
            failure = P12DDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.DPersonsStaged);

        stageObserver?.Invoke(P12GDailyV1RestoreStage.DGenealogyHydratorEntry);
        if (!GenealogyStore.TryCreateFromOwnerSnapshot(
                genealogySnapshot, out GenealogyStore stagedGenealogy, out _)
            || stagedGenealogy == null)
        {
            failure = P12DDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.DGenealogyStaged);

        SpatialLocationRuntime[] stagedLocations = stagedSpatialNetwork.Locations.ToArray();
        string[] stagedRouteIds = stagedSpatialNetwork.Routes.Select(route => route?.RuntimeId).ToArray();
        Dictionary<string, SpatialLocationRuntime> locationsById =
            stagedLocations.ToDictionary(location => location.RuntimeId, StringComparer.Ordinal);
        if (!TryBuildUniqueMap(cityDefinitions, definition => definition?.DefinitionId, out _)
            || !TryBuildUniqueMap(itemDefinitions, definition => definition?.DefinitionId, out _))
        {
            failure = P12DDailyV1OwnerPackageFailure.InvalidDefinition;
            return false;
        }

        List<CityRuntime> stagedCities = new List<CityRuntime>(cityCaptures.Count);
        List<P12DCityMembershipLinker> cityLinkers =
            new List<P12DCityMembershipLinker>(cityCaptures.Count);
        Dictionary<string, string> stagedCityLocationIds =
            new Dictionary<string, string>(StringComparer.Ordinal);
        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12DCityRootOwnerSnapshot.StagingCaptureEnvelope cityCapture in cityCaptures)
        {
            P12DCityRootOwnerSnapshot citySnapshot = cityCapture.Snapshot;
            if (citySnapshot == null || !cityIds.Add(citySnapshot.CityRuntimeId)
                || !locationsById.TryGetValue(citySnapshot.LegacyLocationRuntimeId, out _)
                || !stagedRuntimeIdentities.IsRuntimeIdAvailable(citySnapshot.CityRuntimeId))
            {
                failure = P12DDailyV1OwnerPackageFailure.InvalidIdentityRoot;
                return false;
            }
            stagedCityLocationIds.Add(citySnapshot.CityRuntimeId, citySnapshot.LegacyLocationRuntimeId);
        }

        foreach (P12DCityRootOwnerSnapshot.StagingCaptureEnvelope cityCapture in cityCaptures)
        {
            if (!cityCapture.TryStage(
                    cityDefinitions, itemDefinitions, stagedLocations,
                    out CityRuntime stagedCity,
                    out P12DCityMembershipLinker linker, out _)
                || stagedCity == null || linker == null
                || !stagedRuntimeIdentities.RegisterCity(stagedCity))
            {
                failure = P12DDailyV1OwnerPackageFailure.StageFailed;
                return false;
            }
            stagedCities.Add(stagedCity);
            cityLinkers.Add(linker);
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.DCitiesStaged);

        if (!TryPreflightRelations(
                personSnapshot, genealogySnapshot, cityCaptures,
                dProjection.Rows, locationsById, stagedCityLocationIds))
        {
            failure = P12DDailyV1OwnerPackageFailure.InvalidRelation;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.DRelationsValidated);

        if (!P12DNpcRootOwnerSnapshot.TryStage(
                sourceRuntime, token, dProjection, fProjection, cityLinkers, stagedPersons,
                npcDefinitions, actionDefinitions, statusDefinitions, itemDefinitions,
                stagedLocations, stagedRouteIds, stagedTravelPartyIds,
                out IReadOnlyList<NpcRuntime> stagedNpcRoster, out _)
            || stagedNpcRoster == null)
        {
            failure = P12DDailyV1OwnerPackageFailure.StageFailed;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.DNpcsStaged);

        foreach (NpcRuntime stagedNpc in stagedNpcRoster)
        {
            if (!stagedRuntimeIdentities.RegisterNpc(stagedNpc))
            {
                failure = P12DDailyV1OwnerPackageFailure.InvalidIdentityRoot;
                return false;
            }
        }

        if (!ExplorableSiteStore.TryCreateFromOwnerSnapshot(
                siteSnapshot, admittedDailySiteDefinitions, stagedLocations,
                out ExplorableSiteStore stagedSites, out _)
            || stagedSites == null || stagedSites.Count != 0 || stagedSites.Revision != 0L
            || !TryValidateStagedIdentityCardinalities(
                token, stagedRuntimeIdentities, sourceRuntimeIdentities)
            || !sourceRuntime.TryValidateCompletedDailyCaptureToken(token, out _)
            || !TryValidateFinalGraph(
                stagedCities, stagedNpcRoster, stagedPersons, stagedGenealogy,
                stagedLocations, stagedSpatialNetwork, cityCaptures, dProjection.Rows))
        {
            failure = P12DDailyV1OwnerPackageFailure.InvalidRelation;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.DFinalGraphValidated);

        if (!stagingAttempt.IsCurrentFor(sourceRuntime, token, exactOwnerSectionVector))
        {
            failure = P12DDailyV1OwnerPackageFailure.StaleBoundary;
            return false;
        }

        package = new P12DDailyV1OwnerPackage(
            stagingAttempt,
            stagedWorldId, stagedRuntimeIdentities, stagedSpatialNetwork,
            stagedCities, stagedNpcRoster, fProjection.Rows,
            stagedPersons, stagedGenealogy, stagedSites);
        failure = P12DDailyV1OwnerPackageFailure.None;
        return true;
    }

    private static bool TryValidateSourceOwnerSections(
        DailyCaptureEligibilityToken token,
        RuntimeIdentityRegistry sourceRuntimeIdentities,
        SpatialNetworkRuntime sourceSpatialNetwork,
        ExplorableSiteStore sourceExplorableSites,
        PersonStore sourcePersons,
        GenealogyStore sourceGenealogy)
    {
        if (!TryMatch(token.OwnerSections,
                new PersonMembershipCensusProvider(sourcePersons), OwnerSectionRole.Required)
            || !TryMatch(token.OwnerSections,
                new PersonMaterializationBindingCensusProvider(sourcePersons), OwnerSectionRole.Required)
            || !TryMatch(token.OwnerSections,
                new GenealogyCensusProvider(sourceGenealogy), OwnerSectionRole.Required)
            || !TryMatch(token.OwnerSections,
                SpatialNetworkCensusProvider.CreateProviders(sourceSpatialNetwork)[0], OwnerSectionRole.Required)
            || !TryMatch(token.OwnerSections,
                SpatialNetworkCensusProvider.CreateProviders(sourceSpatialNetwork)[1], OwnerSectionRole.Required)
            || !TryMatch(token.OwnerSections,
                new ExplorableSiteCensusProvider(sourceExplorableSites), OwnerSectionRole.ExplicitlyEmpty))
            return false;

        IReadOnlyList<IOwnerSectionCensusProvider> identityProviders =
            RuntimeIdentityRegistryCensusProvider.CreateProviders(sourceRuntimeIdentities);
        for (int i = 0; i < identityProviders.Count; i++)
        {
            OwnerSectionRole role = i < 4 ? OwnerSectionRole.Required : OwnerSectionRole.ExplicitlyEmpty;
            if (!TryMatch(token.OwnerSections, identityProviders[i], role))
                return false;
        }
        return true;
    }

    private static bool TryValidateCapturedOwnerCardinalities(
        DailyCaptureEligibilityToken token,
        PersonStore sourcePersons,
        PersonStoreOwnerSnapshot personSnapshot,
        GenealogyStore sourceGenealogy,
        GenealogyOwnerSnapshot genealogySnapshot,
        SpatialNetworkRuntime sourceSpatial,
        SpatialNetworkOwnerSnapshot spatialSnapshot,
        ExplorableSiteStore sourceSites,
        ExplorableSiteOwnerSnapshot siteSnapshot)
    {
        if (personSnapshot == null || genealogySnapshot == null || spatialSnapshot == null || siteSnapshot == null)
            return false;
        return TryMatch(token.OwnerSections, PersonMembershipCensusProvider.SectionId,
                   PersonMembershipCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                   sourcePersons, personSnapshot.MembershipCount, personSnapshot.Revision)
            && TryMatch(token.OwnerSections, PersonMaterializationBindingCensusProvider.SectionId,
                   PersonMaterializationBindingCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                   sourcePersons, personSnapshot.BindingCount, personSnapshot.Revision)
            && TryMatch(token.OwnerSections, GenealogyCensusProvider.SectionId,
                   GenealogyCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                   sourceGenealogy, genealogySnapshot.Records?.Count ?? -1, genealogySnapshot.Revision)
            && TryMatch(token.OwnerSections, SpatialNetworkCensusProvider.LocationsSectionId,
                   SpatialNetworkCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                   sourceSpatial, spatialSnapshot.LocationRuntimeIds?.Count ?? -1, spatialSnapshot.Revision)
            && TryMatch(token.OwnerSections, SpatialNetworkCensusProvider.RoutesSectionId,
                   SpatialNetworkCensusProvider.SchemaVersion, OwnerSectionRole.Required,
                   sourceSpatial, spatialSnapshot.Routes?.Count ?? -1, spatialSnapshot.Revision)
            && TryMatch(token.OwnerSections, ExplorableSiteCensusProvider.SectionId,
                   ExplorableSiteCensusProvider.SchemaVersion, OwnerSectionRole.ExplicitlyEmpty,
                   sourceSites, siteSnapshot.Sites?.Count ?? -1, siteSnapshot.Revision)
            && siteSnapshot.Sites.Count == 0 && siteSnapshot.Revision == 0L;
    }

    private static bool TryValidateStagedIdentityRegistryIsEmpty(RuntimeIdentityRegistry registry)
    {
        if (registry.CensusRevision != 0L) return false;
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            RuntimeIdentityRegistryCensusProvider.CreateProviders(registry);
        return providers.All(provider => provider.GetCurrentCensus().Cardinality == 0);
    }

    private static bool TryValidateStagedIdentityCardinalities(
        DailyCaptureEligibilityToken token,
        RuntimeIdentityRegistry staged,
        RuntimeIdentityRegistry source)
    {
        IReadOnlyList<IOwnerSectionCensusProvider> expected = RuntimeIdentityRegistryCensusProvider.CreateProviders(source);
        IReadOnlyList<IOwnerSectionCensusProvider> actual = RuntimeIdentityRegistryCensusProvider.CreateProviders(staged);
        if (expected.Count != actual.Count) return false;
        long expectedRevision = -1L;
        for (int i = 0; i < expected.Count; i++)
        {
            OwnerSectionCensusWitness expectedWitness = expected[i].GetCurrentCensus();
            OwnerSectionCensusWitness actualWitness = actual[i].GetCurrentCensus();
            if (expectedWitness == null || actualWitness == null
                || !string.Equals(expectedWitness.SectionId, actualWitness.SectionId, StringComparison.Ordinal)
                || expectedWitness.Cardinality != actualWitness.Cardinality
                || !TryMatch(token.OwnerSections, expectedWitness.SectionId,
                    expectedWitness.SchemaVersion,
                    i < 4 ? OwnerSectionRole.Required : OwnerSectionRole.ExplicitlyEmpty,
                    source, expectedWitness.Cardinality, expectedWitness.Revision))
                return false;
            if (expectedRevision < 0L) expectedRevision = expectedWitness.Revision;
            else if (expectedRevision != expectedWitness.Revision) return false;
        }
        return staged.CensusRevision == expectedRevision;
    }

    internal static bool TryPreflightRelations(
        PersonStoreOwnerSnapshot persons,
        GenealogyOwnerSnapshot genealogy,
        IReadOnlyList<P12DCityRootOwnerSnapshot.StagingCaptureEnvelope> cityCaptures,
        IReadOnlyList<P12DNpcDRow> npcRows,
        IReadOnlyDictionary<string, SpatialLocationRuntime> locationsById,
        IReadOnlyDictionary<string, string> cityLocationIds)
    {
        if (persons?.Rows == null || genealogy?.Records == null || cityCaptures == null
            || npcRows == null || locationsById == null || cityLocationIds == null)
            return false;

        HashSet<string> personIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, PersonStoreOwnerSnapshotRow> personsById =
            new Dictionary<string, PersonStoreOwnerSnapshotRow>(StringComparer.Ordinal);
        Dictionary<string, string> personByNpcId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (PersonStoreOwnerSnapshotRow person in persons.Rows)
        {
            if (person == null || string.IsNullOrWhiteSpace(person.PersonIdValue)
                || !personIds.Add(person.PersonIdValue)
                || (person.ResidenceSettlementRuntimeId != null
                    && !cityLocationIds.ContainsKey(person.ResidenceSettlementRuntimeId)))
                return false;
            personsById.Add(person.PersonIdValue, person);
            if (!string.IsNullOrWhiteSpace(person.MaterializedNpcRuntimeId)
                && !personByNpcId.TryAdd(person.MaterializedNpcRuntimeId, person.PersonIdValue))
                return false;
        }

        foreach (ParentageRecord edge in genealogy.Records)
        {
            if (edge?.ParentId == null || edge.ChildId == null
                || !personIds.Contains(edge.ParentId.Value)
                || !personIds.Contains(edge.ChildId.Value))
                return false;
        }

        Dictionary<string, P12DNpcDRow> npcsById =
            new Dictionary<string, P12DNpcDRow>(StringComparer.Ordinal);
        foreach (P12DNpcDRow npc in npcRows)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !npcsById.TryAdd(npc.RuntimeId, npc)
                || (npc.CurrentLocationRuntimeId != null && !locationsById.ContainsKey(npc.CurrentLocationRuntimeId))
                || (npc.DestinationLocationRuntimeId != null && !locationsById.ContainsKey(npc.DestinationLocationRuntimeId))
                || (npc.CurrentCityRuntimeId != null && !cityLocationIds.ContainsKey(npc.CurrentCityRuntimeId))
                || (npc.DestinationCityRuntimeId != null && !cityLocationIds.ContainsKey(npc.DestinationCityRuntimeId))
                || (npc.ResidenceSettlementRuntimeId != null && !cityLocationIds.ContainsKey(npc.ResidenceSettlementRuntimeId)))
                return false;
            if (npc.CurrentCityRuntimeId != null
                && (!string.Equals(npc.CurrentLocationRuntimeId,
                    cityLocationIds[npc.CurrentCityRuntimeId], StringComparison.Ordinal)))
                return false;
            if (npc.DestinationCityRuntimeId != null
                && (!string.Equals(npc.DestinationLocationRuntimeId,
                    cityLocationIds[npc.DestinationCityRuntimeId], StringComparison.Ordinal)))
                return false;

            if (npc.PersonIdValue != null)
            {
                if (!personsById.TryGetValue(npc.PersonIdValue, out PersonStoreOwnerSnapshotRow person)
                    || !string.Equals(person.MaterializedNpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal)
                    || !string.Equals(person.ResidenceSettlementRuntimeId,
                        npc.ResidenceSettlementRuntimeId, StringComparison.Ordinal))
                    return false;
            }
            else if (personByNpcId.ContainsKey(npc.RuntimeId))
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, string> binding in personByNpcId)
        {
            if (!npcsById.TryGetValue(binding.Key, out P12DNpcDRow npc)
                || !string.Equals(npc.PersonIdValue, binding.Value, StringComparison.Ordinal))
                return false;
        }

        Dictionary<string, int> expectedMemberships = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (P12DCityRootOwnerSnapshot.StagingCaptureEnvelope capture in cityCaptures)
        {
            P12DCityRootOwnerSnapshot city = capture?.Snapshot;
            if (city?.ImportantNpcRuntimeIds == null
                || !cityLocationIds.TryGetValue(city.CityRuntimeId, out string locationId)
                || !string.Equals(city.LegacyLocationRuntimeId, locationId, StringComparison.Ordinal)
                || !locationsById.ContainsKey(locationId))
                return false;
            HashSet<string> cityMemberIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string npcId in city.ImportantNpcRuntimeIds)
            {
                if (string.IsNullOrWhiteSpace(npcId) || !cityMemberIds.Add(npcId)
                    || !expectedMemberships.TryAdd(npcId, 1)
                    || !npcsById.TryGetValue(npcId, out P12DNpcDRow npc)
                    || !string.Equals(npc.CurrentCityRuntimeId, city.CityRuntimeId, StringComparison.Ordinal)
                    || !string.Equals(npc.CurrentLocationRuntimeId, locationId, StringComparison.Ordinal))
                    return false;
            }
        }

        foreach (P12DNpcDRow npc in npcRows)
        {
            int count = expectedMemberships.TryGetValue(npc.RuntimeId, out int memberCount) ? memberCount : 0;
            if ((npc.CurrentCityRuntimeId == null && count != 0)
                || (npc.CurrentCityRuntimeId != null && count != 1))
                return false;
        }
        return true;
    }

    private static bool TryValidateFinalGraph(
        IReadOnlyList<CityRuntime> cities,
        IReadOnlyList<NpcRuntime> npcs,
        PersonStore persons,
        GenealogyStore genealogy,
        IReadOnlyList<SpatialLocationRuntime> locations,
        SpatialNetworkRuntime spatial,
        IReadOnlyList<P12DCityRootOwnerSnapshot.StagingCaptureEnvelope> cityCaptures,
        IReadOnlyList<P12DNpcDRow> dRows)
    {
        Dictionary<string, CityRuntime> citiesById = cities.ToDictionary(city => city.RuntimeId, StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> npcsById = npcs.ToDictionary(npc => npc.RuntimeId, StringComparer.Ordinal);
        Dictionary<string, SpatialLocationRuntime> locationsById = locations.ToDictionary(location => location.RuntimeId, StringComparer.Ordinal);
        if (cities.Count != cityCaptures.Count || npcs.Count != dRows.Count
            || spatial.Locations.Count() != locations.Count)
            return false;

        for (int i = 0; i < cityCaptures.Count; i++)
        {
            P12DCityRootOwnerSnapshot expected = cityCaptures[i].Snapshot;
            CityRuntime actual = cities[i];
            if (expected == null || actual == null
                || !string.Equals(expected.CityRuntimeId, actual.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(expected.LegacyLocationRuntimeId, actual.Location?.RuntimeId, StringComparison.Ordinal)
                || actual.ImportantNpcRevision != expected.ImportantNpcRevision
                || actual.ImportantNpcs.Count != expected.ImportantNpcRuntimeIds.Count)
                return false;
            for (int member = 0; member < expected.ImportantNpcRuntimeIds.Count; member++)
            {
                if (!npcsById.TryGetValue(expected.ImportantNpcRuntimeIds[member], out NpcRuntime expectedNpc)
                    || !ReferenceEquals(actual.ImportantNpcs[member], expectedNpc)
                    || !ReferenceEquals(expectedNpc.CurrentCity, actual)
                    || !ReferenceEquals(expectedNpc.CurrentLocation, actual.Location))
                    return false;
            }
        }

        foreach (P12DNpcDRow row in dRows)
        {
            if (!npcsById.TryGetValue(row.RuntimeId, out NpcRuntime npc)
                || !ReferenceEquals(npc.CurrentCity,
                    row.CurrentCityRuntimeId == null ? null : citiesById[row.CurrentCityRuntimeId])
                || !ReferenceEquals(npc.CurrentLocation,
                    row.CurrentLocationRuntimeId == null ? null : locationsById[row.CurrentLocationRuntimeId])
                || (row.DestinationCityRuntimeId != null
                    && !ReferenceEquals(npc.DestinationCity, citiesById[row.DestinationCityRuntimeId]))
                || (row.DestinationLocationRuntimeId != null
                    && !ReferenceEquals(npc.DestinationLocation, locationsById[row.DestinationLocationRuntimeId])))
                return false;
            if (row.PersonIdValue != null
                && (!PersonId.TryCreate(row.PersonIdValue, out PersonId personId)
                    || !persons.TryGet(personId, out PersonRuntime person)
                    || !ReferenceEquals(person, npc.BoundPersonRuntime)
                    || !string.Equals(person.MaterializedNpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal)))
                return false;
        }

        HashSet<string> personIds = new HashSet<string>(persons.Persons.Select(person => person.PersonId.Value), StringComparer.Ordinal);
        return genealogy.Records.All(edge => edge != null
            && personIds.Contains(edge.ParentId.Value) && personIds.Contains(edge.ChildId.Value));
    }

    private static bool TryMatch(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        IOwnerSectionCensusProvider provider,
        OwnerSectionRole role)
    {
        OwnerSectionCensusWitness witness;
        try { witness = provider?.GetCurrentCensus(); }
        catch { return false; }
        return witness != null && TryMatch(sections, witness.SectionId, witness.SchemaVersion,
            role, witness.OwnerInstanceIdentity, witness.Cardinality, witness.Revision);
    }

    private static bool TryMatch(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        int schemaVersion,
        OwnerSectionRole role,
        object owner,
        int cardinality,
        long revision)
    {
        if (sections == null || string.IsNullOrWhiteSpace(sectionId) || owner == null) return false;
        OwnerSectionCensusSnapshot match = null;
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (section == null || string.IsNullOrWhiteSpace(section.SectionId) || !ids.Add(section.SectionId))
                return false;
            if (string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
                match = section;
        }
        return match != null && match.SchemaVersion == schemaVersion && match.Role == role
            && ReferenceEquals(match.OwnerInstanceIdentity, owner)
            && match.Cardinality == cardinality && match.Revision == revision;
    }

    private static bool TryBuildUniqueMap<T>(
        IReadOnlyList<T> values,
        Func<T, string> getId,
        out Dictionary<string, T> byId)
    {
        byId = new Dictionary<string, T>(StringComparer.Ordinal);
        if (values == null) return false;
        foreach (T value in values)
        {
            string id = getId(value);
            if (string.IsNullOrWhiteSpace(id) || !byId.TryAdd(id, value))
                return false;
        }
        return true;
    }
}
