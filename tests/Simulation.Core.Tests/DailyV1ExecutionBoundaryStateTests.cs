using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class DailyV1ExecutionBoundaryStateTests
{
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
            result.Add(new OwnerSectionCensusSnapshot(id, 1, role, identity, cardinality, 1L));
        }
    }
}
