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

    [Test]
    public void SelectedActionDecisionUsesEffectiveOverridesAndRejectsForeignGraphNodes()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreignDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npc = definitions.Npcs["AFONSO"];
        List<DailyV1ActionDefinition> actions = definitions.Actions.Values.ToList();
        actions.Add(foreignDefinitions.Actions["COMPRAR_MERCADORIA"]);

        Assert.That(DailyV1NpcActionDecision.TryCreateCandidates(
            npc, definitions, actions, definitions.Statuses.Values.ToList(),
            out List<DailyV1NpcActionCandidate> candidates), Is.True);

        DailyV1NpcActionCandidate work = candidates.Single(value => value.Action.DefinitionId == "COMPRAR_MERCADORIA");
        DailyV1NpcActionCandidate dailyDefault = candidates.Single(value => value.Action.DefinitionId == "VENDER_MERCADORIA");
        Assert.That(work.Action, Is.SameAs(definitions.Actions["COMPRAR_MERCADORIA"]));
        Assert.That(work.Utility, Is.EqualTo(42f),
            "The NPC default action override remains later than the job work-utility override.");
        Assert.That(dailyDefault.Utility, Is.EqualTo(78f));
        Assert.That(candidates.Count, Is.EqualTo(definitions.Actions.Count),
            "A same-ID action from another graph must not enter the selected decision set.");

        DailyV1ActionDefinition requiringStatus = definitions.Actions.Values.FirstOrDefault(action =>
            (action.Values.statusNecessariosParaFazerAcao ?? System.Array.Empty<string>()).Any(id => id != null));
        Assert.That(requiringStatus, Is.Not.Null);
        Assert.That(DailyV1NpcActionDecision.TryCreateCandidates(
            npc, definitions, new[] { requiringStatus }, System.Array.Empty<DailyV1StatusDefinition>(),
            out List<DailyV1NpcActionCandidate> withoutStatus), Is.True);
        Assert.That(withoutStatus, Is.Empty);
    }

    [Test]
    public void SelectedActionDecisionKeepsWeightedOrderingAndDrawBoundary()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        List<DailyV1NpcActionCandidate> candidates = definitions.Actions.Values
            .Select(action => new DailyV1NpcActionCandidate(action, 1f)).ToList();
        FixedRandomSource random = new FixedRandomSource(0f);

        DailyV1ActionDefinition selected = DailyV1NpcActionDecision.ChooseWeightedAction(
            candidates, random, "npc-decision|fixture|12");

        Assert.That(selected, Is.SameAs(definitions.Actions.Values
            .OrderBy(action => action.DefinitionId, System.StringComparer.Ordinal).First()));
        Assert.That(random.Calls, Is.EqualTo(1));
        Assert.That(random.LastStreamKey, Is.EqualTo("npc-decision|fixture|12"));

        foreach (DailyV1NpcActionCandidate candidate in candidates)
            candidate.Utility = 0f;
        random = new FixedRandomSource(0f);
        Assert.That(DailyV1NpcActionDecision.ChooseWeightedAction(candidates, random, "npc-decision|fixture|13"), Is.Null);
        Assert.That(random.Calls, Is.Zero, "Zero total weight must not consume the authoritative decision stream.");
    }

    [Test]
    public void SelectedActionSuccessUsesEffectiveDefinitionAndConsumesExactlyOneDraw()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ActionDefinition action = definitions.Actions.Values.FirstOrDefault(value => value.Values.canFail);
        Assert.That(action, Is.Not.Null, "The selected fixture must exercise the configured action-success path.");
        SequenceRandomSource random = new SequenceRandomSource(0.5f, 0.75f);
        const string actorRuntimeId = "npc-runtime-17";
        const string originDecisionId = "decision-42";
        const float multiplier = 1.25f;
        const long day = 42L;

        Assert.That(DailyV1NpcActionDecision.TryRollActionSuccess(
            definitions, action, actorRuntimeId, originDecisionId, multiplier, day, random,
            out bool succeeded), Is.True);
        float expectedChance = UnityMathfCompatibility.Clamp01(
            action.Values.baseSuccessChance * UnityMathfCompatibility.Max(0f, multiplier));
        Assert.That(succeeded, Is.EqualTo(0.5f <= expectedChance));
        Assert.That(random.Calls, Is.EqualTo(1));
        Assert.That(random.LastStreamKey,
            Is.EqualTo("action-success|npc-runtime-17|decision-42|" + action.DefinitionId + "|42"));
        Assert.That(random.NextUnit("following-operation"), Is.EqualTo(0.75f),
            "The selected success roll must consume one draw and leave the following draw unchanged.");
        Assert.That(random.Calls, Is.EqualTo(2));
    }

    [Test]
    public void SelectedActionSuccessRejectsForeignNodeAndDoesNotDrawForNonFailingAction()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreignDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ActionDefinition nonFailing = definitions.Actions.Values.FirstOrDefault(value => !value.Values.canFail);
        DailyV1ActionDefinition foreignAction = foreignDefinitions.Actions[definitions.Actions.Keys.First()];
        Assert.That(nonFailing, Is.Not.Null, "The selected fixture must contain a non-failing action.");
        SequenceRandomSource random = new SequenceRandomSource(0.25f);

        Assert.That(DailyV1NpcActionDecision.TryRollActionSuccess(
            definitions, foreignAction, "npc-runtime-17", "decision-42", 1f, 42L, random,
            out bool rejectedResult), Is.False);
        Assert.That(rejectedResult, Is.False);
        Assert.That(random.Calls, Is.Zero, "A foreign same-profile node must be rejected before RNG use.");

        Assert.That(DailyV1NpcActionDecision.TryRollActionSuccess(
            definitions, nonFailing, "npc-runtime-17", null, 1f, 42L, random,
            out bool nonFailingResult), Is.True);
        Assert.That(nonFailingResult, Is.True);
        Assert.That(random.Calls, Is.Zero, "A non-failing action must not consume the success stream.");
        Assert.That(random.NextUnit("following-operation"), Is.EqualTo(0.25f));
        Assert.That(random.Calls, Is.EqualTo(1));
    }

    private sealed class FixedRandomSource : IAuthoritativeRandomSource
    {
        private readonly float value;
        public int Calls { get; private set; }
        public string LastStreamKey { get; private set; }

        public FixedRandomSource(float value) { this.value = value; }

        public float NextUnit(string streamKey, long drawIndex = 0L)
        {
            Calls++;
            LastStreamKey = streamKey;
            return value;
        }

        public DeterministicRandomStream CreateStream(string streamKey) => null;
    }

    private sealed class SequenceRandomSource : IAuthoritativeRandomSource
    {
        private readonly float[] values;
        public int Calls { get; private set; }
        public string LastStreamKey { get; private set; }

        public SequenceRandomSource(params float[] values) { this.values = values; }

        public float NextUnit(string streamKey, long drawIndex = 0L)
        {
            LastStreamKey = streamKey;
            return values[Calls++];
        }

        public DeterministicRandomStream CreateStream(string streamKey) => null;
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
