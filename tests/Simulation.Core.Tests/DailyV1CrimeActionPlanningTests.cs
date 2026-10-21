using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

public sealed class DailyV1CrimeActionPlanningTests
{
    [Test]
    public void SelectedStealUsesCoreRosterOrderingAndExactPersonOccurrenceIdentity()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        DailyV1ActionDefinition steal = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.StealActionTypeValue);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(steal, 10f);
        FixedPlanningRandom random = new FixedPlanningRandom(0f);

        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedSteal(
            fixture.Graph, fixture.Actor, candidate, fixture.PersonStore, 41L, random), Is.True);

        DailyV1NpcRuntimeOwners expectedTarget = fixture.City.NpcPresence.OrderedNpcRuntimeIds
            .Where(id => !string.Equals(id, fixture.Actor.RuntimeId, System.StringComparison.Ordinal))
            .Select(id => fixture.Graph.TryGetNpcOwner(id, out DailyV1NpcRuntimeOwners owner) ? owner : null)
            .Where(owner => owner != null && owner.Lifecycle.LifeState == NpcLifeState.Alive
                && !owner.Travel.IsTraveling && owner.HiddenState.RemainingDays <= 0
                && owner.MoneyAccount.Balance > 0f
                && string.Equals(owner.Travel.CurrentCityRuntimeId, fixture.City.RuntimeId,
                    System.StringComparison.Ordinal))
            .OrderBy(owner => owner.RuntimeId, System.StringComparer.Ordinal)
            .First();
        PersonRuntime actorPerson = fixture.PersonStore.TryGetByMaterializedNpcRuntimeId(
            fixture.Actor.RuntimeId, out PersonRuntime exactActor) ? exactActor : null;
        PersonRuntime targetPerson = fixture.PersonStore.TryGetByMaterializedNpcRuntimeId(
            expectedTarget.RuntimeId, out PersonRuntime exactTarget) ? exactTarget : null;
        int expectedAmount = System.Math.Min(steal.Values.crimeSettings.amount,
            UnityMathfCompatibility.FloorToInt(expectedTarget.MoneyAccount.Balance));

        Assert.That(candidate.PreparedPayload, Is.Not.Null);
        Assert.That(candidate.PreparedPayload.TargetNpcRuntimeId, Is.EqualTo(expectedTarget.RuntimeId));
        Assert.That(candidate.PreparedPayload.Amount, Is.EqualTo(expectedAmount));
        Assert.That(candidate.PreparedPayload.StableOccurrenceKey, Is.EqualTo(
            "autonomous|day|41|perp|" + actorPerson.PersonId.Value
            + "|victim|" + targetPerson.PersonId.Value
            + "|amount|" + expectedAmount + "|action|" + steal.DefinitionId));
        Assert.That(candidate.Utility, Is.EqualTo(10f
            + UnityMathfCompatibility.Clamp((50f - fixture.Actor.MoneyAccount.Balance) * 0.35f, 0f, 18f)
            + UnityMathfCompatibility.Min(8f, expectedAmount * 0.2f)).Within(0.0001f));
        Assert.That(random.StreamKeys, Is.EqualTo(new[] { "crime-steal|" + fixture.Actor.RuntimeId + "|41" }));
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void SelectedStealFailsClosedWhenActorOrChosenVictimIdentityIsNotExact(
        bool mismatchActor,
        bool omitChosenVictim)
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        DailyV1ActionDefinition steal = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.StealActionTypeValue);
        DailyV1NpcRuntimeOwners chosenVictim = fixture.City.NpcPresence.OrderedNpcRuntimeIds
            .Where(id => !string.Equals(id, fixture.Actor.RuntimeId, System.StringComparison.Ordinal))
            .Select(id => fixture.Graph.TryGetNpcOwner(id, out DailyV1NpcRuntimeOwners owner) ? owner : null)
            .Where(owner => owner != null && owner.MoneyAccount.Balance > 0f
                && owner.Lifecycle.LifeState == NpcLifeState.Alive)
            .OrderBy(owner => owner.RuntimeId, System.StringComparer.Ordinal)
            .First();
        PersonStore store = CreatePersonStore(fixture, omitNpcRuntimeId: omitChosenVictim
            ? chosenVictim.RuntimeId : null,
            mismatchNpcRuntimeId: mismatchActor ? fixture.Actor.RuntimeId : null);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(steal, 20f);
        FixedPlanningRandom random = new FixedPlanningRandom(0f);

        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedSteal(
            fixture.Graph, fixture.Actor, candidate, store, 9L, random), Is.True);

        Assert.That(candidate.Utility, Is.Zero);
        Assert.That(candidate.PreparedPayload, Is.Null);
        Assert.That(random.StreamKeys, Is.EqualTo(omitChosenVictim
            ? new[] { "crime-steal|" + fixture.Actor.RuntimeId + "|9" }
            : System.Array.Empty<string>()));
    }

    [Test]
    public void SelectedStealUsesUnequalTargetWeights()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        DailyV1ActionDefinition steal = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.StealActionTypeValue);
        DailyV1NpcRuntimeOwners[] targets = fixture.City.NpcPresence.OrderedNpcRuntimeIds
            .Where(id => !string.Equals(id, fixture.Actor.RuntimeId,
                System.StringComparison.Ordinal))
            .Select(id => fixture.Graph.TryGetNpcOwner(id,
                out DailyV1NpcRuntimeOwners owner) ? owner : null)
            .Where(owner => owner != null)
            .OrderBy(owner => owner.RuntimeId, System.StringComparer.Ordinal)
            .ToArray();
        Assert.That(targets.Length, Is.GreaterThan(1),
            "The proving fixture must include multiple same-city actors for weighted selection.");
        Assert.That(steal.Values.crimeSettings.amount, Is.GreaterThan(1));

        SetMoneyBalance(targets[0].MoneyAccount, 1f);
        SetMoneyBalance(targets[1].MoneyAccount, 9f);
        for (int i = 0; i < targets.Length; i++)
            Assert.That(targets[i].HiddenState.TrySetRemainingDays(i < 2 ? 0 : 2), Is.True);

        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(steal, 10f);
        FixedPlanningRandom random = new FixedPlanningRandom(0.2f);
        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedSteal(
            fixture.Graph, fixture.Actor, candidate, fixture.PersonStore, 12L, random), Is.True);

        Assert.That(candidate.PreparedPayload.TargetNpcRuntimeId, Is.EqualTo(targets[1].RuntimeId),
            "The draw lands in the second target's larger weighted interval; uniform target selection would choose the first target.");
        Assert.That(candidate.PreparedPayload.Amount, Is.EqualTo(System.Math.Min(
            steal.Values.crimeSettings.amount, 9)),
            "The selected amount is capped by the victim's floored current balance.");
    }

    [Test]
    public void SelectedStealExcludesHiddenHighBalanceTargetBeforeWeightedDraw()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        DailyV1ActionDefinition steal = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.StealActionTypeValue);
        DailyV1NpcRuntimeOwners[] targets = fixture.City.NpcPresence.OrderedNpcRuntimeIds
            .Where(id => !string.Equals(id, fixture.Actor.RuntimeId,
                System.StringComparison.Ordinal))
            .Select(id => fixture.Graph.TryGetNpcOwner(id,
                out DailyV1NpcRuntimeOwners owner) ? owner : null)
            .Where(owner => owner != null)
            .OrderBy(owner => owner.RuntimeId, System.StringComparer.Ordinal)
            .ToArray();
        Assert.That(targets.Length, Is.GreaterThan(1));

        SetMoneyBalance(targets[0].MoneyAccount, 100f);
        SetMoneyBalance(targets[1].MoneyAccount, 5f);
        Assert.That(targets[0].HiddenState.TrySetRemainingDays(2), Is.True);
        Assert.That(targets[1].HiddenState.TrySetRemainingDays(0), Is.True);
        for (int i = 2; i < targets.Length; i++)
            Assert.That(targets[i].HiddenState.TrySetRemainingDays(2), Is.True);

        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(steal, 10f);
        FixedPlanningRandom random = new FixedPlanningRandom(0.2f);
        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedSteal(
            fixture.Graph, fixture.Actor, candidate, fixture.PersonStore, 12L, random), Is.True);

        Assert.That(candidate.PreparedPayload.TargetNpcRuntimeId, Is.EqualTo(targets[1].RuntimeId),
            "A hidden target with the largest balance must be removed before constructing the weighted interval.");
        Assert.That(candidate.PreparedPayload.Amount, Is.EqualTo(System.Math.Min(
            steal.Values.crimeSettings.amount, 5)));
        Assert.That(random.StreamKeys, Is.EqualTo(new[] {
            "crime-steal|" + fixture.Actor.RuntimeId + "|12" }));
    }

    [Test]
    public void SelectedStealRejectsPositiveFractionalBalanceThatFloorsToZero()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        DailyV1ActionDefinition steal = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.StealActionTypeValue);
        DailyV1NpcRuntimeOwners[] targets = fixture.City.NpcPresence.OrderedNpcRuntimeIds
            .Where(id => !string.Equals(id, fixture.Actor.RuntimeId,
                System.StringComparison.Ordinal))
            .Select(id => fixture.Graph.TryGetNpcOwner(id,
                out DailyV1NpcRuntimeOwners owner) ? owner : null)
            .Where(owner => owner != null)
            .OrderBy(owner => owner.RuntimeId, System.StringComparer.Ordinal)
            .ToArray();
        Assert.That(targets, Is.Not.Empty);
        for (int i = 0; i < targets.Length; i++)
        {
            SetMoneyBalance(targets[i].MoneyAccount, 0.5f);
            Assert.That(targets[i].HiddenState.TrySetRemainingDays(i == 0 ? 0 : 2), Is.True);
        }

        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(steal, 10f);
        FixedPlanningRandom random = new FixedPlanningRandom(0f);
        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedSteal(
            fixture.Graph, fixture.Actor, candidate, fixture.PersonStore, 12L, random), Is.True);

        Assert.That(candidate.Utility, Is.Zero);
        Assert.That(candidate.PreparedPayload, Is.Null);
        Assert.That(random.StreamKeys, Is.EqualTo(new[] {
            "crime-steal|" + fixture.Actor.RuntimeId + "|12" }));
    }

    [Test]
    public void SelectedFleePlansBestKnownAffordableCityWithoutMutatingTravelOwners()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        DailyV1CityRuntimeOwners origin = fixture.City;
        foreach (DailyV1CityRuntimeOwners city in fixture.Graph.Cities)
            fixture.Actor.SpatialKnowledge.DiscoverLocation(city.LocationRuntimeId);
        foreach (SpatialRouteRuntime route in fixture.Graph.SpatialNetwork.Routes)
            fixture.Actor.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
        Assert.That(fixture.Justice.TryAddWantedRecord(new DailyV1JusticeWantedRecordOwner(
            fixture.Actor.RuntimeId, origin.RuntimeId, 1000f, 3)), Is.True);

        DailyV1ActionDefinition flee = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(flee, 1f);
        string locationBefore = fixture.Actor.Travel.CurrentLocationRuntimeId;
        long travelRevisionBefore = fixture.Actor.Travel.Revision;
        long planRevisionBefore = fixture.Actor.TravelPlan.Revision;

        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedFleeCity(
            fixture.Graph, fixture.Actor, candidate), Is.True);

        DailyV1CityRuntimeOwners expected = fixture.Graph.Cities
            .Where(city => !ReferenceEquals(city, origin))
            .OrderBy(city => city.RuntimeId, System.StringComparer.Ordinal)
            .First();
        Assert.That(candidate.PreparedPayload, Is.Not.Null);
        Assert.That(candidate.PreparedPayload.TargetCityRuntimeId, Is.EqualTo(expected.RuntimeId));
        Assert.That(candidate.PreparedPayload.TravelReason, Is.EqualTo(NpcTravelReason.Flee));
        Assert.That(candidate.PreparedPayload.ExpectedUnitPrice, Is.EqualTo(30f));
        Assert.That(candidate.Utility, Is.GreaterThan(1f));
        Assert.That(fixture.Actor.Travel.CurrentLocationRuntimeId, Is.EqualTo(locationBefore));
        Assert.That(fixture.Actor.Travel.Revision, Is.EqualTo(travelRevisionBefore));
        Assert.That(fixture.Actor.TravelPlan.Revision, Is.EqualTo(planRevisionBefore));
    }

    [Test]
    public void SelectedFleePreservesKnownRouteOrderingForApproximatelyEqualUtilities()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture(reverseRouteRegistration: true);
        DailyV1SelectedRuntimeOwnerGraph originalGraph = fixture.Graph;
        DailyV1CityRuntimeOwners origin = fixture.City;
        DailyV1CityDefinition extraDefinition = originalGraph.Definitions.Cities.Values.First();
        const string extraCityRuntimeId = "city/crime-order-extra";
        const string extraLocationRuntimeId = "location/crime-order-extra";
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateInitialCityOwner(
            extraCityRuntimeId, extraLocationRuntimeId,
            originalGraph.Definitions, extraDefinition, null,
            out DailyV1CityRuntimeOwners extraCity), Is.True);
        SpatialLocationRuntime extraLocation = new SpatialLocationRuntime(extraLocationRuntimeId);
        Assert.That(originalGraph.SpatialNetwork.TryRegisterLocation(
            extraLocation, () => true, () => { }, null), Is.True);
        Assert.That(originalGraph.SpatialNetwork.TryRegisterRoute(
            new SpatialRouteRuntime(
                "route/crime/000-order-extra/" + origin.RuntimeId + "/" + extraCityRuntimeId,
                originalGraph.SpatialNetwork.Locations.First(location =>
                    string.Equals(location.RuntimeId, origin.LocationRuntimeId,
                        System.StringComparison.Ordinal)),
                extraLocation, 3), () => true), Is.True);
        List<DailyV1CityRuntimeOwners> augmentedCities =
            new List<DailyV1CityRuntimeOwners>(originalGraph.Cities) { extraCity };
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            augmentedCities, originalGraph.Npcs, originalGraph.SpatialNetwork,
            fixture.Justice, out DailyV1SelectedRuntimeOwnerGraph augmentedGraph), Is.True);
        originalGraph = augmentedGraph;

        DailyV1CityRuntimeOwners[] destinations = originalGraph.Cities
            .Where(city => !ReferenceEquals(city, origin))
            .OrderBy(city => city.RuntimeId, System.StringComparer.Ordinal)
            .Take(2)
            .ToArray();
        DailyV1CityRuntimeOwners lowerIdLowerUtility = destinations[0];
        DailyV1CityRuntimeOwners higherIdHigherUtility = destinations[1];
        string[] competingRouteInsertionOrder = originalGraph.SpatialNetwork
            .GetOutgoingRoutes(originalGraph.SpatialNetwork.Locations.Single(location =>
                string.Equals(location.RuntimeId, origin.LocationRuntimeId,
                    System.StringComparison.Ordinal)))
            .Where(route => string.Equals(route.Destination.RuntimeId,
                    higherIdHigherUtility.LocationRuntimeId, System.StringComparison.Ordinal)
                || string.Equals(route.Destination.RuntimeId,
                    destinations[0].LocationRuntimeId, System.StringComparison.Ordinal))
            .Select(route => route.RuntimeId)
            .ToArray();
        Assert.That(competingRouteInsertionOrder.Length, Is.EqualTo(2));
        Assert.That(competingRouteInsertionOrder,
            Is.Not.EqualTo(competingRouteInsertionOrder.OrderBy(
                routeId => routeId, System.StringComparer.Ordinal).ToArray()),
            "The proving candidate routes are registered in an order that differs from the required RuntimeId order.");

        List<DailyV1CityRuntimeOwners> cityIterationOrder = new List<DailyV1CityRuntimeOwners>
        {
            higherIdHigherUtility,
            lowerIdLowerUtility
        };
        cityIterationOrder.AddRange(originalGraph.Cities.Where(city =>
            !ReferenceEquals(city, higherIdHigherUtility)
            && !ReferenceEquals(city, lowerIdLowerUtility)));
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            cityIterationOrder, originalGraph.Npcs, originalGraph.SpatialNetwork,
            fixture.Justice, out DailyV1SelectedRuntimeOwnerGraph reorderedGraph), Is.True);

        const float currentBounty = 1000f;
        Assert.That(fixture.Justice.TryAddWantedRecord(new DailyV1JusticeWantedRecordOwner(
            fixture.Actor.RuntimeId, origin.RuntimeId, currentBounty, 3)), Is.True);
        Assert.That(fixture.Justice.TryAddWantedRecord(new DailyV1JusticeWantedRecordOwner(
            fixture.Actor.RuntimeId, higherIdHigherUtility.RuntimeId, 100f, 3)), Is.True);
        Assert.That(fixture.Justice.TryAddWantedRecord(new DailyV1JusticeWantedRecordOwner(
            fixture.Actor.RuntimeId, lowerIdLowerUtility.RuntimeId, 100.00006f, 3)), Is.True);
        foreach (DailyV1CityRuntimeOwners city in reorderedGraph.Cities)
        {
            fixture.Actor.SpatialKnowledge.DiscoverLocation(city.LocationRuntimeId);
        }
        foreach (SpatialRouteRuntime route in reorderedGraph.SpatialNetwork.Routes)
            fixture.Actor.SpatialKnowledge.DiscoverRoute(route.RuntimeId);

        Assert.That(reorderedGraph.TryPlanSelectedSoloTravel(
            fixture.Actor, origin.LocationRuntimeId,
            higherIdHigherUtility.LocationRuntimeId, higherIdHigherUtility.RuntimeId,
            out SpatialRouteRuntime highRoute, out float highTravelCost), Is.True);
        Assert.That(reorderedGraph.TryPlanSelectedSoloTravel(
            fixture.Actor, origin.LocationRuntimeId,
            lowerIdLowerUtility.LocationRuntimeId, lowerIdLowerUtility.RuntimeId,
            out SpatialRouteRuntime lowRoute, out float lowTravelCost), Is.True);
        float highUtility = 30f + ((currentBounty - 100f - highTravelCost)
            / UnityMathfCompatibility.Max(1, highRoute.TravelDays));
        float lowUtility = 30f + ((currentBounty - 100.00006f - lowTravelCost)
            / UnityMathfCompatibility.Max(1, lowRoute.TravelDays));
        Assert.That(highUtility, Is.GreaterThan(lowUtility));
        Assert.That(UnityMathfCompatibility.Approximately(highUtility, lowUtility), Is.True);

        DailyV1ActionDefinition flee = ActionOfType(reorderedGraph.Definitions,
            DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(flee, 1f);
        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedFleeCity(
            reorderedGraph, fixture.Actor, candidate), Is.True);

        Assert.That(candidate.PreparedPayload.TargetCityRuntimeId,
            Is.EqualTo(lowerIdLowerUtility.RuntimeId),
            "The existing Unity provider evaluates known direct routes in route RuntimeId order; the lower-utility destination wins the approximate-equality tie because its City RuntimeId sorts first.");
    }

    [Test]
    public void SelectedFleeRejectsUnknownRouteAndMissingCurrentWarrant()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        foreach (DailyV1CityRuntimeOwners city in fixture.Graph.Cities)
            fixture.Actor.SpatialKnowledge.DiscoverLocation(city.LocationRuntimeId);
        DailyV1ActionDefinition flee = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        DailyV1NpcActionCandidate unknownRoute = new DailyV1NpcActionCandidate(flee, 25f);

        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedFleeCity(
            fixture.Graph, fixture.Actor, unknownRoute), Is.True);
        Assert.That(unknownRoute.Utility, Is.Zero);
        Assert.That(unknownRoute.PreparedPayload, Is.Null);

        foreach (SpatialRouteRuntime route in fixture.Graph.SpatialNetwork.Routes)
            fixture.Actor.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
        DailyV1NpcActionCandidate noWarrant = new DailyV1NpcActionCandidate(flee, 25f);
        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedFleeCity(
            fixture.Graph, fixture.Actor, noWarrant), Is.True);
        Assert.That(noWarrant.Utility, Is.Zero);
        Assert.That(noWarrant.PreparedPayload, Is.Null);
    }

    [Test]
    public void SelectedFleeRejectsUnknownDestinationLocation()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        fixture.Actor.SpatialKnowledge.DiscoverLocation(fixture.City.LocationRuntimeId);
        foreach (SpatialRouteRuntime route in fixture.Graph.SpatialNetwork.Routes)
            fixture.Actor.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
        Assert.That(fixture.Justice.TryAddWantedRecord(new DailyV1JusticeWantedRecordOwner(
            fixture.Actor.RuntimeId, fixture.City.RuntimeId, 1000f, 3)), Is.True);

        DailyV1ActionDefinition flee = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(flee, 25f);
        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedFleeCity(
            fixture.Graph, fixture.Actor, candidate), Is.True);

        Assert.That(candidate.Utility, Is.Zero);
        Assert.That(candidate.PreparedPayload, Is.Null);
    }

    [Test]
    public void SelectedFleeRejectsUnaffordableKnownDirectRoutes()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        foreach (DailyV1CityRuntimeOwners city in fixture.Graph.Cities)
            fixture.Actor.SpatialKnowledge.DiscoverLocation(city.LocationRuntimeId);
        foreach (SpatialRouteRuntime route in fixture.Graph.SpatialNetwork.Routes)
            fixture.Actor.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
        Assert.That(fixture.Justice.TryAddWantedRecord(new DailyV1JusticeWantedRecordOwner(
            fixture.Actor.RuntimeId, fixture.City.RuntimeId, 1000f, 3)), Is.True);
        DailyV1CityRuntimeOwners destination = fixture.Graph.Cities
            .First(city => !ReferenceEquals(city, fixture.City));
        Assert.That(fixture.Graph.TryPlanSelectedSoloTravel(
            fixture.Actor, fixture.City.LocationRuntimeId,
            destination.LocationRuntimeId, destination.RuntimeId,
            out _, out float travelCost), Is.True);
        Assert.That(travelCost, Is.GreaterThan(0f));
        SetMoneyBalance(fixture.Actor.MoneyAccount, 0f);

        DailyV1ActionDefinition flee = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(flee, 25f);
        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedFleeCity(
            fixture.Graph, fixture.Actor, candidate), Is.True);

        Assert.That(candidate.Utility, Is.Zero);
        Assert.That(candidate.PreparedPayload, Is.Null);
    }

    [Test]
    public void SelectedFleeRejectsDestinationsWithHigherActiveBounty()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        foreach (DailyV1CityRuntimeOwners city in fixture.Graph.Cities)
            fixture.Actor.SpatialKnowledge.DiscoverLocation(city.LocationRuntimeId);
        foreach (SpatialRouteRuntime route in fixture.Graph.SpatialNetwork.Routes)
            fixture.Actor.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
        Assert.That(fixture.Justice.TryAddWantedRecord(new DailyV1JusticeWantedRecordOwner(
            fixture.Actor.RuntimeId, fixture.City.RuntimeId, 100f, 3)), Is.True);
        foreach (DailyV1CityRuntimeOwners city in fixture.Graph.Cities)
            if (!ReferenceEquals(city, fixture.City))
                Assert.That(fixture.Justice.TryAddWantedRecord(new DailyV1JusticeWantedRecordOwner(
                    fixture.Actor.RuntimeId, city.RuntimeId, 200f, 3)), Is.True);

        DailyV1ActionDefinition flee = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        DailyV1NpcActionCandidate candidate = new DailyV1NpcActionCandidate(flee, 25f);
        Assert.That(DailyV1CrimeActionPlanning.TryPrepareSelectedFleeCity(
            fixture.Graph, fixture.Actor, candidate), Is.True);

        Assert.That(candidate.Utility, Is.Zero);
        Assert.That(candidate.PreparedPayload, Is.Null);
    }

    [Test]
    public void SelectedDecisionStageProjectsCoreStealAndNeverCallsProviderPlanner()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        DailyV1ActionDefinition steal = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.StealActionTypeValue);
        RecordingPlanningBoundary boundary = new RecordingPlanningBoundary();
        FixedPlanningRandom random = new FixedPlanningRandom(0f);

        Assert.That(DailyV1SelectedNpcActionDecisionStage.TryChoose(
            fixture.Graph, fixture.Actor, new[] { steal }, fixture.Actor.Status.Statuses,
            12L, random, boundary, out DailyV1NpcActionCandidate selected,
            fixture.PersonStore), Is.True);

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected.PreparedPayload, Is.Not.Null);
        Assert.That(boundary.StealProjectionCalls, Is.EqualTo(1));
        Assert.That(boundary.LegacyProviderPlanCalls, Is.Zero);
        Assert.That(random.StreamKeys, Is.EqualTo(new[] {
            "crime-steal|" + fixture.Actor.RuntimeId + "|12",
            "npc-decision|" + fixture.Actor.RuntimeId + "|12" }));
    }

    [Test]
    public void SelectedDecisionStageProjectsCoreFleeAndNeverCallsProviderPlanner()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        Assert.That(fixture.Justice.TryAddWantedRecord(new DailyV1JusticeWantedRecordOwner(
            fixture.Actor.RuntimeId, fixture.City.RuntimeId, 1000f, 4)), Is.True);
        foreach (DailyV1CityRuntimeOwners city in fixture.Graph.Cities)
            fixture.Actor.SpatialKnowledge.DiscoverLocation(city.LocationRuntimeId);
        foreach (SpatialRouteRuntime route in fixture.Graph.SpatialNetwork.Routes)
            fixture.Actor.SpatialKnowledge.DiscoverRoute(route.RuntimeId);
        DailyV1ActionDefinition flee = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.FleeCityActionTypeValue);
        RecordingPlanningBoundary boundary = new RecordingPlanningBoundary();
        FixedPlanningRandom random = new FixedPlanningRandom(0f);

        Assert.That(DailyV1SelectedNpcActionDecisionStage.TryChoose(
            fixture.Graph, fixture.Actor, new[] { flee }, fixture.Actor.Status.Statuses,
            18L, random, boundary, out DailyV1NpcActionCandidate selected), Is.True);

        Assert.That(selected, Is.Not.Null);
        Assert.That(selected.PreparedPayload, Is.Not.Null);
        Assert.That(boundary.FleeProjectionCalls, Is.EqualTo(1));
        Assert.That(boundary.LegacyProviderPlanCalls, Is.Zero);
        Assert.That(random.StreamKeys, Is.EqualTo(new[] {
            "npc-decision|" + fixture.Actor.RuntimeId + "|18" }));
    }

    [Test]
    public void SelectedDecisionDoesNotPrepareCrimeActionsWhenProviderIsDisabled()
    {
        CrimePlanningFixture fixture = CreateCrimePlanningFixture();
        DailyV1ActionDefinition steal = ActionOfType(fixture.Graph.Definitions,
            DailyV1NpcDefinitionRules.StealActionTypeValue);
        RecordingPlanningBoundary boundary = new RecordingPlanningBoundary
        {
            CrimeProviderEnabled = false
        };
        Assert.That(DailyV1SelectedNpcActionDecisionStage.TryChoose(
            fixture.Graph, fixture.Actor, new[] { steal }, fixture.Actor.Status.Statuses,
            12L, new FixedPlanningRandom(0f), boundary,
            out DailyV1NpcActionCandidate selected, fixture.PersonStore), Is.True);

        Assert.That(selected, Is.Null,
            "A disabled provider contributes no positive autonomous candidate.");
        Assert.That(boundary.StealProjectionCalls, Is.Zero);
        Assert.That(boundary.LegacyProviderPlanCalls, Is.Zero,
            "A selected graph must not call the Unity provider planner for a disabled action.");
    }

    [Test]
    public void SelectedEscapePreparationMatchesSentenceUrgencyAndFailureMultiplier()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition actor = definitions.Npcs["MARCUS"];
        DailyV1ActionDefinition action = FirstAction(definitions);

        Assert.That(DailyV1CrimeActionPlanning.TryPlanEscapePrison(
            definitions, actor, action, true, false, 2,
            out bool canAttemptTwoDaySentence, out float twoDayUtility), Is.True);
        Assert.That(canAttemptTwoDaySentence, Is.True);
        Assert.That(twoDayUtility, Is.EqualTo(10f + (1f / 7f) * 20f).Within(0.00001f));
        Assert.That(DailyV1CrimeActionPlanning.GetEscapeSuccessChanceMultiplier(0), Is.EqualTo(1f));

        Assert.That(DailyV1CrimeActionPlanning.TryPlanEscapePrison(
            definitions, actor, action, true, false, 8,
            out bool canAttemptLongSentence, out float longSentenceUtility), Is.True);
        Assert.That(canAttemptLongSentence, Is.True);
        Assert.That(longSentenceUtility, Is.EqualTo(30f));
        Assert.That(DailyV1CrimeActionPlanning.GetEscapeSuccessChanceMultiplier(2),
            Is.EqualTo(0.64f).Within(0.000001f));
    }

    [TestCase(false, false, 8)]
    [TestCase(true, true, 8)]
    [TestCase(true, false, 1)]
    public void SelectedEscapePreparationRetainsArrestAndSentenceEligibility(
        bool isArrested,
        bool wasArrestedToday,
        int remainingDays)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1CrimeActionPlanning.TryPlanEscapePrison(
            definitions,
            definitions.Npcs["MARCUS"],
            FirstAction(definitions),
            isArrested,
            wasArrestedToday,
            remainingDays,
            out bool canAttempt,
            out float utility), Is.True);
        Assert.That(canAttempt, Is.False);
        Assert.That(utility, Is.Zero);
        Assert.That(DailyV1CrimeActionPlanning.GetEscapeSuccessChanceMultiplier(0), Is.EqualTo(1f));
    }

    [Test]
    public void SelectedEscapePreparationRejectsForeignActorAndActionNodes()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition actor = definitions.Npcs["MARCUS"];
        DailyV1ActionDefinition action = FirstAction(definitions);

        Assert.That(DailyV1CrimeActionPlanning.TryPlanEscapePrison(
            definitions, foreign.Npcs[actor.DefinitionId], action, true, false, 8,
            out bool foreignActorCanAttempt, out float foreignActorUtility), Is.False);
        Assert.That(foreignActorCanAttempt, Is.False);
        Assert.That(foreignActorUtility, Is.Zero);

        Assert.That(DailyV1CrimeActionPlanning.TryPlanEscapePrison(
            definitions, actor, foreign.Actions[action.DefinitionId], true, false, 8,
            out bool foreignActionCanAttempt, out float foreignActionUtility), Is.False);
        Assert.That(foreignActionCanAttempt, Is.False);
        Assert.That(foreignActionUtility, Is.Zero);
    }

    private static DailyV1ActionDefinition FirstAction(DailyV1EffectiveDefinitions definitions)
    {
        foreach (DailyV1ActionDefinition action in definitions.Actions.Values)
            return action;
        Assert.Fail("The selected fixture must contain an action node.");
        return null;
    }

    private static DailyV1ActionDefinition ActionOfType(
        DailyV1EffectiveDefinitions definitions,
        int actionType) => definitions.Actions.Values.Single(action =>
            action.Values.actionType?.Value == actionType);

    private static CrimePlanningFixture CreateCrimePlanningFixture(bool reverseRouteRegistration = false)
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        SpatialNetworkOwner spatial = new SpatialNetworkOwner();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions, spatial);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        DailyV1CityRuntimeOwners[] cities = builder.Cities.ToArray();
        List<SpatialRouteRuntime> routes = new List<SpatialRouteRuntime>();
        for (int i = 0; i < cities.Length; i++)
        {
            Assert.That(spatial.TryGetLocation(cities[i].LocationRuntimeId,
                out SpatialLocationRuntime origin), Is.True);
            for (int j = 0; j < cities.Length; j++)
            {
                if (i == j) continue;
                Assert.That(spatial.TryGetLocation(cities[j].LocationRuntimeId,
                    out SpatialLocationRuntime destination), Is.True);
                string routeId = "route/crime/" + cities[i].RuntimeId + "/" + cities[j].RuntimeId;
                routes.Add(new SpatialRouteRuntime(routeId, origin, destination, 3));
            }
        }
        if (reverseRouteRegistration) routes.Reverse();
        foreach (SpatialRouteRuntime route in routes)
            Assert.That(spatial.TryRegisterRoute(route, () => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityId => builder.ResolveUniqueStartingCity(cityId)?.RuntimeId), Is.True);
        DailyV1JusticeRecordsOwner justice = new DailyV1JusticeRecordsOwner();
        Assert.That(builder.TryBuild(justice,
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        DailyV1NpcRuntimeOwners actor = graph.Npcs.Single(owner =>
            owner.Definition.DefinitionId == "JOBSON");
        PersonStore persons = CreatePersonStore(graph);
        return new CrimePlanningFixture(graph, actor, justice, persons);
    }

    private static PersonStore CreatePersonStore(
        CrimePlanningFixture fixture,
        string omitNpcRuntimeId = null,
        string mismatchNpcRuntimeId = null) =>
        CreatePersonStore(fixture.Graph, omitNpcRuntimeId, mismatchNpcRuntimeId);

    private static PersonStore CreatePersonStore(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string omitNpcRuntimeId = null,
        string mismatchNpcRuntimeId = null)
    {
        PersonStore persons = new PersonStore();
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (string.Equals(npc.RuntimeId, omitNpcRuntimeId, System.StringComparison.Ordinal)) continue;
            PersonId personId = new PersonId("person/" + npc.RuntimeId);
            PersonRuntime person = new PersonRuntime(personId);
            Assert.That(persons.TryRegister(person, out _), Is.True);
            string materializedNpcId = string.Equals(npc.RuntimeId, mismatchNpcRuntimeId,
                System.StringComparison.Ordinal) ? "npc/mismatched" : npc.RuntimeId;
            Assert.That(persons.TryBindMaterializedNpc(personId, materializedNpcId, out _), Is.True);
        }
        return persons;
    }

    private static void SetMoneyBalance(MoneyAccountRuntime account, float targetBalance)
    {
        Assert.That(account, Is.Not.Null);
        if (account.Balance > targetBalance)
            Assert.That(account.TryDebit(account.Balance - targetBalance), Is.True);
        else if (account.Balance < targetBalance)
            Assert.That(account.TryCredit(targetBalance - account.Balance), Is.True);
        Assert.That(account.Balance, Is.EqualTo(targetBalance));
    }

    private sealed class CrimePlanningFixture
    {
        internal DailyV1SelectedRuntimeOwnerGraph Graph { get; }
        internal DailyV1NpcRuntimeOwners Actor { get; }
        internal DailyV1CityRuntimeOwners City { get; }
        internal DailyV1JusticeRecordsOwner Justice { get; }
        internal PersonStore PersonStore { get; }

        internal CrimePlanningFixture(
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners actor,
            DailyV1JusticeRecordsOwner justice,
            PersonStore personStore)
        {
            Graph = graph;
            Actor = actor;
            City = graph.Cities.Single(city => string.Equals(city.RuntimeId,
                actor.Travel.CurrentCityRuntimeId, System.StringComparison.Ordinal));
            Justice = justice;
            PersonStore = personStore;
        }
    }

    private sealed class FixedPlanningRandom : IAuthoritativeRandomSource
    {
        private readonly float value;
        internal List<string> StreamKeys { get; } = new List<string>();
        internal FixedPlanningRandom(float value) => this.value = value;
        public float NextUnit(string streamKey, long drawIndex = 0L)
        { StreamKeys.Add(streamKey); return value; }
        public DeterministicRandomStream CreateStream(string streamKey) => null;
    }

    private sealed class RecordingPlanningBoundary : IDailyV1SelectedNpcActionDecisionBoundary
    {
        internal int StealProjectionCalls { get; private set; }
        internal int FleeProjectionCalls { get; private set; }
        internal int LegacyProviderPlanCalls { get; private set; }
        internal bool CrimeProviderEnabled { get; set; } = true;

        public bool HasTravelActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => false;
        public TravelPartyStore GetTravelPartyStore(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => null;
        public bool TryProjectTravelAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool HasMerchantActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => false;
        public bool HasEnabledCrimeActionProvider(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action) => CrimeProviderEnabled;
        public bool TryProjectStealAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate)
        { StealProjectionCalls++; return candidate?.PreparedPayload != null; }
        public bool TryProjectFleeCityAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate)
        { FleeProjectionCalls++; return candidate?.PreparedPayload != null; }
        public bool TryProjectBuyAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryProjectSellAction(DailyV1NpcRuntimeOwners actor, DailyV1NpcActionCandidate candidate) => false;
        public bool TryPrepareAutonomousProviderAction(DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate candidate, out float utility)
        { LegacyProviderPlanCalls++; utility = 0f; return false; }
    }
}
