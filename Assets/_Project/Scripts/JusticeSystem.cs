using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class JusticeSystem : IAuthoritativeMutationGuardBindable
{
    private const string BeginDayStepId = "justice-begin-day";
    private const string BeginDayOwnerId = "justice";
    private const string BeginDayOperationKind = "justice.begin-day";
    private const string BeginDayOperationVersion = "1";
    private const string BeginDaySnapshotVersion = "justice-begin-day-owner-v1";
    private const string AdvanceSentencesStepId = "justice-advance-sentences";
    private const string AdvanceSentencesOwnerId = "justice";
    private const string AdvanceSentencesOperationKind = "justice.advance-sentences";
    private const string AdvanceSentencesOperationVersion = "1";
    private const string AdvanceSentencesSnapshotVersion = "justice-advance-sentences-owner-v1";
    private const string SyncWantedStatusesStepId = "justice-sync-wanted-statuses";
    private const string SyncWantedStatusesOwnerId = "justice";
    private const string SyncWantedStatusesOperationKind = "justice.sync-wanted-statuses";
    private const string SyncWantedStatusesOperationVersion = "1";
    private const string SyncWantedStatusesSnapshotVersion = "justice-sync-wanted-statuses-owner-v1";
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private bool p12DailyProfileReceiptBoundaryBound;
    private bool p12AReceiptStateInstalled;
    private readonly DailyV1JusticeRecordsOwner justiceRecordsOwner;
    private readonly Dictionary<DailyV1JusticeWantedRecordOwner, WantedRecordRuntime> wantedRecordViews =
        new Dictionary<DailyV1JusticeWantedRecordOwner, WantedRecordRuntime>();
    private readonly Dictionary<DailyV1JusticeSentenceOwner, PrisonSentenceRuntime> sentenceViews =
        new Dictionary<DailyV1JusticeSentenceOwner, PrisonSentenceRuntime>();
    private readonly Dictionary<DailyV1JusticeSentenceOwner, PrisonSentenceRuntime> preparedSelectedSentenceViews =
        new Dictionary<DailyV1JusticeSentenceOwner, PrisonSentenceRuntime>();
    private readonly IList<WantedRecordRuntime> wantedRecords;
    private readonly IList<PrisonSentenceRuntime> prisonSentences;
    private Dictionary<string, JusticeBeginDayReceipt> beginDayStepReceipts =
        new Dictionary<string, JusticeBeginDayReceipt>(StringComparer.Ordinal);
    private long beginDayStepRevision;
    private Dictionary<string, JusticeAdvanceSentencesReceipt> advanceSentencesStepReceipts =
        new Dictionary<string, JusticeAdvanceSentencesReceipt>(StringComparer.Ordinal);
    private long advanceSentencesStepRevision;
    private Dictionary<string, JusticeSyncWantedStatusesReceipt> syncWantedStatusesStepReceipts =
        new Dictionary<string, JusticeSyncWantedStatusesReceipt>(StringComparer.Ordinal);
    private long syncWantedStatusesStepRevision;
    private readonly NpcStatusData freeStatus;
    private readonly NpcStatusData wantedStatus;
    private readonly NpcStatusData arrestedStatus;
    private readonly NpcStatusData hiddenStatus;
    private readonly DomainEventRecorder domainEventRecorder;
    private readonly SimulationLogger logger;

    internal long P12CrimeJusticeRevision => justiceRecordsOwner.Revision;
    internal DailyV1JusticeRecordsOwner CoreOwner => justiceRecordsOwner;
    internal int P12WantedRecordCount => wantedRecords?.Count ?? -1;
    internal int P12PrisonSentenceCount => prisonSentences?.Count ?? -1;
    internal long P12P18ReceiptCensusRevision
    {
        get
        {
            if (beginDayStepReceipts == null || advanceSentencesStepReceipts == null
                || syncWantedStatusesStepReceipts == null
                || beginDayStepRevision < 0L || advanceSentencesStepRevision < 0L
                || syncWantedStatusesStepRevision < 0L)
                throw new InvalidOperationException("Justice P18 receipt state is invalid.");
            return checked(checked(beginDayStepRevision + beginDayStepReceipts.Count)
                + checked(advanceSentencesStepRevision + advanceSentencesStepReceipts.Count)
                + checked(syncWantedStatusesStepRevision + syncWantedStatusesStepReceipts.Count));
        }
    }

    internal bool TryCaptureP12AReceiptOwnerSnapshot(out P12AJusticeReceiptOwnerSnapshot snapshot)
    {
        snapshot = null;
        if (beginDayStepReceipts == null || advanceSentencesStepReceipts == null
            || syncWantedStatusesStepReceipts == null || beginDayStepRevision < 0L
            || advanceSentencesStepRevision < 0L || syncWantedStatusesStepRevision < 0L) return false;
        try
        {
            P12AJusticeReceiptOwnerSnapshot candidate = new P12AJusticeReceiptOwnerSnapshot(
                beginDayStepRevision, SnapshotReceipts(beginDayStepReceipts,
                    receipt => new P12AReceiptValue(receipt.ExecutionStepIdentity, receipt.DescriptorFingerprint,
                        receipt.OwnerRevisionBefore, receipt.OwnerRevisionAfter)),
                advanceSentencesStepRevision, SnapshotReceipts(advanceSentencesStepReceipts,
                    receipt => new P12AReceiptValue(receipt.ExecutionStepIdentity, receipt.DescriptorFingerprint,
                        receipt.OwnerRevisionBefore, receipt.OwnerRevisionAfter)),
                syncWantedStatusesStepRevision, SnapshotReceipts(syncWantedStatusesStepReceipts,
                    receipt => new P12AReceiptValue(receipt.ExecutionStepIdentity, receipt.DescriptorFingerprint,
                        receipt.OwnerRevisionBefore, receipt.OwnerRevisionAfter)));
            if (!candidate.IsValid || candidate.CensusRevision != P12P18ReceiptCensusRevision) return false;
            snapshot = candidate;
            return true;
        }
        catch (OverflowException) { return false; }
    }

    internal bool TryInstallP12AReceiptOwnerSnapshot(P12AJusticeReceiptOwnerSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.IsValid || p12AReceiptStateInstalled
            || beginDayStepReceipts == null || advanceSentencesStepReceipts == null
            || syncWantedStatusesStepReceipts == null || beginDayStepRevision != 0L
            || advanceSentencesStepRevision != 0L || syncWantedStatusesStepRevision != 0L
            || beginDayStepReceipts.Count != 0 || advanceSentencesStepReceipts.Count != 0
            || syncWantedStatusesStepReceipts.Count != 0) return false;
        Dictionary<string, JusticeBeginDayReceipt> begin = new Dictionary<string, JusticeBeginDayReceipt>(StringComparer.Ordinal);
        foreach (P12AReceiptValue value in snapshot.BeginDayReceipts)
        {
            if (begin.ContainsKey(value.Identity)) return false;
            begin.Add(value.Identity, new JusticeBeginDayReceipt(value.Identity, value.DescriptorFingerprint,
                value.RevisionBefore, value.RevisionAfter));
        }
        Dictionary<string, JusticeAdvanceSentencesReceipt> advance =
            new Dictionary<string, JusticeAdvanceSentencesReceipt>(StringComparer.Ordinal);
        foreach (P12AReceiptValue value in snapshot.AdvanceSentencesReceipts)
        {
            if (advance.ContainsKey(value.Identity)) return false;
            advance.Add(value.Identity, new JusticeAdvanceSentencesReceipt(value.Identity, value.DescriptorFingerprint,
                value.RevisionBefore, value.RevisionAfter));
        }
        Dictionary<string, JusticeSyncWantedStatusesReceipt> sync =
            new Dictionary<string, JusticeSyncWantedStatusesReceipt>(StringComparer.Ordinal);
        foreach (P12AReceiptValue value in snapshot.SyncWantedStatusesReceipts)
        {
            if (sync.ContainsKey(value.Identity)) return false;
            sync.Add(value.Identity, new JusticeSyncWantedStatusesReceipt(value.Identity, value.DescriptorFingerprint,
                value.RevisionBefore, value.RevisionAfter));
        }
        beginDayStepReceipts = begin;
        advanceSentencesStepReceipts = advance;
        syncWantedStatusesStepReceipts = sync;
        beginDayStepRevision = snapshot.BeginDayRevision;
        advanceSentencesStepRevision = snapshot.AdvanceSentencesRevision;
        syncWantedStatusesStepRevision = snapshot.SyncWantedStatusesRevision;
        p12AReceiptStateInstalled = true;
        return P12P18ReceiptCensusRevision == snapshot.CensusRevision;
    }

    public JusticeSystem(
        NpcStatusData freeStatus,
        NpcStatusData wantedStatus,
        NpcStatusData arrestedStatus,
        NpcStatusData hiddenStatus,
        SimulationLogger logger = null)
        : this(freeStatus, wantedStatus, arrestedStatus, hiddenStatus, null, logger)
    {
    }

    public JusticeSystem(
        NpcStatusData freeStatus,
        NpcStatusData wantedStatus,
        NpcStatusData arrestedStatus,
        NpcStatusData hiddenStatus,
        DomainEventRecorder domainEventRecorder,
        SimulationLogger logger)
        : this(freeStatus, wantedStatus, arrestedStatus, hiddenStatus,
            domainEventRecorder, logger, null)
    {
    }

    internal JusticeSystem(
        NpcStatusData freeStatus,
        NpcStatusData wantedStatus,
        NpcStatusData arrestedStatus,
        NpcStatusData hiddenStatus,
        DomainEventRecorder domainEventRecorder,
        SimulationLogger logger,
        DailyV1JusticeRecordsOwner selectedOwner)
    {
        this.freeStatus = freeStatus;
        this.wantedStatus = wantedStatus;
        this.arrestedStatus = arrestedStatus;
        this.hiddenStatus = hiddenStatus;
        this.domainEventRecorder = domainEventRecorder;
        this.logger = logger ?? UnitySimulationLoggerAdapter.Create(null);
        justiceRecordsOwner = selectedOwner ?? new DailyV1JusticeRecordsOwner();
        wantedRecords = new JusticeOwnerProjectionList<DailyV1JusticeWantedRecordOwner, WantedRecordRuntime>(
            () => justiceRecordsOwner.WantedRecords,
            ProjectWantedRecord,
            view => view?.CoreOwner,
            AddWantedRecordView,
            null);
        prisonSentences = new JusticeOwnerProjectionList<DailyV1JusticeSentenceOwner, PrisonSentenceRuntime>(
            () => justiceRecordsOwner.Sentences,
            ProjectSentence,
            view => view?.CoreOwner,
            AddSentenceView,
            index =>
            {
                DailyV1JusticeSentenceOwner row = justiceRecordsOwner.Sentences[index];
                if (!justiceRecordsOwner.TryRemoveSentenceAt(index))
                    throw new InvalidOperationException("The selected Justice sentence row could not be removed.");
                sentenceViews.Remove(row);
            });
    }

    private WantedRecordRuntime ProjectWantedRecord(DailyV1JusticeWantedRecordOwner row)
    {
        if (row == null || !wantedRecordViews.TryGetValue(row, out WantedRecordRuntime view))
            throw new InvalidOperationException("A Core Justice wanted row has no exact Unity reference projection.");
        return view;
    }

    private PrisonSentenceRuntime ProjectSentence(DailyV1JusticeSentenceOwner row)
    {
        if (row == null || !sentenceViews.TryGetValue(row, out PrisonSentenceRuntime view))
            throw new InvalidOperationException("A Core Justice sentence row has no exact Unity reference projection.");
        return view;
    }

    private void AddWantedRecordView(WantedRecordRuntime view)
    {
        DailyV1JusticeWantedRecordOwner row = view?.CoreOwner;
        if (row == null || (wantedRecordViews.TryGetValue(row, out WantedRecordRuntime existing)
                && !ReferenceEquals(existing, view))
            || !justiceRecordsOwner.TryAddWantedRecord(row))
            throw new InvalidOperationException("The Core Justice wanted-record owner rejected its exact row projection.");
        wantedRecordViews[row] = view;
    }

    /// <summary>
    /// Binds the Unity compatibility view for a wanted row already committed
    /// by Simulation.Core. This method never inserts or mutates Core owner
    /// state; the exact row must already belong to this Justice authority.
    /// </summary>
    internal bool TryProjectSelectedCoreWantedRecord(
        DailyV1JusticeWantedRecordOwner row,
        NpcRuntime target,
        CityRuntime city)
    {
        if (row == null || target == null || city == null
            || !ReferenceEquals(justiceRecordsOwner,
                target.SelectedRuntimeOwnerGraph?.JusticeRecordsOwner)
            || !justiceRecordsOwner.ContainsWantedRecord(row)
            || !string.Equals(row.TargetRuntimeId, target.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(row.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal))
            return false;

        if (wantedRecordViews.TryGetValue(row, out WantedRecordRuntime existing))
            return existing != null
                && ReferenceEquals(existing.CoreOwner, row)
                && ReferenceEquals(existing.Target, target)
                && ReferenceEquals(existing.City, city);

        if (!WantedRecordRuntime.TryCreateFromCoreOwner(target, city, row,
                out WantedRecordRuntime view))
            return false;
        wantedRecordViews.Add(row, view);
        return true;
    }

    /// <summary>
    /// Prepares the Unity reference view for a new Core-owned wanted row before
    /// its revision is published to P12. This keeps the census coherent at the
    /// Core commit callback while the Core owner remains authoritative.
    /// </summary>
    internal bool TryPrepareSelectedCoreWantedRecordProjection(
        DailyV1JusticeWantedRecordOwner row,
        NpcRuntime target,
        CityRuntime city)
    {
        if (row == null || target == null || city == null
            || !ReferenceEquals(justiceRecordsOwner,
                target.SelectedRuntimeOwnerGraph?.JusticeRecordsOwner)
            || justiceRecordsOwner.ContainsWantedRecord(row)
            || !string.Equals(row.TargetRuntimeId, target.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(row.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal))
            return false;

        if (wantedRecordViews.TryGetValue(row, out WantedRecordRuntime existing))
            return existing != null
                && ReferenceEquals(existing.CoreOwner, row)
                && ReferenceEquals(existing.Target, target)
                && ReferenceEquals(existing.City, city);

        if (!WantedRecordRuntime.TryCreateFromCoreOwner(target, city, row,
                out WantedRecordRuntime view))
            return false;
        wantedRecordViews.Add(row, view);
        return true;
    }

    internal void DiscardPreparedSelectedCoreWantedRecordProjection(
        DailyV1JusticeWantedRecordOwner row)
    {
        if (row != null && !justiceRecordsOwner.ContainsWantedRecord(row))
            wantedRecordViews.Remove(row);
    }

    private void AddSentenceView(PrisonSentenceRuntime view)
    {
        DailyV1JusticeSentenceOwner row = view?.CoreOwner;
        if (row == null || (sentenceViews.TryGetValue(row, out PrisonSentenceRuntime existing)
                && !ReferenceEquals(existing, view))
            || !justiceRecordsOwner.TryAddSentence(row))
            throw new InvalidOperationException("The Core Justice sentence owner rejected its exact row projection.");
        sentenceViews[row] = view;
    }

    public void BeginDay()
    {
        ThrowIfFaulted();

        foreach (PrisonSentenceRuntime sentence in prisonSentences)
        {
            if (sentence != null)
            {
                sentence.ClearArrestedToday();
            }
        }
    }

    /// <summary>Captures the justice-owned daily reset as a frozen P18 boundary step.</summary>
    public bool TryCreateBeginDayStep(
        DailyBoundaryOperation operation,
        int ordinal,
        out BoundaryContinuationStep step,
        out TimelineFailure failure)
    {
        step = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || ordinal < 0 || !mutationGuardBinding.CanMutate
            || beginDayStepRevision == long.MaxValue
            || !TryCaptureBeginDaySnapshot(out _, out string ownerRevision))
        {
            return false;
        }

        step = new BoundaryContinuationStep(
            ordinal,
            BeginDayStepId,
            BeginDayOwnerId,
            BeginDayOperationKind,
            BeginDayOperationVersion,
            ownerRevision,
            string.Empty);
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Looks up an exact P18 boundary-step receipt without reading mutable sentence state.</summary>
    public bool TryResolveBeginDayReceipt(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out JusticeBeginDayReceipt receipt,
        out TimelineFailure failure)
    {
        receipt = null;
        if (!TryGetBeginDayIdentity(manifest, step, out string identity, out string fingerprint))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (beginDayStepReceipts == null)
        {
            beginDayStepReceipts = new Dictionary<string, JusticeBeginDayReceipt>(StringComparer.Ordinal);
        }

        if (!beginDayStepReceipts.TryGetValue(identity, out JusticeBeginDayReceipt existing))
        {
            failure = TimelineFailure.None;
            return false;
        }

        if (!string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        receipt = existing;
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Stages the justice-owned reset and occurrence receipt as one boundary-step commit.</summary>
    public bool TryPrepareBeginDayStep(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (!TryGetBeginDayIdentity(manifest, step, out string identity, out string fingerprint))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (beginDayStepReceipts == null)
        {
            beginDayStepReceipts = new Dictionary<string, JusticeBeginDayReceipt>(StringComparer.Ordinal);
        }

        if (beginDayStepReceipts.TryGetValue(identity, out JusticeBeginDayReceipt existing))
        {
            if (!string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }

            prepared = new JusticeBeginDayCommit(
                this,
                existing,
                Array.Empty<JusticeBeginDaySentenceSnapshot>(),
                true,
                fingerprint,
                step.OwnerRevision,
                beginDayStepRevision,
                beginDayStepReceipts,
                beginDayStepReceipts);
            failure = TimelineFailure.None;
            return true;
        }

        if (!mutationGuardBinding.CanMutate || beginDayStepRevision == long.MaxValue
            || !TryCaptureBeginDaySnapshot(
                out List<JusticeBeginDaySentenceSnapshot> snapshots,
                out string currentOwnerRevision)
            || !string.Equals(currentOwnerRevision, step.OwnerRevision, StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        JusticeBeginDayReceipt receipt = new JusticeBeginDayReceipt(
            identity,
            fingerprint,
            beginDayStepRevision,
            beginDayStepRevision + 1L);
        Dictionary<string, JusticeBeginDayReceipt> nextReceipts =
            new Dictionary<string, JusticeBeginDayReceipt>(beginDayStepReceipts, StringComparer.Ordinal)
            {
                [identity] = receipt
            };
        prepared = new JusticeBeginDayCommit(
            this,
            receipt,
            snapshots,
            false,
            fingerprint,
            step.OwnerRevision,
            beginDayStepRevision,
            beginDayStepReceipts,
            nextReceipts);
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Captures the exact roster and Justice target topology for sentence advancement.</summary>
    public bool TryCreateAdvanceSentencesStep(
        DailyBoundaryOperation operation,
        List<NpcRuntime> npcRuntimeList,
        int ordinal,
        out BoundaryContinuationStep step,
        out TimelineFailure failure)
    {
        step = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || ordinal < 0
            || !TryCaptureAdvanceSentencesSnapshot(npcRuntimeList,
                out _, out string ownerRevision))
        {
            return false;
        }

        step = new BoundaryContinuationStep(
            ordinal,
            AdvanceSentencesStepId,
            AdvanceSentencesOwnerId,
            AdvanceSentencesOperationKind,
            AdvanceSentencesOperationVersion,
            ownerRevision,
            string.Empty);
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Looks up an exact sentence-advance receipt without reading live Justice state.</summary>
    public bool TryResolveAdvanceSentencesReceipt(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out JusticeAdvanceSentencesReceipt receipt,
        out TimelineFailure failure)
    {
        receipt = null;
        if (!TryGetAdvanceSentencesIdentity(manifest, step, out string identity, out string fingerprint))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (advanceSentencesStepReceipts == null)
        {
            advanceSentencesStepReceipts =
                new Dictionary<string, JusticeAdvanceSentencesReceipt>(StringComparer.Ordinal);
        }

        if (!advanceSentencesStepReceipts.TryGetValue(identity, out JusticeAdvanceSentencesReceipt existing))
        {
            failure = TimelineFailure.None;
            return false;
        }

        if (!string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        receipt = existing;
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Stages legacy sentence advancement and its owner-local occurrence receipt.</summary>
    public bool TryPrepareAdvanceSentencesStep(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        List<NpcRuntime> npcRuntimeList,
        out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (!TryGetAdvanceSentencesIdentity(manifest, step, out string identity, out string fingerprint))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (advanceSentencesStepReceipts == null)
        {
            advanceSentencesStepReceipts =
                new Dictionary<string, JusticeAdvanceSentencesReceipt>(StringComparer.Ordinal);
        }

        if (advanceSentencesStepReceipts.TryGetValue(identity, out JusticeAdvanceSentencesReceipt existing))
        {
            if (!string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }

            prepared = new JusticeAdvanceSentencesCommit(
                this,
                existing,
                npcRuntimeList,
                null,
                null,
                true,
                fingerprint,
                step.OwnerRevision,
                advanceSentencesStepRevision,
                advanceSentencesStepReceipts,
                advanceSentencesStepReceipts);
            failure = TimelineFailure.None;
            return true;
        }

        if (!mutationGuardBinding.CanMutate || advanceSentencesStepRevision == long.MaxValue
            || !string.IsNullOrEmpty(step.Payload) || !string.IsNullOrEmpty(step.PersonId)
            || !TryCaptureAdvanceSentencesSnapshot(npcRuntimeList,
                out JusticeAdvanceSentencesSnapshot snapshot,
                out string currentOwnerRevision)
            || !string.Equals(currentOwnerRevision, step.OwnerRevision, StringComparison.Ordinal)
            || !TryReserveAdvanceSentencesStatusCapacity(snapshot))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        JusticeAdvanceSentencesReceipt receipt = new JusticeAdvanceSentencesReceipt(
            identity,
            fingerprint,
            advanceSentencesStepRevision,
            advanceSentencesStepRevision + 1L);
        Dictionary<string, JusticeAdvanceSentencesReceipt> nextReceipts =
            new Dictionary<string, JusticeAdvanceSentencesReceipt>(
                advanceSentencesStepReceipts, StringComparer.Ordinal)
            {
                [identity] = receipt
            };
        int diagnosticCapacity;
        if (!TryGetAdvanceSentencesDiagnosticCapacity(snapshot, out diagnosticCapacity))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        List<JusticeAdvanceSentencesLogNotice> notices =
            new List<JusticeAdvanceSentencesLogNotice>(diagnosticCapacity);
        prepared = new JusticeAdvanceSentencesCommit(
            this,
            receipt,
            npcRuntimeList,
            snapshot,
            notices,
            false,
            fingerprint,
            step.OwnerRevision,
            advanceSentencesStepRevision,
            advanceSentencesStepReceipts,
            nextReceipts);
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Captures the topology for the distinct legacy wanted-status roster pass.</summary>
    public bool TryCreateSyncWantedStatusesStep(
        DailyBoundaryOperation operation,
        List<NpcRuntime> npcRuntimeList,
        int ordinal,
        out BoundaryContinuationStep step,
        out TimelineFailure failure)
    {
        step = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || ordinal < 0
            || !TryCaptureSyncWantedStatusesSnapshot(npcRuntimeList,
                out _, out string ownerRevision, includeMutableValues: false))
        {
            return false;
        }

        step = new BoundaryContinuationStep(
            ordinal,
            SyncWantedStatusesStepId,
            SyncWantedStatusesOwnerId,
            SyncWantedStatusesOperationKind,
            SyncWantedStatusesOperationVersion,
            ownerRevision,
            string.Empty);
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Looks up an exact later wanted-status pass receipt.</summary>
    public bool TryResolveSyncWantedStatusesReceipt(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out JusticeSyncWantedStatusesReceipt receipt,
        out TimelineFailure failure)
    {
        receipt = null;
        if (!TryGetSyncWantedStatusesIdentity(manifest, step, out string identity, out string fingerprint))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (syncWantedStatusesStepReceipts == null)
        {
            syncWantedStatusesStepReceipts = new Dictionary<string, JusticeSyncWantedStatusesReceipt>(StringComparer.Ordinal);
        }

        if (!syncWantedStatusesStepReceipts.TryGetValue(identity, out JusticeSyncWantedStatusesReceipt existing))
        {
            failure = TimelineFailure.None;
            return false;
        }

        if (!string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        receipt = existing;
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Stages the separate wanted-status pass using live values after predecessor effects.</summary>
    public bool TryPrepareSyncWantedStatusesStep(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        List<NpcRuntime> npcRuntimeList,
        out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (!TryGetSyncWantedStatusesIdentity(manifest, step, out string identity, out string fingerprint))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (syncWantedStatusesStepReceipts == null)
        {
            syncWantedStatusesStepReceipts = new Dictionary<string, JusticeSyncWantedStatusesReceipt>(StringComparer.Ordinal);
        }

        if (syncWantedStatusesStepReceipts.TryGetValue(identity, out JusticeSyncWantedStatusesReceipt existing))
        {
            if (!string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }

            prepared = new JusticeSyncWantedStatusesCommit(
                this, existing, npcRuntimeList, null, true, fingerprint, step.OwnerRevision,
                syncWantedStatusesStepRevision, syncWantedStatusesStepReceipts, syncWantedStatusesStepReceipts);
            failure = TimelineFailure.None;
            return true;
        }

        if (!mutationGuardBinding.CanMutate || syncWantedStatusesStepRevision == long.MaxValue
            || !string.IsNullOrEmpty(step.Payload) || !string.IsNullOrEmpty(step.PersonId)
            || !TryCaptureSyncWantedStatusesSnapshot(npcRuntimeList,
                out JusticeSyncWantedStatusesSnapshot snapshot,
                out string currentOwnerRevision, includeMutableValues: true)
            || !string.Equals(currentOwnerRevision, step.OwnerRevision, StringComparison.Ordinal)
            || !TryReserveSyncWantedStatusCapacity(snapshot))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        JusticeSyncWantedStatusesReceipt receipt = new JusticeSyncWantedStatusesReceipt(
            identity, fingerprint, syncWantedStatusesStepRevision, syncWantedStatusesStepRevision + 1L);
        Dictionary<string, JusticeSyncWantedStatusesReceipt> nextReceipts =
            new Dictionary<string, JusticeSyncWantedStatusesReceipt>(syncWantedStatusesStepReceipts, StringComparer.Ordinal)
            {
                [identity] = receipt
            };
        prepared = new JusticeSyncWantedStatusesCommit(
            this, receipt, npcRuntimeList, snapshot, false, fingerprint, step.OwnerRevision,
            syncWantedStatusesStepRevision, syncWantedStatusesStepReceipts, nextReceipts);
        failure = TimelineFailure.None;
        return true;
    }

    internal bool TryCommitSyncWantedStatusesStep(
        JusticeSyncWantedStatusesReceipt receipt,
        List<NpcRuntime> npcRuntimeList,
        JusticeSyncWantedStatusesSnapshot expectedSnapshot,
        bool replay,
        string fingerprint,
        string expectedOwnerRevision,
        long expectedRevision,
        Dictionary<string, JusticeSyncWantedStatusesReceipt> expectedReceipts,
        Dictionary<string, JusticeSyncWantedStatusesReceipt> nextReceipts,
        out TimelineFailure failure)
    {
        failure = TimelineFailure.ContinuationFailed;
        if (p12DailyProfileReceiptBoundaryBound)
        {
            failure = TimelineFailure.UnsupportedProfile;
            return false;
        }
        if (receipt == null)
        {
            return false;
        }

        if (replay)
        {
            if (syncWantedStatusesStepReceipts != null
                && syncWantedStatusesStepReceipts.TryGetValue(receipt.ExecutionStepIdentity, out JusticeSyncWantedStatusesReceipt existing)
                && ReferenceEquals(existing, receipt)
                && string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.None;
                return true;
            }

            return false;
        }

        if (npcRuntimeList == null || expectedSnapshot == null || !mutationGuardBinding.CanMutate
            || syncWantedStatusesStepRevision != expectedRevision
            || !ReferenceEquals(syncWantedStatusesStepReceipts, expectedReceipts)
            || nextReceipts == null || !nextReceipts.ContainsKey(receipt.ExecutionStepIdentity)
            || syncWantedStatusesStepReceipts.ContainsKey(receipt.ExecutionStepIdentity)
            || syncWantedStatusesStepRevision == long.MaxValue
            || receipt.OwnerRevisionBefore != syncWantedStatusesStepRevision
            || receipt.OwnerRevisionAfter != syncWantedStatusesStepRevision + 1L
            || !string.Equals(receipt.DescriptorFingerprint, fingerprint, StringComparison.Ordinal)
            || !TryCaptureSyncWantedStatusesSnapshot(npcRuntimeList,
                out JusticeSyncWantedStatusesSnapshot currentSnapshot,
                out string currentOwnerRevision, includeMutableValues: true)
            || !string.Equals(currentOwnerRevision, expectedOwnerRevision, StringComparison.Ordinal)
            || !SameSyncWantedStatusesSnapshot(expectedSnapshot, currentSnapshot)
            || !TryReserveSyncWantedStatusCapacity(currentSnapshot))
        {
            return false;
        }

        foreach (JusticeSyncWantedRosterSnapshot entry in currentSnapshot.Roster)
        {
            if (entry.Npc == null || wantedStatus == null)
            {
                continue;
            }

            if (entry.HasActiveWarrant)
            {
                entry.Npc.AddStatus(wantedStatus);
            }
            else
            {
                entry.Npc.RemoveStatus(wantedStatus);
            }
        }

        syncWantedStatusesStepRevision = receipt.OwnerRevisionAfter;
        syncWantedStatusesStepReceipts = nextReceipts;
        failure = TimelineFailure.None;
        return true;
    }

    internal bool TryCommitBeginDayStep(
        JusticeBeginDayReceipt receipt,
        IReadOnlyList<JusticeBeginDaySentenceSnapshot> expectedSentences,
        bool replay,
        string fingerprint,
        string expectedOwnerRevision,
        long expectedBeginDayRevision,
        Dictionary<string, JusticeBeginDayReceipt> expectedReceipts,
        Dictionary<string, JusticeBeginDayReceipt> nextReceipts,
        out TimelineFailure failure)
    {
        failure = TimelineFailure.ContinuationFailed;
        if (p12DailyProfileReceiptBoundaryBound)
        {
            failure = TimelineFailure.UnsupportedProfile;
            return false;
        }
        if (receipt == null || expectedSentences == null)
        {
            return false;
        }

        if (replay)
        {
            if (beginDayStepReceipts != null
                && beginDayStepReceipts.TryGetValue(receipt.ExecutionStepIdentity, out JusticeBeginDayReceipt existing)
                && ReferenceEquals(existing, receipt)
                && string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.None;
                return true;
            }

            return false;
        }

        if (!mutationGuardBinding.CanMutate || beginDayStepRevision != expectedBeginDayRevision
            || !ReferenceEquals(beginDayStepReceipts, expectedReceipts)
            || nextReceipts == null || !nextReceipts.ContainsKey(receipt.ExecutionStepIdentity)
            || beginDayStepReceipts.ContainsKey(receipt.ExecutionStepIdentity)
            || beginDayStepRevision == long.MaxValue
            || receipt.OwnerRevisionBefore != beginDayStepRevision
            || receipt.OwnerRevisionAfter != beginDayStepRevision + 1L
            || !string.Equals(receipt.DescriptorFingerprint, fingerprint, StringComparison.Ordinal)
            || !TryCaptureBeginDaySnapshot(out List<JusticeBeginDaySentenceSnapshot> currentSentences,
                out string currentOwnerRevision)
            || !string.Equals(currentOwnerRevision, expectedOwnerRevision, StringComparison.Ordinal)
            || !SameSentenceReferences(expectedSentences, currentSentences))
        {
            return false;
        }

        foreach (JusticeBeginDaySentenceSnapshot snapshot in expectedSentences)
        {
            if (snapshot?.Sentence != null && snapshot.WasArrestedToday)
            {
                snapshot.Sentence.ClearArrestedToday();
            }
        }

        beginDayStepRevision = receipt.OwnerRevisionAfter;
        beginDayStepReceipts = nextReceipts;
        failure = TimelineFailure.None;
        return true;
    }

    internal bool TryCommitAdvanceSentencesStep(
        JusticeAdvanceSentencesReceipt receipt,
        List<NpcRuntime> npcRuntimeList,
        JusticeAdvanceSentencesSnapshot expectedSnapshot,
        List<JusticeAdvanceSentencesLogNotice> notices,
        bool replay,
        string fingerprint,
        string expectedOwnerRevision,
        long expectedRevision,
        Dictionary<string, JusticeAdvanceSentencesReceipt> expectedReceipts,
        Dictionary<string, JusticeAdvanceSentencesReceipt> nextReceipts,
        out TimelineFailure failure)
    {
        failure = TimelineFailure.ContinuationFailed;
        if (p12DailyProfileReceiptBoundaryBound)
        {
            failure = TimelineFailure.UnsupportedProfile;
            return false;
        }
        if (receipt == null)
        {
            return false;
        }

        if (replay)
        {
            if (advanceSentencesStepReceipts != null
                && advanceSentencesStepReceipts.TryGetValue(receipt.ExecutionStepIdentity,
                    out JusticeAdvanceSentencesReceipt existing)
                && ReferenceEquals(existing, receipt)
                && string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.None;
                return true;
            }

            return false;
        }

        if (npcRuntimeList == null || expectedSnapshot == null || notices == null
            || !mutationGuardBinding.CanMutate || advanceSentencesStepRevision != expectedRevision
            || !ReferenceEquals(advanceSentencesStepReceipts, expectedReceipts)
            || nextReceipts == null || !nextReceipts.ContainsKey(receipt.ExecutionStepIdentity)
            || advanceSentencesStepReceipts.ContainsKey(receipt.ExecutionStepIdentity)
            || advanceSentencesStepRevision == long.MaxValue
            || receipt.OwnerRevisionBefore != advanceSentencesStepRevision
            || receipt.OwnerRevisionAfter != advanceSentencesStepRevision + 1L
            || !string.Equals(receipt.DescriptorFingerprint, fingerprint, StringComparison.Ordinal)
            || !TryCaptureAdvanceSentencesSnapshot(npcRuntimeList,
                out JusticeAdvanceSentencesSnapshot currentSnapshot,
                out string currentOwnerRevision)
            || !string.Equals(currentOwnerRevision, expectedOwnerRevision, StringComparison.Ordinal)
            || !SameAdvanceSentencesSnapshot(expectedSnapshot, currentSnapshot)
            || !TryReserveAdvanceSentencesStatusCapacity(currentSnapshot))
        {
            return false;
        }

        if (!TryCaptureAdvanceSentencesLogCandidates(
            currentSnapshot,
            out List<JusticeAdvanceSentencesSentenceLogCandidates> sentenceLogCandidates,
            out List<JusticeAdvanceSentencesLogNotice> rosterLogCandidates))
        {
            return false;
        }

        ApplyAdvanceSentences(npcRuntimeList, notices, sentenceLogCandidates, rosterLogCandidates);
        advanceSentencesStepRevision = receipt.OwnerRevisionAfter;
        advanceSentencesStepReceipts = nextReceipts;
        failure = TimelineFailure.None;

        // Diagnostics follow the durable owner receipt so an uncertain caller retry cannot replay effects.
        EmitAdvanceSentencesLogNotices(notices);
        return true;
    }

    private bool TryCaptureSyncWantedStatusesSnapshot(
        List<NpcRuntime> npcRuntimeList,
        out JusticeSyncWantedStatusesSnapshot snapshot,
        out string ownerRevision,
        bool includeMutableValues)
    {
        snapshot = null;
        ownerRevision = null;
        if (npcRuntimeList == null || !mutationGuardBinding.CanMutate || syncWantedStatusesStepRevision == long.MaxValue)
        {
            return false;
        }

        List<JusticeSyncWantedRosterSnapshot> roster = new List<JusticeSyncWantedRosterSnapshot>(npcRuntimeList.Count);
        List<JusticeSyncWantedRecordSnapshot> records = new List<JusticeSyncWantedRecordSnapshot>(wantedRecords.Count);
        List<string> parts = new List<string>(4 + npcRuntimeList.Count * 4 + wantedRecords.Count * 4)
        {
            SyncWantedStatusesSnapshotVersion,
            SyncWantedStatusesOperationVersion,
            syncWantedStatusesStepRevision.ToString(CultureInfo.InvariantCulture),
            WantedStatusIdentity,
            npcRuntimeList.Count.ToString(CultureInfo.InvariantCulture)
        };
        Dictionary<string, NpcRuntime> runtimeIds = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);

        for (int i = 0; i < npcRuntimeList.Count; i++)
        {
            NpcRuntime npc = npcRuntimeList[i];
            parts.Add("roster");
            parts.Add(i.ToString(CultureInfo.InvariantCulture));
            if (npc == null)
            {
                roster.Add(new JusticeSyncWantedRosterSnapshot(null, string.Empty, string.Empty, false, 0));
                parts.Add("null-slot");
                continue;
            }

            string runtimeId = npc.RuntimeId;
            if (!TryRegisterAdvanceSentencesNpcRuntime(runtimeIds, npc))
            {
                return false;
            }

            string personId = npc.PersonId?.Value ?? string.Empty;
            roster.Add(new JusticeSyncWantedRosterSnapshot(
                npc, runtimeId, personId,
                includeMutableValues && HasAnyActiveWarrant(npc),
                includeMutableValues ? CountAdvanceSentencesStatus(npc, wantedStatus) : 0));
            parts.Add(runtimeId);
            parts.Add(personId);
        }

        parts.Add(wantedRecords.Count.ToString(CultureInfo.InvariantCulture));
        for (int i = 0; i < wantedRecords.Count; i++)
        {
            WantedRecordRuntime record = wantedRecords[i];
            parts.Add("wanted-record");
            parts.Add(i.ToString(CultureInfo.InvariantCulture));
            if (record == null)
            {
                records.Add(new JusticeSyncWantedRecordSnapshot(null, null, string.Empty, string.Empty, false));
                parts.Add("null-slot");
                continue;
            }

            NpcRuntime target = record.Target;
            if (!TryRegisterAdvanceSentencesNpcRuntime(runtimeIds, target))
            {
                return false;
            }

            string runtimeId = target?.RuntimeId ?? string.Empty;
            string personId = target?.PersonId?.Value ?? string.Empty;
            records.Add(new JusticeSyncWantedRecordSnapshot(
                record, target, runtimeId, personId, includeMutableValues && record.IsActive));
            parts.Add(runtimeId);
            parts.Add(personId);
        }

        ownerRevision = SpatialStableKey.Encode(parts.ToArray());
        snapshot = new JusticeSyncWantedStatusesSnapshot(roster, records);
        return true;
    }

    private bool TryReserveSyncWantedStatusCapacity(JusticeSyncWantedStatusesSnapshot snapshot)
    {
        if (snapshot == null || wantedStatus == null)
        {
            return snapshot != null;
        }

        Dictionary<NpcRuntime, int> additions = new Dictionary<NpcRuntime, int>();
        foreach (JusticeSyncWantedRosterSnapshot entry in snapshot.Roster)
        {
            if (entry?.Npc == null || !entry.HasActiveWarrant)
            {
                continue;
            }

            additions.TryGetValue(entry.Npc, out int count);
            if (count == int.MaxValue)
            {
                return false;
            }
            additions[entry.Npc] = count + 1;
        }

        foreach (KeyValuePair<NpcRuntime, int> item in additions)
        {
            IReadOnlyList<NpcStatusData> statuses = item.Key.CurrentStatus;
            if (item.Value > int.MaxValue - statuses.Count)
            {
                return false;
            }
            if (!item.Key.TryReserveCurrentStatusCapacity(item.Value)) return false;
        }

        return true;
    }

    private static bool SameSyncWantedStatusesSnapshot(
        JusticeSyncWantedStatusesSnapshot expected,
        JusticeSyncWantedStatusesSnapshot actual)
    {
        if (expected == null || actual == null
            || expected.Roster.Count != actual.Roster.Count
            || expected.Records.Count != actual.Records.Count)
        {
            return false;
        }

        for (int i = 0; i < expected.Roster.Count; i++)
        {
            JusticeSyncWantedRosterSnapshot left = expected.Roster[i];
            JusticeSyncWantedRosterSnapshot right = actual.Roster[i];
            if (left == null || right == null
                || !ReferenceEquals(left.Npc, right.Npc)
                || !string.Equals(left.RuntimeId, right.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(left.PersonId, right.PersonId, StringComparison.Ordinal)
                || left.HasActiveWarrant != right.HasActiveWarrant
                || left.WantedMarkerCount != right.WantedMarkerCount)
            {
                return false;
            }
        }

        for (int i = 0; i < expected.Records.Count; i++)
        {
            JusticeSyncWantedRecordSnapshot left = expected.Records[i];
            JusticeSyncWantedRecordSnapshot right = actual.Records[i];
            if (left == null || right == null
                || !ReferenceEquals(left.Record, right.Record)
                || !ReferenceEquals(left.Target, right.Target)
                || !string.Equals(left.TargetRuntimeId, right.TargetRuntimeId, StringComparison.Ordinal)
                || !string.Equals(left.TargetPersonId, right.TargetPersonId, StringComparison.Ordinal)
                || left.IsActive != right.IsActive)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryGetSyncWantedStatusesIdentity(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out string identity,
        out string fingerprint)
    {
        identity = null;
        fingerprint = null;
        if (manifest == null || step == null || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step)
            || step.StepId != SyncWantedStatusesStepId || step.OwnerId != SyncWantedStatusesOwnerId
            || step.OperationKind != SyncWantedStatusesOperationKind
            || step.OperationVersion != SyncWantedStatusesOperationVersion
            || step.Disposition != "included")
        {
            return false;
        }

        string expectedBoundaryOccurrenceId = SpatialStableKey.Encode(
            manifest.WorldId, manifest.ProfileId, manifest.AbsoluteDay.ToString(CultureInfo.InvariantCulture));
        if (!string.Equals(manifest.BoundaryOccurrenceId, expectedBoundaryOccurrenceId, StringComparison.Ordinal))
        {
            return false;
        }

        identity = SpatialStableKey.Encode(manifest.BoundaryOccurrenceId, step.StepId);
        fingerprint = SpatialStableKey.Encode(
            manifest.BoundaryOccurrenceId, manifest.ContinuationId, manifest.SubphaseKind,
            manifest.SubphaseVersion, manifest.ConfigurationIdentity, manifest.ContentIdentity,
            step.Ordinal.ToString(CultureInfo.InvariantCulture), step.StepId, step.OwnerId,
            step.OperationKind, step.OperationVersion, step.OwnerRevision, step.Payload,
            step.PersonId, step.Disposition);
        return true;
    }

    private bool TryCaptureBeginDaySnapshot(
        out List<JusticeBeginDaySentenceSnapshot> snapshots,
        out string ownerRevision)
    {
        snapshots = null;
        ownerRevision = null;
        if (!mutationGuardBinding.CanMutate || beginDayStepRevision == long.MaxValue)
        {
            return false;
        }

        snapshots = new List<JusticeBeginDaySentenceSnapshot>(prisonSentences.Count);
        List<string> entries = new List<string>(prisonSentences.Count);
        foreach (PrisonSentenceRuntime sentence in prisonSentences)
        {
            if (sentence == null)
            {
                snapshots.Add(new JusticeBeginDaySentenceSnapshot(null, false));
                entries.Add(SpatialStableKey.Encode("null-sentence"));
                continue;
            }

            bool wasArrestedToday = sentence.WasArrestedToday;
            string identity = SpatialStableKey.Encode(
                sentence.Target?.RuntimeId,
                sentence.City?.RuntimeId,
                sentence.Warrant?.Target?.RuntimeId,
                sentence.Warrant?.City?.RuntimeId);
            string entry = SpatialStableKey.Encode(identity, wasArrestedToday ? "1" : "0");
            snapshots.Add(new JusticeBeginDaySentenceSnapshot(sentence, wasArrestedToday));
            entries.Add(entry);
        }

        entries.Sort(StringComparer.Ordinal);
        List<string> parts = new List<string>(3 + entries.Count)
        {
            BeginDaySnapshotVersion,
            beginDayStepRevision.ToString(CultureInfo.InvariantCulture),
            entries.Count.ToString(CultureInfo.InvariantCulture)
        };
        parts.AddRange(entries);
        ownerRevision = SpatialStableKey.Encode(parts.ToArray());
        return true;
    }

    private bool TryCaptureAdvanceSentencesSnapshot(
        List<NpcRuntime> npcRuntimeList,
        out JusticeAdvanceSentencesSnapshot snapshot,
        out string ownerRevision)
    {
        snapshot = null;
        ownerRevision = null;
        if (npcRuntimeList == null || !mutationGuardBinding.CanMutate
            || advanceSentencesStepRevision == long.MaxValue)
        {
            return false;
        }

        List<JusticeAdvanceSentencesRosterSnapshot> roster =
            new List<JusticeAdvanceSentencesRosterSnapshot>(npcRuntimeList.Count);
        List<JusticeAdvanceSentencesSentenceSnapshot> sentences =
            new List<JusticeAdvanceSentencesSentenceSnapshot>(prisonSentences.Count);
        List<JusticeAdvanceSentencesWantedSnapshot> wanted =
            new List<JusticeAdvanceSentencesWantedSnapshot>(wantedRecords.Count);
        List<string> parts = new List<string>(
            8 + npcRuntimeList.Count * 4 + prisonSentences.Count * 8 + wantedRecords.Count * 6)
        {
            AdvanceSentencesSnapshotVersion,
            AdvanceSentencesOperationVersion,
            advanceSentencesStepRevision.ToString(CultureInfo.InvariantCulture),
            FreeStatusIdentity,
            WantedStatusIdentity,
            ArrestedStatusIdentity,
            HiddenStatusIdentity,
            npcRuntimeList.Count.ToString(CultureInfo.InvariantCulture)
        };
        Dictionary<string, NpcRuntime> runtimeIds = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);

        for (int i = 0; i < npcRuntimeList.Count; i++)
        {
            NpcRuntime npc = npcRuntimeList[i];
            parts.Add("roster");
            parts.Add(i.ToString(CultureInfo.InvariantCulture));
            if (npc == null)
            {
                roster.Add(new JusticeAdvanceSentencesRosterSnapshot(null, string.Empty, string.Empty,
                    0, 0, 0, 0, 0));
                parts.Add("null-slot");
                continue;
            }

            string runtimeId = npc.RuntimeId;
            if (!TryRegisterAdvanceSentencesNpcRuntime(runtimeIds, npc))
            {
                return false;
            }

            runtimeIds[runtimeId] = npc;
            string personId = npc.PersonId?.Value ?? string.Empty;
            roster.Add(CaptureAdvanceSentencesRosterEntry(npc, runtimeId, personId));
            parts.Add(runtimeId);
            parts.Add(personId);
        }

        parts.Add(prisonSentences.Count.ToString(CultureInfo.InvariantCulture));
        for (int i = 0; i < prisonSentences.Count; i++)
        {
            PrisonSentenceRuntime sentence = prisonSentences[i];
            parts.Add("sentence");
            parts.Add(i.ToString(CultureInfo.InvariantCulture));
            if (sentence == null)
            {
                sentences.Add(new JusticeAdvanceSentencesSentenceSnapshot(null, null, null, null,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    0, 0, 0, 0, 0, 0, false));
                parts.Add("null-slot");
                continue;
            }

            NpcRuntime target = sentence.Target;
            CityRuntime city = sentence.City;
            WantedRecordRuntime warrant = sentence.Warrant;
            NpcRuntime warrantTarget = warrant?.Target;
            CityRuntime warrantCity = warrant?.City;
            if (!TryRegisterAdvanceSentencesNpcRuntime(runtimeIds, target)
                || !TryRegisterAdvanceSentencesNpcRuntime(runtimeIds, warrantTarget))
            {
                return false;
            }

            string targetRuntimeId = target?.RuntimeId ?? string.Empty;
            string targetPersonId = target?.PersonId?.Value ?? string.Empty;
            string cityRuntimeId = city?.RuntimeId ?? string.Empty;
            string warrantTargetRuntimeId = warrantTarget?.RuntimeId ?? string.Empty;
            string warrantTargetPersonId = warrantTarget?.PersonId?.Value ?? string.Empty;
            string warrantCityRuntimeId = warrantCity?.RuntimeId ?? string.Empty;
            bool warrantActive = warrant != null && warrant.IsActive;
            sentences.Add(new JusticeAdvanceSentencesSentenceSnapshot(
                sentence, target, city, warrant, targetRuntimeId, targetPersonId, cityRuntimeId,
                warrantTargetRuntimeId, warrantTargetPersonId, warrantCityRuntimeId,
                sentence.RemainingDays,
                target?.HiddenDaysRemaining ?? 0,
                CountAdvanceSentencesStatus(target, arrestedStatus),
                CountAdvanceSentencesStatus(target, freeStatus),
                CountAdvanceSentencesStatus(target, wantedStatus),
                CountAdvanceSentencesStatus(target, hiddenStatus),
                warrantActive));
            AddAdvanceSentencesNpcIdentity(parts, targetRuntimeId, targetPersonId);
            parts.Add(cityRuntimeId);
            AddAdvanceSentencesNpcIdentity(parts, warrantTargetRuntimeId, warrantTargetPersonId);
            parts.Add(warrantCityRuntimeId);
        }

        parts.Add(wantedRecords.Count.ToString(CultureInfo.InvariantCulture));
        for (int i = 0; i < wantedRecords.Count; i++)
        {
            WantedRecordRuntime record = wantedRecords[i];
            parts.Add("wanted-record");
            parts.Add(i.ToString(CultureInfo.InvariantCulture));
            if (record == null)
            {
                wanted.Add(new JusticeAdvanceSentencesWantedSnapshot(null, null, null,
                    string.Empty, string.Empty, string.Empty, false));
                parts.Add("null-slot");
                continue;
            }

            NpcRuntime target = record.Target;
            CityRuntime city = record.City;
            if (!TryRegisterAdvanceSentencesNpcRuntime(runtimeIds, target))
            {
                return false;
            }

            string targetRuntimeId = target?.RuntimeId ?? string.Empty;
            string targetPersonId = target?.PersonId?.Value ?? string.Empty;
            string cityRuntimeId = city?.RuntimeId ?? string.Empty;
            wanted.Add(new JusticeAdvanceSentencesWantedSnapshot(
                record, target, city, targetRuntimeId, targetPersonId, cityRuntimeId, record.IsActive));
            AddAdvanceSentencesNpcIdentity(parts, targetRuntimeId, targetPersonId);
            parts.Add(cityRuntimeId);
        }

        ownerRevision = SpatialStableKey.Encode(parts.ToArray());
        snapshot = new JusticeAdvanceSentencesSnapshot(roster, sentences, wanted);
        return true;
    }

    private JusticeAdvanceSentencesRosterSnapshot CaptureAdvanceSentencesRosterEntry(
        NpcRuntime npc,
        string runtimeId,
        string personId)
    {
        return new JusticeAdvanceSentencesRosterSnapshot(
            npc,
            runtimeId,
            personId,
            npc.HiddenDaysRemaining,
            CountAdvanceSentencesStatus(npc, arrestedStatus),
            CountAdvanceSentencesStatus(npc, freeStatus),
            CountAdvanceSentencesStatus(npc, wantedStatus),
            CountAdvanceSentencesStatus(npc, hiddenStatus));
    }

    private static int CountAdvanceSentencesStatus(NpcRuntime npc, NpcStatusData status)
    {
        if (npc == null || status == null)
        {
            return 0;
        }

        int count = 0;
        foreach (NpcStatusData current in npc.CurrentStatus)
        {
            if (ReferenceEquals(current, status))
            {
                count++;
            }
        }

        return count;
    }

    private static void AddAdvanceSentencesNpcIdentity(List<string> parts, string runtimeId, string personId)
    {
        parts.Add(runtimeId ?? string.Empty);
        parts.Add(personId ?? string.Empty);
    }

    private static bool TryRegisterAdvanceSentencesNpcRuntime(
        Dictionary<string, NpcRuntime> runtimeIds,
        NpcRuntime npc)
    {
        if (npc == null)
        {
            return true;
        }

        string runtimeId = npc.RuntimeId;
        if (string.IsNullOrWhiteSpace(runtimeId)
            || (runtimeIds.TryGetValue(runtimeId, out NpcRuntime registered)
                && !ReferenceEquals(registered, npc)))
        {
            return false;
        }

        runtimeIds[runtimeId] = npc;
        return true;
    }

    private static string GetAdvanceSentencesStatusIdentity(NpcStatusData status) =>
        status == null ? string.Empty : status.statusName ?? string.Empty;

    private string FreeStatusIdentity => GetAdvanceSentencesStatusIdentity(freeStatus);
    private string WantedStatusIdentity => GetAdvanceSentencesStatusIdentity(wantedStatus);
    private string ArrestedStatusIdentity => GetAdvanceSentencesStatusIdentity(arrestedStatus);
    private string HiddenStatusIdentity => GetAdvanceSentencesStatusIdentity(hiddenStatus);

    private bool TryReserveAdvanceSentencesStatusCapacity(JusticeAdvanceSentencesSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return false;
        }

        HashSet<NpcRuntime> reserved = new HashSet<NpcRuntime>();
        foreach (JusticeAdvanceSentencesRosterSnapshot entry in snapshot.Roster)
        {
            if (!TryReserveAdvanceSentencesStatusCapacity(entry?.Npc, reserved))
            {
                return false;
            }
        }

        foreach (JusticeAdvanceSentencesSentenceSnapshot entry in snapshot.Sentences)
        {
            if (!TryReserveAdvanceSentencesStatusCapacity(entry?.Target, reserved))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryReserveAdvanceSentencesStatusCapacity(NpcRuntime npc, HashSet<NpcRuntime> reserved)
    {
        if (npc == null || !reserved.Add(npc))
        {
            return true;
        }

        IReadOnlyList<NpcStatusData> statuses = npc.CurrentStatus;
        if (statuses.Count > int.MaxValue - 2)
        {
            return false;
        }

        return npc.TryReserveCurrentStatusCapacity(2);
    }

    private static bool TryGetAdvanceSentencesDiagnosticCapacity(
        JusticeAdvanceSentencesSnapshot snapshot,
        out int capacity)
    {
        capacity = 0;
        if (snapshot == null || snapshot.Sentences.Count > int.MaxValue - snapshot.Roster.Count)
        {
            return false;
        }

        capacity = snapshot.Sentences.Count + snapshot.Roster.Count;
        return true;
    }

    private static bool TryCaptureAdvanceSentencesLogCandidates(
        JusticeAdvanceSentencesSnapshot snapshot,
        out List<JusticeAdvanceSentencesSentenceLogCandidates> sentenceCandidates,
        out List<JusticeAdvanceSentencesLogNotice> rosterCandidates)
    {
        sentenceCandidates = null;
        rosterCandidates = null;
        if (snapshot == null)
        {
            return false;
        }

        sentenceCandidates = new List<JusticeAdvanceSentencesSentenceLogCandidates>(snapshot.Sentences.Count);
        foreach (JusticeAdvanceSentencesSentenceSnapshot sentence in snapshot.Sentences)
        {
            if (sentence == null || sentence.Sentence == null || sentence.Target == null)
            {
                sentenceCandidates.Add(null);
                continue;
            }

            if (sentence.WarrantActive && sentence.TargetArrestedStatusCount > 0
                && sentence.RemainingDays <= 1 && sentence.City == null)
            {
                // Legacy expiry diagnostics require the sentence city. Reject the malformed
                // owner state before the first mutation instead of failing midway through commit.
                return false;
            }

            sentenceCandidates.Add(new JusticeAdvanceSentencesSentenceLogCandidates(
                new JusticeAdvanceSentencesLogNotice(
                    false,
                    $"{sentence.Target.NpcName} foi libertado porque seu mandado nao esta mais ativo."),
                new JusticeAdvanceSentencesLogNotice(
                    false,
                    sentence.City == null
                        ? string.Empty
                        : $"{sentence.Target.NpcName} cumpriu sua pena em {sentence.City.CityName} e foi libertado.")));
        }

        rosterCandidates = new List<JusticeAdvanceSentencesLogNotice>(snapshot.Roster.Count);
        foreach (JusticeAdvanceSentencesRosterSnapshot actor in snapshot.Roster)
        {
            rosterCandidates.Add(actor?.Npc == null
                ? null
                : new JusticeAdvanceSentencesLogNotice(
                    true,
                    $"{actor.Npc.NpcName} estava preso sem sentenca ativa e foi libertado."));
        }

        return true;
    }

    private static bool SameAdvanceSentencesSnapshot(
        JusticeAdvanceSentencesSnapshot expected,
        JusticeAdvanceSentencesSnapshot actual)
    {
        if (expected == null || actual == null
            || !SameAdvanceSentencesRoster(expected.Roster, actual.Roster)
            || !SameAdvanceSentencesSentences(expected.Sentences, actual.Sentences)
            || !SameAdvanceSentencesWanted(expected.WantedRecords, actual.WantedRecords))
        {
            return false;
        }

        return true;
    }

    private static bool SameAdvanceSentencesRoster(
        IReadOnlyList<JusticeAdvanceSentencesRosterSnapshot> expected,
        IReadOnlyList<JusticeAdvanceSentencesRosterSnapshot> actual)
    {
        if (expected == null || actual == null || expected.Count != actual.Count)
        {
            return false;
        }

        for (int i = 0; i < expected.Count; i++)
        {
            JusticeAdvanceSentencesRosterSnapshot left = expected[i];
            JusticeAdvanceSentencesRosterSnapshot right = actual[i];
            if (left == null || right == null
                || !ReferenceEquals(left.Npc, right.Npc)
                || !string.Equals(left.RuntimeId, right.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(left.PersonId, right.PersonId, StringComparison.Ordinal)
                || left.HiddenDaysRemaining != right.HiddenDaysRemaining
                || left.ArrestedStatusCount != right.ArrestedStatusCount
                || left.FreeStatusCount != right.FreeStatusCount
                || left.WantedStatusCount != right.WantedStatusCount
                || left.HiddenStatusCount != right.HiddenStatusCount)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameAdvanceSentencesSentences(
        IReadOnlyList<JusticeAdvanceSentencesSentenceSnapshot> expected,
        IReadOnlyList<JusticeAdvanceSentencesSentenceSnapshot> actual)
    {
        if (expected == null || actual == null || expected.Count != actual.Count)
        {
            return false;
        }

        for (int i = 0; i < expected.Count; i++)
        {
            JusticeAdvanceSentencesSentenceSnapshot left = expected[i];
            JusticeAdvanceSentencesSentenceSnapshot right = actual[i];
            if (left == null || right == null
                || !ReferenceEquals(left.Sentence, right.Sentence)
                || !ReferenceEquals(left.Target, right.Target)
                || !ReferenceEquals(left.City, right.City)
                || !ReferenceEquals(left.Warrant, right.Warrant)
                || !string.Equals(left.TargetRuntimeId, right.TargetRuntimeId, StringComparison.Ordinal)
                || !string.Equals(left.TargetPersonId, right.TargetPersonId, StringComparison.Ordinal)
                || !string.Equals(left.CityRuntimeId, right.CityRuntimeId, StringComparison.Ordinal)
                || !string.Equals(left.WarrantTargetRuntimeId, right.WarrantTargetRuntimeId, StringComparison.Ordinal)
                || !string.Equals(left.WarrantTargetPersonId, right.WarrantTargetPersonId, StringComparison.Ordinal)
                || !string.Equals(left.WarrantCityRuntimeId, right.WarrantCityRuntimeId, StringComparison.Ordinal)
                || left.RemainingDays != right.RemainingDays
                || left.TargetHiddenDaysRemaining != right.TargetHiddenDaysRemaining
                || left.TargetArrestedStatusCount != right.TargetArrestedStatusCount
                || left.TargetFreeStatusCount != right.TargetFreeStatusCount
                || left.TargetWantedStatusCount != right.TargetWantedStatusCount
                || left.TargetHiddenStatusCount != right.TargetHiddenStatusCount
                || left.WarrantActive != right.WarrantActive)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameAdvanceSentencesWanted(
        IReadOnlyList<JusticeAdvanceSentencesWantedSnapshot> expected,
        IReadOnlyList<JusticeAdvanceSentencesWantedSnapshot> actual)
    {
        if (expected == null || actual == null || expected.Count != actual.Count)
        {
            return false;
        }

        for (int i = 0; i < expected.Count; i++)
        {
            JusticeAdvanceSentencesWantedSnapshot left = expected[i];
            JusticeAdvanceSentencesWantedSnapshot right = actual[i];
            if (left == null || right == null
                || !ReferenceEquals(left.Record, right.Record)
                || !ReferenceEquals(left.Target, right.Target)
                || !ReferenceEquals(left.City, right.City)
                || !string.Equals(left.TargetRuntimeId, right.TargetRuntimeId, StringComparison.Ordinal)
                || !string.Equals(left.TargetPersonId, right.TargetPersonId, StringComparison.Ordinal)
                || !string.Equals(left.CityRuntimeId, right.CityRuntimeId, StringComparison.Ordinal)
                || left.IsActive != right.IsActive)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryGetAdvanceSentencesIdentity(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out string identity,
        out string fingerprint)
    {
        identity = null;
        fingerprint = null;
        if (manifest == null || step == null || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step)
            || step.StepId != AdvanceSentencesStepId || step.OwnerId != AdvanceSentencesOwnerId
            || step.OperationKind != AdvanceSentencesOperationKind
            || step.OperationVersion != AdvanceSentencesOperationVersion
            || step.Disposition != "included")
        {
            return false;
        }

        string expectedBoundaryOccurrenceId = SpatialStableKey.Encode(
            manifest.WorldId,
            manifest.ProfileId,
            manifest.AbsoluteDay.ToString(CultureInfo.InvariantCulture));
        if (!string.Equals(manifest.BoundaryOccurrenceId, expectedBoundaryOccurrenceId, StringComparison.Ordinal))
        {
            return false;
        }

        identity = SpatialStableKey.Encode(manifest.BoundaryOccurrenceId, step.StepId);
        fingerprint = SpatialStableKey.Encode(
            manifest.BoundaryOccurrenceId,
            manifest.ContinuationId,
            manifest.SubphaseKind,
            manifest.SubphaseVersion,
            manifest.ConfigurationIdentity,
            manifest.ContentIdentity,
            step.Ordinal.ToString(CultureInfo.InvariantCulture),
            step.StepId,
            step.OwnerId,
            step.OperationKind,
            step.OperationVersion,
            step.OwnerRevision,
            step.Payload,
            step.PersonId,
            step.Disposition);
        return true;
    }

    private bool TryGetBeginDayIdentity(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out string identity,
        out string fingerprint)
    {
        identity = null;
        fingerprint = null;
        if (manifest == null || step == null || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step)
            || step.StepId != BeginDayStepId || step.OwnerId != BeginDayOwnerId
            || step.OperationKind != BeginDayOperationKind
            || step.OperationVersion != BeginDayOperationVersion
            || step.Disposition != "included")
        {
            return false;
        }

        string expectedBoundaryOccurrenceId = SpatialStableKey.Encode(
            manifest.WorldId,
            manifest.ProfileId,
            manifest.AbsoluteDay.ToString(CultureInfo.InvariantCulture));
        if (!string.Equals(manifest.BoundaryOccurrenceId, expectedBoundaryOccurrenceId, StringComparison.Ordinal))
        {
            return false;
        }

        identity = SpatialStableKey.Encode(manifest.BoundaryOccurrenceId, step.StepId);
        fingerprint = SpatialStableKey.Encode(
            manifest.BoundaryOccurrenceId,
            manifest.ContinuationId,
            manifest.SubphaseKind,
            manifest.SubphaseVersion,
            manifest.ConfigurationIdentity,
            manifest.ContentIdentity,
            step.Ordinal.ToString(CultureInfo.InvariantCulture),
            step.StepId,
            step.OwnerId,
            step.OperationKind,
            step.OperationVersion,
            step.OwnerRevision,
            step.Payload,
            step.PersonId,
            step.Disposition);
        return true;
    }

    private static bool SameSentenceReferences(
        IReadOnlyList<JusticeBeginDaySentenceSnapshot> expected,
        IReadOnlyList<JusticeBeginDaySentenceSnapshot> actual)
    {
        if (expected == null || actual == null || expected.Count != actual.Count)
        {
            return false;
        }

        Dictionary<PrisonSentenceRuntime, int> expectedCounts =
            new Dictionary<PrisonSentenceRuntime, int>();
        int expectedNullCount = 0;
        foreach (JusticeBeginDaySentenceSnapshot snapshot in expected)
        {
            if (snapshot?.Sentence == null)
            {
                expectedNullCount++;
                continue;
            }

            expectedCounts.TryGetValue(snapshot.Sentence, out int count);
            expectedCounts[snapshot.Sentence] = count + 1;
        }

        foreach (JusticeBeginDaySentenceSnapshot snapshot in actual)
        {
            if (snapshot?.Sentence == null)
            {
                expectedNullCount--;
                continue;
            }

            if (!expectedCounts.TryGetValue(snapshot.Sentence, out int count))
            {
                return false;
            }

            if (count == 1)
            {
                expectedCounts.Remove(snapshot.Sentence);
            }
            else
            {
                expectedCounts[snapshot.Sentence] = count - 1;
            }
        }

        return expectedNullCount == 0 && expectedCounts.Count == 0;
    }

    public void CreateInitialWarrants(
        SimulationConfigData config,
        Func<NpcData, NpcRuntime> getSingleNpcRuntimeByDefinition,
        Func<CityData, CityRuntime> getSingleCityRuntimeByDefinition)
    {
        ThrowIfFaulted();

        if (config == null || getSingleNpcRuntimeByDefinition == null || getSingleCityRuntimeByDefinition == null)
        {
            return;
        }

        foreach (InitialWantedRecordConfig warrantConfig in config.InitialWarrants)
        {
            if (warrantConfig == null || warrantConfig.target == null || warrantConfig.city == null)
            {
                continue;
            }

            NpcRuntime target = getSingleNpcRuntimeByDefinition(warrantConfig.target);
            CityRuntime city = getSingleCityRuntimeByDefinition(warrantConfig.city);
            CreateOrIncreaseWarrant(target, city, warrantConfig.bounty, warrantConfig.sentenceDays);
        }
    }

    public WantedRecordRuntime CreateOrIncreaseWarrant(NpcRuntime target, CityRuntime city, float bounty, int sentenceDays)
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            return null;
        }

        if (target == null || city == null)
        {
            return null;
        }

        WantedRecordRuntime record = GetActiveWarrant(target, city);

        if (record == null)
        {
            record = new WantedRecordRuntime(target, city, bounty, sentenceDays);
            if (mutationGuardBinding.BoundGuard != null
                && record.TryBindMutationGuard(mutationGuardBinding.BoundGuard) == false)
            {
                return null;
            }
            EnsureP12CrimeJusticeMutationAllowed();
            wantedRecords.Add(record);
            NotifyP12CrimeJusticeMutationCommitted();
        }
        else
        {
            record.AddPenalty(bounty, sentenceDays);
        }

        SyncWantedStatus(target);
        logger.Log(SimulationLogCategory.Justice, $"{target.NpcName} agora possui mandado em {city.CityName}. Recompensa: {record.Bounty:0.##}. Pena: {record.SentenceDays} dias.");
        return record;
    }

    public bool Arrest(NpcRuntime guardRuntime, NpcRuntime targetRuntime, CityRuntime city, string originDecisionId = null)
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            return false;
        }

        if (guardRuntime == null || targetRuntime == null || city == null || arrestedStatus == null || IsArrested(targetRuntime) == true)
        {
            return false;
        }

        WantedRecordRuntime record = GetActiveWarrant(targetRuntime, city);

        if (record == null)
        {
            return false;
        }

        PrisonSentenceRuntime currentSentence = GetActiveSentence(targetRuntime);

        if (currentSentence != null)
        {
            return false;
        }

        PrisonSentenceRuntime sentence = new PrisonSentenceRuntime(targetRuntime, city, record, record.SentenceDays);
        if (mutationGuardBinding.BoundGuard != null
            && sentence.TryBindMutationGuard(mutationGuardBinding.BoundGuard) == false)
        {
            return false;
        }
        EnsureP12CrimeJusticeMutationAllowed();
        prisonSentences.Add(sentence);
        NotifyP12CrimeJusticeMutationCommitted();
        targetRuntime.ClearHidden();
        targetRuntime.RemoveStatus(hiddenStatus);
        targetRuntime.RemoveStatus(freeStatus);
        targetRuntime.AddStatus(arrestedStatus);
        SyncWantedStatus(targetRuntime);
        domainEventRecorder?.Record((eventId, absoluteDay, recordSequence) => new NpcArrestedEvent(
            eventId,
            absoluteDay,
            recordSequence,
            guardRuntime.RuntimeId,
            targetRuntime.RuntimeId,
            city.Location?.RuntimeId,
            originDecisionId));
        logger.Log(SimulationLogCategory.Justice, $"{guardRuntime.NpcName} prendeu {targetRuntime.NpcName} em {city.CityName}. Pena restante: {sentence.RemainingDays} dias.");
        return true;
    }

    /// <summary>Prepares, but does not publish, the Unity view for a Core-owned selected Arrest row.</summary>
    internal bool TryPrepareSelectedArrestSentenceProjection(
        NpcRuntime targetRuntime,
        CityRuntime city,
        WantedRecordRuntime warrant,
        DailyV1JusticeSentenceOwner sentenceOwner)
    {
        if (targetRuntime == null || city == null || warrant == null || sentenceOwner == null
            || justiceRecordsOwner.ContainsSentence(sentenceOwner)
            || sentenceViews.ContainsKey(sentenceOwner)
            || preparedSelectedSentenceViews.ContainsKey(sentenceOwner)
            || !ReferenceEquals(sentenceOwner.Warrant, warrant.CoreOwner)
            || !string.Equals(sentenceOwner.TargetRuntimeId, targetRuntime.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(sentenceOwner.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
            || !wantedRecordViews.TryGetValue(warrant.CoreOwner, out WantedRecordRuntime exactWarrant)
            || !ReferenceEquals(exactWarrant, warrant)
            || !ReferenceEquals(warrant.Target, targetRuntime)
            || !ReferenceEquals(warrant.City, city)
            || !PrisonSentenceRuntime.TryCreateSelectedProjection(
                targetRuntime, city, warrant, sentenceOwner,
                out PrisonSentenceRuntime projectedSentence))
            return false;

        preparedSelectedSentenceViews.Add(sentenceOwner, projectedSentence);
        return true;
    }

    internal void DiscardPreparedSelectedArrestSentenceProjection(
        DailyV1JusticeSentenceOwner sentenceOwner)
    {
        if (sentenceOwner != null && !sentenceViews.ContainsKey(sentenceOwner))
            preparedSelectedSentenceViews.Remove(sentenceOwner);
    }

    /// <summary>Publishes the prepared Unity sentence view after Core owns the row.</summary>
    internal bool CommitPreparedSelectedArrestSentenceProjection(
        DailyV1JusticeSentenceOwner sentenceOwner)
    {
        if (sentenceOwner == null
            || !justiceRecordsOwner.ContainsSentence(sentenceOwner)
            || sentenceViews.ContainsKey(sentenceOwner)
            || !preparedSelectedSentenceViews.TryGetValue(
                sentenceOwner, out PrisonSentenceRuntime prepared)
            || prepared == null || !ReferenceEquals(prepared.CoreOwner, sentenceOwner))
            return false;

        sentenceViews.Add(sentenceOwner, prepared);
        preparedSelectedSentenceViews.Remove(sentenceOwner);
        return true;
    }

    internal bool CanRemoveSelectedSentenceProjection(
        DailyV1JusticeSentenceOwner sentenceOwner) =>
        sentenceOwner != null
        && justiceRecordsOwner.ContainsSentence(sentenceOwner)
        && sentenceViews.TryGetValue(sentenceOwner, out PrisonSentenceRuntime view)
        && view != null && ReferenceEquals(view.CoreOwner, sentenceOwner)
        && CanCommitP12CrimeJusticeMutation();

    /// <summary>Removes only the Unity view after the Core sentence owner committed removal.</summary>
    internal void CommitSelectedSentenceProjectionRemoval(
        DailyV1JusticeSentenceOwner sentenceOwner)
    {
        if (sentenceOwner == null || justiceRecordsOwner.ContainsSentence(sentenceOwner)
            || !sentenceViews.TryGetValue(sentenceOwner, out PrisonSentenceRuntime view)
            || view == null || !ReferenceEquals(view.CoreOwner, sentenceOwner)
            || !sentenceViews.Remove(sentenceOwner))
            throw new InvalidOperationException(
                "The selected Core Justice sentence was removed without its exact Unity view.");
    }

    /// <summary>Removes an exact Core sentence owner and retires its Unity view as one selected-owner commit.</summary>
    internal bool TryRemoveSelectedSentence(DailyV1JusticeSentenceOwner sentenceOwner)
    {
        if (sentenceOwner == null
            || !justiceRecordsOwner.ContainsSentence(sentenceOwner)
            || !sentenceViews.TryGetValue(sentenceOwner, out PrisonSentenceRuntime view)
            || view == null || !ReferenceEquals(view.CoreOwner, sentenceOwner)
            || !CanCommitP12CrimeJusticeMutation())
            return false;

        if (!justiceRecordsOwner.TryRemoveSentence(sentenceOwner))
            return false;
        sentenceViews.Remove(sentenceOwner);
        return !justiceRecordsOwner.ContainsSentence(sentenceOwner)
            && !sentenceViews.ContainsKey(sentenceOwner);
    }

    /// <summary>Publishes the compatibility event and log after the selected
    /// Core arrest transition has committed its owner writes.</summary>
    internal void PublishSelectedArrest(
        NpcRuntime guardRuntime,
        NpcRuntime targetRuntime,
        CityRuntime city,
        string originDecisionId)
    {
        if (guardRuntime == null || targetRuntime == null || city == null)
            throw new InvalidOperationException("A selected arrest lost its exact Unity projections before publication.");
        PrisonSentenceRuntime sentence = GetActiveSentence(targetRuntime);
        if (sentence == null || sentence.Warrant == null
            || !ReferenceEquals(sentence.Target, targetRuntime)
            || !ReferenceEquals(sentence.City, city))
            throw new InvalidOperationException("A selected arrest has no exact Justice sentence projection.");

        domainEventRecorder?.Record((eventId, absoluteDay, recordSequence) => new NpcArrestedEvent(
            eventId,
            absoluteDay,
            recordSequence,
            guardRuntime.RuntimeId,
            targetRuntime.RuntimeId,
            city.Location?.RuntimeId,
            originDecisionId));
        logger.Log(SimulationLogCategory.Justice,
            $"{guardRuntime.NpcName} prendeu {targetRuntime.NpcName} em {city.CityName}. Pena restante: {sentence.RemainingDays} dias.");
    }

    /// <summary>Publishes the event and legacy log after a Core-owned selected Escape transition.</summary>
    internal void PublishSelectedEscape(
        NpcRuntime targetRuntime,
        CityRuntime eventCity,
        bool hadActiveSentence,
        float bountyAfterPenalty,
        string originDecisionId)
    {
        if (targetRuntime == null
            || (eventCity != null
                && !ReferenceEquals(eventCity.SelectedRuntimeOwners?.Definitions,
                    targetRuntime.SelectedDailyV1DefinitionGraph)))
            throw new InvalidOperationException("Selected EscapePrison lost its exact output projections.");

        RecordNpcEscaped(targetRuntime, eventCity, originDecisionId);
        if (hadActiveSentence)
            logger.Log(SimulationLogCategory.Justice,
                $"{targetRuntime.NpcName} fugiu da prisao em {eventCity.CityName}. Recompensa atual: {bountyAfterPenalty:0.##}.");
    }

    public void AdvanceSentences(List<NpcRuntime> npcRuntimeList)
    {
        ThrowIfFaulted();
        ApplyAdvanceSentences(npcRuntimeList, null, null, null);
    }

    private void ApplyAdvanceSentences(
        List<NpcRuntime> npcRuntimeList,
        List<JusticeAdvanceSentencesLogNotice> notices,
        IReadOnlyList<JusticeAdvanceSentencesSentenceLogCandidates> sentenceLogCandidates,
        IReadOnlyList<JusticeAdvanceSentencesLogNotice> rosterLogCandidates)
    {
        for (int i = prisonSentences.Count - 1; i >= 0; i--)
        {
            PrisonSentenceRuntime sentence = prisonSentences[i];

            if (sentence == null || sentence.Target == null)
            {
                RemoveP12PrisonSentenceAt(i);
                continue;
            }

            if (sentence.Warrant == null || sentence.Warrant.IsActive == false)
            {
                ReleasePrisoner(sentence.Target);
                RemoveP12PrisonSentenceAt(i);
                if (notices == null)
                {
                    logger.Log(SimulationLogCategory.Justice,
                        $"{sentence.Target.NpcName} foi libertado porque seu mandado nao esta mais ativo.");
                }
                else
                {
                    notices.Add(sentenceLogCandidates[i].InactiveWarrantNotice);
                }
                continue;
            }

            if (IsArrested(sentence.Target) == false)
            {
                RemoveP12PrisonSentenceAt(i);
                SyncWantedStatus(sentence.Target);
                continue;
            }

            sentence.AdvanceDay();

            if (sentence.RemainingDays > 0)
            {
                continue;
            }

            ResolveWarrant(sentence.Warrant);
            ReleasePrisoner(sentence.Target);
            RemoveP12PrisonSentenceAt(i);
            if (notices == null)
            {
                logger.Log(SimulationLogCategory.Justice,
                    $"{sentence.Target.NpcName} cumpriu sua pena em {sentence.City.CityName} e foi libertado.");
            }
            else
            {
                notices.Add(sentenceLogCandidates[i].ExpiredSentenceNotice);
            }
        }

        ReleasePrisonersWithoutActiveSentence(npcRuntimeList, notices, rosterLogCandidates);
        SyncWantedStatuses(npcRuntimeList);
    }

    private void EmitAdvanceSentencesLogNotices(IReadOnlyList<JusticeAdvanceSentencesLogNotice> notices)
    {
        if (notices == null)
        {
            return;
        }

        foreach (JusticeAdvanceSentencesLogNotice notice in notices)
        {
            if (notice == null)
            {
                continue;
            }

            if (notice.IsWarning)
            {
                logger.LogWarning(notice.Message);
            }
            else
            {
                logger.Log(SimulationLogCategory.Justice, notice.Message);
            }
        }
    }

    public bool EscapePrison(NpcRuntime targetRuntime, float escapeBountyPenalty, string originDecisionId = null)
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            return false;
        }

        if (targetRuntime == null || IsArrested(targetRuntime) == false || WasArrestedToday(targetRuntime) == true)
        {
            return false;
        }

        return ApplyEscapeSuccess(targetRuntime, escapeBountyPenalty, originDecisionId);
    }

    public bool ApplyEscapeSuccess(NpcRuntime targetRuntime, float escapeBountyPenalty, string originDecisionId = null)
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            return false;
        }

        if (targetRuntime == null || IsArrested(targetRuntime) == false)
        {
            return false;
        }

        PrisonSentenceRuntime sentence = GetActiveSentence(targetRuntime);

        if (sentence == null || sentence.Warrant == null || sentence.Warrant.IsActive == false)
        {
            CityRuntime escapeCity = targetRuntime.CurrentCity;
            ReleasePrisoner(targetRuntime);
            SyncWantedStatus(targetRuntime);
            RecordNpcEscaped(targetRuntime, escapeCity, originDecisionId);
            return true;
        }

        if (escapeBountyPenalty > 0f)
        {
            sentence.Warrant.AddPenalty(escapeBountyPenalty, 0);
        }

        RemoveP12PrisonSentence(sentence);
        targetRuntime.RemoveStatus(arrestedStatus);
        targetRuntime.AddStatus(freeStatus);
        SyncWantedStatus(targetRuntime);
        RecordNpcEscaped(targetRuntime, sentence.City, originDecisionId);
        logger.Log(SimulationLogCategory.Justice, $"{targetRuntime.NpcName} fugiu da prisao em {sentence.City.CityName}. Recompensa atual: {sentence.Warrant.Bounty:0.##}.");
        return true;
    }

    public void SyncWantedStatuses(List<NpcRuntime> npcRuntimeList)
    {
        ThrowIfFaulted();

        if (npcRuntimeList == null)
        {
            return;
        }

        foreach (NpcRuntime npcRuntime in npcRuntimeList)
        {
            SyncWantedStatus(npcRuntime);
        }
    }

    public void SyncWantedStatus(NpcRuntime npcRuntime)
    {
        ThrowIfFaulted();

        if (npcRuntime == null || wantedStatus == null)
        {
            return;
        }

        if (HasAnyActiveWarrant(npcRuntime) == true)
        {
            npcRuntime.AddStatus(wantedStatus);
        }
        else
        {
            npcRuntime.RemoveStatus(wantedStatus);
        }
    }

    public bool HasActiveWarrantInCity(NpcRuntime targetRuntime, CityRuntime cityRuntime)
    {
        return GetActiveWarrant(targetRuntime, cityRuntime) != null;
    }

    public bool HasAnyActiveWarrant(NpcRuntime targetRuntime)
    {
        if (targetRuntime == null) return false;
        if (string.IsNullOrWhiteSpace(targetRuntime.RuntimeId))
        {
            foreach (WantedRecordRuntime record in wantedRecords)
            {
                if (record != null && record.IsActive == true && record.Target == targetRuntime)
                    return true;
            }
            return false;
        }

        foreach (DailyV1JusticeWantedRecordOwner row in justiceRecordsOwner.FindActiveWantedRecords(targetRuntime.RuntimeId))
            if (wantedRecordViews.TryGetValue(row, out WantedRecordRuntime record)
                && record.IsActive && ReferenceEquals(record.Target, targetRuntime))
                return true;
        return false;
    }

    public float GetBounty(NpcRuntime targetRuntime, CityRuntime cityRuntime)
    {
        WantedRecordRuntime record = GetActiveWarrant(targetRuntime, cityRuntime);
        return record != null ? record.Bounty : 0f;
    }

    public int GetRemainingSentenceDays(NpcRuntime targetRuntime)
    {
        PrisonSentenceRuntime sentence = GetActiveSentence(targetRuntime);
        return sentence != null ? sentence.RemainingDays : 0;
    }

    public int GetFailedEscapeAttempts(NpcRuntime targetRuntime)
    {
        PrisonSentenceRuntime sentence = GetActiveSentence(targetRuntime);
        return sentence != null ? sentence.FailedEscapeAttempts : 0;
    }

    public bool WasArrestedToday(NpcRuntime targetRuntime)
    {
        PrisonSentenceRuntime sentence = GetActiveSentence(targetRuntime);
        return sentence != null && sentence.WasArrestedToday;
    }

    public bool RegisterFailedEscape(NpcRuntime targetRuntime, int additionalSentenceDays)
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            return false;
        }

        PrisonSentenceRuntime sentence = GetActiveSentence(targetRuntime);

        if (sentence == null || sentence.Warrant == null || sentence.Warrant.IsActive == false)
        {
            return false;
        }

        sentence.RegisterFailedEscape(additionalSentenceDays);
        return true;
    }

    public bool IsArrested(NpcRuntime targetRuntime)
    {
        if (targetRuntime == null)
            return false;

        if (targetRuntime.UsesSelectedDailyV1Definitions)
        {
            return targetRuntime.HasSelectedDailyV1Status(
                targetRuntime.SelectedDailyV1DefinitionGraph?.Configuration?.arrestedStatus);
        }

        return arrestedStatus != null && targetRuntime.CurrentStatus.Contains(arrestedStatus);
    }

    public WantedRecordRuntime GetActiveWarrant(NpcRuntime targetRuntime, CityRuntime cityRuntime)
    {
        if (targetRuntime == null || cityRuntime == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(targetRuntime.RuntimeId)
            || string.IsNullOrWhiteSpace(cityRuntime.RuntimeId))
        {
            foreach (WantedRecordRuntime record in wantedRecords)
                if (record != null && record.IsActive
                    && ReferenceEquals(record.Target, targetRuntime)
                    && ReferenceEquals(record.City, cityRuntime))
                    return record;
            return null;
        }

        foreach (DailyV1JusticeWantedRecordOwner row in
            justiceRecordsOwner.FindActiveWantedRecords(targetRuntime.RuntimeId, cityRuntime.RuntimeId))
            if (wantedRecordViews.TryGetValue(row, out WantedRecordRuntime projectedRecord)
                && projectedRecord.IsActive && ReferenceEquals(projectedRecord.Target, targetRuntime)
                && ReferenceEquals(projectedRecord.City, cityRuntime))
                return projectedRecord;
        return null;
    }

    public List<WantedRecordRuntime> GetActiveWarrants(NpcRuntime targetRuntime)
    {
        List<WantedRecordRuntime> records = new List<WantedRecordRuntime>();

        if (targetRuntime == null)
        {
            return records;
        }

        if (string.IsNullOrWhiteSpace(targetRuntime.RuntimeId))
        {
            foreach (WantedRecordRuntime record in wantedRecords)
                if (record != null && record.IsActive && ReferenceEquals(record.Target, targetRuntime))
                    records.Add(record);
            return records;
        }

        foreach (DailyV1JusticeWantedRecordOwner row in justiceRecordsOwner.FindActiveWantedRecords(targetRuntime.RuntimeId))
            if (wantedRecordViews.TryGetValue(row, out WantedRecordRuntime projectedRecord)
                && projectedRecord.IsActive && ReferenceEquals(projectedRecord.Target, targetRuntime))
                records.Add(projectedRecord);

        return records;
    }

    internal bool TryReadP12CrimeJusticeCensus(
        Func<NpcRuntime, bool> isNpcInstalled,
        Func<CityRuntime, bool> isCityInstalled,
        out int wantedCount,
        out int sentenceCount,
        out long revision)
    {
        wantedCount = 0;
        sentenceCount = 0;
        revision = justiceRecordsOwner.Revision;
        if (isNpcInstalled == null || isCityInstalled == null
            || wantedRecords == null || prisonSentences == null
            || revision < 0L)
            return false;

        HashSet<WantedRecordRuntime> wantedRows = new HashSet<WantedRecordRuntime>();
        foreach (WantedRecordRuntime record in wantedRecords)
        {
            if (record == null || record.Target == null || record.City == null
                || !isNpcInstalled(record.Target) || !isCityInstalled(record.City)
                || !wantedRows.Add(record))
                return false;
        }
        HashSet<PrisonSentenceRuntime> sentenceRows = new HashSet<PrisonSentenceRuntime>();
        foreach (PrisonSentenceRuntime sentence in prisonSentences)
        {
            if (sentence == null || sentence.Target == null || sentence.City == null
                || sentence.Warrant == null || !wantedRows.Contains(sentence.Warrant)
                || !sentenceRows.Add(sentence)
                || !ReferenceEquals(sentence.Target, sentence.Warrant.Target)
                || !ReferenceEquals(sentence.City, sentence.Warrant.City)
                || !isNpcInstalled(sentence.Target) || !isCityInstalled(sentence.City))
                return false;
        }

        wantedCount = wantedRecords.Count;
        sentenceCount = prisonSentences.Count;
        return true;
    }

    internal bool TryReadP12CrimeJusticeCoreCensus(
        Func<string, bool> isNpcRuntimeIdInstalled,
        Func<string, bool> isCityRuntimeIdInstalled,
        out int wantedCount,
        out int sentenceCount,
        out long revision)
    {
        wantedCount = 0;
        sentenceCount = 0;
        revision = justiceRecordsOwner.Revision;
        if (isNpcRuntimeIdInstalled == null || isCityRuntimeIdInstalled == null
            || revision < 0L || justiceRecordsOwner.WantedRecords == null
            || justiceRecordsOwner.Sentences == null)
            return false;

        HashSet<DailyV1JusticeWantedRecordOwner> wantedRows =
            new HashSet<DailyV1JusticeWantedRecordOwner>();
        foreach (DailyV1JusticeWantedRecordOwner record in justiceRecordsOwner.WantedRecords)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.TargetRuntimeId)
                || string.IsNullOrWhiteSpace(record.CityRuntimeId)
                || !isNpcRuntimeIdInstalled(record.TargetRuntimeId)
                || !isCityRuntimeIdInstalled(record.CityRuntimeId)
                || !wantedRows.Add(record))
                return false;
        }

        HashSet<DailyV1JusticeSentenceOwner> sentenceRows =
            new HashSet<DailyV1JusticeSentenceOwner>();
        foreach (DailyV1JusticeSentenceOwner sentence in justiceRecordsOwner.Sentences)
        {
            if (sentence == null || string.IsNullOrWhiteSpace(sentence.TargetRuntimeId)
                || string.IsNullOrWhiteSpace(sentence.CityRuntimeId)
                || sentence.Warrant == null || !wantedRows.Contains(sentence.Warrant)
                || !string.Equals(sentence.TargetRuntimeId, sentence.Warrant.TargetRuntimeId, StringComparison.Ordinal)
                || !string.Equals(sentence.CityRuntimeId, sentence.Warrant.CityRuntimeId, StringComparison.Ordinal)
                || !isNpcRuntimeIdInstalled(sentence.TargetRuntimeId)
                || !isCityRuntimeIdInstalled(sentence.CityRuntimeId)
                || !sentenceRows.Add(sentence))
                return false;
        }

        wantedCount = justiceRecordsOwner.WantedRecords.Count;
        sentenceCount = justiceRecordsOwner.Sentences.Count;
        return true;
    }

    internal bool TryCaptureP12EOwnerSnapshotRows(
        out IReadOnlyList<P12EJusticeWantedSnapshotRow> wantedRows,
        out IReadOnlyList<P12EJusticeSentenceSnapshotRow> sentenceRows)
    {
        wantedRows = null;
        sentenceRows = null;
        if (wantedRecords == null || prisonSentences == null || justiceRecordsOwner.Revision < 0L)
            return false;

        List<P12EJusticeWantedSnapshotRow> wanted = new List<P12EJusticeWantedSnapshotRow>(wantedRecords.Count);
        Dictionary<WantedRecordRuntime, int> wantedOrdinals =
            new Dictionary<WantedRecordRuntime, int>();
        for (int i = 0; i < wantedRecords.Count; i++)
        {
            WantedRecordRuntime row = wantedRecords[i];
            if (row == null || row.Target == null || row.City == null || wantedOrdinals.ContainsKey(row))
                return false;
            wantedOrdinals.Add(row, i);
            wanted.Add(new P12EJusticeWantedSnapshotRow(
                i,
                row.Target.RuntimeId,
                row.Target.PersonId?.Value,
                row.City.RuntimeId,
                row.Bounty,
                row.SentenceDays,
                row.P12Resolved));
        }

        List<P12EJusticeSentenceSnapshotRow> sentences =
            new List<P12EJusticeSentenceSnapshotRow>(prisonSentences.Count);
        HashSet<PrisonSentenceRuntime> seenSentences = new HashSet<PrisonSentenceRuntime>();
        for (int i = 0; i < prisonSentences.Count; i++)
        {
            PrisonSentenceRuntime row = prisonSentences[i];
            if (row == null || row.Target == null || row.City == null || row.Warrant == null
                || !seenSentences.Add(row) || !wantedOrdinals.TryGetValue(row.Warrant, out int warrantOrdinal)
                || !ReferenceEquals(row.Target, row.Warrant.Target)
                || !ReferenceEquals(row.City, row.Warrant.City))
                return false;
            sentences.Add(new P12EJusticeSentenceSnapshotRow(
                i,
                row.Target.RuntimeId,
                row.Target.PersonId?.Value,
                row.City.RuntimeId,
                warrantOrdinal,
                row.RemainingDays,
                row.FailedEscapeAttempts,
                row.WasArrestedToday));
        }

        wantedRows = wanted.AsReadOnly();
        sentenceRows = sentences.AsReadOnly();
        return true;
    }

    internal static bool TryCreateFromP12EOwnerSnapshot(
        NpcStatusData freeStatus,
        NpcStatusData wantedStatus,
        NpcStatusData arrestedStatus,
        NpcStatusData hiddenStatus,
        DomainEventRecorder domainEventRecorder,
        SimulationLogger logger,
        IReadOnlyList<WantedRecordRuntime> wantedRows,
        IReadOnlyList<PrisonSentenceRuntime> sentenceRows,
        long revision,
        out JusticeSystem staged,
        P12AJusticeReceiptOwnerSnapshot receiptSnapshot = null)
    {
        staged = null;
        if (freeStatus == null || wantedStatus == null || arrestedStatus == null || hiddenStatus == null
            || wantedRows == null || sentenceRows == null || revision < 0L)
            return false;

        HashSet<WantedRecordRuntime> seenWanted = new HashSet<WantedRecordRuntime>();
        foreach (WantedRecordRuntime row in wantedRows)
        {
            if (row == null || row.Target == null || row.City == null || !seenWanted.Add(row))
                return false;
        }
        HashSet<PrisonSentenceRuntime> seenSentences = new HashSet<PrisonSentenceRuntime>();
        foreach (PrisonSentenceRuntime row in sentenceRows)
        {
            if (row == null || row.Target == null || row.City == null || row.Warrant == null
                || !seenSentences.Add(row) || !seenWanted.Contains(row.Warrant)
                || !ReferenceEquals(row.Target, row.Warrant.Target)
                || !ReferenceEquals(row.City, row.Warrant.City))
                return false;
        }

        try
        {
            JusticeSystem candidate = new JusticeSystem(
                freeStatus, wantedStatus, arrestedStatus, hiddenStatus, domainEventRecorder, logger);
            foreach (WantedRecordRuntime row in wantedRows) candidate.wantedRecords.Add(row);
            foreach (PrisonSentenceRuntime row in sentenceRows) candidate.prisonSentences.Add(row);
            if (!candidate.justiceRecordsOwner.TryInstallExactRevision(revision)) return false;
            if (receiptSnapshot != null)
            {
                if (!candidate.TryInstallP12AReceiptOwnerSnapshot(receiptSnapshot)) return false;
            }
            else if (candidate.P12P18ReceiptCensusRevision != 0L) return false;
            staged = candidate;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException || exception is OverflowException)
        {
            staged = null;
            return false;
        }
    }

    internal bool TryBindP12CrimeJusticeMutationBoundary(
        Func<JusticeSystem, bool> admission,
        Action<JusticeSystem> committed)
    {
        if (admission == null || committed == null) return false;
        if (!p12AReceiptStateInstalled && P12P18ReceiptCensusRevision != 0L) return false;
        return justiceRecordsOwner.TryBindP12MutationBoundary(
            () => admission(this),
            () => committed(this));
    }

    internal bool TryBindP12DailyProfileReceiptBoundary()
    {
        if (p12DailyProfileReceiptBoundaryBound) return true;
        if (!p12AReceiptStateInstalled && P12P18ReceiptCensusRevision != 0L) return false;
        p12DailyProfileReceiptBoundaryBound = true;
        return true;
    }

    private static IReadOnlyList<P12AReceiptValue> SnapshotReceipts<T>(
        Dictionary<string, T> receipts, Func<T, P12AReceiptValue> convert)
    {
        if (receipts == null || convert == null) throw new ArgumentNullException();
        List<string> identities = new List<string>(receipts.Keys);
        identities.Sort(StringComparer.Ordinal);
        List<P12AReceiptValue> result = new List<P12AReceiptValue>(identities.Count);
        foreach (string identity in identities)
        {
            if (string.IsNullOrWhiteSpace(identity) || !receipts.TryGetValue(identity, out T receipt)
                || ReferenceEquals(receipt, null)) throw new InvalidOperationException("Receipt owner map is invalid.");
            P12AReceiptValue value = convert(receipt);
            if (value == null || !string.Equals(identity, value.Identity, StringComparison.Ordinal))
                throw new InvalidOperationException("Receipt key does not match its immutable value.");
            result.Add(value);
        }
        return result.AsReadOnly();
    }

    private bool CanCommitP12CrimeJusticeMutation()
        => justiceRecordsOwner.CanCommitMutation();

    private void EnsureP12CrimeJusticeMutationAllowed()
    {
        if (!CanCommitP12CrimeJusticeMutation())
            throw new InvalidOperationException("Justice records are outside an admitted P12 Crime/Justice boundary.");
    }

    private void NotifyP12CrimeJusticeMutationCommitted()
        => justiceRecordsOwner.NotifyMutationCommitted();

    private bool RemoveP12PrisonSentenceAt(int index)
    {
        if (index < 0 || index >= prisonSentences.Count) return false;
        EnsureP12CrimeJusticeMutationAllowed();
        prisonSentences.RemoveAt(index);
        NotifyP12CrimeJusticeMutationCommitted();
        return true;
    }

    private bool RemoveP12PrisonSentence(PrisonSentenceRuntime sentence)
    {
        int index = prisonSentences.IndexOf(sentence);
        return index >= 0 && RemoveP12PrisonSentenceAt(index);
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        // The Core owner contains the authoritative rows and validates every
        // row's guard binding. Enumerating the Unity projection here can force
        // a reference projection before constructor-time owner rows have had
        // their runtime views materialized.
        return mutationGuardBinding.CanBindTo(guard)
            && justiceRecordsOwner.CanBindMutationGuard(guard);
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard)
            && mutationGuardBinding.TryBindTo(guard)
            && justiceRecordsOwner.TryBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard);
    }

    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return TryBindMutationGuard(guard);
    }

    private void ThrowIfFaulted()
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            throw new InvalidOperationException("Runtime mutation is faulted.");
        }
    }

    private PrisonSentenceRuntime GetActiveSentence(NpcRuntime targetRuntime)
    {
        if (targetRuntime == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(targetRuntime.RuntimeId))
        {
            foreach (PrisonSentenceRuntime sentence in prisonSentences)
                if (sentence != null && sentence.IsActive && ReferenceEquals(sentence.Target, targetRuntime))
                    return sentence;
            return null;
        }

        foreach (DailyV1JusticeSentenceOwner row in justiceRecordsOwner.FindActiveSentences(targetRuntime.RuntimeId))
            if (sentenceViews.TryGetValue(row, out PrisonSentenceRuntime projectedSentence)
                && projectedSentence.IsActive && ReferenceEquals(projectedSentence.Target, targetRuntime))
                return projectedSentence;
        return null;
    }

    private void ResolveWarrant(WantedRecordRuntime record)
    {
        if (record == null)
        {
            return;
        }

        record.Resolve();
        SyncWantedStatus(record.Target);
    }

    private void ReleasePrisonersWithoutActiveSentence(
        List<NpcRuntime> npcRuntimeList,
        List<JusticeAdvanceSentencesLogNotice> notices,
        IReadOnlyList<JusticeAdvanceSentencesLogNotice> rosterLogCandidates)
    {
        if (npcRuntimeList == null)
        {
            return;
        }

        for (int i = 0; i < npcRuntimeList.Count; i++)
        {
            NpcRuntime npcRuntime = npcRuntimeList[i];
            if (npcRuntime == null || IsArrested(npcRuntime) == false || GetActiveSentence(npcRuntime) != null)
            {
                continue;
            }

            ReleasePrisoner(npcRuntime);
            if (notices == null)
            {
                logger.LogWarning($"{npcRuntime.NpcName} estava preso sem sentenca ativa e foi libertado.");
            }
            else
            {
                notices.Add(rosterLogCandidates[i]);
            }
        }
    }

    private void ReleasePrisoner(NpcRuntime targetRuntime)
    {
        if (targetRuntime == null)
        {
            return;
        }

        targetRuntime.RemoveStatus(arrestedStatus);
        targetRuntime.AddStatus(freeStatus);
        targetRuntime.ClearHidden();
        targetRuntime.RemoveStatus(hiddenStatus);
        SyncWantedStatus(targetRuntime);
    }

    private void RecordNpcEscaped(NpcRuntime targetRuntime, CityRuntime cityRuntime, string originDecisionId)
    {
        domainEventRecorder?.Record((eventId, absoluteDay, recordSequence) => new NpcEscapedEvent(
            eventId,
            absoluteDay,
            recordSequence,
            targetRuntime?.RuntimeId,
            cityRuntime?.Location?.RuntimeId,
            originDecisionId));
    }
}

[Serializable]
public sealed class JusticeBeginDayReceipt
{
    public string ExecutionStepIdentity { get; }
    public long OwnerRevisionBefore { get; }
    public long OwnerRevisionAfter { get; }
    internal string DescriptorFingerprint { get; }

    internal JusticeBeginDayReceipt(
        string executionStepIdentity,
        string descriptorFingerprint,
        long ownerRevisionBefore,
        long ownerRevisionAfter)
    {
        ExecutionStepIdentity = executionStepIdentity ?? throw new ArgumentNullException(nameof(executionStepIdentity));
        DescriptorFingerprint = descriptorFingerprint ?? throw new ArgumentNullException(nameof(descriptorFingerprint));
        OwnerRevisionBefore = ownerRevisionBefore;
        OwnerRevisionAfter = ownerRevisionAfter;
    }
}

internal sealed class JusticeBeginDaySentenceSnapshot
{
    public PrisonSentenceRuntime Sentence { get; }
    public bool WasArrestedToday { get; }

    public JusticeBeginDaySentenceSnapshot(
        PrisonSentenceRuntime sentence,
        bool wasArrestedToday)
    {
        Sentence = sentence;
        WasArrestedToday = wasArrestedToday;
    }
}

internal sealed class JusticeBeginDayCommit : IBoundaryContinuationStepCommit
{
    private readonly JusticeSystem owner;
    private readonly JusticeBeginDayReceipt receipt;
    private readonly IReadOnlyList<JusticeBeginDaySentenceSnapshot> sentences;
    private readonly bool replay;
    private readonly string fingerprint;
    private readonly string expectedOwnerRevision;
    private readonly long expectedBeginDayRevision;
    private readonly Dictionary<string, JusticeBeginDayReceipt> expectedReceipts;
    private readonly Dictionary<string, JusticeBeginDayReceipt> nextReceipts;
    private bool completed;

    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
    public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();
    public JusticeBeginDayReceipt Receipt => receipt;

    public JusticeBeginDayCommit(
        JusticeSystem owner,
        JusticeBeginDayReceipt receipt,
        IReadOnlyList<JusticeBeginDaySentenceSnapshot> sentences,
        bool replay,
        string fingerprint,
        string expectedOwnerRevision,
        long expectedBeginDayRevision,
        Dictionary<string, JusticeBeginDayReceipt> expectedReceipts,
        Dictionary<string, JusticeBeginDayReceipt> nextReceipts)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
        this.sentences = sentences ?? Array.Empty<JusticeBeginDaySentenceSnapshot>();
        this.replay = replay;
        this.fingerprint = fingerprint ?? string.Empty;
        this.expectedOwnerRevision = expectedOwnerRevision ?? string.Empty;
        this.expectedBeginDayRevision = expectedBeginDayRevision;
        this.expectedReceipts = expectedReceipts;
        this.nextReceipts = nextReceipts;
    }

    public bool TryCommit(out TimelineFailure failure)
    {
        if (completed)
        {
            failure = TimelineFailure.None;
            return true;
        }

        completed = owner.TryCommitBeginDayStep(
            receipt,
            sentences,
            replay,
            fingerprint,
            expectedOwnerRevision,
            expectedBeginDayRevision,
            expectedReceipts,
            nextReceipts,
            out failure);
        return completed;
    }
}

[Serializable]
public sealed class JusticeSyncWantedStatusesReceipt
{
    public string ExecutionStepIdentity { get; }
    public long OwnerRevisionBefore { get; }
    public long OwnerRevisionAfter { get; }
    internal string DescriptorFingerprint { get; }

    internal JusticeSyncWantedStatusesReceipt(
        string executionStepIdentity,
        string descriptorFingerprint,
        long ownerRevisionBefore,
        long ownerRevisionAfter)
    {
        ExecutionStepIdentity = executionStepIdentity ?? throw new ArgumentNullException(nameof(executionStepIdentity));
        DescriptorFingerprint = descriptorFingerprint ?? throw new ArgumentNullException(nameof(descriptorFingerprint));
        OwnerRevisionBefore = ownerRevisionBefore;
        OwnerRevisionAfter = ownerRevisionAfter;
    }
}

internal sealed class JusticeSyncWantedStatusesSnapshot
{
    public IReadOnlyList<JusticeSyncWantedRosterSnapshot> Roster { get; }
    public IReadOnlyList<JusticeSyncWantedRecordSnapshot> Records { get; }

    public JusticeSyncWantedStatusesSnapshot(
        IReadOnlyList<JusticeSyncWantedRosterSnapshot> roster,
        IReadOnlyList<JusticeSyncWantedRecordSnapshot> records)
    {
        Roster = roster ?? Array.Empty<JusticeSyncWantedRosterSnapshot>();
        Records = records ?? Array.Empty<JusticeSyncWantedRecordSnapshot>();
    }
}

internal sealed class JusticeSyncWantedRosterSnapshot
{
    public NpcRuntime Npc { get; }
    public string RuntimeId { get; }
    public string PersonId { get; }
    public bool HasActiveWarrant { get; }
    public int WantedMarkerCount { get; }

    public JusticeSyncWantedRosterSnapshot(
        NpcRuntime npc, string runtimeId, string personId, bool hasActiveWarrant, int wantedMarkerCount)
    {
        Npc = npc;
        RuntimeId = runtimeId ?? string.Empty;
        PersonId = personId ?? string.Empty;
        HasActiveWarrant = hasActiveWarrant;
        WantedMarkerCount = wantedMarkerCount;
    }
}

internal sealed class JusticeSyncWantedRecordSnapshot
{
    public WantedRecordRuntime Record { get; }
    public NpcRuntime Target { get; }
    public string TargetRuntimeId { get; }
    public string TargetPersonId { get; }
    public bool IsActive { get; }

    public JusticeSyncWantedRecordSnapshot(
        WantedRecordRuntime record, NpcRuntime target, string targetRuntimeId, string targetPersonId, bool isActive)
    {
        Record = record;
        Target = target;
        TargetRuntimeId = targetRuntimeId ?? string.Empty;
        TargetPersonId = targetPersonId ?? string.Empty;
        IsActive = isActive;
    }
}

internal sealed class JusticeSyncWantedStatusesCommit : IBoundaryContinuationStepCommit
{
    private readonly JusticeSystem owner;
    private readonly JusticeSyncWantedStatusesReceipt receipt;
    private readonly List<NpcRuntime> npcRuntimeList;
    private readonly JusticeSyncWantedStatusesSnapshot snapshot;
    private readonly bool replay;
    private readonly string fingerprint;
    private readonly string expectedOwnerRevision;
    private readonly long expectedRevision;
    private readonly Dictionary<string, JusticeSyncWantedStatusesReceipt> expectedReceipts;
    private readonly Dictionary<string, JusticeSyncWantedStatusesReceipt> nextReceipts;
    private bool completed;

    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
    public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();
    public JusticeSyncWantedStatusesReceipt Receipt => receipt;

    public JusticeSyncWantedStatusesCommit(
        JusticeSystem owner,
        JusticeSyncWantedStatusesReceipt receipt,
        List<NpcRuntime> npcRuntimeList,
        JusticeSyncWantedStatusesSnapshot snapshot,
        bool replay,
        string fingerprint,
        string expectedOwnerRevision,
        long expectedRevision,
        Dictionary<string, JusticeSyncWantedStatusesReceipt> expectedReceipts,
        Dictionary<string, JusticeSyncWantedStatusesReceipt> nextReceipts)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
        this.npcRuntimeList = npcRuntimeList;
        this.snapshot = snapshot;
        this.replay = replay;
        this.fingerprint = fingerprint ?? string.Empty;
        this.expectedOwnerRevision = expectedOwnerRevision ?? string.Empty;
        this.expectedRevision = expectedRevision;
        this.expectedReceipts = expectedReceipts;
        this.nextReceipts = nextReceipts;
    }

    public bool TryCommit(out TimelineFailure failure)
    {
        if (completed)
        {
            failure = TimelineFailure.None;
            return true;
        }

        completed = owner.TryCommitSyncWantedStatusesStep(
            receipt, npcRuntimeList, snapshot, replay, fingerprint, expectedOwnerRevision,
            expectedRevision, expectedReceipts, nextReceipts, out failure);
        return completed;
    }
}

[Serializable]
public sealed class JusticeAdvanceSentencesReceipt
{
    public string ExecutionStepIdentity { get; }
    public long OwnerRevisionBefore { get; }
    public long OwnerRevisionAfter { get; }
    internal string DescriptorFingerprint { get; }

    internal JusticeAdvanceSentencesReceipt(
        string executionStepIdentity,
        string descriptorFingerprint,
        long ownerRevisionBefore,
        long ownerRevisionAfter)
    {
        ExecutionStepIdentity = executionStepIdentity ?? throw new ArgumentNullException(nameof(executionStepIdentity));
        DescriptorFingerprint = descriptorFingerprint ?? throw new ArgumentNullException(nameof(descriptorFingerprint));
        OwnerRevisionBefore = ownerRevisionBefore;
        OwnerRevisionAfter = ownerRevisionAfter;
    }
}

internal sealed class JusticeAdvanceSentencesSnapshot
{
    public IReadOnlyList<JusticeAdvanceSentencesRosterSnapshot> Roster { get; }
    public IReadOnlyList<JusticeAdvanceSentencesSentenceSnapshot> Sentences { get; }
    public IReadOnlyList<JusticeAdvanceSentencesWantedSnapshot> WantedRecords { get; }

    public JusticeAdvanceSentencesSnapshot(
        IReadOnlyList<JusticeAdvanceSentencesRosterSnapshot> roster,
        IReadOnlyList<JusticeAdvanceSentencesSentenceSnapshot> sentences,
        IReadOnlyList<JusticeAdvanceSentencesWantedSnapshot> wantedRecords)
    {
        Roster = roster ?? Array.Empty<JusticeAdvanceSentencesRosterSnapshot>();
        Sentences = sentences ?? Array.Empty<JusticeAdvanceSentencesSentenceSnapshot>();
        WantedRecords = wantedRecords ?? Array.Empty<JusticeAdvanceSentencesWantedSnapshot>();
    }
}

internal sealed class JusticeAdvanceSentencesRosterSnapshot
{
    public NpcRuntime Npc { get; }
    public string RuntimeId { get; }
    public string PersonId { get; }
    public int HiddenDaysRemaining { get; }
    public int ArrestedStatusCount { get; }
    public int FreeStatusCount { get; }
    public int WantedStatusCount { get; }
    public int HiddenStatusCount { get; }

    public JusticeAdvanceSentencesRosterSnapshot(
        NpcRuntime npc,
        string runtimeId,
        string personId,
        int hiddenDaysRemaining,
        int arrestedStatusCount,
        int freeStatusCount,
        int wantedStatusCount,
        int hiddenStatusCount)
    {
        Npc = npc;
        RuntimeId = runtimeId ?? string.Empty;
        PersonId = personId ?? string.Empty;
        HiddenDaysRemaining = hiddenDaysRemaining;
        ArrestedStatusCount = arrestedStatusCount;
        FreeStatusCount = freeStatusCount;
        WantedStatusCount = wantedStatusCount;
        HiddenStatusCount = hiddenStatusCount;
    }
}

internal sealed class JusticeAdvanceSentencesSentenceSnapshot
{
    public PrisonSentenceRuntime Sentence { get; }
    public NpcRuntime Target { get; }
    public CityRuntime City { get; }
    public WantedRecordRuntime Warrant { get; }
    public string TargetRuntimeId { get; }
    public string TargetPersonId { get; }
    public string CityRuntimeId { get; }
    public string WarrantTargetRuntimeId { get; }
    public string WarrantTargetPersonId { get; }
    public string WarrantCityRuntimeId { get; }
    public int RemainingDays { get; }
    public int TargetHiddenDaysRemaining { get; }
    public int TargetArrestedStatusCount { get; }
    public int TargetFreeStatusCount { get; }
    public int TargetWantedStatusCount { get; }
    public int TargetHiddenStatusCount { get; }
    public bool WarrantActive { get; }

    public JusticeAdvanceSentencesSentenceSnapshot(
        PrisonSentenceRuntime sentence,
        NpcRuntime target,
        CityRuntime city,
        WantedRecordRuntime warrant,
        string targetRuntimeId,
        string targetPersonId,
        string cityRuntimeId,
        string warrantTargetRuntimeId,
        string warrantTargetPersonId,
        string warrantCityRuntimeId,
        int remainingDays,
        int targetHiddenDaysRemaining,
        int targetArrestedStatusCount,
        int targetFreeStatusCount,
        int targetWantedStatusCount,
        int targetHiddenStatusCount,
        bool warrantActive)
    {
        Sentence = sentence;
        Target = target;
        City = city;
        Warrant = warrant;
        TargetRuntimeId = targetRuntimeId ?? string.Empty;
        TargetPersonId = targetPersonId ?? string.Empty;
        CityRuntimeId = cityRuntimeId ?? string.Empty;
        WarrantTargetRuntimeId = warrantTargetRuntimeId ?? string.Empty;
        WarrantTargetPersonId = warrantTargetPersonId ?? string.Empty;
        WarrantCityRuntimeId = warrantCityRuntimeId ?? string.Empty;
        RemainingDays = remainingDays;
        TargetHiddenDaysRemaining = targetHiddenDaysRemaining;
        TargetArrestedStatusCount = targetArrestedStatusCount;
        TargetFreeStatusCount = targetFreeStatusCount;
        TargetWantedStatusCount = targetWantedStatusCount;
        TargetHiddenStatusCount = targetHiddenStatusCount;
        WarrantActive = warrantActive;
    }
}

internal sealed class JusticeAdvanceSentencesWantedSnapshot
{
    public WantedRecordRuntime Record { get; }
    public NpcRuntime Target { get; }
    public CityRuntime City { get; }
    public string TargetRuntimeId { get; }
    public string TargetPersonId { get; }
    public string CityRuntimeId { get; }
    public bool IsActive { get; }

    public JusticeAdvanceSentencesWantedSnapshot(
        WantedRecordRuntime record,
        NpcRuntime target,
        CityRuntime city,
        string targetRuntimeId,
        string targetPersonId,
        string cityRuntimeId,
        bool isActive)
    {
        Record = record;
        Target = target;
        City = city;
        TargetRuntimeId = targetRuntimeId ?? string.Empty;
        TargetPersonId = targetPersonId ?? string.Empty;
        CityRuntimeId = cityRuntimeId ?? string.Empty;
        IsActive = isActive;
    }
}

internal sealed class JusticeAdvanceSentencesLogNotice
{
    public bool IsWarning { get; }
    public string Message { get; }

    public JusticeAdvanceSentencesLogNotice(bool isWarning, string message)
    {
        IsWarning = isWarning;
        Message = message ?? string.Empty;
    }
}

internal sealed class JusticeAdvanceSentencesSentenceLogCandidates
{
    public JusticeAdvanceSentencesLogNotice InactiveWarrantNotice { get; }
    public JusticeAdvanceSentencesLogNotice ExpiredSentenceNotice { get; }

    public JusticeAdvanceSentencesSentenceLogCandidates(
        JusticeAdvanceSentencesLogNotice inactiveWarrantNotice,
        JusticeAdvanceSentencesLogNotice expiredSentenceNotice)
    {
        InactiveWarrantNotice = inactiveWarrantNotice;
        ExpiredSentenceNotice = expiredSentenceNotice;
    }
}

internal sealed class JusticeAdvanceSentencesCommit : IBoundaryContinuationStepCommit
{
    private readonly JusticeSystem owner;
    private readonly JusticeAdvanceSentencesReceipt receipt;
    private readonly List<NpcRuntime> npcRuntimeList;
    private readonly JusticeAdvanceSentencesSnapshot snapshot;
    private readonly List<JusticeAdvanceSentencesLogNotice> notices;
    private readonly bool replay;
    private readonly string fingerprint;
    private readonly string expectedOwnerRevision;
    private readonly long expectedRevision;
    private readonly Dictionary<string, JusticeAdvanceSentencesReceipt> expectedReceipts;
    private readonly Dictionary<string, JusticeAdvanceSentencesReceipt> nextReceipts;
    private bool completed;

    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
    public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();
    public JusticeAdvanceSentencesReceipt Receipt => receipt;

    public JusticeAdvanceSentencesCommit(
        JusticeSystem owner,
        JusticeAdvanceSentencesReceipt receipt,
        List<NpcRuntime> npcRuntimeList,
        JusticeAdvanceSentencesSnapshot snapshot,
        List<JusticeAdvanceSentencesLogNotice> notices,
        bool replay,
        string fingerprint,
        string expectedOwnerRevision,
        long expectedRevision,
        Dictionary<string, JusticeAdvanceSentencesReceipt> expectedReceipts,
        Dictionary<string, JusticeAdvanceSentencesReceipt> nextReceipts)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
        this.npcRuntimeList = npcRuntimeList;
        this.snapshot = snapshot;
        this.notices = notices;
        this.replay = replay;
        this.fingerprint = fingerprint ?? string.Empty;
        this.expectedOwnerRevision = expectedOwnerRevision ?? string.Empty;
        this.expectedRevision = expectedRevision;
        this.expectedReceipts = expectedReceipts;
        this.nextReceipts = nextReceipts;
    }

    public bool TryCommit(out TimelineFailure failure)
    {
        if (completed)
        {
            failure = TimelineFailure.None;
            return true;
        }

        completed = owner.TryCommitAdvanceSentencesStep(
            receipt,
            npcRuntimeList,
            snapshot,
            notices,
            replay,
            fingerprint,
            expectedOwnerRevision,
            expectedRevision,
            expectedReceipts,
            nextReceipts,
            out failure);
        return completed;
    }
}

[Serializable]
public class WantedRecordRuntime : IAuthoritativeMutationGuardBindable
{
    [NonSerialized] private NpcRuntime target;
    [NonSerialized] private CityRuntime city;
    [NonSerialized] private DailyV1JusticeWantedRecordOwner coreOwner;

    public NpcRuntime Target => target;
    public CityRuntime City => city;
    public float Bounty => coreOwner?.Bounty ?? 0f;
    public int SentenceDays => coreOwner?.SentenceDays ?? 0;
    public bool IsActive => coreOwner != null && !coreOwner.IsResolved && target != null && city != null;
    internal bool P12Resolved => coreOwner?.IsResolved ?? false;
    internal DailyV1JusticeWantedRecordOwner CoreOwner => coreOwner;

    public WantedRecordRuntime(NpcRuntime target, CityRuntime city, float bounty, int sentenceDays)
    {
        this.target = target;
        this.city = city;
        coreOwner = new DailyV1JusticeWantedRecordOwner(
            target?.RuntimeId, city?.RuntimeId, bounty, sentenceDays);
    }

    private WantedRecordRuntime(
        NpcRuntime target,
        CityRuntime city,
        DailyV1JusticeWantedRecordOwner coreOwner)
    {
        this.target = target;
        this.city = city;
        this.coreOwner = coreOwner ?? throw new ArgumentNullException(nameof(coreOwner));
    }

    internal static bool TryCreateFromP12EOwnerSnapshot(
        NpcRuntime target,
        CityRuntime city,
        float bounty,
        int sentenceDays,
        bool resolved,
        out WantedRecordRuntime staged)
    {
        staged = null;
        if (target == null || city == null || float.IsNaN(bounty) || float.IsInfinity(bounty)
            || bounty < 0f || sentenceDays < 1)
            return false;
        WantedRecordRuntime candidate = new WantedRecordRuntime(target, city,
            new DailyV1JusticeWantedRecordOwner(target.RuntimeId, city.RuntimeId,
                bounty, sentenceDays, resolved));
        staged = candidate;
        return true;
    }

    internal static bool TryCreateFromCoreOwner(
        NpcRuntime target,
        CityRuntime city,
        DailyV1JusticeWantedRecordOwner coreOwner,
        out WantedRecordRuntime view)
    {
        view = null;
        if (target == null || city == null || coreOwner == null
            || !string.Equals(target.RuntimeId, coreOwner.TargetRuntimeId, StringComparison.Ordinal)
            || !string.Equals(city.RuntimeId, coreOwner.CityRuntimeId, StringComparison.Ordinal))
            return false;
        view = new WantedRecordRuntime(target, city, coreOwner);
        return true;
    }

    public void AddPenalty(float additionalBounty, int additionalSentenceDays)
    {
        if (coreOwner == null || !coreOwner.TryAddPenalty(additionalBounty, additionalSentenceDays))
            throw new InvalidOperationException("Wanted record is outside an admitted P12 Justice boundary.");
    }

    public void Resolve()
    {
        if (coreOwner == null || !coreOwner.TryResolve())
            throw new InvalidOperationException("Wanted record is outside an admitted P12 Justice boundary.");
    }

    internal bool TryBindP12MutationBoundary(Func<bool> admission, Action committed)
        => coreOwner != null && coreOwner.TryBindP12MutationBoundary(admission, committed);

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => coreOwner != null && coreOwner.CanBindMutationGuard(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => coreOwner != null && coreOwner.TryBindMutationGuard(guard);

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}

[Serializable]
public class PrisonSentenceRuntime : IAuthoritativeMutationGuardBindable
{
    [NonSerialized] private NpcRuntime target;
    [NonSerialized] private CityRuntime city;
    [NonSerialized] private WantedRecordRuntime warrant;
    [NonSerialized] private DailyV1JusticeSentenceOwner coreOwner;

    public NpcRuntime Target => target;
    public CityRuntime City => city;
    public WantedRecordRuntime Warrant => warrant;
    public int RemainingDays => coreOwner?.RemainingDays ?? 0;
    public int FailedEscapeAttempts => coreOwner?.FailedEscapeAttempts ?? 0;
    public bool WasArrestedToday => coreOwner?.WasArrestedToday ?? false;
    public bool IsActive => target != null && city != null && coreOwner?.IsActive == true;
    internal DailyV1JusticeSentenceOwner CoreOwner => coreOwner;

    public PrisonSentenceRuntime(NpcRuntime target, CityRuntime city, WantedRecordRuntime warrant, int sentenceDays)
    {
        this.target = target;
        this.city = city;
        this.warrant = warrant;
        coreOwner = new DailyV1JusticeSentenceOwner(
            target?.RuntimeId, city?.RuntimeId, warrant?.CoreOwner,
            UnityMathfCompatibility.Max(1, sentenceDays));
    }

    private PrisonSentenceRuntime(
        NpcRuntime target,
        CityRuntime city,
        WantedRecordRuntime warrant,
        DailyV1JusticeSentenceOwner coreOwner)
    {
        this.target = target;
        this.city = city;
        this.warrant = warrant;
        this.coreOwner = coreOwner ?? throw new ArgumentNullException(nameof(coreOwner));
    }

    internal static bool TryCreateFromP12EOwnerSnapshot(
        NpcRuntime target,
        CityRuntime city,
        WantedRecordRuntime warrant,
        int remainingDays,
        int failedEscapeAttempts,
        bool wasArrestedToday,
        out PrisonSentenceRuntime staged)
    {
        staged = null;
        if (target == null || city == null || warrant == null || remainingDays < 0
            || failedEscapeAttempts < 0 || !ReferenceEquals(target, warrant.Target)
            || !ReferenceEquals(city, warrant.City))
            return false;
        PrisonSentenceRuntime candidate = new PrisonSentenceRuntime(target, city, warrant,
            new DailyV1JusticeSentenceOwner(target.RuntimeId, city.RuntimeId,
                warrant.CoreOwner, remainingDays, failedEscapeAttempts, wasArrestedToday));
        staged = candidate;
        return true;
    }

    internal static bool TryCreateSelectedProjection(
        NpcRuntime target,
        CityRuntime city,
        WantedRecordRuntime warrant,
        DailyV1JusticeSentenceOwner coreOwner,
        out PrisonSentenceRuntime projected)
    {
        projected = null;
        if (target == null || city == null || warrant == null || coreOwner == null
            || !ReferenceEquals(warrant.Target, target) || !ReferenceEquals(warrant.City, city)
            || !ReferenceEquals(coreOwner.Warrant, warrant.CoreOwner)
            || !string.Equals(coreOwner.TargetRuntimeId, target.RuntimeId, StringComparison.Ordinal)
            || !string.Equals(coreOwner.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal))
            return false;
        projected = new PrisonSentenceRuntime(target, city, warrant, coreOwner);
        return true;
    }

    public void AdvanceDay()
    {
        if (coreOwner == null || !coreOwner.TryAdvanceDay())
            throw new InvalidOperationException("Prison sentence is outside an admitted P12 Justice boundary.");
    }

    public void RegisterFailedEscape(int additionalSentenceDays)
    {
        if (coreOwner == null || !coreOwner.TryRegisterFailedEscape(additionalSentenceDays))
            throw new InvalidOperationException("Prison sentence is outside an admitted P12 Justice boundary.");
    }

    public void ClearArrestedToday()
    {
        if (coreOwner == null || !coreOwner.TryClearArrestedToday())
            throw new InvalidOperationException("Prison sentence is outside an admitted P12 Justice boundary.");
    }

    internal bool TryBindP12MutationBoundary(Func<bool> admission, Action committed)
        => coreOwner != null && coreOwner.TryBindP12MutationBoundary(admission, committed);

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => coreOwner != null && coreOwner.CanBindMutationGuard(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => coreOwner != null && coreOwner.TryBindMutationGuard(guard);

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}

/// <summary>
/// Compatibility projection over the Core-owned ordered Justice rows. The
/// adapter retains Unity references and object-facing APIs, while list order
/// and row values remain in the shared owner.
/// </summary>
internal sealed class JusticeOwnerProjectionList<TCore, TView> : IList<TView>
    where TCore : class
    where TView : class
{
    private readonly Func<IReadOnlyList<TCore>> getRows;
    private readonly Func<TCore, TView> project;
    private readonly Func<TView, TCore> getOwner;
    private readonly Action<TView> add;
    private readonly Action<int> removeAt;

    internal JusticeOwnerProjectionList(
        Func<IReadOnlyList<TCore>> getRows,
        Func<TCore, TView> project,
        Func<TView, TCore> getOwner,
        Action<TView> add,
        Action<int> removeAt)
    {
        this.getRows = getRows ?? throw new ArgumentNullException(nameof(getRows));
        this.project = project ?? throw new ArgumentNullException(nameof(project));
        this.getOwner = getOwner ?? throw new ArgumentNullException(nameof(getOwner));
        this.add = add ?? throw new ArgumentNullException(nameof(add));
        this.removeAt = removeAt;
    }

    public TView this[int index]
    {
        get => project(getRows()[index]);
        set => throw new NotSupportedException("Justice row replacement must use an owner operation.");
    }

    public int Count => getRows().Count;
    public bool IsReadOnly => false;
    public void Add(TView item) => add(item);
    public void Clear() => throw new NotSupportedException("Justice owner rows cannot be cleared through a projection.");
    public bool Contains(TView item) => IndexOf(item) >= 0;

    public void CopyTo(TView[] array, int arrayIndex)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex > array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (array.Length - arrayIndex < Count) throw new ArgumentException("The destination array is too small.");
        for (int i = 0; i < Count; i++) array[arrayIndex + i] = this[i];
    }

    public IEnumerator<TView> GetEnumerator()
    {
        for (int i = 0; i < Count; i++) yield return this[i];
    }

    public int IndexOf(TView item)
    {
        TCore owner = item == null ? null : getOwner(item);
        if (owner == null) return -1;
        IReadOnlyList<TCore> rows = getRows();
        for (int i = 0; i < rows.Count; i++)
            if (ReferenceEquals(rows[i], owner)) return i;
        return -1;
    }

    public void Insert(int index, TView item) =>
        throw new NotSupportedException("Justice row order is owned by Core operations.");

    public bool Remove(TView item)
    {
        int index = IndexOf(item);
        if (index < 0) return false;
        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        if (removeAt == null) throw new NotSupportedException("Justice row removal is not supported through this projection.");
        removeAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
