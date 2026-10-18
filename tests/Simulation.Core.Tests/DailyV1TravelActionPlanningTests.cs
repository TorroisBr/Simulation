using NUnit.Framework;

public sealed class DailyV1TravelActionPlanningTests
{
    [Test]
    public void SelectedTravelPlanUtilityMatchesTradeUrgencyAndClampRules()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition actor = definitions.Npcs["MARCUS"];
        DailyV1ActionDefinition action = FirstAction(definitions);

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, actor, action, 40f, true, true, 2, 12f,
            out float tradeUtility), Is.True);
        Assert.That(tradeUtility, Is.EqualTo(60f));

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, actor, action, 90f, true, true, 12, 15f,
            out float cappedUtility), Is.True);
        Assert.That(cappedUtility, Is.EqualTo(100f),
            "The plan-plus-trade urgency is clamped before maxing against the candidate utility.");

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, actor, action, -20f, true, false, 0, 3f,
            out float floorUtility), Is.True);
        Assert.That(floorUtility, Is.EqualTo(3f));

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, actor, action, 45f, false, true, 9, 12f,
            out float nonTradeUtility), Is.True);
        Assert.That(nonTradeUtility, Is.EqualTo(45f),
            "Merchant urgency applies only to a Trade travel plan that matches the current merchant plan.");
    }

    [Test]
    public void SelectedTravelPlanUtilityRejectsForeignActorAndActionNodes()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition actor = definitions.Npcs["MARCUS"];
        DailyV1ActionDefinition action = FirstAction(definitions);

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, foreign.Npcs[actor.DefinitionId], action, 40f, true, true, 2, 12f,
            out float foreignActorUtility), Is.False);
        Assert.That(foreignActorUtility, Is.Zero);

        Assert.That(DailyV1TravelActionPlanning.TryApplyExistingTravelPlanUtility(
            definitions, actor, foreign.Actions[action.DefinitionId], 40f, true, true, 2, 12f,
            out float foreignActionUtility), Is.False);
        Assert.That(foreignActionUtility, Is.Zero);
    }

    private static DailyV1ActionDefinition FirstAction(DailyV1EffectiveDefinitions definitions)
    {
        foreach (DailyV1ActionDefinition action in definitions.Actions.Values)
            return action;
        Assert.Fail("The selected fixture must contain an action node.");
        return null;
    }
}
