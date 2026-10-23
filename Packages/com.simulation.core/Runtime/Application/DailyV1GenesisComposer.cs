using System;
using System.Globalization;

/// <summary>
/// Unpublished Core-owned selected Daily-v1 genesis authorities. The result is
/// a construction value; the application still owns admission, runtime
/// composition, P12 publication, and the single active-session exchange.
/// </summary>
internal sealed class DailyV1GenesisComposition
{
    internal DailyV1EffectiveDefinitions Definitions { get; }
    internal string P9ProfileFingerprint { get; }
    internal System.Collections.Generic.IReadOnlyList<string> P9ProvenanceRecords { get; }
    internal WorldId WorldId { get; }
    internal RuntimeIdAllocator RuntimeIdAllocator { get; }
    internal SimulationTime SimulationTime { get; }
    internal IAuthoritativeRandomSource RandomSource { get; }
    internal SimulationRecordSequence RecordSequence { get; }
    internal NpcDecisionStore DecisionStore { get; }
    internal PersonStore PersonStore { get; }
    internal ActorChoiceStore ActorChoiceStore { get; }
    internal TravelPartyStore TravelPartyStore { get; }
    internal SimulationRuntimeAdmissionContext AdmissionContext { get; }
    internal EffectiveSimulationConfiguration EffectiveConfiguration { get; }
    internal SimulationCalendar Calendar { get; }
    internal DailyV1ExecutionBoundaryState ExecutionBoundaryState { get; }
    internal SpatialAuthorityStore SpatialAuthority { get; }
    internal LegacySpatialAnchorBindingStore LegacySpatialAnchorBindings { get; }
    internal DailyV1SelectedRuntimeOwnerGraph OwnerGraph { get; }

    internal DailyV1GenesisComposition(
        DailyV1EffectiveDefinitions definitions,
        string p9ProfileFingerprint,
        System.Collections.Generic.IReadOnlyList<string> p9ProvenanceRecords,
        WorldId worldId,
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationTime simulationTime,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        TravelPartyStore travelPartyStore,
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration effectiveConfiguration,
        SimulationCalendar calendar,
        DailyV1ExecutionBoundaryState executionBoundaryState,
        SpatialAuthorityStore spatialAuthority,
        LegacySpatialAnchorBindingStore legacySpatialAnchorBindings,
        DailyV1SelectedRuntimeOwnerGraph ownerGraph)
    {
        Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        P9ProfileFingerprint = string.IsNullOrWhiteSpace(p9ProfileFingerprint)
            ? throw new ArgumentException("The P9 profile fingerprint is required.", nameof(p9ProfileFingerprint))
            : p9ProfileFingerprint;
        P9ProvenanceRecords = p9ProvenanceRecords
            ?? throw new ArgumentNullException(nameof(p9ProvenanceRecords));
        WorldId = worldId ?? throw new ArgumentNullException(nameof(worldId));
        RuntimeIdAllocator = runtimeIdAllocator ?? throw new ArgumentNullException(nameof(runtimeIdAllocator));
        SimulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        RandomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        RecordSequence = recordSequence ?? throw new ArgumentNullException(nameof(recordSequence));
        DecisionStore = decisionStore ?? throw new ArgumentNullException(nameof(decisionStore));
        PersonStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
        ActorChoiceStore = actorChoiceStore ?? throw new ArgumentNullException(nameof(actorChoiceStore));
        TravelPartyStore = travelPartyStore ?? throw new ArgumentNullException(nameof(travelPartyStore));
        AdmissionContext = admissionContext ?? throw new ArgumentNullException(nameof(admissionContext));
        EffectiveConfiguration = effectiveConfiguration ?? throw new ArgumentNullException(nameof(effectiveConfiguration));
        Calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        ExecutionBoundaryState = executionBoundaryState
            ?? throw new ArgumentNullException(nameof(executionBoundaryState));
        SpatialAuthority = spatialAuthority ?? throw new ArgumentNullException(nameof(spatialAuthority));
        LegacySpatialAnchorBindings = legacySpatialAnchorBindings
            ?? throw new ArgumentNullException(nameof(legacySpatialAnchorBindings));
        OwnerGraph = ownerGraph ?? throw new ArgumentNullException(nameof(ownerGraph));
        if (!ReferenceEquals(OwnerGraph.Definitions, Definitions))
            throw new ArgumentException("The unpublished owner graph must retain the exact selected definition graph.");
    }
}

/// <summary>
/// Core subroutine for the selected P9 Daily-v1 genesis sequence. It reuses
/// the typed graph's already-admitted stage order and keeps the established
/// allocator order: City/Location, Routes, P8 geography/anchors, NPCs,
/// directives, initial knowledge/Justice, then owner-graph admission. P10
/// site/topology profiles are intentionally not composed by this path.
/// </summary>
internal static class DailyV1GenesisComposer
{
    private const string ResolveProfileStage = "p9.genesis.resolve-profile/v1";
    private const string AuthoredWorldStage = "p9.genesis.authored-world/v1";
    private const string AuthoredActorsStage = "p9.genesis.authored-actors/v1";
    private const string ValidateProfileStage = "p9.genesis.validate-profile/v1";
    private const string PublishStage = "p9.genesis.publish/v1";

    internal static bool TryComposeSelectedOwners(
        DailyV1EffectiveDefinitions definitions,
        SimulationRuntimeAdmissionContext admissionContext,
        out DailyV1GenesisComposition composition,
        out string diagnostic)
    {
        return TryComposeSelectedOwners(
            definitions, admissionContext, null, null, out composition, out diagnostic);
    }

    internal static bool TryComposeSelectedOwners(
        DailyV1EffectiveDefinitions definitions,
        SimulationRuntimeAdmissionContext admissionContext,
        Func<WorldId> allocateWorldId,
        Action<string> stageCompleted,
        out DailyV1GenesisComposition composition,
        out string diagnostic)
    {
        composition = null;
        diagnostic = null;
        if (!TryValidateInput(definitions, admissionContext, out diagnostic)) return false;

        // Complete selected profile identity and provenance before allocating
        // World/runtime owners, matching the existing pre-genesis validation
        // and fingerprint boundary.
        string p9ProfileFingerprint = DailyV1DefinitionIdentity.CreateP9Fingerprint(
            definitions, out System.Collections.Generic.IReadOnlyList<string> p9ProvenanceRecords);

        // The selected runtime's identity and continuation roots are created
        // only after profile admission and before runtime-ID allocation, as in
        // the existing Unity bootstrap sequence.
        DailyV1SimulationConfigData config = definitions.Configuration;
        WorldId worldId = allocateWorldId != null
            ? allocateWorldId()
            : new WorldId(Guid.NewGuid());
        if (worldId == null)
            return Fail("World identity allocation returned no identity.", out diagnostic);
        SimulationTime time = new SimulationTime();
        DeterministicRandomSource randomSource = new DeterministicRandomSource(
            config.useFixedSimulationSeed ? config.simulationSeed : 0);
        SimulationRecordSequence recordSequence = new SimulationRecordSequence();
        NpcDecisionStore decisionStore = new NpcDecisionStore();
        PersonStore personStore = new PersonStore();
        ActorChoiceStore actorChoiceStore = new ActorChoiceStore(personStore);
        RuntimeIdAllocator runtimeIds = new RuntimeIdAllocator();
        CalendarDefinition calendarDefinition = definitions.Calendar.monthLengths != null
            && definitions.Calendar.monthLengths.Count > 0
            ? new CalendarDefinition(
                new System.Collections.Generic.List<int>(definitions.Calendar.monthLengths),
                definitions.Calendar.daysPerWeek)
            : new CalendarDefinition(
                definitions.Calendar.monthsPerYear,
                definitions.Calendar.weeksPerMonth,
                definitions.Calendar.daysPerWeek);
        SimulationCalendar calendar = new SimulationCalendar(calendarDefinition);
        DailyV1ExecutionBoundaryState executionBoundaryState =
            new DailyV1ExecutionBoundaryState(
                time, admissionContext, SimulationRuntimeCompositionProfile.Standard, worldId);
        if (!executionBoundaryState.TryBindRuntimeConfiguration(
                definitions.EffectiveConfiguration, calendar))
            return Fail("The selected Core runtime could not bind its exact effective configuration and calendar.", out diagnostic);
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(time);
        TravelPartyStore travelParties = new TravelPartyStore();
        DailyV1JusticeRecordsOwner justice = new DailyV1JusticeRecordsOwner();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        SpatialAuthorityStore spatialAuthority = new SpatialAuthorityStore();
        LegacySpatialAnchorBindingStore anchorBindings = new LegacySpatialAnchorBindingStore(
            spatialAuthority,
            owner => owner != null
                && owner.Kind == SpatialAnchorOwnerKind.City
                && ContainsCity(builder, owner.Value));

        bool invokingStageCallback = false;
        void CompleteStage(string stageId)
        {
            if (stageCompleted == null) return;
            invokingStageCallback = true;
            try { stageCompleted(stageId); }
            finally { invokingStageCallback = false; }
        }

        try
        {
            CompleteStage(ResolveProfileStage);
        }
        catch
        {
            throw;
        }

        try
        {
            // Match SimulationGenesisPipeline.ResolveStageOrder and the Unity
            // bootstrap allocator timing. P8 consumes typed authored values
            // after route IDs have been allocated and before any NPC ID.
            foreach (string stage in definitions.StageOrder)
            {
                if (string.Equals(stage, ResolveProfileStage, StringComparison.Ordinal)
                    || string.Equals(stage, PublishStage, StringComparison.Ordinal))
                    continue;

                if (string.Equals(stage, AuthoredWorldStage, StringComparison.Ordinal))
                {
                    if (!builder.TryCreateConfiguredCities(runtimeIds)
                        || !builder.TryCreateConfiguredCityRoutes(runtimeIds))
                        return Fail("Core selected City/Route genesis rejected the admitted definition graph.", out diagnostic);
                    CompleteStage(stage);
                    continue;
                }

                if (string.Equals(stage, definitions.GeographyStageIdentity, StringComparison.Ordinal))
                {
                    if (!TryComposeAuthoredGeography(definitions, spatialAuthority, out diagnostic))
                        return false;
                    if (!TryBindSelectedCityAnchors(builder, anchorBindings, out diagnostic))
                        return false;
                    CompleteStage(stage);
                    continue;
                }

                if (string.Equals(stage, AuthoredActorsStage, StringComparison.Ordinal))
                {
                    if (!builder.TryCreateConfiguredNpcs(runtimeIds, null)
                        || !builder.TryCreateConfiguredScheduledDirectives(runtimeIds, directives)
                        || !builder.TryBootstrapInitialSpatialKnowledge()
                        || !builder.TryBootstrapInitialCommercialKnowledge(time.AbsoluteDay)
                        || !builder.TryCreateConfiguredInitialJusticeState(justice))
                        return Fail("Core selected NPC/directive/initial-owner genesis rejected the admitted definition graph.", out diagnostic);
                    CompleteStage(stage);
                    continue;
                }

                if (string.Equals(stage, ValidateProfileStage, StringComparison.Ordinal))
                {
                    if (!builder.TryBuild(justice, travelParties, directives,
                            out DailyV1SelectedRuntimeOwnerGraph graph))
                        return Fail("Core selected owner graph failed exact genesis admission.", out diagnostic);
                    if (!graph.TryBindPersonStore(personStore)
                        || !ReferenceEquals(actorChoiceStore.PersonStore, personStore)
                        || !executionBoundaryState.TryBindSelectedRuntimeOwnerGraph(graph)
                        || !executionBoundaryState.TryBindInitialSelectedRuntimeActorRosterFromOwnerGraph()
                        || !executionBoundaryState.TryBindSelectedRuntimeExecutionRoots(
                            runtimeIds,
                            randomSource,
                            recordSequence,
                            decisionStore,
                            actorChoiceStore))
                        return Fail("The selected Core runtime could not bind its exact genesis owner graph.", out diagnostic);
                    composition = new DailyV1GenesisComposition(
                        definitions, p9ProfileFingerprint, p9ProvenanceRecords,
                        worldId, runtimeIds, time, randomSource, recordSequence,
                        decisionStore, personStore, actorChoiceStore, travelParties,
                        admissionContext, definitions.EffectiveConfiguration,
                        calendar, executionBoundaryState, spatialAuthority, anchorBindings, graph);
                    continue;
                }

                return Fail("The selected Core genesis path does not own stage '" + stage + "'.", out diagnostic);
            }
        }
        catch (FormatException exception)
            when (!invokingStageCallback)
        {
            return Fail("Authored P8 scale value is invalid: " + exception.Message, out diagnostic);
        }
        catch (OverflowException exception)
            when (!invokingStageCallback)
        {
            return Fail("Selected Core genesis numeric input overflowed: " + exception.Message, out diagnostic);
        }
        catch (ArgumentException exception)
            when (!invokingStageCallback)
        {
            return Fail("Selected Core genesis input is invalid: " + exception.Message, out diagnostic);
        }
        catch (InvalidOperationException exception)
            when (!invokingStageCallback)
        {
            return Fail("Selected Core genesis failed: " + exception.Message, out diagnostic);
        }

        if (composition == null)
            return Fail("Selected Core genesis did not reach its graph-admission stage.", out diagnostic);
        return true;
    }

    private static bool TryValidateInput(
        DailyV1EffectiveDefinitions definitions,
        SimulationRuntimeAdmissionContext admissionContext,
        out string diagnostic)
    {
        diagnostic = null;
        if (definitions == null || definitions.Configuration == null
            || definitions.EffectiveConfiguration == null || definitions.Calendar == null
            || admissionContext == null
            || admissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || admissionContext.IsRestoredContinuation
            || !admissionContext.IsOwnedByCurrentThread())
            return Fail("Selected Daily-v1 definitions and a current-thread bootstrap admission context are required.", out diagnostic);

        DailyV1SimulationConfigData config = definitions.Configuration;
        if ((config.explorableSites?.Count ?? 0) != 0
            || definitions.ExplorableSites.Count != 0
            || !string.IsNullOrWhiteSpace(config.authoredP10RuinSite)
            || !string.IsNullOrWhiteSpace(config.p10bStableSiteKey))
            return Fail("The selected Core Daily-v1 genesis path requires the admitted exact-empty ExplorableSite/P10 topology profile.", out diagnostic);

        if (definitions.StageOrder == null || definitions.StageOrder.Count < 5
            || !string.Equals(definitions.StageOrder[0], ResolveProfileStage, StringComparison.Ordinal)
            || !string.Equals(definitions.StageOrder[1], AuthoredWorldStage, StringComparison.Ordinal)
            || !string.Equals(definitions.StageOrder[definitions.StageOrder.Count - 2], ValidateProfileStage, StringComparison.Ordinal)
            || !string.Equals(definitions.StageOrder[definitions.StageOrder.Count - 1], PublishStage, StringComparison.Ordinal))
            return Fail("The admitted Daily-v1 genesis stage order does not match the selected P9 contract.", out diagnostic);

        int geographyIndex = -1;
        for (int index = 0; index < definitions.StageOrder.Count; index++)
            if (string.Equals(definitions.StageOrder[index], definitions.GeographyStageIdentity, StringComparison.Ordinal))
            {
                if (geographyIndex >= 0) return Fail("Authored geography stage is duplicated.", out diagnostic);
                geographyIndex = index;
            }

        bool geographyEnabled = config.useAuthoredGeographyProfile;
        if (geographyEnabled != (geographyIndex >= 0)
            || definitions.StageOrder.Count != (geographyEnabled ? 6 : 5)
            || (geographyEnabled && geographyIndex != 2)
            || !string.Equals(definitions.StageOrder[geographyEnabled ? 3 : 2], AuthoredActorsStage, StringComparison.Ordinal))
            return Fail("The admitted Daily-v1 stage order is not the exact selected P9 sequence.", out diagnostic);
        return true;
    }

    private static bool TryComposeAuthoredGeography(
        DailyV1EffectiveDefinitions definitions,
        SpatialAuthorityStore spatialAuthority,
        out string diagnostic)
    {
        diagnostic = null;
        DailyV1SimulationConfigData config = definitions.Configuration;
        if (!config.useAuthoredGeographyProfile) return true;
        if (string.IsNullOrWhiteSpace(config.authoredHexId)
            || string.IsNullOrWhiteSpace(config.authoredTerrainDefinitionId)
            || string.IsNullOrWhiteSpace(config.authoredTerrainRevisionToken)
            || string.IsNullOrWhiteSpace(config.authoredLocationId)
            || string.IsNullOrWhiteSpace(config.authoredScaleConventionId)
            || string.IsNullOrWhiteSpace(config.authoredScaleSourceIdentity)
            || string.IsNullOrWhiteSpace(config.authoredScaleSourceVersion)
            || string.IsNullOrWhiteSpace(config.authoredDistancePerNeighborStep)
            || string.IsNullOrWhiteSpace(config.authoredScaleUnit))
            return Fail("The typed authored geography profile is incomplete.", out diagnostic);

        decimal scale = decimal.Parse(config.authoredDistancePerNeighborStep,
            NumberStyles.Number, CultureInfo.InvariantCulture);
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext(config.authoredScaleConventionId,
                config.authoredScaleSourceIdentity, config.authoredScaleSourceVersion,
                scale, config.authoredScaleUnit),
            new[]
            {
                new HexRecord(new HexId(config.authoredHexId),
                    new HexCoordinate(config.authoredHexQ, config.authoredHexR),
                    new TerrainReference(new TerrainDefinitionId(config.authoredTerrainDefinitionId),
                        config.authoredTerrainRevisionToken))
            },
            new[] { new LocationRecord(new LocationId(config.authoredLocationId), new HexId(config.authoredHexId)) });
        if (!spatialAuthority.TryComposeGeography(geography, out SpatialAuthorityFailure failure))
            return Fail("Authored P8 geography failed atomic Core composition: " + failure, out diagnostic);
        return true;
    }

    private static bool TryBindSelectedCityAnchors(
        DailyV1SelectedRuntimeOwnerGraphBuilder builder,
        LegacySpatialAnchorBindingStore anchorBindings,
        out string diagnostic)
    {
        diagnostic = null;
        foreach (DailyV1CityRuntimeOwners city in builder.Cities)
        {
            if (city == null || !city.HasLocalDailyMaterialFlow
                || string.IsNullOrWhiteSpace(city.Definition?.Values?.materialFlowLocationId))
                continue;
            if (!anchorBindings.TryBindCity(city.RuntimeId,
                    new LocationId(city.Definition.Values.materialFlowLocationId),
                    out SpatialAnchorBindingFailure failure))
                return Fail("Selected City P8 anchor binding failed: " + failure, out diagnostic);
        }
        return true;
    }

    private static bool ContainsCity(DailyV1SelectedRuntimeOwnerGraphBuilder builder, string runtimeId)
    {
        foreach (DailyV1CityRuntimeOwners city in builder.Cities)
            if (string.Equals(city?.RuntimeId, runtimeId, StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool Fail(string message, out string diagnostic)
    {
        diagnostic = message;
        return false;
    }
}
