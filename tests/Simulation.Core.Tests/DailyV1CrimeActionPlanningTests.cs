using NUnit.Framework;

public sealed class DailyV1CrimeActionPlanningTests
{
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
}
