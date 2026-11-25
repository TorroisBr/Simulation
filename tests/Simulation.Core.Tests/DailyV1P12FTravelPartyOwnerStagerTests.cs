using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1P12FTravelPartyOwnerStagerTests
{
    [Test]
    public void StagesSnapshotAgainstExactCoreNpcOwnersAndSpatialTopology()
    {
        Fixture fixture = CreateFixture();

        Assert.That(DailyV1P12FTravelPartyOwnerStager.TryStage(
            fixture.Snapshot, fixture.Graph.Npcs, fixture.Rows,
            fixture.Graph.SpatialNetwork, out TravelPartyStore staged,
            out P12FCommitmentSnapshotFailure failure), Is.True, failure.ToString());

        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.None));
        Assert.That(staged, Is.Not.SameAs(fixture.Source));
        Assert.That(staged.Revision, Is.EqualTo(fixture.Snapshot.Revision));
        TravelPartyRuntime restored = staged.GetById(fixture.Party.TravelPartyId);
        Assert.That(restored.MemberRuntimeIds, Is.EqualTo(fixture.Party.MemberRuntimeIds));
        Assert.That(restored.OriginLocationRuntimeId, Is.EqualTo(fixture.Party.OriginLocationRuntimeId));
        Assert.That(restored.DestinationLocationRuntimeId, Is.EqualTo(fixture.Party.DestinationLocationRuntimeId));
    }

    [Test]
    public void RejectsMissingSelectedRowsDuplicateOwnersAndMismatchedLinks()
    {
        Fixture fixture = CreateFixture();
        List<P12DNpcFRow> missingRow = fixture.Rows.ToList();
        int selectedMemberIndex = IndexOf(fixture.Graph.Npcs, fixture.Members[0].RuntimeId);
        missingRow.RemoveAt(selectedMemberIndex);

        Assert.That(DailyV1P12FTravelPartyOwnerStager.TryStage(
            fixture.Snapshot, fixture.Graph.Npcs, missingRow, fixture.Graph.SpatialNetwork,
            out TravelPartyStore missingRowStore, out P12FCommitmentSnapshotFailure failure), Is.False);
        Assert.That(missingRowStore, Is.Null);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidNpcBinding));

        List<DailyV1NpcRuntimeOwners> duplicateOwner = fixture.Graph.Npcs.ToList();
        duplicateOwner.Add(fixture.Graph.Npcs[0]);
        Assert.That(DailyV1P12FTravelPartyOwnerStager.TryStage(
            fixture.Snapshot, duplicateOwner, fixture.Rows, fixture.Graph.SpatialNetwork,
            out TravelPartyStore duplicateOwnerStore, out failure), Is.False);
        Assert.That(duplicateOwnerStore, Is.Null);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidNpcBinding));

        List<P12DNpcFRow> duplicateRow = fixture.Rows.ToList();
        duplicateRow.Add(fixture.Rows[0]);
        Assert.That(DailyV1P12FTravelPartyOwnerStager.TryStage(
            fixture.Snapshot, fixture.Graph.Npcs, duplicateRow, fixture.Graph.SpatialNetwork,
            out TravelPartyStore duplicateRowStore, out failure), Is.False);
        Assert.That(duplicateRowStore, Is.Null);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidNpcBinding));

        int memberIndex = IndexOf(fixture.Graph.Npcs, fixture.Members[0].RuntimeId);
        List<P12DNpcFRow> mismatchedPartyLink = fixture.Rows.ToList();
        mismatchedPartyLink[memberIndex] = CreateFRow(fixture.Members[0], null);
        Assert.That(DailyV1P12FTravelPartyOwnerStager.TryStage(
            fixture.Snapshot, fixture.Graph.Npcs, mismatchedPartyLink, fixture.Graph.SpatialNetwork,
            out TravelPartyStore mismatchedStore, out failure), Is.False);
        Assert.That(mismatchedStore, Is.Null);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidNpcBinding));
    }

    [Test]
    public void RejectsRouteEndpointMismatchAndDuplicateCrossPartyMembership()
    {
        Fixture fixture = CreateFixture();
        P12FTravelPartyOwnerSnapshotRecord party = fixture.Snapshot.Parties[0];
        P12FTravelPartyOwnerSnapshot wrongDestination = new P12FTravelPartyOwnerSnapshot(
            P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion, fixture.Snapshot.Revision,
            new[]
            {
                new P12FTravelPartyOwnerSnapshotRecord(
                    party.TravelPartyId, party.OriginLocationRuntimeId,
                    party.OriginLocationRuntimeId, party.RouteRuntimeId,
                    party.TravelerRuntimeIds, party.EscortRuntimeIds, party.MemberRuntimeIds,
                    party.MemberCosts, party.TravelDaysTotal, party.OriginDecisionId)
            });

        Assert.That(DailyV1P12FTravelPartyOwnerStager.TryStage(
            wrongDestination, fixture.Graph.Npcs, fixture.Rows, fixture.Graph.SpatialNetwork,
            out TravelPartyStore wrongRouteStore, out P12FCommitmentSnapshotFailure failure), Is.False);
        Assert.That(wrongRouteStore, Is.Null);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidIdentityBinding));

        P12FTravelPartyOwnerSnapshot duplicateMember = new P12FTravelPartyOwnerSnapshot(
            P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion, fixture.Snapshot.Revision,
            new[]
            {
                party,
                new P12FTravelPartyOwnerSnapshotRecord(
                    "party/duplicate-membership", party.OriginLocationRuntimeId,
                    party.DestinationLocationRuntimeId, party.RouteRuntimeId,
                    party.TravelerRuntimeIds, party.EscortRuntimeIds, party.MemberRuntimeIds,
                    party.MemberCosts, party.TravelDaysTotal, party.OriginDecisionId)
            });

        Assert.That(DailyV1P12FTravelPartyOwnerStager.TryStage(
            duplicateMember, fixture.Graph.Npcs, fixture.Rows, fixture.Graph.SpatialNetwork,
            out TravelPartyStore duplicateStore, out failure), Is.False);
        Assert.That(duplicateStore, Is.Null);
        Assert.That(failure, Is.EqualTo(P12FCommitmentSnapshotFailure.InvalidNpcBinding));
    }

    [Test]
    public void AcceptsExactEmptyOwnerVectorAndEmptySnapshot()
    {
        P12FTravelPartyOwnerSnapshot empty = new P12FTravelPartyOwnerSnapshot(
            P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion, 0L,
            Array.Empty<P12FTravelPartyOwnerSnapshotRecord>());

        Assert.That(DailyV1P12FTravelPartyOwnerStager.TryStage(
            empty, Array.Empty<DailyV1NpcRuntimeOwners>(), Array.Empty<P12DNpcFRow>(),
            new SpatialNetworkOwner(), out TravelPartyStore staged,
            out P12FCommitmentSnapshotFailure failure), Is.True, failure.ToString());
        Assert.That(staged, Is.Not.Null);
        Assert.That(staged.Revision, Is.Zero);
        Assert.That(staged.ActiveParties, Is.Empty);
    }

    private static Fixture CreateFixture()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1SelectedRuntimeOwnerGraphBuilder.TryBuildConfigured(
            definitions, new RuntimeIdAllocator(), out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.Npcs.Count, Is.GreaterThanOrEqualTo(2));

        SpatialRouteRuntime route = graph.SpatialNetwork.Routes.FirstOrDefault(candidate =>
            graph.Cities.Any(city => string.Equals(city.LocationRuntimeId,
                candidate.Origin.RuntimeId, StringComparison.Ordinal))
            && graph.Cities.Any(city => string.Equals(city.LocationRuntimeId,
                candidate.Destination.RuntimeId, StringComparison.Ordinal)));
        Assert.That(route, Is.Not.Null);
        Assert.That(route.TravelDays, Is.GreaterThan(0));
        DailyV1CityRuntimeOwners destination = graph.Cities.Single(city =>
            string.Equals(city.LocationRuntimeId, route.Destination.RuntimeId, StringComparison.Ordinal));

        DailyV1NpcRuntimeOwners[] members = graph.Npcs.Take(2).ToArray();
        const string partyId = "party/core-stager";
        const string decisionId = "decision/core-stager";
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            foreach (DailyV1NpcRuntimeOwners member in members)
                if (city.NpcPresence.Contains(member.RuntimeId))
                    Assert.That(city.NpcPresence.TryRemove(member.RuntimeId), Is.True);
        }
        foreach (DailyV1NpcRuntimeOwners member in members)
        {
            DailyV1NpcTravelOwner travel = member.Travel;
            Assert.That(travel.TryInstallState(
                null, null, route.Destination.RuntimeId, destination.RuntimeId,
                route.TravelDays, route.TravelDays, route.RuntimeId, true, decisionId,
                partyId, travel.Revision + 1), Is.True);
        }

        string[] travelerIds = members.Select(member => member.RuntimeId).ToArray();
        const string compatibilityNpcId = "npc/unselected-compatibility-member";
        string[] memberIds = travelerIds.Concat(new[] { compatibilityNpcId }).ToArray();
        TravelPartyRuntime party = new TravelPartyRuntime(
            partyId, route.Origin.RuntimeId, route.Destination.RuntimeId, route.RuntimeId,
            travelerIds, new[] { compatibilityNpcId }, route.TravelDays, decisionId,
            memberIds.Select(id => new TravelPartyMemberCost(id, 0f)));
        TravelPartyStore source = new TravelPartyStore();
        Assert.That(source.Add(party), Is.True);
        P12FTravelPartyOwnerSnapshot snapshot = new P12FTravelPartyOwnerSnapshot(
            P12FTravelPartyOwnerSnapshot.CurrentSchemaVersion, source.Revision,
            new[] { new P12FTravelPartyOwnerSnapshotRecord(party) });
        List<P12DNpcFRow> rows = graph.Npcs.Select(owner =>
            CreateFRow(owner, owner.Travel.ActiveTravelPartyId)).ToList();
        rows.Add(CreateFRow(compatibilityNpcId, partyId));

        return new Fixture(graph, source, snapshot, rows, party, members);
    }

    private static P12DNpcFRow CreateFRow(DailyV1NpcRuntimeOwners owner, string partyId)
    {
        DailyV1NpcTravelOwner travel = owner.Travel;
        return new P12DNpcFRow(owner.RuntimeId, null,
            travel.TravelDaysRemaining, travel.TravelDaysTotal, travel.TravelRouteRuntimeId,
            travel.TravelStartedToday, travel.TravelOriginDecisionId, partyId, travel.Revision,
            null, null,
            Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<P12DNpcCommercialShareReceiptValue>(), 0L);
    }

    private static P12DNpcFRow CreateFRow(string runtimeId, string partyId) =>
        new P12DNpcFRow(runtimeId, null, 0, 0, null, false, null, partyId, 0L, null, null,
            Array.Empty<ExplorableSiteKnowledgeObservation>(), 0L,
            Array.Empty<string>(), Array.Empty<string>(), 0L,
            Array.Empty<AdventureOppositionObservation>(), Array.Empty<AdventureNotableItemObservation>(),
            Array.Empty<AdventureCommonResourceObservation>(), Array.Empty<AdventureAccessObservation>(), 0L,
            Array.Empty<P12DNpcCommercialMarketValue>(), Array.Empty<P12DNpcCommercialLiquidityValue>(),
            Array.Empty<P12DNpcCommercialShareReceiptValue>(), 0L);
    private static int IndexOf(IReadOnlyList<DailyV1NpcRuntimeOwners> owners, string runtimeId)
    {
        for (int index = 0; index < owners.Count; index++)
            if (string.Equals(owners[index].RuntimeId, runtimeId, StringComparison.Ordinal))
                return index;
        return -1;
    }

    private sealed class Fixture
    {
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal TravelPartyStore Source { get; }
        internal P12FTravelPartyOwnerSnapshot Snapshot { get; }
        internal IReadOnlyList<P12DNpcFRow> Rows { get; }
        internal TravelPartyRuntime Party { get; }
        internal DailyV1NpcRuntimeOwners[] Members { get; }

        internal Fixture(DailyV1SelectedRuntimeOwnerGraph graph, TravelPartyStore source,
            P12FTravelPartyOwnerSnapshot snapshot, IReadOnlyList<P12DNpcFRow> rows,
            TravelPartyRuntime party, DailyV1NpcRuntimeOwners[] members)
        {
            Graph = graph;
            Source = source;
            Snapshot = snapshot;
            Rows = rows;
            Party = party;
            Members = members;
        }
    }
}
