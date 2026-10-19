using NUnit.Framework;

public sealed class DailyV1NpcMerchantTradePlanOwnerTests
{
    [Test]
    public void InitialOwnerUsesAdmittedTypedItemAndAdvancesLocalRevisionOnlyOnChange()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        Assert.That(DailyV1NpcMerchantTradePlanOwner.TryCreateInitial(
            definitions, out DailyV1NpcMerchantTradePlanOwner owner), Is.True);

        Assert.That(owner.Revision, Is.Zero);
        Assert.That(owner.TryInstallState(item, "city-origin", "city-target",
            4, 4, 2.5f, 0, 0, "decision-1"), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(1));
        Assert.That(owner.ItemDefinition, Is.SameAs(item));
        Assert.That(owner.TryInstallState(item, "city-origin", "city-target",
            4, 4, 2.5f, 0, 0, "decision-1"), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(1), "An exact no-op must not advance the owner revision.");
    }

    [Test]
    public void SnapshotHydrationPreservesExactRevisionAndRejectsForeignDefinitionIdentity()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1EffectiveDefinitions foreign = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ItemDefinition item = definitions.Items["FERRO"];

        Assert.That(DailyV1NpcMerchantTradePlanOwner.TryCreateFromOwnerSnapshot(
            definitions, item, "city-origin", "city-target", 5, 2, 3.25f,
            1, 4, "decision-7", 19,
            out DailyV1NpcMerchantTradePlanOwner owner), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(19));
        Assert.That(owner.RemainingAmount, Is.EqualTo(2));
        Assert.That(owner.ItemDefinition, Is.SameAs(item));

        Assert.That(owner.TryInstallState(foreign.Items["FERRO"], "city-origin",
            "city-target", 1, 1, 1f, 0, 0, null), Is.False);
        Assert.That(owner.Revision, Is.EqualTo(19));
        Assert.That(owner.ItemDefinition, Is.SameAs(item));
    }

    [Test]
    public void RevisionOverflowRejectsChangedInstallWithoutChangingState()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1NpcMerchantTradePlanOwner.TryCreateFromOwnerSnapshot(
            definitions, definitions.Items["FERRO"], null, "city-target",
            1, 1, 1f, 0, 0, null, long.MaxValue,
            out DailyV1NpcMerchantTradePlanOwner owner), Is.True);

        Assert.That(owner.TryInstallState(definitions.Items["TRIGO"], null,
            "city-target", 2, 2, 1f, 0, 0, null), Is.False);
        Assert.That(owner.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(owner.ItemDefinition, Is.SameAs(definitions.Items["FERRO"]));
    }
}
