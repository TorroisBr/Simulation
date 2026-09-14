using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class GeneralizedSpatialTravelTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void StartingCityEstablishesCanonicalLocationAndCityProjection()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime npc = fixture.CreateNpc("starting-city", fixture.CityA, 10f);

        Assert.That(npc.CurrentCity, Is.SameAs(fixture.CityA));
        Assert.That(npc.CurrentLocation, Is.SameAs(fixture.CityA.Location));
        Assert.That(fixture.CityA.ImportantNpcs, Has.Member(npc));
    }

    [Test]
    public void NpcCanBePlacedAtNonCityLocationWithoutCityProjection()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime npc = fixture.CreateNpc("site-resident", fixture.CityA, 10f);

        Assert.That(npc.SetCurrentPresence(fixture.Site.Location), Is.True);

        Assert.That(npc.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(npc.CurrentCity, Is.Null);
        Assert.That(fixture.CityA.ImportantNpcs.Contains(npc), Is.False);
    }

    [Test]
    public void PresenceTransitionRejectsMismatchedCityProjection()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime npc = fixture.CreateNpc("projection", fixture.CityA, 10f);

        Assert.That(npc.SetCurrentPresence(fixture.CityB.Location, fixture.CityA), Is.False);
        Assert.That(npc.CurrentLocation, Is.SameAs(fixture.CityA.Location));
        Assert.That(npc.CurrentCity, Is.SameAs(fixture.CityA));
    }

    [Test]
    public void IsTravelingAndPresenceAreSpatialForSiteDestination()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime npc = fixture.CreateNpc("site-traveler", fixture.CityA, 10f);

        Assert.That(fixture.Travel.TryStartTravel(npc, fixture.Site.Location, fixture.SiteRoute.TravelDays), Is.True);

        Assert.That(npc.IsTraveling, Is.True);
        Assert.That(npc.CurrentLocation, Is.Null);
        Assert.That(npc.CurrentCity, Is.Null);
        Assert.That(npc.DestinationLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(npc.DestinationCity, Is.Null);
    }

    [Test]
    public void CityToSiteAndSiteToCityUseTheSameSpatialTravelCore()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime npc = fixture.CreateNpc("round-trip", fixture.CityA, 20f);

        Assert.That(fixture.Travel.TryStartTravel(npc, fixture.Site.Location, fixture.SiteRoute.TravelDays), Is.True);
        npc.ClearTravelStartedToday();
        Assert.That(fixture.Travel.AdvanceTravels(new List<NpcRuntime> { npc }).Count, Is.EqualTo(0));
        Assert.That(fixture.Travel.AdvanceTravels(new List<NpcRuntime> { npc }).Count, Is.EqualTo(1));
        Assert.That(npc.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(npc.CurrentCity, Is.Null);

        Assert.That(fixture.Travel.TryStartTravel(npc, fixture.CityA.Location, fixture.SiteRoute.TravelDays), Is.True);
        npc.ClearTravelStartedToday();
        Assert.That(fixture.Travel.AdvanceTravels(new List<NpcRuntime> { npc }).Count, Is.EqualTo(0));
        Assert.That(fixture.Travel.AdvanceTravels(new List<NpcRuntime> { npc }).Count, Is.EqualTo(1));
        Assert.That(npc.CurrentLocation, Is.SameAs(fixture.CityA.Location));
        Assert.That(npc.CurrentCity, Is.SameAs(fixture.CityA));
        Assert.That(fixture.CityA.ImportantNpcs.FindAll(candidate => candidate == npc).Count, Is.EqualTo(1));
    }

    [Test]
    public void GenericTravelDaysAndCityWrapperReturnTheSameTruth()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();

        Assert.That(
            fixture.Travel.GetTravelDays(fixture.CityA.Location, fixture.CityB.Location),
            Is.EqualTo(fixture.Travel.GetTravelDays(fixture.CityA, fixture.CityB)));
        Assert.That(
            fixture.Travel.GetTravelDays(fixture.CityA.Location, fixture.Site.Location),
            Is.EqualTo(fixture.SiteRoute.TravelDays));
    }

    [Test]
    public void KnownTravelRequiresOriginDestinationAndRouteKnowledge()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime npc = fixture.CreateNpc("known-traveler", fixture.CityA, 10f);
        npc.SpatialKnowledge.DiscoverLocation(fixture.CityA.Location.RuntimeId);
        npc.SpatialKnowledge.DiscoverLocation(fixture.Site.Location.RuntimeId);

        Assert.That(
            fixture.Travel.CanPlanKnownTravel(npc, fixture.Site.Location, out _, out _),
            Is.False);

        npc.SpatialKnowledge.DiscoverRoute(fixture.SiteRoute.RuntimeId);

        Assert.That(
            fixture.Travel.CanPlanKnownTravel(npc, fixture.Site.Location, out int travelDays, out float travelCost),
            Is.True);
        Assert.That(travelDays, Is.EqualTo(fixture.SiteRoute.TravelDays));
        Assert.That(travelCost, Is.EqualTo(fixture.SiteRoute.TravelDays));
    }

    [Test]
    public void TruthExecutionCanTravelToSiteWithoutDestinationKnowledge()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime npc = fixture.CreateNpc("truth-traveler", fixture.CityA, 10f);

        Assert.That(fixture.Travel.TryStartTravel(npc, fixture.Site.Location, fixture.SiteRoute.TravelDays), Is.True);
        Assert.That(npc.SpatialKnowledge.KnowsLocation(fixture.Site.Location.RuntimeId), Is.False);
        Assert.That(npc.SpatialKnowledge.KnowsRoute(fixture.SiteRoute.RuntimeId), Is.True);
    }

    [Test]
    public void SiteArrivalProducesDirectObservationOnlyForArrivedNpc()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime npc = fixture.CreateNpc("observer", fixture.CityA, 10f);
        ExplorableSiteRuntime secondSite = fixture.AddSecondSiteAtTargetLocation();
        ExplorableSiteKnowledgeSystem knowledgeSystem = new ExplorableSiteKnowledgeSystem();

        Assert.That(fixture.Travel.TryStartTravel(npc, fixture.Site.Location, fixture.SiteRoute.TravelDays), Is.True);
        npc.ClearTravelStartedToday();

        SimulationRuntime runtime = new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.CityA, fixture.CityB },
            new[] { npc },
            economyEnabled: false,
            travelSystem: fixture.Travel,
            explorableSiteStore: fixture.Sites,
            explorableSiteKnowledgeSystem: knowledgeSystem);

        runtime.AdvanceDays(fixture.SiteRoute.TravelDays);

        Assert.That(npc.ExplorableSiteKnowledge.KnowsSite(fixture.Site.RuntimeId), Is.True);
        Assert.That(npc.ExplorableSiteKnowledge.KnowsSite(secondSite.RuntimeId), Is.True);
        Assert.That(npc.ExplorableSiteKnowledge.TryGetObservation(
            fixture.Site.RuntimeId,
            out ExplorableSiteKnowledgeObservation observation), Is.True);
        Assert.That(observation.Source, Is.EqualTo(ExplorableSiteKnowledgeSource.DirectObservation));
        Assert.That(observation.ObservedDay, Is.EqualTo(runtime.CurrentDay));
        Assert.That(npc.ExplorableSiteKnowledge.KnowsSite(fixture.UnrelatedSite.RuntimeId), Is.False);
    }

    [Test]
    public void MerchantAtSiteDoesNotObserveOrShareAMarket()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime merchant = fixture.CreateNpc(
            "merchant-at-site",
            fixture.CityA,
            10f,
            NpcJobType.Merchant,
            MerchantBehavior.Traveling);
        ItemData item = SimulationTestFactory.CreateItem("site-market-item");
        fixture.CityA.Market.AddStock(item, 10, 10);

        Assert.That(fixture.Travel.TryStartTravel(merchant, fixture.Site.Location, fixture.SiteRoute.TravelDays), Is.True);
        merchant.ClearTravelStartedToday();

        SimulationRuntime runtime = new SimulationRuntime(
            fixture.Records.Time,
            new[] { fixture.CityA, fixture.CityB },
            new[] { merchant },
            economyEnabled: false,
            travelSystem: fixture.Travel,
            merchantSystem: fixture.MerchantSystem);

        Assert.DoesNotThrow(() => runtime.AdvanceDay());
        Assert.That(merchant.CurrentCity, Is.Null);
        Assert.That(merchant.CommercialKnowledge.TryGetObservation(
            fixture.Site.Location.RuntimeId,
            item.DefinitionId,
            out _), Is.False);
    }

    [Test]
    public void GroupTravelCanStartFromCityAndArriveAtSite()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("group-performer", fixture.CityA, 10f);
        NpcRuntime support = fixture.CreateNpc("group-support", fixture.CityA, 10f);
        TravelPartyStore parties = new TravelPartyStore();
        TravelPartySystem groups = new TravelPartySystem(
            parties,
            fixture.Records.Allocator,
            fixture.IdentityRegistry,
            fixture.Travel,
            fixture.Records.Time,
            fixture.Records.Sequence,
            fixture.Records.EventRecorder);

        ActionExecutionContext context = CreateGroupContext(
            performer,
            support,
            fixture.Site.Location,
            fixture.SiteRoute.RuntimeId);

        Assert.That(groups.TryStartTravelParty(context, out TravelPartyRuntime party), Is.True);
        Assert.That(party.DestinationLocationRuntimeId, Is.EqualTo(fixture.Site.Location.RuntimeId));
        Assert.That(performer.DestinationCity, Is.Null);
        Assert.That(support.DestinationCity, Is.Null);

        performer.ClearTravelStartedToday();
        support.ClearTravelStartedToday();
        Assert.That(groups.AdvanceParties().Count, Is.EqualTo(0));
        Assert.That(groups.AdvanceParties().Count, Is.EqualTo(2));
        Assert.That(parties.ActiveParties.Count, Is.EqualTo(0));
        Assert.That(performer.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(support.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(performer.CurrentCity, Is.Null);
        Assert.That(support.CurrentCity, Is.Null);
    }

    [Test]
    public void GroupTravelCanStartFromCommonNonCityLocation()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("site-performer", fixture.CityA, 10f);
        NpcRuntime support = fixture.CreateNpc("site-support", fixture.CityA, 10f);
        performer.SetCurrentPresence(fixture.Site.Location);
        support.SetCurrentPresence(fixture.Site.Location);
        TravelPartyStore parties = new TravelPartyStore();
        TravelPartySystem groups = new TravelPartySystem(
            parties,
            fixture.Records.Allocator,
            fixture.IdentityRegistry,
            fixture.Travel,
            fixture.Records.Time,
            fixture.Records.Sequence,
            fixture.Records.EventRecorder);

        ActionExecutionContext context = CreateGroupContext(
            performer,
            support,
            fixture.CityA.Location,
            fixture.SiteToCityRoute.RuntimeId);

        Assert.That(groups.TryStartTravelParty(context, out _), Is.True);
        Assert.That(performer.CurrentCity, Is.Null);
        Assert.That(support.CurrentCity, Is.Null);
        Assert.That(performer.CurrentLocation, Is.Null);
        Assert.That(performer.DestinationLocation, Is.SameAs(fixture.CityA.Location));
    }

    [Test]
    public void FailedGroupStartRestoresNonCityPresenceAndMoney()
    {
        SpatialTravelFixture fixture = new SpatialTravelFixture();
        NpcRuntime performer = fixture.CreateNpc("rollback-performer", fixture.CityA, fixture.SiteRoute.TravelDays);
        NpcRuntime support = fixture.CreateNpc("rollback-support", fixture.CityA, 0f);
        performer.SetCurrentPresence(fixture.Site.Location);
        support.SetCurrentPresence(fixture.Site.Location);
        float originalMoney = performer.Money;
        TravelPartyStore parties = new TravelPartyStore();
        TravelPartySystem groups = new TravelPartySystem(
            parties,
            fixture.Records.Allocator,
            fixture.IdentityRegistry,
            fixture.Travel,
            fixture.Records.Time,
            fixture.Records.Sequence,
            fixture.Records.EventRecorder);

        Assert.That(groups.TryStartTravelParty(CreateGroupContext(
            performer,
            support,
            fixture.CityA.Location,
            fixture.SiteToCityRoute.RuntimeId), out _), Is.False);

        Assert.That(parties.ActiveParties.Count, Is.EqualTo(0));
        Assert.That(performer.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(performer.CurrentCity, Is.Null);
        Assert.That(performer.DestinationLocation, Is.Null);
        Assert.That(performer.Money, Is.EqualTo(originalMoney));
        Assert.That(support.CurrentLocation, Is.SameAs(fixture.Site.Location));
        Assert.That(support.Money, Is.EqualTo(0f));
    }

    private static ActionExecutionContext CreateGroupContext(
        NpcRuntime performer,
        NpcRuntime support,
        SpatialLocationRuntime destination,
        string routeRuntimeId)
    {
        return new ActionExecutionContext(
            "group-travel-generalized",
            new[]
            {
                new ActionExecutionParticipant(performer.RuntimeId, ActionExecutionParticipantRole.Performer),
                new ActionExecutionParticipant(support.RuntimeId, ActionExecutionParticipantRole.Support)
            },
            destination.RuntimeId,
            routeRuntimeId);
    }
}

internal sealed class SpatialTravelFixture
{
    public CityRuntime CityA { get; }
    public CityRuntime CityB { get; }
    public ExplorableSiteRuntime Site { get; }
    public ExplorableSiteRuntime UnrelatedSite { get; }
    public SpatialRouteRuntime SiteRoute { get; }
    public SpatialRouteRuntime SiteToCityRoute { get; }
    public RuntimeIdentityRegistry IdentityRegistry { get; }
    public SpatialNetworkRuntime Network { get; }
    public ExplorableSiteStore Sites { get; }
    public ExplorableSiteKnowledgeSystem Knowledge { get; }
    public RecordFixture Records { get; }
    public TravelSystem Travel { get; }
    public MerchantSystem MerchantSystem { get; }
    public TravelPartySystem TravelPartySystem { get; private set; }
    public TravelPartyStore TravelParties { get; private set; }

    private readonly Dictionary<SpatialLocationRuntime, CityRuntime> citiesByLocation;

    public SpatialTravelFixture()
    {
        CityA = SimulationTestFactory.CreateCity("spatial-city-a", "spatial-location-a");
        CityB = SimulationTestFactory.CreateCity("spatial-city-b", "spatial-location-b");
        SpatialLocationRuntime siteLocation = new SpatialLocationRuntime("spatial-site-location");
        SpatialLocationRuntime unrelatedLocation = new SpatialLocationRuntime("spatial-unrelated-location");
        Site = CreateSite("spatial-site", siteLocation);
        UnrelatedSite = CreateSite("spatial-unrelated-site", unrelatedLocation);
        SiteRoute = new SpatialRouteRuntime("spatial-route-a-site", CityA.Location, Site.Location, 2);
        SiteToCityRoute = new SpatialRouteRuntime("spatial-route-site-a", Site.Location, CityA.Location, 2);
        IdentityRegistry = new RuntimeIdentityRegistry();
        IdentityRegistry.RegisterExplorableSite(Site);
        IdentityRegistry.RegisterExplorableSite(UnrelatedSite);
        Network = new SpatialNetworkRuntime(IdentityRegistry);
        Network.RegisterLocation(CityA.Location);
        Network.RegisterLocation(CityB.Location);
        Network.RegisterLocation(Site.Location);
        Network.RegisterLocation(UnrelatedSite.Location);
        Network.RegisterRoute(SiteRoute);
        Network.RegisterRoute(SiteToCityRoute);
        SpatialRouteRuntime cityToCity = new SpatialRouteRuntime(
            "spatial-route-a-b",
            CityA.Location,
            CityB.Location,
            1);
        Network.RegisterRoute(cityToCity);
        citiesByLocation = new Dictionary<SpatialLocationRuntime, CityRuntime>
        {
            { CityA.Location, CityA },
            { CityB.Location, CityB }
        };
        Sites = new ExplorableSiteStore();
        Sites.Add(Site);
        Sites.Add(UnrelatedSite);
        Knowledge = new ExplorableSiteKnowledgeSystem();
        Records = SimulationTestFactory.CreateRecordFixture();
        Travel = new TravelSystem(
            Network,
            location => citiesByLocation.TryGetValue(location, out CityRuntime city) ? city : null,
            1f,
            Records.EventRecorder,
            null);
        MerchantSystem = SimulationTestFactory.CreateMerchantSystem(Travel, Records.Time);
    }

    public void SetTravelPartySystem(TravelPartySystem system)
    {
        TravelPartySystem = system;
        TravelParties = system != null ? system.Store : null;
    }

    public NpcRuntime CreateNpc(
        string id,
        CityRuntime city,
        float money,
        NpcJobType jobType = NpcJobType.None,
        MerchantBehavior merchantBehavior = MerchantBehavior.Traveling)
    {
        NpcRuntime npc = new NpcRuntime(
            id,
            SimulationTestFactory.CreateNpc(id, jobType, merchantBehavior),
            city,
            money);
        Assert.That(IdentityRegistry.RegisterNpc(npc), Is.True);
        return npc;
    }

    public ExplorableSiteRuntime AddSecondSiteAtTargetLocation()
    {
        ExplorableSiteRuntime secondSite = CreateSite(
            "spatial-site-second",
            Site.Location);
        IdentityRegistry.RegisterExplorableSite(secondSite);
        Sites.Add(secondSite);
        return secondSite;
    }

    private static ExplorableSiteRuntime CreateSite(string id, SpatialLocationRuntime location)
    {
        return new ExplorableSiteRuntime(
            id + "-runtime",
            SimulationTestFactory.CreateExplorableSite(id),
            location);
    }
}
