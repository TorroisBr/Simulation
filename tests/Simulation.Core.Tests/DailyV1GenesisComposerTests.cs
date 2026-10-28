using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1GenesisComposerTests
{
    [Test]
    public void SelectedGenesisUsesSuppliedWorldIdentityAndReportsOwnedStagesInOrder()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        WorldId expectedWorldId = new WorldId(Guid.NewGuid());
        int allocations = 0;
        var completed = new System.Collections.Generic.List<string>();

        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            () => { allocations++; return expectedWorldId; },
            completed.Add,
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(allocations, Is.EqualTo(1));
        Assert.That(composition.WorldId, Is.SameAs(expectedWorldId));
        Assert.That(composition.OwnerGraph.Expeditions, Is.SameAs(composition.Expeditions),
            "The Core graph must retain the exact ExpeditionStore created by genesis.");
        Assert.That(composition.OwnerGraph.TryBindExpeditionStore(new ExpeditionStore()), Is.False,
            "A different ExpeditionStore cannot replace the genesis owner identity.");
        Assert.That(composition.P9ManifestSnapshot, Is.Not.Null,
            "Core genesis retains the exact P9-B manifest facts needed by detached P12-C capture.");
        Assert.That(composition.P9ManifestSnapshot.TryValidate(out string p9Diagnostic), Is.True, p9Diagnostic);
        Assert.That(composition.P9ManifestSnapshot.Fingerprint, Is.EqualTo(composition.P9ProfileFingerprint));
        Assert.That(composition.P9ManifestSnapshot.CanonicalProvenanceRecords,
            Is.EqualTo(composition.P9ProvenanceRecords));
        Assert.That(composition.P9ManifestSnapshot.EffectiveConfiguration, Is.Not.Null);
        Assert.That(composition.P9ManifestSnapshot.StageOrder, Is.EqualTo(definitions.StageOrder));
        Assert.That(completed, Is.EqualTo(definitions.StageOrder.Take(definitions.StageOrder.Count - 2)));
    }

    [Test]
    public void RestoredExecutionBoundaryIsCoreComposedFromExactStagedOwnersAndRoots()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext bootstrapAdmission), Is.True);
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            bootstrapAdmission,
            out DailyV1GenesisComposition source,
            out string sourceDiagnostic), Is.True, sourceDiagnostic);

        Assert.That(source.OwnerGraph.TryBindExpeditionStore(new ExpeditionStore()), Is.False,
            "A restored graph cannot replace the exact ExpeditionStore retained by Core genesis.");
        SimulationRuntimeAdmissionContext restoredAdmission =
            source.AdmissionContext.CreateRestoredContinuationContext();
        SimulationTime restoredTime = new SimulationTime(source.SimulationTime.AbsoluteDay);

        Assert.That(DailyV1RestoredExecutionBoundaryComposer.TryCreate(
            definitions, source.EffectiveConfiguration, source.Calendar, restoredTime,
            restoredAdmission, source.WorldId, source.OwnerGraph,
            source.RuntimeIdAllocator, source.RandomSource, source.RecordSequence,
            source.DecisionStore, new ActorChoiceStore(new PersonStore()),
            out _, out string rejectedDiagnostic), Is.False);
        Assert.That(rejectedDiagnostic, Does.Contain("exact admitted definitions"));

        Assert.That(DailyV1RestoredExecutionBoundaryComposer.TryCreate(
            definitions, source.EffectiveConfiguration, source.Calendar, restoredTime,
            restoredAdmission, source.WorldId, source.OwnerGraph,
            source.RuntimeIdAllocator, source.RandomSource, source.RecordSequence,
            source.DecisionStore, source.ActorChoiceStore,
            out DailyV1ExecutionBoundaryState restoredBoundary,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(restoredBoundary.SelectedRuntimeOwnerGraph, Is.SameAs(source.OwnerGraph));
        Assert.That(restoredBoundary.SimulationTime, Is.SameAs(restoredTime));
        Assert.That(restoredBoundary.AdmissionContext, Is.SameAs(restoredAdmission));
        Assert.That(restoredBoundary.WorldId, Is.SameAs(source.WorldId));
        Assert.That(restoredBoundary.SelectedRuntimeActorRoster.Select(member => member.RuntimeId),
            Is.EqualTo(source.OwnerGraph.Npcs.Select(owner => owner.RuntimeId)));
        Assert.That(restoredBoundary.HasAdmittedSelectedDailyExecution, Is.True);
        Assert.That(restoredBoundary.HasExactSelectedRuntimeExecutionRoots(
            source.OwnerGraph, source.PersonStore, source.ActorChoiceStore, source.RandomSource,
            source.RuntimeIdAllocator, source.RecordSequence, source.DecisionStore), Is.True);

        Assert.That(application.TryCreateRestoredActiveRuntimeSession(
            restoredBoundary, definitions, restoredTime.AbsoluteDay, null,
            out DailyV1ActiveRuntimeSession restoredSession, out diagnostic), Is.True, diagnostic);
        Assert.That(restoredSession.ExecutionBoundaryState, Is.SameAs(restoredBoundary));
        Assert.That(restoredSession.Definitions, Is.SameAs(definitions));
        Assert.That(restoredSession.RuntimeAdvanceBoundary, Is.Null);
    }

    [Test]
    public void RestoredExecutionBoundaryComposerOwnsSelectedP12DOwnerGraphAdmission()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition source,
            out string sourceDiagnostic), Is.True, sourceDiagnostic);

        SimulationRuntimeAdmissionContext restoredAdmission =
            source.AdmissionContext.CreateRestoredContinuationContext();
        SimulationTime restoredTime = new SimulationTime(source.SimulationTime.AbsoluteDay);
        CalendarDefinition calendarDefinition = definitions.Calendar.monthLengths != null
            && definitions.Calendar.monthLengths.Count > 0
            ? new CalendarDefinition(new List<int>(definitions.Calendar.monthLengths),
                definitions.Calendar.daysPerWeek)
            : new CalendarDefinition(definitions.Calendar.monthsPerYear,
                definitions.Calendar.weeksPerMonth, definitions.Calendar.daysPerWeek);
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(restoredTime);

        Assert.That(DailyV1RestoredExecutionBoundaryComposer.TryCreate(
            definitions,
            source.EffectiveConfiguration,
            calendarDefinition,
            restoredTime,
            restoredAdmission,
            source.WorldId,
            source.OwnerGraph.Cities,
            source.OwnerGraph.Npcs,
            source.OwnerGraph.SpatialNetwork,
            source.OwnerGraph.JusticeRecordsOwner,
            source.TravelPartyStore,
            source.OwnerGraph.RuntimeIdentityIndex,
            source.PersonStore,
            directives,
            source.Expeditions,
            source.RuntimeIdAllocator,
            source.RandomSource,
            source.RecordSequence,
            source.DecisionStore,
            source.ActorChoiceStore,
            out DailyV1ExecutionBoundaryState restoredBoundary,
            out SimulationCalendar restoredCalendar,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(restoredCalendar.SemanticVersion, Is.EqualTo(source.Calendar.SemanticVersion));
        Assert.That(restoredBoundary.SelectedRuntimeOwnerGraph, Is.Not.SameAs(source.OwnerGraph));
        Assert.That(restoredBoundary.SelectedRuntimeOwnerGraph.Cities,
            Is.EqualTo(source.OwnerGraph.Cities).AsCollection);
        Assert.That(restoredBoundary.SelectedRuntimeOwnerGraph.Npcs,
            Is.EqualTo(source.OwnerGraph.Npcs).AsCollection);
        Assert.That(restoredBoundary.SelectedRuntimeOwnerGraph.ScheduledDirectives,
            Is.SameAs(directives));
        Assert.That(restoredBoundary.SelectedRuntimeOwnerGraph.Expeditions,
            Is.SameAs(source.Expeditions));
        Assert.That(restoredBoundary.HasExactSelectedRuntimeExecutionRoots(
            restoredBoundary.SelectedRuntimeOwnerGraph,
            source.PersonStore,
            source.ActorChoiceStore,
            source.RandomSource,
            source.RuntimeIdAllocator,
            source.RecordSequence,
            source.DecisionStore), Is.True);
    }

    [Test]
    public void RestoredExecutionBoundaryComposerRejectsDuplicateOwnerBeforePublishingBoundary()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition source,
            out string sourceDiagnostic), Is.True, sourceDiagnostic);

        DailyV1CityRuntimeOwners firstCity = source.OwnerGraph.Cities[0];
        List<DailyV1CityRuntimeOwners> duplicateCities =
            new List<DailyV1CityRuntimeOwners>(source.OwnerGraph.Cities) { firstCity };
        SimulationTime restoredTime = new SimulationTime(source.SimulationTime.AbsoluteDay);
        CalendarDefinition calendarDefinition = new CalendarDefinition(
            definitions.Calendar.monthsPerYear,
            definitions.Calendar.weeksPerMonth,
            definitions.Calendar.daysPerWeek);

        Assert.That(DailyV1RestoredExecutionBoundaryComposer.TryCreate(
            definitions,
            source.EffectiveConfiguration,
            calendarDefinition,
            restoredTime,
            source.AdmissionContext.CreateRestoredContinuationContext(),
            source.WorldId,
            duplicateCities,
            source.OwnerGraph.Npcs,
            source.OwnerGraph.SpatialNetwork,
            source.OwnerGraph.JusticeRecordsOwner,
            source.TravelPartyStore,
            source.OwnerGraph.RuntimeIdentityIndex,
            source.PersonStore,
            new ScheduledDirectiveStore(restoredTime),
            source.Expeditions,
            source.RuntimeIdAllocator,
            source.RandomSource,
            source.RecordSequence,
            source.DecisionStore,
            source.ActorChoiceStore,
            out DailyV1ExecutionBoundaryState restoredBoundary,
            out SimulationCalendar restoredCalendar,
            out string diagnostic), Is.False);

        Assert.That(restoredBoundary, Is.Null);
        Assert.That(restoredCalendar, Is.Null);
        Assert.That(diagnostic, Does.Contain("cross-owner relationships"));
    }

    [Test]
    public void ApplicationComposesRestoredOwnerGraphBoundaryAndUnpublishedSessionTogether()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1Application application = new DailyV1Application();
        Assert.That(application.TryCaptureBootstrapAdmissionContext(
            out SimulationRuntimeAdmissionContext bootstrapAdmission), Is.True);
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            bootstrapAdmission,
            out DailyV1GenesisComposition source,
            out string sourceDiagnostic), Is.True, sourceDiagnostic);

        SimulationTime restoredTime = new SimulationTime(source.SimulationTime.AbsoluteDay);
        SimulationRuntimeAdmissionContext restoredAdmission =
            bootstrapAdmission.CreateRestoredContinuationContext();
        CalendarDefinition calendarDefinition = definitions.Calendar.monthLengths != null
            && definitions.Calendar.monthLengths.Count > 0
            ? new CalendarDefinition(new List<int>(definitions.Calendar.monthLengths),
                definitions.Calendar.daysPerWeek)
            : new CalendarDefinition(definitions.Calendar.monthsPerYear,
                definitions.Calendar.weeksPerMonth, definitions.Calendar.daysPerWeek);
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(restoredTime);

        Assert.That(application.TryCreateRestoredActiveRuntimeSession(
            definitions,
            source.EffectiveConfiguration,
            calendarDefinition,
            restoredTime,
            restoredAdmission,
            source.WorldId,
            source.OwnerGraph.Cities,
            source.OwnerGraph.Npcs,
            source.OwnerGraph.SpatialNetwork,
            source.OwnerGraph.JusticeRecordsOwner,
            source.TravelPartyStore,
            source.OwnerGraph.RuntimeIdentityIndex,
            source.PersonStore,
            directives,
            source.Expeditions,
            source.RuntimeIdAllocator,
            source.RandomSource,
            source.RecordSequence,
            source.DecisionStore,
            source.ActorChoiceStore,
            lastEconomySnapshotDay: 0L,
            replacementOperation: null,
            out DailyV1ActiveRuntimeSession candidate,
            out SimulationCalendar restoredCalendar,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(candidate, Is.Not.Null);
        Assert.That(candidate.Definitions, Is.SameAs(definitions));
        Assert.That(candidate.ExecutionBoundaryState.SimulationTime, Is.SameAs(restoredTime));
        Assert.That(candidate.ExecutionBoundaryState.AdmissionContext, Is.SameAs(restoredAdmission));
        Assert.That(candidate.ExecutionBoundaryState.WorldId, Is.SameAs(source.WorldId));
        Assert.That(candidate.OwnerGraph.Definitions, Is.SameAs(definitions));
        Assert.That(candidate.OwnerGraph.ScheduledDirectives, Is.SameAs(directives));
        Assert.That(candidate.OwnerGraph.Expeditions, Is.SameAs(source.Expeditions));
        Assert.That(restoredCalendar.SemanticVersion, Is.EqualTo(source.Calendar.SemanticVersion));
        Assert.That(candidate.RuntimeAdvanceBoundary, Is.Null);
        Assert.That(application.ActiveSession, Is.Null,
            "Composition returns one unpublished Core candidate; only the existing restore commit may exchange the active session.");

        Assert.That(application.TryCreateRestoredActiveRuntimeSession(
            definitions,
            null,
            calendarDefinition,
            restoredTime,
            restoredAdmission,
            source.WorldId,
            source.OwnerGraph.Cities,
            source.OwnerGraph.Npcs,
            source.OwnerGraph.SpatialNetwork,
            source.OwnerGraph.JusticeRecordsOwner,
            source.TravelPartyStore,
            source.OwnerGraph.RuntimeIdentityIndex,
            source.PersonStore,
            directives,
            source.Expeditions,
            source.RuntimeIdAllocator,
            source.RandomSource,
            source.RecordSequence,
            source.DecisionStore,
            source.ActorChoiceStore,
            lastEconomySnapshotDay: 0L,
            replacementOperation: null,
            out DailyV1ActiveRuntimeSession rejectedCandidate,
            out SimulationCalendar rejectedCalendar,
            out string rejectedDiagnostic), Is.False);
        Assert.That(rejectedCandidate, Is.Null);
        Assert.That(rejectedCalendar, Is.Null);
        Assert.That(rejectedDiagnostic, Does.Contain("exact selected definitions"));
        Assert.That(application.ActiveSession, Is.Null);
    }

    [Test]
    public void SelectedGenesisPreservesStageCallbackExceptionBoundary()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        var expected = new InvalidOperationException("stage callback sentinel");
        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
            DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                () => new WorldId(Guid.NewGuid()),
                stage => { if (stage == "p9.genesis.authored-world/v1") throw expected; },
                out _, out _));

        Assert.That(actual, Is.SameAs(expected));
    }

    [Test]
    public void SelectedP9CompositionConsumesP8BeforeNpcAllocationAndRetainsCanonicalOwners()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(definitions.Configuration.useAuthoredGeographyProfile, Is.True);
        Assert.That(definitions.Configuration.explorableSites, Is.Empty);

        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions, SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition, out string diagnostic),
            Is.True, diagnostic);
        Assert.That(composition.Definitions, Is.SameAs(definitions));
        Assert.That(composition.OwnerGraph.Definitions, Is.SameAs(definitions));
        Assert.That(composition.OwnerGraph.SpatialAuthority,
            Is.SameAs(composition.SpatialAuthority),
            "The selected Core graph must retain the exact spatial authority used by P8 census owners.");
        Assert.That(WorldId.TryParse(composition.WorldId.Value, out WorldId parsedWorld), Is.True);
        Assert.That(parsedWorld, Is.Not.SameAs(composition.WorldId));
        Assert.That(composition.SimulationTime.AbsoluteDay, Is.Zero);
        Assert.That(composition.RandomSource, Is.TypeOf<DeterministicRandomSource>());
        Assert.That(((DeterministicRandomSource)composition.RandomSource).Seed,
            Is.EqualTo(definitions.Configuration.useFixedSimulationSeed
                ? definitions.Configuration.simulationSeed : 0));
        Assert.That(composition.RecordSequence.CaptureSnapshot().NextSequence, Is.EqualTo(1L));
        Assert.That(composition.DecisionStore.Decisions, Is.Empty);
        Assert.That(composition.OwnerGraph.PersonStore, Is.SameAs(composition.PersonStore));
        Assert.That(composition.ActorChoiceStore.PersonStore, Is.SameAs(composition.PersonStore));
        Assert.That(composition.OwnerGraph.PersonSpatialPositionStore, Is.Not.Null);
        Assert.That(composition.OwnerGraph.PersonSpatialPositionStore.PersonStore,
            Is.SameAs(composition.PersonStore));
        Assert.That(composition.OwnerGraph.PersonSpatialPositionStore.SpatialAuthorityStore,
            Is.SameAs(composition.SpatialAuthority));
        Assert.That(composition.OwnerGraph.PersonSpatialPositionStore.Count, Is.Zero);
        Assert.That(composition.OwnerGraph.PersonSpatialPositionStore.Revision, Is.Zero);
        Assert.That(composition.OwnerGraph.LegacySpatialAnchors,
            Is.SameAs(composition.LegacySpatialAnchorBindings));
        Assert.That(composition.OwnerGraph.LegacySpatialAnchors.SpatialLocationLookup,
            Is.SameAs(composition.SpatialAuthority));
        Assert.That(composition.Expeditions, Is.Not.Null);
        Assert.That(composition.Expeditions.ActiveExpeditions, Is.Empty);
        Assert.That(composition.Expeditions.Revision, Is.Zero);
        Assert.That(composition.OwnerGraph.Expeditions, Is.SameAs(composition.Expeditions),
            "P9 genesis binds the exact P12 owner instance while its supported initial content remains exact-empty.");
        Assert.That(composition.ExecutionBoundaryState.SelectedRuntimeOwnerGraph,
            Is.SameAs(composition.OwnerGraph));
        IReadOnlyList<DailyV1ActorTurnRosterProjection> actorRoster =
            composition.ExecutionBoundaryState.SelectedRuntimeActorRoster;
        Assert.That(actorRoster.Select(member => member.RuntimeId),
            Is.EqualTo(composition.OwnerGraph.Npcs.Select(owner => owner.RuntimeId)),
            "Core genesis supplies the exact actor-turn order from its admitted NPC owner graph.");
        Assert.That(actorRoster.All(member => member.PersonId == null), Is.True,
            "The genesis fixture has no materialized Person bindings; later bindings update this Core roster through the existing membership transaction.");
        Assert.That(composition.ExecutionBoundaryState.TryBindOrValidateSelectedRuntimeActorRoster(
            actorRoster), Is.True,
            "A host projection that exactly matches Core's actor identities and PersonId instances is accepted.");
        Assert.That(composition.ExecutionBoundaryState.TryBindOrValidateSelectedRuntimeActorRoster(
            actorRoster.Reverse()), Is.False,
            "A host cannot reorder the actor identities that determine persisted actor-choice ordinals.");
        Assert.That(composition.ExecutionBoundaryState.SimulationTime,
            Is.SameAs(composition.SimulationTime));
        Assert.That(composition.ExecutionBoundaryState.WorldId, Is.SameAs(composition.WorldId));
        Assert.That(composition.ExecutionBoundaryState.AdmissionContext,
            Is.SameAs(composition.AdmissionContext));
        Assert.That(composition.ExecutionBoundaryState.ConfigurationIdentity,
            Is.SameAs(composition.EffectiveConfiguration));
        Assert.That(composition.ExecutionBoundaryState.CalendarIdentity,
            Is.SameAs(composition.Calendar));
        Assert.That(composition.ExecutionBoundaryState.SelectedRuntimeExecutionRootsBound, Is.True);
        Assert.That(composition.ExecutionBoundaryState.HasAdmittedSelectedDailyExecution, Is.True,
            "Core selects the shared Daily-v1 path from its admitted definition, owner, and execution-root identities.");
        Assert.That(composition.ExecutionBoundaryState.HasExactSelectedRuntimeExecutionRoots(
            composition.OwnerGraph,
            composition.PersonStore,
            composition.ActorChoiceStore,
            composition.RandomSource,
            composition.RuntimeIdAllocator,
            composition.RecordSequence,
            composition.DecisionStore), Is.True);
        Assert.That(composition.ExecutionBoundaryState.HasExactSelectedRuntimeExecutionRoots(
            composition.OwnerGraph,
            composition.PersonStore,
            composition.ActorChoiceStore,
            new DeterministicRandomSource(),
            composition.RuntimeIdAllocator,
            composition.RecordSequence,
            composition.DecisionStore), Is.False,
            "A host cannot substitute a random source after Core genesis binds the runtime roots.");
        Assert.That(composition.ExecutionBoundaryState.TryBindSelectedRuntimeExecutionRoots(
            composition.RuntimeIdAllocator,
            composition.RandomSource,
            composition.RecordSequence,
            composition.DecisionStore,
            composition.ActorChoiceStore), Is.True,
            "Rebinding the exact source identities is idempotent.");
        Assert.That(composition.ExecutionBoundaryState.TryBindSelectedRuntimeExecutionRoots(
            composition.RuntimeIdAllocator,
            new DeterministicRandomSource(),
            composition.RecordSequence,
            composition.DecisionStore,
            composition.ActorChoiceStore), Is.False,
            "A second random source cannot replace the admitted runtime root.");
        Assert.That(composition.ExecutionBoundaryState.TryBindSelectedRuntimeExecutionRoots(
            null, composition.RandomSource, composition.RecordSequence,
            composition.DecisionStore, composition.ActorChoiceStore), Is.False,
            "A runtime with no allocator cannot enter selected execution.");
        Assert.That(composition.ExecutionBoundaryState.TryBindSelectedRuntimeExecutionRoots(
            composition.RuntimeIdAllocator, composition.RandomSource, null,
            composition.DecisionStore, composition.ActorChoiceStore), Is.False,
            "A runtime with no record sequence cannot enter selected execution.");
        Assert.That(composition.ExecutionBoundaryState.TryBindSelectedRuntimeExecutionRoots(
            composition.RuntimeIdAllocator, composition.RandomSource, composition.RecordSequence,
            null, composition.ActorChoiceStore), Is.False,
            "A runtime with no decision store cannot enter selected execution.");

        Assert.That(composition.P9ProfileFingerprint,
            Is.EqualTo(DailyV1DefinitionIdentity.CreateP9Fingerprint(definitions, out _)));
        Assert.That(composition.P9ProvenanceRecords, Is.Not.Empty);

        Assert.That(composition.SpatialAuthority.HasGeography, Is.True);
        Assert.That(composition.SpatialAuthority.Hexes, Has.Count.EqualTo(1));
        Assert.That(composition.SpatialAuthority.Locations, Has.Count.EqualTo(1));
        Assert.That(composition.SpatialAuthority.Hexes.Single().Id.Value,
            Is.EqualTo(definitions.Configuration.authoredHexId));
        Assert.That(composition.SpatialAuthority.Locations.Single().Id.Value,
            Is.EqualTo(definitions.Configuration.authoredLocationId));
        Assert.That(composition.SpatialAuthority.PassageAuthority, Is.Not.Null);

        Assert.That(composition.OwnerGraph.Cities, Is.Not.Empty);
        Assert.That(composition.OwnerGraph.Npcs, Is.Not.Empty);
        Assert.That(composition.OwnerGraph.JusticeRecordsOwner, Is.Not.Null);
        Assert.That(composition.OwnerGraph.JusticeRecordsOwner.WantedRecords, Is.Empty);
        Assert.That(composition.OwnerGraph.ScheduledDirectives, Is.Not.Null);
        Assert.That(composition.OwnerGraph.ScheduledDirectives.Directives, Is.Empty);
        Assert.That(composition.OwnerGraph.TravelParties, Is.SameAs(composition.TravelPartyStore));
        Assert.That(composition.TravelPartyStore.ActiveParties, Is.Empty);
        Assert.That(composition.OwnerGraph.Expeditions, Is.SameAs(composition.Expeditions),
            "The accepted Daily-v1 profile binds its exact P12 owner while selecting no P10 expedition content.");
        Assert.That(composition.OwnerGraph.Expeditions.ActiveExpeditions, Is.Empty);
        Assert.That(composition.OwnerGraph.Expeditions.Revision, Is.Zero);
        Assert.That(composition.LegacySpatialAnchorBindings.Count, Is.Zero,
            "No City anchor is fabricated when the accepted definitions contain no City material-flow LocationId.");
        Assert.That(definitions.ExplorableSites, Is.Empty,
            "P10 site and local topology owners remain exact-empty in this proving profile.");

        // Compare allocator-derived selected City/Location/Route/NPC identities
        // with the existing Core graph path. P8 consumes no runtime IDs, so its
        // insertion must not shift the established owner identity sequence.
        Assert.That(DailyV1SelectedRuntimeOwnerGraphBuilder.TryBuildConfigured(
            definitions, new RuntimeIdAllocator(), out DailyV1SelectedRuntimeOwnerGraph expected), Is.True);
        Assert.That(composition.OwnerGraph.Cities.Select(owner => owner.RuntimeId),
            Is.EqualTo(expected.Cities.Select(owner => owner.RuntimeId)));
        Assert.That(composition.OwnerGraph.Cities.Select(owner => owner.LocationRuntimeId),
            Is.EqualTo(expected.Cities.Select(owner => owner.LocationRuntimeId)));
        Assert.That(composition.OwnerGraph.SpatialNetwork.Routes.Select(route => route.RuntimeId),
            Is.EqualTo(expected.SpatialNetwork.Routes.Select(route => route.RuntimeId)));
        Assert.That(composition.OwnerGraph.Npcs.Select(owner => owner.RuntimeId),
            Is.EqualTo(expected.Npcs.Select(owner => owner.RuntimeId)));
    }
}
