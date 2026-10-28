using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1P12DSelectedOwnerGraphStagerTests
{
    [Test]
    public void RestoredGraphUsesExactOwnersAndDeterministicIdentityOrder()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        PersonStore persons = new PersonStore();
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(new SimulationTime(8L));
        ExpeditionStore expeditions = new ExpeditionStore();

        Assert.That(DailyV1P12DSelectedOwnerGraphStager.TryCreate(
            definitions,
            source.Cities.Reverse().ToArray(),
            source.Npcs.Reverse().ToArray(),
            source.SpatialNetwork,
            source.JusticeRecordsOwner,
            new TravelPartyStore(),
            source.RuntimeIdentityIndex,
            persons,
            directives,
            expeditions,
            out DailyV1SelectedRuntimeOwnerGraph restored,
            out DailyV1P12DSelectedOwnerGraphFailure failure), Is.True, failure.ToString());

        Assert.That(restored.Definitions, Is.SameAs(definitions));
        Assert.That(restored.SpatialNetwork, Is.SameAs(source.SpatialNetwork));
        Assert.That(restored.RuntimeIdentityIndex, Is.SameAs(source.RuntimeIdentityIndex));
        Assert.That(restored.PersonStore, Is.SameAs(persons));
        Assert.That(restored.ScheduledDirectives, Is.SameAs(directives));
        Assert.That(restored.Expeditions, Is.SameAs(expeditions));
        Assert.That(restored.Cities.Select(owner => owner.RuntimeId).ToArray(),
            Is.EqualTo(source.Cities.Select(owner => owner.RuntimeId)
                .OrderBy(runtimeId => runtimeId, System.StringComparer.Ordinal).ToArray()));
        Assert.That(restored.Npcs.Select(owner => owner.RuntimeId).ToArray(),
            Is.EqualTo(source.Npcs.Select(owner => owner.RuntimeId)
                .OrderBy(runtimeId => runtimeId, System.StringComparer.Ordinal).ToArray()));
        foreach (DailyV1CityRuntimeOwners owner in source.Cities)
            Assert.That(restored.ContainsExactCityOwner(owner), Is.True);
        foreach (DailyV1NpcRuntimeOwners owner in source.Npcs)
            Assert.That(restored.ContainsExactNpcOwner(owner), Is.True);
    }

    [Test]
    public void RestoredMembershipPreservesCityOrderIndependentOfRosterOrder()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        DailyV1CityRuntimeOwners city = source.Cities.First(candidate =>
            candidate.NpcPresence.OrderedNpcRuntimeIds.Count > 1);
        string[] initialMembership = city.NpcPresence.OrderedNpcRuntimeIds.ToArray();
        string[] reorderedMembership = initialMembership.Reverse().ToArray();
        string[] configuredRosterOrder = source.Npcs
            .Where(npc => npc.Travel.CurrentCityRuntimeId == city.RuntimeId)
            .Select(npc => npc.RuntimeId).ToArray();
        Assert.That(reorderedMembership, Is.Not.EqualTo(configuredRosterOrder));

        foreach (string npcRuntimeId in initialMembership)
            Assert.That(city.NpcPresence.TryRemove(npcRuntimeId), Is.True);
        foreach (string npcRuntimeId in reorderedMembership)
            Assert.That(city.NpcPresence.TryAdd(npcRuntimeId), Is.True);

        Assert.That(TryStage(source, definitions,
            out DailyV1SelectedRuntimeOwnerGraph restored,
            out DailyV1P12DSelectedOwnerGraphFailure failure), Is.True, failure.ToString());
        DailyV1CityRuntimeOwners restoredCity = restored.Cities.Single(candidate =>
            ReferenceEquals(candidate, city));
        Assert.That(restoredCity.NpcPresence.OrderedNpcRuntimeIds, Is.EqualTo(reorderedMembership));
    }

    [Test]
    public void RejectsRestoredGraphWithMissingCityMember()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        DailyV1CityRuntimeOwners city = source.Cities.First(candidate =>
            candidate.NpcPresence.OrderedNpcRuntimeIds.Count > 0);
        Assert.That(city.NpcPresence.TryRemove(city.NpcPresence.OrderedNpcRuntimeIds[0]), Is.True);

        AssertRejectedMembership(source, definitions);
    }

    [Test]
    public void RejectsRestoredGraphWithDanglingCityMember()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        Assert.That(source.Cities[0].NpcPresence.TryAdd("npc/missing"), Is.True);

        AssertRejectedMembership(source, definitions);
    }

    [Test]
    public void RejectsNpcListedByMoreThanOneRestoredCity()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        Assert.That(source.Cities.Count, Is.GreaterThan(1));
        string npcRuntimeId = source.Cities[0].NpcPresence.OrderedNpcRuntimeIds[0];
        Assert.That(source.Cities[1].NpcPresence.TryAdd(npcRuntimeId), Is.True);

        AssertRejectedMembership(source, definitions);
    }

    [Test]
    public void RejectsNonreciprocalRestoredCurrentCityLink()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        DailyV1CityRuntimeOwners originalCity = source.Cities[0];
        string npcRuntimeId = originalCity.NpcPresence.OrderedNpcRuntimeIds[0];
        DailyV1NpcRuntimeOwners npc = source.Npcs.Single(candidate =>
            candidate.RuntimeId == npcRuntimeId);
        DailyV1CityRuntimeOwners otherCity = source.Cities.Single(candidate =>
            !ReferenceEquals(candidate, originalCity));
        DailyV1NpcTravelOwner travel = npc.Travel;
        Assert.That(travel.TryInstallState(
            otherCity.LocationRuntimeId, otherCity.RuntimeId,
            travel.DestinationLocationRuntimeId, travel.DestinationCityRuntimeId,
            travel.TravelDaysRemaining, travel.TravelDaysTotal,
            travel.TravelRouteRuntimeId, travel.TravelStartedToday,
            travel.TravelOriginDecisionId, travel.ActiveTravelPartyId, travel.Revision), Is.True);

        AssertRejectedMembership(source, definitions);
    }

    [Test]
    public void AdmitsRestoredTravelPartyAcrossTheDynamicNpcOwnerVector()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        Assert.That(source.Npcs.Count, Is.GreaterThan(2));

        TravelPartyStore travelParties = CreateActiveTravelParty(
            source, source.Npcs.Count, out TravelPartyRuntime party,
            out DailyV1NpcRuntimeOwners[] partyMembers);

        Assert.That(party.MemberRuntimeIds.Count, Is.EqualTo(source.Npcs.Count));
        Assert.That(TryStage(source, definitions, travelParties,
            out DailyV1SelectedRuntimeOwnerGraph restored,
            out DailyV1P12DSelectedOwnerGraphFailure failure), Is.True, failure.ToString());
        Assert.That(restored.TravelParties, Is.SameAs(travelParties));
        foreach (DailyV1NpcRuntimeOwners member in partyMembers)
        {
            Assert.That(member.Travel.ActiveTravelPartyId, Is.EqualTo(party.TravelPartyId));
            Assert.That(restored.ContainsExactNpcOwner(member), Is.True);
        }
    }

    [Test]
    public void RejectsRestoredTravelPartyWithNonreciprocalNpcPartyId()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        TravelPartyStore travelParties = CreateActiveTravelParty(
            source, 2, out _, out DailyV1NpcRuntimeOwners[] partyMembers);
        DailyV1NpcTravelOwner travel = partyMembers[0].Travel;

        Assert.That(travel.TryInstallState(
            travel.CurrentLocationRuntimeId, travel.CurrentCityRuntimeId,
            travel.DestinationLocationRuntimeId, travel.DestinationCityRuntimeId,
            travel.TravelDaysRemaining, travel.TravelDaysTotal,
            travel.TravelRouteRuntimeId, travel.TravelStartedToday,
            travel.TravelOriginDecisionId, "party/forged", travel.Revision + 1), Is.True);

        AssertRejectedTravelParty(source, definitions, travelParties);
    }

    [Test]
    public void RejectsRestoredTravelPartyWhenNpcRouteAndDestinationDoNotMatchItsRoute()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        TravelPartyStore travelParties = CreateActiveTravelParty(
            source, 2, out _, out DailyV1NpcRuntimeOwners[] partyMembers);
        DailyV1NpcTravelOwner travel = partyMembers[0].Travel;
        SpatialLocationRuntime alternativeDestination = source.SpatialNetwork.Locations
            .First(location => !string.Equals(
                location.RuntimeId, travel.DestinationLocationRuntimeId, System.StringComparison.Ordinal));
        DailyV1CityRuntimeOwners alternativeCity = source.Cities.FirstOrDefault(city =>
            string.Equals(city.LocationRuntimeId, alternativeDestination.RuntimeId,
                System.StringComparison.Ordinal));

        Assert.That(travel.TryInstallState(
            travel.CurrentLocationRuntimeId, travel.CurrentCityRuntimeId,
            alternativeDestination.RuntimeId, alternativeCity?.RuntimeId,
            travel.TravelDaysRemaining, travel.TravelDaysTotal,
            "route/mismatched", travel.TravelStartedToday,
            travel.TravelOriginDecisionId, travel.ActiveTravelPartyId, travel.Revision + 1), Is.True);

        AssertRejectedTravelParty(source, definitions, travelParties);
    }

    [Test]
    public void RejectsNpcPartyBackReferenceWithoutAnActivePartyMembership()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        Assert.That(source.Npcs.Count, Is.GreaterThan(2));
        TravelPartyStore travelParties = CreateActiveTravelParty(
            source, 2, out _, out _);
        DailyV1NpcRuntimeOwners ungroupedNpc = source.Npcs[2];
        DailyV1NpcTravelOwner travel = ungroupedNpc.Travel;

        Assert.That(travel.TryInstallState(
            travel.CurrentLocationRuntimeId, travel.CurrentCityRuntimeId,
            travel.DestinationLocationRuntimeId, travel.DestinationCityRuntimeId,
            travel.TravelDaysRemaining, travel.TravelDaysTotal,
            travel.TravelRouteRuntimeId, travel.TravelStartedToday,
            travel.TravelOriginDecisionId, "party/missing", travel.Revision + 1), Is.True);

        AssertRejectedTravelParty(source, definitions, travelParties);
    }

    [Test]
    public void RejectsActiveTravelPartyMembersWithDifferentDailyStartBoundaries()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        TravelPartyStore travelParties = CreateActiveTravelParty(
            source, 2, out _, out DailyV1NpcRuntimeOwners[] partyMembers);
        DailyV1NpcTravelOwner travel = partyMembers[1].Travel;

        Assert.That(travel.TryInstallState(
            travel.CurrentLocationRuntimeId, travel.CurrentCityRuntimeId,
            travel.DestinationLocationRuntimeId, travel.DestinationCityRuntimeId,
            travel.TravelDaysRemaining, travel.TravelDaysTotal,
            travel.TravelRouteRuntimeId, !travel.TravelStartedToday,
            travel.TravelOriginDecisionId, travel.ActiveTravelPartyId, travel.Revision + 1), Is.True);

        AssertRejectedTravelParty(source, definitions, travelParties);
    }

    [Test]
    public void RejectsDuplicateOrForeignOwnerWithoutReturningPartialGraph()
    {
        DailyV1SelectedRuntimeOwnerGraph source = CreateSourceGraph(
            out DailyV1EffectiveDefinitions definitions);
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(new SimulationTime(8L));
        ExpeditionStore expeditions = new ExpeditionStore();
        TravelPartyStore parties = new TravelPartyStore();

        Assert.That(DailyV1P12DSelectedOwnerGraphStager.TryCreate(
            definitions,
            source.Cities.Concat(new[] { source.Cities[0] }).ToArray(),
            source.Npcs,
            source.SpatialNetwork,
            source.JusticeRecordsOwner,
            parties,
            source.RuntimeIdentityIndex,
            new PersonStore(),
            directives,
            expeditions,
            out DailyV1SelectedRuntimeOwnerGraph duplicateGraph,
            out DailyV1P12DSelectedOwnerGraphFailure duplicateFailure), Is.False);
        Assert.That(duplicateGraph, Is.Null);
        Assert.That(duplicateFailure, Is.EqualTo(DailyV1P12DSelectedOwnerGraphFailure.InvalidCityOwner));

        DailyV1EffectiveDefinitions foreignDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "foreign-npc", foreignDefinitions, foreignDefinitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners foreignNpc), Is.True);
        Assert.That(DailyV1P12DSelectedOwnerGraphStager.TryCreate(
            definitions,
            source.Cities,
            source.Npcs.Concat(new[] { foreignNpc }).ToArray(),
            source.SpatialNetwork,
            source.JusticeRecordsOwner,
            parties,
            source.RuntimeIdentityIndex,
            new PersonStore(),
            directives,
            expeditions,
            out DailyV1SelectedRuntimeOwnerGraph foreignGraph,
            out DailyV1P12DSelectedOwnerGraphFailure foreignFailure), Is.False);
        Assert.That(foreignGraph, Is.Null);
        Assert.That(foreignFailure, Is.EqualTo(DailyV1P12DSelectedOwnerGraphFailure.InvalidNpcOwner));
    }

    [Test]
    public void AllowsNpcOnlyOwnerVectorButRejectsCompletelyEmptyOwnerVector()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialNpcOwner(
            "npc/without-city", definitions, definitions.Npcs.Values.First(), 0f,
            out DailyV1NpcRuntimeOwners npc), Is.True);

        SpatialNetworkOwner spatialNetwork = new SpatialNetworkOwner();
        DailyV1JusticeRecordsOwner justiceRecords = new DailyV1JusticeRecordsOwner();
        TravelPartyStore travelParties = new TravelPartyStore();
        DailyV1RuntimeIdentityIndex identities = new DailyV1RuntimeIdentityIndex();
        Assert.That(identities.TryStageRegister(npc.RuntimeId, DailyV1RuntimeIdentityKind.Npc), Is.True);
        PersonStore persons = new PersonStore();
        ScheduledDirectiveStore directives = new ScheduledDirectiveStore(new SimulationTime(8L));
        ExpeditionStore expeditions = new ExpeditionStore();

        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            System.Array.Empty<DailyV1CityRuntimeOwners>(), new[] { npc }, spatialNetwork,
            justiceRecords, travelParties, identities, persons,
            out DailyV1SelectedRuntimeOwnerGraph priorBoundaryGraph), Is.True);

        Assert.That(DailyV1P12DSelectedOwnerGraphStager.TryCreate(
            definitions,
            System.Array.Empty<DailyV1CityRuntimeOwners>(),
            new[] { npc },
            spatialNetwork,
            justiceRecords,
            travelParties,
            identities,
            persons,
            directives,
            expeditions,
            out DailyV1SelectedRuntimeOwnerGraph restored,
            out DailyV1P12DSelectedOwnerGraphFailure failure), Is.True, failure.ToString());
        Assert.That(restored.Cities, Is.Empty);
        Assert.That(restored.Npcs.Count, Is.EqualTo(1));
        Assert.That(restored.Npcs[0], Is.SameAs(npc));

        Assert.That(DailyV1P12DSelectedOwnerGraphStager.TryCreate(
            definitions,
            System.Array.Empty<DailyV1CityRuntimeOwners>(),
            System.Array.Empty<DailyV1NpcRuntimeOwners>(),
            spatialNetwork,
            justiceRecords,
            travelParties,
            new DailyV1RuntimeIdentityIndex(),
            new PersonStore(),
            new ScheduledDirectiveStore(new SimulationTime(8L)),
            new ExpeditionStore(),
            out DailyV1SelectedRuntimeOwnerGraph emptyGraph,
            out DailyV1P12DSelectedOwnerGraphFailure emptyFailure), Is.False);
        Assert.That(emptyGraph, Is.Null);
        Assert.That(emptyFailure, Is.EqualTo(DailyV1P12DSelectedOwnerGraphFailure.InvalidInput));
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateSourceGraph(
        out DailyV1EffectiveDefinitions definitions)
    {
        definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1SelectedRuntimeOwnerGraphBuilder.TryBuildConfigured(
            definitions, new RuntimeIdAllocator(), out DailyV1SelectedRuntimeOwnerGraph source), Is.True);
        return source;
    }

    private static bool TryStage(
        DailyV1SelectedRuntimeOwnerGraph source,
        DailyV1EffectiveDefinitions definitions,
        out DailyV1SelectedRuntimeOwnerGraph restored,
        out DailyV1P12DSelectedOwnerGraphFailure failure)
    {
        return TryStage(source, definitions, new TravelPartyStore(), out restored, out failure);
    }

    private static bool TryStage(
        DailyV1SelectedRuntimeOwnerGraph source,
        DailyV1EffectiveDefinitions definitions,
        TravelPartyStore travelParties,
        out DailyV1SelectedRuntimeOwnerGraph restored,
        out DailyV1P12DSelectedOwnerGraphFailure failure)
    {
        return DailyV1P12DSelectedOwnerGraphStager.TryCreate(
            definitions, source.Cities, source.Npcs, source.SpatialNetwork,
            source.JusticeRecordsOwner, travelParties, source.RuntimeIdentityIndex,
            new PersonStore(), new ScheduledDirectiveStore(new SimulationTime(8L)),
            new ExpeditionStore(), out restored, out failure);
    }

    private static TravelPartyStore CreateActiveTravelParty(
        DailyV1SelectedRuntimeOwnerGraph source,
        int memberCount,
        out TravelPartyRuntime party,
        out DailyV1NpcRuntimeOwners[] partyMembers)
    {
        Assert.That(memberCount, Is.GreaterThan(1));
        Assert.That(source.Npcs.Count, Is.GreaterThanOrEqualTo(memberCount));
        SpatialRouteRuntime route = source.SpatialNetwork.Routes.FirstOrDefault(candidate =>
            source.Cities.Any(city => string.Equals(city.LocationRuntimeId,
                candidate.Origin.RuntimeId, System.StringComparison.Ordinal))
            && source.Cities.Any(city => string.Equals(city.LocationRuntimeId,
                candidate.Destination.RuntimeId, System.StringComparison.Ordinal)));
        Assert.That(route, Is.Not.Null, "The fixture must contain a routed City-to-City location pair.");
        DailyV1CityRuntimeOwners destinationCity = source.Cities.Single(city =>
            string.Equals(city.LocationRuntimeId, route.Destination.RuntimeId,
                System.StringComparison.Ordinal));

        partyMembers = source.Npcs.Take(memberCount).ToArray();
        string partyId = "party/restored-dynamic";
        string originDecisionId = "decision/restored-dynamic";
        foreach (DailyV1CityRuntimeOwners city in source.Cities)
        {
            foreach (DailyV1NpcRuntimeOwners member in partyMembers)
            {
                if (city.NpcPresence.Contains(member.RuntimeId))
                    Assert.That(city.NpcPresence.TryRemove(member.RuntimeId), Is.True);
            }
        }

        foreach (DailyV1NpcRuntimeOwners member in partyMembers)
        {
            DailyV1NpcTravelOwner travel = member.Travel;
            Assert.That(travel.TryInstallState(
                null, null,
                route.Destination.RuntimeId, destinationCity.RuntimeId,
                route.TravelDays, route.TravelDays, route.RuntimeId,
                true, originDecisionId, partyId, travel.Revision + 1), Is.True);
        }

        string[] memberIds = partyMembers.Select(member => member.RuntimeId).ToArray();
        party = new TravelPartyRuntime(
            partyId, route.Origin.RuntimeId, route.Destination.RuntimeId, route.RuntimeId,
            memberIds, System.Array.Empty<string>(), route.TravelDays, originDecisionId,
            memberIds.Select(memberId => new TravelPartyMemberCost(memberId, 0f)));
        Assert.That(TravelPartyStore.TryCreateFromOwnerRows(
            new[] { party }, 1L, out TravelPartyStore stagedStore), Is.True);
        return stagedStore;
    }

    private static void AssertRejectedTravelParty(
        DailyV1SelectedRuntimeOwnerGraph source,
        DailyV1EffectiveDefinitions definitions,
        TravelPartyStore travelParties)
    {
        Assert.That(TryStage(source, definitions, travelParties,
            out DailyV1SelectedRuntimeOwnerGraph restored,
            out DailyV1P12DSelectedOwnerGraphFailure failure), Is.False);
        Assert.That(restored, Is.Null);
        Assert.That(failure, Is.EqualTo(
            DailyV1P12DSelectedOwnerGraphFailure.InvalidTravelPartyBinding));
    }

    private static void AssertRejectedMembership(
        DailyV1SelectedRuntimeOwnerGraph source,
        DailyV1EffectiveDefinitions definitions)
    {
        Assert.That(TryStage(source, definitions,
            out DailyV1SelectedRuntimeOwnerGraph restored,
            out DailyV1P12DSelectedOwnerGraphFailure failure), Is.False);
        Assert.That(restored, Is.Null);
        Assert.That(failure, Is.EqualTo(DailyV1P12DSelectedOwnerGraphFailure.InvalidCityMembership));
    }
}
