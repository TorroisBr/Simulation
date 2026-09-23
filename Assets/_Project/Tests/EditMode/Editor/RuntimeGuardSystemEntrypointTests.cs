using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class RuntimeGuardSystemEntrypointTests
{
    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void TravelAndPartySystemsRejectBeforeResolverIdsOrNpcMutation()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        SimulationRuntime runtimeA = new SimulationRuntime(new SimulationTime(), null, null);
        SimulationRuntime runtimeB = new SimulationRuntime(new SimulationTime(), null, null);
        int cityResolverCalls = 0;
        TravelSystem travel = new TravelSystem(
            fixture.World.Network,
            location =>
            {
                cityResolverCalls++;
                return fixture.World.CitiesByLocation.TryGetValue(location, out CityRuntime city) ? city : null;
            },
            1f,
            null,
            null);
        TravelPartyStore parties = new TravelPartyStore();
        TravelPartySystem partySystem = new TravelPartySystem(
            parties,
            fixture.Records.Allocator,
            fixture.World.IdentityRegistry,
            travel,
            fixture.Records.Time,
            fixture.Records.Sequence,
            fixture.Records.EventRecorder);

        object guard = GetGuard(runtimeA);
        Assert.That(Bind(partySystem, guard), Is.True);
        Assert.That(Bind(partySystem, guard), Is.True, "same-guard binding is idempotent");
        Assert.That(Bind(partySystem, GetGuard(runtimeB)), Is.False, "an authority cannot be reused by another runtime");
        MarkFaulted(runtimeA);

        float moneyBefore = fixture.Bruno.Money;
        Assert.That(partySystem.TryStartTravelParty(null, out TravelPartyRuntime party), Is.False);
        Assert.That(party, Is.Null);
        Assert.That(parties.ActiveParties, Is.Empty);
        Assert.That(fixture.Bruno.IsTraveling, Is.False);
        Assert.That(fixture.Bruno.Money, Is.EqualTo(moneyBefore));
        Assert.That(fixture.Records.Allocator.AllocateTravelPartyId(), Is.EqualTo("travel-party-000001"));

        Assert.That(travel.TryStartTravel(fixture.Bruno, fixture.World.B.Location, (CityRuntime)null), Is.False);
        Assert.That(cityResolverCalls, Is.Zero, "fault rejection precedes the city resolver callback");
        Assert.Throws<InvalidOperationException>(() => travel.AdvanceTravels(new List<NpcRuntime> { fixture.Bruno }));
        Assert.Throws<InvalidOperationException>(() => partySystem.AdvanceParties());
        Assert.That(parties.ActiveParties, Is.Empty);
    }

    [Test]
    public void MerchantAndKnowledgeSharingRejectFaultedMutationBeforeChangingKnowledge()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null);
        MerchantSystem merchantSystem = new MerchantSystem(
            new EffectiveMerchantTradeConfiguration(enabled: true),
            new EffectiveCommercialKnowledgeConfiguration(),
            fixture.Travel,
            fixture.Records.Time,
            fixture.Records.DecisionRecorder);
        CommercialKnowledgeSharingSystem sharingSystem = new CommercialKnowledgeSharingSystem(
            fixture.Records.Time,
            new EffectiveCommercialKnowledgeConfiguration());

        object guard = GetGuard(runtime);
        Assert.That(Bind(merchantSystem, guard), Is.True);
        Assert.That(Bind(merchantSystem, guard), Is.True, "same-guard binding is idempotent");
        Assert.That(
            Bind(merchantSystem, GetGuard(new SimulationRuntime(new SimulationTime(), null, null))),
            Is.False,
            "a merchant system and its time/travel dependencies cannot be reused by another runtime");
        Assert.That(Bind(sharingSystem, guard), Is.True);
        MarkFaulted(runtime);

        int brunoMarketCount = fixture.Bruno.CommercialKnowledge.Observations.Count;
        int caioMarketCount = fixture.Caio.CommercialKnowledge.Observations.Count;
        int brunoLiquidityCount = fixture.Bruno.CommercialKnowledge.LiquidityObservations.Count;
        int caioLiquidityCount = fixture.Caio.CommercialKnowledge.LiquidityObservations.Count;

        Assert.Throws<InvalidOperationException>(() => merchantSystem.ObserveCurrentMarket(fixture.Bruno));
        Assert.Throws<InvalidOperationException>(() => merchantSystem.AdvanceNpcTradeState(fixture.Bruno));
        Assert.Throws<InvalidOperationException>(() => merchantSystem.BootstrapInitialKnowledge(fixture.Bruno));
        Assert.That(merchantSystem.TryExecuteAction(fixture.Bruno, null).Success, Is.False);
        Assert.Throws<InvalidOperationException>(() => sharingSystem.ShareAmongPresentMerchants(fixture.Members));

        Assert.That(fixture.Bruno.CommercialKnowledge.Observations, Has.Count.EqualTo(brunoMarketCount));
        Assert.That(fixture.Caio.CommercialKnowledge.Observations, Has.Count.EqualTo(caioMarketCount));
        Assert.That(fixture.Bruno.CommercialKnowledge.LiquidityObservations, Has.Count.EqualTo(brunoLiquidityCount));
        Assert.That(fixture.Caio.CommercialKnowledge.LiquidityObservations, Has.Count.EqualTo(caioLiquidityCount));
    }

    [Test]
    public void SiteKnowledgeAndExpeditionSystemsRejectBeforeRecordingOrAllocating()
    {
        TravelPartyFixture fixture = SimulationTestFactory.CreateTravelPartyFixture();
        ExplorableSiteRuntime site = new ExplorableSiteRuntime(
            "site-guard-test",
            SimulationTestFactory.CreateExplorableSite("site-guard-test"),
            fixture.World.B.Location);
        SimulationRuntime runtime = new SimulationRuntime(new SimulationTime(), null, null);
        ExplorableSiteKnowledgeSystem siteKnowledge = new ExplorableSiteKnowledgeSystem();
        Assert.That(Bind(siteKnowledge, GetGuard(runtime)), Is.True);
        MarkFaulted(runtime);

        Assert.That(siteKnowledge.RecordDirectObservation(fixture.Bruno, site, 1L), Is.False);
        Assert.That(fixture.Bruno.ExplorableSiteKnowledge.KnowsSite(site.RuntimeId), Is.False);
        Assert.That(fixture.Bruno.SpatialKnowledge.KnowsLocation(site.Location.RuntimeId), Is.False);

        TravelPartyFixture expeditionFixture = SimulationTestFactory.CreateTravelPartyFixture();
        ExpeditionStore expeditions = new ExpeditionStore();
        ExplorableSiteStore sites = new ExplorableSiteStore();
        PlaceContentStore content = new PlaceContentStore(
            expeditionFixture.Records.Allocator,
            expeditionFixture.World.IdentityRegistry);
        LocalTopologyStore topologies = new LocalTopologyStore(expeditionFixture.World.IdentityRegistry);
        ExpeditionSystem expeditionSystem = CreateExpeditionSystem(
            expeditionFixture,
            expeditions,
            sites,
            content,
            topologies,
            new ExplorableSiteKnowledgeSystem());
        SimulationRuntime expeditionRuntime = new SimulationRuntime(new SimulationTime(), null, null);
        object expeditionGuard = GetGuard(expeditionRuntime);

        Assert.That(Bind(expeditionSystem, expeditionGuard), Is.True);
        Assert.That(
            Bind(expeditionSystem, GetGuard(new SimulationRuntime(new SimulationTime(), null, null))),
            Is.False,
            "an expedition system and its nested authorities cannot be reused by another runtime");
        MarkFaulted(expeditionRuntime);
        Assert.That(expeditionSystem.TryStartExpedition(null, null, out ExpeditionRuntime expedition), Is.False);
        Assert.That(expedition, Is.Null);
        Assert.That(expeditionFixture.Records.Allocator.AllocateExpeditionId(), Is.EqualTo("expedition-000001"));
        Assert.That(expeditionSystem.TryBeginReturn(null, out string reason), Is.False);
        Assert.That(reason, Does.Contain("faulted"));
        Assert.Throws<InvalidOperationException>(() => expeditionSystem.ReconcileAfterTravel());

        CountingPartyAssemblyPolicy policy = new CountingPartyAssemblyPolicy();
        AdventureExpeditionAutonomySystem autonomy = new AdventureExpeditionAutonomySystem(
            new AdventureAutonomySystem(null, policy),
            expeditionSystem,
            expeditions,
            sites,
            topologies,
            content,
            expeditionFixture.World.IdentityRegistry,
            expeditionFixture.World.Network,
            expeditionFixture.Records.DecisionRecorder,
            expeditionFixture.Records.Time);
        Assert.That(Bind(autonomy, expeditionGuard), Is.True);
        Assert.That(Bind(autonomy, GetGuard(new SimulationRuntime(new SimulationTime(), null, null))), Is.False);
        Assert.That(autonomy.TryStartAutonomousExpedition(expeditionFixture.Bruno, expeditionFixture.Members), Is.False);
        Assert.That(policy.AssembleCalls, Is.Zero, "fault rejection precedes candidate/party-selection callbacks");
        Assert.Throws<InvalidOperationException>(() => autonomy.AdvanceActiveExpeditions());
    }

    private static ExpeditionSystem CreateExpeditionSystem(
        TravelPartyFixture fixture,
        ExpeditionStore expeditions,
        ExplorableSiteStore sites,
        PlaceContentStore content,
        LocalTopologyStore topologies,
        ExplorableSiteKnowledgeSystem siteKnowledge)
    {
        return new ExpeditionSystem(
            expeditions,
            fixture.Records.Allocator,
            fixture.World.IdentityRegistry,
            sites,
            fixture.System,
            fixture.Parties,
            siteKnowledge,
            fixture.Records.Time,
            fixture.Records.EventRecorder,
            null,
            content,
            topologies);
    }

    private static object GetGuard(SimulationRuntime runtime)
    {
        PropertyInfo property = typeof(SimulationRuntime).GetProperty(
            "MutationGuard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(property, Is.Not.Null);
        return property.GetValue(runtime);
    }

    private static bool Bind(object authority, object guard)
    {
        MethodInfo method = authority.GetType().GetMethod(
            "TryBindMutationGuard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, authority.GetType().Name + " should expose its internal guard binding");
        return (bool)method.Invoke(authority, new[] { guard });
    }

    private static void MarkFaulted(SimulationRuntime runtime)
    {
        MethodInfo method = typeof(SimulationRuntime).GetMethod(
            "MarkAuthoritativeMutationFaulted",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(runtime, new object[] { AuthoritativeMutationFaultReason.RollbackRestoreFailed });
    }

    private sealed class CountingPartyAssemblyPolicy : IAdventurePartyAssemblyPolicy
    {
        public int AssembleCalls { get; private set; }

        public IReadOnlyList<AdventurePartySelection> Assemble(
            NpcRuntime decisionMaker,
            IReadOnlyList<NpcRuntime> availableNpcs)
        {
            AssembleCalls++;
            return new[] { new AdventurePartySelection(decisionMaker, NpcDecisionParticipantRole.Performer) };
        }
    }
}
