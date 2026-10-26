using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class DailyV1P12CrimeJusticeMutationCoordinatorTests
{
    private const string NpcSection = "p12b.npc-status-crime-state/npc-1";
    private const string JusticeSection = "p12b.justice-records/v1";
    private const string ReceiptsSection = "p18d.justice-receipts/v1";

    [Test]
    public void DailyMutationReservesOneEpochAndPublishesOnlyChangedExactOwnerSections()
    {
        FakeProtocol protocol = new FakeProtocol();
        int faults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator = CreateCoordinator(protocol, () => faults++);
        object npc = new object();
        object justice = new object();
        long npcRevision = 7;
        long justiceRevision = 3;
        DailyV1P12CrimeJusticeOwnerBinding[] owners =
        {
            Binding(npc, NpcSection, () => npcRevision),
            Binding(justice, JusticeSection, () => justiceRevision)
        };
        string[] sections = { NpcSection, JusticeSection, ReceiptsSection };

        coordinator.ExecuteDailyMutation(owners, sections, () =>
        {
            Assert.That(coordinator.CanBeginDailyMutation(), Is.False,
                "A reentrant daily scope cannot overlap the current reserved operation.");
            Assert.That(coordinator.CanCommit(npc, NpcSection), Is.True);
            npcRevision++;
            coordinator.NotifyCommitted(npc, NpcSection);
            Assert.That(coordinator.CanCommit(npc, NpcSection), Is.True,
                "A section already changed in the reserved scope does not recapture its old census baseline.");
            Assert.That(coordinator.CanCommit(justice, JusticeSection), Is.True);
        });

        Assert.That(protocol.Calls, Is.EqualTo(new[]
        {
            "validate:" + string.Join("|", sections),
            "reserve",
            "validate:" + NpcSection,
            "validate:" + JusticeSection,
            "reserved-commit:" + NpcSection
        }));
        Assert.That(faults, Is.Zero);
        Assert.That(coordinator.CanBeginDailyMutation(), Is.True);
        Assert.That(coordinator.CanCommit(npc, NpcSection), Is.False,
            "A committed owner cannot be written after its scope has closed.");
        Assert.That(faults, Is.EqualTo(1));
    }

    [Test]
    public void NoOpDailyMutationReleasesReservationWithoutAdvancingEpoch()
    {
        FakeProtocol protocol = new FakeProtocol();
        DailyV1P12CrimeJusticeMutationCoordinator coordinator = CreateCoordinator(protocol, () => { });
        object npc = new object();

        coordinator.ExecuteDailyMutation(
            new[] { Binding(npc, NpcSection, () => 0L) },
            new[] { NpcSection },
            () => { });

        Assert.That(protocol.Calls, Is.EqualTo(new[]
        {
            "validate:" + NpcSection,
            "reserve",
            "release"
        }));
    }

    [Test]
    public void NpcStatusBindingUsesExactCoreCompositeAndItsPersistedRevision()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npcDefinition = definitions.Npcs.Values.First();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-status-binding", definitions, npcDefinition, 0f,
            out DailyV1NpcRuntimeOwners owner), Is.True);

        DailyV1P12CrimeJusticeOwnerBinding binding =
            DailyV1P12CrimeJusticeOwnerBinding.ForNpcStatusOwner(owner);

        Assert.That(binding.OwnerIdentity, Is.SameAs(owner));
        Assert.That(binding.SectionId, Is.EqualTo(
            DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(owner.RuntimeId)));
        Assert.That(DailyV1SelectedOwnerSectionCensusContract.NpcSectionPrefixFor(19),
            Is.EqualTo(DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimePrefix));
        Assert.That(owner.TryInstallCrimeJusticeRevision(17L), Is.True);
        Assert.That(binding.ReadRevision(), Is.EqualTo(17L),
            "The operation binding observes the restored composite owner's exact revision.");
        owner.NotifyCrimeJusticeMutationCommitted();
        Assert.That(binding.ReadRevision(), Is.EqualTo(18L));
        Assert.Throws<ArgumentException>(() =>
            DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(" "));
    }

    [Test]
    public void SelectedNpcStatusScopeDerivesExactBindingsAndOrderedSectionsFromCoreGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition[] npcDefinitions = definitions.Npcs.Values.Take(2).ToArray();
        Assert.That(npcDefinitions, Has.Length.EqualTo(2));
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-scope-a", definitions, npcDefinitions[0], 0f,
            out DailyV1NpcRuntimeOwners first), Is.True);
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-scope-b", definitions, npcDefinitions[1], 0f,
            out DailyV1NpcRuntimeOwners second), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, Array.Empty<DailyV1CityRuntimeOwners>(), new[] { first, second },
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);

        FakeProtocol protocol = new FakeProtocol();
        int faults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator = CreateCoordinator(protocol, () => faults++);
        DailyV1NpcRuntimeOwners[] participants = { first, null, second, first };

        Assert.That(coordinator.TryBeginSelectedNpcStatusMutation(
            graph, participants, out IDisposable scope), Is.True);
        using (scope)
        {
            string firstSection = DailyV1SelectedOwnerSectionCensusContract
                .NpcStatusCrimeSectionIdFor(first.RuntimeId);
            Assert.That(coordinator.CanCommit(first, firstSection), Is.True);
            first.NotifyCrimeJusticeMutationCommitted();
            coordinator.NotifyCommitted(first, firstSection);
        }

        string firstId = DailyV1SelectedOwnerSectionCensusContract
            .NpcStatusCrimeSectionIdFor(first.RuntimeId);
        string secondId = DailyV1SelectedOwnerSectionCensusContract
            .NpcStatusCrimeSectionIdFor(second.RuntimeId);
        Assert.That(protocol.Calls, Is.EqualTo(new[]
        {
            "validate:" + firstId + "|" + secondId,
            "validate:" + firstId,
            "capacity",
            "notify:" + firstId
        }));
        Assert.That(faults, Is.Zero);
    }

    [Test]
    public void SelectedDailyMutationDerivesNpcOwnersAndSectionsFromExactAdmittedCoreGraph()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        FakeProtocol protocol = new FakeProtocol();
        int faults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator =
            CreateCoordinator(protocol, () => faults++);
        object crimeProjection = new object();
        object justiceProjection = new object();
        DailyV1JusticeRecordsOwner justiceOwner = composition.OwnerGraph.JusticeRecordsOwner;
        Assert.That(justiceOwner, Is.Not.Null);
        DailyV1JusticeWantedRecordOwner wantedRecord = new DailyV1JusticeWantedRecordOwner(
            composition.OwnerGraph.Npcs[0].RuntimeId,
            composition.OwnerGraph.Cities[0].RuntimeId,
            bounty: 10f,
            sentenceDays: 2);
        justiceOwner.AddWantedRecordForStaging(wantedRecord);
        Assert.That(justiceOwner.Revision, Is.Zero);
        Assert.That(justiceOwner.TryBindP12MutationBoundary(
            () => coordinator.CanCommit(justiceProjection,
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId),
            () => coordinator.NotifyCommitted(justiceProjection,
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId)), Is.True);
        DailyV1NpcRuntimeOwners changedNpc = composition.OwnerGraph.Npcs[0];
        string changedNpcSection = DailyV1SelectedOwnerSectionCensusContract
            .NpcStatusCrimeSectionIdFor(changedNpc.RuntimeId);

        coordinator.ExecuteSelectedDailyMutation(
            composition.ExecutionBoundaryState,
            composition.OwnerGraph,
            DailyV1SelectedCrimeJusticeDailyPhase.AdvanceSentences,
            crimeProjection,
            crimeProjection,
            justiceProjection,
            justiceProjection,
            includeCrimeReceiptSection: true,
            includeJusticeReceiptSection: true,
            () =>
            {
                Assert.That(coordinator.CanCommit(changedNpc, changedNpcSection), Is.True,
                    "The selected Core status owner is admitted directly from its exact graph instance.");
                changedNpc.NotifyCrimeJusticeMutationCommitted();
                coordinator.NotifyCommitted(changedNpc, changedNpcSection);

                Assert.That(coordinator.CanCommit(justiceProjection,
                    DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId), Is.True);
                Assert.That(wantedRecord.TryAddPenalty(5f, 1), Is.True,
                    "Core mutates the exact Justice owner revision inside the same admitted daily scope.");
            });

        List<string> expectedSections = new List<string>
        {
            DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.JusticeP18ReceiptsSectionId,
            DailyV1SelectedOwnerSectionCensusContract.CrimeP18ReceiptsSectionId
        };
        expectedSections.AddRange(composition.OwnerGraph.Npcs
            .Select(owner => DailyV1SelectedOwnerSectionCensusContract
                .NpcStatusCrimeSectionIdFor(owner.RuntimeId))
            .OrderBy(section => section, StringComparer.Ordinal));
        Assert.That(protocol.Calls[0], Is.EqualTo("validate:" + string.Join("|", expectedSections)),
            "The selected mutation validates the exact selected-owner inventory and preserves the established section order.");
        Assert.That(protocol.Calls, Does.Contain("reserved-commit:" + string.Join("|",
            new[]
            {
                changedNpcSection,
                DailyV1SelectedOwnerSectionCensusContract.JusticeRecordsSectionId
            }.OrderBy(section => section, StringComparer.Ordinal))));
        Assert.That(justiceOwner.Revision, Is.EqualTo(1L),
            "Justice revision is read from the exact Core graph owner, not a host callback.");
        Assert.That(faults, Is.Zero);
        Assert.That(coordinator.CanBeginDailyMutation(), Is.True);
    }

    [Test]
    public void SelectedDailyMutationRejectsMismatchedCrimeReceiptIdentityBeforeMutation()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        FakeProtocol protocol = new FakeProtocol();
        int faults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator =
            CreateCoordinator(protocol, () => faults++);
        object currentCrimeIdentity = new object();
        object boundCrimeReceiptIdentity = new object();
        int mutationCount = 0;

        Assert.Throws<InvalidOperationException>(() =>
            coordinator.ExecuteSelectedDailyMutation(
                composition.ExecutionBoundaryState,
                composition.OwnerGraph,
                DailyV1SelectedCrimeJusticeDailyPhase.ClearArrestedToday,
                currentCrimeIdentity,
                boundCrimeReceiptIdentity,
                justiceOwnerIdentity: null,
                justiceReceiptCensusOwnerIdentity: null,
                includeCrimeReceiptSection: true,
                includeJusticeReceiptSection: false,
                mutation: () => mutationCount++));

        Assert.That(mutationCount, Is.Zero,
            "Core must reject a different Crime receipt census identity before entering the mutation scope.");
        Assert.That(protocol.Calls, Is.EqualTo(new[] { "fault" }),
            "The Core-owned selected path closes admission, without reading owner baselines or reserving an epoch.");
        Assert.That(faults, Is.Zero,
            "Selected Daily-v1 faulting is owned by Core and its census protocol port, not the legacy host callback.");
    }

    [Test]
    public void SelectedDailyMutationRejectsMismatchedJusticeReceiptIdentityBeforeMutation()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        FakeProtocol protocol = new FakeProtocol();
        int faults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator =
            CreateCoordinator(protocol, () => faults++);
        object currentJusticeIdentity = new object();
        object boundJusticeReceiptIdentity = new object();
        int mutationCount = 0;

        Assert.Throws<InvalidOperationException>(() =>
            coordinator.ExecuteSelectedDailyMutation(
                composition.ExecutionBoundaryState,
                composition.OwnerGraph,
                DailyV1SelectedCrimeJusticeDailyPhase.AdvanceSentences,
                crimeOwnerIdentity: null,
                crimeReceiptCensusOwnerIdentity: null,
                justiceOwnerIdentity: currentJusticeIdentity,
                justiceReceiptCensusOwnerIdentity: boundJusticeReceiptIdentity,
                includeCrimeReceiptSection: false,
                includeJusticeReceiptSection: true,
                mutation: () => mutationCount++));

        Assert.That(mutationCount, Is.Zero,
            "Core must reject a different Justice receipt census identity before entering the mutation scope.");
        Assert.That(protocol.Calls, Is.EqualTo(new[] { "fault" }),
            "The Core-owned selected path closes admission, without reading owner baselines or reserving an epoch.");
        Assert.That(faults, Is.Zero,
            "Selected Daily-v1 faulting is owned by Core and its census protocol port, not the legacy host callback.");
    }

    [Test]
    public void SelectedDailyMutationRejectsUnadmittedExecutionStateBeforeCensusOrMutation()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);
        DailyV1ExecutionBoundaryState unadmittedState = new DailyV1ExecutionBoundaryState(
            composition.ExecutionBoundaryState.SimulationTime,
            null,
            composition.ExecutionBoundaryState.CompositionProfile,
            composition.ExecutionBoundaryState.WorldId);

        FakeProtocol protocol = new FakeProtocol();
        int legacyFaults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator =
            CreateCoordinator(protocol, () => legacyFaults++);
        int mutationCount = 0;

        Assert.Throws<InvalidOperationException>(() =>
            coordinator.ExecuteSelectedDailyMutation(
                unadmittedState,
                composition.OwnerGraph,
                DailyV1SelectedCrimeJusticeDailyPhase.ClearArrestedToday,
                crimeOwnerIdentity: null,
                crimeReceiptCensusOwnerIdentity: null,
                justiceOwnerIdentity: null,
                justiceReceiptCensusOwnerIdentity: null,
                includeCrimeReceiptSection: false,
                includeJusticeReceiptSection: false,
                mutation: () => mutationCount++));

        Assert.That(mutationCount, Is.Zero);
        Assert.That(protocol.Calls, Is.EqualTo(new[] { "fault" }),
            "An unadmitted Core execution state is faulted before section baseline reads or epoch reservation.");
        Assert.That(legacyFaults, Is.Zero);
    }

    [Test]
    public void SelectedDailyMutationRejectsWrongOwnerThreadBeforeCensusOrMutation()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);

        FakeProtocol protocol = new FakeProtocol();
        int legacyFaults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator =
            CreateCoordinator(protocol, () => legacyFaults++);
        int mutationCount = 0;
        Exception workerException = null;
        System.Threading.Thread worker = new System.Threading.Thread(() =>
        {
            try
            {
                coordinator.ExecuteSelectedDailyMutation(
                    composition.ExecutionBoundaryState,
                    composition.OwnerGraph,
                    DailyV1SelectedCrimeJusticeDailyPhase.ClearArrestedToday,
                    crimeOwnerIdentity: null,
                    crimeReceiptCensusOwnerIdentity: null,
                    justiceOwnerIdentity: null,
                    justiceReceiptCensusOwnerIdentity: null,
                    includeCrimeReceiptSection: false,
                    includeJusticeReceiptSection: false,
                    mutation: () => mutationCount++);
            }
            catch (Exception exception)
            {
                workerException = exception;
            }
        });

        worker.Start();
        worker.Join();

        Assert.That(workerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(mutationCount, Is.Zero,
            "The owner-thread rejection must precede any selected daily mutation.");
        Assert.That(protocol.Calls, Is.EqualTo(new[] { "fault" }),
            "Core rejects the wrong owner thread before reading section baselines or reserving an epoch.");
        Assert.That(legacyFaults, Is.Zero);
    }

    [Test]
    public void SelectedDailyPreflightRejectsUnadmittedStateAndWrongOwnerThread()
    {
        DailyV1EffectiveDefinitions definitions =
            DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        Assert.That(DailyV1GenesisComposer.TryComposeSelectedOwners(
                definitions,
                SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
                out DailyV1GenesisComposition composition,
                out string diagnostic), Is.True, diagnostic);
        DailyV1ExecutionBoundaryState unadmittedState = new DailyV1ExecutionBoundaryState(
            composition.ExecutionBoundaryState.SimulationTime,
            null,
            composition.ExecutionBoundaryState.CompositionProfile,
            composition.ExecutionBoundaryState.WorldId);

        FakeProtocol unadmittedProtocol = new FakeProtocol();
        int unadmittedLegacyFaults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator unadmittedCoordinator =
            CreateCoordinator(unadmittedProtocol, () => unadmittedLegacyFaults++);
        Assert.That(unadmittedCoordinator.CanBeginSelectedDailyMutation(
            unadmittedState, composition.OwnerGraph), Is.False);
        Assert.That(unadmittedProtocol.Calls, Is.EqualTo(new[] { "fault" }));
        Assert.That(unadmittedLegacyFaults, Is.Zero);

        FakeProtocol wrongThreadProtocol = new FakeProtocol();
        int wrongThreadLegacyFaults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator wrongThreadCoordinator =
            CreateCoordinator(wrongThreadProtocol, () => wrongThreadLegacyFaults++);
        bool workerResult = true;
        System.Threading.Thread worker = new System.Threading.Thread(() =>
            workerResult = wrongThreadCoordinator.CanBeginSelectedDailyMutation(
                composition.ExecutionBoundaryState, composition.OwnerGraph));

        worker.Start();
        worker.Join();

        Assert.That(workerResult, Is.False);
        Assert.That(wrongThreadProtocol.Calls, Is.EqualTo(new[] { "fault" }));
        Assert.That(wrongThreadLegacyFaults, Is.Zero);
    }

    [Test]
    public void SelectedNpcStatusScopeRejectsEqualButUninstalledOwnerBeforeProtocolAccess()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1NpcDefinition npcDefinition = definitions.Npcs.Values.First();
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-exact-scope", definitions, npcDefinition, 0f,
            out DailyV1NpcRuntimeOwners installed), Is.True);
        Assert.That(DailyV1SelectedRuntimeOwnerGraph.TryCreate(
            definitions, Array.Empty<DailyV1CityRuntimeOwners>(), new[] { installed },
            out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        Assert.That(DailyV1NpcRuntimeOwners.TryCreateInitial(
            "npc-exact-scope", definitions, npcDefinition, 0f,
            out DailyV1NpcRuntimeOwners alias), Is.True);

        FakeProtocol protocol = new FakeProtocol();
        int faults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator = CreateCoordinator(protocol, () => faults++);

        Assert.That(coordinator.TryBeginSelectedNpcStatusMutation(
            graph, new[] { alias }, out IDisposable scope), Is.False);
        Assert.That(scope, Is.Null);
        Assert.That(faults, Is.EqualTo(1));
        Assert.That(protocol.Calls, Is.Empty,
            "The Core graph rejects a foreign same-ID owner before consulting P12 baselines or reserving an operation.");
    }

    [Test]
    public void ImmediateMutationRequiresExactOwnerAndNotifiesCommittedWriteImmediately()
    {
        FakeProtocol protocol = new FakeProtocol();
        int faults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator = CreateCoordinator(protocol, () => faults++);
        EqualIdentity npc = new EqualIdentity();
        EqualIdentity alias = new EqualIdentity();
        long revision = 4;
        DailyV1P12CrimeJusticeOwnerBinding binding = Binding(npc, NpcSection, () => revision);

        Assert.That(coordinator.TryBeginImmediateMutation(
            new[] { binding }, new[] { NpcSection }, out IDisposable scope), Is.True);
        using (scope)
        {
            Assert.That(coordinator.CanCommit(alias, NpcSection), Is.False,
                "Equal-but-distinct owner objects do not inherit the exact admitted owner identity.");
        }

        Assert.That(faults, Is.EqualTo(1));
        Assert.That(coordinator.TryBeginImmediateMutation(
            new[] { binding }, new[] { NpcSection }, out scope), Is.True);
        using (scope)
        {
            Assert.That(coordinator.CanCommit(npc, NpcSection), Is.True);
            revision++;
            coordinator.NotifyCommitted(npc, NpcSection);
        }
        Assert.That(protocol.Calls.TakeLast(4), Is.EqualTo(new[]
        {
            "validate:" + NpcSection,
            "validate:" + NpcSection,
            "capacity",
            "notify:" + NpcSection
        }));
    }

    [Test]
    public void DailyMutationCompletesReservationBeforeRethrowingWriterFailure()
    {
        FakeProtocol protocol = new FakeProtocol();
        DailyV1P12CrimeJusticeMutationCoordinator coordinator = CreateCoordinator(protocol, () => { });
        object npc = new object();
        InvalidOperationException expected = new InvalidOperationException("writer failure");

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
            coordinator.ExecuteDailyMutation(
                new[] { Binding(npc, NpcSection, () => 0L) },
                new[] { NpcSection },
                () =>
                {
                    Assert.That(coordinator.CanCommit(npc, NpcSection), Is.True);
                    coordinator.NotifyCommitted(npc, NpcSection);
                    throw expected;
                }));

        Assert.That(actual, Is.SameAs(expected));
        Assert.That(protocol.Calls.Last(), Is.EqualTo("reserved-commit:" + NpcSection));
    }

    [Test]
    public void RevisionExhaustionFaultsBeforeCommit()
    {
        FakeProtocol protocol = new FakeProtocol();
        int faults = 0;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator = CreateCoordinator(protocol, () => faults++);
        object npc = new object();
        Assert.That(coordinator.TryBeginImmediateMutation(
            new[] { Binding(npc, NpcSection, () => long.MaxValue) },
            new[] { NpcSection }, out IDisposable scope), Is.True);
        using (scope)
            Assert.That(coordinator.CanCommit(npc, NpcSection), Is.False);

        Assert.That(faults, Is.EqualTo(1));
        Assert.That(protocol.Calls, Does.Not.Contain("capacity"));
        Assert.That(protocol.Calls, Does.Not.Contain("notify:" + NpcSection));
    }

    private static DailyV1P12CrimeJusticeMutationCoordinator CreateCoordinator(
        FakeProtocol protocol,
        Action fault) => new DailyV1P12CrimeJusticeMutationCoordinator(
            () => true, () => true, fault, protocol);

    private static DailyV1P12CrimeJusticeOwnerBinding Binding(
        object owner, string section, Func<long> revision) =>
        new DailyV1P12CrimeJusticeOwnerBinding(owner, section, revision);

    private sealed class EqualIdentity
    {
        public override bool Equals(object obj) => obj is EqualIdentity;
        public override int GetHashCode() => 1;
    }

    private sealed class FakeProtocol : IDailyV1P12CrimeJusticeMutationProtocol
    {
        internal readonly List<string> Calls = new List<string>();

        public void FaultClosed() => Calls.Add("fault");

        public bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds)
        {
            Calls.Add("validate:" + string.Join("|", sectionIds));
            return true;
        }

        public bool TryReserveMutationEpoch(out object reservation)
        {
            Calls.Add("reserve");
            reservation = new object();
            return true;
        }

        public bool TryNotifyReservedCommittedMutations(
            object reservation, IReadOnlyCollection<string> sectionIds)
        {
            Calls.Add("reserved-commit:" + string.Join("|", sectionIds.OrderBy(value => value, StringComparer.Ordinal)));
            return true;
        }

        public void ReleaseMutationEpoch(object reservation) => Calls.Add("release");
        public bool TryValidateMutationEpochCapacity() { Calls.Add("capacity"); return true; }
        public bool NotifyCommittedMutation(string sectionId) { Calls.Add("notify:" + sectionId); return true; }
    }
}
