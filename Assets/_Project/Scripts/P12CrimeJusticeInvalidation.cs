using System;
using System.Collections.Generic;

public sealed partial class SimulationRuntime
{
    private sealed class P12CrimeJusticeMutationScope : IDisposable
    {
        private IDisposable coreScope;

        internal P12CrimeJusticeMutationScope(IDisposable coreScope) => this.coreScope = coreScope;

        public void Dispose()
        {
            IDisposable scope = coreScope;
            coreScope = null;
            scope?.Dispose();
        }
    }

    private readonly HashSet<NpcRuntime> p12CrimeJusticeBoundNpcs = new HashSet<NpcRuntime>();
    private JusticeSystem p12CrimeJusticeBoundJusticeSystem;
    private CrimeSystem p12CrimeJusticeBoundCrimeSystem;
    private DailyV1P12CrimeJusticeMutationCoordinator p12CrimeJusticeMutationCoordinator;

    private sealed class ContinuationCrimeJusticeMutationProtocolAdapter
        : IDailyV1P12CrimeJusticeMutationProtocol
    {
        private readonly Func<ContinuationCensusProtocol> resolveProtocol;

        internal ContinuationCrimeJusticeMutationProtocolAdapter(
            Func<ContinuationCensusProtocol> resolveProtocol) =>
            this.resolveProtocol = resolveProtocol ?? throw new ArgumentNullException(nameof(resolveProtocol));

        public bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds) =>
            resolveProtocol()?.TryValidateUnchangedSections(sectionIds, out _) == true;

        public bool TryReserveMutationEpoch(out object reservation)
        {
            reservation = null;
            ContinuationCensusProtocol protocol = resolveProtocol();
            if (protocol == null
                || !protocol.TryReserveMutationEpochCapacity(
                    out ContinuationMutationEpochReservation candidate, out _))
                return false;
            reservation = candidate;
            return true;
        }

        public bool TryNotifyReservedCommittedMutations(
            object reservation,
            IReadOnlyCollection<string> sectionIds) =>
            reservation is ContinuationMutationEpochReservation typed
            && resolveProtocol()?.TryNotifyReservedCommittedMutations(typed, sectionIds, out _) == true;

        public void ReleaseMutationEpoch(object reservation)
        {
            if (reservation is ContinuationMutationEpochReservation typed)
                resolveProtocol()?.ReleaseMutationEpochReservation(typed);
        }

        public bool TryValidateMutationEpochCapacity() =>
            resolveProtocol()?.TryValidateMutationEpochCapacity(out _) == true;

        public bool NotifyCommittedMutation(string sectionId) =>
            resolveProtocol()?.NotifyCommittedMutation(sectionId, out _) == true;
    }

    private DailyV1P12CrimeJusticeMutationCoordinator P12CrimeJusticeMutationCoordinator =>
        p12CrimeJusticeMutationCoordinator ??= new DailyV1P12CrimeJusticeMutationCoordinator(
            () => runtimeAdmissionContext != null,
            IsRuntimeAdmissionOwnerThreadCurrent,
            FaultRuntimeAdmission,
            new ContinuationCrimeJusticeMutationProtocolAdapter(() => npcRosterCensusProtocol));

    private bool TryRegisterP12CrimeJusticeOwnerSections(ContinuationCensusProtocol protocol)
    {
        if (protocol == null) return false;
        try
        {
            if (justiceSystem != null)
            {
                P12CrimeJusticeCensusProvider.JusticeRecordsSectionProvider recordsProvider =
                    new P12CrimeJusticeCensusProvider.JusticeRecordsSectionProvider(
                        justiceSystem,
                        npcRuntimeId => !string.IsNullOrWhiteSpace(npcRuntimeId)
                            && npcRegistryById.TryGetValue(npcRuntimeId, out NpcRuntime installed)
                            && string.Equals(installed.RuntimeId, npcRuntimeId, StringComparison.Ordinal),
                        IsExactInstalledP12JusticeCityRuntimeId);
                OwnerSectionCensusWitness recordsWitness = recordsProvider.GetCurrentCensus();
                if (recordsWitness.SchemaVersion != P12CrimeJusticeCensusProvider.SchemaVersion
                    || !ReferenceEquals(recordsWitness.OwnerInstanceIdentity, justiceSystem))
                    return false;
                OwnerSectionContract recordsContract = new OwnerSectionContract(
                    P12CrimeJusticeCensusProvider.JusticeRecordsSectionId,
                    P12CrimeJusticeCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required);
                if (!protocol.RegisterExpectedSection(recordsContract, out _)
                    || !protocol.RegisterCensusProvider(recordsContract.SectionId, recordsProvider, out _))
                    return false;

                P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionProvider justiceReceiptsProvider =
                    new P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionProvider(justiceSystem);
                OwnerSectionCensusWitness justiceReceiptsWitness = justiceReceiptsProvider.GetCurrentCensus();
                if (justiceReceiptsWitness.Revision != 0L
                    || justiceReceiptsWitness.Cardinality != 1
                    || !ReferenceEquals(justiceReceiptsWitness.OwnerInstanceIdentity, justiceSystem))
                    return false;
                OwnerSectionContract justiceReceiptsContract = new OwnerSectionContract(
                    P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId,
                    P12CrimeJusticeCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required);
                if (!protocol.RegisterExpectedSection(justiceReceiptsContract, out _)
                    || !protocol.RegisterCensusProvider(justiceReceiptsContract.SectionId, justiceReceiptsProvider, out _))
                    return false;
            }

            if (crimeSystem != null)
            {
                P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionProvider crimeReceiptsProvider =
                    new P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionProvider(crimeSystem);
                OwnerSectionCensusWitness crimeReceiptsWitness = crimeReceiptsProvider.GetCurrentCensus();
                if (crimeReceiptsWitness.Revision != 0L
                    || crimeReceiptsWitness.Cardinality != 1
                    || !ReferenceEquals(crimeReceiptsWitness.OwnerInstanceIdentity, crimeSystem))
                    return false;
                OwnerSectionContract crimeReceiptsContract = new OwnerSectionContract(
                    P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionId,
                    P12CrimeJusticeCensusProvider.SchemaVersion,
                    OwnerSectionRole.Required);
                if (!protocol.RegisterExpectedSection(crimeReceiptsContract, out _)
                    || !protocol.RegisterCensusProvider(crimeReceiptsContract.SectionId, crimeReceiptsProvider, out _))
                    return false;
            }

            return justiceSystem == null
                || crimeSystem == null
                || ReferenceEquals(crimeSystem.JusticeOwner, justiceSystem);
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryBindP12CrimeJusticeMutationBoundaries()
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent() || npcRosterCensusProtocol == null)
            return false;

        if (justiceSystem != null)
        {
            if (p12CrimeJusticeBoundJusticeSystem != null
                && !ReferenceEquals(p12CrimeJusticeBoundJusticeSystem, justiceSystem))
                return false;
            if (p12CrimeJusticeBoundJusticeSystem == null)
            {
                if (!justiceSystem.TryBindP12CrimeJusticeMutationBoundary(
                        CanCommitP12JusticeRecordsMutation,
                        NotifyP12JusticeRecordsMutation)
                    || !justiceSystem.TryBindP12DailyProfileReceiptBoundary())
                    return false;
                p12CrimeJusticeBoundJusticeSystem = justiceSystem;
            }
        }

        if (crimeSystem != null)
        {
            if (p12CrimeJusticeBoundCrimeSystem != null
                && !ReferenceEquals(p12CrimeJusticeBoundCrimeSystem, crimeSystem))
                return false;
            if (p12CrimeJusticeBoundCrimeSystem == null)
            {
                if (!crimeSystem.TryBindP12DailyProfileReceiptBoundary())
                    return false;
                p12CrimeJusticeBoundCrimeSystem = crimeSystem;
            }
        }

        foreach (NpcRuntime npc in npcRuntimeSnapshot)
        {
            if (npc == null) return false;
            if (npc.IsP12CrimeJusticeBound)
            {
                if (!p12CrimeJusticeBoundNpcs.Contains(npc)) return false;
                continue;
            }
            if (!npc.TryBindP12CrimeJusticeMutationBoundary(
                    CanCommitP12NpcCrimeJusticeMutation,
                    NotifyP12NpcCrimeJusticeMutation))
                return false;
            p12CrimeJusticeBoundNpcs.Add(npc);
        }
        return true;
    }

    private bool IsExactInstalledP12JusticeCityRuntimeId(string cityRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(cityRuntimeId)) return false;
        int matches = 0;
        foreach (CityRuntime city in cities)
        {
            if (city == null || !string.Equals(city.RuntimeId, cityRuntimeId, StringComparison.Ordinal))
                continue;
            matches++;
            if (matches > 1) return false;
        }
        return matches == 1;
    }

    private string GetP12CrimeJusticeNpcSectionId(NpcRuntime npc)
    {
        if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
            || !npcRegistryById.TryGetValue(npc.RuntimeId, out NpcRuntime installed)
            || !ReferenceEquals(installed, npc)
            || !p12CrimeJusticeBoundNpcs.Contains(npc))
            return null;
        DailyV1NpcRuntimeOwners selectedOwner = npc.SelectedRuntimeOwners;
        if (selectedOwner != null
            && !string.Equals(selectedOwner.RuntimeId, npc.RuntimeId, StringComparison.Ordinal))
            return null;
        return DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(npc.RuntimeId);
    }

    private DailyV1P12CrimeJusticeOwnerBinding CreateP12CrimeJusticeNpcBinding(NpcRuntime npc)
    {
        string sectionId = GetP12CrimeJusticeNpcSectionId(npc);
        if (sectionId == null) return null;
        DailyV1NpcRuntimeOwners selectedOwner = npc.SelectedRuntimeOwners;
        return selectedOwner != null
            ? DailyV1P12CrimeJusticeOwnerBinding.ForNpcStatusOwner(selectedOwner)
            : new DailyV1P12CrimeJusticeOwnerBinding(
                npc, sectionId, () => npc.P12CrimeJusticeRevision);
    }

    private DailyV1P12CrimeJusticeOwnerBinding CreateP12CrimeJusticeJusticeBinding(JusticeSystem justice)
    {
        if (justice == null || !ReferenceEquals(justice, justiceSystem)
            || !ReferenceEquals(p12CrimeJusticeBoundJusticeSystem, justice))
            return null;
        return new DailyV1P12CrimeJusticeOwnerBinding(
            justice,
            P12CrimeJusticeCensusProvider.JusticeRecordsSectionId,
            () => justice.P12CrimeJusticeRevision);
    }

    private string[] CreateP12CrimeJusticeDailySectionIds()
    {
        List<string> sections = new List<string>();
        if (justiceSystem != null)
        {
            if (!ReferenceEquals(p12CrimeJusticeBoundJusticeSystem, justiceSystem)) return null;
            sections.Add(P12CrimeJusticeCensusProvider.JusticeRecordsSectionId);
            sections.Add(P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId);
        }
        if (crimeSystem != null)
        {
            if (!ReferenceEquals(p12CrimeJusticeBoundCrimeSystem, crimeSystem)) return null;
            sections.Add(P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionId);
        }

        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            P12CrimeJusticeCensusProvider.CreateNpcStatusProviders(npcRuntimeSnapshot);
        if (providers.Count != npcRuntimeSnapshot.Count) return null;
        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            if (!(provider is P12CrimeJusticeCensusProvider.NpcStatusSectionProvider statusProvider)
                || GetP12CrimeJusticeNpcSectionId(statusProvider.NpcOwner) != statusProvider.SectionId)
                return null;
            sections.Add(statusProvider.SectionId);
        }
        return sections.ToArray();
    }

    private void RunP12CrimeJusticeDailyMutation(Action mutation)
    {
        if (mutation == null) throw new ArgumentNullException(nameof(mutation));
        if (runtimeAdmissionContext == null)
        {
            mutation();
            return;
        }
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || !P12CrimeJusticeMutationCoordinator.CanBeginDailyMutation())
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException("P12 Crime/Justice daily mutation scope is unavailable.");
        }

        string[] sections;
        try { sections = CreateP12CrimeJusticeDailySectionIds(); }
        catch { sections = null; }
        List<DailyV1P12CrimeJusticeOwnerBinding> owners =
            new List<DailyV1P12CrimeJusticeOwnerBinding>();
        if (runtimeAdmissionContext != null && sections != null)
        {
            if (justiceSystem != null)
            {
                DailyV1P12CrimeJusticeOwnerBinding justiceBinding =
                    CreateP12CrimeJusticeJusticeBinding(justiceSystem);
                if (justiceBinding == null)
                {
                    FaultRuntimeAdmission();
                    throw new InvalidOperationException("P12 Crime/Justice owner inventory is invalid.");
                }
                owners.Add(justiceBinding);
            }
            foreach (NpcRuntime npc in npcRuntimeSnapshot)
            {
                DailyV1P12CrimeJusticeOwnerBinding npcBinding = CreateP12CrimeJusticeNpcBinding(npc);
                if (npcBinding == null)
                {
                    FaultRuntimeAdmission();
                    throw new InvalidOperationException("P12 Crime/Justice owner inventory is invalid.");
                }
                owners.Add(npcBinding);
            }
        }
        if (sections == null)
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException("P12 Crime/Justice owner inventory is invalid.");
        }
        P12CrimeJusticeMutationCoordinator.ExecuteDailyMutation(owners, sections, mutation);
    }

    private bool TryBeginP12CrimeJusticeActionScope(
        IReadOnlyList<NpcRuntime> participants,
        bool includeJustice,
        out P12CrimeJusticeMutationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || participants == null)
        {
            FaultRuntimeAdmission();
            return false;
        }

        DailyV1SelectedRuntimeOwnerGraph selectedGraph = SelectedRuntimeOwnerGraph;
        if (!includeJustice && selectedGraph != null)
        {
            List<DailyV1NpcRuntimeOwners> selectedOwners =
                new List<DailyV1NpcRuntimeOwners>(participants.Count);
            HashSet<NpcRuntime> uniqueSelectedRuntimes = new HashSet<NpcRuntime>();
            bool allParticipantsAreSelected = true;
            foreach (NpcRuntime npc in participants)
            {
                if (npc == null || !uniqueSelectedRuntimes.Add(npc)) continue;
                if (GetP12CrimeJusticeNpcSectionId(npc) == null)
                {
                    FaultRuntimeAdmission();
                    return false;
                }

                DailyV1NpcRuntimeOwners selectedOwner = npc.SelectedRuntimeOwners;
                if (selectedOwner == null)
                {
                    allParticipantsAreSelected = false;
                    break;
                }
                selectedOwners.Add(selectedOwner);
            }

            if (allParticipantsAreSelected)
            {
                if (selectedOwners.Count == 0
                    || !P12CrimeJusticeMutationCoordinator.TryBeginSelectedNpcStatusMutation(
                        selectedGraph, selectedOwners, out IDisposable selectedCoreScope))
                {
                    FaultRuntimeAdmission();
                    return false;
                }
                scope = selectedCoreScope == null
                    ? null
                    : new P12CrimeJusticeMutationScope(selectedCoreScope);
                return true;
            }
        }

        List<DailyV1P12CrimeJusticeOwnerBinding> owners =
            new List<DailyV1P12CrimeJusticeOwnerBinding>(participants.Count + (includeJustice ? 1 : 0));
        List<string> sections = new List<string>(participants.Count + (includeJustice ? 3 : 0));
        HashSet<NpcRuntime> uniqueNpcs = new HashSet<NpcRuntime>();
        foreach (NpcRuntime npc in participants)
        {
            if (npc == null || !uniqueNpcs.Add(npc)) continue;
            string sectionId = GetP12CrimeJusticeNpcSectionId(npc);
            if (sectionId == null)
            {
                FaultRuntimeAdmission();
                return false;
            }
            DailyV1P12CrimeJusticeOwnerBinding binding = CreateP12CrimeJusticeNpcBinding(npc);
            if (binding == null)
            {
                FaultRuntimeAdmission();
                return false;
            }
            owners.Add(binding);
            sections.Add(sectionId);
        }

        if (includeJustice && justiceSystem != null)
        {
            DailyV1P12CrimeJusticeOwnerBinding binding =
                CreateP12CrimeJusticeJusticeBinding(justiceSystem);
            if (binding == null)
            {
                FaultRuntimeAdmission();
                return false;
            }
            owners.Add(binding);
            sections.Add(P12CrimeJusticeCensusProvider.JusticeRecordsSectionId);
            sections.Add(P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId);
        }
        if (includeJustice && crimeSystem != null)
            sections.Add(P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionId);

        if (sections.Count == 0
            || !P12CrimeJusticeMutationCoordinator.TryBeginImmediateMutation(
                owners, sections, out IDisposable coreScope))
        {
            FaultRuntimeAdmission();
            return false;
        }
        scope = coreScope == null ? null : new P12CrimeJusticeMutationScope(coreScope);
        return true;
    }

    private bool CanReuseP12CrimeJusticeActionScopeForForcedEscape(
        IReadOnlyList<NpcRuntime> participants)
    {
        if (runtimeAdmissionContext == null) return true;

        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || participants == null
            || justiceSystem == null
            || !ReferenceEquals(p12CrimeJusticeBoundJusticeSystem, justiceSystem))
        {
            FaultRuntimeAdmission();
            return false;
        }

        HashSet<NpcRuntime> expectedNpcs = new HashSet<NpcRuntime>();
        List<DailyV1P12CrimeJusticeOwnerBinding> owners =
            new List<DailyV1P12CrimeJusticeOwnerBinding>(participants.Count + 1);
        List<string> expectedSections = new List<string>(participants.Count + 3);
        foreach (NpcRuntime npc in participants)
        {
            if (npc == null || !expectedNpcs.Add(npc)) continue;
            string sectionId = GetP12CrimeJusticeNpcSectionId(npc);
            DailyV1P12CrimeJusticeOwnerBinding binding = CreateP12CrimeJusticeNpcBinding(npc);
            if (sectionId == null || binding == null)
            {
                FaultRuntimeAdmission();
                return false;
            }
            owners.Add(binding);
            expectedSections.Add(sectionId);
        }

        if (expectedNpcs.Count != 1
            || CreateP12CrimeJusticeJusticeBinding(justiceSystem) == null)
        {
            FaultRuntimeAdmission();
            return false;
        }
        owners.Add(CreateP12CrimeJusticeJusticeBinding(justiceSystem));
        expectedSections.Add(P12CrimeJusticeCensusProvider.JusticeRecordsSectionId);
        expectedSections.Add(P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId);
        if (crimeSystem != null)
            expectedSections.Add(P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionId);

        return P12CrimeJusticeMutationCoordinator.CanReuseImmediateMutation(owners, expectedSections);
    }

    private DailyV1SelectedActionStatusPlanResult ApplySelectedActionStatusOperation(
        IReadOnlyList<NpcRuntime> participants,
        bool reuseForcedEscapeScope,
        Func<DailyV1SelectedActionStatusPlanResult> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));
        if (runtimeAdmissionContext == null)
            return operation();

        if (reuseForcedEscapeScope)
        {
            if (!CanReuseP12CrimeJusticeActionScopeForForcedEscape(participants))
                throw new InvalidOperationException(
                    "P12 forced Escape status effects lost their exact admitted operation scope.");
            return operation();
        }

        if (!TryBeginP12CrimeJusticeActionScope(
                participants, includeJustice: false,
                out P12CrimeJusticeMutationScope scope))
            throw new InvalidOperationException(
                "P12 action status effects could not be admitted.");
        using (scope) return operation();
    }

    private bool CanCommitP12NpcCrimeJusticeMutation(NpcRuntime npc)
    {
        if (runtimeAdmissionContext == null) return true;
        DailyV1P12CrimeJusticeOwnerBinding binding = CreateP12CrimeJusticeNpcBinding(npc);
        if (binding == null)
        {
            FaultRuntimeAdmission();
            return false;
        }
        return P12CrimeJusticeMutationCoordinator.CanCommit(
            binding.OwnerIdentity, binding.SectionId);
    }

    private void NotifyP12NpcCrimeJusticeMutation(NpcRuntime npc)
    {
        if (runtimeAdmissionContext == null) return;
        DailyV1P12CrimeJusticeOwnerBinding binding = CreateP12CrimeJusticeNpcBinding(npc);
        if (binding == null)
        {
            FaultRuntimeAdmission();
            return;
        }
        P12CrimeJusticeMutationCoordinator.NotifyCommitted(
            binding.OwnerIdentity, binding.SectionId);
    }

    private bool CanCommitP12JusticeRecordsMutation(JusticeSystem justice)
    {
        if (runtimeAdmissionContext == null) return true;
        string sectionId = P12CrimeJusticeCensusProvider.JusticeRecordsSectionId;
        if (CreateP12CrimeJusticeJusticeBinding(justice) == null)
        {
            FaultRuntimeAdmission();
            return false;
        }
        return P12CrimeJusticeMutationCoordinator.CanCommit(justice, sectionId);
    }

    private void NotifyP12JusticeRecordsMutation(JusticeSystem justice)
    {
        if (runtimeAdmissionContext == null) return;
        string sectionId = P12CrimeJusticeCensusProvider.JusticeRecordsSectionId;
        if (CreateP12CrimeJusticeJusticeBinding(justice) == null)
        {
            FaultRuntimeAdmission();
            return;
        }
        P12CrimeJusticeMutationCoordinator.NotifyCommitted(justice, sectionId);
    }
}
