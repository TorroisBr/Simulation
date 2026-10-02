using System.Reflection;
using System.Threading;
using NUnit.Framework;

public sealed class NpcPlanCensusTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void ProviderNamesExactEmbeddedPlanOwnersAsSingletonSections()
    {
        NpcRuntime npc = new NpcRuntime("npc-plan-census", SimulationTestFactory.CreateNpc("plan-census"));
        MerchantTradePlanRuntime merchantPlan = npc.MerchantTradePlan;
        NpcTravelPlanRuntime travelPlan = npc.TravelPlan;

        var providers = NpcPlanCensusProvider.CreateProviders(new[] { npc });

        Assert.That(providers.Count, Is.EqualTo(2));
        OwnerSectionCensusWitness merchant = providers[0].GetCurrentCensus();
        OwnerSectionCensusWitness travel = providers[1].GetCurrentCensus();
        Assert.That(merchant.SectionId,
            Is.EqualTo(NpcPlanCensusProvider.MerchantTradePlanSectionPrefix + npc.RuntimeId));
        Assert.That(merchant.OwnerInstanceIdentity, Is.SameAs(merchantPlan));
        Assert.That(merchant.Cardinality, Is.EqualTo(1));
        Assert.That(merchant.Revision, Is.Zero);
        Assert.That(travel.SectionId,
            Is.EqualTo(NpcPlanCensusProvider.TravelPlanSectionPrefix + npc.RuntimeId));
        Assert.That(travel.OwnerInstanceIdentity, Is.SameAs(travelPlan));
        Assert.That(travel.Cardinality, Is.EqualTo(1));
        Assert.That(travel.Revision, Is.Zero);
    }

    [Test]
    public void PlanLocalRevisionsAdvanceOnlyForSuccessfulStateChangesAndRejectSaturation()
    {
        CityRuntime firstCity = SimulationTestFactory.CreateCity("plan-first", "plan-first-location");
        CityRuntime secondCity = SimulationTestFactory.CreateCity("plan-second", "plan-second-location");
        ItemData item = SimulationTestFactory.CreateItem("plan-item");
        NpcRuntime npc = new NpcRuntime("npc-plan-revision", SimulationTestFactory.CreateNpc("plan-revision"));
        MerchantTradePlanRuntime merchant = npc.MerchantTradePlan;
        NpcTravelPlanRuntime travel = npc.TravelPlan;

        merchant.Set(item, firstCity, firstCity, 3, 4f);
        Assert.That(merchant.Revision, Is.EqualTo(1));
        merchant.Set(item, firstCity, firstCity, 3, 4f);
        Assert.That(merchant.Revision, Is.EqualTo(1), "a no-op plan installation has no revision delta");
        merchant.RedirectTo(secondCity);
        Assert.That(merchant.Revision, Is.EqualTo(2));
        Assert.That(merchant.TargetCity, Is.SameAs(secondCity));

        travel.Set(firstCity, NpcTravelReason.Trade, 1f, 2f, "decision-a");
        Assert.That(travel.Revision, Is.EqualTo(1));
        travel.Set(firstCity, NpcTravelReason.Trade, 1f, 2f, "decision-a");
        Assert.That(travel.Revision, Is.EqualTo(1));

        SetRevision(merchant, long.MaxValue);
        merchant.RedirectTo(firstCity);
        Assert.That(merchant.TargetCity, Is.SameAs(secondCity));
        Assert.That(merchant.Revision, Is.EqualTo(long.MaxValue));

        SetRevision(travel, long.MaxValue);
        travel.Clear();
        Assert.That(travel.IsActive, Is.True);
        Assert.That(travel.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void PlanFamilyRejectsOneEmbeddedOwnerAliasedAcrossActors()
    {
        NpcRuntime first = new NpcRuntime("npc-plan-alias-a", SimulationTestFactory.CreateNpc("plan-alias-a"));
        NpcRuntime second = new NpcRuntime("npc-plan-alias-b", SimulationTestFactory.CreateNpc("plan-alias-b"));
        _ = first.MerchantTradePlan;
        _ = second.MerchantTradePlan;
        typeof(NpcRuntime).GetField("merchantTradePlan", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(second, first.MerchantTradePlan);
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();

        Assert.That(protocol.RegisterNpcPlanRosterFamily(new[] { first, second },
            out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.OwnerCoverageIncomplete));
    }

    [Test]
    public void BoundPlanMutationOnWrongThreadIsRejectedBeforeOwnerFactsChange()
    {
        NpcRuntime npc = new NpcRuntime("npc-plan-thread", SimulationTestFactory.CreateNpc("plan-thread"));
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { npc },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        MerchantTradePlanRuntime plan = npc.MerchantTradePlan;

        Thread wrongThread = new Thread(() => plan.Set(null, null, null, 4, 1f));
        wrongThread.Start();
        wrongThread.Join();

        Assert.That(plan.HasData, Is.False);
        Assert.That(plan.Revision, Is.Zero);
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    [Test]
    public void BoundKnowledgeMutationsOnWrongThreadAreRejectedBeforeOwnerFactsChange()
    {
        NpcRuntime spatialNpc = new NpcRuntime("npc-spatial-thread", SimulationTestFactory.CreateNpc("spatial-thread"));
        SimulationRuntime spatialRuntime = CreateBoundRuntime(spatialNpc);
        string locationId = "off-thread-location";
        bool spatialChanged = true;
        Thread spatialThread = new Thread(() =>
        {
            spatialChanged = spatialNpc.SpatialKnowledge.DiscoverLocation(locationId);
        });
        spatialThread.Start();
        spatialThread.Join();

        Assert.That(spatialChanged, Is.False);
        Assert.That(spatialNpc.SpatialKnowledge.KnowsLocation(locationId), Is.False);
        Assert.That(spatialNpc.SpatialKnowledge.Revision, Is.Zero);
        Assert.That(spatialRuntime.TryAssessNpcRosterCensus(out ContinuationCensusFailure spatialFailure), Is.False);
        Assert.That(spatialFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));

        NpcRuntime commercialNpc = new NpcRuntime("npc-commercial-thread", SimulationTestFactory.CreateNpc("commercial-thread"));
        SimulationRuntime commercialRuntime = CreateBoundRuntime(commercialNpc);
        ItemData item = SimulationTestFactory.CreateItem("commercial-thread-item");
        CommercialMarketObservation observation = SimulationTestFactory.CreateObservation(
            "commercial-thread-location", item, 5f, 2, 0L, 0L);
        bool commercialChanged = true;
        Thread commercialThread = new Thread(() =>
        {
            commercialChanged = commercialNpc.CommercialKnowledge.RecordObservation(observation);
        });
        commercialThread.Start();
        commercialThread.Join();

        Assert.That(commercialChanged, Is.False);
        Assert.That(commercialNpc.CommercialKnowledge.Observations, Is.Empty);
        Assert.That(commercialNpc.CommercialKnowledge.Revision, Is.Zero);
        Assert.That(commercialRuntime.TryAssessNpcRosterCensus(out ContinuationCensusFailure commercialFailure), Is.False);
        Assert.That(commercialFailure, Is.EqualTo(ContinuationCensusFailure.ProtocolFaulted));
    }

    private static SimulationRuntime CreateBoundRuntime(NpcRuntime npc)
    {
        return new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { npc },
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
    }

    private static void SetRevision(object owner, long value)
    {
        owner.GetType().GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(owner, value);
    }
}
