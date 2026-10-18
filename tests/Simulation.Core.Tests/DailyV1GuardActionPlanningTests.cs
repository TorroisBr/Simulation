using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyV1GuardActionPlanningTests
{
    [Test]
    public void SelectedArrestPlanningMatchesPreviousOrderedTargetAndUtilityRule()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition guard = definitions.Npcs["MARCUS"];
        DailyV1ActionDefinition action = FirstAction(definitions);

        AssertMatchesPreviousRule(definitions, guard, action, new[]
        {
            new DailyV1GuardArrestCandidate("guard-target-z", 80f),
            new DailyV1GuardArrestCandidate("guard-target-a", 80f),
            new DailyV1GuardArrestCandidate("guard-target-b", 80f),
            new DailyV1GuardArrestCandidate("guard-target-higher", 100f)
        }, 12f);

        AssertMatchesPreviousRule(definitions, guard, action, new[]
        {
            new DailyV1GuardArrestCandidate("guard-target-z", 80f),
            new DailyV1GuardArrestCandidate("guard-target-a", 79.99999f)
        }, 12f);

        AssertMatchesPreviousRule(definitions, guard, action, new[]
        {
            new DailyV1GuardArrestCandidate("guard-target-first", -10f),
            new DailyV1GuardArrestCandidate("guard-target-second", -20f)
        }, 100f);

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            definitions, guard, action, new List<DailyV1GuardArrestCandidate>(), 12f,
            out int noTargetIndex, out float noTargetUtility), Is.True);
        Assert.That(noTargetIndex, Is.EqualTo(-1));
        Assert.That(noTargetUtility, Is.Zero,
            "The existing GuardSystem sets utility to zero when no arrest target is available.");
    }

    [Test]
    public void SelectedArrestPlanningRejectsForeignActorOrActionNodes()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition guard = definitions.Npcs["MARCUS"];
        DailyV1ActionDefinition action = FirstAction(definitions);
        List<DailyV1GuardArrestCandidate> candidates = new List<DailyV1GuardArrestCandidate>
        {
            new DailyV1GuardArrestCandidate("guard-target", 80f)
        };

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            definitions, foreign.Npcs[guard.DefinitionId], action, candidates, 12f,
            out int foreignActorIndex, out float foreignActorUtility), Is.False);
        Assert.That(foreignActorIndex, Is.EqualTo(-1));
        Assert.That(foreignActorUtility, Is.Zero);

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            definitions, guard, foreign.Actions[action.DefinitionId], candidates, 12f,
            out int foreignActionIndex, out float foreignActionUtility), Is.False);
        Assert.That(foreignActionIndex, Is.EqualTo(-1));
        Assert.That(foreignActionUtility, Is.Zero);
    }

    private static DailyV1ActionDefinition FirstAction(DailyV1EffectiveDefinitions definitions)
    {
        foreach (DailyV1ActionDefinition action in definitions.Actions.Values)
            return action;
        Assert.Fail("The selected fixture must contain an action node.");
        return null;
    }

    private static void AssertMatchesPreviousRule(
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition guard,
        DailyV1ActionDefinition action,
        IReadOnlyList<DailyV1GuardArrestCandidate> candidates,
        float currentUtility)
    {
        int expectedIndex = -1;
        float expectedBounty = 0f;
        string expectedRuntimeId = null;
        for (int index = 0; index < candidates.Count; index++)
        {
            DailyV1GuardArrestCandidate candidate = candidates[index];
            if (expectedIndex < 0
                || candidate.Bounty > expectedBounty
                || (UnityMathfCompatibility.Approximately(candidate.Bounty, expectedBounty)
                    && string.CompareOrdinal(candidate.RuntimeId, expectedRuntimeId) < 0))
            {
                expectedIndex = index;
                expectedBounty = candidate.Bounty;
                expectedRuntimeId = candidate.RuntimeId;
            }
        }

        float expectedUtility = expectedIndex < 0
            ? 0f
            : UnityMathfCompatibility.Max(currentUtility, 55f + expectedBounty * 0.25f);

        Assert.That(DailyV1GuardActionPlanning.TryPlanArrest(
            definitions, guard, action, candidates, currentUtility,
            out int selectedIndex, out float resultingUtility), Is.True);
        Assert.That(selectedIndex, Is.EqualTo(expectedIndex));
        Assert.That(resultingUtility, Is.EqualTo(expectedUtility));
        if (selectedIndex >= 0)
            Assert.That(candidates[selectedIndex].RuntimeId, Is.EqualTo(expectedRuntimeId));
    }
}
