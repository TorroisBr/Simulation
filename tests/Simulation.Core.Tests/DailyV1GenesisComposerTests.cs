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
        Assert.That(composition.OwnerGraph.GenealogyStore, Is.Not.Null,
            "Selected genesis must create its Genealogy authority in Core.");
        Assert.That(composition.OwnerGraph.GenealogyStore.Count, Is.Zero);
        Assert.That(composition.OwnerGraph.GenealogyStore.Revision, Is.Zero);
        Assert.That(composition.OwnerGraph.TryBindGenealogyStore(new GenealogyStore()), Is.False,
            "The selected Core genesis graph cannot replace its exact empty GenealogyStore.");
        Assert.That(composition.OwnerGraph.P12EOwners, Is.Not.Null,
            "Core Genesis publishes the complete selected P12-E owner set before its graph is exposed.");
        Assert.That(composition.OwnerGraph.P12EOwners.Persons, Is.SameAs(composition.PersonStore));
        Assert.That(composition.OwnerGraph.P12EOwners.JusticeRecords,
            Is.SameAs(composition.OwnerGraph.JusticeRecordsOwner),
            "Genesis Justice facts remain the exact Core-authored owner.");
        Assert.That(composition.OwnerGraph.P12EOwners.PoliticalDecisions,
            Is.SameAs(composition.OwnerGraph.PoliticalDecisionStore));
        Assert.That(composition.OwnerGraph.P12EOwners.Positions.SpatialAuthorityStore,
            Is.SameAs(composition.SpatialAuthority));
        Assert.That(composition.OwnerGraph.P12EOwners.Battles.WarStore,
            Is.SameAs(composition.OwnerGraph.P12EOwners.Wars));
        Assert.That(composition.OwnerGraph.P12EOwners.CrimeSocialAppraisal.SimulationTime,
            Is.SameAs(composition.SimulationTime));
        Assert.That(DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedP12EOwnerSections(
            composition.OwnerGraph, Array.Empty<OwnerSectionCensusSnapshot>(),
            out IReadOnlyList<OwnerSectionCensusSnapshot> genesisP12ERows,
            out string p12ECensusDiagnostic), Is.True, p12ECensusDiagnostic);
        Assert.That(genesisP12ERows, Has.Count.EqualTo(23));
        Assert.That(genesisP12ERows.All(section => section.Role == OwnerSectionRole.Required), Is.True);
        Assert.That(composition.OwnerGraph.Expeditions, Is.SameAs(composition.Expeditions),
            "The Core graph must retain the exact ExpeditionStore created by genesis.");
        Assert.That(composition.ExplorableSites, Is.Not.Null);
        Assert.That(composition.ExplorableSites.Count, Is.Zero);
        Assert.That(composition.ExplorableSites.Revision, Is.Zero);
        Assert.That(composition.OwnerGraph.ExplorableSites, Is.SameAs(composition.ExplorableSites),
            "Core genesis must retain the exact empty site owner in the selected graph.");
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

        Assert.That(TryComposeRestoredBoundary(
            source, restoredTime, restoredAdmission, new ActorChoiceStore(new PersonStore()),
            out _, out string rejectedDiagnostic), Is.False);
        Assert.That(rejectedDiagnostic, Does.Contain("exact admitted definitions"));

        Assert.That(TryComposeRestoredBoundary(
            source, restoredTime, restoredAdmission, source.ActorChoiceStore,
            out DailyV1ExecutionBoundaryState restoredBoundary,
            out string diagnostic), Is.True, diagnostic);

        Assert.That(restoredBoundary.SelectedRuntimeOwnerGraph, Is.Not.SameAs(source.OwnerGraph));
        Assert.That(restoredBoundary.SelectedRuntimeOwnerGraph.P12EOwners.CrimeSocialAppraisal.SimulationTime,
            Is.SameAs(restoredTime));
        Assert.That(restoredBoundary.SimulationTime, Is.SameAs(restoredTime));
        Assert.That(restoredBoundary.AdmissionContext, Is.SameAs(restoredAdmission));
        Assert.That(restoredBoundary.WorldId, Is.SameAs(source.WorldId));
        Assert.That(restoredBoundary.SelectedRuntimeActorRoster.Select(member => member.RuntimeId),
            Is.EqualTo(source.OwnerGraph.Npcs.Select(owner => owner.RuntimeId)));
        Assert.That(restoredBoundary.HasAdmittedSelectedDailyExecution, Is.True);
        Assert.That(restoredBoundary.HasExactSelectedRuntimeExecutionRoots(
            restoredBoundary.SelectedRuntimeOwnerGraph, source.PersonStore, source.ActorChoiceStore, source.RandomSource,
            source.RuntimeIdAllocator, source.RecordSequence, source.DecisionStore), Is.True);

        Assert.That(application.TryCreateRestoredActiveRuntimeSession(
            restoredBoundary, definitions, restoredTime.AbsoluteDay, null,
            out DailyV1ActiveRuntimeSession restoredSession, out diagnostic), Is.True, diagnostic);
        Assert.That(restoredSession.ExecutionBoundaryState, Is.SameAs(restoredBoundary));
        Assert.That(restoredSession.Definitions, Is.SameAs(definitions));
        Assert.That(restoredSession.RuntimeAdvanceBoundary, Is.Null);
    }

    [Test]
    public void RestoredCoreBindsItsExactOwnerGraphBeforeHostProjectionBinding()
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
        Assert.That(TryComposeRestoredBoundary(
            source, restoredTime, restoredAdmission, source.ActorChoiceStore,
            out DailyV1ExecutionBoundaryState restoredBoundary,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = restoredBoundary.SelectedRuntimeOwnerGraph;
        AuthoritativeMutationGuard foreignGuard = new AuthoritativeMutationGuard();
        Assert.That(((IAuthoritativeMutationGuardBindable)graph.PersonStore)
            .CanBindMutationGuard(foreignGuard), Is.True);
        Assert.That(PopulationOwnerBridge.CanBindMutationStatus(
            graph.Cities[0].Population, foreignGuard), Is.True);

        Assert.That(restoredBoundary.TryBindSelectedCoreOwnerMutationGuardAuthorities(
            out diagnostic), Is.True, diagnostic);
        Assert.That(restoredBoundary.SelectedCoreOwnerMutationGuardAuthoritiesBound, Is.True);
        Assert.That(((IAuthoritativeMutationGuardBindable)graph.PersonStore)
            .CanBindMutationGuard(restoredBoundary.MutationGuard), Is.True,
            "Core binds the exact graph root to its own session guard.");
        Assert.That(((IAuthoritativeMutationGuardBindable)graph.PersonStore)
            .CanBindMutationGuard(foreignGuard), Is.False,
            "A host callback cannot replace the selected graph's Core guard.");
        Assert.That(PopulationOwnerBridge.CanBindMutationStatus(
            graph.Cities[0].Population, foreignGuard), Is.False,
            "The selected City population authority is bound before Unity projection binding.");
        Assert.That(SimulationTimeOwnerBridge.CanBindMutationStatus(
            restoredTime, foreignGuard), Is.False);
        Assert.That(((IAuthoritativeMutationGuardBindable)source.ActorChoiceStore)
            .CanBindMutationGuard(foreignGuard), Is.False);
    }

    [Test]
    public void RestoredCoreMutationGuardPreflightDoesNotPartiallyBindAnInvalidGraph()
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
        Assert.That(TryComposeRestoredBoundary(
            source, restoredTime, restoredAdmission, source.ActorChoiceStore,
            out DailyV1ExecutionBoundaryState restoredBoundary,
            out string diagnostic), Is.True, diagnostic);

        DailyV1SelectedRuntimeOwnerGraph graph = restoredBoundary.SelectedRuntimeOwnerGraph;
        AuthoritativeMutationGuard foreignGuard = new AuthoritativeMutationGuard();
        Assert.That(PopulationOwnerBridge.TryBindMutationStatus(
            graph.Cities[0].Population, foreignGuard), Is.True);

        Assert.That(restoredBoundary.TryBindSelectedCoreOwnerMutationGuardAuthorities(
            out diagnostic), Is.False);
        Assert.That(restoredBoundary.SelectedCoreOwnerMutationGuardAuthoritiesBound, Is.False);
        Assert.That(((IAuthoritativeMutationGuardBindable)graph.PersonStore)
            .CanBindMutationGuard(foreignGuard), Is.True,
            "Core preflights the entire graph before binding any member.");
        Assert.That(SimulationTimeOwnerBridge.CanBindMutationStatus(
            restoredTime, foreignGuard), Is.True);
        Assert.That(((IAuthoritativeMutationGuardBindable)source.ActorChoiceStore)
            .CanBindMutationGuard(foreignGuard), Is.True);
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
            null,
            directives,
            source.Expeditions,
            source.RuntimeIdAllocator,
            source.RandomSource,
            source.RecordSequence,
            source.DecisionStore,
            source.ActorChoiceStore,
            out DailyV1ExecutionBoundaryState restoredBoundary,
            out SimulationCalendar restoredCalendar,
            out string diagnostic,
            source.OwnerGraph.PoliticalDecisionStore), Is.True, diagnostic);

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
    public void RestoredExecutionBoundaryRequiresExactPoliticalDecisionOwner()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition source,
            out string sourceDiagnostic), Is.True, sourceDiagnostic);

        SimulationTime restoredTime = new SimulationTime(source.SimulationTime.AbsoluteDay);
        CalendarDefinition calendarDefinition = new CalendarDefinition(
            definitions.Calendar.monthsPerYear,
            definitions.Calendar.weeksPerMonth,
            definitions.Calendar.daysPerWeek);
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(restoredTime);
        SimulationRuntimeAdmissionContext restoredAdmission =
            source.AdmissionContext.CreateRestoredContinuationContext();

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
            source.OwnerGraph.GenealogyStore,
            directives,
            source.Expeditions,
            source.RuntimeIdAllocator,
            source.RandomSource,
            source.RecordSequence,
            source.DecisionStore,
            source.ActorChoiceStore,
            out DailyV1ExecutionBoundaryState missingOwnerBoundary,
            out SimulationCalendar missingOwnerCalendar,
            out string missingOwnerDiagnostic,
            null), Is.False);
        Assert.That(missingOwnerBoundary, Is.Null);
        Assert.That(missingOwnerCalendar, Is.Null);
        Assert.That(missingOwnerDiagnostic, Does.Contain("P12-E PoliticalDecision owner"));

        Assert.That(DailyV1P12DSelectedOwnerGraphStager.TryCreate(
            definitions,
            source.OwnerGraph.Cities,
            source.OwnerGraph.Npcs,
            source.OwnerGraph.SpatialNetwork,
            source.OwnerGraph.JusticeRecordsOwner,
            new TravelPartyStore(),
            source.OwnerGraph.RuntimeIdentityIndex,
            new PersonStore(),
            new ScheduledDirectiveStore(restoredTime),
            new ExpeditionStore(),
            out DailyV1SelectedRuntimeOwnerGraph p12dOnlyGraph,
            out DailyV1P12DSelectedOwnerGraphFailure graphFailure), Is.True, graphFailure.ToString());

        ActorChoiceStore actorChoices = new ActorChoiceStore(p12dOnlyGraph.PersonStore);
        Assert.That(DailyV1RestoredExecutionBoundaryComposer.TryCreate(
            definitions,
            source.EffectiveConfiguration,
            source.Calendar,
            restoredTime,
            restoredAdmission,
            source.WorldId,
            p12dOnlyGraph,
            source.RuntimeIdAllocator,
            source.RandomSource,
            source.RecordSequence,
            source.DecisionStore,
            actorChoices,
            out DailyV1ExecutionBoundaryState p12dOnlyBoundary,
            out string p12dOnlyDiagnostic), Is.False);
        Assert.That(p12dOnlyBoundary, Is.Null);
        Assert.That(p12dOnlyDiagnostic, Does.Contain("P12-E PoliticalDecision owner"));
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
            null,
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
        DailyV1DomainEventAuthority eventAuthority = DailyV1DomainEventAuthority.Create(
            source.RuntimeIdAllocator, restoredTime, source.RecordSequence, source.DecisionStore);
        CalendarDefinition calendarDefinition = definitions.Calendar.monthLengths != null
            && definitions.Calendar.monthLengths.Count > 0
            ? new CalendarDefinition(new List<int>(definitions.Calendar.monthLengths),
                definitions.Calendar.daysPerWeek)
            : new CalendarDefinition(definitions.Calendar.monthsPerYear,
                definitions.Calendar.weeksPerMonth, definitions.Calendar.daysPerWeek);
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(restoredTime);

        Assert.That(application.TryCreateRestoredActiveRuntimeSession(
            definitions,
            null,
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
            source.OwnerGraph.GenealogyStore,
            directives,
            source.Expeditions,
            null,
            eventAuthority,
            source.RuntimeIdAllocator,
            source.RandomSource,
            source.RecordSequence,
            source.DecisionStore,
            source.ActorChoiceStore,
            lastEconomySnapshotDay: 0L,
            replacementOperation: null,
            out DailyV1ActiveRuntimeSession candidate,
            out SimulationCalendar restoredCalendar,
            out string diagnostic,
            source.OwnerGraph.PoliticalDecisionStore), Is.True, diagnostic);

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
            source.P9ManifestSnapshot,
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
            source.SpatialAuthority,
            source.RuntimeIdAllocator,
            source.RandomSource,
            source.RecordSequence,
            null,
            source.ActorChoiceStore,
            0L,
            null,
            out DailyV1ActiveRuntimeSession missingDecisionStoreCandidate,
            out SimulationCalendar missingDecisionStoreCalendar,
            out string missingDecisionStoreDiagnostic), Is.False);
        Assert.That(missingDecisionStoreCandidate, Is.Null);
        Assert.That(missingDecisionStoreCalendar, Is.Null);
        Assert.That(missingDecisionStoreDiagnostic, Does.Contain("decision-store roots"));
        Assert.That(application.ActiveSession, Is.Null,
            "A restored composition with no decision-store owner must not publish a session.");

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

    private static bool TryComposeRestoredBoundary(
        DailyV1GenesisComposition source,
        SimulationTime restoredTime,
        SimulationRuntimeAdmissionContext restoredAdmission,
        ActorChoiceStore actorChoiceStore,
        out DailyV1ExecutionBoundaryState boundary,
        out string diagnostic)
    {
        P12ECoreOwnerRestoreStage sourceP12E = source.OwnerGraph.P12EOwners;
        P12ECoreOwnerRestoreStage restoredP12E = new P12ECoreOwnerRestoreStage(
            sourceP12E.Persons,
            sourceP12E.Institutions,
            sourceP12E.Offices,
            sourceP12E.PropertyOwnership,
            sourceP12E.Estates,
            sourceP12E.Factions,
            sourceP12E.PoliticalClaims,
            sourceP12E.PoliticalSupport,
            sourceP12E.PoliticalDecisions,
            sourceP12E.JusticeRecords,
            sourceP12E.ArmedForces,
            sourceP12E.Manpower,
            sourceP12E.Positions,
            sourceP12E.Conflicts,
            sourceP12E.Wars,
            sourceP12E.Battles,
            new CrimeSocialAppraisalWorldState(
                sourceP12E.Persons, sourceP12E.Institutions, restoredTime));
        DailyV1CalendarDefinition calendar = source.Definitions.Calendar;
        CalendarDefinition calendarDefinition = calendar.monthLengths != null
            && calendar.monthLengths.Count > 0
            ? new CalendarDefinition(new List<int>(calendar.monthLengths), calendar.daysPerWeek)
            : new CalendarDefinition(calendar.monthsPerYear,
                calendar.weeksPerMonth, calendar.daysPerWeek);
        return DailyV1RestoredExecutionBoundaryComposer.TryCreate(
            source.Definitions,
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
            source.OwnerGraph.GenealogyStore,
            new ScheduledDirectiveStore(restoredTime),
            source.Expeditions,
            source.RuntimeIdAllocator,
            source.RandomSource,
            source.RecordSequence,
            source.DecisionStore,
            actorChoiceStore,
            out boundary,
            out _,
            out diagnostic,
            source.OwnerGraph.PoliticalDecisionStore,
            restoredP12E);
    }
}
