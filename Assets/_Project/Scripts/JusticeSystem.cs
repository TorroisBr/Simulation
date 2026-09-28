using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class JusticeSystem : IAuthoritativeMutationGuardBindable
{
    private const string BeginDayStepId = "justice-begin-day";
    private const string BeginDayOwnerId = "justice";
    private const string BeginDayOperationKind = "justice.begin-day";
    private const string BeginDayOperationVersion = "1";
    private const string BeginDaySnapshotVersion = "justice-begin-day-owner-v1";
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly List<WantedRecordRuntime> wantedRecords = new List<WantedRecordRuntime>();
    private readonly List<PrisonSentenceRuntime> prisonSentences = new List<PrisonSentenceRuntime>();
    private Dictionary<string, JusticeBeginDayReceipt> beginDayStepReceipts =
        new Dictionary<string, JusticeBeginDayReceipt>(StringComparer.Ordinal);
    private long beginDayStepRevision;
    private readonly NpcStatusData freeStatus;
    private readonly NpcStatusData wantedStatus;
    private readonly NpcStatusData arrestedStatus;
    private readonly NpcStatusData hiddenStatus;
    private readonly DomainEventRecorder domainEventRecorder;
    private readonly SimulationLogger logger;

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
    {
        this.freeStatus = freeStatus;
        this.wantedStatus = wantedStatus;
        this.arrestedStatus = arrestedStatus;
        this.hiddenStatus = hiddenStatus;
        this.domainEventRecorder = domainEventRecorder;
        this.logger = logger ?? new SimulationLogger(null);
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

        string executionStepIdentity = manifest.GetExecutionStepIdentity(step);
        JusticeBeginDayReceipt receipt = new JusticeBeginDayReceipt(
            executionStepIdentity,
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

        identity = manifest.GetExecutionStepIdentity(step);
        fingerprint = SpatialStableKey.Encode(
            manifest.BoundaryOccurrenceId,
            manifest.ContinuationId,
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

            wantedRecords.Add(record);
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

        prisonSentences.Add(sentence);
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

    public void AdvanceSentences(List<NpcRuntime> npcRuntimeList)
    {
        ThrowIfFaulted();

        for (int i = prisonSentences.Count - 1; i >= 0; i--)
        {
            PrisonSentenceRuntime sentence = prisonSentences[i];

            if (sentence == null || sentence.Target == null)
            {
                prisonSentences.RemoveAt(i);
                continue;
            }

            if (sentence.Warrant == null || sentence.Warrant.IsActive == false)
            {
                ReleasePrisoner(sentence.Target);
                prisonSentences.RemoveAt(i);
                logger.Log(SimulationLogCategory.Justice, $"{sentence.Target.NpcName} foi libertado porque seu mandado nao esta mais ativo.");
                continue;
            }

            if (IsArrested(sentence.Target) == false)
            {
                prisonSentences.RemoveAt(i);
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
            prisonSentences.RemoveAt(i);
            logger.Log(SimulationLogCategory.Justice, $"{sentence.Target.NpcName} cumpriu sua pena em {sentence.City.CityName} e foi libertado.");
        }

        ReleasePrisonersWithoutActiveSentence(npcRuntimeList);
        SyncWantedStatuses(npcRuntimeList);
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

        prisonSentences.Remove(sentence);
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
        foreach (WantedRecordRuntime record in wantedRecords)
        {
            if (record != null && record.IsActive == true && record.Target == targetRuntime)
            {
                return true;
            }
        }

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
        return targetRuntime != null && arrestedStatus != null && targetRuntime.CurrentStatus.Contains(arrestedStatus) == true;
    }

    public WantedRecordRuntime GetActiveWarrant(NpcRuntime targetRuntime, CityRuntime cityRuntime)
    {
        if (targetRuntime == null || cityRuntime == null)
        {
            return null;
        }

        foreach (WantedRecordRuntime record in wantedRecords)
        {
            if (record != null && record.IsActive == true && record.Target == targetRuntime && record.City == cityRuntime)
            {
                return record;
            }
        }

        return null;
    }

    public List<WantedRecordRuntime> GetActiveWarrants(NpcRuntime targetRuntime)
    {
        List<WantedRecordRuntime> records = new List<WantedRecordRuntime>();

        if (targetRuntime == null)
        {
            return records;
        }

        foreach (WantedRecordRuntime record in wantedRecords)
        {
            if (record != null && record.IsActive == true && record.Target == targetRuntime)
            {
                records.Add(record);
            }
        }

        return records;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        if (mutationGuardBinding.CanBindTo(guard) == false)
        {
            return false;
        }

        foreach (WantedRecordRuntime record in wantedRecords)
        {
            if (record != null && record.CanBindMutationGuard(guard) == false)
            {
                return false;
            }
        }

        foreach (PrisonSentenceRuntime sentence in prisonSentences)
        {
            if (sentence != null && sentence.CanBindMutationGuard(guard) == false)
            {
                return false;
            }
        }

        return true;
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        if (CanBindMutationGuard(guard) == false || mutationGuardBinding.TryBindTo(guard) == false)
        {
            return false;
        }

        foreach (WantedRecordRuntime record in wantedRecords)
        {
            if (record != null && record.TryBindMutationGuard(guard) == false)
            {
                return false;
            }
        }

        foreach (PrisonSentenceRuntime sentence in prisonSentences)
        {
            if (sentence != null && sentence.TryBindMutationGuard(guard) == false)
            {
                return false;
            }
        }

        return true;
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

        foreach (PrisonSentenceRuntime sentence in prisonSentences)
        {
            if (sentence != null && sentence.IsActive == true && sentence.Target == targetRuntime)
            {
                return sentence;
            }
        }

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

    private void ReleasePrisonersWithoutActiveSentence(List<NpcRuntime> npcRuntimeList)
    {
        if (npcRuntimeList == null)
        {
            return;
        }

        foreach (NpcRuntime npcRuntime in npcRuntimeList)
        {
            if (npcRuntime == null || IsArrested(npcRuntime) == false || GetActiveSentence(npcRuntime) != null)
            {
                continue;
            }

            ReleasePrisoner(npcRuntime);
            logger.LogWarning($"{npcRuntime.NpcName} estava preso sem sentenca ativa e foi libertado.");
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
public class WantedRecordRuntime : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    [NonSerialized] private NpcRuntime target;
    [NonSerialized] private CityRuntime city;
    [SerializeField] private float bounty;
    [SerializeField] private int sentenceDays;
    [SerializeField] private bool resolved;

    public NpcRuntime Target => target;
    public CityRuntime City => city;
    public float Bounty => bounty;
    public int SentenceDays => sentenceDays;
    public bool IsActive => resolved == false && target != null && city != null;

    public WantedRecordRuntime(NpcRuntime target, CityRuntime city, float bounty, int sentenceDays)
    {
        this.target = target;
        this.city = city;
        this.bounty = Mathf.Max(0f, bounty);
        this.sentenceDays = Mathf.Max(1, sentenceDays);
    }

    public void AddPenalty(float additionalBounty, int additionalSentenceDays)
    {
        ThrowIfFaulted();

        bounty += Mathf.Max(0f, additionalBounty);
        sentenceDays += Mathf.Max(0, additionalSentenceDays);
        sentenceDays = Mathf.Max(1, sentenceDays);
    }

    public void Resolve()
    {
        ThrowIfFaulted();

        resolved = true;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);

    private void ThrowIfFaulted()
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            throw new InvalidOperationException("Runtime mutation is faulted.");
        }
    }
}

[Serializable]
public class PrisonSentenceRuntime : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    [NonSerialized] private NpcRuntime target;
    [NonSerialized] private CityRuntime city;
    [NonSerialized] private WantedRecordRuntime warrant;
    [SerializeField] private int remainingDays;
    [SerializeField] private int failedEscapeAttempts;
    [SerializeField] private bool wasArrestedToday;

    public NpcRuntime Target => target;
    public CityRuntime City => city;
    public WantedRecordRuntime Warrant => warrant;
    public int RemainingDays => remainingDays;
    public int FailedEscapeAttempts => failedEscapeAttempts;
    public bool WasArrestedToday => wasArrestedToday;
    public bool IsActive => target != null && city != null && remainingDays > 0;

    public PrisonSentenceRuntime(NpcRuntime target, CityRuntime city, WantedRecordRuntime warrant, int sentenceDays)
    {
        this.target = target;
        this.city = city;
        this.warrant = warrant;
        remainingDays = Mathf.Max(1, sentenceDays);
        wasArrestedToday = true;
    }

    public void AdvanceDay()
    {
        ThrowIfFaulted();

        remainingDays = Mathf.Max(0, remainingDays - 1);
    }

    public void RegisterFailedEscape(int additionalSentenceDays)
    {
        ThrowIfFaulted();

        failedEscapeAttempts++;
        remainingDays += Mathf.Max(0, additionalSentenceDays);
    }

    public void ClearArrestedToday()
    {
        ThrowIfFaulted();

        wasArrestedToday = false;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);

    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);

    private void ThrowIfFaulted()
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            throw new InvalidOperationException("Runtime mutation is faulted.");
        }
    }
}
