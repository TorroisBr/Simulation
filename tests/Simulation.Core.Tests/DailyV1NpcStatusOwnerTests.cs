using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1NpcStatusOwnerTests
{
    [Test]
    public void SelectedStatusOwnerPreservesIdentityOrderMultiplicityAndNoOpMutationSemantics()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions otherDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1StatusDefinition first = definitions.Statuses.Values.First();
        DailyV1StatusDefinition second = definitions.Statuses.Values.Skip(1).First();

        Assert.That(DailyV1NpcStatusOwner.TryCreate(definitions,
            new[] { first, second, first }, out DailyV1NpcStatusOwner owner), Is.True);
        Assert.That(owner.Statuses, Is.EqualTo(new[] { first, second, first }),
            "Initial membership preserves the captured order and duplicate authored rows.");
        Assert.That(owner.TryCopyDefinitionIds(new System.Collections.Generic.List<string>()), Is.True);

        Assert.That(owner.TryAdd(first), Is.False, "An existing identity is a no-op.");
        Assert.That(owner.TryAdd(otherDefinitions.Statuses[first.DefinitionId]), Is.False,
            "A same-ID status from another effective-definition graph is not admitted.");
        Assert.That(owner.TryAdd(second), Is.False);
        Assert.That(owner.TryRemove(first), Is.True);
        Assert.That(owner.Statuses, Is.EqualTo(new[] { second, first }),
            "Removal removes one matching occurrence and retains the remaining order.");
        Assert.That(owner.TryRemove(otherDefinitions.Statuses[first.DefinitionId]), Is.False);
        Assert.That(owner.TryRemove(first), Is.True);
        Assert.That(owner.TryRemove(first), Is.False);
        Assert.That(owner.Statuses, Is.EqualTo(new[] { second }));
    }

    [Test]
    public void SelectedStatusOwnerRejectsForeignNodesDuringInitialInstallation()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions otherDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();

        Assert.That(DailyV1NpcStatusOwner.TryCreate(definitions,
            new[] { otherDefinitions.Statuses.Values.First() }, out DailyV1NpcStatusOwner owner), Is.False);
        Assert.That(owner, Is.Null);
    }

    [Test]
    public void SelectedStatusOwnerAppliesOrderedRemovalThenAdditionAndNotifiesEachWrite()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1StatusDefinition first = definitions.Statuses.Values.First();
        DailyV1StatusDefinition second = definitions.Statuses.Values.Skip(1).First();
        Assert.That(DailyV1NpcStatusOwner.TryCreate(definitions,
            new[] { first, second }, out DailyV1NpcStatusOwner owner), Is.True);
        List<string> events = new List<string>();

        Assert.That(owner.TryApplyOrderedChanges(
            new[] { first }, new[] { first, second },
            () => events.Add("admit"), () => events.Add("commit")), Is.True);

        Assert.That(owner.Statuses, Is.EqualTo(new[] { second, first }),
            "The existing writer removes first, then adds; duplicate add remains a no-op.");
        Assert.That(events, Is.EqualTo(new[] { "admit", "commit", "admit", "commit" }),
            "Admission and commit callbacks run once for each successful membership write.");
    }

    [Test]
    public void SelectedStatusOwnerDoesNotWriteWhenMutationAdmissionThrows()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1StatusDefinition first = definitions.Statuses.Values.First();
        Assert.That(DailyV1NpcStatusOwner.TryCreate(definitions,
            new[] { first }, out DailyV1NpcStatusOwner owner), Is.True);

        Assert.Throws<InvalidOperationException>(() => owner.TryApplyOrderedChanges(
            new[] { first }, Array.Empty<DailyV1StatusDefinition>(),
            () => throw new InvalidOperationException("admission rejected"), () => { }));

        Assert.That(owner.Statuses, Is.EqualTo(new[] { first }));
    }

    [Test]
    public void SelectedActionStatusPlanAppliesActorThenTargetUsingExactOrderedOwnerWrites()
    {
        DailyV1EffectiveDefinitions baseline = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ActionDefinition baselineAction = baseline.Actions.Values.First();
        DailyV1EffectiveDefinitions definitions = CreateDefinitionsWithActionStatusChanges(
            baseline, baselineAction,
            "status/Status-Livre", "status/Status-Preso",
            "status/Status-Escondido", "status/Status-Procurado");
        DailyV1ActionDefinition action = definitions.Actions[baselineAction.DefinitionId];
        DailyV1StatusDefinition actorInitial = definitions.Statuses["status/Status-Livre"];
        DailyV1StatusDefinition actorResult = definitions.Statuses["status/Status-Preso"];
        DailyV1StatusDefinition targetInitial = definitions.Statuses["status/Status-Escondido"];
        DailyV1StatusDefinition targetResult = definitions.Statuses["status/Status-Procurado"];
        DailyV1NpcRuntimeOwners actor = CreateNpcOwner(
            "actor", definitions, definitions.Npcs.Values.First(), actorInitial);
        DailyV1NpcRuntimeOwners target = CreateNpcOwner(
            "target", definitions, definitions.Npcs.Values.Skip(1).First(), targetInitial);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, Array.Empty<DailyV1CityRuntimeOwners>(), new[] { actor, target },
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.TryCreateActionStatusPlan(
            actor, action, true, target, out DailyV1SelectedActionStatusPlan plan), Is.True);

        List<string> events = new List<string>();
        DailyV1SelectedActionStatusPlanResult result = plan.Apply(
            (owner, remove, add) => ApplyAndRecord(owner, remove, add, events),
            (owner, remove, add) => ApplyAndRecord(owner, remove, add, events));

        Assert.That(result, Is.EqualTo(DailyV1SelectedActionStatusPlanResult.Applied));
        Assert.That(events, Is.EqualTo(new[]
        {
            "admit:actor", "commit:actor:",
            "admit:actor", "commit:actor:status/Status-Preso",
            "admit:target", "commit:target:",
            "admit:target", "commit:target:status/Status-Procurado"
        }), "Each owner keeps remove-before-add writes, and the actor completes before target mutation starts.");
        Assert.That(actor.Status.Statuses, Is.EqualTo(new[] { actorResult }));
        Assert.That(target.Status.Statuses, Is.EqualTo(new[] { targetResult }));
        Assert.That(actor.Status.Statuses[0], Is.SameAs(actorResult));
        Assert.That(target.Status.Statuses[0], Is.SameAs(targetResult));
    }

    [Test]
    public void SelectedActionStatusPlanPreservesActorFirstPartialFailureSemantics()
    {
        DailyV1EffectiveDefinitions baseline = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions definitions = CreateDefinitionsWithActionStatusChanges(
            baseline, baseline.Actions.Values.First(),
            "status/Status-Livre", "status/Status-Preso",
            "status/Status-Escondido", "status/Status-Procurado");
        DailyV1NpcRuntimeOwners actor = CreateNpcOwner(
            "actor", definitions, definitions.Npcs.Values.First(), definitions.Statuses["status/Status-Livre"]);
        DailyV1NpcRuntimeOwners target = CreateNpcOwner(
            "target", definitions, definitions.Npcs.Values.Skip(1).First(), definitions.Statuses["status/Status-Escondido"]);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, Array.Empty<DailyV1CityRuntimeOwners>(), new[] { actor, target },
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(graph.TryCreateActionStatusPlan(
            actor, definitions.Actions.Values.First(), true, target,
            out DailyV1SelectedActionStatusPlan plan), Is.True);

        bool targetCalled = false;
        Assert.That(plan.Apply(
            (_, __, ___) => false,
            (_, __, ___) => { targetCalled = true; return true; }),
            Is.EqualTo(DailyV1SelectedActionStatusPlanResult.ActorRejected));
        Assert.That(targetCalled, Is.False);
        Assert.That(actor.Status.Statuses, Is.EqualTo(new[] { definitions.Statuses["status/Status-Livre"] }));

        Assert.That(plan.Apply(
            (owner, remove, add) => owner.Status.TryApplyOrderedChanges(remove, add, () => { }, () => { }),
            (_, __, ___) => false), Is.EqualTo(DailyV1SelectedActionStatusPlanResult.TargetRejected));
        Assert.That(actor.Status.Statuses, Is.EqualTo(new[] { definitions.Statuses["status/Status-Preso"] }),
            "The existing actuation order keeps completed actor mutations when target application fails.");
        Assert.That(target.Status.Statuses, Is.EqualTo(new[] { definitions.Statuses["status/Status-Escondido"] }));
    }

    [Test]
    public void SelectedActionStatusPlanRejectsForeignActorAndActionIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcRuntimeOwners actor = CreateNpcOwner(
            "actor", definitions, definitions.Npcs.Values.First(), Array.Empty<DailyV1StatusDefinition>());
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, Array.Empty<DailyV1CityRuntimeOwners>(), new[] { actor },
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        Assert.That(graph.TryCreateActionStatusPlan(
            actor, foreign.Actions.Values.First(), false, null, out _), Is.False);
        DailyV1NpcRuntimeOwners foreignActor = CreateNpcOwner(
            "foreign-actor", foreign, foreign.Npcs.Values.First(), Array.Empty<DailyV1StatusDefinition>());
        Assert.That(graph.TryCreateActionStatusPlan(
            foreignActor, definitions.Actions.Values.First(), false, null, out _), Is.False);
    }

    private static bool ApplyAndRecord(
        DailyV1NpcRuntimeOwners owner,
        IReadOnlyList<DailyV1StatusDefinition> remove,
        IReadOnlyList<DailyV1StatusDefinition> add,
        List<string> events)
    {
        return owner != null && owner.Status.TryApplyOrderedChanges(
            remove,
            add,
            () => events.Add("admit:" + owner.RuntimeId),
            () => events.Add("commit:" + owner.RuntimeId + ":"
                + string.Join(",", owner.Status.Statuses.Select(status => status.DefinitionId))));
    }

    private static DailyV1NpcRuntimeOwners CreateNpcOwner(
        string runtimeId,
        DailyV1EffectiveDefinitions definitions,
        DailyV1NpcDefinition npc,
        params DailyV1StatusDefinition[] statuses)
    {
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateBound(
            runtimeId, definitions, npc, statuses, null, new MoneyAccountRuntime(),
            out DailyV1NpcRuntimeOwners owner), Is.True);
        return owner;
    }

    private static DailyV1EffectiveDefinitions CreateDefinitionsWithActionStatusChanges(
        DailyV1EffectiveDefinitions source,
        DailyV1ActionDefinition selectedAction,
        string actorRemove,
        string actorAdd,
        string targetRemove,
        string targetAdd)
    {
        List<DailyV1NpcActionData> actions = source.Actions.Values.Select(action =>
        {
            if (!ReferenceEquals(action, selectedAction)) return action.Values;
            DailyV1NpcActionData values = action.Values;
            return new DailyV1NpcActionData(
                values.id,
                values.statusNecessariosParaFazerAcao,
                values.actionName,
                values.normalActionLogText,
                values.actionCategory,
                values.actionType,
                values.baseUtility,
                values.statusModifiers,
                values.canFail,
                values.baseSuccessChance,
                new[] { actorAdd },
                new[] { actorRemove },
                new[] { targetAdd },
                new[] { targetRemove },
                values.crimeSettings);
        }).ToList();

        return DailyV1EffectiveDefinitions.CreateSelectedSchema(
            source.Configuration,
            source.EffectiveConfiguration,
            source.Calendar,
            source.Cities.Values.Select(city => city.Values),
            source.Items.Values.Select(item => item.Values),
            source.Npcs.Values.Select(npc => npc.Values),
            source.Jobs.Values.Select(job => job.Values),
            actions,
            source.Statuses.Values.Select(status => status.Values),
            source.StageOrder,
            source.ProfileContractIdentity,
            source.GeographyProfileContractIdentity,
            source.GeographyStageIdentity,
            source.HexCoordinateConvention,
            source.HexCoordinateCanonicalOrder,
            source.Traits.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.DisplayName)),
            source.CapabilityAttributes.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value.DisplayName)),
            source.ExplorableSites.Values.Select(site => site.Values));
    }

    [Test]
    public void SelectedNpcActionSlotPreservesExactAdmittedActionNodeIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions otherDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npc = definitions.Npcs.Values.First();
        DailyV1ActionDefinition action = definitions.Actions.Values.First();
        DailyV1ActionDefinition sameIdForeignAction = otherDefinitions.Actions[action.DefinitionId];

        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, npc, 0f, out DailyV1NpcRuntimeOwners owner), Is.True);
        Assert.That(owner.ActionSlot, Is.Not.Null);
        Assert.That(owner.ActionSlot.CurrentAction, Is.Null);

        Assert.That(owner.ActionSlot.TrySetCurrentAction(action), Is.True);
        Assert.That(owner.ActionSlot.CurrentAction, Is.SameAs(action));
        Assert.That(owner.ActionSlot.TrySetCurrentAction(sameIdForeignAction), Is.False,
            "Matching DefinitionId does not admit an action node from a different effective graph.");
        Assert.That(owner.ActionSlot.CurrentAction, Is.SameAs(action),
            "A rejected foreign node leaves the current canonical identity unchanged.");
        Assert.That(owner.ActionSlot.TrySetCurrentAction(null), Is.True);
        Assert.That(owner.ActionSlot.CurrentAction, Is.Null);
    }

    [Test]
    public void SelectedNpcRuntimeOwnerCompositionConsumesExactGraphAndPreservesOwnerInstances()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npc = definitions.Npcs.Values.First();

        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, npc, 12.5f, out DailyV1NpcRuntimeOwners initial), Is.True);
        Assert.That(initial.RuntimeId, Is.EqualTo("npc-runtime"));
        Assert.That(initial.Definitions, Is.SameAs(definitions));
        Assert.That(initial.Definition, Is.SameAs(npc));
        Assert.That(initial.Inventory.Definitions, Is.SameAs(definitions));
        Assert.That(initial.MoneyAccount.Balance, Is.EqualTo(12.5f));

        DailyV1InventoryOwner inventory = initial.Inventory;
        MoneyAccountRuntime money = initial.MoneyAccount;
        DailyV1StatusDefinition[] statuses = initial.Status.Statuses.ToArray();
        System.Collections.Generic.List<string> statusIds = new System.Collections.Generic.List<string>();
        Assert.That(initial.TryCopyStatusDefinitionIds(statusIds), Is.True);
        Assert.That(statusIds, Is.EqualTo(statuses.Select(status => status.DefinitionId)),
            "The Core owner exports captured status identities in authored order and multiplicity.");
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateBound(
            "npc-runtime", definitions, npc, statuses, inventory, money,
            out DailyV1NpcRuntimeOwners rebound), Is.True);
        Assert.That(rebound.Inventory, Is.SameAs(inventory));
        Assert.That(rebound.MoneyAccount, Is.SameAs(money));
        Assert.That(rebound.Status.Statuses, Is.EqualTo(statuses));
        Assert.That(rebound.Definition, Is.SameAs(npc));
        statusIds.Clear();
        Assert.That(rebound.TryCopyStatusDefinitionIds(statusIds), Is.True);
        Assert.That(statusIds, Is.EqualTo(statuses.Select(status => status.DefinitionId)));
    }

    [Test]
    public void SelectedNpcHiddenStateOwnerPreservesExactNonNegativeCounterValues()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npc = definitions.Npcs.Values.First();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-runtime", definitions, npc, 0f, out DailyV1NpcRuntimeOwners owners), Is.True);

        Assert.That(owners.HiddenState.RemainingDays, Is.Zero);
        Assert.That(owners.HiddenState.TrySetRemainingDays(4), Is.True);
        Assert.That(owners.HiddenState.RemainingDays, Is.EqualTo(4));
        Assert.That(owners.HiddenState.TryInstallExact(2), Is.True);
        Assert.That(owners.HiddenState.RemainingDays, Is.EqualTo(2));
        Assert.That(owners.HiddenState.TryInstallExact(-1), Is.False);
        Assert.That(owners.HiddenState.RemainingDays, Is.EqualTo(2),
            "A rejected invalid restoration value leaves the existing owner unchanged.");
    }

    [Test]
    public void SelectedNpcRuntimeOwnerCompositionRejectsForeignGraphOwnerBindings()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions otherDefinitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npc = definitions.Npcs.Values.First();
        DailyV1InventoryOwner foreignInventory = new DailyV1InventoryOwner(otherDefinitions);

        Assert.That(DailyV1NpcRuntimeOwners.TryCreateBound(
            "npc-runtime", definitions, npc, Array.Empty<DailyV1StatusDefinition>(), foreignInventory,
            new MoneyAccountRuntime(), out DailyV1NpcRuntimeOwners owners), Is.False);
        Assert.That(owners, Is.Null);
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateBound(
            "npc-runtime", definitions, otherDefinitions.Npcs[npc.DefinitionId], Array.Empty<DailyV1StatusDefinition>(),
            null, new MoneyAccountRuntime(), out owners), Is.False);
        Assert.That(owners, Is.Null);
    }
}
