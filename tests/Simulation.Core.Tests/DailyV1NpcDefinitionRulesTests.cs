using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1NpcDefinitionRulesTests
{
    [Test]
    public void SelectedMerchantPolicyUsesEffectiveJobAndCanonicalItemValues()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npc = definitions.Npcs["AFONSO"];
        DailyV1ActionDefinition workAction = definitions.Actions["COMPRAR_MERCADORIA"];
        DailyV1ItemDefinition preferredItem = definitions.Items["FERRO"];

        Assert.That(DailyV1NpcDefinitionRules.HasJobType(npc, definitions, 1), Is.True);
        Assert.That(DailyV1NpcDefinitionRules.HasMerchantBehavior(npc, definitions, 0), Is.True);
        Assert.That(DailyV1NpcDefinitionRules.MinimumProfitPerItem(npc, definitions), Is.EqualTo(1f));
        Assert.That(DailyV1NpcDefinitionRules.GetJob(npc, definitions),
            Is.SameAs(definitions.Jobs["job/Job-MercadorFerro"].Values));

        Assert.That(DailyV1NpcDefinitionRules.TryGetWorkActionUtility(
            npc, definitions, workAction, out float workUtility), Is.True);
        Assert.That(workUtility, Is.EqualTo(80f));
        Assert.That(DailyV1NpcDefinitionRules.TryGetDefaultActionUtility(
            npc, definitions, definitions.Actions["VENDER_MERCADORIA"], out float defaultUtility), Is.True);
        Assert.That(defaultUtility, Is.EqualTo(78f));

        List<string> preferredIds = new List<string>();
        DailyV1NpcDefinitionRules.AppendPreferredTradeItemDefinitionIds(npc, definitions, preferredIds);
        CollectionAssert.AreEqual(new[] { "FERRO" }, preferredIds);
        Assert.That(DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(
            npc, definitions, preferredItem, out float multiplier), Is.True);
        Assert.That(multiplier, Is.EqualTo(2f));
        Assert.That(DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(
            npc, definitions, definitions.Items["TRIGO"], out _), Is.False);

        DailyV1NpcDefinition guard = definitions.Npcs["MARCUS"];
        Assert.That(DailyV1NpcDefinitionRules.HasJobType(guard, definitions, 1), Is.False);
        Assert.That(DailyV1NpcDefinitionRules.HasJobType(guard, definitions, 2), Is.True);
        Assert.That(DailyV1NpcDefinitionRules.HasMerchantBehavior(guard, definitions, 1), Is.False);
    }

    [Test]
    public void SelectedMerchantPolicyRejectsSameIdNodesFromAnotherDefinitionGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions otherDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npc = definitions.Npcs["AFONSO"];

        Assert.That(DailyV1NpcDefinitionRules.GetJob(
            otherDefinitions.Npcs["AFONSO"], definitions), Is.Null);
        Assert.That(DailyV1NpcDefinitionRules.HasJobType(
            otherDefinitions.Npcs["AFONSO"], definitions, 1), Is.False);
        Assert.That(DailyV1NpcDefinitionRules.TryGetWorkActionUtility(
            npc, definitions, otherDefinitions.Actions["COMPRAR_MERCADORIA"], out _), Is.False);
        Assert.That(DailyV1NpcDefinitionRules.TryGetDefaultActionUtility(
            npc, definitions, otherDefinitions.Actions["VENDER_MERCADORIA"], out _), Is.False);
        Assert.That(DailyV1NpcDefinitionRules.TryGetPreferredTradeItemMultiplier(
            npc, definitions, otherDefinitions.Items["FERRO"], out _), Is.False);
    }

    [Test]
    public void SelectedActionPolicyUsesCanonicalNodesAndResolvesStatusChangesInOrder()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions otherDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        List<DailyV1StatusDefinition> allStatuses = definitions.Statuses.Values.ToList();

        foreach (DailyV1ActionDefinition action in definitions.Actions.Values)
        {
            Assert.That(DailyV1NpcDefinitionRules.GetActionName(definitions, action),
                Is.EqualTo(action.Values.actionName ?? string.Empty));
            Assert.That(DailyV1NpcDefinitionRules.GetNormalActionLogText(definitions, action),
                Is.EqualTo(action.Values.normalActionLogText ?? string.Empty));
            Assert.That(DailyV1NpcDefinitionRules.GetActionCanFail(definitions, action), Is.EqualTo(action.Values.canFail));
            Assert.That(DailyV1NpcDefinitionRules.GetActionBaseSuccessChance(definitions, action), Is.EqualTo(action.Values.baseSuccessChance));
            Assert.That(DailyV1NpcDefinitionRules.GetActionTypeOrder(definitions, action),
                Is.EqualTo(action.Values.actionType?.Value ?? int.MinValue));
            Assert.That(DailyV1NpcDefinitionRules.GetActionCategoryOrder(definitions, action),
                Is.EqualTo(action.Values.actionCategory?.Value ?? int.MinValue));
            Assert.That(DailyV1NpcDefinitionRules.GetActionBaseUtility(definitions, action), Is.EqualTo(action.Values.baseUtility));
            Assert.That(DailyV1NpcDefinitionRules.GetCrimeActionSettings(definitions, action),
                Is.SameAs(action.Values.crimeSettings));
            Assert.That(DailyV1NpcDefinitionRules.HasRequiredActionStatuses(definitions, action, allStatuses), Is.True);

            Assert.That(DailyV1NpcDefinitionRules.TryGetActionStatusChanges(definitions, action,
                out IReadOnlyList<DailyV1StatusDefinition> remove,
                out IReadOnlyList<DailyV1StatusDefinition> add,
                out IReadOnlyList<DailyV1StatusDefinition> targetRemove,
                out IReadOnlyList<DailyV1StatusDefinition> targetAdd), Is.True);
            AssertStatusIds(action.Values.statusToRemove, remove);
            AssertStatusIds(action.Values.statusToAdd, add);
            AssertStatusIds(action.Values.targetStatusToRemove, targetRemove);
            AssertStatusIds(action.Values.targetStatusToAdd, targetAdd);

            float expectedUtility = 3f;
            foreach (DailyV1StatusWeightModifier modifier in action.Values.statusModifiers ?? System.Array.Empty<DailyV1StatusWeightModifier>())
                if (modifier != null && !string.IsNullOrWhiteSpace(modifier.status)
                    && definitions.Statuses.ContainsKey(modifier.status))
                    expectedUtility *= modifier.multiplier;
            Assert.That(DailyV1NpcDefinitionRules.ApplyStatusModifiers(
                definitions, action, allStatuses, 3f), Is.EqualTo(expectedUtility));
        }

        DailyV1ActionDefinition foreignAction = otherDefinitions.Actions["COMPRAR_MERCADORIA"];
        Assert.That(DailyV1NpcDefinitionRules.GetActionName(definitions, foreignAction), Is.Empty);
        Assert.That(DailyV1NpcDefinitionRules.GetActionBaseSuccessChance(definitions, foreignAction), Is.Zero);
        Assert.That(DailyV1NpcDefinitionRules.HasRequiredActionStatuses(definitions, foreignAction, allStatuses), Is.False);
        Assert.That(DailyV1NpcDefinitionRules.TryGetActionStatusChanges(definitions, foreignAction,
            out _, out _, out _, out _), Is.False);
        Assert.That(DailyV1NpcDefinitionRules.ApplyStatusModifiers(definitions, foreignAction, allStatuses, 3f), Is.Zero);
    }

    private static void AssertStatusIds(
        IReadOnlyList<string> expectedIds,
        IReadOnlyList<DailyV1StatusDefinition> actual)
    {
        string[] expected = (expectedIds ?? System.Array.Empty<string>())
            .Where(value => value != null).ToArray();
        Assert.That(actual.Select(value => value.DefinitionId), Is.EqualTo(expected));
    }
}
