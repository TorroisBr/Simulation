using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class P12EDailyV1OwnerPackageTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void TryCaptureAndStage_ComposesAcceptedEmptyOwnerSetWithoutPublishingOrMutatingInputs()
    {
        DailyFixture fixture = CreateDailyFixture("p12e-package-success");
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        Assert.That(TryCreateContext(fixture, token, new SimulationTime(token.AbsoluteDay),
            out P12EDailyV1OwnerStagingContext context), Is.True);

        long sourcePersonRevision = fixture.Runtime.PersonStore.Revision;
        long sourceFactionRevision = FindSection(
            token.OwnerSections, FactionStoreCensusProvider.FactionsSectionId).Revision;
        long stagedPersonRevision = context.P12DPackage.Persons.Revision;
        long stagedSpatialRevision = context.P12CRoots.SpatialAuthority.Revision;
        Assert.That(P12EDailyV1OwnerPackage.TryCaptureAndStage(
            fixture.Runtime, token, token.OwnerSections, context,
            out P12EDailyV1OwnerPackage package,
            out P12EDailyV1OwnerPackageFailure failure), Is.True, failure.ToString());

        Assert.That(package, Is.Not.Null);
        Assert.That(package.WorldId.Value, Is.EqualTo(token.WorldId.Value));
        Assert.That(package.Institutions, Is.Not.Null);
        Assert.That(package.Offices, Is.Not.Null);
        Assert.That(package.PropertyOwnership, Is.Not.Null);
        Assert.That(package.Estates, Is.Not.Null);
        Assert.That(package.Factions, Is.Not.Null);
        Assert.That(package.PoliticalClaims, Is.Not.Null);
        Assert.That(package.PoliticalSupport, Is.Not.Null);
        Assert.That(package.PoliticalDecisions, Is.Not.Null);
        Assert.That(package.ArmedForces, Is.Not.Null);
        Assert.That(package.ContingentManpower, Is.Not.Null);
        Assert.That(package.ArmedForcePositions, Is.Not.Null);
        Assert.That(package.Conflicts, Is.Not.Null);
        Assert.That(package.Wars, Is.Not.Null);
        Assert.That(package.Battles, Is.Not.Null);
        Assert.That(package.Justice, Is.Not.Null);
        Assert.That(package.CrimeSocialAppraisal, Is.Not.Null);
        Assert.That(package.CrimeSocialAppraisal.SimulationTime, Is.SameAs(context.StagedSimulationTime));
        Assert.That(package.CrimeSocialAppraisal.SimulationTime.AbsoluteDay, Is.EqualTo(token.AbsoluteDay));
        Assert.That(package.PoliticalDecisions.Count, Is.Zero,
            "The accepted Daily-v1 profile admits the exact-zero PoliticalDecision owner only.");
        Assert.That(package.Factions.Revision, Is.EqualTo(sourceFactionRevision));
        Assert.That(package.PropertyOwnership.Revision, Is.EqualTo(FindSection(
            token.OwnerSections, PropertyOwnershipCensusProvider.OwnershipSectionId).Revision));
        Assert.That(package.Estates.Revision, Is.EqualTo(FindSection(
            token.OwnerSections, EstateCensusProvider.SectionId).Revision));
        Assert.That(package.ArmedForces.Revision, Is.EqualTo(FindSection(
            token.OwnerSections, ArmedForceStoreCensusProvider.ForcesSectionId).Revision));
        Assert.That(package.Conflicts.Revision, Is.EqualTo(FindSection(
            token.OwnerSections, PersistentConflictCensusProvider.SectionId).Revision));
        Assert.That(package.Wars.Revision, Is.EqualTo(FindSection(
            token.OwnerSections, PersistentWarCensusProvider.SectionId).Revision));
        Assert.That(package.Battles.Revision, Is.EqualTo(FindSection(
            token.OwnerSections, PersistentBattleCensusProvider.SectionId).Revision));
        Assert.That(package.UnresolvedPoliticalKnowledgeBindings, Is.Empty);
        Assert.That(context.P12DPackage.EmptyExplorableSites.Count, Is.Zero);
        Assert.That(fixture.Runtime.LocalTopologyStore, Is.Null);
        Assert.That(fixture.Runtime.PersonStore.Revision, Is.EqualTo(sourcePersonRevision));
        Assert.That(FindSection(token.OwnerSections, FactionStoreCensusProvider.FactionsSectionId).Revision,
            Is.EqualTo(sourceFactionRevision));
        Assert.That(context.P12DPackage.Persons.Revision, Is.EqualTo(stagedPersonRevision));
        Assert.That(context.P12CRoots.SpatialAuthority.Revision, Is.EqualTo(stagedSpatialRevision));

        Assert.That(P12EDailyV1OwnerPackage.TryCaptureAndStage(
            fixture.Runtime, token, token.OwnerSections, context,
            out P12EDailyV1OwnerPackage repeated,
            out P12EDailyV1OwnerPackageFailure repeatedFailure), Is.True, repeatedFailure.ToString());
        Assert.That(repeated.Factions.Revision, Is.EqualTo(package.Factions.Revision));
        Assert.That(repeated.ArmedForces.Revision, Is.EqualTo(package.ArmedForces.Revision));
        Assert.That(repeated.CrimeSocialAppraisal.SimulationTime, Is.SameAs(context.StagedSimulationTime));
    }

    [Test]
    public void TryCaptureAndStage_RejectsDifferentTokenVectorAndWrongStagedDayWithoutPackage()
    {
        DailyFixture fixture = CreateDailyFixture("p12e-package-context");
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        Assert.That(TryCreateContext(fixture, token, new SimulationTime(token.AbsoluteDay),
            out P12EDailyV1OwnerStagingContext validContext), Is.True);

        IReadOnlyList<OwnerSectionCensusSnapshot> copiedVector =
            Array.AsReadOnly(token.OwnerSections.ToArray());
        Assert.That(P12EDailyV1OwnerPackage.TryCaptureAndStage(
            fixture.Runtime, token, copiedVector, validContext,
            out P12EDailyV1OwnerPackage wrongVectorPackage,
            out P12EDailyV1OwnerPackageFailure wrongVectorFailure), Is.False);
        Assert.That(wrongVectorPackage, Is.Null);
        Assert.That(wrongVectorFailure, Is.EqualTo(P12EDailyV1OwnerPackageFailure.InvalidCaptureContext));

        Assert.That(TryCreateContext(fixture, token, new SimulationTime(token.AbsoluteDay + 1L),
            out P12EDailyV1OwnerStagingContext wrongDayContext), Is.True);
        Assert.That(P12EDailyV1OwnerPackage.TryCaptureAndStage(
            fixture.Runtime, token, token.OwnerSections, wrongDayContext,
            out P12EDailyV1OwnerPackage wrongDayPackage,
            out P12EDailyV1OwnerPackageFailure wrongDayFailure), Is.False);
        Assert.That(wrongDayPackage, Is.Null);
        Assert.That(wrongDayFailure, Is.EqualTo(P12EDailyV1OwnerPackageFailure.InvalidCaptureContext));

        DailyFixture otherFixture = CreateDailyFixture("p12e-package-other-world");
        DailyCaptureEligibilityToken otherToken = CompleteDailyBoundary(otherFixture.Runtime);
        Assert.That(TryCreateContext(otherFixture, otherToken, new SimulationTime(token.AbsoluteDay),
            out P12EDailyV1OwnerStagingContext otherContext), Is.True);
        P12EDailyV1OwnerStagingContext mismatchedWorldContext = new P12EDailyV1OwnerStagingContext(
            validContext.P12CRoots, otherContext.P12DPackage, new SimulationTime(token.AbsoluteDay),
            validContext.FreeStatus, validContext.WantedStatus,
            validContext.ArrestedStatus, validContext.HiddenStatus,
            validContext.DomainEventRecorder, validContext.Logger);
        Assert.That(P12EDailyV1OwnerPackage.TryCaptureAndStage(
            fixture.Runtime, token, token.OwnerSections, mismatchedWorldContext,
            out P12EDailyV1OwnerPackage mismatchedWorldPackage,
            out P12EDailyV1OwnerPackageFailure mismatchedWorldFailure), Is.False);
        Assert.That(mismatchedWorldPackage, Is.Null);
        Assert.That(mismatchedWorldFailure, Is.EqualTo(P12EDailyV1OwnerPackageFailure.InvalidCaptureContext));
    }

    [Test]
    public void TryCaptureAndStage_RejectsTokenAfterSourceMutationAndReturnsNoPackage()
    {
        DailyFixture fixture = CreateDailyFixture("p12e-package-stale");
        DailyCaptureEligibilityToken token = CompleteDailyBoundary(fixture.Runtime);
        Assert.That(TryCreateContext(fixture, token, new SimulationTime(token.AbsoluteDay),
            out P12EDailyV1OwnerStagingContext context), Is.True);
        long stagedPersonRevision = context.P12DPackage.Persons.Revision;

        Assert.That(fixture.Runtime.TryRegisterPerson(
            new PersonRuntime(new PersonId("p12e-package-after-boundary-person"), token.AbsoluteDay),
            out PersonStoreFailure personFailure), Is.True, personFailure.ToString());

        Assert.That(P12EDailyV1OwnerPackage.TryCaptureAndStage(
            fixture.Runtime, token, token.OwnerSections, context,
            out P12EDailyV1OwnerPackage package,
            out P12EDailyV1OwnerPackageFailure failure), Is.False);
        Assert.That(package, Is.Null);
        Assert.That(failure, Is.EqualTo(P12EDailyV1OwnerPackageFailure.InvalidCaptureContext));
        Assert.That(context.P12DPackage.Persons.Revision, Is.EqualTo(stagedPersonRevision));
    }

    [Test]
    public void UnresolvedBinding_CopiesTypedDecisionIdentityOrderedKnowledgeReferencesAndRevisions()
    {
        P12EPoliticalDecisionSnapshotRow row = new P12EPoliticalDecisionSnapshotRow(
            "p12e-binding-decision", 0, "holder", 0,
            Array.Empty<string>(), 0, null, null, 4L, 5L,
            Array.Empty<string>(), new[] { "knowledge-b", "knowledge-a" },
            17L, 23L, null, null);

        P12EUnresolvedPoliticalKnowledgeBinding binding =
            new P12EUnresolvedPoliticalKnowledgeBinding(row);

        Assert.That(binding.DecisionId.Value, Is.EqualTo("p12e-binding-decision"));
        Assert.That(binding.KnowledgeReferences, Is.EqualTo(new[] { "knowledge-b", "knowledge-a" }));
        Assert.That(binding.ExpectedKnowledgeRevision, Is.EqualTo(23L));
        Assert.That(binding.ExpectedWorldRevision, Is.EqualTo(17L));
    }

    private sealed class DailyFixture
    {
        internal SimulationRuntime Runtime;
        internal CityRuntime[] Cities;
        internal NpcRuntime[] Npcs;
        internal ExplorableSiteStore Sites;
        internal RuntimeIdentityRegistry Identities;
        internal SpatialNetworkRuntime Network;
        internal NpcStatusData FreeStatus;
        internal NpcStatusData WantedStatus;
        internal NpcStatusData ArrestedStatus;
        internal NpcStatusData HiddenStatus;
        internal SimulationLogger Logger;
    }

    private static DailyFixture CreateDailyFixture(string prefix)
    {
        CityRuntime[] cities =
        {
            SimulationTestFactory.CreateCity(prefix + "-city-a", prefix + "-location-a"),
            SimulationTestFactory.CreateCity(prefix + "-city-b", prefix + "-location-b")
        };
        RuntimeIdentityRegistry identities = new RuntimeIdentityRegistry();
        foreach (CityRuntime city in cities)
            Assert.That(identities.RegisterCity(city), Is.True);

        NpcRuntime[] npcs = new NpcRuntime[10];
        npcs[0] = new NpcRuntime(prefix + "-npc-0",
            SimulationTestFactory.CreateNpc(prefix + "-npc-definition-0"), cities[0], 0f);
        Assert.That(identities.RegisterNpc(npcs[0]), Is.True);
        for (int i = 1; i < npcs.Length; i++)
        {
            npcs[i] = new NpcRuntime(prefix + "-npc-" + i,
                SimulationTestFactory.CreateNpc(prefix + "-npc-definition-" + i));
            Assert.That(identities.RegisterNpc(npcs[i]), Is.True);
        }

        SpatialNetworkRuntime network = new SpatialNetworkRuntime(identities);
        foreach (CityRuntime city in cities)
            Assert.That(network.RegisterLocation(city.Location), Is.True);
        Assert.That(network.RegisterRoute(new SpatialRouteRuntime(
            prefix + "-route-a", cities[0].Location, cities[1].Location, 1)), Is.True);
        Assert.That(network.RegisterRoute(new SpatialRouteRuntime(
            prefix + "-route-b", cities[1].Location, cities[0].Location, 1)), Is.True);

        NpcStatusData freeStatus = SimulationTestFactory.CreateStatus(prefix + "-free");
        NpcStatusData wantedStatus = SimulationTestFactory.CreateStatus(prefix + "-wanted");
        NpcStatusData arrestedStatus = SimulationTestFactory.CreateStatus(prefix + "-arrested");
        NpcStatusData hiddenStatus = SimulationTestFactory.CreateStatus(prefix + "-hidden");
        SimulationLogger logger = new SimulationLogger(null);
        JusticeSystem justice = new JusticeSystem(
            freeStatus, wantedStatus, arrestedStatus, hiddenStatus, null, logger);
        ExplorableSiteStore sites = new ExplorableSiteStore();
        SpatialAuthorityStore authority = CreateSpatialAuthority(prefix);
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(), cities, npcs,
            economyEnabled: false,
            justiceSystem: justice,
            logger: logger,
            explorableSiteStore: sites,
            spatialAuthorityStore: authority,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            runtimeIdentityRegistry: identities,
            spatialNetworkRuntime: network,
            requireP12RuntimeIdentitySpatialCensusOwners: true,
            worldId: new WorldId(Guid.NewGuid()));

        return new DailyFixture
        {
            Runtime = runtime,
            Cities = cities,
            Npcs = npcs,
            Sites = sites,
            Identities = identities,
            Network = network,
            FreeStatus = freeStatus,
            WantedStatus = wantedStatus,
            ArrestedStatus = arrestedStatus,
            HiddenStatus = hiddenStatus,
            Logger = logger
        };
    }

    private static bool TryCreateContext(
        DailyFixture fixture,
        DailyCaptureEligibilityToken token,
        SimulationTime stagedTime,
        out P12EDailyV1OwnerStagingContext context)
    {
        context = null;
        if (!P12CWorldIdentitySnapshot.TryCapture(
                token.WorldId, out P12CWorldIdentitySnapshot identitySnapshot, out _)
            || !identitySnapshot.TryStage(out WorldId stagedWorldId, out _)
            || !P12CSpatialAuthoritySnapshot.TryCapture(
                SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
                SimulationGenesisPipeline.GeographyProfileContractIdentity,
                2,
                fixture.Runtime.SpatialAuthorityStore,
                out P12CSpatialAuthoritySnapshot spatialSnapshot, out _)
            || !P12CSpatialAuthoritySnapshot.TryCreateStagedFromSnapshot(
                spatialSnapshot, out SpatialAuthorityStore stagedSpatialAuthority, out _))
            return false;

        P12CStagedContinuationRoot stagedC = new P12CStagedContinuationRoot(
            stagedWorldId, null, null, stagedSpatialAuthority, null, null);
        if (!P12DDailyV1OwnerPackage.TryCaptureAndStage(
                fixture.Runtime, token, new object(), token.OwnerSections,
                fixture.Identities, fixture.Network, fixture.Sites,
                new RuntimeIdentityRegistry(), stagedWorldId,
                fixture.Cities.Select(city => city.CityData).ToArray(),
                Array.Empty<ItemData>(),
                fixture.Npcs.Select(npc => npc.NpcData).ToArray(),
                Array.Empty<NpcActionData>(), Array.Empty<NpcStatusData>(),
                Array.Empty<ExplorableSiteData>(), Array.Empty<string>(),
                out P12DDailyV1OwnerPackage stagedD, out _))
            return false;

        context = new P12EDailyV1OwnerStagingContext(
            stagedC, stagedD, stagedTime,
            fixture.FreeStatus, fixture.WantedStatus,
            fixture.ArrestedStatus, fixture.HiddenStatus,
            null, fixture.Logger);
        return true;
    }

    private static SpatialAuthorityStore CreateSpatialAuthority(string prefix)
    {
        SpatialGeographyDefinition geography = new SpatialGeographyDefinition(
            new SpatialWorldScaleContext(prefix + "-scale", "fixture", "v1", 1m, "step"),
            new[]
            {
                new HexRecord(new HexId(prefix + "-hex"), new HexCoordinate(0, 0),
                    new TerrainReference(new TerrainDefinitionId("terrain." + prefix), "v1"))
            },
            new[] { new LocationRecord(new LocationId(prefix + "-p8-location"), new HexId(prefix + "-hex")) });
        SpatialAuthorityStore authority = new SpatialAuthorityStore();
        if (!authority.TryComposeGeography(geography, out SpatialAuthorityFailure failure))
            throw new InvalidOperationException("Could not create the P12-E P8 fixture: " + failure);
        return authority;
    }

    private static DailyCaptureEligibilityToken CompleteDailyBoundary(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryMarkWorldPublishedForFactualRead(), Is.True);
        Assert.That(runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure advanceFailure), Is.True,
            advanceFailure.ToString());
        Assert.That(runtime.TryGetCompletedDailyCaptureToken(
            out DailyCaptureEligibilityToken token,
            out DailyCaptureEligibilityFailure tokenFailure), Is.True, tokenFailure.ToString());
        return token;
    }

    private static OwnerSectionCensusSnapshot FindSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId)
    {
        OwnerSectionCensusSnapshot match = null;
        foreach (OwnerSectionCensusSnapshot section in sections)
        {
            if (section == null || !string.Equals(section.SectionId, sectionId, StringComparison.Ordinal))
                continue;
            if (match != null) throw new InvalidOperationException("Duplicate section: " + sectionId);
            match = section;
        }
        if (match == null) throw new InvalidOperationException("Missing section: " + sectionId);
        return match;
    }
}
