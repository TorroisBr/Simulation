using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class CrimeSystem : INpcActionProvider, INpcActionFailureHandler, IAutonomousNpcActionPolicy, IAuthoritativeMutationGuardBindable
{
    private const float EscapeSuccessMultiplierPerFailure = 0.8f;
    private const string HiddenStatusStepId = "crime-hidden-statuses";
    private const string HiddenStatusOwnerId = "crime";
    private const string HiddenStatusOperationKind = "crime.advance-hidden-statuses";
    private const string HiddenStatusOperationVersion = "1";
    private const string HiddenStatusSnapshotVersion = "crime-hidden-statuses-owner-v1";
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly JusticeSystem justiceSystem;
    private readonly TravelSystem travelSystem;
    private readonly NpcStatusData hiddenStatus;
    private readonly SimulationLogger logger;
    private readonly EconomyTransactionService transactionService;
    private readonly EffectiveCrimeConfiguration configuration;
    private readonly IAuthoritativeRandomSource randomSource;
    private Dictionary<string, CrimeHiddenStatusReceipt> hiddenStatusStepReceipts =
        new Dictionary<string, CrimeHiddenStatusReceipt>(StringComparer.Ordinal);
    private long hiddenStatusStepRevision;
    private SimulationTime simulationTime;
    private ITheftOutcomeSink theftOutcomeSink;

    public CrimeSystem(
        JusticeSystem justiceSystem,
        TravelSystem travelSystem,
        NpcStatusData hiddenStatus,
        SimulationLogger logger = null,
        EconomyTransactionService transactionService = null,
        EffectiveCrimeConfiguration configuration = null,
        IAuthoritativeRandomSource randomSource = null,
        SimulationTime simulationTime = null,
        ITheftOutcomeSink theftOutcomeSink = null)
    {
        this.justiceSystem = justiceSystem;
        this.travelSystem = travelSystem;
        this.hiddenStatus = hiddenStatus;
        this.logger = logger ?? new SimulationLogger(null);
        this.transactionService = transactionService ?? new EconomyTransactionService();
        this.configuration = configuration ?? new EffectiveCrimeConfiguration(true, true);
        this.randomSource = randomSource ?? new DeterministicRandomSource();
        this.simulationTime = simulationTime;
        this.theftOutcomeSink = theftOutcomeSink;
    }

    public bool AllowAutonomousAction(NpcActionData action)
    {
        return configuration.Enabled && configuration.AutonomousEnabled;
    }

    public bool TryBindTheftOutcomeSink(ITheftOutcomeSink sink)
    {
        if (sink == null || theftOutcomeSink != null)
        {
            return false;
        }

        theftOutcomeSink = sink;
        return true;
    }

    public ITheftOutcomeSink TheftOutcomeSink => theftOutcomeSink;
    public SimulationTime SimulationTime => simulationTime;

    public bool TryBindSimulationTime(SimulationTime worldSimulationTime)
    {
        if (worldSimulationTime == null)
        {
            return false;
        }

        if (simulationTime == null)
        {
            simulationTime = worldSimulationTime;
            return true;
        }

        return ReferenceEquals(simulationTime, worldSimulationTime);
    }

    public bool HandlesAction(NpcActionData action)
    {
        if (action == null)
        {
            return false;
        }

        return action.actionType == NpcActionType.Steal
            || action.actionType == NpcActionType.Hide
            || action.actionType == NpcActionType.FleeCity
            || action.actionType == NpcActionType.EscapePrison;
    }

    public NpcActionRuntime CreateAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            utility = 0f;
            return null;
        }

        if (action == null || configuration.Enabled == false)
        {
            utility = 0f;
            return null;
        }

        if (action.actionType == NpcActionType.Steal)
        {
            return CreateStealAction(npcRuntime, action, ref utility);
        }

        if (action.actionType == NpcActionType.Hide)
        {
            return CreateHideAction(npcRuntime, action, ref utility);
        }

        if (action.actionType == NpcActionType.FleeCity)
        {
            return CreateFleeCityAction(npcRuntime, action, ref utility);
        }

        if (action.actionType == NpcActionType.EscapePrison)
        {
            return CreateEscapePrisonAction(npcRuntime, action, ref utility);
        }

        utility = 0f;
        return null;
    }

    public NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            return NpcActionResult.Failed("Runtime mutation is faulted.");
        }

        if (configuration.Enabled == false || actionRuntime == null || actionRuntime.Action == null)
        {
            return NpcActionResult.Failed();
        }

        if (actionRuntime.Action.actionType == NpcActionType.Steal)
        {
            return TryExecuteSteal(npcRuntime, actionRuntime);
        }

        if (actionRuntime.Action.actionType == NpcActionType.Hide)
        {
            return TryExecuteHide(npcRuntime, actionRuntime);
        }

        if (actionRuntime.Action.actionType == NpcActionType.FleeCity)
        {
            return TryExecuteFleeCity(npcRuntime, actionRuntime);
        }

        if (actionRuntime.Action.actionType == NpcActionType.EscapePrison)
        {
            return TryExecuteEscapePrison(npcRuntime, actionRuntime);
        }

        return NpcActionResult.Failed();
    }

    public NpcActionResult HandleActionFailure(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (mutationGuardBinding.CanMutate == false)
        {
            return null;
        }

        if (actionRuntime == null || actionRuntime.Action == null || actionRuntime.Action.actionType != NpcActionType.EscapePrison)
        {
            return null;
        }

        CrimeActionSettings settings = GetCrimeSettings(actionRuntime.Action);
        int sentencePenalty = Mathf.Max(0, settings.failedEscapeSentencePenalty);

        if (justiceSystem == null || justiceSystem.RegisterFailedEscape(npcRuntime, sentencePenalty) == false)
        {
            return null;
        }

        logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} tentou fugir da prisao e falhou. Pena aumentou em {sentencePenalty} dias. Vigilancia aumentou.");
        return NpcActionResult.Failed();
    }

    public void AdvanceHiddenStatuses(List<NpcRuntime> npcRuntimeList)
    {
        ThrowIfFaulted();

        if (npcRuntimeList == null)
        {
            return;
        }

        foreach (NpcRuntime npcRuntime in npcRuntimeList)
        {
            if (npcRuntime == null)
            {
                continue;
            }

            if (npcRuntime.IsHidden == false)
            {
                npcRuntime.RemoveStatus(hiddenStatus);
                continue;
            }

            if (hiddenStatus != null && npcRuntime.CurrentStatus.Contains(hiddenStatus) == false)
            {
                npcRuntime.AddStatus(hiddenStatus);
            }

            if (npcRuntime.AdvanceHiddenDay() == true)
            {
                npcRuntime.RemoveStatus(hiddenStatus);
                logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} nao esta mais escondido.");
            }
        }
    }

    /// <summary>Captures the current crime-owned roster traversal as a frozen P18 boundary step.</summary>
    public bool TryCreateAdvanceHiddenStatusesStep(
        DailyBoundaryOperation operation,
        List<NpcRuntime> npcRuntimeList,
        int ordinal,
        out BoundaryContinuationStep step,
        out TimelineFailure failure)
    {
        step = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || npcRuntimeList == null || ordinal < 0
            || !mutationGuardBinding.CanMutate || hiddenStatusStepRevision == long.MaxValue
            || !TryCaptureHiddenStatusSnapshot(npcRuntimeList, out _, out string ownerRevision))
        {
            return false;
        }

        step = new BoundaryContinuationStep(
            ordinal,
            HiddenStatusStepId,
            HiddenStatusOwnerId,
            HiddenStatusOperationKind,
            HiddenStatusOperationVersion,
            ownerRevision,
            string.Empty);
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>Looks up an exact crime-owned occurrence receipt without reading the live roster.</summary>
    public bool TryResolveAdvanceHiddenStatusesReceipt(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out CrimeHiddenStatusReceipt receipt,
        out TimelineFailure failure)
    {
        receipt = null;
        if (!TryGetHiddenStatusIdentity(manifest, step, out string identity, out string fingerprint))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (hiddenStatusStepReceipts == null)
        {
            hiddenStatusStepReceipts = new Dictionary<string, CrimeHiddenStatusReceipt>(StringComparer.Ordinal);
        }

        if (!hiddenStatusStepReceipts.TryGetValue(identity, out CrimeHiddenStatusReceipt existing))
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

    /// <summary>Stages the legacy hidden-status traversal and its occurrence receipt as one owner commit.</summary>
    public bool TryPrepareAdvanceHiddenStatusesStep(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        List<NpcRuntime> npcRuntimeList,
        out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (!TryGetHiddenStatusIdentity(manifest, step, out string identity, out string fingerprint))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        if (hiddenStatusStepReceipts == null)
        {
            hiddenStatusStepReceipts = new Dictionary<string, CrimeHiddenStatusReceipt>(StringComparer.Ordinal);
        }

        if (hiddenStatusStepReceipts.TryGetValue(identity, out CrimeHiddenStatusReceipt existing))
        {
            if (!string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.ContinuationFailed;
                return false;
            }

            prepared = new CrimeHiddenStatusCommit(
                this,
                existing,
                null,
                null,
                true,
                fingerprint,
                step.OwnerRevision,
                hiddenStatusStepRevision,
                hiddenStatusStepReceipts,
                hiddenStatusStepReceipts);
            failure = TimelineFailure.None;
            return true;
        }

        if (npcRuntimeList == null || !mutationGuardBinding.CanMutate
            || hiddenStatusStepRevision == long.MaxValue
            || !TryCaptureHiddenStatusSnapshot(
                npcRuntimeList,
                out List<CrimeHiddenStatusSnapshotEntry> snapshots,
                out string currentOwnerRevision)
            || !string.Equals(currentOwnerRevision, step.OwnerRevision, StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        string executionStepIdentity = manifest.GetExecutionStepIdentity(step);
        CrimeHiddenStatusReceipt receipt = new CrimeHiddenStatusReceipt(
            executionStepIdentity,
            fingerprint,
            hiddenStatusStepRevision,
            hiddenStatusStepRevision + 1L);
        Dictionary<string, CrimeHiddenStatusReceipt> nextReceipts =
            new Dictionary<string, CrimeHiddenStatusReceipt>(hiddenStatusStepReceipts, StringComparer.Ordinal)
            {
                [executionStepIdentity] = receipt
            };
        List<string> expiryNotices = CaptureHiddenStatusExpiryNotices(snapshots);

        // AddStatus must not allocate after the first timer mutation. Capacity changes do not alter domain truth.
        if (hiddenStatus != null)
        {
            HashSet<NpcRuntime> reserved = new HashSet<NpcRuntime>();
            foreach (CrimeHiddenStatusSnapshotEntry snapshot in snapshots)
            {
                if (snapshot.Npc == null || snapshot.HiddenDaysRemaining <= 0
                    || snapshot.HiddenStatusCount != 0 || !reserved.Add(snapshot.Npc))
                {
                    continue;
                }

                List<NpcStatusData> statuses = snapshot.Npc.CurrentStatus;
                if (statuses.Capacity < statuses.Count + 1)
                {
                    statuses.Capacity = statuses.Count + 1;
                }
            }
        }

        prepared = new CrimeHiddenStatusCommit(
            this,
            receipt,
            npcRuntimeList,
            snapshots,
            false,
            fingerprint,
            step.OwnerRevision,
            hiddenStatusStepRevision,
            hiddenStatusStepReceipts,
            nextReceipts,
            expiryNotices);
        failure = TimelineFailure.None;
        return true;
    }

    internal bool TryCommitAdvanceHiddenStatusesStep(
        CrimeHiddenStatusReceipt receipt,
        List<NpcRuntime> npcRuntimeList,
        IReadOnlyList<CrimeHiddenStatusSnapshotEntry> expectedSnapshots,
        IReadOnlyList<string> expiryNotices,
        bool replay,
        string fingerprint,
        string expectedOwnerRevision,
        long expectedRevision,
        Dictionary<string, CrimeHiddenStatusReceipt> expectedReceipts,
        Dictionary<string, CrimeHiddenStatusReceipt> nextReceipts,
        out TimelineFailure failure)
    {
        failure = TimelineFailure.ContinuationFailed;
        if (receipt == null)
        {
            return false;
        }

        if (replay)
        {
            if (hiddenStatusStepReceipts != null
                && hiddenStatusStepReceipts.TryGetValue(receipt.ExecutionStepIdentity, out CrimeHiddenStatusReceipt existing)
                && ReferenceEquals(existing, receipt)
                && string.Equals(existing.DescriptorFingerprint, fingerprint, StringComparison.Ordinal))
            {
                failure = TimelineFailure.None;
                return true;
            }

            return false;
        }

        if (npcRuntimeList == null || expectedSnapshots == null || expiryNotices == null
            || !mutationGuardBinding.CanMutate || hiddenStatusStepRevision != expectedRevision
            || !ReferenceEquals(hiddenStatusStepReceipts, expectedReceipts)
            || nextReceipts == null || !nextReceipts.ContainsKey(receipt.ExecutionStepIdentity)
            || hiddenStatusStepReceipts.ContainsKey(receipt.ExecutionStepIdentity)
            || hiddenStatusStepRevision == long.MaxValue
            || receipt.OwnerRevisionBefore != hiddenStatusStepRevision
            || receipt.OwnerRevisionAfter != hiddenStatusStepRevision + 1L
            || !string.Equals(receipt.DescriptorFingerprint, fingerprint, StringComparison.Ordinal)
            || !TryCaptureHiddenStatusSnapshot(npcRuntimeList, out List<CrimeHiddenStatusSnapshotEntry> currentSnapshots,
                out string currentOwnerRevision)
            || !string.Equals(currentOwnerRevision, expectedOwnerRevision, StringComparison.Ordinal)
            || !SameHiddenStatusSnapshot(expectedSnapshots, currentSnapshots))
        {
            return false;
        }

        foreach (CrimeHiddenStatusSnapshotEntry snapshot in expectedSnapshots)
        {
            NpcRuntime npcRuntime = snapshot.Npc;
            if (npcRuntime == null)
            {
                continue;
            }

            if (npcRuntime.IsHidden == false)
            {
                npcRuntime.RemoveStatus(hiddenStatus);
                continue;
            }

            if (hiddenStatus != null && npcRuntime.CurrentStatus.Contains(hiddenStatus) == false)
            {
                npcRuntime.AddStatus(hiddenStatus);
            }

            npcRuntime.AdvanceHiddenDay();
            if (npcRuntime.IsHidden == false)
            {
                npcRuntime.RemoveStatus(hiddenStatus);
            }
        }

        hiddenStatusStepRevision = receipt.OwnerRevisionAfter;
        hiddenStatusStepReceipts = nextReceipts;
        failure = TimelineFailure.None;

        // Logs are diagnostics, not source signals or world truth. Emit them only after the receipt is durable.
        foreach (string notice in expiryNotices)
        {
            logger.Log(SimulationLogCategory.Crime, notice);
        }

        return true;
    }

    private bool TryCaptureHiddenStatusSnapshot(
        List<NpcRuntime> npcRuntimeList,
        out List<CrimeHiddenStatusSnapshotEntry> snapshots,
        out string ownerRevision)
    {
        snapshots = null;
        ownerRevision = null;
        if (npcRuntimeList == null || !mutationGuardBinding.CanMutate || hiddenStatusStepRevision == long.MaxValue)
        {
            return false;
        }

        snapshots = new List<CrimeHiddenStatusSnapshotEntry>(npcRuntimeList.Count);
        List<string> revisionParts = new List<string>(3 + npcRuntimeList.Count)
        {
            HiddenStatusSnapshotVersion,
            hiddenStatusStepRevision.ToString(CultureInfo.InvariantCulture),
            npcRuntimeList.Count.ToString(CultureInfo.InvariantCulture),
            hiddenStatus == null ? string.Empty : hiddenStatus.statusName
        };
        Dictionary<string, NpcRuntime> runtimeIds = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        for (int i = 0; i < npcRuntimeList.Count; i++)
        {
            NpcRuntime npcRuntime = npcRuntimeList[i];
            if (npcRuntime == null)
            {
                snapshots.Add(new CrimeHiddenStatusSnapshotEntry(null, string.Empty, 0, 0));
                revisionParts.Add(SpatialStableKey.Encode(i.ToString(CultureInfo.InvariantCulture), "null-slot"));
                continue;
            }

            string runtimeId = npcRuntime.RuntimeId;
            if (string.IsNullOrWhiteSpace(runtimeId)
                || (runtimeIds.TryGetValue(runtimeId, out NpcRuntime registered)
                    && !ReferenceEquals(registered, npcRuntime)))
            {
                return false;
            }

            runtimeIds[runtimeId] = npcRuntime;
            int markerCount = CountHiddenStatusMarkers(npcRuntime);
            int hiddenDaysRemaining = npcRuntime.HiddenDaysRemaining;
            snapshots.Add(new CrimeHiddenStatusSnapshotEntry(npcRuntime, runtimeId, hiddenDaysRemaining, markerCount));
            revisionParts.Add(SpatialStableKey.Encode(
                i.ToString(CultureInfo.InvariantCulture),
                runtimeId,
                hiddenDaysRemaining.ToString(CultureInfo.InvariantCulture),
                markerCount.ToString(CultureInfo.InvariantCulture)));
        }

        ownerRevision = SpatialStableKey.Encode(revisionParts.ToArray());
        return true;
    }

    private int CountHiddenStatusMarkers(NpcRuntime npcRuntime)
    {
        int count = 0;
        foreach (NpcStatusData status in npcRuntime.CurrentStatus)
        {
            if (ReferenceEquals(status, hiddenStatus))
            {
                count++;
            }
        }

        return count;
    }

    private List<string> CaptureHiddenStatusExpiryNotices(IReadOnlyList<CrimeHiddenStatusSnapshotEntry> snapshots)
    {
        List<string> notices = new List<string>();
        Dictionary<NpcRuntime, int> remainingByNpc = new Dictionary<NpcRuntime, int>();
        foreach (CrimeHiddenStatusSnapshotEntry snapshot in snapshots)
        {
            NpcRuntime npcRuntime = snapshot.Npc;
            if (npcRuntime == null)
            {
                continue;
            }

            if (!remainingByNpc.TryGetValue(npcRuntime, out int remaining))
            {
                remaining = snapshot.HiddenDaysRemaining;
            }

            if (remaining <= 0)
            {
                remainingByNpc[npcRuntime] = remaining;
                continue;
            }

            remaining = Mathf.Max(0, remaining - 1);
            remainingByNpc[npcRuntime] = remaining;
            if (remaining <= 0)
            {
                notices.Add($"{npcRuntime.NpcName} nao esta mais escondido.");
            }
        }

        return notices;
    }

    private bool TryGetHiddenStatusIdentity(
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step,
        out string identity,
        out string fingerprint)
    {
        identity = null;
        fingerprint = null;
        if (manifest == null || step == null || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step)
            || step.StepId != HiddenStatusStepId || step.OwnerId != HiddenStatusOwnerId
            || step.OperationKind != HiddenStatusOperationKind
            || step.OperationVersion != HiddenStatusOperationVersion
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

    private static bool SameHiddenStatusSnapshot(
        IReadOnlyList<CrimeHiddenStatusSnapshotEntry> expected,
        IReadOnlyList<CrimeHiddenStatusSnapshotEntry> actual)
    {
        if (expected == null || actual == null || expected.Count != actual.Count)
        {
            return false;
        }

        for (int i = 0; i < expected.Count; i++)
        {
            CrimeHiddenStatusSnapshotEntry left = expected[i];
            CrimeHiddenStatusSnapshotEntry right = actual[i];
            if (left == null || right == null
                || !ReferenceEquals(left.Npc, right.Npc)
                || !string.Equals(left.RuntimeId, right.RuntimeId, StringComparison.Ordinal)
                || left.HiddenDaysRemaining != right.HiddenDaysRemaining
                || left.HiddenStatusCount != right.HiddenStatusCount)
            {
                return false;
            }
        }

        return true;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return mutationGuardBinding.CanBindTo(guard)
            && (travelSystem == null || travelSystem.CanBindMutationGuard(guard))
            && (justiceSystem == null || justiceSystem.CanBindMutationGuard(guard));
    }

    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard)
    {
        return CanBindMutationGuard(guard)
            && mutationGuardBinding.TryBindTo(guard)
            && (travelSystem == null || travelSystem.TryBindMutationGuard(guard))
            && (justiceSystem == null || justiceSystem.TryBindMutationGuard(guard));
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
            throw new System.InvalidOperationException("Runtime mutation is faulted.");
        }
    }

    private NpcActionRuntime CreateStealAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (CanActInCity(npcRuntime) == false || npcRuntime.TravelPlan.IsActive == true || HasNpcDefaultAction(npcRuntime, action) == false)
        {
            utility = 0f;
            return null;
        }

        NpcRuntime target = FindStealTarget(npcRuntime);

        if (target == null)
        {
            utility = 0f;
            return null;
        }

        int amount = GetConfiguredAmount(action, target);

        if (amount <= 0)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(0f, utility) + GetMoneyPressureBonus(npcRuntime) + Mathf.Min(8f, amount * 0.2f);
        NpcActionRuntime runtime = new NpcActionRuntime(action, target, amount);
        if (npcRuntime.PersonId != null && target.PersonId != null)
        {
            runtime.SetStableOccurrenceKey(BuildAutonomousTheftOccurrenceKey(
                npcRuntime.PersonId,
                target.PersonId,
                amount,
                action.DefinitionId));
        }

        return runtime;
    }

    private NpcActionRuntime CreateHideAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (CanActInCity(npcRuntime) == false || npcRuntime.IsHidden == true || npcRuntime.TravelPlan.IsActive == true || justiceSystem.HasActiveWarrantInCity(npcRuntime, npcRuntime.CurrentCity) == false)
        {
            utility = 0f;
            return null;
        }

        float bounty = justiceSystem.GetBounty(npcRuntime, npcRuntime.CurrentCity);
        utility = Mathf.Max(utility, 20f + bounty * 0.15f);
        return new NpcActionRuntime(action);
    }

    private NpcActionRuntime CreateFleeCityAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (CanActInCity(npcRuntime) == false || npcRuntime.IsHidden == true || npcRuntime.TravelPlan.IsActive == true || justiceSystem.HasActiveWarrantInCity(npcRuntime, npcRuntime.CurrentCity) == false)
        {
            utility = 0f;
            return null;
        }

        FleeDestinationOption option = FindBestFleeDestination(npcRuntime);

        if (option == null)
        {
            utility = 0f;
            return null;
        }

        utility = Mathf.Max(utility, option.Utility);
        return new NpcActionRuntime(action, option.TargetCity, null, 0, option.TravelCost);
    }

    private NpcActionRuntime CreateEscapePrisonAction(NpcRuntime npcRuntime, NpcActionData action, ref float utility)
    {
        if (npcRuntime == null || justiceSystem.IsArrested(npcRuntime) == false || justiceSystem.WasArrestedToday(npcRuntime) == true)
        {
            utility = 0f;
            return null;
        }

        int remainingDays = justiceSystem.GetRemainingSentenceDays(npcRuntime);

        if (remainingDays <= 1)
        {
            utility = 0f;
            return null;
        }

        float remainingSentenceUrgency = Mathf.Clamp01((remainingDays - 1f) / 7f);
        utility = Mathf.Clamp(10f + remainingSentenceUrgency * 20f, 10f, 30f);

        NpcActionRuntime actionRuntime = new NpcActionRuntime(action);
        int failedAttempts = justiceSystem.GetFailedEscapeAttempts(npcRuntime);
        actionRuntime.SetSuccessChanceMultiplier(Mathf.Pow(EscapeSuccessMultiplierPerFailure, failedAttempts));
        return actionRuntime;
    }

    private NpcActionResult TryExecuteSteal(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (CanActInCity(npcRuntime) == false || IsValidStealTarget(npcRuntime, actionRuntime.TargetNpc) == false)
        {
            return NpcActionResult.Failed();
        }

        int amount = Mathf.Min(actionRuntime.Amount, Mathf.FloorToInt(actionRuntime.TargetNpc.Money));
        if (theftOutcomeSink != null
            && (npcRuntime.PersonId == null
                || actionRuntime.TargetNpc.PersonId == null
                || string.IsNullOrWhiteSpace(actionRuntime.StableOccurrenceKey)))
        {
            return NpcActionResult.Failed();
        }

        TheftOutcome theftOutcome = CreateTheftOutcome(npcRuntime, actionRuntime, amount);

        if (theftOutcome != null
            && theftOutcomeSink.CanAcceptTheftOutcome(theftOutcome) == false)
        {
            return NpcActionResult.Failed();
        }

        if (amount <= 0 || transactionService.TryTransferMoney(actionRuntime.TargetNpc, npcRuntime, amount).Success == false)
        {
            return NpcActionResult.Failed();
        }

        CrimeActionSettings settings = GetCrimeSettings(actionRuntime.Action);
        logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} roubou {actionRuntime.TargetNpc.NpcName} e levou {amount} moedas.");
        if (theftOutcome != null
            && theftOutcomeSink.TryAcceptTheftOutcome(theftOutcome) == false)
        {
            EconomyTransactionResult rollback = transactionService.TryTransferMoney(
                npcRuntime,
                actionRuntime.TargetNpc,
                amount);
            if (rollback.Success == false)
            {
                logger.LogWarning("O outcome social do roubo falhou e a reversao monetaria tambem falhou.");
            }

            return NpcActionResult.Failed();
        }
        justiceSystem.CreateOrIncreaseWarrant(npcRuntime, npcRuntime.CurrentCity, settings.bounty, settings.sentenceDays);
        return NpcActionResult.Succeeded();
    }

    private TheftOutcome CreateTheftOutcome(
        NpcRuntime perpetratorRuntime,
        NpcActionRuntime actionRuntime,
        int amount)
    {
        if (theftOutcomeSink == null
            || perpetratorRuntime?.PersonId == null
            || actionRuntime?.TargetNpc?.PersonId == null)
        {
            return null;
        }

        if (amount <= 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(actionRuntime.StableOccurrenceKey))
        {
            return null;
        }

        long absoluteDay = simulationTime?.AbsoluteDay ?? 0L;
        return new TheftOutcome(
            TheftOutcomeId.Create(
                perpetratorRuntime.PersonId,
                actionRuntime.TargetNpc.PersonId,
                absoluteDay,
                actionRuntime.StableOccurrenceKey),
            perpetratorRuntime.PersonId,
            actionRuntime.TargetNpc.PersonId,
            amount,
            absoluteDay,
            actionRuntime.StableOccurrenceKey,
            actionRuntime.OriginDecisionId);
    }

    private string BuildAutonomousTheftOccurrenceKey(
        PersonId perpetratorPersonId,
        PersonId victimPersonId,
        int amount,
        string actionDefinitionId)
    {
        // One autonomous crime action owns one semantic occurrence slot for a
        // day/source/target/amount/content tuple. A repeated slot is a
        // duplicate outcome and is rejected before money is mutated.
        return "autonomous|day|"
            + (simulationTime?.AbsoluteDay ?? 0L).ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "|perp|"
            + perpetratorPersonId.Value
            + "|victim|"
            + victimPersonId.Value
            + "|amount|"
            + amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "|action|"
            + (actionDefinitionId ?? string.Empty);
    }

    private NpcActionResult TryExecuteHide(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (CanActInCity(npcRuntime) == false || justiceSystem.HasActiveWarrantInCity(npcRuntime, npcRuntime.CurrentCity) == false)
        {
            return NpcActionResult.Failed();
        }

        int hiddenDays = Mathf.Max(1, GetCrimeSettings(actionRuntime.Action).hiddenDays);
        npcRuntime.HideForDays(hiddenDays);
        npcRuntime.AddStatus(hiddenStatus);
        logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} esta escondido.");
        return NpcActionResult.Succeeded();
    }

    private NpcActionResult TryExecuteFleeCity(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (CanActInCity(npcRuntime) == false || actionRuntime.TargetCity == null)
        {
            return NpcActionResult.Failed();
        }

        if (travelSystem == null || travelSystem.CanStartTravel(npcRuntime, actionRuntime.TargetCity, out _, out float travelCost) == false)
        {
            return NpcActionResult.Failed();
        }

        npcRuntime.SetTravelPlan(actionRuntime.TargetCity, NpcTravelReason.Flee, 80f, travelCost, actionRuntime.OriginDecisionId);
        logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} decidiu fugir para {actionRuntime.TargetCity.CityName}. Custo de viagem: {travelCost:0.##}.");
        return NpcActionResult.Succeeded();
    }

    private NpcActionResult TryExecuteEscapePrison(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        CrimeActionSettings settings = GetCrimeSettings(actionRuntime.Action);

        if (justiceSystem.EscapePrison(npcRuntime, settings.escapeBountyPenalty, actionRuntime.OriginDecisionId) == false)
        {
            return NpcActionResult.Failed();
        }

        string wantedText = justiceSystem.HasAnyActiveWarrant(npcRuntime) == true ? "continua procurado" : "nao possui mandado ativo";
        logger.Log(SimulationLogCategory.Crime, $"{npcRuntime.NpcName} esta livre e {wantedText}.");
        return NpcActionResult.Succeeded();
    }

    private NpcRuntime FindStealTarget(NpcRuntime thiefRuntime)
    {
        float totalWeight = 0f;
        List<NpcRuntime> candidates = new List<NpcRuntime>();

        foreach (NpcRuntime candidate in thiefRuntime.CurrentCity.ImportantNpcs)
        {
            if (IsValidStealTarget(thiefRuntime, candidate) == false)
            {
                continue;
            }

            totalWeight += GetStealTargetWeight(candidate);
            candidates.Add(candidate);
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        candidates.Sort((left, right) => string.CompareOrdinal(left.RuntimeId, right.RuntimeId));
        float randomValue = randomSource.NextUnit(
            "crime-steal|" + thiefRuntime.RuntimeId + "|" + (simulationTime?.AbsoluteDay ?? 0L)) * totalWeight;
        float currentWeight = 0f;

        foreach (NpcRuntime candidate in candidates)
        {
            if (IsValidStealTarget(thiefRuntime, candidate) == false)
            {
                continue;
            }

            currentWeight += GetStealTargetWeight(candidate);

            if (randomValue <= currentWeight)
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsValidStealTarget(NpcRuntime thiefRuntime, NpcRuntime targetRuntime)
    {
        return targetRuntime != null
            && targetRuntime.IsAlive == true
            && targetRuntime != thiefRuntime
            && targetRuntime.CurrentCity == thiefRuntime.CurrentCity
            && targetRuntime.IsTraveling == false
            && targetRuntime.IsHidden == false
            && justiceSystem.IsArrested(targetRuntime) == false
            && targetRuntime.Money > 0f;
    }

    private FleeDestinationOption FindBestFleeDestination(NpcRuntime npcRuntime)
    {
        if (travelSystem == null || npcRuntime == null || npcRuntime.CurrentCity == null)
        {
            return null;
        }

        float currentBounty = justiceSystem.GetBounty(npcRuntime, npcRuntime.CurrentCity);
        FleeDestinationOption bestOption = null;

        foreach (CityRuntime targetCity in travelSystem.GetKnownDirectDestinationCities(npcRuntime, npcRuntime.CurrentCity))
        {
            if (targetCity == null || targetCity == npcRuntime.CurrentCity)
            {
                continue;
            }

            if (travelSystem.CanPlanKnownTravel(npcRuntime, targetCity, out int travelDays, out float travelCost) == false)
            {
                continue;
            }

            float destinationBounty = justiceSystem.GetBounty(npcRuntime, targetCity);
            float safetyGain = currentBounty - destinationBounty;

            if (safetyGain <= 0f)
            {
                continue;
            }

            float score = (safetyGain - travelCost) / Mathf.Max(1, travelDays);

            if (score <= 0f)
            {
                continue;
            }

            float utility = 30f + score;

            if (bestOption == null
                || utility > bestOption.Utility
                || (Mathf.Approximately(utility, bestOption.Utility)
                    && string.CompareOrdinal(
                        targetCity.RuntimeId,
                        bestOption.TargetCity?.RuntimeId ?? string.Empty) < 0))
            {
                bestOption = new FleeDestinationOption(targetCity, travelCost, utility);
            }
        }

        return bestOption;
    }

    private bool CanActInCity(NpcRuntime npcRuntime)
    {
        return justiceSystem != null
            && npcRuntime != null
            && npcRuntime.IsAlive == true
            && npcRuntime.CurrentCity != null
            && npcRuntime.IsTraveling == false
            && justiceSystem.IsArrested(npcRuntime) == false;
    }

    private bool HasNpcDefaultAction(NpcRuntime npcRuntime, NpcActionData action)
    {
        if (npcRuntime == null || npcRuntime.NpcData == null || npcRuntime.NpcData.acoesPadrao == null)
        {
            return false;
        }

        return npcRuntime.NpcData.acoesPadrao.Exists(x => x != null && x.action == action);
    }

    private int GetConfiguredAmount(NpcActionData action, NpcRuntime targetRuntime)
    {
        int configuredAmount = Mathf.Max(0, GetCrimeSettings(action).amount);
        return Mathf.Min(configuredAmount, Mathf.FloorToInt(targetRuntime.Money));
    }

    private float GetMoneyPressureBonus(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null)
        {
            return 0f;
        }

        return Mathf.Clamp((50f - npcRuntime.Money) * 0.35f, 0f, 18f);
    }

    private float GetStealTargetWeight(NpcRuntime candidate)
    {
        return candidate != null ? Mathf.Max(1f, candidate.Money) : 0f;
    }

    private CrimeActionSettings GetCrimeSettings(NpcActionData action)
    {
        return action != null && action.crimeSettings != null ? action.crimeSettings : new CrimeActionSettings();
    }

    private class FleeDestinationOption
    {
        public CityRuntime TargetCity { get; }
        public float TravelCost { get; }
        public float Utility { get; }

        public FleeDestinationOption(CityRuntime targetCity, float travelCost, float utility)
        {
            TargetCity = targetCity;
            TravelCost = travelCost;
            Utility = utility;
        }
    }
}

public sealed class CrimeHiddenStatusReceipt
{
    public string ExecutionStepIdentity { get; }
    public string DescriptorFingerprint { get; }
    public long OwnerRevisionBefore { get; }
    public long OwnerRevisionAfter { get; }

    internal CrimeHiddenStatusReceipt(
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

internal sealed class CrimeHiddenStatusSnapshotEntry
{
    public NpcRuntime Npc { get; }
    public string RuntimeId { get; }
    public int HiddenDaysRemaining { get; }
    public int HiddenStatusCount { get; }

    public CrimeHiddenStatusSnapshotEntry(
        NpcRuntime npc,
        string runtimeId,
        int hiddenDaysRemaining,
        int hiddenStatusCount)
    {
        Npc = npc;
        RuntimeId = runtimeId ?? string.Empty;
        HiddenDaysRemaining = hiddenDaysRemaining;
        HiddenStatusCount = hiddenStatusCount;
    }
}

internal sealed class CrimeHiddenStatusCommit : IBoundaryContinuationStepCommit
{
    private readonly CrimeSystem owner;
    private readonly CrimeHiddenStatusReceipt receipt;
    private readonly List<NpcRuntime> npcRuntimeList;
    private readonly IReadOnlyList<CrimeHiddenStatusSnapshotEntry> snapshots;
    private readonly IReadOnlyList<string> expiryNotices;
    private readonly bool replay;
    private readonly string fingerprint;
    private readonly string expectedOwnerRevision;
    private readonly long expectedRevision;
    private readonly Dictionary<string, CrimeHiddenStatusReceipt> expectedReceipts;
    private readonly Dictionary<string, CrimeHiddenStatusReceipt> nextReceipts;
    private bool completed;

    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => Array.Empty<DueWorkReference>();
    public IReadOnlyList<string> RetainedSourceSignals => Array.Empty<string>();
    public CrimeHiddenStatusReceipt Receipt => receipt;

    public CrimeHiddenStatusCommit(
        CrimeSystem owner,
        CrimeHiddenStatusReceipt receipt,
        List<NpcRuntime> npcRuntimeList,
        IReadOnlyList<CrimeHiddenStatusSnapshotEntry> snapshots,
        bool replay,
        string fingerprint,
        string expectedOwnerRevision,
        long expectedRevision,
        Dictionary<string, CrimeHiddenStatusReceipt> expectedReceipts,
        Dictionary<string, CrimeHiddenStatusReceipt> nextReceipts,
        IReadOnlyList<string> expiryNotices = null)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
        this.npcRuntimeList = npcRuntimeList;
        this.snapshots = snapshots ?? Array.Empty<CrimeHiddenStatusSnapshotEntry>();
        this.replay = replay;
        this.fingerprint = fingerprint ?? string.Empty;
        this.expectedOwnerRevision = expectedOwnerRevision ?? string.Empty;
        this.expectedRevision = expectedRevision;
        this.expectedReceipts = expectedReceipts;
        this.nextReceipts = nextReceipts;
        this.expiryNotices = expiryNotices ?? Array.Empty<string>();
    }

    public bool TryCommit(out TimelineFailure failure)
    {
        if (completed)
        {
            failure = TimelineFailure.None;
            return true;
        }

        completed = owner.TryCommitAdvanceHiddenStatusesStep(
            receipt,
            npcRuntimeList,
            snapshots,
            expiryNotices,
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
