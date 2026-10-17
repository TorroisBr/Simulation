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
}
