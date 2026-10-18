using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyV1MerchantActionPlanningTests
{
    [Test]
    public void LocalSalePreservesSelectedItemIdentityAndExistingUtilityOrdering()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["AFONSO"];
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        DailyV1ItemDefinition grain = definitions.Items["TRIGO"];
        List<DailyV1MerchantLocalSaleCandidate> candidates = new List<DailyV1MerchantLocalSaleCandidate>
        {
            new DailyV1MerchantLocalSaleCandidate(iron, 5, 10f, 15f, 1f, 4),
            new DailyV1MerchantLocalSaleCandidate(grain, 5, 1f, 9f, 1f, 4)
        };

        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, merchant, candidates, 1f, out DailyV1MerchantLocalSalePlan plan), Is.True);

        // The authored iron preference is 2x. Existing formula scores iron at 40
        // and grain at 32, despite grain's greater per-unit margin.
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.CandidateIndex, Is.EqualTo(0));
        Assert.That(plan.Item, Is.SameAs(iron));
        Assert.That(plan.Amount, Is.EqualTo(4));
        Assert.That(plan.ReferencePrice, Is.EqualTo(10f));
        Assert.That(plan.ProfitPerItem, Is.EqualTo(5f));
        Assert.That(plan.Score, Is.EqualTo(40f));
    }

    [Test]
    public void LocalSaleUsesTypedBasePriceAndOrdinalTieBreak()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["JORGE"];
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        DailyV1ItemDefinition grain = definitions.Items["TRIGO"];
        List<DailyV1MerchantLocalSaleCandidate> candidates = new List<DailyV1MerchantLocalSaleCandidate>
        {
            new DailyV1MerchantLocalSaleCandidate(grain, 2, 0f, grain.Values.basePrice + 4f, 1f, 1),
            new DailyV1MerchantLocalSaleCandidate(iron, 2, 0f, iron.Values.basePrice + 4f, 1f, 1)
        };

        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, merchant, candidates, 1f, out DailyV1MerchantLocalSalePlan plan), Is.True);

        Assert.That(plan.Item, Is.SameAs(iron),
            "Equal scores retain the existing ordinal DefinitionId tie-break rather than inventory order.");
        Assert.That(plan.ReferencePrice, Is.EqualTo(iron.Values.basePrice),
            "Selected planning reads the captured effective item value, not a Unity ItemData projection.");
    }

    [Test]
    public void LocalSaleRejectsForeignMerchantAndItemNodes()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["JORGE"];
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        DailyV1MerchantLocalSaleCandidate candidate = new DailyV1MerchantLocalSaleCandidate(
            item, 3, 1f, 5f, 1f, 2);

        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, foreign.Npcs[merchant.DefinitionId], new[] { candidate }, 0f, out _), Is.False);
        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, merchant,
            new[] { new DailyV1MerchantLocalSaleCandidate(foreign.Items[item.DefinitionId], 3, 1f, 5f, 1f, 2) },
            0f, out _), Is.False);
    }

    [Test]
    public void LocalSaleSkipsStaleEmptyAndBelowThresholdCandidates()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["JORGE"];
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        List<DailyV1MerchantLocalSaleCandidate> candidates = new List<DailyV1MerchantLocalSaleCandidate>
        {
            new DailyV1MerchantLocalSaleCandidate(iron, 5, 10f, 20f, 0f, 5),
            new DailyV1MerchantLocalSaleCandidate(iron, 0, 10f, 20f, 1f, 5),
            new DailyV1MerchantLocalSaleCandidate(iron, 5, 10f, 10.5f, 1f, 5)
        };

        Assert.That(DailyV1MerchantActionPlanning.TryPlanLocalSale(
            definitions, merchant, candidates, 1f, out DailyV1MerchantLocalSalePlan plan), Is.True);
        Assert.That(plan, Is.Null);
    }

    [Test]
    public void TradeOpportunityAppliesTypedPreferenceAndExactProfitScoring()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["AFONSO"];
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        DailyV1ItemDefinition grain = definitions.Items["TRIGO"];
        List<DailyV1MerchantTradeCandidate> candidates = new List<DailyV1MerchantTradeCandidate>
        {
            new DailyV1MerchantTradeCandidate(iron, "city-iron", 2, 10f, 16f, 4f, 2, 0.5f, 0.8f),
            new DailyV1MerchantTradeCandidate(grain, "city-grain", 1, 1f, 11f, 0f, 1, 1f, 1f)
        };

        Assert.That(DailyV1MerchantActionPlanning.TryPlanTradeOpportunity(
            definitions, merchant, candidates, 1f, out DailyV1MerchantTradePlan plan), Is.True);

        // Iron's net profit is 8; 8 / 2 travel days * min(.5,.8) * authored 2x preference = 4.
        // Grain's score is 10, so it wins despite the Iron preference.
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.CandidateIndex, Is.EqualTo(1));
        Assert.That(plan.Item, Is.SameAs(grain));
        Assert.That(plan.NetProfit, Is.EqualTo(10f));
        Assert.That(plan.Score, Is.EqualTo(10f));
    }

    [Test]
    public void TradeOpportunityTieBreaksByDestinationBeforeItemIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["AFONSO"];
        DailyV1ItemDefinition iron = definitions.Items["FERRO"];
        DailyV1ItemDefinition grain = definitions.Items["TRIGO"];
        List<DailyV1MerchantTradeCandidate> candidates = new List<DailyV1MerchantTradeCandidate>
        {
            new DailyV1MerchantTradeCandidate(iron, "city-z", 1, 10f, 15f, 0f, 1, 1f, 1f),
            new DailyV1MerchantTradeCandidate(grain, "city-a", 1, 1f, 11f, 0f, 1, 1f, 1f),
            new DailyV1MerchantTradeCandidate(iron, "city-a", 1, 10f, 15f, 0f, 1, 1f, 1f)
        };

        Assert.That(DailyV1MerchantActionPlanning.TryPlanTradeOpportunity(
            definitions, merchant, candidates, 1f, out DailyV1MerchantTradePlan plan), Is.True);

        Assert.That(plan.CandidateIndex, Is.EqualTo(2),
            "Target city ordering wins first; equal city and score then use the ordinal item DefinitionId.");
        Assert.That(plan.TargetRuntimeId, Is.EqualTo("city-a"));
        Assert.That(plan.Item, Is.SameAs(iron));
    }

    [Test]
    public void TradeOpportunityRejectsForeignNodesAndSkipsIneligibleCandidates()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition merchant = definitions.Npcs["JORGE"];
        DailyV1ItemDefinition item = definitions.Items["FERRO"];

        Assert.That(DailyV1MerchantActionPlanning.TryPlanTradeOpportunity(
            definitions,
            foreign.Npcs[merchant.DefinitionId],
            new[] { new DailyV1MerchantTradeCandidate(item, "city-a", 1, 1f, 5f, 0f, 1, 1f, 1f) },
            0f,
            out _), Is.False);
        Assert.That(DailyV1MerchantActionPlanning.TryPlanTradeOpportunity(
            definitions,
            merchant,
            new[] { new DailyV1MerchantTradeCandidate(foreign.Items[item.DefinitionId], "city-a", 1, 1f, 5f, 0f, 1, 1f, 1f) },
            0f,
            out _), Is.False);

        Assert.That(DailyV1MerchantActionPlanning.TryPlanTradeOpportunity(
            definitions,
            merchant,
            new[]
            {
                new DailyV1MerchantTradeCandidate(item, "city-a", 1, 1f, 10f, 0f, 0, 1f, 1f),
                new DailyV1MerchantTradeCandidate(item, "city-b", 0, 1f, 10f, 0f, 1, 1f, 1f)
            },
            0f,
            out DailyV1MerchantTradePlan plan), Is.True);
        Assert.That(plan, Is.Null);
    }
}
