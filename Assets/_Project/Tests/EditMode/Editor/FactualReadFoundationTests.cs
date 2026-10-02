using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

public sealed class FactualReadFoundationTests
{
    [Test]
    public void ResultVocabularyDistinguishesPresenceAbsenceAndScope()
    {
        FactReadResult<string> present = FactReadResult<string>.Present("known");
        FactReadResult<string> absent = FactReadResult<string>.Absent();
        FactReadResult<string> unavailable = FactReadResult<string>.Unavailable();
        FactReadResult<string> unsupported = FactReadResult<string>.Unsupported();
        FactReadResult<string> outside = FactReadResult<string>.OutsideFactualScope(
            FactReadOutsideScopeReason.ActorKnowledge,
            "PoliticalKnowledgeStore");

        Assert.That(present.Status, Is.EqualTo(FactReadStatus.Present));
        Assert.That(present.Value, Is.EqualTo("known"));
        Assert.That(absent.Status, Is.EqualTo(FactReadStatus.Absent));
        Assert.That(absent.TryGetValue(out _), Is.False);
        Assert.That(unavailable.Status, Is.EqualTo(FactReadStatus.Unavailable));
        Assert.That(unsupported.Status, Is.EqualTo(FactReadStatus.Unsupported));
        Assert.That(outside.Status, Is.EqualTo(FactReadStatus.OutsideFactualScope));
        Assert.That(outside.OutsideScopeReason, Is.EqualTo(FactReadOutsideScopeReason.ActorKnowledge));
        Assert.That(outside.SourceAuthority, Is.EqualTo("PoliticalKnowledgeStore"));

        Assert.Throws<ArgumentNullException>(() => FactReadResult<string>.Present(null));
        Assert.Throws<InvalidOperationException>(
            () => FactReadResult<IReadOnlyList<string>>.Absent());
        Assert.Throws<ArgumentException>(() => FactReadResult<string>.OutsideFactualScope(
            FactReadOutsideScopeReason.DerivedDisplay,
            " "));

        List<string> mutableValues = new List<string> { "first" };
        FactReadResult<IReadOnlyList<string>> copiedValues = FactReadResultFactory.PresentList(mutableValues);
        mutableValues.Add("later");
        Assert.That(copiedValues.Status, Is.EqualTo(FactReadStatus.Present));
        Assert.That(copiedValues.Value, Is.EqualTo(new[] { "first" }));
        IList<string> copiedView = copiedValues.Value as IList<string>;
        Assert.That(copiedView, Is.Not.Null);
        Assert.That(copiedView.IsReadOnly, Is.True);

        FactReadResult<IReadOnlyList<string>> knownEmpty = FactReadResultFactory.PresentList(Array.Empty<string>());
        Assert.That(knownEmpty.Status, Is.EqualTo(FactReadStatus.Present));
        Assert.That(knownEmpty.Value, Is.Empty);
    }

    [Test]
    public void CoordinatorReturnsSortedDeduplicatedCapabilitiesAndExplicitUnsupported()
    {
        ReadHarness harness = CreateBoundHarness();
        FactualReadCoordinator coordinator = new FactualReadCoordinator(
            harness.Admission,
            new IFactualReader[]
            {
                new DelegateReader<string>("zeta", 3, () => FactReadResult<string>.Present("last")),
                new DelegateReader<string>("alpha", 2, () => FactReadResult<string>.Absent())
            });
        string[] request = { "zeta", "alpha", "zeta", "missing" };

        Assert.That(coordinator.TryCaptureCoherent(out FactualReadCapture capture, request), Is.True);
        request[0] = "mutated-after-capture";

        Assert.That(capture.IsCoherent, Is.True);
        Assert.That(capture.Mode, Is.EqualTo(FactualReadCaptureMode.Coherent));
        Assert.That(capture.LogicalBoundary, Is.EqualTo(0L));
        Assert.That(capture.FactionStoreRevision, Is.EqualTo(0L));
        Assert.That(capture.PersonStoreRevision, Is.EqualTo(0L));
        Assert.That(capture.RequestedCapabilityIds, Is.EqualTo(new[] { "alpha", "missing", "zeta" }));
        Assert.That(capture.SourceVersions, Has.Count.EqualTo(2));
        Assert.That(capture.SourceVersions[0].CapabilityId, Is.EqualTo("alpha"));
        Assert.That(capture.SourceVersions[0].Version, Is.EqualTo(2));
        Assert.That(capture.SourceVersions[1].CapabilityId, Is.EqualTo("zeta"));
        Assert.That(capture.TryGet<string>("alpha", out FactReadResult<string> absent), Is.True);
        Assert.That(absent.Status, Is.EqualTo(FactReadStatus.Absent));
        Assert.That(capture.TryGet<string>("zeta", out FactReadResult<string> present), Is.True);
        Assert.That(present.Value, Is.EqualTo("last"));
        Assert.That(capture.TryGet<int>("zeta", out _), Is.False);
        Assert.That(capture.TryGet<int>("missing", out FactReadResult<int> notSupported), Is.True);
        Assert.That(notSupported.Status, Is.EqualTo(FactReadStatus.Unsupported));

        IList<string> requestedView = capture.RequestedCapabilityIds as IList<string>;
        Assert.That(requestedView, Is.Not.Null);
        Assert.That(requestedView.IsReadOnly, Is.True);
    }

    [Test]
    public void ReentrantCaptureFailsUnavailableWhileOuterCaptureRemainsCoherent()
    {
        ReadHarness harness = CreateBoundHarness();
        FactualReadCoordinator coordinator = null;
        bool nestedSucceeded = true;
        FactualReadCapture nestedCapture = null;
        coordinator = new FactualReadCoordinator(
            harness.Admission,
            new IFactualReader[]
            {
                new DelegateReader<int>("value", 1, () =>
                {
                    nestedSucceeded = coordinator.TryCaptureCoherent(out nestedCapture, "value");
                    return FactReadResult<int>.Present(11);
                })
            });

        Assert.That(coordinator.TryCaptureCoherent(out FactualReadCapture outerCapture, "value"), Is.True);
        Assert.That(outerCapture.IsCoherent, Is.True);
        Assert.That(outerCapture.TryGet<int>("value", out FactReadResult<int> outerValue), Is.True);
        Assert.That(outerValue.Value, Is.EqualTo(11));
        Assert.That(nestedSucceeded, Is.False);
        AssertUnavailable<int>(nestedCapture, "value");
    }

    [Test]
    public void CoordinatorDiscardsEveryResultWhenReaderThrowsOrBoundaryChanges()
    {
        ReadHarness throwingHarness = CreateBoundHarness();
        FactualReadCoordinator throwingCoordinator = new FactualReadCoordinator(
            throwingHarness.Admission,
            new IFactualReader[]
            {
                new DelegateReader<int>("first", 1, () => FactReadResult<int>.Present(7)),
                new DelegateReader<string>("throws", 1, () => throw new InvalidOperationException("reader failure"))
            });

        Assert.That(throwingCoordinator.TryCaptureCoherent(
            out FactualReadCapture thrownCapture,
            "first",
            "throws"), Is.False);
        AssertUnavailable<int>(thrownCapture, "first");
        AssertUnavailable<string>(thrownCapture, "throws");
        Assert.That(thrownCapture.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(thrownCapture.Diagnostics[0].CapabilityId, Is.EqualTo("throws"));
        Assert.That(thrownCapture.Diagnostics[0].Code, Is.EqualTo("reader-exception"));
        Assert.That(thrownCapture.Diagnostics[0].Message, Does.Not.Contain("reader failure"));

        ReadHarness changedHarness = CreateBoundHarness();
        FactualReadCoordinator changedCoordinator = new FactualReadCoordinator(
            changedHarness.Admission,
            new IFactualReader[]
            {
                new DelegateReader<int>("first", 1, () => FactReadResult<int>.Present(7)),
                new DelegateReader<int>("changes-boundary", 1, () =>
                {
                    changedHarness.Runtime.LogicalBoundary++;
                    return FactReadResult<int>.Present(9);
                })
            });

        Assert.That(changedCoordinator.TryCaptureCoherent(
            out FactualReadCapture changedCapture,
            "first",
            "changes-boundary"), Is.False);
        AssertUnavailable<int>(changedCapture, "first");
        AssertUnavailable<int>(changedCapture, "changes-boundary");
        Assert.That(changedCapture.LogicalBoundary, Is.Null);
    }

    [Test]
    public void DiagnosticsAreCopiedSortedAndReaderUnavailableGetsStableDiagnostic()
    {
        FactualReadDiagnostic[] diagnostics =
        {
            new FactualReadDiagnostic("zeta", "z.code", "z message"),
            new FactualReadDiagnostic("alpha", "b.code", "b message"),
            new FactualReadDiagnostic("alpha", "a.code", "a message")
        };
        FactualReadCapture diagnosticCapture = new FactualReadCapture(
            false,
            null,
            null,
            null,
            new[]
            {
                new FactualReadResultEntry("alpha", typeof(int), FactReadResult<int>.Unavailable()),
                new FactualReadResultEntry("zeta", typeof(int), FactReadResult<int>.Unavailable())
            },
            null,
            diagnostics);
        diagnostics[0] = null;

        Assert.That(diagnosticCapture.Diagnostics, Has.Count.EqualTo(3));
        Assert.That(diagnosticCapture.Diagnostics[0].CapabilityId, Is.EqualTo("alpha"));
        Assert.That(diagnosticCapture.Diagnostics[0].Code, Is.EqualTo("a.code"));
        Assert.That(diagnosticCapture.Diagnostics[1].Code, Is.EqualTo("b.code"));
        Assert.That(diagnosticCapture.Diagnostics[2].CapabilityId, Is.EqualTo("zeta"));
        IList<FactualReadDiagnostic> diagnosticView = diagnosticCapture.Diagnostics as IList<FactualReadDiagnostic>;
        Assert.That(diagnosticView, Is.Not.Null);
        Assert.That(diagnosticView.IsReadOnly, Is.True);

        ReadHarness harness = CreateBoundHarness();
        FactualReadCoordinator coordinator = new FactualReadCoordinator(
            harness.Admission,
            new IFactualReader[]
            {
                new DelegateReader<int>("unavailable", 1, () => FactReadResult<int>.Unavailable())
            });
        Assert.That(coordinator.TryCaptureCoherent(out FactualReadCapture unavailableCapture, "unavailable"), Is.False);
        Assert.That(unavailableCapture.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(unavailableCapture.Diagnostics[0].CapabilityId, Is.EqualTo("unavailable"));
        Assert.That(unavailableCapture.Diagnostics[0].Code, Is.EqualTo("reader-unavailable"));
    }

    [Test]
    public void CoherentCaptureRequiresPublishedHealthyIdleSelectedOwnerThread()
    {
        AssertCaptureUnavailable(state => state.IsWorldPublished = false);
        AssertCaptureUnavailable(state => state.IsHealthy = false);
        AssertCaptureUnavailable(state => state.IsBootstrapOrAdvanceActive = true);

        ReadHarness unselected = CreateUnselectedHarness();
        Assert.That(unselected.Admission.TryBindStores(unselected.FactionStore, unselected.PersonStore), Is.False);
        FactualReadCoordinator unselectedCoordinator = new FactualReadCoordinator(
            unselected.Admission,
            new IFactualReader[]
            {
                new DelegateReader<int>("value", 1, () => FactReadResult<int>.Present(1))
            });
        Assert.That(unselectedCoordinator.TryCaptureCoherent(out FactualReadCapture unselectedCapture, "value"), Is.False);
        AssertUnavailable<int>(unselectedCapture, "value");

        ReadHarness offThread = CreateBoundHarness();
        FactualReadCoordinator offThreadCoordinator = new FactualReadCoordinator(
            offThread.Admission,
            new IFactualReader[]
            {
                new DelegateReader<int>("value", 1, () => FactReadResult<int>.Present(1))
            });
        FactualReadCapture captured = null;
        bool succeeded = true;
        Thread captureThread = new Thread(() =>
        {
            succeeded = offThreadCoordinator.TryCaptureCoherent(out captured, "value");
        });
        captureThread.Start();
        Assert.That(captureThread.Join(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(succeeded, Is.False);
        AssertUnavailable<int>(captured, "value");
    }

    [Test]
    public void EveryBoundFactionAndPersonMutationEntryRejectsDuringCoherentRead()
    {
        ReadHarness harness = CreateUnboundHarness();
        PersonRuntime activeMember = RegisterPerson(harness.PersonStore, "frb.active");
        PersonRuntime addMember = RegisterPerson(harness.PersonStore, "frb.add");
        PersonRuntime directAffiliationMember = RegisterPerson(harness.PersonStore, "frb.direct-affiliation");
        PersonRuntime rollbackRegistration = RegisterPerson(harness.PersonStore, "frb.rollback-registration");
        PersonRuntime bindMember = RegisterPerson(harness.PersonStore, "frb.bind");
        PersonRuntime rollbackBinding = RegisterPerson(harness.PersonStore, "frb.rollback-binding");
        Assert.That(harness.PersonStore.TryBindMaterializedNpc(
            rollbackBinding.PersonId,
            "npc.frb.rollback-binding",
            out PersonStoreFailure initialBindingFailure), Is.True, initialBindingFailure.ToString());

        FactionId factionId = new FactionId("frb.faction");
        Assert.That(harness.FactionStore.TryRegister(new FactionRecord(factionId, "FR-B", 0L), out _), Is.True);
        Assert.That(harness.FactionStore.TryRegisterAffiliation(
            new FactionAffiliationRecord(factionId, activeMember.PersonId, 0L), out _), Is.True);
        Assert.That(FactionAffiliationSystem.TryProposeAdd(
            harness.FactionStore,
            factionId,
            addMember.PersonId,
            0L,
            out FactionAffiliationAddTransition addTransition,
            out FactionFoundationFailure addFailure), Is.True, addFailure.ToString());
        Assert.That(FactionAffiliationSystem.TryProposeEnd(
            harness.FactionStore,
            factionId,
            activeMember.PersonId,
            0L,
            out FactionAffiliationEndTransition endTransition,
            out FactionFoundationFailure endFailure), Is.True, endFailure.ToString());
        Assert.That(harness.Admission.TryBindStores(harness.FactionStore, harness.PersonStore), Is.True);

        long factionRevision = harness.FactionStore.Revision;
        long personRevision = harness.PersonStore.Revision;
        List<bool> writesAccepted = new List<bool>();
        FactionFoundationFailure factionFailure = FactionFoundationFailure.None;
        PersonStoreFailure personFailure = PersonStoreFailure.None;

        FactualReadCoordinator coordinator = new FactualReadCoordinator(
            harness.Admission,
            new IFactualReader[]
            {
                new DelegateReader<int>("attempt-writes", 1, () =>
                {
                    writesAccepted.Add(harness.FactionStore.TryRegister(
                        new FactionRecord(new FactionId("frb.extra"), "Extra", 0L),
                        out factionFailure));
                    writesAccepted.Add(harness.FactionStore.TryRegisterAffiliation(
                        new FactionAffiliationRecord(factionId, directAffiliationMember.PersonId, 0L),
                        out factionFailure));
                    writesAccepted.Add(FactionAffiliationSystem.TryApplyAdd(
                        harness.FactionStore,
                        addTransition,
                        out factionFailure));
                    writesAccepted.Add(FactionAffiliationSystem.TryApplyEnd(
                        harness.FactionStore,
                        endTransition,
                        out factionFailure));
                    writesAccepted.Add(harness.PersonStore.TryRegister(
                        new PersonRuntime(new PersonId("frb.extra-person")),
                        out personFailure));
                    writesAccepted.Add(harness.PersonStore.TryRollbackRegistration(rollbackRegistration));
                    writesAccepted.Add(harness.PersonStore.TryBindMaterializedNpc(
                        bindMember.PersonId,
                        "npc.frb.bind",
                        out personFailure));
                    writesAccepted.Add(harness.PersonStore.TryRollbackMaterializedNpcBinding(
                        rollbackBinding.PersonId,
                        "npc.frb.rollback-binding"));
                    return FactReadResult<int>.Present(1);
                })
            });

        Assert.That(coordinator.TryCaptureCoherent(out FactualReadCapture capture, "attempt-writes"), Is.True);
        Assert.That(capture.IsCoherent, Is.True);
        Assert.That(writesAccepted, Has.Count.EqualTo(8));
        Assert.That(writesAccepted, Has.All.False);
        Assert.That(factionFailure.Code, Is.EqualTo(FactionFoundationFailureCode.RuntimeFaulted));
        Assert.That(personFailure, Is.EqualTo(PersonStoreFailure.RuntimeFaulted));
        Assert.That(harness.FactionStore.Revision, Is.EqualTo(factionRevision));
        Assert.That(harness.PersonStore.Revision, Is.EqualTo(personRevision));

        Assert.That(harness.FactionStore.TryRegister(
            new FactionRecord(new FactionId("frb.after-read"), "After Read", 0L), out _), Is.True);
        Assert.That(harness.PersonStore.TryRegister(
            new PersonRuntime(new PersonId("frb.after-read")), out _), Is.True);
    }

    [Test]
    public void BoundStoresRejectOffThreadWritesBeforeChangingRevision()
    {
        ReadHarness harness = CreateBoundHarness();
        long factionRevision = harness.FactionStore.Revision;
        long personRevision = harness.PersonStore.Revision;
        bool factionAccepted = true;
        bool personAccepted = true;
        FactionFoundationFailure factionFailure = FactionFoundationFailure.None;
        PersonStoreFailure personFailure = PersonStoreFailure.None;

        Thread writer = new Thread(() =>
        {
            factionAccepted = harness.FactionStore.TryRegister(
                new FactionRecord(new FactionId("frb.off-thread"), "Off Thread", 0L),
                out factionFailure);
            personAccepted = harness.PersonStore.TryRegister(
                new PersonRuntime(new PersonId("frb.off-thread")),
                out personFailure);
        });
        writer.Start();
        Assert.That(writer.Join(TimeSpan.FromSeconds(5)), Is.True);

        Assert.That(factionAccepted, Is.False);
        Assert.That(factionFailure.Code, Is.EqualTo(FactionFoundationFailureCode.RuntimeFaulted));
        Assert.That(personAccepted, Is.False);
        Assert.That(personFailure, Is.EqualTo(PersonStoreFailure.RuntimeFaulted));
        Assert.That(harness.FactionStore.Revision, Is.EqualTo(factionRevision));
        Assert.That(harness.PersonStore.Revision, Is.EqualTo(personRevision));
    }

    [Test]
    public void ExistingAuthoritativeMutationFaultStillBlocksBoundStoreWrites()
    {
        ReadHarness harness = CreateBoundHarness();
        AuthoritativeMutationGuard guard = new AuthoritativeMutationGuard();
        Assert.That(harness.FactionStore.TryBindMutationGuard(guard), Is.True);
        guard.MarkFaulted(AuthoritativeMutationFaultReason.RollbackRestoreFailed);
        long factionRevision = harness.FactionStore.Revision;
        long personRevision = harness.PersonStore.Revision;

        Assert.That(harness.FactionStore.TryRegister(
            new FactionRecord(new FactionId("frb.faulted"), "Faulted", 0L),
            out FactionFoundationFailure factionFailure), Is.False);
        Assert.That(factionFailure.Code, Is.EqualTo(FactionFoundationFailureCode.RuntimeFaulted));
        Assert.That(harness.PersonStore.TryRegister(
            new PersonRuntime(new PersonId("frb.faulted")),
            out PersonStoreFailure personFailure), Is.False);
        Assert.That(personFailure, Is.EqualTo(PersonStoreFailure.RuntimeFaulted));
        Assert.That(harness.FactionStore.Revision, Is.EqualTo(factionRevision));
        Assert.That(harness.PersonStore.Revision, Is.EqualTo(personRevision));
    }

    private static ReadHarness CreateBoundHarness()
    {
        ReadHarness harness = CreateUnboundHarness();
        Assert.That(harness.Admission.TryBindStores(harness.FactionStore, harness.PersonStore), Is.True);
        return harness;
    }

    private static ReadHarness CreateUnboundHarness()
    {
        RuntimeState runtime = new RuntimeState
        {
            AdmissionContext = SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            IsWorldPublished = true,
            IsHealthy = true,
            IsBootstrapOrAdvanceActive = false,
            LogicalBoundary = 0L
        };
        PersonStore personStore = new PersonStore();
        FactionStore factionStore = new FactionStore(personStore);
        return new ReadHarness(runtime, personStore, factionStore, new FactualReadAdmission(runtime));
    }

    private static ReadHarness CreateUnselectedHarness()
    {
        RuntimeState runtime = new RuntimeState
        {
            AdmissionContext = null,
            IsWorldPublished = true,
            IsHealthy = true,
            IsBootstrapOrAdvanceActive = false,
            LogicalBoundary = 0L
        };
        PersonStore personStore = new PersonStore();
        FactionStore factionStore = new FactionStore(personStore);
        return new ReadHarness(runtime, personStore, factionStore, new FactualReadAdmission(runtime));
    }

    private static PersonRuntime RegisterPerson(PersonStore store, string id)
    {
        PersonRuntime person = new PersonRuntime(new PersonId(id));
        Assert.That(store.TryRegister(person, out PersonStoreFailure failure), Is.True, failure.ToString());
        return person;
    }

    private static void AssertCaptureUnavailable(Action<RuntimeState> update)
    {
        ReadHarness harness = CreateUnboundHarness();
        update(harness.Runtime);
        Assert.That(harness.Admission.TryBindStores(harness.FactionStore, harness.PersonStore), Is.True);
        FactualReadCoordinator coordinator = new FactualReadCoordinator(
            harness.Admission,
            new IFactualReader[]
            {
                new DelegateReader<int>("value", 1, () => FactReadResult<int>.Present(1))
            });

        Assert.That(coordinator.TryCaptureCoherent(out FactualReadCapture capture, "value"), Is.False);
        AssertUnavailable<int>(capture, "value");
    }

    private static void AssertUnavailable<T>(FactualReadCapture capture, string capabilityId)
    {
        Assert.That(capture, Is.Not.Null);
        Assert.That(capture.IsCoherent, Is.False);
        Assert.That(capture.LogicalBoundary, Is.Null);
        Assert.That(capture.TryGet<T>(capabilityId, out FactReadResult<T> result), Is.True);
        Assert.That(result.Status, Is.EqualTo(FactReadStatus.Unavailable));
    }

    private sealed class ReadHarness
    {
        internal ReadHarness(
            RuntimeState runtime,
            PersonStore personStore,
            FactionStore factionStore,
            FactualReadAdmission admission)
        {
            Runtime = runtime;
            PersonStore = personStore;
            FactionStore = factionStore;
            Admission = admission;
        }

        internal RuntimeState Runtime { get; }
        internal PersonStore PersonStore { get; }
        internal FactionStore FactionStore { get; }
        internal FactualReadAdmission Admission { get; }
    }

    private sealed class RuntimeState : IFactualReadRuntimeState
    {
        public SimulationRuntimeAdmissionContext AdmissionContext { get; set; }
        public bool IsWorldPublished { get; set; }
        public bool IsHealthy { get; set; }
        public bool IsBootstrapOrAdvanceActive { get; set; }
        public long LogicalBoundary { get; set; }

        public bool TryReadCompletedLogicalBoundary(out long logicalBoundary)
        {
            logicalBoundary = LogicalBoundary;
            return true;
        }
    }

    private sealed class DelegateReader<T> : IFactualReader<T>
    {
        private readonly Func<FactReadResult<T>> read;

        internal DelegateReader(string capabilityId, int version, Func<FactReadResult<T>> read)
        {
            CapabilityId = capabilityId;
            Version = version;
            this.read = read ?? throw new ArgumentNullException(nameof(read));
        }

        public string CapabilityId { get; }
        public int Version { get; }
        public Type ValueType => typeof(T);
        public FactualReadOutcome<T> Read() => FactualReadOutcome<T>.FromResult(read());

        IFactualReadOutcome IFactualReader.ReadUntyped() => Read();
    }
}
