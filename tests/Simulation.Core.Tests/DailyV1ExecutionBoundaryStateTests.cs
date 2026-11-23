using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1ExecutionBoundaryStateTests
{
    private sealed class MutableCensusProvider : IOwnerSectionCensusProvider
    {
        internal MutableCensusProvider(OwnerSectionCensusSnapshot snapshot)
        {
            Witness = new OwnerSectionCensusWitness(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.OwnerInstanceIdentity,
                snapshot.Cardinality, snapshot.Revision);
        }

        internal OwnerSectionCensusWitness Witness { get; set; }
        internal Action OnRead { get; set; }
        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            OwnerSectionCensusWitness observed = Witness;
            OnRead?.Invoke();
            return observed;
        }
    }

    private sealed class CensusRegistryFixture
    {
        internal DailyV1ExecutionBoundaryState State;
        internal readonly List<DailyV1OwnerSectionContractIdentity> Contracts =
            new List<DailyV1OwnerSectionContractIdentity>();
        internal readonly Dictionary<string, MutableCensusProvider> Providers =
            new Dictionary<string, MutableCensusProvider>(StringComparer.Ordinal);
        internal readonly Dictionary<string, DailyV1OwnerSectionCensusProviderRegistration> Registrations =
            new Dictionary<string, DailyV1OwnerSectionCensusProviderRegistration>(StringComparer.Ordinal);

        internal List<DailyV1OwnerSectionCensusProviderRegistration> OrderedRegistrations()
        {
            List<string> ids = new List<string>(Registrations.Keys);
            ids.Sort(StringComparer.Ordinal);
            List<DailyV1OwnerSectionCensusProviderRegistration> result =
                new List<DailyV1OwnerSectionCensusProviderRegistration>(ids.Count);
            foreach (string id in ids) result.Add(Registrations[id]);
            return result;
        }
    }

    private sealed class AdmissionEvidenceBoundary : IDailyV1RuntimeAdvanceBoundary
    {
        private readonly DailyV1ExecutionBoundaryState state;

        internal AdmissionEvidenceBoundary(
            DailyV1ExecutionBoundaryState state,
            IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            long mutationEpoch)
        {
            this.state = state;
            OwnerSections = ownerSections;
            MutationEpoch = mutationEpoch;
        }

        internal IReadOnlyList<OwnerSectionCensusSnapshot> OwnerSections { get; set; }
        internal long MutationEpoch { get; set; }
        internal bool AdmissionCaptureAllowed { get; set; } = true;
        internal bool? RequestedSessionReplacement { get; private set; }
        public DailyV1ExecutionBoundaryState ExecutionState => state;
        public bool HasIntradayTimeline => false;

        public bool TryEnterDailyAdvanceOperation(out IDisposable operationScope)
        { operationScope = null; return false; }
        public bool TryCreateSelectedDailyExecutionBoundary(
            out IDailyV1SelectedDailyExecutionBoundary executionBoundary)
        { executionBoundary = null; return false; }
        public bool TryAdvanceDayCore(out SimulationRuntimeAdvanceFailure failure)
        { failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted; return false; }
        public bool TryAdvanceIntradayToNextDayBoundary(out SimulationRuntimeAdvanceFailure failure)
        { failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted; return false; }
        public bool TryCaptureCompletedDailyCaptureEvidence(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure)
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }
        public bool TryCaptureDailyBoundaryAdmissionEvidence(
            bool sessionReplacement,
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure)
        {
            RequestedSessionReplacement = sessionReplacement;
            ownerSections = AdmissionCaptureAllowed ? OwnerSections : null;
            mutationEpoch = AdmissionCaptureAllowed ? MutationEpoch : 0L;
            failure = AdmissionCaptureAllowed
                ? DailyCaptureEligibilityFailure.None
                : DailyCaptureEligibilityFailure.OperationInProgress;
            return AdmissionCaptureAllowed;
        }

        public bool TryCaptureContinuationSaveEvidence(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure) =>
            TryCaptureDailyBoundaryAdmissionEvidence(
                sessionReplacement: false, out ownerSections, out mutationEpoch, out failure);
        public void FaultRuntimeAdmission() { }
    }

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
        OwnerSectionCensusSnapshot[] sections = CreateCompleteSnapshot();
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
    public void SelectedRuntimeBoundaryStateOwnsExactClockAdmissionWorldAndConfigurationIdentities()
    {
        SimulationTime clock = new SimulationTime(4L);
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        WorldId world = new WorldId(Guid.NewGuid());
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState(
            clock, admission, SimulationRuntimeCompositionProfile.Standard, world);
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow();
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));

        Assert.That(state.SimulationTime, Is.SameAs(clock));
        Assert.That(state.AdmissionContext, Is.SameAs(admission));
        Assert.That(state.WorldId, Is.SameAs(world));
        Assert.That(state.TryBindRuntimeConfiguration(configuration, calendar), Is.True);
        Assert.That(state.IsHealthyDailyOwnerThreadBoundary(), Is.True,
            "An idle admitted runtime is healthy before its first completed capture token exists.");
        Assert.That(state.ConfigurationIdentity, Is.SameAs(configuration));
        Assert.That(state.CalendarIdentity, Is.SameAs(calendar));
        Assert.That(state.TryBindRuntimeConfiguration(configuration, calendar), Is.True,
            "Rebinding is idempotent only for the exact same configuration and calendar instances.");
        Assert.That(state.TryBindRuntimeConfiguration(configuration,
            new SimulationCalendar(new CalendarDefinition(1, 1, 1))), Is.False);

        Assert.That(state.TryRecordCompletedCore(), Is.True);
        OwnerSectionCensusSnapshot[] sections = CreateCompleteSnapshot();
        Assert.That(state.TryCreateCompletedAdvanceToken(
            admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            WorldId.Parse(world.Value), 4L, 9L, sections, out _), Is.False,
            "A selected runtime must not publish a token for a host-substituted WorldId instance.");
        Assert.That(state.TryCreateCompletedAdvanceToken(
            admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            world, 3L, 9L, sections, out _), Is.False,
            "A selected runtime must not publish a token for a day other than its owned clock.");
        Assert.That(state.TryCreateCompletedAdvanceToken(
            admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            world, 4L, 9L, sections, out DailyCaptureEligibilityToken token), Is.True);
        Assert.That(token.WorldId, Is.SameAs(world));
        Assert.That(token.AbsoluteDay, Is.EqualTo(clock.AbsoluteDay));
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
        OwnerSectionCensusSnapshot[] sections = CreateCompleteSnapshot();

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
    public void CompletedAdvanceIssuanceRequiresACompletedBoundaryInTheHeldLease()
    {
        SimulationTime clock = new SimulationTime(5L);
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow();
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));
        WorldId world = new WorldId(Guid.NewGuid());
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState(
            clock, admission, SimulationRuntimeCompositionProfile.Standard, world);
        Assert.That(state.TryBindRuntimeConfiguration(configuration, calendar), Is.True);

        // A prior count does not authorize reissuing the previous boundary from
        // a later lease. The current advance must complete one new boundary.
        Assert.That(state.TryRecordCompletedCore(), Is.True);
        OwnerSectionCensusSnapshot[] sections = CreateCompleteSnapshot();
        Assert.That(state.TryIssueCompletedAdvanceCaptureToken(9L, sections, out _), Is.False);
        Assert.That(state.TryBeginAdvance(true, out DailyV1AdvanceLease lease), Is.True);
        Assert.That(state.TryIssueCompletedAdvanceCaptureToken(9L, sections, out _), Is.False);

        Assert.That(state.TryRecordCompletedCore(), Is.True);
        Assert.That(state.TryIssueCompletedAdvanceCaptureToken(
            9L, sections, out DailyCaptureEligibilityToken token), Is.True);
        Assert.That(token, Is.SameAs(state.CurrentCaptureToken));
        Assert.That(token.CompletedCoreSequence, Is.EqualTo(2L));
        Assert.That(token.AbsoluteDay, Is.EqualTo(5L));
        Assert.That(token.OwnerSections, Is.SameAs(sections));

        lease.Dispose();
        Assert.That(state.AdvanceLeaseHeld, Is.False);
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
        OwnerSectionCensusSnapshot[] sections = CreateCompleteSnapshot();

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

    [Test]
    public void CompletedBoundaryAdmissionRechecksExactCensusEpochAndReplacementScope()
    {
        SimulationTime clock = new SimulationTime(8L);
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow();
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));
        WorldId world = new WorldId(Guid.NewGuid());
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState(
            clock, admission, SimulationRuntimeCompositionProfile.Standard, world);
        Assert.That(state.TryBindRuntimeConfiguration(configuration, calendar), Is.True);
        Assert.That(state.TryRecordCompletedCore(), Is.True);
        OwnerSectionCensusSnapshot[] sections = CreateCompleteSnapshot();
        Assert.That(state.TryCreateCompletedAdvanceToken(
            admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            world, 8L, 9L, sections, out _), Is.True);

        AdmissionEvidenceBoundary boundary = new AdmissionEvidenceBoundary(state, sections, 9L);
        Assert.That(state.HasValidCompletedDailyBoundaryAdmission(boundary, sessionReplacement: false), Is.True);
        Assert.That(boundary.RequestedSessionReplacement, Is.EqualTo(false));
        Assert.That(state.HasValidCompletedDailyBoundaryAdmission(boundary, sessionReplacement: true), Is.True);
        Assert.That(boundary.RequestedSessionReplacement, Is.EqualTo(true),
            "Replacement admission must request the host's explicitly scoped census allowance.");

        boundary.MutationEpoch = 10L;
        Assert.That(state.HasValidCompletedDailyBoundaryAdmission(boundary, sessionReplacement: false), Is.False,
            "The current owner mutation epoch must still match the published capture token.");

        boundary.MutationEpoch = 9L;
        List<OwnerSectionCensusSnapshot> changedOwners = new List<OwnerSectionCensusSnapshot>(sections);
        OwnerSectionCensusSnapshot original = changedOwners[0];
        changedOwners[0] = new OwnerSectionCensusSnapshot(
            original.SectionId, original.SchemaVersion, original.Role, new object(),
            original.Cardinality, original.Revision);
        boundary.OwnerSections = changedOwners;
        Assert.That(state.HasValidCompletedDailyBoundaryAdmission(boundary, sessionReplacement: false), Is.False,
            "A different live owner instance must not reuse the completed capture token.");

        boundary.OwnerSections = sections;
        boundary.AdmissionCaptureAllowed = false;
        Assert.That(state.HasValidCompletedDailyBoundaryAdmission(boundary, sessionReplacement: false), Is.False,
            "A failed live admission census must reject the session boundary.");
    }

    [Test]
    public void RestoredBoundaryAdmissionRequiresRestoredProvenanceAndExactCensus()
    {
        SimulationTime clock = new SimulationTime(12L);
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow();
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));
        WorldId world = new WorldId(Guid.NewGuid());
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState(
            clock, admission, SimulationRuntimeCompositionProfile.Standard, world);
        Assert.That(state.TryBindRuntimeConfiguration(configuration, calendar), Is.True);
        OwnerSectionCensusSnapshot[] sections = CreateCompleteSnapshot();
        Assert.That(state.TryCreateRestoredContinuationToken(
            admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            world, 12L, 4L, 9L, sections, out _), Is.True);

        AdmissionEvidenceBoundary boundary = new AdmissionEvidenceBoundary(state, sections, 9L);
        Assert.That(state.HasValidRestoredDailyBoundaryAdmission(boundary), Is.True);
        Assert.That(boundary.RequestedSessionReplacement, Is.EqualTo(false));
        Assert.That(state.HasValidCompletedDailyBoundaryAdmission(boundary, sessionReplacement: false), Is.False,
            "A restored token must not be accepted as a completed advance, or vice versa.");

        boundary.MutationEpoch = 8L;
        Assert.That(state.HasValidRestoredDailyBoundaryAdmission(boundary), Is.False);
    }

    [Test]
    public void CompletedTokenRejectsIncompleteSelectedProfileCensus()
    {
        DailyV1ExecutionBoundaryState state = new DailyV1ExecutionBoundaryState();
        Assert.That(state.TryRecordCompletedCore(), Is.True);
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow();
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));
        Assert.That(state.TryCreateCompletedAdvanceToken(
            admission, configuration, calendar, SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()), 1L, 1L,
            new[] { new OwnerSectionCensusSnapshot("city.market", 1, OwnerSectionRole.Required, new object(), 1, 1L) },
            out DailyCaptureEligibilityToken token), Is.False);
        Assert.That(token, Is.Null);
        Assert.That(state.CurrentCaptureToken, Is.Null);
    }

    [Test]
    public void SelectedProfileCensusRejectsMissingDynamicOwnerAndBrokenAlias()
    {
        OwnerSectionCensusSnapshot[] complete = CreateCompleteSnapshot();
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(complete), Is.True);

        List<OwnerSectionCensusSnapshot> missingDynamic = new List<OwnerSectionCensusSnapshot>(complete);
        missingDynamic.RemoveAt(missingDynamic.FindIndex(value =>
            value.SectionId == DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes[0] + "npc-1"));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(missingDynamic), Is.False);

        List<OwnerSectionCensusSnapshot> brokenAlias = new List<OwnerSectionCensusSnapshot>(complete);
        int aliasIndex = brokenAlias.FindIndex(value => value.SectionId == "p12b.justice-records");
        OwnerSectionCensusSnapshot original = brokenAlias[aliasIndex];
        brokenAlias[aliasIndex] = new OwnerSectionCensusSnapshot(original.SectionId,
            original.SchemaVersion, original.Role, new object(), original.Cardinality, original.Revision);
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(brokenAlias), Is.False);
    }

    [Test]
    public void SelectedProfileCensusRejectsNonemptyFixedEmptyWitness()
    {
        List<OwnerSectionCensusSnapshot> sections = new List<OwnerSectionCensusSnapshot>(CreateCompleteSnapshot());
        int index = sections.FindIndex(value => value.SectionId == DailyV1SelectedOwnerSectionCensusContract.FixedEmpty[0]);
        OwnerSectionCensusSnapshot original = sections[index];
        sections[index] = new OwnerSectionCensusSnapshot(original.SectionId,
            original.SchemaVersion, original.Role, original.OwnerInstanceIdentity, 1, original.Revision);
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(sections), Is.False);
    }

    [Test]
    public void CoreOwnedSelectedCensusRegistryCapturesTheSealedFullOwnerVector()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();

        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            fixture.State, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
        Assert.That(captured.Count, Is.EqualTo(fixture.Registrations.Count));
        Assert.That(mutationEpoch, Is.Zero);

        string firstId = captured[0].SectionId;
        MutableCensusProvider provider = fixture.Providers[firstId];
        OwnerSectionCensusWitness original = provider.Witness;
        provider.Witness = new OwnerSectionCensusWitness(
            original.SectionId, original.SchemaVersion, original.OwnerInstanceIdentity,
            original.Cardinality, original.Revision + 1L);

        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            fixture.State, DailyV1CaptureScopeAllowance.None, out _, out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(DailyCaptureEligibilityFailure.OwnerCoverageIncomplete));
        Assert.That(fixture.State.IsSelectedRuntimeCensusProtocolFaulted, Is.True,
            "An unannounced provider change faults the Core census authority closed.");
    }

    [Test]
    public void CoreOwnedSelectedCensusRefreshRequiresExactReservationAndCompletesOneEpoch()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        string changedId = DailyV1SelectedOwnerSectionCensusContract.FixedRequired[0];
        MutableCensusProvider changedProvider = fixture.Providers[changedId];
        OwnerSectionCensusWitness prior = changedProvider.Witness;
        changedProvider.Witness = new OwnerSectionCensusWitness(
            prior.SectionId, prior.SchemaVersion, prior.OwnerInstanceIdentity,
            prior.Cardinality, prior.Revision + 1L);
        fixture.Registrations[changedId] = CreateRegistration(changedProvider);

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.True);
        Assert.That(state.MutationEpochState.TryReserve(
            out DailyV1MutationEpochReservation reservation), Is.True);
        DailyV1MutationEpochState otherEpoch = new DailyV1MutationEpochState();
        Assert.That(otherEpoch.TryReserve(out DailyV1MutationEpochReservation wrongReservation), Is.True);

        Assert.That(state.TryRefreshSelectedOwnerSectionCensusBaselines(
            fixture.OrderedRegistrations(), new[] { changedId }, wrongReservation), Is.False,
            "A reservation from another runtime cannot authorize the baseline replacement.");
        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None, out _, out _, out _), Is.False,
            "Capture is rejected while a mutation reservation is active.");

        Assert.That(state.TryRefreshSelectedOwnerSectionCensusBaselines(
            fixture.OrderedRegistrations(), new[] { changedId }, reservation), Is.True);
        Assert.That(state.MutationEpochState.TryComplete(reservation, committedMutation: true), Is.True);
        Assert.That(otherEpoch.TryRelease(wrongReservation), Is.True);
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);

        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(mutationEpoch, Is.EqualTo(1L));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
    }

    [Test]
    public void CoreActionSlotMutationRefreshesOnlyItsRequiredWitnessAndAdvancesOneEpoch()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        string sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix + "npc-1";
        Assert.That(fixture.Providers.TryGetValue(sectionId, out MutableCensusProvider provider), Is.True);
        OwnerSectionCensusWitness before = provider.Witness;
        provider.Witness = new OwnerSectionCensusWitness(
            before.SectionId,
            before.SchemaVersion,
            before.OwnerInstanceIdentity,
            before.Cardinality,
            before.Revision + 1L);

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
        bool compatibilitySynchronized = false;
        Assert.That(state.TryRefreshAndAdvanceSelectedNpcActionSlotMutation(
            new[] { sectionId }, changed =>
            {
                compatibilitySynchronized = changed.Count == 1
                    && string.Equals(changed.First(), sectionId, StringComparison.Ordinal);
                return compatibilitySynchronized;
            }), Is.True);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(1L));
        Assert.That(compatibilitySynchronized, Is.True);
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);

        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(mutationEpoch, Is.EqualTo(1L));
        OwnerSectionCensusSnapshot refreshed = captured.Single(row => row.SectionId == sectionId);
        Assert.That(refreshed.Revision, Is.EqualTo(before.Revision + 1L));
        Assert.That(refreshed.Cardinality, Is.EqualTo(before.Cardinality));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
    }

    [Test]
    public void CoreActionSlotMutationAdvancesCompleteCensusWithoutHostMirror()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        string sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix + "npc-1";
        MutableCensusProvider provider = fixture.Providers[sectionId];
        OwnerSectionCensusWitness before = provider.Witness;
        provider.Witness = new OwnerSectionCensusWitness(
            before.SectionId,
            before.SchemaVersion,
            before.OwnerInstanceIdentity,
            before.Cardinality,
            before.Revision + 1L);

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
        Assert.That(state.TryRefreshAndAdvanceSelectedNpcActionSlotMutation(
            new[] { sectionId }, synchronizeCompatibilityBaselines: null), Is.True,
            "The sealed Core registry publishes the owner row and epoch without a Unity mirror.");
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(1L));
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);

        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(mutationEpoch, Is.EqualTo(1L));
        OwnerSectionCensusSnapshot refreshed = captured.Single(row => row.SectionId == sectionId);
        Assert.That(refreshed.Revision, Is.EqualTo(before.Revision + 1L));
        Assert.That(refreshed.Cardinality, Is.EqualTo(before.Cardinality));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
    }

    [Test]
    public void CoreActionSlotMutationAllowsOneToZeroAndZeroToOneCardinalityTransitions()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        string sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix + "npc-1";
        MutableCensusProvider provider = fixture.Providers[sectionId];

        foreach (int nextCardinality in new[] { 0, 1 })
        {
            OwnerSectionCensusWitness prior = provider.Witness;
            provider.Witness = new OwnerSectionCensusWitness(
                prior.SectionId, prior.SchemaVersion, prior.OwnerInstanceIdentity,
                nextCardinality, prior.Revision + 1L);

            Assert.That(state.TryBeginRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
            Assert.That(state.TryRefreshAndAdvanceSelectedNpcActionSlotMutation(
                new[] { sectionId }, synchronizeCompatibilityBaselines: null), Is.True,
                "An admitted current-action slot is present zero or one times; each committed transition refreshes its exact owner row.");
            Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(nextCardinality + 1L));
            Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);

            Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
                state, DailyV1CaptureScopeAllowance.None,
                out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
                out long mutationEpoch,
                out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
            Assert.That(mutationEpoch, Is.EqualTo(nextCardinality + 1L));
            Assert.That(captured.Single(row => row.SectionId == sectionId).Cardinality,
                Is.EqualTo(nextCardinality));
            Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
        }
    }

    [Test]
    public void CoreActionSlotMutationRejectsCardinalityAboveOneWithoutPublishing()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        string sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix + "npc-1";
        MutableCensusProvider provider = fixture.Providers[sectionId];
        OwnerSectionCensusWitness prior = provider.Witness;
        provider.Witness = new OwnerSectionCensusWitness(
            prior.SectionId, prior.SchemaVersion, prior.OwnerInstanceIdentity,
            cardinality: 2, revision: prior.Revision + 1L);

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
        Assert.That(state.TryRefreshAndAdvanceSelectedNpcActionSlotMutation(
            new[] { sectionId }, synchronizeCompatibilityBaselines: null), Is.False);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.Zero);
        provider.Witness = prior;
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
        Assert.That(state.TryGetSelectedOwnerSectionCensusRegistry(
            DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registered,
            out _, out _, out _), Is.True);
        Assert.That(registered.Single(row => row.SectionId == sectionId).BaselineCardinality,
            Is.EqualTo(prior.Cardinality),
            "An invalid action cardinality cannot replace the previously committed census baseline.");
    }

    [Test]
    public void CoreCensusKeepsOtherDynamicFamilyCardinalityTransitionsRejected()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder =
            new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(state.TryBindSelectedRuntimeOwnerGraph(graph), Is.True);

        string sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcInventoryPrefix + "npc-1";
        MutableCensusProvider provider = fixture.Providers[sectionId];
        OwnerSectionCensusWitness prior = provider.Witness;
        provider.Witness = new OwnerSectionCensusWitness(
            prior.SectionId, prior.SchemaVersion, prior.OwnerInstanceIdentity,
            prior.Cardinality + 1, prior.Revision + 1L);

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);
        Assert.That(state.MutationEpochState.TryReserve(
            out DailyV1MutationEpochReservation reservation), Is.True);
        Assert.That(state.TryRefreshCoreSelectedOwnerSectionCensusRows(
            graph, new[] { sectionId }, reservation), Is.False,
            "The bounded zero/one exception applies only to an NPC current-action section.");
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.True,
            "A rejected unrelated cardinality change leaves the caller's reservation available.");
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.Zero);
        provider.Witness = prior;
        Assert.That(state.MutationEpochState.TryRelease(reservation), Is.True);
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
        Assert.That(state.TryGetSelectedOwnerSectionCensusRegistry(
            DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registered,
            out _, out _, out _), Is.True);
        Assert.That(registered.Single(row => row.SectionId == sectionId).BaselineCardinality,
            Is.EqualTo(prior.Cardinality));
    }

    [Test]
    public void CoreActionSlotMutationRejectsUnchangedRevisionAndDoesNotAdvanceEpoch()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        string sectionId = DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix + "npc-1";
        MutableCensusProvider provider = fixture.Providers[sectionId];
        OwnerSectionCensusWitness priorWitness = provider.Witness;
        long priorRevision = priorWitness.Revision;
        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.DailyAdvance), Is.True);

        Assert.That(state.TryRefreshAndAdvanceSelectedNpcActionSlotMutation(
            new[] { sectionId }, _ => true), Is.False,
            "A current-action owner must advance its local revision for a committed write.");
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.Zero);
        Assert.That(state.LastSelectedOwnerSectionCensusRefreshRejection,
            Is.EqualTo("ActionSlotCoreWitnessMismatch:section=" + sectionId
                + ";cardinality=" + priorWitness.Cardinality
                + ";revision=" + priorRevision
                + ";baseline=" + priorWitness.Cardinality + "/" + priorRevision),
            "The unchanged local revision is rejected before Core registry publication.");
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
        Assert.That(provider.Witness.Revision, Is.EqualTo(priorRevision));
    }

    [Test]
    public void CoreOwnedDynamicRosterReplacementSwapsFullCensusAndEpochTogether()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        List<OwnerSectionCensusSnapshot> expanded = CreateSnapshotWithSecondNpc();
        foreach (OwnerSectionCensusSnapshot snapshot in expanded)
        {
            if (fixture.Registrations.ContainsKey(snapshot.SectionId)) continue;
            MutableCensusProvider provider = new MutableCensusProvider(snapshot);
            fixture.Providers.Add(snapshot.SectionId, provider);
            fixture.Registrations.Add(snapshot.SectionId, CreateRegistration(provider));
        }

        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(expanded), Is.True);
        List<DailyV1OwnerSectionContractIdentity> contracts = new List<DailyV1OwnerSectionContractIdentity>();
        foreach (OwnerSectionCensusSnapshot snapshot in expanded)
            contracts.Add(new DailyV1OwnerSectionContractIdentity(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role));

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.True);
        Assert.That(state.MutationEpochState.TryReserve(
            out DailyV1MutationEpochReservation reservation), Is.True);
        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryDuringRuntimeOperation(
            contracts, fixture.OrderedRegistrations(), Array.Empty<string>(), reservation,
            committedMutation: true), Is.True);
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.False,
            "The Core transaction completes the exact reservation with the registry swap.");
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);

        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(mutationEpoch, Is.EqualTo(1L));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
        Assert.That(captured.Count, Is.EqualTo(expanded.Count));
        Assert.That(captured.Count, Is.EqualTo(61 + 22 * 2 + 4));

        List<OwnerSectionCensusSnapshot> reduced =
            new List<OwnerSectionCensusSnapshot>(CreateCompleteSnapshot());
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes)
        {
            string id = prefix + "npc-2";
            fixture.Registrations.Remove(id);
            fixture.Providers.Remove(id);
        }
        List<DailyV1OwnerSectionContractIdentity> reducedContracts =
            new List<DailyV1OwnerSectionContractIdentity>();
        foreach (OwnerSectionCensusSnapshot snapshot in reduced)
            reducedContracts.Add(new DailyV1OwnerSectionContractIdentity(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role));

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.True);
        Assert.That(state.MutationEpochState.TryReserve(
            out DailyV1MutationEpochReservation removalReservation), Is.True);
        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryDuringRuntimeOperation(
            reducedContracts, fixture.OrderedRegistrations(), Array.Empty<string>(), removalReservation,
            committedMutation: true), Is.True,
            "A roster removal removes the whole keyed family while retaining the complete selected contract.");
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);

        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out captured, out mutationEpoch, out failure), Is.True, failure.ToString());
        Assert.That(mutationEpoch, Is.EqualTo(2L));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
        Assert.That(captured.Count, Is.EqualTo(reduced.Count));
        Assert.That(captured.Count, Is.EqualTo(61 + 22 + 4));
    }

    [Test]
    public void CoreRejectsDynamicCensusAddWithoutCommittedMutationAndKeepsReservationOpen()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        List<OwnerSectionCensusSnapshot> expanded = AddSecondNpcToFixture(fixture);
        List<DailyV1OwnerSectionContractIdentity> contracts = CreateContracts(expanded);

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.True);
        Assert.That(state.MutationEpochState.TryReserve(
            out DailyV1MutationEpochReservation reservation), Is.True);
        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryDuringRuntimeOperation(
            contracts, fixture.OrderedRegistrations(), Array.Empty<string>(), reservation,
            committedMutation: false), Is.False,
            "Core must require one committed epoch when the complete dynamic vector adds owner keys.");
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.True,
            "A rejected replacement leaves the exact reservation to its caller for rollback/release.");
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.Zero);
        Assert.That(state.MutationEpochState.TryRelease(reservation), Is.True);
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);

        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
        Assert.That(captured.Count, Is.EqualTo(61 + 22 + 4),
            "A rejected addition leaves the original full census registry published.");
        Assert.That(mutationEpoch, Is.Zero);
    }

    [Test]
    public void CoreRejectsDynamicCensusRemovalWithoutCommittedMutationAndKeepsPriorVector()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        List<OwnerSectionCensusSnapshot> expanded = AddSecondNpcToFixture(fixture);
        List<DailyV1OwnerSectionContractIdentity> expandedContracts = CreateContracts(expanded);

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.True);
        Assert.That(state.MutationEpochState.TryReserve(
            out DailyV1MutationEpochReservation addReservation), Is.True);
        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryDuringRuntimeOperation(
            expandedContracts, fixture.OrderedRegistrations(), Array.Empty<string>(), addReservation,
            committedMutation: true), Is.True);
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(1L));

        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes)
        {
            string id = prefix + "npc-2";
            fixture.Registrations.Remove(id);
            fixture.Providers.Remove(id);
        }
        List<OwnerSectionCensusSnapshot> reduced =
            new List<OwnerSectionCensusSnapshot>(CreateCompleteSnapshot());

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.True);
        Assert.That(state.MutationEpochState.TryReserve(
            out DailyV1MutationEpochReservation removeReservation), Is.True);
        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryDuringRuntimeOperation(
            CreateContracts(reduced), fixture.OrderedRegistrations(), Array.Empty<string>(), removeReservation,
            committedMutation: false), Is.False,
            "Core must require one committed epoch when the complete dynamic vector removes owner keys.");
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.True);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.EqualTo(1L));
        Assert.That(state.MutationEpochState.TryRelease(removeReservation), Is.True);
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);

        Assert.That(DailyV1CoreOwnerSectionCensusCapture.TryCapture(
            state, DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
            out long mutationEpoch,
            out DailyCaptureEligibilityFailure failure), Is.True, failure.ToString());
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured), Is.True);
        Assert.That(captured.Count, Is.EqualTo(61 + 22 * 2 + 4),
            "A rejected removal leaves the previously committed two-owner vector published.");
        Assert.That(mutationEpoch, Is.EqualTo(1L));
    }

    [Test]
    public void CoreRejectsRetainedDynamicWitnessChangeWithoutIncreasingRevision()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        string changedId = DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes[0] + "npc-1";
        MutableCensusProvider provider = fixture.Providers[changedId];
        OwnerSectionCensusWitness prior = provider.Witness;
        provider.Witness = new OwnerSectionCensusWitness(
            prior.SectionId, prior.SchemaVersion, prior.OwnerInstanceIdentity,
            prior.Cardinality + 1, prior.Revision);
        fixture.Registrations[changedId] = CreateRegistration(provider);

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.True);
        Assert.That(state.MutationEpochState.TryReserve(
            out DailyV1MutationEpochReservation reservation), Is.True);
        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryDuringRuntimeOperation(
            fixture.Contracts, fixture.OrderedRegistrations(), Array.Empty<string>(), reservation,
            committedMutation: true), Is.False,
            "A committed roster transaction cannot change a retained dynamic owner without advancing its revision.");
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.True,
            "Rejecting the non-monotonic witness leaves the exact reservation available to the caller.");
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.Zero,
            "A rejected dynamic row change does not consume the shared epoch.");
        Assert.That(state.MutationEpochState.TryRelease(reservation), Is.True);
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);

        Assert.That(state.TryGetSelectedOwnerSectionCensusRegistry(
            DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registered,
            out _, out _, out _), Is.True);
        DailyV1OwnerSectionCensusProviderRegistration retained =
            registered.Single(section => section.SectionId == changedId);
        Assert.That(retained.BaselineCardinality, Is.EqualTo(prior.Cardinality));
        Assert.That(retained.BaselineRevision, Is.EqualTo(prior.Revision));
        Assert.That(retained.Provider, Is.SameAs(provider));
    }
    [Test]
    public void CoreRejectsRetainedDynamicWitnessChangeWithoutCommittedMutation()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture();
        DailyV1ExecutionBoundaryState state = fixture.State;
        string changedId = DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes[0] + "npc-1";
        MutableCensusProvider changedProvider = fixture.Providers[changedId];
        OwnerSectionCensusWitness prior = changedProvider.Witness;
        changedProvider.Witness = new OwnerSectionCensusWitness(
            prior.SectionId, prior.SchemaVersion, prior.OwnerInstanceIdentity,
            prior.Cardinality + 1, prior.Revision + 1L);
        fixture.Registrations[changedId] = CreateRegistration(changedProvider);

        Assert.That(state.TryBeginRuntimeProtocolOperation(
            DailyV1RuntimeProtocolOperationContracts.NpcMembership), Is.True);
        Assert.That(state.MutationEpochState.TryReserve(
            out DailyV1MutationEpochReservation reservation), Is.True);
        Assert.That(state.TryReplaceSelectedOwnerSectionCensusRegistryDuringRuntimeOperation(
            fixture.Contracts, fixture.OrderedRegistrations(), Array.Empty<string>(), reservation,
            committedMutation: false), Is.False,
            "Core must require one committed epoch for a retained dynamic row's cardinality/revision change.");
        Assert.That(state.MutationEpochState.HasActiveReservation, Is.True);
        Assert.That(state.MutationEpochState.CurrentEpoch, Is.Zero);
        Assert.That(state.MutationEpochState.TryRelease(reservation), Is.True);
        Assert.That(state.TryEndRuntimeProtocolOperation(), Is.True);

        Assert.That(state.TryGetSelectedOwnerSectionCensusRegistry(
            DailyV1CaptureScopeAllowance.None,
            out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> registered,
            out _, out _, out _), Is.True);
        DailyV1OwnerSectionCensusProviderRegistration retained =
            registered.Single(provider => provider.SectionId == changedId);
        Assert.That(retained.BaselineCardinality, Is.EqualTo(prior.Cardinality));
        Assert.That(retained.BaselineRevision, Is.EqualTo(prior.Revision),
            "A rejected dynamic replacement does not publish the staged witness baseline.");
    }

    [Test]
    public void CoreLiveProviderSealSamplesAndPublishesTheExactProfileInOrdinalOrder()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture(
            sealProviderRegistry: false, markReady: false);
        Assert.That(fixture.State.HasSealedSelectedOwnerSectionCensusRegistry, Is.False);

        Assert.That(fixture.State.TrySealSelectedOwnerSectionCensusRegistryFromLiveProviders(
            fixture.OrderedRegistrations(),
            out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> sealedProviders), Is.True);
        Assert.That(sealedProviders, Is.Not.Null);
        Assert.That(sealedProviders.Count, Is.EqualTo(fixture.Contracts.Count));
        Assert.That(sealedProviders.Select(provider => provider.SectionId),
            Is.EqualTo(sealedProviders.Select(provider => provider.SectionId).OrderBy(id => id, StringComparer.Ordinal)));
        Assert.That(fixture.State.HasSealedSelectedOwnerSectionCensusRegistry, Is.True);
        Assert.That(fixture.State.IsSelectedRuntimeCensusProtocolReadyForPublication, Is.True);

        foreach (DailyV1OwnerSectionCensusProviderRegistration registration in sealedProviders)
        {
            Assert.That(registration.HasBaseline, Is.True);
            Assert.That(fixture.Providers.TryGetValue(registration.SectionId, out MutableCensusProvider provider), Is.True);
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            Assert.That(registration.Provider, Is.SameAs(provider));
            Assert.That(registration.BaselineOwnerInstanceIdentity, Is.SameAs(witness.OwnerInstanceIdentity));
            Assert.That(registration.BaselineCardinality, Is.EqualTo(witness.Cardinality));
            Assert.That(registration.BaselineRevision, Is.EqualTo(witness.Revision));
        }

        DailyV1OwnerSectionCensusProviderRegistration[] exactEmpty = sealedProviders
            .Where(provider => provider.Role == OwnerSectionRole.ExplicitlyEmpty).ToArray();
        Assert.That(exactEmpty.Length, Is.GreaterThan(0));
        Assert.That(exactEmpty.All(provider => provider.BaselineCardinality == 0), Is.True);
    }

    [Test]
    public void CoreLiveProviderSealRejectsMissingRowsAndNonzeroExplicitEmptyAtomically()
    {
        CensusRegistryFixture missingFixture = CreateCensusRegistryFixture(
            sealProviderRegistry: false, markReady: false);
        List<DailyV1OwnerSectionCensusProviderRegistration> missing =
            missingFixture.OrderedRegistrations();
        missing.RemoveAt(0);
        Assert.That(missingFixture.State.TrySealSelectedOwnerSectionCensusRegistryFromLiveProviders(
            missing, out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> missingResult), Is.False);
        Assert.That(missingResult, Is.Null);
        Assert.That(missingFixture.State.HasSealedSelectedOwnerSectionCensusRegistry, Is.False);

        CensusRegistryFixture emptyFixture = CreateCensusRegistryFixture(
            sealProviderRegistry: false, markReady: false);
        string emptyId = DailyV1SelectedOwnerSectionCensusContract.FixedEmpty[0];
        MutableCensusProvider emptyProvider = emptyFixture.Providers[emptyId];
        OwnerSectionCensusWitness prior = emptyProvider.Witness;
        emptyProvider.Witness = new OwnerSectionCensusWitness(
            prior.SectionId, prior.SchemaVersion, prior.OwnerInstanceIdentity, 1, prior.Revision);
        Assert.That(emptyFixture.State.TrySealSelectedOwnerSectionCensusRegistryFromLiveProviders(
            emptyFixture.OrderedRegistrations(),
            out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> emptyResult), Is.False);
        Assert.That(emptyResult, Is.Null);
        Assert.That(emptyFixture.State.HasSealedSelectedOwnerSectionCensusRegistry, Is.False);
    }

    [Test]
    public void CoreLiveProviderSealRejectsWitnessDriftDuringInitialSampling()
    {
        CensusRegistryFixture fixture = CreateCensusRegistryFixture(
            sealProviderRegistry: false, markReady: false);
        string sectionId = DailyV1SelectedOwnerSectionCensusContract.FixedRequired[0];
        MutableCensusProvider provider = fixture.Providers[sectionId];
        OwnerSectionCensusWitness prior = provider.Witness;
        provider.OnRead = () =>
        {
            provider.OnRead = null;
            provider.Witness = new OwnerSectionCensusWitness(
                prior.SectionId, prior.SchemaVersion, prior.OwnerInstanceIdentity,
                prior.Cardinality, prior.Revision + 1L);
        };

        Assert.That(fixture.State.TrySealSelectedOwnerSectionCensusRegistryFromLiveProviders(
            fixture.OrderedRegistrations(),
            out IReadOnlyList<DailyV1OwnerSectionCensusProviderRegistration> result), Is.False);
        Assert.That(result, Is.Null);
        Assert.That(fixture.State.HasSealedSelectedOwnerSectionCensusRegistry, Is.False);
    }
    private static List<OwnerSectionCensusSnapshot> AddSecondNpcToFixture(
        CensusRegistryFixture fixture)
    {
        List<OwnerSectionCensusSnapshot> expanded = CreateSnapshotWithSecondNpc();
        foreach (OwnerSectionCensusSnapshot snapshot in expanded)
        {
            if (fixture.Registrations.ContainsKey(snapshot.SectionId)) continue;
            MutableCensusProvider provider = new MutableCensusProvider(snapshot);
            fixture.Providers.Add(snapshot.SectionId, provider);
            fixture.Registrations.Add(snapshot.SectionId, CreateRegistration(provider));
        }
        return expanded;
    }

    private static List<DailyV1OwnerSectionContractIdentity> CreateContracts(
        IEnumerable<OwnerSectionCensusSnapshot> snapshots)
    {
        List<DailyV1OwnerSectionContractIdentity> contracts =
            new List<DailyV1OwnerSectionContractIdentity>();
        foreach (OwnerSectionCensusSnapshot snapshot in snapshots)
            contracts.Add(new DailyV1OwnerSectionContractIdentity(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role));
        return contracts;
    }

    private static CensusRegistryFixture CreateCensusRegistryFixture(
        bool sealProviderRegistry = true, bool markReady = true)
    {
        CensusRegistryFixture fixture = new CensusRegistryFixture();
        SimulationRuntimeAdmissionContext admission =
            SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1();
        fixture.State = new DailyV1ExecutionBoundaryState(
            new SimulationTime(), admission, SimulationRuntimeCompositionProfile.Standard,
            new WorldId(Guid.NewGuid()));
        Assert.That(fixture.State.TryBindRuntimeConfiguration(
            SimulationConfigurationResolver.ResolveOrThrow(),
            new SimulationCalendar(new CalendarDefinition(1, 1, 1))), Is.True);

        foreach (OwnerSectionCensusSnapshot snapshot in CreateCompleteSnapshot())
        {
            MutableCensusProvider provider = new MutableCensusProvider(snapshot);
            fixture.Providers.Add(snapshot.SectionId, provider);
            fixture.Registrations.Add(snapshot.SectionId,
                CreateRegistration(provider, hasBaseline: sealProviderRegistry));
            fixture.Contracts.Add(new DailyV1OwnerSectionContractIdentity(
                snapshot.SectionId, snapshot.SchemaVersion, snapshot.Role));
        }

        Assert.That(fixture.State.TryReplaceSelectedOwnerSectionCensusRegistryBeforeSeal(
            fixture.Contracts, fixture.OrderedRegistrations()), Is.True);
        Assert.That(fixture.State.TrySealExpectedOwnerSectionContractInventory(), Is.True);
        if (sealProviderRegistry)
            Assert.That(fixture.State.TrySealSelectedOwnerSectionCensusRegistry(
                fixture.OrderedRegistrations()), Is.True);

        foreach (string operationContractId
            in DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                hasRuntimeAdmission: true, hasTravelPartySystem: false, hasTravelSystem: false))
            Assert.That(fixture.State.TryRegisterRuntimeProtocolOperationContract(operationContractId), Is.True);
        Assert.That(fixture.State.TrySealRuntimeProtocolOperationInventory(), Is.True);
        if (markReady)
        {
            Assert.That(sealProviderRegistry, Is.True);
            Assert.That(fixture.State.TryMarkSelectedRuntimeCensusProtocolReady(), Is.True);
        }
        return fixture;
    }

    private static DailyV1OwnerSectionCensusProviderRegistration CreateRegistration(
        MutableCensusProvider provider, bool hasBaseline = true)
    {
        if (!hasBaseline)
            return new DailyV1OwnerSectionCensusProviderRegistration(
                provider.Witness.SectionId,
                provider.Witness.SchemaVersion,
                RoleForSection(provider.Witness.SectionId),
                provider);

        return new DailyV1OwnerSectionCensusProviderRegistration(
            provider.Witness.SectionId,
            provider.Witness.SchemaVersion,
            RoleForSection(provider.Witness.SectionId),
            provider,
            provider.Witness.OwnerInstanceIdentity,
            provider.Witness.Cardinality,
            provider.Witness.Revision,
            hasBaseline: true);
    }
    private static OwnerSectionRole RoleForSection(string sectionId)
    {
        foreach (string id in DailyV1SelectedOwnerSectionCensusContract.FixedEmpty)
            if (string.Equals(id, sectionId, StringComparison.Ordinal)) return OwnerSectionRole.ExplicitlyEmpty;
        return OwnerSectionRole.Required;
    }

    private static List<OwnerSectionCensusSnapshot> CreateSnapshotWithSecondNpc()
    {
        List<OwnerSectionCensusSnapshot> result =
            new List<OwnerSectionCensusSnapshot>(CreateCompleteSnapshot());
        Dictionary<string, object> identities = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes)
        {
            string sectionId = prefix + "npc-2";
            string alias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(sectionId);
            if (alias == null) alias = "unique/" + sectionId;
            if (!identities.TryGetValue(alias, out object identity))
                identities.Add(alias, identity = new object());
            result.Add(new OwnerSectionCensusSnapshot(
                sectionId, 1, OwnerSectionRole.Required, identity, 1, 1L));
        }
        return result;
    }

    internal static OwnerSectionCensusSnapshot[] CreateCompleteSnapshot()
    {
        const string npc = "npc-1";
        const string city = "city-1";
        List<OwnerSectionCensusSnapshot> result = new List<OwnerSectionCensusSnapshot>();
        Dictionary<string, object> identities = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (string id in DailyV1SelectedOwnerSectionCensusContract.FixedRequired)
            Add(id, OwnerSectionRole.Required, 1);
        foreach (string id in DailyV1SelectedOwnerSectionCensusContract.FixedEmpty)
            Add(id, OwnerSectionRole.ExplicitlyEmpty, 0);
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.NpcPrefixes)
            Add(prefix + npc, OwnerSectionRole.Required, 1);
        foreach (string prefix in DailyV1SelectedOwnerSectionCensusContract.CityPrefixes)
            Add(prefix == "p12b.city.important-npcs/" ? prefix + city : prefix + city.Length + ":" + city,
                OwnerSectionRole.Required, 1);
        return result.ToArray();

        void Add(string id, OwnerSectionRole role, int cardinality)
        {
            string alias = DailyV1SelectedOwnerSectionCensusContract.ExpectedAliasGroupForSection(id);
            if (alias == null) alias = "unique/" + id;
            if (!identities.TryGetValue(alias, out object identity))
                identities.Add(alias, identity = new object());
            long revision = string.Equals(id,
                    DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId,
                    StringComparison.Ordinal)
                || string.Equals(id,
                    DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
                    StringComparison.Ordinal)
                ? 0L
                : 1L;
            result.Add(new OwnerSectionCensusSnapshot(id, 1, role, identity, cardinality, revision));
        }
    }
}
