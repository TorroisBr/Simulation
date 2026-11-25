using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1P12FExpeditionOwnerSnapshotStagerTests
{
    [Test]
    public void StagesPopulatedExpeditionAgainstCoreSiteSpatialNpcAndPartyOwners()
    {
        Fixture fixture = CreateFixture(withSite: true, traveling: true);
        P12FExpeditionOwnerSnapshot snapshot = CreateSnapshot(
            fixture, ExpeditionState.TravelingToSite, fixture.Party.TravelPartyId);

        Assert.That(P12FExpeditionOwnerSnapshotStager.TryStage(
            snapshot, fixture.NpcRuntimeIds, fixture.NpcFRows, fixture.Graph.Npcs,
            fixture.Graph.RuntimeIdentityIndex, fixture.Graph.SpatialNetwork,
            fixture.Sites, fixture.StagedTravelParties,
            out ExpeditionStore staged, out P12FCommitmentSnapshotFailure failure),
            Is.True, failure.ToString());

        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.None));
        Assert.That(staged, Is.Not.SameAs(fixture.SourceExpeditions));
        Assert.That(staged.Revision, Is.EqualTo(snapshot.Revision));
        ExpeditionRuntime expedition = staged.GetById("expedition/core-stager");
        Assert.That(expedition, Is.Not.Null);
        Assert.That(expedition.State, Is.EqualTo(ExpeditionState.TravelingToSite));
        Assert.That(expedition.MemberRuntimeIds, Is.EqualTo(new[] { fixture.Member.RuntimeId }));
    }

    [Test]
    public void RejectsPopulatedExpeditionWhenAcceptedDailyProfileHasExactEmptySiteRoots()
    {
        Fixture fixture = CreateFixture(withSite: false, traveling: false);
        P12FExpeditionOwnerSnapshot snapshot = CreateSnapshot(fixture, ExpeditionState.AtSite, null);

        Assert.That(P12FExpeditionOwnerSnapshotStager.TryStage(
            snapshot, fixture.NpcRuntimeIds, fixture.NpcFRows, fixture.Graph.Npcs,
            fixture.Graph.RuntimeIdentityIndex, fixture.Graph.SpatialNetwork,
            fixture.Sites, fixture.StagedTravelParties,
            out ExpeditionStore staged, out P12FCommitmentSnapshotFailure failure), Is.False);

        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidIdentityBinding));
        Assert.That(fixture.Sites.Count, Is.Zero);
        Assert.That(fixture.Graph.RuntimeIdentityIndex.GetCardinality(
            DailyV1RuntimeIdentityKind.ExplorableSite), Is.Zero);
    }

    [Test]
    public void RejectsTravelingExpeditionWithoutExactStagedTravelParty()
    {
        Fixture fixture = CreateFixture(withSite: true, traveling: true);
        P12FExpeditionOwnerSnapshot snapshot = CreateSnapshot(
            fixture, ExpeditionState.TravelingToSite, fixture.Party.TravelPartyId);

        Assert.That(P12FExpeditionOwnerSnapshotStager.TryStage(
            snapshot, fixture.NpcRuntimeIds, fixture.NpcFRows, fixture.Graph.Npcs,
            fixture.Graph.RuntimeIdentityIndex, fixture.Graph.SpatialNetwork,
            fixture.Sites, new TravelPartyStore(),
            out ExpeditionStore staged, out P12FCommitmentSnapshotFailure failure), Is.False);

        Assert.That(staged, Is.Null);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidNpcBinding));
    }

    private static P12FExpeditionOwnerSnapshot CreateSnapshot(
        Fixture fixture, ExpeditionState state, string partyId)
    {
        P12FExpeditionOwnerSnapshotRecord record = new P12FExpeditionOwnerSnapshotRecord(
            "expedition/core-stager",
            fixture.SiteRuntimeId,
            fixture.Route.Origin.RuntimeId,
            fixture.Route.Destination.RuntimeId,
            fixture.Route.RuntimeId,
            partyId,
            "decision/core-stager",
            new[] { fixture.Member.RuntimeId },
            new[] { fixture.Member.RuntimeId },
            Array.Empty<string>(),
            state,
            null,
            Array.Empty<string>(),
            Array.Empty<string>(),
            ExpeditionObjectiveType.Explore,
            null,
            null,
            null,
            1,
            0,
            false,
            true);
        return new P12FExpeditionOwnerSnapshot(
            P12FExpeditionOwnerSnapshot.CurrentSchemaVersion, 1L, new[] { record });
    }

    private static Fixture CreateFixture(bool withSite, bool traveling)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1SelectedRuntimeOwnerGraphBuilder.TryBuildConfigured(
            definitions, new RuntimeIdAllocator(), out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        SpatialRouteRuntime route = graph.SpatialNetwork.Routes.First();
        DailyV1NpcRuntimeOwners member = graph.Npcs.First();
        DailyV1CityRuntimeOwners destination = graph.Cities.Single(city =>
            string.Equals(city.LocationRuntimeId, route.Destination.RuntimeId, StringComparison.Ordinal));

        string siteId = "site-runtime/expedition-stager";
        ExplorableSiteStore sites = new ExplorableSiteStore();
        ExplorableSiteRuntime site = null;
        if (withSite)
        {
            DailyV1ExplorableSiteDefinition siteDefinition = new DailyV1ExplorableSiteDefinition(
                "ExplorableSiteData", "site-definition/expedition-stager",
                new DailyV1ExplorableSiteData("site-definition/expedition-stager",
                    "Fixture site", new DailyV1EnumValue(0, "Ruin")));
            site = new ExplorableSiteRuntime(siteId, siteDefinition, route.Destination,
                "site-instance/expedition-stager");
            Assert.That(sites.Add(site), Is.True);
            Assert.That(graph.RuntimeIdentityIndex.TryStageRegister(
                siteId, DailyV1RuntimeIdentityKind.ExplorableSite), Is.True);
            Assert.That(graph.RuntimeIdentityIndex.TryAdvanceRevision(), Is.True);
        }

        TravelPartyRuntime party = null;
        TravelPartyStore sourceParties = new TravelPartyStore();
        TravelPartyStore stagedParties = new TravelPartyStore();
        if (traveling)
        {
            const string partyId = "party/expedition-stager";
            Assert.That(member.Travel.TryInstallState(
                null, null, route.Destination.RuntimeId, destination.RuntimeId,
                route.TravelDays, route.TravelDays, route.RuntimeId, true,
                "decision/core-stager", partyId, member.Travel.Revision + 1L), Is.True);
            party = new TravelPartyRuntime(
                partyId, route.Origin.RuntimeId, route.Destination.RuntimeId, route.RuntimeId,
                new[] { member.RuntimeId }, Array.Empty<string>(), route.TravelDays,
                "decision/core-stager", new[] { new TravelPartyMemberCost(member.RuntimeId, 0f) });
            Assert.That(sourceParties.Add(party), Is.True);
            P12FTravelPartyOwnerSnapshot partySnapshot = new P12FTravelPartyOwnerSnapshot(
                P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion, sourceParties.Revision,
                new[] { new P12FTravelPartyOwnerSnapshotRecord(party) });
            List<P12DNpcFRow> rows = CreateRows(graph.Npcs);
            Assert.That(DailyV1P12FTravelPartyOwnerStager.TryStage(
                partySnapshot, graph.Npcs, rows, graph.SpatialNetwork,
                out stagedParties, out P12FCommitmentSnapshotFailure partyFailure),
                Is.True, partyFailure.ToString());
        }

        List<string> npcIds = graph.Npcs.Select(owner => owner.RuntimeId).ToList();
        npcIds.Add("npc/compatibility-row");
        List<P12DNpcFRow> fRows = CreateRows(graph.Npcs);
        fRows.Add(CreateFRow("npc/compatibility-row", null));

        return new Fixture(graph, route, member, siteId, site, sites, party,
            npcIds.AsReadOnly(), fRows.AsReadOnly(), stagedParties, new ExpeditionStore());
    }

    private static List<P12DNpcFRow> CreateRows(IReadOnlyList<DailyV1NpcRuntimeOwners> owners)
    {
        List<P12DNpcFRow> rows = new List<P12DNpcFRow>(owners.Count);
        foreach (DailyV1NpcRuntimeOwners owner in owners)
            rows.Add(CreateFRow(owner.RuntimeId, owner.Travel.ActiveTravelPartyId));
        return rows;
    }

    private static P12DNpcFRow CreateFRow(string runtimeId, string partyId) =>
        new P12DNpcFRow(runtimeId, null, 0, 0, null, false, null, partyId, 0L, null, null,
            Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<P12DNpcCommercialShareReceiptValue>(), 0L);

    private sealed class Fixture
    {
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal SpatialRouteRuntime Route { get; }
        internal DailyV1NpcRuntimeOwners Member { get; }
        internal string SiteRuntimeId { get; }
        internal ExplorableSiteRuntime Site { get; }
        internal ExplorableSiteStore Sites { get; }
        internal TravelPartyRuntime Party { get; }
        internal IReadOnlyList<string> NpcRuntimeIds { get; }
        internal IReadOnlyList<P12DNpcFRow> NpcFRows { get; }
        internal TravelPartyStore StagedTravelParties { get; }
        internal ExpeditionStore SourceExpeditions { get; }

        internal Fixture(DailyV1SelectedRuntimeOwnerGraph graph, SpatialRouteRuntime route,
            DailyV1NpcRuntimeOwners member, string siteRuntimeId, ExplorableSiteRuntime site, ExplorableSiteStore sites,
            TravelPartyRuntime party, IReadOnlyList<string> npcRuntimeIds,
            IReadOnlyList<P12DNpcFRow> npcFRows, TravelPartyStore stagedTravelParties,
            ExpeditionStore sourceExpeditions)
        {
            Graph = graph;
            Route = route;
            Member = member;
            SiteRuntimeId = siteRuntimeId;
            Site = site;
            Sites = sites;
            Party = party;
            NpcRuntimeIds = npcRuntimeIds;
            NpcFRows = npcFRows;
            StagedTravelParties = stagedTravelParties;
            SourceExpeditions = sourceExpeditions;
        }
    }
}
