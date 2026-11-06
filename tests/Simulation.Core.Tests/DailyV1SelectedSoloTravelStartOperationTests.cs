using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1SelectedSoloTravelStartOperationTests
{
    [Test]
    public void CoreDerivesExactSoloTravelStartOwnerSectionsFromItsSelectedGraph()
    {
        DailyV1GenesisComposition composition = Compose();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];

        Assert.That(state.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out IReadOnlyList<string> sectionIds), Is.True);
        Assert.That(sectionIds, Does.Contain(
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + actor.RuntimeId));
        Assert.That(sectionIds, Does.Contain(
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix + actor.RuntimeId));
        Assert.That(sectionIds, Does.Contain(
            DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, actor.RuntimeId)));
        Assert.That(sectionIds, Does.Contain(
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + actor.RuntimeId));
        Assert.That(sectionIds, Does.Contain(
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + actor.RuntimeId));
        Assert.That(sectionIds, Does.Contain(RuntimeIdAllocatorCensusProvider.EventsSectionId));
        Assert.That(sectionIds, Does.Contain(SimulationRecordSequenceCensusProvider.SectionId));
        Assert.That(sectionIds.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(sectionIds.Count));
        Assert.That(sectionIds, Is.Ordered.Using<string>(StringComparer.Ordinal));
    }

    [Test]
    public void CoreIncludesCityPresenceSectionOnlyWhenActorIsInThatOwner()
    {
        DailyV1GenesisComposition composition = Compose();
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        DailyV1CityRuntimeOwners city = graph.Cities.Single(
            candidate => string.Equals(
                candidate.RuntimeId, actor.Travel.CurrentCityRuntimeId, StringComparison.Ordinal));
        string presenceSectionId = DailyV1SelectedOwnerSectionCensusContract
            .CityImportantNpcSectionIdFor(city.RuntimeId);

        Assert.That(city.NpcPresence.Contains(actor.RuntimeId), Is.True);
        Assert.That(city.NpcPresence.TryRemove(actor.RuntimeId), Is.True);
        Assert.That(composition.ExecutionBoundaryState.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out IReadOnlyList<string> withoutPresence), Is.True);
        Assert.That(withoutPresence, Does.Not.Contain(presenceSectionId));

        Assert.That(city.NpcPresence.TryAdd(actor.RuntimeId), Is.True);
        Assert.That(composition.ExecutionBoundaryState.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out IReadOnlyList<string> withPresence), Is.True);
        Assert.That(withPresence, Does.Contain(presenceSectionId));
        Assert.That(withPresence.Count, Is.EqualTo(withoutPresence.Count + 1));
    }

    [Test]
    public void CoreOwnsSoloTravelStartScopeAndNotifiesOnlyCommittedAllowedSections()
    {
        DailyV1GenesisComposition composition = Compose();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        Assert.That(state.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out IReadOnlyList<string> sectionIds), Is.True);
        RegisterAndSealRequiredSections(state, sectionIds);
        string changed = DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix + actor.RuntimeId;
        TestProtocol protocol = new TestProtocol();

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        try
        {
            Assert.That(state.TryBeginSelectedP12SoloTravelStart(
                graph, actor, sectionIds, protocol, out IDisposable scope), Is.True);
            Assert.That(scope, Is.Not.Null);
            Assert.That(protocol.ValidationCalls, Is.EqualTo(1));
            Assert.That(protocol.CapacityChecks, Is.EqualTo(1));
            Assert.That(protocol.EnterCalls, Is.EqualTo(1));

            Assert.That(state.TryCanCommitSelectedP12SoloTravelStartMutation(
                new[] { changed }, out bool commitHandled), Is.True);
            Assert.That(commitHandled, Is.True);
            Assert.That(protocol.ValidationCalls, Is.EqualTo(1),
                "Core validates its exact TravelState witness directly; the host vector is validated once at operation entry.");

            Assert.That(actor.Travel.TryCommitState(
                actor.Travel.CurrentLocationRuntimeId,
                actor.Travel.CurrentCityRuntimeId,
                "location/solo-travel-scope", "city/solo-travel-scope",
                1, 1, "route/solo-travel-scope", true,
                "decision/solo-travel-scope", null, true), Is.True);
            Assert.That(state.TryRecordSelectedP12SoloTravelStartMutation(
                new[] { changed }, out bool recordHandled), Is.True);
            Assert.That(recordHandled, Is.True);
            Assert.That(state.TryCanCommitSelectedP12SoloTravelStartMutation(
                new[] { changed }, out commitHandled), Is.True);
            Assert.That(commitHandled, Is.True);
            Assert.That(protocol.ValidationCalls, Is.EqualTo(1),
                "Core checks the advanced exact TravelState baseline without asking the host to revalidate the old vector row.");

            scope.Dispose();
            scope.Dispose();
            Assert.That(protocol.NotifiedSections, Is.EqualTo(new[] { changed }));
            Assert.That(protocol.ScopeDisposed, Is.True);
            Assert.That(protocol.Faulted, Is.False);
            Assert.That(state.TryCanCommitSelectedP12SoloTravelStartMutation(
                new[] { changed }, out bool inactiveHandled), Is.True);
            Assert.That(inactiveHandled, Is.False);
        }
        finally
        {
            Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        }
    }

    [Test]
    public void CoreRechecksTravelAndSpatialOwnerBaselinesAcrossTheHostVectorPreflight()
    {
        DailyV1GenesisComposition composition = Compose();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        Assert.That(state.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out IReadOnlyList<string> sectionIds), Is.True);
        RegisterAndSealRequiredSections(state, sectionIds);
        TestProtocol protocol = new TestProtocol
        {
            DuringNextValidation = () =>
                Assert.That(actor.SpatialKnowledge.DiscoverLocation(
                    "solo-travel-preflight-drift"), Is.True)
        };

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        try
        {
            Assert.That(state.TryBeginSelectedP12SoloTravelStart(
                graph, actor, sectionIds, protocol, out IDisposable scope), Is.False);
            Assert.That(scope, Is.Null);
            Assert.That(protocol.ValidationCalls, Is.EqualTo(1));
            Assert.That(protocol.EnterCalls, Is.Zero);
            Assert.That(protocol.Faulted, Is.True);
        }
        finally
        {
            Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        }
    }

    [Test]
    public void CoreRejectsUnreceiptedTravelStateDriftBeforeHostMutationAdmission()
    {
        DailyV1GenesisComposition composition = Compose();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        Assert.That(state.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out IReadOnlyList<string> sectionIds), Is.True);
        RegisterAndSealRequiredSections(state, sectionIds);
        TestProtocol protocol = new TestProtocol();
        string travelSection = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
            12, actor.RuntimeId);
        IDisposable scope = null;

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        try
        {
            Assert.That(state.TryBeginSelectedP12SoloTravelStart(
                graph, actor, sectionIds, protocol, out scope), Is.True);
            Assert.That(actor.Travel.TryCommitState(
                "location/unreceipted-start", "city/unreceipted-start",
                "location/unreceipted-destination", "city/unreceipted-destination",
                2, 2, "route/unreceipted-start", true,
                "decision/unreceipted-start", null, true), Is.True);

            Assert.That(state.TryCanCommitSelectedP12SoloTravelStartMutation(
                new[] { travelSection }, out bool handled), Is.False);
            Assert.That(handled, Is.True);
            Assert.That(protocol.ValidationCalls, Is.EqualTo(1),
                "Core must reject its own stale TravelState witness before delegating any remaining host rows.");
            Assert.That(protocol.Faulted, Is.True);
        }
        finally
        {
            scope?.Dispose();
            Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        }
    }

    [Test]
    public void CoreRejectsUnreceiptedSpatialKnowledgeDriftBeforeHostMutationAdmission()
    {
        DailyV1GenesisComposition composition = Compose();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        Assert.That(state.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out IReadOnlyList<string> sectionIds), Is.True);
        RegisterAndSealRequiredSections(state, sectionIds);
        TestProtocol protocol = new TestProtocol();
        string locationsSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + actor.RuntimeId;
        string routesSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + actor.RuntimeId;
        IDisposable scope = null;

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        try
        {
            Assert.That(state.TryBeginSelectedP12SoloTravelStart(
                graph, actor, sectionIds, protocol, out scope), Is.True);
            Assert.That(actor.SpatialKnowledge.DiscoverRoute(
                "solo-travel-unreceipted-route"), Is.True);

            Assert.That(state.TryCanCommitSelectedP12SoloTravelStartMutation(
                new[] { locationsSection, routesSection }, out bool handled), Is.False);
            Assert.That(handled, Is.True);
            Assert.That(protocol.ValidationCalls, Is.EqualTo(1),
                "Core must reject its own stale paired SpatialKnowledge witness before host validation.");
            Assert.That(protocol.Faulted, Is.True);
        }
        finally
        {
            scope?.Dispose();
            Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        }
    }

    [Test]
    public void CoreAdvancesExactTravelAndPairedSpatialBaselinesOnlyForCommittedReceipts()
    {
        DailyV1GenesisComposition composition = Compose();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        Assert.That(state.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out IReadOnlyList<string> sectionIds), Is.True);
        RegisterAndSealRequiredSections(state, sectionIds);
        TestProtocol protocol = new TestProtocol();
        string travelSection = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
            12, actor.RuntimeId);
        string locationsSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + actor.RuntimeId;
        string routesSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + actor.RuntimeId;
        string[] spatialPair = { locationsSection, routesSection };
        long travelRevisionBefore = actor.Travel.Revision;
        long spatialRevisionBefore = actor.SpatialKnowledge.Revision;
        int locationCountBefore = actor.SpatialKnowledge.KnownLocationRuntimeIds.Count;
        int routeCountBefore = actor.SpatialKnowledge.KnownRouteRuntimeIds.Count;
        IDisposable scope = null;

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        try
        {
            Assert.That(state.TryBeginSelectedP12SoloTravelStart(
                graph, actor, sectionIds, protocol, out scope), Is.True);

            Assert.That(state.TryCanCommitSelectedP12SoloTravelStartMutation(
                new[] { travelSection }, out bool travelHandled), Is.True);
            Assert.That(travelHandled, Is.True);
            Assert.That(actor.Travel.TryCommitState(
                actor.Travel.CurrentLocationRuntimeId,
                actor.Travel.CurrentCityRuntimeId,
                "location/solo-travel-start", "city/solo-travel-start",
                2, 2, "route/solo-travel-start", true,
                "decision/solo-travel-start", null, true), Is.True);
            Assert.That(state.TryRecordSelectedP12SoloTravelStartMutation(
                new[] { travelSection }, out bool travelRecorded), Is.True);
            Assert.That(travelRecorded, Is.True);
            Assert.That(state.TryCanCommitSelectedP12SoloTravelStartMutation(
                new[] { travelSection }, out travelHandled), Is.True);
            Assert.That(travelHandled, Is.True);

            Assert.That(state.TryCanCommitSelectedP12SoloTravelStartMutation(
                spatialPair, out bool spatialHandled), Is.True);
            Assert.That(spatialHandled, Is.True);
            Assert.That(actor.SpatialKnowledge.DiscoverLocation(
                "solo-travel-start-location"), Is.True);
            Assert.That(state.TryRecordSelectedP12SoloTravelStartMutation(
                spatialPair, out bool spatialRecorded), Is.True);
            Assert.That(spatialRecorded, Is.True);

            Assert.That(state.TryCanCommitSelectedP12SoloTravelStartMutation(
                spatialPair, out spatialHandled), Is.True);
            Assert.That(spatialHandled, Is.True);
            Assert.That(actor.SpatialKnowledge.DiscoverRoute(
                "solo-travel-start-route"), Is.True);
            Assert.That(state.TryRecordSelectedP12SoloTravelStartMutation(
                spatialPair, out spatialRecorded), Is.True);
            Assert.That(spatialRecorded, Is.True);
        }
        finally
        {
            scope?.Dispose();
            Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        }

        Assert.That(actor.Travel.Revision, Is.EqualTo(travelRevisionBefore + 1L));
        Assert.That(actor.SpatialKnowledge.Revision, Is.EqualTo(spatialRevisionBefore + 2L));
        Assert.That(actor.SpatialKnowledge.KnownLocationRuntimeIds.Count,
            Is.EqualTo(locationCountBefore + 1));
        Assert.That(actor.SpatialKnowledge.KnownRouteRuntimeIds.Count,
            Is.EqualTo(routeCountBefore + 1));
        Assert.That(protocol.NotifiedSections, Is.EquivalentTo(
            new[] { travelSection, locationsSection, routesSection }));
        Assert.That(protocol.Faulted, Is.False);
    }

    [Test]
    public void CoreRejectsPartialOrMultiRevisionSpatialKnowledgeReceipts()
    {
        DailyV1GenesisComposition composition = Compose();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        Assert.That(state.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out IReadOnlyList<string> sectionIds), Is.True);
        RegisterAndSealRequiredSections(state, sectionIds);
        TestProtocol protocol = new TestProtocol();
        string locationsSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + actor.RuntimeId;
        string routesSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + actor.RuntimeId;
        IDisposable scope = null;

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        try
        {
            Assert.That(state.TryBeginSelectedP12SoloTravelStart(
                graph, actor, sectionIds, protocol, out scope), Is.True);
            Assert.That(actor.SpatialKnowledge.DiscoverLocation(
                "solo-travel-receipt-first"), Is.True);
            Assert.That(state.TryRecordSelectedP12SoloTravelStartMutation(
                new[] { locationsSection }, out bool partialHandled), Is.False);
            Assert.That(partialHandled, Is.True);
            Assert.That(protocol.Faulted, Is.True);
        }
        finally
        {
            scope?.Dispose();
            Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        }

        composition = Compose();
        state = composition.ExecutionBoundaryState;
        graph = composition.OwnerGraph;
        actor = graph.Npcs[0];
        Assert.That(state.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out sectionIds), Is.True);
        RegisterAndSealRequiredSections(state, sectionIds);
        protocol = new TestProtocol();
        locationsSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + actor.RuntimeId;
        routesSection =
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + actor.RuntimeId;
        scope = null;

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        try
        {
            Assert.That(state.TryBeginSelectedP12SoloTravelStart(
                graph, actor, sectionIds, protocol, out scope), Is.True);
            Assert.That(actor.SpatialKnowledge.DiscoverLocation(
                "solo-travel-receipt-second-a"), Is.True);
            Assert.That(actor.SpatialKnowledge.DiscoverLocation(
                "solo-travel-receipt-second-b"), Is.True);
            Assert.That(state.TryRecordSelectedP12SoloTravelStartMutation(
                new[] { locationsSection, routesSection }, out bool batchedHandled), Is.False);
            Assert.That(batchedHandled, Is.True);
            Assert.That(protocol.Faulted, Is.True);
        }
        finally
        {
            scope?.Dispose();
            Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        }
    }

    [Test]
    public void CoreFaultsSoloTravelStartWhenHostSectionSetDiffersFromExactOwnerCut()
    {
        DailyV1GenesisComposition composition = Compose();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        Assert.That(state.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out IReadOnlyList<string> sectionIds), Is.True);
        List<string> incorrect = sectionIds.ToList();
        incorrect[0] = "p12f.npc-travel-state/foreign-runtime";
        TestProtocol protocol = new TestProtocol();

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        try
        {
            Assert.That(state.TryBeginSelectedP12SoloTravelStart(
                graph, actor, incorrect, protocol, out IDisposable scope), Is.False);
            Assert.That(scope, Is.Null);
            Assert.That(protocol.EnterCalls, Is.Zero);
            Assert.That(protocol.Faulted, Is.True);
        }
        finally
        {
            Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        }
    }

    [Test]
    public void CoreFaultsSoloTravelStartWhenHostSectionSetContainsAnExtraOwner()
    {
        DailyV1GenesisComposition composition = Compose();
        DailyV1ExecutionBoundaryState state = composition.ExecutionBoundaryState;
        DailyV1SelectedRuntimeOwnerGraph graph = composition.OwnerGraph;
        DailyV1NpcRuntimeOwners actor = graph.Npcs[0];
        Assert.That(state.TryCreateSelectedP12SoloTravelStartSectionIds(
            graph, actor, out IReadOnlyList<string> sectionIds), Is.True);
        string[] withExtraOwner = sectionIds
            .Concat(new[] { "p12f.npc-travel-state/foreign-runtime" })
            .ToArray();
        TestProtocol protocol = new TestProtocol();

        Assert.That(state.TryBeginOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        try
        {
            Assert.That(state.TryBeginSelectedP12SoloTravelStart(
                graph, actor, withExtraOwner, protocol, out IDisposable scope), Is.False);
            Assert.That(scope, Is.Null);
            Assert.That(protocol.EnterCalls, Is.Zero);
            Assert.That(protocol.Faulted, Is.True);
        }
        finally
        {
            Assert.That(state.TryEndOperation(DailyV1OperationScopeKind.DailyAdvance), Is.True);
        }
    }

    private static DailyV1GenesisComposition Compose()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
            definitions,
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            out DailyV1GenesisComposition composition,
            out string diagnostic), Is.True, diagnostic);
        return composition;
    }

    private static void RegisterAndSealRequiredSections(
        DailyV1ExecutionBoundaryState state,
        IReadOnlyList<string> sectionIds)
    {
        foreach (string sectionId in sectionIds)
            Assert.That(state.TryRegisterExpectedOwnerSectionContract(
                sectionId, 1, OwnerSectionRole.Required), Is.True, sectionId);
        Assert.That(state.TrySealExpectedOwnerSectionContractInventory(), Is.True);
    }

    private sealed class TestProtocol : IDailyV1SelectedP12SoloTravelStartProtocol
    {
        private sealed class Scope : IDisposable
        {
            private readonly TestProtocol owner;
            internal Scope(TestProtocol owner) => this.owner = owner;
            public void Dispose() => owner.ScopeDisposed = true;
        }

        internal int ValidationCalls;
        internal int CapacityChecks;
        internal int EnterCalls;
        internal bool Faulted;
        internal bool ScopeDisposed;
        internal Action DuringNextValidation;
        internal IReadOnlyCollection<string> NotifiedSections;

        public bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds)
        {
            ValidationCalls++;
            Action callback = DuringNextValidation;
            DuringNextValidation = null;
            callback?.Invoke();
            return sectionIds != null;
        }

        public bool TryValidateMutationEpochCapacity()
        {
            CapacityChecks++;
            return true;
        }

        public bool TryEnterOperation(out IDisposable operationScope)
        {
            EnterCalls++;
            operationScope = new Scope(this);
            return true;
        }

        public bool NotifyCommittedMutations(IReadOnlyCollection<string> sectionIds)
        {
            NotifiedSections = sectionIds?.ToArray();
            return sectionIds != null;
        }

        public void FaultClosed() => Faulted = true;
    }
}
