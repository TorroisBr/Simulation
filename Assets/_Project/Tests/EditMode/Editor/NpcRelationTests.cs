using NUnit.Framework;

public sealed class NpcRelationTests
{
    [Test]
    public void DirectedPairs_AreDistinct()
    {
        NpcRelationStore store = new NpcRelationStore();
        NpcRelationRuntime fromA = store.GetOrCreateRelation("npc-a", "npc-b");
        NpcRelationRuntime fromB = store.GetOrCreateRelation("npc-b", "npc-a");

        Assert.That(fromA, Is.Not.Null);
        Assert.That(fromB, Is.Not.Null);
        Assert.That(fromA, Is.Not.SameAs(fromB));
        Assert.That(store.Relations, Has.Count.EqualTo(2));
    }

    [Test]
    public void CreatingAtoB_DoesNotCreateBtoA()
    {
        NpcRelationStore store = new NpcRelationStore();

        Assert.That(store.GetOrCreateRelation("npc-a", "npc-b"), Is.Not.Null);
        Assert.That(store.TryGetRelation("npc-b", "npc-a", out _), Is.False);
    }

    [Test]
    public void SelfRelation_IsRejected()
    {
        NpcRelationStore store = new NpcRelationStore();

        Assert.That(store.GetOrCreateRelation("npc-a", "npc-a"), Is.Null);
        Assert.That(store.Relations, Has.Count.EqualTo(0));
    }

    [Test]
    public void DuplicateDirectedPair_DoesNotCreateAnotherRelation()
    {
        NpcRelationStore store = new NpcRelationStore();
        NpcRelationRuntime first = store.GetOrCreateRelation("npc-a", "npc-b");
        NpcRelationRuntime second = store.GetOrCreateRelation("npc-a", "npc-b");

        Assert.That(second, Is.SameAs(first));
        Assert.That(store.Relations, Has.Count.EqualTo(1));
    }

    [Test]
    public void NewRelation_StartsNeutral()
    {
        NpcRelationRuntime relation = new NpcRelationRuntime("npc-a", "npc-b");

        Assert.That(relation.Affinity, Is.EqualTo(0f));
        Assert.That(relation.Trust, Is.EqualTo(0f));
        Assert.That(relation.Fear, Is.EqualTo(0f));
    }

    [Test]
    public void Affinity_IsClampedAtPositiveLimit()
    {
        NpcRelationStore store = new NpcRelationStore();

        Assert.That(store.SetAffinity("npc-a", "npc-b", 150f), Is.True);
        Assert.That(store.TryGetRelation("npc-a", "npc-b", out NpcRelationRuntime relation), Is.True);
        Assert.That(relation.Affinity, Is.EqualTo(NpcRelationValueRanges.AffinityMax));
    }

    [Test]
    public void Affinity_IsClampedAtNegativeLimit()
    {
        NpcRelationStore store = new NpcRelationStore();

        Assert.That(store.SetAffinity("npc-a", "npc-b", -150f), Is.True);
        Assert.That(store.TryGetRelation("npc-a", "npc-b", out NpcRelationRuntime relation), Is.True);
        Assert.That(relation.Affinity, Is.EqualTo(NpcRelationValueRanges.AffinityMin));
    }

    [Test]
    public void Trust_IsClampedAtPositiveLimit()
    {
        NpcRelationStore store = new NpcRelationStore();

        Assert.That(store.SetTrust("npc-a", "npc-b", 150f), Is.True);
        Assert.That(store.TryGetRelation("npc-a", "npc-b", out NpcRelationRuntime relation), Is.True);
        Assert.That(relation.Trust, Is.EqualTo(NpcRelationValueRanges.TrustMax));
    }

    [Test]
    public void Trust_IsClampedAtNegativeLimit()
    {
        NpcRelationStore store = new NpcRelationStore();

        Assert.That(store.SetTrust("npc-a", "npc-b", -150f), Is.True);
        Assert.That(store.TryGetRelation("npc-a", "npc-b", out NpcRelationRuntime relation), Is.True);
        Assert.That(relation.Trust, Is.EqualTo(NpcRelationValueRanges.TrustMin));
    }

    [Test]
    public void Fear_IsClampedAtZero()
    {
        NpcRelationStore store = new NpcRelationStore();

        Assert.That(store.SetFear("npc-a", "npc-b", -10f), Is.True);
        Assert.That(store.TryGetRelation("npc-a", "npc-b", out NpcRelationRuntime relation), Is.True);
        Assert.That(relation.Fear, Is.EqualTo(NpcRelationValueRanges.FearMin));
    }

    [Test]
    public void Fear_IsClampedAtPositiveLimit()
    {
        NpcRelationStore store = new NpcRelationStore();

        Assert.That(store.SetFear("npc-a", "npc-b", 150f), Is.True);
        Assert.That(store.TryGetRelation("npc-a", "npc-b", out NpcRelationRuntime relation), Is.True);
        Assert.That(relation.Fear, Is.EqualTo(NpcRelationValueRanges.FearMax));
    }

    [Test]
    public void Adjust_IsDeterministicAndClamped()
    {
        NpcRelationStore store = new NpcRelationStore();

        Assert.That(store.AdjustAffinity("npc-a", "npc-b", 25f), Is.True);
        Assert.That(store.AdjustAffinity("npc-a", "npc-b", -10f), Is.True);
        Assert.That(store.AdjustTrust("npc-a", "npc-b", 35f), Is.True);
        Assert.That(store.AdjustFear("npc-a", "npc-b", 12f), Is.True);
        Assert.That(store.TryGetRelation("npc-a", "npc-b", out NpcRelationRuntime relation), Is.True);
        Assert.That(relation.Affinity, Is.EqualTo(15f));
        Assert.That(relation.Trust, Is.EqualTo(35f));
        Assert.That(relation.Fear, Is.EqualTo(12f));
    }

    [Test]
    public void Query_DoesNotCreateRelation()
    {
        NpcRelationStore store = new NpcRelationStore();

        Assert.That(store.TryGetRelation("npc-a", "npc-b", out _), Is.False);
        Assert.That(store.GetRelationsFrom("npc-a"), Is.Empty);
        Assert.That(store.GetRelationsToward("npc-b"), Is.Empty);
        Assert.That(store.Relations, Is.Empty);
    }

    [Test]
    public void GetRelationsFrom_ReturnsOnlyOriginatingRelations()
    {
        NpcRelationStore store = new NpcRelationStore();
        store.GetOrCreateRelation("npc-a", "npc-b");
        store.GetOrCreateRelation("npc-a", "npc-c");
        store.GetOrCreateRelation("npc-c", "npc-a");

        Assert.That(store.GetRelationsFrom("npc-a"), Has.Count.EqualTo(2));
        Assert.That(store.GetRelationsFrom("npc-a")[0].TargetNpcRuntimeId, Is.EqualTo("npc-b"));
        Assert.That(store.GetRelationsFrom("npc-a")[1].TargetNpcRuntimeId, Is.EqualTo("npc-c"));
    }

    [Test]
    public void GetRelationsToward_ReturnsOnlyReceivedRelations()
    {
        NpcRelationStore store = new NpcRelationStore();
        store.GetOrCreateRelation("npc-a", "npc-c");
        store.GetOrCreateRelation("npc-b", "npc-c");
        store.GetOrCreateRelation("npc-c", "npc-a");

        Assert.That(store.GetRelationsToward("npc-c"), Has.Count.EqualTo(2));
        Assert.That(store.GetRelationsToward("npc-c")[0].SourceNpcRuntimeId, Is.EqualTo("npc-a"));
        Assert.That(store.GetRelationsToward("npc-c")[1].SourceNpcRuntimeId, Is.EqualTo("npc-b"));
    }

    [Test]
    public void MutatingAtoB_DoesNotChangeBtoA()
    {
        NpcRelationStore store = new NpcRelationStore();
        store.GetOrCreateRelation("npc-a", "npc-b");
        store.GetOrCreateRelation("npc-b", "npc-a");

        store.SetAffinity("npc-a", "npc-b", 80f);
        store.SetTrust("npc-a", "npc-b", -20f);
        store.SetFear("npc-a", "npc-b", 40f);

        Assert.That(store.TryGetRelation("npc-b", "npc-a", out NpcRelationRuntime reverse), Is.True);
        Assert.That(reverse.Affinity, Is.EqualTo(0f));
        Assert.That(reverse.Trust, Is.EqualTo(0f));
        Assert.That(reverse.Fear, Is.EqualTo(0f));
    }

    [Test]
    public void EmptyOrNullIds_AreRejected()
    {
        NpcRelationStore store = new NpcRelationStore();

        Assert.That(store.GetOrCreateRelation(null, "npc-b"), Is.Null);
        Assert.That(store.GetOrCreateRelation("npc-a", null), Is.Null);
        Assert.That(store.GetOrCreateRelation("   ", "npc-b"), Is.Null);
        Assert.That(store.GetOrCreateRelation("npc-a", ""), Is.Null);
        Assert.That(store.Relations, Is.Empty);
        Assert.That(() => new NpcRelationRuntime(null, "npc-b"), Throws.ArgumentException);
        Assert.That(() => new NpcRelationRuntime("npc-a", " "), Throws.ArgumentException);
    }
}
