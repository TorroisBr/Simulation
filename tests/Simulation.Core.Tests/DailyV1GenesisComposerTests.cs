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
        Assert.That(completed, Is.EqualTo(definitions.StageOrder.Take(definitions.StageOrder.Count - 2)));
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
        Assert.That(composition.OwnerGraph.Expeditions, Is.Null,
            "P9 genesis retains the P12 exact-empty owner without admitting P10 expedition participation into the selected profile graph.");
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
        Assert.That(composition.OwnerGraph.Expeditions, Is.Null,
            "The accepted Daily-v1 profile has no selected P10 expedition content.");
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
