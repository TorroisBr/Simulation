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
            definitions, definitions.Items["FERRO"], "city-origin", "city-target",
            1, 1, 1f, 0, 0, null, long.MaxValue,
            out DailyV1NpcMerchantTradePlanOwner owner), Is.True);

        Assert.That(owner.CanRegisterSale(1), Is.False,
            "The trade stage must reject an uncommittable plan transition before owner writes.");

        Assert.That(owner.TryInstallState(definitions.Items["TRIGO"], null,
            "city-target", 2, 2, 1f, 0, 0, null), Is.False);
        Assert.That(owner.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(owner.ItemDefinition, Is.SameAs(definitions.Items["FERRO"]));
    }

    [Test]
    public void RegisterSaleUsesCoreOwnerTransitionAndPreservesCompletionSemantics()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1ItemDefinition item = definitions.Items["FERRO"];
        Assert.That(DailyV1NpcMerchantTradePlanOwner.TryCreateFromOwnerSnapshot(
            definitions, item, "origin", "target", 5, 3, 2.5f,
            2, 4, "decision-9", 8, out DailyV1NpcMerchantTradePlanOwner owner), Is.True);
        int admissions = 0;
        int notifications = 0;

        bool committed = owner.TryRegisterSale(1, () => { admissions++; return true; },
            () => notifications++, out bool completed);

        Assert.That(committed, Is.True);
        Assert.That(completed, Is.False);
        Assert.That(owner.RemainingAmount, Is.EqualTo(2));
        Assert.That(owner.PlannedAmount, Is.EqualTo(5));
        Assert.That(owner.WaitDaysAtDestination, Is.Zero);
        Assert.That(owner.PendingTravelDays, Is.EqualTo(4));
        Assert.That(owner.OriginDecisionId, Is.EqualTo("decision-9"));
        Assert.That(owner.Revision, Is.EqualTo(9));
        Assert.That(admissions, Is.EqualTo(1));
        Assert.That(notifications, Is.EqualTo(1));

        committed = owner.TryRegisterSale(9, () => { admissions++; return true; },
            () => notifications++, out completed);

        Assert.That(committed, Is.True);
        Assert.That(completed, Is.True);
        Assert.That(owner.IsActive, Is.False);
        Assert.That(owner.HasData, Is.False);
        Assert.That(owner.Revision, Is.EqualTo(10));
        Assert.That(admissions, Is.EqualTo(2));
        Assert.That(notifications, Is.EqualTo(2));
    }

    [Test]
    public void RejectedSaleRegistrationLeavesPlanAndRevisionUntouched()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1NpcMerchantTradePlanOwner.TryCreateFromOwnerSnapshot(
            definitions, definitions.Items["FERRO"], "origin", "target", 5, 3, 2.5f,
            2, 4, "decision-9", 8, out DailyV1NpcMerchantTradePlanOwner owner), Is.True);

        Assert.That(owner.TryRegisterSale(1, () => false, () => Assert.Fail(), out _), Is.False);

        Assert.That(owner.RemainingAmount, Is.EqualTo(3));
        Assert.That(owner.WaitDaysAtDestination, Is.EqualTo(2));
        Assert.That(owner.Revision, Is.EqualTo(8));
    }
}
