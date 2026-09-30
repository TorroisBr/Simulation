using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class SpatialKnowledgeCensusTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void ProvidersArePerNpcSortedAndShareOwnerRevisionAcrossSections()
    {
        NpcRuntime second = new NpcRuntime("npc-z", null);
        NpcRuntime first = new NpcRuntime("npc-a", null);
        first.SpatialKnowledge.DiscoverLocation("location-a");
        first.SpatialKnowledge.DiscoverRoute("route-a");
        second.SpatialKnowledge.DiscoverLocation("location-z");

        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            SpatialKnowledgeCensusProvider.CreateProviders(new[] { second, first });

        Assert.That(providers.Select(provider => provider.GetCurrentCensus().SectionId), Is.EqualTo(new[]
        {
            SpatialKnowledgeCensusProvider.LocationsSectionPrefix + "npc-a",
            SpatialKnowledgeCensusProvider.RoutesSectionPrefix + "npc-a",
            SpatialKnowledgeCensusProvider.LocationsSectionPrefix + "npc-z",
            SpatialKnowledgeCensusProvider.RoutesSectionPrefix + "npc-z"
        }));

        OwnerSectionCensusWitness firstLocations = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness firstRoutes = providers[1].GetCurrentCensus();
        Assert.That(firstLocations.SchemaVersion, Is.EqualTo(SpatialKnowledgeCensusProvider.SchemaVersion));
        Assert.That(firstLocations.OwnerInstanceIdentity, Is.SameAs(first.SpatialKnowledge));
        Assert.That(firstRoutes.OwnerInstanceIdentity, Is.SameAs(first.SpatialKnowledge));
        Assert.That(firstLocations.Cardinality, Is.EqualTo(1));
        Assert.That(firstRoutes.Cardinality, Is.EqualTo(1));
        Assert.That(firstLocations.Revision, Is.EqualTo(2L));
        Assert.That(firstRoutes.Revision, Is.EqualTo(firstLocations.Revision));
        OwnerSectionCensusWitness repeatedFirstLocations = providers[0].GetCurrentCensus();
        Assert.That(repeatedFirstLocations.OwnerInstanceIdentity, Is.SameAs(firstLocations.OwnerInstanceIdentity));
        Assert.That(repeatedFirstLocations.Cardinality, Is.EqualTo(firstLocations.Cardinality));
        Assert.That(repeatedFirstLocations.Revision, Is.EqualTo(firstLocations.Revision));

        OwnerSectionCensusWitness secondLocations = providers[2].GetCurrentCensus();
        OwnerSectionCensusWitness secondRoutes = providers[3].GetCurrentCensus();
        Assert.That(secondLocations.OwnerInstanceIdentity, Is.SameAs(second.SpatialKnowledge));
        Assert.That(secondRoutes.OwnerInstanceIdentity, Is.SameAs(second.SpatialKnowledge));
        Assert.That(secondLocations.Cardinality, Is.EqualTo(1));
        Assert.That(secondRoutes.Cardinality, Is.Zero);
        Assert.That(secondLocations.Revision, Is.EqualTo(1L));
        Assert.That(secondRoutes.Revision, Is.EqualTo(secondLocations.Revision));
    }

    [Test]
    public void KnowledgeViewsAreReadOnlyLiveViewsAndDiscoveryRemainsIdempotent()
    {
        SpatialKnowledgeRuntime owner = new SpatialKnowledgeRuntime("npc-view");
        Assert.That(owner.DiscoverLocation("location-a"), Is.True);
        Assert.That(owner.DiscoverRoute("route-a"), Is.True);

        IReadOnlyList<string> locations = owner.KnownLocationRuntimeIds;
        IReadOnlyList<string> routes = owner.KnownRouteRuntimeIds;
        Assert.That(locations, Is.Not.InstanceOf<List<string>>());
        Assert.That(routes, Is.Not.InstanceOf<List<string>>());
        Assert.Throws<System.NotSupportedException>(() => ((IList<string>)locations).Add("forged-location"));
        Assert.Throws<System.NotSupportedException>(() => ((IList<string>)routes).Clear());

        Assert.That(owner.DiscoverLocation("location-a"), Is.False);
        Assert.That(owner.DiscoverRoute("route-a"), Is.False);
        Assert.That(owner.DiscoverLocation(" "), Is.False);
        Assert.That(owner.DiscoverRoute(null), Is.False);
        Assert.That(owner.DiscoverLocation("location-b"), Is.True);
        Assert.That(owner.DiscoverRoute("route-b"), Is.True);

        Assert.That(locations, Is.EqualTo(new[] { "location-a", "location-b" }));
        Assert.That(routes, Is.EqualTo(new[] { "route-a", "route-b" }));
        Assert.That(owner.Revision, Is.EqualTo(4L));
    }

    [Test]
    public void SaturatedRevisionRejectsNewKnowledgeBeforeChangingCardinality()
    {
        NpcRuntime npc = new NpcRuntime("npc-saturated", null);
        SpatialKnowledgeRuntime owner = npc.SpatialKnowledge;
        Assert.That(owner.DiscoverLocation("location-existing"), Is.True);
        typeof(SpatialKnowledgeRuntime).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(owner, long.MaxValue);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            SpatialKnowledgeCensusProvider.CreateProviders(new[] { npc });
        OwnerSectionCensusWitness locationsBefore = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness routesBefore = providers[1].GetCurrentCensus();

        Assert.That(owner.DiscoverLocation("location-new"), Is.False);
        Assert.That(owner.DiscoverRoute("route-new"), Is.False);
        Assert.That(providers[0].GetCurrentCensus().Cardinality, Is.EqualTo(locationsBefore.Cardinality));
        Assert.That(providers[1].GetCurrentCensus().Cardinality, Is.EqualTo(routesBefore.Cardinality));
        Assert.That(providers[0].GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue));
        Assert.That(providers[1].GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue));
        Assert.That(owner.KnownLocationRuntimeIds, Is.EqualTo(new[] { "location-existing" }));
        Assert.That(owner.KnownRouteRuntimeIds, Is.Empty);
    }

    [Test]
    public void DirectRosterTransactionsAddAndRemoveExactlyOnePairAndEpoch()
    {
        SimulationRuntime runtime = CreateRuntime();
        AssertCensusHealthy(runtime, 0L);
        Assert.That(runtime.SpatialKnowledgeCensusProviders, Is.Empty);

        NpcRuntime npc = new NpcRuntime("npc-direct-census", null);
        Assert.That(runtime.TryRegisterNpc(npc, out WorldNpcRegistryFailure registerFailure), Is.True,
            registerFailure.ToString());
        Assert.That(runtime.SpatialKnowledgeCensusProviders.Select(provider => provider.GetCurrentCensus().SectionId),
            Is.EqualTo(new[]
            {
                SpatialKnowledgeCensusProvider.LocationsSectionPrefix + npc.RuntimeId,
                SpatialKnowledgeCensusProvider.RoutesSectionPrefix + npc.RuntimeId
            }));
        AssertPair(runtime.SpatialKnowledgeCensusProviders, 0, npc);
        AssertCensusHealthy(runtime, 1L);

        Assert.That(runtime.TryUnregisterNpc(npc.RuntimeId, out WorldNpcRegistryFailure unregisterFailure), Is.True,
            unregisterFailure.ToString());
        Assert.That(runtime.SpatialKnowledgeCensusProviders, Is.Empty);
        AssertCensusHealthy(runtime, 2L);
    }

    [Test]
    public void DynamicNpcMembershipKeepsOrdinalPairAdjacentProviderOrdering()
    {
        SimulationRuntime runtime = CreateRuntime();
        NpcRuntime zulu = new NpcRuntime("npc-dynamic-zulu", null);
        NpcRuntime alpha = new NpcRuntime("npc-dynamic-alpha", null);

        Assert.That(runtime.TryRegisterNpc(zulu, out WorldNpcRegistryFailure zuluFailure), Is.True,
            zuluFailure.ToString());
        Assert.That(runtime.TryRegisterNpc(alpha, out WorldNpcRegistryFailure alphaFailure), Is.True,
            alphaFailure.ToString());

        Assert.That(runtime.SpatialKnowledgeCensusProviders.Select(provider => provider.GetCurrentCensus().SectionId),
            Is.EqualTo(new[]
            {
                SpatialKnowledgeCensusProvider.LocationsSectionPrefix + alpha.RuntimeId,
                SpatialKnowledgeCensusProvider.RoutesSectionPrefix + alpha.RuntimeId,
                SpatialKnowledgeCensusProvider.LocationsSectionPrefix + zulu.RuntimeId,
                SpatialKnowledgeCensusProvider.RoutesSectionPrefix + zulu.RuntimeId
            }));
        AssertPair(runtime.SpatialKnowledgeCensusProviders, 0, alpha);
        AssertPair(runtime.SpatialKnowledgeCensusProviders, 2, zulu);
        AssertCensusHealthy(runtime, 2L);
    }

    [Test]
    public void NoOpAndRejectedRosterMutationsDoNotAdvanceEpoch()
    {
        SimulationRuntime runtime = CreateRuntime();
        NpcRuntime npc = new NpcRuntime("npc-noop-census", null);
        Assert.That(runtime.TryRegisterNpc(npc, out _), Is.True);
        AssertCensusHealthy(runtime, 1L);

        Assert.That(runtime.TryRegisterNpc(null, out WorldNpcRegistryFailure nullFailure), Is.False);
        Assert.That(nullFailure, Is.EqualTo(WorldNpcRegistryFailure.InvalidNpc));
        Assert.That(runtime.TryRegisterNpc(new NpcRuntime(npc.RuntimeId, null), out WorldNpcRegistryFailure duplicateFailure), Is.False);
        Assert.That(duplicateFailure, Is.EqualTo(WorldNpcRegistryFailure.DuplicateRuntimeId));
        Assert.That(runtime.TryUnregisterNpc("npc-not-registered", out WorldNpcRegistryFailure missingFailure), Is.False);
        Assert.That(missingFailure, Is.EqualTo(WorldNpcRegistryFailure.NpcNotRegistered));
        AssertCensusHealthy(runtime, 1L);
        AssertPair(runtime.SpatialKnowledgeCensusProviders, 0, npc);
    }

    [Test]
    public void MaterializationPublishesBothPersonSectionsAndDynamicPairInOneEpoch()
    {
        PersonStore store = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("person-spatial-census-materialize"));
        Assert.That(store.TryRegister(person, out _), Is.True);
        SimulationRuntime runtime = CreateRuntime(store);
        AssertCensusHealthy(runtime, 0L);

        Assert.That(runtime.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc("spatial-census-materialize"),
            "npc-spatial-census-materialize",
            null,
            0f,
            out NpcRuntime npc,
            out PersonMaterializationFailure failure), Is.True, failure.ToString());

        Assert.That(npc, Is.Not.Null);
        Assert.That(runtime.SpatialKnowledgeCensusProviders, Has.Count.EqualTo(2));
        AssertPair(runtime.SpatialKnowledgeCensusProviders, 0, npc);
        AssertPersonWitnesses(runtime, 1, 1, store.Revision);
        AssertCensusHealthy(runtime, 1L);
    }

    [Test]
    public void AdoptionSuccessNotifiesBothPersonSectionsWithoutChangingSpatialFamily()
    {
        PersonStore store = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("person-spatial-census-adopt"));
        Assert.That(store.TryRegister(person, out _), Is.True);
        NpcRuntime npc = new NpcRuntime("npc-spatial-census-adopt", null);
        SimulationRuntime runtime = CreateRuntime(store, npc);
        IOwnerSectionCensusProvider[] providersBefore = runtime.SpatialKnowledgeCensusProviders.ToArray();
        OwnerSectionCensusWitness oldLocations = providersBefore[0].GetCurrentCensus();
        OwnerSectionCensusWitness oldRoutes = providersBefore[1].GetCurrentCensus();
        AssertCensusHealthy(runtime, 0L);

        Assert.That(runtime.TryBindExistingNpcToPerson(
            person.PersonId,
            npc.RuntimeId,
            out PersonMaterializationFailure failure), Is.True, failure.ToString());

        Assert.That(runtime.SpatialKnowledgeCensusProviders.Select(provider => provider.GetCurrentCensus().SectionId),
            Is.EqualTo(providersBefore.Select(provider => provider.GetCurrentCensus().SectionId)));
        Assert.That(runtime.SpatialKnowledgeCensusProviders[0].GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(oldLocations.OwnerInstanceIdentity));
        Assert.That(runtime.SpatialKnowledgeCensusProviders[1].GetCurrentCensus().OwnerInstanceIdentity,
            Is.SameAs(oldRoutes.OwnerInstanceIdentity));
        AssertPersonWitnesses(runtime, 1, 1, store.Revision);
        AssertCensusHealthy(runtime, 1L);
    }

    [Test]
    public void AdoptionResidenceConflictIsRejectedBeforeStoreWriteAndKeepsPair()
    {
        PersonStore store = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("person-spatial-census-adopt-conflict"));
        typeof(PersonRuntime).GetMethod("TrySetResidenceSettlementRuntimeId", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(person, new object[] { "settlement-person" });
        Assert.That(store.TryRegister(person, out _), Is.True);
        NpcRuntime npc = new NpcRuntime("npc-spatial-census-adopt-conflict", null);
        typeof(NpcRuntime).GetField("residenceSettlementRuntimeId", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(npc, "settlement-npc");
        SimulationRuntime runtime = CreateRuntime(store, npc);
        AssertCensusHealthy(runtime, 0L);

        Assert.That(runtime.TryBindExistingNpcToPerson(
            person.PersonId,
            npc.RuntimeId,
            out PersonMaterializationFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(PersonMaterializationFailure.ResidenceConflict));
        Assert.That(store.Revision, Is.EqualTo(1L), "The existing conflict is rejected before PersonStore binding.");
        Assert.That(person.IsMaterialized, Is.False);
        AssertPair(runtime.SpatialKnowledgeCensusProviders, 0, npc);
        AssertCensusHealthy(runtime, 0L);
    }

    [Test]
    public void DeathAndEmigrationRetainRosterCensusPairUntilExplicitUnregistration()
    {
        CityRuntime city = SimulationTestFactory.CreateCity("spatial-census-city", "spatial-census-location");
        NpcRuntime npc = new NpcRuntime("npc-spatial-census-lifecycle", null);
        SimulationRuntime runtime = CreateRuntime(null, npc, city);
        AssertCensusHealthy(runtime, 0L);

        Assert.That(runtime.TryApplyImmigration(
            npc, city, out _, out NpcPopulationLifecycleFailure immigrationFailure), Is.True,
            immigrationFailure.ToString());
        AssertPair(runtime.SpatialKnowledgeCensusProviders, 0, npc);
        Assert.That(runtime.TryApplyEmigration(
            npc, city, out _, out NpcPopulationLifecycleFailure emigrationFailure), Is.True,
            emigrationFailure.ToString());
        AssertPair(runtime.SpatialKnowledgeCensusProviders, 0, npc);
        Assert.That(runtime.TryApplyImmigration(
            npc, city, out _, out immigrationFailure), Is.True, immigrationFailure.ToString());
        Assert.That(runtime.TryApplyResidentDeath(
            npc, city, out _, out NpcPopulationLifecycleFailure deathFailure), Is.True,
            deathFailure.ToString());
        Assert.That(npc.IsDead, Is.True);
        AssertPair(runtime.SpatialKnowledgeCensusProviders, 0, npc);
        AssertCensusHealthy(runtime, 0L);
    }

    [Test]
    public void ReusingRuntimeIdAfterExplicitRemovalSeedsNewExactOwner()
    {
        SimulationRuntime runtime = CreateRuntime();
        NpcRuntime first = new NpcRuntime("npc-replacement-census", null);
        Assert.That(runtime.TryRegisterNpc(first, out _), Is.True);
        object originalOwner = runtime.SpatialKnowledgeCensusProviders[0].GetCurrentCensus().OwnerInstanceIdentity;

        Assert.That(runtime.TryUnregisterNpc(first.RuntimeId, out _), Is.True);
        NpcRuntime replacement = new NpcRuntime(first.RuntimeId, null);
        Assert.That(runtime.TryRegisterNpc(replacement, out _), Is.True);

        Assert.That(runtime.SpatialKnowledgeCensusProviders, Has.Count.EqualTo(2));
        AssertPair(runtime.SpatialKnowledgeCensusProviders, 0, replacement);
        Assert.That(runtime.SpatialKnowledgeCensusProviders[0].GetCurrentCensus().OwnerInstanceIdentity,
            Is.Not.SameAs(originalOwner));
        AssertCensusHealthy(runtime, 3L);
    }

    [Test]
    public void UnreconciledRosterChangeFailsClosedAndRetainsLastPublishedSnapshot()
    {
        SimulationRuntime runtime = CreateRuntime();
        NpcRuntime missed = new NpcRuntime("npc-missed-reconciliation", null);
        object roster = typeof(SimulationRuntime).GetField("npcRuntimes", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);
        ((List<NpcRuntime>)roster).Add(missed);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(runtime.SpatialKnowledgeCensusProviders, Is.Empty,
            "A missed boundary does not publish a new provider array.");
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long epoch, out ContinuationCensusFailure epochFailure), Is.False);
        Assert.That(epoch, Is.Zero);
        Assert.That(epochFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void UntrackedPersonStoreWriterMakesPartialCensusFailClosed()
    {
        SimulationRuntime runtime = CreateRuntime();
        Assert.That(runtime.PersonStore.TryRegister(
            new PersonRuntime(new PersonId("person-untracked-census-writer")), out _), Is.True);

        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long epoch, out ContinuationCensusFailure epochFailure), Is.False);
        Assert.That(epoch, Is.Zero);
        Assert.That(epochFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void UnexpectedPersonStoreRevisionDriftInsideMembershipScopeFailsClosed()
    {
        PersonStore store = new PersonStore();
        SimulationRuntime runtime = CreateRuntime(store);
        IReadOnlyList<IOwnerSectionCensusProvider> providersBefore = runtime.SpatialKnowledgeCensusProviders;
        IDisposable scope = (IDisposable)typeof(SimulationRuntime).GetMethod(
                "BeginNpcMembershipCensusScope", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(runtime, null);

        Assert.That(store.TryRegister(
            new PersonRuntime(new PersonId("person-unexpected-in-scope-census-writer")), out _), Is.True);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure activeFailure), Is.False);
        Assert.That(activeFailure, Is.EqualTo(ContinuationCensusFailure.OperationInProgress));

        scope.Dispose();
        Assert.That(runtime.SpatialKnowledgeCensusProviders, Is.SameAs(providersBefore));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epoch, out ContinuationCensusFailure epochFailure), Is.False);
        Assert.That(epoch, Is.Zero);
        Assert.That(epochFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void CrossThreadNestedMembershipEntryCannotJoinOrReconcileOwnerContext()
    {
        SimulationRuntime runtime = CreateRuntime();
        IReadOnlyList<IOwnerSectionCensusProvider> providersBefore = runtime.SpatialKnowledgeCensusProviders;
        IDisposable ownerScope = (IDisposable)typeof(SimulationRuntime).GetMethod(
                "BeginNpcMembershipCensusScope", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(runtime, null);
        Exception workerFailure = null;
        Thread worker = new Thread(() =>
        {
            try
            {
                runtime.TryRegisterNpc(new NpcRuntime("npc-cross-thread-census", null), out _);
            }
            catch (Exception exception)
            {
                workerFailure = exception;
            }
        });

        worker.Start();
        worker.Join();
        ownerScope.Dispose();

        Assert.That(workerFailure, Is.Null);
        Assert.That(runtime.SpatialKnowledgeCensusProviders, Is.SameAs(providersBefore),
            "A mismatched nested entry cannot publish through the live owner context.");
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epoch, out ContinuationCensusFailure epochFailure), Is.False);
        Assert.That(epoch, Is.Zero);
        Assert.That(epochFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void FailedCompensationHandlerFaultsRuntimeAndBlocksCensusAssessment()
    {
        SimulationRuntime runtime = CreateRuntime();
        typeof(SimulationRuntime).GetMethod(
            "MarkNpcMembershipCensusCompensationFailed", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(runtime, null);

        Assert.That(runtime.IsMutationFaulted, Is.True);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    private static SimulationRuntime CreateRuntime(
        PersonStore personStore = null,
        NpcRuntime npc = null,
        CityRuntime city = null)
    {
        return new SimulationRuntime(
            new SimulationTime(),
            city != null ? new[] { city } : null,
            npc != null ? new[] { npc } : null,
            economyEnabled: false,
            personStore: personStore);
    }

    private static void AssertPair(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        int index,
        NpcRuntime npc)
    {
        Assert.That(providers[index].GetCurrentCensus().SectionId,
            Is.EqualTo(SpatialKnowledgeCensusProvider.LocationsSectionPrefix + npc.RuntimeId));
        Assert.That(providers[index + 1].GetCurrentCensus().SectionId,
            Is.EqualTo(SpatialKnowledgeCensusProvider.RoutesSectionPrefix + npc.RuntimeId));
        Assert.That(providers[index].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(npc.SpatialKnowledge));
        Assert.That(providers[index + 1].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(npc.SpatialKnowledge));
    }

    private static void AssertPersonWitnesses(SimulationRuntime runtime, int persons, int bindings, long revision)
    {
        IReadOnlyList<IOwnerSectionCensusProvider> providers = PersonStoreCensusProvider.CreateProviders(runtime.PersonStore);
        Assert.That(providers[0].GetCurrentCensus().Cardinality, Is.EqualTo(persons));
        Assert.That(providers[1].GetCurrentCensus().Cardinality, Is.EqualTo(bindings));
        Assert.That(providers[0].GetCurrentCensus().Revision, Is.EqualTo(revision));
        Assert.That(providers[1].GetCurrentCensus().Revision, Is.EqualTo(revision));
    }

    private static void AssertCensusHealthy(SimulationRuntime runtime, long expectedEpoch)
    {
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure assessmentFailure), Is.True,
            assessmentFailure.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epoch, out ContinuationCensusFailure epochFailure), Is.True, epochFailure.ToString());
        Assert.That(epoch, Is.EqualTo(expectedEpoch));
    }
}
