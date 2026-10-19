using System;
using NUnit.Framework;

public sealed class DailyV1ExecutionBoundaryStateTests
{
    [Test]
    public void CoreAdvanceLeaseIsReentrantGuardAndCanBeReleasedExactlyOnce()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();

        Assert.That(state.AdvanceLeaseHeld, Is.False);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease first), Is.True);
        Assert.That(state.AdvanceLeaseHeld, Is.True);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease rejected), Is.False);
        Assert.That(rejected, Is.Null);

        first.Dispose();
        Assert.That(state.AdvanceLeaseHeld, Is.False);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease second), Is.True);
        first.Dispose();
        Assert.That(state.AdvanceLeaseHeld, Is.True,
            "Disposing a stale lease handle must not release the newer runtime window.");
        Assert.That(state.TryAcquireAdvanceLease(out rejected), Is.False);

        second.Dispose();
        Assert.That(state.AdvanceLeaseHeld, Is.False);
        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease third), Is.True);
        third.Dispose();
    }

    [Test]
    public void BeginAdvanceInvalidatesCaptureOnlyAfterLeaseAdmission()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow();
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));
        WorldId world = new WorldId(Guid.NewGuid());
        OwnerSectionCensusSnapshot[] sections =
        {
            new OwnerSectionCensusSnapshot("city.market", 1, OwnerSectionRole.Required, new object(), 1, 4L)
        };
        Assert.That(state.TryRecordCompletedCore(), Is.True);
        Assert.That(state.TryCreateCompletedAdvanceToken(
            admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            world, 1L, 9L, sections, out DailyCaptureEligibilityToken priorToken), Is.True);

        Assert.That(state.TryAcquireAdvanceLease(out DailyV1AdvanceLease competingLease), Is.True);
        Assert.That(state.TryBeginAdvance(true, out DailyV1AdvanceLease rejectedLease), Is.False);
        Assert.That(rejectedLease, Is.Null);
        Assert.That(state.CurrentCaptureToken, Is.SameAs(priorToken),
            "A rejected concurrent advance must not invalidate the last completed boundary.");
        competingLease.Dispose();

        Assert.That(state.TryBeginAdvance(true, out DailyV1AdvanceLease admittedLease), Is.True);
        Assert.That(state.CurrentCaptureToken, Is.Null,
            "An admitted advance invalidates the previous token before any mutation.");
        admittedLease.Dispose();
    }

    [Test]
    public void RuntimeIdentityIsStableAndUniquePerBoundaryState()
    {
        DailyV1ExecutionBoundaryState first = new DailyV1ExecutionBoundaryState();
        DailyV1ExecutionBoundaryState second = new DailyV1ExecutionBoundaryState();

        Assert.That(first.RuntimeInstanceIdentity, Is.SameAs(first.RuntimeInstanceIdentity));
        Assert.That(first.RuntimeInstanceIdentity, Is.Not.SameAs(second.RuntimeInstanceIdentity));
    }

    [Test]
    public void CompletedCoreSequencePreflightAndIncrementRejectOverflow()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();

        Assert.That(state.CompletedCoreSequence, Is.Zero);
        Assert.That(state.CanRecordCompletedCoreCount(0L), Is.False);
        Assert.That(state.CanRecordCompletedCoreCount(1L), Is.True);
        Assert.That(state.TryRecordCompletedCore(), Is.True);
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(1L));

        Assert.That(state.TryInstallRestoredCompletedCoreSequence(long.MaxValue), Is.False,
            "Restored sequence installation is permitted only before a completed boundary exists.");
    }

    [Test]
    public void RestoredSequenceCanOnlyBeInstalledAtIdleEmptyBoundary()
    {
        DailyV1ExecutionBoundaryState leased = new DailyV1ExecutionBoundaryState();
        Assert.That(leased.TryAcquireAdvanceLease(out DailyV1AdvanceLease lease), Is.True);
        Assert.That(leased.TryInstallRestoredCompletedCoreSequence(3L), Is.False);
        lease.Dispose();

        Assert.That(leased.TryInstallRestoredCompletedCoreSequence(3L), Is.True);
        Assert.That(leased.CompletedCoreSequence, Is.EqualTo(3L));
        Assert.That(leased.TryInstallRestoredCompletedCoreSequence(4L), Is.False);
        Assert.That(leased.CanRecordCompletedCoreCount(long.MaxValue), Is.False);
    }

    [Test]
    public void MaximumSequenceCanBeRestoredButCannotAdvanceFurther()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();

        Assert.That(state.TryInstallRestoredCompletedCoreSequence(long.MaxValue), Is.True);
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(long.MaxValue));
        Assert.That(state.CanRecordCompletedCoreCount(1L), Is.False);
        Assert.That(state.TryRecordCompletedCore(), Is.False);
    }

    [Test]
    public void OwnerSectionComparisonPreservesExactOwnerIdentityAndRevision()
    {
        object owner = new object();
        OwnerSectionCensusSnapshot original = new OwnerSectionCensusSnapshot(
            "city.market", 1, OwnerSectionRole.Required, owner, 1, 2L);
        OwnerSectionCensusSnapshot sameIdentity = new OwnerSectionCensusSnapshot(
            "city.market", 1, OwnerSectionRole.Required, owner, 1, 2L);
        OwnerSectionCensusSnapshot alias = new OwnerSectionCensusSnapshot(
            "city.market", 1, OwnerSectionRole.Required, new object(), 1, 2L);
        OwnerSectionCensusSnapshot changedRevision = new OwnerSectionCensusSnapshot(
            "city.market", 1, OwnerSectionRole.Required, owner, 1, 3L);

        Assert.That(DailyV1ExecutionBoundaryState.AreOwnerSectionsEqual(
            new[] { original }, new[] { sameIdentity }), Is.True);
        Assert.That(DailyV1ExecutionBoundaryState.AreOwnerSectionsEqual(
            new[] { original }, new[] { alias }), Is.False);
        Assert.That(DailyV1ExecutionBoundaryState.AreOwnerSectionsEqual(
            new[] { original }, new[] { changedRevision }), Is.False);
    }

    [Test]
    public void CompletedTokenRetainsRuntimeAdmissionDefinitionCalendarWorldAndOwnerIdentity()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow();
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));
        WorldId world = new WorldId(Guid.NewGuid());
        object owner = new object();
        OwnerSectionCensusSnapshot[] sections =
        {
            new OwnerSectionCensusSnapshot("city.market", 1, OwnerSectionRole.Required, owner, 1, 4L)
        };

        Assert.That(state.TryRecordCompletedCore(), Is.True);
        Assert.That(state.TryCreateCompletedAdvanceToken(
            admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            world, 1L, 9L, sections, out DailyCaptureEligibilityToken token), Is.True);
        Assert.That(state.CurrentCaptureToken, Is.SameAs(token));
        Assert.That(token.RuntimeInstanceIdentity, Is.SameAs(state.RuntimeInstanceIdentity));
        Assert.That(state.TryCompareCurrentCaptureToken(
            token, admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            world, 1L, sections, 9L), Is.True);

        OwnerSectionCensusSnapshot[] aliasedSections =
        {
            new OwnerSectionCensusSnapshot("city.market", 1, OwnerSectionRole.Required, new object(), 1, 4L)
        };
        Assert.That(state.TryCompareCurrentCaptureToken(
            token, admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            world, 1L, aliasedSections, 9L), Is.False);
        Assert.That(state.TryCompareCurrentCaptureToken(
            token, admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            world, 1L, sections, 10L), Is.False);

        state.InvalidateCurrentCaptureToken();
        Assert.That(state.CurrentCaptureToken, Is.Null);
    }

    [Test]
    public void RestoredTokenKeepsRestoredBoundaryProvenanceAndSequenceAtomicity()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow();
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));
        WorldId world = new WorldId(Guid.NewGuid());
        OwnerSectionCensusSnapshot[] sections =
        {
            new OwnerSectionCensusSnapshot("city.market", 1, OwnerSectionRole.Required, new object(), 1, 2L)
        };

        Assert.That(state.TryCreateRestoredContinuationToken(
            admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            world, 6L, 0L, 3L, sections, out DailyCaptureEligibilityToken rejected), Is.False);
        Assert.That(rejected, Is.Null);
        Assert.That(state.CompletedCoreSequence, Is.Zero);
        Assert.That(state.CurrentCaptureToken, Is.Null);

        Assert.That(state.TryCreateRestoredContinuationToken(
            admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            world, 6L, 11L, 3L, sections, out DailyCaptureEligibilityToken restored), Is.True);
        Assert.That(restored.BoundaryProvenance, Is.EqualTo(DailyCaptureBoundaryProvenance.RestoredContinuation));
        Assert.That(restored.CompletedCoreSequence, Is.EqualTo(11L));
        Assert.That(state.CompletedCoreSequence, Is.EqualTo(11L));
        Assert.That(state.CurrentCaptureToken, Is.SameAs(restored));
    }
}
