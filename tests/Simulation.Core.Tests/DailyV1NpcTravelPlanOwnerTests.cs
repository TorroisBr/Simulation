using NUnit.Framework;

[TestFixture]
public sealed class DailyV1NpcTravelPlanOwnerTests
{
    [Test]
    public void ExactInstallRetainsStableTargetIdentityAndRevision()
    {
        DailyV1NpcTravelPlanOwner owner = new DailyV1NpcTravelPlanOwner();

        Assert.That(owner.TryInstallExact(
            "location/target", "city/target", NpcTravelReason.Trade,
            0.75f, 12.5f, "decision/travel", 7L), Is.True);

        Assert.That(owner.TargetLocationRuntimeId, Is.EqualTo("location/target"));
        Assert.That(owner.TargetCityRuntimeId, Is.EqualTo("city/target"));
        Assert.That(owner.Reason, Is.EqualTo(NpcTravelReason.Trade));
        Assert.That(owner.Utility, Is.EqualTo(0.75f));
        Assert.That(owner.ExpectedCost, Is.EqualTo(12.5f));
        Assert.That(owner.OriginDecisionId, Is.EqualTo("decision/travel"));
        Assert.That(owner.Revision, Is.EqualTo(7L));
    }

    [Test]
    public void CommitAdvancesOnlyChangedStateAndPreservesExactEmptyPlan()
    {
        DailyV1NpcTravelPlanOwner owner = new DailyV1NpcTravelPlanOwner();

        Assert.That(owner.TryCommit(null, null, NpcTravelReason.None, 0f, 0f, null), Is.True);
        Assert.That(owner.Revision, Is.Zero);
        Assert.That(owner.TryCommit(
            "location/target", "city/target", NpcTravelReason.CommercialScout,
            1f, 3f, "decision/scout"), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(1L));
        Assert.That(owner.TryCommit(
            "location/target", "city/target", NpcTravelReason.CommercialScout,
            1f, 3f, "decision/scout"), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(1L));
        Assert.That(owner.TryCommit(null, null, NpcTravelReason.None, 0f, 0f, null), Is.True);
        Assert.That(owner.Revision, Is.EqualTo(2L));
    }

    [Test]
    public void InvalidAndSaturatedCommitsLeaveStateUnchanged()
    {
        DailyV1NpcTravelPlanOwner owner = new DailyV1NpcTravelPlanOwner();
        Assert.That(owner.TryCommit(
            "location/target", "city/target", NpcTravelReason.Trade,
            0.5f, 2f, "decision/travel"), Is.True);

        Assert.That(owner.TryCommit(
            null, "city/target", NpcTravelReason.Trade,
            0.5f, 2f, "decision/invalid"), Is.False);
        Assert.That(owner.TargetLocationRuntimeId, Is.EqualTo("location/target"));
        Assert.That(owner.TargetCityRuntimeId, Is.EqualTo("city/target"));
        Assert.That(owner.Revision, Is.EqualTo(1L));

        Assert.That(owner.TryInstallExact(
            "location/target", "city/target", NpcTravelReason.Trade,
            0.5f, 2f, "decision/travel", long.MaxValue), Is.True);
        Assert.That(owner.CanCommit(
            "location/target", "city/target", NpcTravelReason.Trade,
            0.5f, 2f, "decision/travel"), Is.True);
        Assert.That(owner.TryCommit(
            "location/next", "city/next", NpcTravelReason.Flee,
            1f, 4f, "decision/next"), Is.False);
        Assert.That(owner.TargetLocationRuntimeId, Is.EqualTo("location/target"));
        Assert.That(owner.TargetCityRuntimeId, Is.EqualTo("city/target"));
        Assert.That(owner.Reason, Is.EqualTo(NpcTravelReason.Trade));
        Assert.That(owner.Revision, Is.EqualTo(long.MaxValue));
    }
}
