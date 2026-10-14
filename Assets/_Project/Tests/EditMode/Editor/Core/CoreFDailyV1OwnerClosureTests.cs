using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CoreFDailyV1OwnerClosureTests
{
    private GameObject simulationObject;

    [TearDown]
    public void TearDown()
    {
        if (simulationObject != null) Object.DestroyImmediate(simulationObject);
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void SelectedDailyV1OwnerVectorAndSupportedNpcTransitionStayBoundToTheLiveProtocol()
    {
        SimulationConfigData configuration = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-DailyV1.asset");
        Assert.That(configuration, Is.Not.Null);
        Assert.That(configuration.useAuthoredGeographyProfile, Is.True);
        Assert.That(configuration.authoredP10RuinSite, Is.Null);

        simulationObject = new GameObject("core-f-daily-v1-owner-closure");
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, configuration);
        typeof(TesteSimulacao).GetField("runtimeAdmissionProfile", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1);
        simulation.Start();

        SimulationRuntime runtime = simulation.Bootstrap.Runtime;
        Assert.That(runtime, Is.Not.Null);
        Assert.That(runtime.Cities, Has.Count.EqualTo(2));
        Assert.That(runtime.NpcRuntimes, Has.Count.EqualTo(10));
        ContinuationCensusProtocol protocol = (ContinuationCensusProtocol)typeof(SimulationRuntime)
            .GetField("npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(runtime);
        Assert.That(protocol, Is.Not.Null);

        IReadOnlyCollection<string> operations = CoreFDailyV1OwnerClosureVectors.ReadSealedOperationIds(protocol);
        Assert.That(operations, Is.EquivalentTo(CoreFDailyV1OwnerClosureVectors.ExpectedConditionalOperationIds),
            "The selected composition includes the solo and group travel services, so all 24 source-registered operations apply.");

        IReadOnlyList<OwnerSectionCensusSnapshot> initial =
            CoreFDailyV1OwnerClosureVectors.Capture(protocol, out P12ADailyV1Census initialCensus);
        Assert.That(initial, Has.Count.EqualTo(CoreFDailyV1OwnerClosureVectors.ExpectedSectionCount(
            runtime.NpcRuntimes.Count, 0, runtime.PersonStore.Persons.Count, runtime.Cities.Count)));
        Assert.That(initial, Has.Count.EqualTo(299), "61 + 22*N + U + P + 4*C, with N=10, U=10, P=0, C=2.");
        Assert.That(initial.Select(row => row.SectionId), Is.Ordered.Using<string>(System.StringComparer.Ordinal));
        Assert.That(initial.Select(row => row.SectionId).Distinct(System.StringComparer.Ordinal), Has.Count.EqualTo(initial.Count));
        Assert.That(initial.All(row => row.Role == OwnerSectionRole.Required
            || row.Role == OwnerSectionRole.ExplicitlyEmpty), Is.True);
        CoreFDailyV1OwnerClosureVectors.AssertAliasGroupsMatchReferences(initial, initialCensus);

        AssertOwnerAlias(initial,
            RuntimeIdentityRegistryCensusProvider.NpcsSectionId,
            RuntimeIdentityRegistryCensusProvider.CitiesSectionId,
            RuntimeIdentityRegistryCensusProvider.LocationsSectionId,
            RuntimeIdentityRegistryCensusProvider.RoutesSectionId,
            RuntimeIdentityRegistryCensusProvider.ExplorableSitesSectionId,
            RuntimeIdentityRegistryCensusProvider.LocalPlacesSectionId,
            RuntimeIdentityRegistryCensusProvider.LocalConnectionsSectionId,
            RuntimeIdentityRegistryCensusProvider.NotableItemsSectionId);

        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken completedToken,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());
        IReadOnlyList<OwnerSectionCensusSnapshot> afterAdvance =
            CoreFDailyV1OwnerClosureVectors.Capture(protocol, out P12ADailyV1Census afterAdvanceCensus);
        CoreFDailyV1OwnerClosureVectors.AssertAliasGroupsMatchReferences(afterAdvance, afterAdvanceCensus);
        CoreFDailyV1OwnerClosureVectors.AssertSameWitnesses(completedToken.OwnerSections, afterAdvance);

        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long beforeRegistrationEpoch, out _), Is.True);
        NpcRuntime added = new NpcRuntime("core-f-owner-closure-added-npc", null);
        Assert.That(runtime.TryRegisterNpc(added, out WorldNpcRegistryFailure registerFailure), Is.True,
            registerFailure.ToString());
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(out long afterRegistrationEpoch, out _), Is.True);
        Assert.That(afterRegistrationEpoch, Is.EqualTo(beforeRegistrationEpoch + 1L),
            "The reserved NPC lifecycle mutation publishes one shared mutation epoch for its registered owner batch.");
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(completedToken, out tokenFailure), Is.False,
            "A supported owner mutation makes the previous completed-boundary token stale.");
        Assert.That(tokenFailure, Is.EqualTo(DailyCaptureEligibilityFailure.StaleToken));

        IReadOnlyList<OwnerSectionCensusSnapshot> evolved =
            CoreFDailyV1OwnerClosureVectors.Capture(protocol, out P12ADailyV1Census evolvedCensus);
        Assert.That(evolved, Has.Count.EqualTo(CoreFDailyV1OwnerClosureVectors.ExpectedSectionCount(
            runtime.NpcRuntimes.Count, 0, runtime.PersonStore.Persons.Count, runtime.Cities.Count)));
        Assert.That(evolved, Has.Count.EqualTo(322), "Adding one unbound NPC contributes 22 per-NPC rows and one residence row.");
        Assert.That(evolved.Select(row => row.SectionId), Is.Not.EqualTo(initial.Select(row => row.SectionId)));
        CoreFDailyV1OwnerClosureVectors.AssertAliasGroupsMatchReferences(evolved, evolvedCensus);

        Assert.That(runtime.TryAdvanceDay(out advanceFailure), Is.True, advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(out DailyCaptureEligibilityToken refreshedToken, out tokenFailure),
            Is.True, tokenFailure.ToString());
        IReadOnlyList<OwnerSectionCensusSnapshot> refreshed =
            CoreFDailyV1OwnerClosureVectors.Capture(protocol, out P12ADailyV1Census refreshedCensus);
        CoreFDailyV1OwnerClosureVectors.AssertSameWitnesses(refreshedToken.OwnerSections, refreshed);
        CoreFDailyV1OwnerClosureVectors.AssertAliasGroupsMatchReferences(refreshed, refreshedCensus);
        Assert.That(runtime.TryValidateCompletedDailyCaptureToken(refreshedToken, out tokenFailure), Is.True,
            tokenFailure.ToString());
    }

    private static void AssertOwnerAlias(
        IReadOnlyList<OwnerSectionCensusSnapshot> rows,
        params string[] sectionIds)
    {
        object owner = rows.Single(row => row.SectionId == sectionIds[0]).OwnerInstanceIdentity;
        foreach (string sectionId in sectionIds)
            Assert.That(rows.Single(row => row.SectionId == sectionId).OwnerInstanceIdentity, Is.SameAs(owner), sectionId);
    }
}
