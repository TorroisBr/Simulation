using System;
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
