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
    internal IReadOnlyList<DailyV1CityRuntimeOwners> SelectedCityOwners { get; }
    internal IReadOnlyList<DailyV1NpcRuntimeOwners> SelectedNpcOwners { get; }
    internal IReadOnlyList<P12DNpcFRow> NpcFRows { get; }
    internal IReadOnlyList<P12DNpcDRow> NpcDRows { get; }
    internal IReadOnlyList<P12DCityRootOwnerSnapshot> CitySnapshots { get; }
    internal SpatialNetworkOwnerSnapshot SpatialNetworkSnapshot { get; }
    internal PersonStoreOwnerSnapshot PersonSnapshot { get; }
    internal GenealogyOwnerSnapshot GenealogySnapshot { get; }
    internal ExplorableSiteOwnerSnapshot ExplorableSiteSnapshot { get; }
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
        IReadOnlyList<DailyV1CityRuntimeOwners> selectedCityOwners,
        IReadOnlyList<DailyV1NpcRuntimeOwners> selectedNpcOwners,
        IReadOnlyList<P12DCityRootOwnerSnapshot> citySnapshots,
        IReadOnlyList<P12DNpcDRow> npcDRows,
        IReadOnlyList<P12DNpcFRow> npcFRows,
        SpatialNetworkOwnerSnapshot spatialNetworkSnapshot,
        PersonStoreOwnerSnapshot personSnapshot,
        GenealogyOwnerSnapshot genealogySnapshot,
        ExplorableSiteOwnerSnapshot explorableSiteSnapshot,
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
        SelectedCityOwners = new ReadOnlyCollection<DailyV1CityRuntimeOwners>(
            new List<DailyV1CityRuntimeOwners>(selectedCityOwners));
        SelectedNpcOwners = new ReadOnlyCollection<DailyV1NpcRuntimeOwners>(
            new List<DailyV1NpcRuntimeOwners>(selectedNpcOwners));
        CitySnapshots = new ReadOnlyCollection<P12DCityRootOwnerSnapshot>(new List<P12DCityRootOwnerSnapshot>(citySnapshots));
        NpcDRows = new ReadOnlyCollection<P12DNpcDRow>(new List<P12DNpcDRow>(npcDRows));
        NpcFRows = new ReadOnlyCollection<P12DNpcFRow>(new List<P12DNpcFRow>(npcFRows));
        SpatialNetworkSnapshot = spatialNetworkSnapshot;
        PersonSnapshot = personSnapshot;
        GenealogySnapshot = genealogySnapshot;
        ExplorableSiteSnapshot = explorableSiteSnapshot;
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
        IReadOnlyList<DailyV1ExplorableSiteDefinition> admittedDailySiteDefinitions,
        IReadOnlyList<string> stagedTravelPartyIds,
        out P12DDailyV1OwnerPackage package,
        out P12DDailyV1OwnerPackageFailure failure,
        Action<P12GDailyV1RestoreStage> stageObserver = null,
        Action afterGenealogyRecordStagedForTest = null,
        Func<GenealogyOwnerSnapshot, GenealogyOwnerSnapshot> genealogySnapshotOverrideForTest = null,
        DailyV1EffectiveDefinitions selectedDefinitions = null,
        Action<P12DNpcRootOwnerSnapshotFailure> npcStageFailureObserver = null)
    {
        package = null;
        failure = P12DDailyV1OwnerPackageFailure.InvalidCaptureContext;
        if (sourceRuntime == null || token == null || stagingAttempt == null
            || exactOwnerSectionVector == null || sourceRuntimeIdentities == null
            || sourceSpatialNetwork == null || sourceExplorableSites == null
            || stagedRuntimeIdentities == null || stagedWorldId == null
            || (selectedDefinitions == null && cityDefinitions == null) || itemDefinitions == null
            || npcDefinitions == null || actionDefinitions == null
            || statusDefinitions == null || admittedDailySiteDefinitions == null
            || stagedTravelPartyIds == null
            || !ReferenceEquals(token.OwnerSections, exactOwnerSectionVector)
            || !stagingAttempt.IsCurrentFor(sourceRuntime, token, exactOwnerSectionVector)
            || !ReferenceEquals(token.WorldId, sourceRuntime.WorldId)
            || !token.WorldId.Equals(stagedWorldId)
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
                out _, stagingAttempt)
            || dProjection == null || fProjection == null
            || dProjection.Rows.Count != sourceRuntime.NpcRuntimes.Count
            || fProjection.Rows.Count != sourceRuntime.NpcRuntimes.Count)
        {
            failure = P12DDailyV1OwnerPackageFailure.InvalidOwnerSnapshot;
            return false;
        }

        PersonStoreOwnerSnapshot personSnapshot = sourcePersons.CaptureOwnerSnapshot();
        GenealogyOwnerSnapshot genealogySnapshot = new GenealogyOwnerSnapshot(
            GenealogyOwnerBridge.CaptureOwnerSnapshot(sourceGenealogy));
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

        if (!stagingAttempt.IsCurrentFor(sourceRuntime, token, exactOwnerSectionVector)
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

        if (genealogySnapshotOverrideForTest != null)
            genealogySnapshot = genealogySnapshotOverrideForTest(genealogySnapshot);

        stageObserver?.Invoke(P12GDailyV1RestoreStage.DGenealogyHydratorEntry);
        if (!GenealogyOwnerBridge.TryCreateStagedOwner(
                genealogySnapshot.Export, out GenealogyStore stagedGenealogy, out _,
                afterGenealogyRecordStagedForTest)
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
        if ((selectedDefinitions == null
                && !TryBuildUniqueMap(cityDefinitions, definition => definition?.DefinitionId, out _))
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
                    out P12DCityMembershipLinker linker, out _, selectedDefinitions)
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
                personSnapshot, genealogySnapshot, cityCaptures.Select(capture => capture.Snapshot).ToArray(),
                dProjection.Rows, locationsById, stagedCityLocationIds))
        {
            failure = P12DDailyV1OwnerPackageFailure.InvalidRelation;
            return false;
        }
        stageObserver?.Invoke(P12GDailyV1RestoreStage.DRelationsValidated);

        bool npcStageSucceeded = P12DNpcRootOwnerSnapshot.TryStage(
                sourceRuntime, token, dProjection, fProjection, cityLinkers, stagedPersons,
                npcDefinitions, actionDefinitions, statusDefinitions, itemDefinitions,
                stagedLocations, stagedRouteIds, stagedTravelPartyIds,
                out IReadOnlyList<NpcRuntime> stagedNpcRoster,
                out P12DNpcRootOwnerSnapshotFailure npcStageFailure,
                selectedDefinitions,
                stagingAttempt);
        if (!npcStageSucceeded || stagedNpcRoster == null)
        {
            npcStageFailureObserver?.Invoke(npcStageFailure);
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
            || !stagingAttempt.IsCurrentFor(sourceRuntime, token, exactOwnerSectionVector)
            || !TryValidateFinalGraph(
                stagedCities, stagedNpcRoster, stagedPersons, stagedGenealogy,
                stagedLocations, stagedSpatialNetwork,
                cityCaptures.Select(capture => capture.Snapshot).ToArray(), dProjection.Rows))
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

        if (!TryCollectSelectedOwners(selectedDefinitions, stagedCities, stagedNpcRoster,
                out IReadOnlyList<DailyV1CityRuntimeOwners> selectedCityOwners,
                out IReadOnlyList<DailyV1NpcRuntimeOwners> selectedNpcOwners))
        {
            failure = P12DDailyV1OwnerPackageFailure.InvalidRelation;
            return false;
        }

        package = new P12DDailyV1OwnerPackage(
            stagingAttempt,
            stagedWorldId, stagedRuntimeIdentities, stagedSpatialNetwork,
            stagedCities, stagedNpcRoster, selectedCityOwners, selectedNpcOwners,
            cityCaptures.Select(capture => capture.Snapshot).ToArray(),
            dProjection.Rows, fProjection.Rows,
            spatialSnapshot, personSnapshot, genealogySnapshot, siteSnapshot,
            stagedPersons, stagedGenealogy, stagedSites);
        failure = P12DDailyV1OwnerPackageFailure.None;
        return true;
    }

    internal static bool TryStageFromImage(
        IReadOnlyList<P12AOwnerSection> ownerSections,
        IReadOnlyList<P12DCityRootOwnerSnapshot> citySnapshots,
        IReadOnlyList<P12DNpcDRow> npcDRows,
        IReadOnlyList<P12DNpcFRow> npcFRows,
        SpatialNetworkOwnerSnapshot spatialSnapshot,
        PersonStoreOwnerSnapshot personSnapshot,
        GenealogyOwnerSnapshot genealogySnapshot,
        ExplorableSiteOwnerSnapshot siteSnapshot,
        RuntimeIdentityRegistry stagedRuntimeIdentities,
        WorldId stagedWorldId,
        IReadOnlyList<CityData> cityDefinitions,
        IReadOnlyList<ItemData> itemDefinitions,
        IReadOnlyList<NpcData> npcDefinitions,
        IReadOnlyList<NpcActionData> actionDefinitions,
        IReadOnlyList<NpcStatusData> statusDefinitions,
        IReadOnlyList<DailyV1ExplorableSiteDefinition> admittedDailySiteDefinitions,
        IReadOnlyList<string> stagedTravelPartyIds,
        out P12DDailyV1OwnerPackage package,
        out P12DDailyV1OwnerPackageFailure failure,
        DailyV1EffectiveDefinitions selectedDefinitions = null)
    {
        package = null;
        failure = P12DDailyV1OwnerPackageFailure.InvalidCaptureContext;
        if (ownerSections == null || citySnapshots == null || citySnapshots.Count == 0
            || npcDRows == null || npcFRows == null || spatialSnapshot == null
            || personSnapshot == null || genealogySnapshot == null || siteSnapshot == null
            || stagedRuntimeIdentities == null || stagedWorldId == null
            || (selectedDefinitions == null && cityDefinitions == null)
            || itemDefinitions == null || npcDefinitions == null
            || actionDefinitions == null || statusDefinitions == null || admittedDailySiteDefinitions == null
            || admittedDailySiteDefinitions.Count != 0 || stagedTravelPartyIds == null
            || !TryValidateStagedIdentityRegistryIsEmpty(stagedRuntimeIdentities)
            || siteSnapshot.Sites == null || siteSnapshot.Sites.Count != 0 || siteSnapshot.Revision != 0L
            || npcDRows.Count != npcFRows.Count)
            return false;

        if (!SpatialNetworkRuntime.TryCreateFromOwnerSnapshot(spatialSnapshot,
                stagedRuntimeIdentities, out SpatialNetworkRuntime stagedSpatialNetwork, out _)
            || stagedSpatialNetwork == null
            || !PersonStore.TryCreateFromOwnerSnapshot(personSnapshot,
                out PersonStore stagedPersons, out _) || stagedPersons == null
            || !GenealogyOwnerBridge.TryCreateStagedOwner(genealogySnapshot.Export,
                out GenealogyStore stagedGenealogy, out _) || stagedGenealogy == null)
        { failure = P12DDailyV1OwnerPackageFailure.StageFailed; return false; }

        SpatialLocationRuntime[] stagedLocations = stagedSpatialNetwork.Locations.ToArray();
        string[] stagedRouteIds = stagedSpatialNetwork.Routes.Select(route => route?.RuntimeId).ToArray();
        Dictionary<string, SpatialLocationRuntime> locationsById =
            stagedLocations.ToDictionary(location => location.RuntimeId, StringComparer.Ordinal);
        if ((selectedDefinitions == null
                && !TryBuildUniqueMap(cityDefinitions, definition => definition?.DefinitionId, out _))
            || !TryBuildUniqueMap(itemDefinitions, definition => definition?.DefinitionId, out _))
        { failure = P12DDailyV1OwnerPackageFailure.InvalidDefinition; return false; }

        List<CityRuntime> stagedCities = new List<CityRuntime>(citySnapshots.Count);
        List<P12DCityMembershipLinker> cityLinkers = new List<P12DCityMembershipLinker>(citySnapshots.Count);
        List<DailyV1P12DCityCoreOwnerHydrationInput> selectedCityInputs =
            selectedDefinitions == null ? null : new List<DailyV1P12DCityCoreOwnerHydrationInput>(citySnapshots.Count);
        Dictionary<string, string> cityLocationIds = new Dictionary<string, string>(StringComparer.Ordinal);
        HashSet<string> cityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (P12DCityRootOwnerSnapshot snapshot in citySnapshots)
        {
            if (snapshot == null || snapshot.SchemaVersion != P12DCityRootOwnerSnapshot.CurrentSchemaVersion
                || snapshot.ImportantNpcRuntimeIds == null || snapshot.MarketItems == null
                || snapshot.PopulationReceipts == null || !cityIds.Add(snapshot.CityRuntimeId)
                || !locationsById.TryGetValue(snapshot.LegacyLocationRuntimeId,
                    out SpatialLocationRuntime location)
                || !stagedRuntimeIdentities.IsRuntimeIdAvailable(snapshot.CityRuntimeId))
            { failure = P12DDailyV1OwnerPackageFailure.InvalidIdentityRoot; return false; }
            cityLocationIds.Add(snapshot.CityRuntimeId, snapshot.LegacyLocationRuntimeId);
            if (selectedCityInputs != null)
                selectedCityInputs.Add(snapshot.CreateSelectedCoreHydrationInput(location.RuntimeId));
        }

        DailyV1P12DSelectedCoreOwnerStage selectedCoreOwnerStage = null;
        if (selectedDefinitions != null)
        {
            if (npcDRows.Count != npcFRows.Count)
            { failure = P12DDailyV1OwnerPackageFailure.InvalidOwnerSnapshot; return false; }

            List<DailyV1P12DNpcCoreOwnerHydrationInput> selectedNpcInputs =
                new List<DailyV1P12DNpcCoreOwnerHydrationInput>(npcDRows.Count);
            for (int i = 0; i < npcDRows.Count; i++)
            {
                if (!P12DNpcRootOwnerSnapshot.TryCreateSelectedCoreHydrationInput(
                        npcDRows[i], npcFRows[i], out DailyV1P12DNpcCoreOwnerHydrationInput input)
                    || input == null)
                { failure = P12DDailyV1OwnerPackageFailure.InvalidOwnerSnapshot; return false; }
                selectedNpcInputs.Add(input);
            }

            if (!DailyV1P12DSelectedCoreOwnerStager.TryCreate(
                    selectedDefinitions, selectedCityInputs, selectedNpcInputs,
                    stagedSpatialNetwork.SelectedOwner, stagedTravelPartyIds,
                    out selectedCoreOwnerStage, out _)
                || selectedCoreOwnerStage == null)
            { failure = P12DDailyV1OwnerPackageFailure.InvalidRelation; return false; }
        }

        foreach (P12DCityRootOwnerSnapshot snapshot in citySnapshots)
        {
            DailyV1CityRuntimeOwners selectedOwner = null;
            if (selectedCoreOwnerStage != null
                && !selectedCoreOwnerStage.TryGetCity(snapshot.CityRuntimeId, out selectedOwner))
            { failure = P12DDailyV1OwnerPackageFailure.InvalidRelation; return false; }

            if (!snapshot.TryStageForImage(cityDefinitions, itemDefinitions, stagedLocations,
                    out CityRuntime city, out P12DCityMembershipLinker linker, out _, selectedDefinitions,
                    selectedOwner)
                || city == null || linker == null || !stagedRuntimeIdentities.RegisterCity(city))
            { failure = P12DDailyV1OwnerPackageFailure.StageFailed; return false; }
            stagedCities.Add(city);
            cityLinkers.Add(linker);
        }

        if (!TryPreflightRelations(personSnapshot, genealogySnapshot, citySnapshots,
                npcDRows, locationsById, cityLocationIds))
        { failure = P12DDailyV1OwnerPackageFailure.InvalidRelation; return false; }

        Dictionary<string, DailyV1NpcRuntimeOwners> stagedSelectedNpcOwners = selectedCoreOwnerStage == null
            ? null
            : selectedCoreOwnerStage.Npcs.ToDictionary(owner => owner.RuntimeId, StringComparer.Ordinal);
        if (!P12DNpcRootOwnerSnapshot.TryStageForImage(npcDRows, npcFRows, cityLinkers,
                stagedPersons, npcDefinitions, actionDefinitions, statusDefinitions, itemDefinitions,
                stagedLocations, stagedRouteIds, stagedTravelPartyIds, ownerSections,
                out IReadOnlyList<NpcRuntime> stagedNpcRoster, out _, selectedDefinitions,
                stagedSelectedNpcOwners)
            || stagedNpcRoster == null)
        { failure = P12DDailyV1OwnerPackageFailure.StageFailed; return false; }

        foreach (NpcRuntime npc in stagedNpcRoster)
            if (!stagedRuntimeIdentities.RegisterNpc(npc))
            { failure = P12DDailyV1OwnerPackageFailure.InvalidIdentityRoot; return false; }

        if (!ExplorableSiteStore.TryCreateFromOwnerSnapshot(siteSnapshot, admittedDailySiteDefinitions,
                stagedLocations, out ExplorableSiteStore stagedSites, out _)
            || stagedSites == null || stagedSites.Count != 0 || stagedSites.Revision != 0L
            || !TryValidateFinalGraph(stagedCities, stagedNpcRoster, stagedPersons, stagedGenealogy,
                stagedLocations, stagedSpatialNetwork, citySnapshots, npcDRows))
        { failure = P12DDailyV1OwnerPackageFailure.InvalidRelation; return false; }

        if (!TryCollectSelectedOwners(selectedDefinitions, stagedCities, stagedNpcRoster,
                out IReadOnlyList<DailyV1CityRuntimeOwners> selectedCityOwners,
                out IReadOnlyList<DailyV1NpcRuntimeOwners> selectedNpcOwners))
        { failure = P12DDailyV1OwnerPackageFailure.InvalidRelation; return false; }

        package = new P12DDailyV1OwnerPackage(null, stagedWorldId, stagedRuntimeIdentities,
            stagedSpatialNetwork, stagedCities, stagedNpcRoster, selectedCityOwners, selectedNpcOwners,
            citySnapshots, npcDRows, npcFRows,
            spatialSnapshot, personSnapshot, genealogySnapshot, siteSnapshot,
            stagedPersons, stagedGenealogy, stagedSites);
        failure = P12DDailyV1OwnerPackageFailure.None;
        return true;
    }

    private static bool TryCollectSelectedOwners(
        DailyV1EffectiveDefinitions selectedDefinitions,
        IReadOnlyList<CityRuntime> cities,
        IReadOnlyList<NpcRuntime> npcs,
        out IReadOnlyList<DailyV1CityRuntimeOwners> selectedCityOwners,
        out IReadOnlyList<DailyV1NpcRuntimeOwners> selectedNpcOwners)
    {
        selectedCityOwners = Array.Empty<DailyV1CityRuntimeOwners>();
        selectedNpcOwners = Array.Empty<DailyV1NpcRuntimeOwners>();
        if (selectedDefinitions == null) return true;
        if (cities == null || npcs == null || (cities.Count == 0 && npcs.Count == 0)) return false;

        List<DailyV1CityRuntimeOwners> cityOwners = new List<DailyV1CityRuntimeOwners>(cities.Count);
        foreach (CityRuntime city in cities)
        {
            DailyV1CityRuntimeOwners owner = city?.SelectedRuntimeOwners;
            if (owner == null || !ReferenceEquals(owner.Definitions, selectedDefinitions)) return false;
            cityOwners.Add(owner);
        }

        List<DailyV1NpcRuntimeOwners> npcOwners = new List<DailyV1NpcRuntimeOwners>(npcs.Count);
        foreach (NpcRuntime npc in npcs)
        {
            DailyV1NpcRuntimeOwners owner = npc?.SelectedRuntimeOwners;
            if (owner == null || !ReferenceEquals(owner.Definitions, selectedDefinitions)) return false;
            npcOwners.Add(owner);
        }

        selectedCityOwners = cityOwners.AsReadOnly();
        selectedNpcOwners = npcOwners.AsReadOnly();
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
                    source.CoreIdentityIndex, expectedWitness.Cardinality, expectedWitness.Revision))
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
        if (cityCaptures == null) return false;
        return TryPreflightRelations(persons, genealogy,
            cityCaptures.Select(capture => capture?.Snapshot).ToArray(), npcRows,
            locationsById, cityLocationIds);
    }

    internal static bool TryPreflightRelations(
        PersonStoreOwnerSnapshot persons,
        GenealogyOwnerSnapshot genealogy,
        IReadOnlyList<P12DCityRootOwnerSnapshot> citySnapshots,
        IReadOnlyList<P12DNpcDRow> npcRows,
        IReadOnlyDictionary<string, SpatialLocationRuntime> locationsById,
        IReadOnlyDictionary<string, string> cityLocationIds)
    {
        if (persons?.Rows == null || genealogy?.Records == null || citySnapshots == null
            || npcRows == null || locationsById == null || cityLocationIds == null)
            return false;

        DailyV1P12DPersonRelation[] personRelations = persons.Rows
            .Select(person => person == null
                ? null
                : new DailyV1P12DPersonRelation(
                    person.PersonIdValue, person.ResidenceSettlementRuntimeId, person.MaterializedNpcRuntimeId))
            .ToArray();
        DailyV1P12DParentageRelation[] genealogyRelations = genealogy.Records
            .Select(edge => edge == null
                ? null
                : new DailyV1P12DParentageRelation(edge.ParentId?.Value, edge.ChildId?.Value))
            .ToArray();
        DailyV1P12DCityRelation[] cityRelations = citySnapshots
            .Select(city => city == null
                ? null
                : new DailyV1P12DCityRelation(
                    city.CityRuntimeId, city.LegacyLocationRuntimeId, city.ImportantNpcRuntimeIds))
            .ToArray();
        DailyV1P12DNpcRelation[] npcRelations = npcRows
            .Select(npc => npc == null
                ? null
                : new DailyV1P12DNpcRelation(
                    npc.RuntimeId, npc.PersonIdValue, npc.ResidenceSettlementRuntimeId,
                    npc.CurrentCityRuntimeId, npc.CurrentLocationRuntimeId,
                    npc.DestinationCityRuntimeId, npc.DestinationLocationRuntimeId))
            .ToArray();

        return DailyV1P12DRelationshipValidator.TryValidate(
            personRelations, genealogyRelations, cityRelations, npcRelations,
            locationsById.Keys.ToArray(), cityLocationIds);
    }
    private static bool TryValidateFinalGraph(
        IReadOnlyList<CityRuntime> cities,
        IReadOnlyList<NpcRuntime> npcs,
        PersonStore persons,
        GenealogyStore genealogy,
        IReadOnlyList<SpatialLocationRuntime> locations,
        SpatialNetworkRuntime spatial,
        IReadOnlyList<P12DCityRootOwnerSnapshot> citySnapshots,
        IReadOnlyList<P12DNpcDRow> dRows)
    {
        Dictionary<string, CityRuntime> citiesById = cities.ToDictionary(city => city.RuntimeId, StringComparer.Ordinal);
        Dictionary<string, NpcRuntime> npcsById = npcs.ToDictionary(npc => npc.RuntimeId, StringComparer.Ordinal);
        Dictionary<string, SpatialLocationRuntime> locationsById = locations.ToDictionary(location => location.RuntimeId, StringComparer.Ordinal);
        if (cities.Count != citySnapshots.Count || npcs.Count != dRows.Count
            || spatial.Locations.Count() != locations.Count)
            return false;

        for (int i = 0; i < citySnapshots.Count; i++)
        {
            P12DCityRootOwnerSnapshot expected = citySnapshots[i];
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
