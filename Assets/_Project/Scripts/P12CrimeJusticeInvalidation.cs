using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

public sealed partial class SimulationRuntime
{
    private volatile P12CrimeJusticeMutationContext activeP12CrimeJusticeMutationContext;
    private readonly HashSet<NpcRuntime> p12CrimeJusticeBoundNpcs = new HashSet<NpcRuntime>();
    private JusticeSystem p12CrimeJusticeBoundJusticeSystem;
    private CrimeSystem p12CrimeJusticeBoundCrimeSystem;

    private enum P12CrimeJusticeMutationMode
    {
        DailyReserved,
        ActionImmediate
    }

    private sealed class P12CrimeJusticeMutationContext
    {
        public readonly P12CrimeJusticeMutationMode Mode;
        public readonly Thread OwnerThread;
        public readonly int OwnerManagedThreadId;
        public readonly HashSet<NpcRuntime> AllowedNpcs;
        public readonly HashSet<string> AllowedSectionIds;
        public readonly HashSet<string> ChangedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        public readonly ContinuationMutationEpochReservation EpochReservation;

        public P12CrimeJusticeMutationContext(
            P12CrimeJusticeMutationMode mode,
            IEnumerable<NpcRuntime> allowedNpcs,
            IEnumerable<string> allowedSectionIds,
            ContinuationMutationEpochReservation epochReservation)
        {
            Mode = mode;
            OwnerThread = Thread.CurrentThread;
            OwnerManagedThreadId = OwnerThread.ManagedThreadId;
            AllowedNpcs = new HashSet<NpcRuntime>(allowedNpcs ?? Array.Empty<NpcRuntime>());
            AllowedSectionIds = new HashSet<string>(allowedSectionIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            EpochReservation = epochReservation;
        }

        public bool IsOwnedByCurrentThread() => ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class P12CrimeJusticeMutationScope : IDisposable
    {
        private readonly SimulationRuntime owner;
        private readonly P12CrimeJusticeMutationContext context;
        private bool disposed;

        public P12CrimeJusticeMutationScope(
            SimulationRuntime owner,
            P12CrimeJusticeMutationContext context)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            owner.ExitP12CrimeJusticeActionScope(context);
        }
    }

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
                        npc => npc != null
                            && !string.IsNullOrWhiteSpace(npc.RuntimeId)
                            && npcRegistryById.TryGetValue(npc.RuntimeId, out NpcRuntime installed)
                            && ReferenceEquals(installed, npc),
                        city => city != null && cities.Contains(city));
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

    private string GetP12CrimeJusticeNpcSectionId(NpcRuntime npc)
    {
        if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
            || !npcRegistryById.TryGetValue(npc.RuntimeId, out NpcRuntime installed)
            || !ReferenceEquals(installed, npc)
            || !p12CrimeJusticeBoundNpcs.Contains(npc))
            return null;
        return P12CrimeJusticeCensusProvider.NpcStatusSectionIdFor(npc.RuntimeId);
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
            || activeP12CrimeJusticeMutationContext != null)
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException("P12 Crime/Justice daily mutation scope is unavailable.");
        }

        string[] sections;
        try { sections = CreateP12CrimeJusticeDailySectionIds(); }
        catch { sections = null; }
        if (sections == null)
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException("P12 Crime/Justice owner inventory is invalid.");
        }
        if (sections.Length == 0)
        {
            mutation();
            return;
        }
        if (new HashSet<string>(sections, StringComparer.Ordinal).Count != sections.Length
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(sections, out _)
            || !npcRosterCensusProtocol.TryReserveMutationEpochCapacity(
                out ContinuationMutationEpochReservation reservation,
                out _))
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException("P12 Crime/Justice daily mutation could not reserve its owner baseline and epoch.");
        }

        P12CrimeJusticeMutationContext context = new P12CrimeJusticeMutationContext(
            P12CrimeJusticeMutationMode.DailyReserved,
            npcRuntimeSnapshot,
            sections,
            reservation);
        activeP12CrimeJusticeMutationContext = context;
        Exception originalException = null;
        try
        {
            mutation();
        }
        catch (Exception exception)
        {
            originalException = exception;
        }

        bool completed = ExitP12CrimeJusticeDailyScope(context);
        if (originalException != null)
            ExceptionDispatchInfo.Capture(originalException).Throw();
        if (!completed)
            throw new InvalidOperationException("P12 Crime/Justice daily mutation notification failed.");
    }

    private bool ExitP12CrimeJusticeDailyScope(P12CrimeJusticeMutationContext context)
    {
        if (context == null) return false;
        bool valid = context.Mode == P12CrimeJusticeMutationMode.DailyReserved
            && context.IsOwnedByCurrentThread()
            && ReferenceEquals(activeP12CrimeJusticeMutationContext, context)
            && context.EpochReservation != null;
        if (!valid)
        {
            FaultRuntimeAdmission();
            npcRosterCensusProtocol?.ReleaseMutationEpochReservation(context.EpochReservation);
            if (ReferenceEquals(activeP12CrimeJusticeMutationContext, context))
                activeP12CrimeJusticeMutationContext = null;
            return false;
        }

        bool notified;
        if (context.ChangedSectionIds.Count == 0)
        {
            try
            {
                npcRosterCensusProtocol.ReleaseMutationEpochReservation(context.EpochReservation);
                notified = true;
            }
            catch { notified = false; }
        }
        else
        {
            notified = npcRosterCensusProtocol.TryNotifyReservedCommittedMutations(
                context.EpochReservation,
                context.ChangedSectionIds,
                out _);
        }
        if (!notified) FaultRuntimeAdmission();
        activeP12CrimeJusticeMutationContext = null;
        return notified;
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
            || activeP12CrimeJusticeMutationContext != null
            || participants == null)
        {
            FaultRuntimeAdmission();
            return false;
        }

        List<NpcRuntime> exactNpcs = new List<NpcRuntime>(participants.Count);
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
            exactNpcs.Add(npc);
            sections.Add(sectionId);
        }

        if (includeJustice && justiceSystem != null)
        {
            if (!ReferenceEquals(p12CrimeJusticeBoundJusticeSystem, justiceSystem))
            {
                FaultRuntimeAdmission();
                return false;
            }
            sections.Add(P12CrimeJusticeCensusProvider.JusticeRecordsSectionId);
            sections.Add(P12CrimeJusticeCensusProvider.JusticeP18ReceiptsSectionId);
        }
        if (includeJustice && crimeSystem != null)
            sections.Add(P12CrimeJusticeCensusProvider.CrimeP18ReceiptsSectionId);

        if (sections.Count == 0
            || new HashSet<string>(sections, StringComparer.Ordinal).Count != sections.Count
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(sections, out _))
        {
            FaultRuntimeAdmission();
            return false;
        }

        P12CrimeJusticeMutationContext context = new P12CrimeJusticeMutationContext(
            P12CrimeJusticeMutationMode.ActionImmediate,
            exactNpcs,
            sections,
            null);
        activeP12CrimeJusticeMutationContext = context;
        scope = new P12CrimeJusticeMutationScope(this, context);
        return true;
    }

    private void ExitP12CrimeJusticeActionScope(P12CrimeJusticeMutationContext context)
    {
        if (context == null) return;
        if (context.Mode != P12CrimeJusticeMutationMode.ActionImmediate
            || !context.IsOwnedByCurrentThread()
            || !ReferenceEquals(activeP12CrimeJusticeMutationContext, context))
            FaultRuntimeAdmission();
        if (ReferenceEquals(activeP12CrimeJusticeMutationContext, context))
            activeP12CrimeJusticeMutationContext = null;
    }

    private bool CanCommitP12NpcCrimeJusticeMutation(NpcRuntime npc)
    {
        if (runtimeAdmissionContext == null) return true;
        string sectionId = GetP12CrimeJusticeNpcSectionId(npc);
        P12CrimeJusticeMutationContext context = activeP12CrimeJusticeMutationContext;
        if (sectionId == null || context == null || !context.IsOwnedByCurrentThread()
            || !context.AllowedNpcs.Contains(npc)
            || !context.AllowedSectionIds.Contains(sectionId))
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (npc.P12CrimeJusticeRevision == long.MaxValue)
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (context.Mode == P12CrimeJusticeMutationMode.DailyReserved)
        {
            return context.EpochReservation != null
                && (context.ChangedSectionIds.Contains(sectionId)
                    || npcRosterCensusProtocol.TryValidateUnchangedSections(new[] { sectionId }, out _));
        }
        if (context.Mode != P12CrimeJusticeMutationMode.ActionImmediate
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(new[] { sectionId }, out _)
            || !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _))
        {
            FaultRuntimeAdmission();
            return false;
        }
        return true;
    }

    private void NotifyP12NpcCrimeJusticeMutation(NpcRuntime npc)
    {
        if (runtimeAdmissionContext == null) return;
        P12CrimeJusticeMutationContext context = activeP12CrimeJusticeMutationContext;
        string sectionId = GetP12CrimeJusticeNpcSectionId(npc);
        if (sectionId == null || context == null || !context.IsOwnedByCurrentThread()
            || !context.AllowedNpcs.Contains(npc) || !context.AllowedSectionIds.Contains(sectionId))
        {
            FaultRuntimeAdmission();
            return;
        }
        if (context.Mode == P12CrimeJusticeMutationMode.DailyReserved)
        {
            context.ChangedSectionIds.Add(sectionId);
            return;
        }
        if (context.Mode != P12CrimeJusticeMutationMode.ActionImmediate
            || !npcRosterCensusProtocol.NotifyCommittedMutation(sectionId, out _))
            FaultRuntimeAdmission();
    }

    private bool CanCommitP12JusticeRecordsMutation(JusticeSystem justice)
    {
        if (runtimeAdmissionContext == null) return true;
        P12CrimeJusticeMutationContext context = activeP12CrimeJusticeMutationContext;
        string sectionId = P12CrimeJusticeCensusProvider.JusticeRecordsSectionId;
        if (!ReferenceEquals(justice, justiceSystem)
            || !ReferenceEquals(p12CrimeJusticeBoundJusticeSystem, justice)
            || context == null || !context.IsOwnedByCurrentThread()
            || !context.AllowedSectionIds.Contains(sectionId))
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (justice.P12CrimeJusticeRevision == long.MaxValue)
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (context.Mode == P12CrimeJusticeMutationMode.DailyReserved)
        {
            return context.EpochReservation != null
                && (context.ChangedSectionIds.Contains(sectionId)
                    || npcRosterCensusProtocol.TryValidateUnchangedSections(new[] { sectionId }, out _));
        }
        if (context.Mode != P12CrimeJusticeMutationMode.ActionImmediate
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(new[] { sectionId }, out _)
            || !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _))
        {
            FaultRuntimeAdmission();
            return false;
        }
        return true;
    }

    private void NotifyP12JusticeRecordsMutation(JusticeSystem justice)
    {
        if (runtimeAdmissionContext == null) return;
        P12CrimeJusticeMutationContext context = activeP12CrimeJusticeMutationContext;
        string sectionId = P12CrimeJusticeCensusProvider.JusticeRecordsSectionId;
        if (!ReferenceEquals(justice, justiceSystem)
            || !ReferenceEquals(p12CrimeJusticeBoundJusticeSystem, justice)
            || context == null || !context.IsOwnedByCurrentThread()
            || !context.AllowedSectionIds.Contains(sectionId))
        {
            FaultRuntimeAdmission();
            return;
        }
        if (context.Mode == P12CrimeJusticeMutationMode.DailyReserved)
        {
            context.ChangedSectionIds.Add(sectionId);
            return;
        }
        if (context.Mode != P12CrimeJusticeMutationMode.ActionImmediate
            || !npcRosterCensusProtocol.NotifyCommittedMutation(sectionId, out _))
            FaultRuntimeAdmission();
    }
}
