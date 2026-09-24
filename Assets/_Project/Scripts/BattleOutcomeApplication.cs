using System;
using System.Collections.Generic;
using System.Threading;

public enum BattleOutcomeApplicationStatus
{
    Applied = 0,
    AppliedWithPostCommitWarning = 1,
    AlreadyResolved = 2,
    Rejected = 3,
    TransactionFaulted = 4
}

public enum BattleOutcomeApplicationFailureCode
{
    None = 0,
    RuntimeFaulted = 1,
    ReentrantApplication = 2,
    InvalidRequest = 3,
    BattleNotFound = 4,
    BattleNotActive = 5,
    PlanningFailed = 6,
    PolicyNotConfigured = 7,
    NumericProfileUnsupported = 8,
    ExpectedFingerprintMismatch = 9,
    PlanStale = 10,
    ManpowerPrepareFailed = 11,
    SourcePrepareFailed = 12,
    ConservationFailed = 13,
    RevisionOverflow = 14,
    CommitFailedRolledBack = 15,
    RollbackFailed = 16,
    BattleWritePrepareFailed = 17
}

[Flags]
internal enum BattleOutcomeApplicationFailureInjection
{
    None = 0,
    AfterManpowerCommit = 1,
    AfterFirstSourceCommit = 2,
    BeforeBattleCommit = 4,
    RollbackFailure = 8,
    BeforeSecondSourcePrepare = 16,
    AfterBattleCommit = 32
}

public sealed class BattleOutcomeApplicationFailure
{
    public BattleOutcomeApplicationFailureCode Code { get; }
    public string Message { get; }
    public BattleOutcomePlanningFailure D5Failure { get; }
    public BattleDirectConsequenceFailure D6B2Failure { get; }

    internal BattleOutcomeApplicationFailure(
        BattleOutcomeApplicationFailureCode code,
        string message,
        BattleOutcomePlanningFailure d5Failure = null,
        BattleDirectConsequenceFailure d6b2Failure = null)
    {
        Code = code;
        Message = message ?? string.Empty;
        D5Failure = d5Failure;
        D6B2Failure = d6b2Failure;
    }
}

public sealed class BattleOutcomeApplicationResult
{
    public BattleOutcomeApplicationStatus Status { get; }
    public PersistentBattleTerminalOutcome Outcome { get; }
    public BattleOutcomeApplicationFailure Failure { get; }
    public bool IsSuccess => Status == BattleOutcomeApplicationStatus.Applied
        || Status == BattleOutcomeApplicationStatus.AppliedWithPostCommitWarning
        || Status == BattleOutcomeApplicationStatus.AlreadyResolved;

    internal BattleOutcomeApplicationResult(
        BattleOutcomeApplicationStatus status,
        PersistentBattleTerminalOutcome outcome,
        BattleOutcomeApplicationFailure failure)
    {
        Status = status;
        Outcome = outcome;
        Failure = failure;
    }
}

/// <summary>
/// Explicit, synchronous D7 coordinator. It reconstructs world-authorized D5/D6B2
/// state and owns only the narrow transaction boundary for one Battle.
/// </summary>
public sealed class BattleOutcomeApplicationService
{
    private sealed class PreparedSourceTransition
    {
        internal ManpowerSourceId SourceId { get; }
        internal ManpowerSourceConsequenceProposal Proposal { get; }
        internal SettlementPopulationRuntime Population { get; }
        internal int RepresentedResidentFloor { get; }
        internal AggregateDemographyTransition Transition { get; }
        internal int PopulationBefore { get; }
        internal long PopulationRevisionBefore { get; }

        internal PreparedSourceTransition(
            ManpowerSourceId sourceId,
            ManpowerSourceConsequenceProposal proposal,
            SettlementPopulationRuntime population,
            int representedResidentFloor,
            AggregateDemographyTransition transition)
        {
            SourceId = sourceId;
            Proposal = proposal;
            Population = population;
            RepresentedResidentFloor = representedResidentFloor;
            Transition = transition;
            PopulationBefore = population.CurrentPopulation;
            PopulationRevisionBefore = population.Revision;
        }
    }

    private readonly SimulationRuntime world;
    private readonly AuthoritativeMutationGuard mutationGuard;
    private readonly IDomainEventRecorder battleResolvedEventRecorder;
    private int applicationLease;

    // Internal-only deterministic injection seam used by EditMode transaction tests.
    internal BattleOutcomeApplicationFailureInjection FailureInjectionForTests { get; set; }

    internal BattleOutcomeApplicationService(
        SimulationRuntime world,
        AuthoritativeMutationGuard mutationGuard,
        IDomainEventRecorder battleResolvedEventRecorder)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.mutationGuard = mutationGuard ?? throw new ArgumentNullException(nameof(mutationGuard));
        this.battleResolvedEventRecorder = battleResolvedEventRecorder;
    }

    public BattleOutcomeApplicationResult Apply(
        BattleId battleId,
        BattleExecutionPlan executionPlan = null,
        string expectedD6B2PlanFingerprint = null)
    {
        // The sticky D7G guard is intentionally the first authority checked.
        if (!mutationGuard.CanMutate)
            return Reject(BattleOutcomeApplicationFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.");
        if (battleId == null || (expectedD6B2PlanFingerprint != null
            && string.IsNullOrWhiteSpace(expectedD6B2PlanFingerprint)))
            return Reject(BattleOutcomeApplicationFailureCode.InvalidRequest, "A BattleId and, when supplied, a non-empty D6B2 fingerprint are required.");
        if (Interlocked.CompareExchange(ref applicationLease, 1, 0) != 0)
            return Reject(BattleOutcomeApplicationFailureCode.ReentrantApplication, "Another D7 application is already active for this SimulationRuntime.");

        try
        {
            return ApplyUnderLease(battleId, executionPlan, expectedD6B2PlanFingerprint);
        }
        finally
        {
            Volatile.Write(ref applicationLease, 0);
        }
    }

    private BattleOutcomeApplicationResult ApplyUnderLease(
        BattleId battleId,
        BattleExecutionPlan executionPlan,
        string expectedD6B2PlanFingerprint)
    {
        if (!mutationGuard.CanMutate)
            return Reject(BattleOutcomeApplicationFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.");
        if (!world.BattleStore.TryGet(battleId, out PersistentBattleRecord battle))
            return Reject(BattleOutcomeApplicationFailureCode.BattleNotFound, "The BattleId is not registered.");

        // Idempotency deliberately precedes every policy/planner access.
        if (battle.LifecycleState == BattleLifecycleState.Resolved)
        {
            PersistentBattleTerminalOutcome accepted = battle.TerminalOutcome;
            if (accepted == null)
                return Reject(BattleOutcomeApplicationFailureCode.BattleWritePrepareFailed, "The resolved Battle has no persisted terminal outcome.");
            if (expectedD6B2PlanFingerprint != null
                && !string.Equals(
                    expectedD6B2PlanFingerprint,
                    accepted.Provenance.D6B2PlanFingerprint,
                    StringComparison.Ordinal))
                return Reject(BattleOutcomeApplicationFailureCode.ExpectedFingerprintMismatch, "The expected D6B2 fingerprint differs from the persisted accepted fingerprint.");
            return new BattleOutcomeApplicationResult(
                BattleOutcomeApplicationStatus.AlreadyResolved,
                accepted,
                null);
        }
        if (battle.LifecycleState != BattleLifecycleState.Active || battle.TerminalOutcome != null)
            return Reject(BattleOutcomeApplicationFailureCode.BattleNotActive, "Only an Active Battle without a terminal outcome can be applied.");

        if (!world.BattleDirectConsequencePlanningService.IsConfigured)
            return Reject(BattleOutcomeApplicationFailureCode.PolicyNotConfigured, "The world has no explicitly configured D6B2 consequence policy.");
        if (world.BattleResolutionPolicy == null)
            return Reject(BattleOutcomeApplicationFailureCode.PolicyNotConfigured, "The world has no explicitly configured D5 resolution policy.");
        if (!world.BattleResolutionPolicy.NumericExecutionProfileSupported)
            return Reject(BattleOutcomeApplicationFailureCode.NumericProfileUnsupported, "The configured D5 numeric execution profile is not explicitly supported by this runtime.");

        BattleDirectConsequencePlan plan;
        BattleDirectConsequenceFailure planningFailure;
        try
        {
            if (!world.BattleDirectConsequencePlanningService.TryCreatePlan(
                    battleId,
                    executionPlan,
                    out plan,
                    out planningFailure))
                return MapPlanningFailure(planningFailure);
        }
        catch (Exception)
        {
            return Reject(BattleOutcomeApplicationFailureCode.PlanningFailed, "World-authorized D5/D6B2 planning failed before transaction preparation.");
        }

        if (plan == null || plan.BattleId != battleId || !plan.IsComplete
            || plan.D5ApplicationPlan?.Outcome?.Provenance == null
            || plan.AbsoluteDay != plan.D5ApplicationPlan.Outcome.ResolvedAbsoluteDay)
            return Reject(BattleOutcomeApplicationFailureCode.PlanningFailed, "D6B2 returned a malformed or incomplete plan.");
        if (expectedD6B2PlanFingerprint != null
            && !string.Equals(expectedD6B2PlanFingerprint, plan.Fingerprint, StringComparison.Ordinal))
            return Reject(BattleOutcomeApplicationFailureCode.ExpectedFingerprintMismatch, "The expected D6B2 fingerprint differs from the freshly reconstructed plan.");

        BattleResolutionProvenance d5Provenance = plan.D5ApplicationPlan.Outcome.Provenance;
        if (!world.BattleResolutionPolicy.NumericExecutionProfileSupported
            || !string.Equals(
                d5Provenance.NumericExecutionProfileKey,
                world.BattleResolutionPolicy.NumericExecutionProfileKey,
                StringComparison.Ordinal))
            return Reject(BattleOutcomeApplicationFailureCode.NumericProfileUnsupported, "The accepted D5 outcome does not carry the runtime-supported numeric execution profile.");

        PersistentBattleTerminalOutcome terminalOutcome;
        try
        {
            terminalOutcome = new PersistentBattleTerminalOutcome(
                battleId,
                plan.OutcomeType,
                plan.WinningSideId,
                plan.AbsoluteDay,
                new PersistentBattleOutcomeProvenance(
                    d5Provenance,
                    plan.D6B2PolicyFingerprint,
                    BattleDirectConsequencePlanningService.PlanSchemaVersion,
                    BattleDirectConsequencePlanningService.CoverageVersion,
                    plan.Fingerprint));
        }
        catch (Exception)
        {
            return Reject(BattleOutcomeApplicationFailureCode.PlanningFailed, "The accepted D5/D6B2 provenance could not form a stable terminal outcome.");
        }

        if (!world.BattleStore.TryPrepareTerminalWrite(
                terminalOutcome,
                out PreparedBattleTerminalWrite battleWrite,
                out PersistentStateFailure battlePrepareFailure))
            return Reject(
                battlePrepareFailure.Code == PersistentStateFailureCode.RevisionOverflow
                    ? BattleOutcomeApplicationFailureCode.RevisionOverflow
                    : BattleOutcomeApplicationFailureCode.BattleWritePrepareFailed,
                battlePrepareFailure.ToString());

        if (!world.ContingentManpowerStateStore.TryPrepareBattleBatch(
                plan,
                out PreparedBattleManpowerBatch manpowerBatch,
                out ContingentManpowerFailure manpowerFailure))
            return Reject(
                manpowerFailure.Code == ContingentManpowerFailureCode.RevisionOverflow
                    ? BattleOutcomeApplicationFailureCode.RevisionOverflow
                    : BattleOutcomeApplicationFailureCode.ManpowerPrepareFailed,
                manpowerFailure.ToString());

        if (!TryPrepareSources(plan, out List<PreparedSourceTransition> sourceWrites, out string sourceFailure))
            return Reject(BattleOutcomeApplicationFailureCode.SourcePrepareFailed, sourceFailure);

        if (!TryValidatePlanAndPreparedWrites(battle, battleWrite, plan, manpowerBatch, sourceWrites,
                out BattleOutcomeApplicationResult staleResult))
            return staleResult;

        bool manpowerMayHaveChanged = false;
        bool battleMayHaveChanged = false;
        List<PreparedSourceTransition> sourcesMayHaveChanged = new List<PreparedSourceTransition>();
        try
        {
            manpowerMayHaveChanged = true;
            if (!world.ContingentManpowerStateStore.TryCommitBattleBatch(
                    manpowerBatch,
                    out _,
                    out bool manpowerWriteStarted,
                    out ContingentManpowerFailure commitManpowerFailure))
            {
                manpowerMayHaveChanged = manpowerWriteStarted;
                if (!manpowerMayHaveChanged)
                    return Reject(
                        commitManpowerFailure.Code == ContingentManpowerFailureCode.RevisionOverflow
                            ? BattleOutcomeApplicationFailureCode.RevisionOverflow
                            : BattleOutcomeApplicationFailureCode.PlanStale,
                        commitManpowerFailure.ToString());
                throw new InvalidOperationException("Prepared D6A/mirror commit failed after a write began.");
            }
            manpowerMayHaveChanged = manpowerBatch.StateWrites.Count > 0
                || manpowerBatch.MirrorBatch.ReplacementRecords.Count > 0;
            if (HasInjection(BattleOutcomeApplicationFailureInjection.AfterManpowerCommit))
                throw new InvalidOperationException("Injected D7 post-manpower failure.");

            for (int index = 0; index < sourceWrites.Count; index++)
            {
                PreparedSourceTransition source = sourceWrites[index];
                sourcesMayHaveChanged.Add(source);
                if (!AggregateDemographySystem.TryApply(
                        source.Population,
                        source.RepresentedResidentFloor,
                        source.Transition,
                        out AggregateDemographyFailure sourceApplyFailure))
                    throw new InvalidOperationException("A prepared source transition failed during commit: " + sourceApplyFailure + ".");
                if (index == 0 && HasInjection(BattleOutcomeApplicationFailureInjection.AfterFirstSourceCommit))
                    throw new InvalidOperationException("Injected D7 post-source failure.");
            }

            if (HasInjection(BattleOutcomeApplicationFailureInjection.BeforeBattleCommit))
                throw new InvalidOperationException("Injected D7 pre-Battle-write failure.");
            // Be conservative if the store throws after its assignment but before
            // returning the out flag: the exact pre-write Battle snapshot exists.
            battleMayHaveChanged = true;
            if (!world.BattleStore.TryCommitTerminalWrite(
                    battleWrite,
                    out bool battleWriteStarted,
                    out PersistentStateFailure battleCommitFailure))
            {
                battleMayHaveChanged = battleWriteStarted;
                throw new InvalidOperationException("Prepared terminal Battle write failed during commit: " + battleCommitFailure + ".");
            }
            battleMayHaveChanged = battleWriteStarted;
            if (HasInjection(BattleOutcomeApplicationFailureInjection.AfterBattleCommit))
                throw new InvalidOperationException("Injected D7 post-Battle-write failure.");
        }
        catch (Exception)
        {
            bool rollbackSucceeded = Rollback(
                battle,
                battleWrite.ExpectedStoreRevision,
                battleMayHaveChanged,
                sourcesMayHaveChanged,
                manpowerBatch.Snapshot,
                manpowerMayHaveChanged);
            if (!rollbackSucceeded)
            {
                mutationGuard.MarkFaulted(AuthoritativeMutationFaultReason.RollbackRestoreFailed);
                return new BattleOutcomeApplicationResult(
                    BattleOutcomeApplicationStatus.TransactionFaulted,
                    null,
                    new BattleOutcomeApplicationFailure(
                        BattleOutcomeApplicationFailureCode.RollbackFailed,
                        "D7 commit failed and exact rollback could not be proven; the runtime is now Faulted."));
            }
            return Reject(BattleOutcomeApplicationFailureCode.CommitFailedRolledBack, "D7 commit failed unexpectedly; authoritative state and revisions were restored.");
        }

        bool eventAccepted = false;
        if (battleResolvedEventRecorder != null)
        {
            try
            {
                eventAccepted = battleResolvedEventRecorder.Record((eventId, _, sequence) =>
                    new BattleResolvedEvent(
                        eventId,
                        terminalOutcome.ResolvedAbsoluteDay,
                        sequence,
                        terminalOutcome.BattleId,
                        terminalOutcome.OutcomeType,
                        terminalOutcome.WinningBattleSideId,
                        terminalOutcome.Provenance.D5Resolution.PolicyFingerprint,
                        terminalOutcome.Provenance.D5Resolution.CausalResolutionFingerprint,
                        terminalOutcome.Provenance.D6B2PolicyFingerprint,
                        terminalOutcome.Provenance.D6B2PlanFingerprint));
            }
            catch (Exception)
            {
                eventAccepted = false;
            }
        }

        if (!eventAccepted)
            return new BattleOutcomeApplicationResult(
                BattleOutcomeApplicationStatus.AppliedWithPostCommitWarning,
                terminalOutcome,
                new BattleOutcomeApplicationFailure(
                    BattleOutcomeApplicationFailureCode.None,
                    battleResolvedEventRecorder == null
                        ? "World Truth committed; no BattleResolved event recorder was composed."
                        : "World Truth committed; BattleResolved event/history recording failed."));
        return new BattleOutcomeApplicationResult(
            BattleOutcomeApplicationStatus.Applied,
            terminalOutcome,
            null);
    }

    private bool TryPrepareSources(
        BattleDirectConsequencePlan plan,
        out List<PreparedSourceTransition> sourceWrites,
        out string failure)
    {
        sourceWrites = new List<PreparedSourceTransition>();
        failure = string.Empty;
        List<BattleDirectConsequenceSourceGroup> groups = new List<BattleDirectConsequenceSourceGroup>(plan.SourceGroups);
        groups.Sort((left, right) => StringComparer.Ordinal.Compare(left?.SourceId?.Value, right?.SourceId?.Value));
        HashSet<string> sourceIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> settlementIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<SettlementPopulationRuntime> populations = new HashSet<SettlementPopulationRuntime>();

        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            if (groupIndex == 1 && HasInjection(BattleOutcomeApplicationFailureInjection.BeforeSecondSourcePrepare))
            {
                failure = "Injected stale second-source validation before any D7 transaction write.";
                return false;
            }
            BattleDirectConsequenceSourceGroup group = groups[groupIndex];
            if (group?.SourceId == null || group.Proposal == null || group.DeathAmount <= 0L
                || !sourceIds.Add(group.SourceId.Value))
            {
                failure = "D6B2 source groups must have unique stable IDs and positive exact death proposals.";
                return false;
            }
            ManpowerSourceConsequenceProposal proposal = group.Proposal;
            if (proposal.SourceId != group.SourceId || proposal.Effect == null
                || proposal.Effect.Kind != ManpowerSourceEffectKind.Death
                || proposal.Effect.Amount != group.DeathAmount
                || proposal.RegistrationIdentity == null
                || proposal.RegistrationIdentity.SourceId != group.SourceId
                || proposal.SettlementPopulationDeath == null)
            {
                failure = "D6B1 did not provide the exact registered settlement-population death transition for source " + group.SourceId.Value + ".";
                return false;
            }

            ManpowerSourceConsequenceValidationResult current;
            try
            {
                current = world.ManpowerSourceConsequencePlanningService.TryValidateCurrent(proposal);
            }
            catch (Exception)
            {
                failure = "D6B1 source currentness validation failed before commit.";
                return false;
            }
            if (current == null || current.Status != ManpowerSourceConsequenceValidationStatus.Current)
            {
                failure = "D6B1 source proposal is stale or invalid for source " + group.SourceId.Value + ".";
                return false;
            }

            SettlementPopulationDeathSourceProposal typed = proposal.SettlementPopulationDeath;
            SettlementPopulationRuntime population = proposal.RegistrationIdentity.Population;
            AggregateDemographyTransition transition = typed.Transition;
            if (population == null || transition == null
                || typed.SourceId != group.SourceId
                || proposal.ExpectedPopulationRevision != typed.ExpectedPopulationRevision
                || proposal.PopulationBefore != typed.PopulationBefore
                || proposal.RepresentedResidentFloor != typed.RepresentedResidentFloor
                || proposal.Effect.Amount != group.DeathAmount
                || group.DeathAmount > int.MaxValue
                || transition.Deaths != (int)group.DeathAmount
                || transition.Births != 0
                || transition.PopulationBefore != population.CurrentPopulation
                || transition.ExpectedPopulationRevision != population.Revision
                || transition.PopulationAfter != typed.PopulationAfter
                || transition.PopulationAfter != transition.PopulationBefore - transition.Deaths
                || transition.RepresentedResidentFloor != typed.RepresentedResidentFloor
                || !string.Equals(population.SettlementRuntimeId, typed.SettlementRuntimeId, StringComparison.Ordinal)
                || !settlementIds.Add(typed.SettlementRuntimeId)
                || !populations.Add(population))
            {
                failure = "The captured AggregateDemographyTransition does not exactly match the current registered source state.";
                return false;
            }
            if (transition.ExpectedPopulationRevision == long.MaxValue)
            {
                failure = "A settlement population revision cannot advance.";
                return false;
            }

            sourceWrites.Add(new PreparedSourceTransition(
                group.SourceId,
                proposal,
                population,
                typed.RepresentedResidentFloor,
                transition));
        }

        // Every positive terminal death must be represented once by one source group.
        Dictionary<string, long> deathsBySource = new Dictionary<string, long>(StringComparer.Ordinal);
        Dictionary<string, ManpowerSourceId> sourceByCohort = new Dictionary<string, ManpowerSourceId>(StringComparer.Ordinal);
        foreach (BattleDirectConsequenceCohortInput input in plan.Inputs)
        {
            if (input?.Identity == null || string.IsNullOrWhiteSpace(input.Identity.StableKey))
            {
                failure = "D6B2 has a malformed cohort input while reconciling source deaths.";
                return false;
            }
            sourceByCohort[input.Identity.StableKey] = input.SourceId;
        }
        try
        {
            foreach (BattleCohortConsequencePartition partition in plan.Partitions)
            {
                if (partition?.InputIdentity == null)
                {
                    failure = "D6B2 has a malformed terminal cohort partition.";
                    return false;
                }
                if (partition.DeathAmount == 0L) continue;
                if (!sourceByCohort.TryGetValue(partition.InputIdentity.StableKey, out ManpowerSourceId sourceId)
                    || sourceId == null)
                {
                    failure = "A terminal death is not bound to one explicit ManpowerSourceId.";
                    return false;
                }
                deathsBySource.TryGetValue(sourceId.Value, out long existing);
                deathsBySource[sourceId.Value] = checked(existing + partition.DeathAmount);
            }
        }
        catch (OverflowException)
        {
            failure = "Cross-domain death totals exceed Int64 capacity.";
            return false;
        }
        foreach (KeyValuePair<string, long> sourceDeath in deathsBySource)
        {
            if (!sourceIds.Contains(sourceDeath.Key))
            {
                failure = "Terminal deaths for source " + sourceDeath.Key + " have no D6B1 source proposal.";
                return false;
            }
            BattleDirectConsequenceSourceGroup group = null;
            foreach (BattleDirectConsequenceSourceGroup candidate in groups)
                if (candidate.SourceId.Value == sourceDeath.Key) { group = candidate; break; }
            if (group == null || group.DeathAmount != sourceDeath.Value)
            {
                failure = "D6B2 cohort traces, source group, and D6B1 effect amount do not conserve deaths.";
                return false;
            }
        }
        foreach (BattleDirectConsequenceSourceGroup group in groups)
            if (!deathsBySource.TryGetValue(group.SourceId.Value, out long total) || total != group.DeathAmount)
            {
                failure = "A D6B1 source proposal does not equal its terminal D6B2 cohort deaths.";
                return false;
            }

        return true;
    }

    private bool TryValidatePlanAndPreparedWrites(
        PersistentBattleRecord expectedBattle,
        PreparedBattleTerminalWrite battleWrite,
        BattleDirectConsequencePlan plan,
        PreparedBattleManpowerBatch manpowerBatch,
        IReadOnlyList<PreparedSourceTransition> sourceWrites,
        out BattleOutcomeApplicationResult failureResult)
    {
        failureResult = null;
        if (!mutationGuard.CanMutate)
        {
            failureResult = Reject(BattleOutcomeApplicationFailureCode.RuntimeFaulted, "The SimulationRuntime became Faulted before the first D7 write.");
            return false;
        }
        if (Volatile.Read(ref applicationLease) != 1)
        {
            failureResult = Reject(BattleOutcomeApplicationFailureCode.ReentrantApplication, "The D7 application lease is no longer owned.");
            return false;
        }
        if (!world.BattleStore.TryGet(expectedBattle.Id, out PersistentBattleRecord currentBattle)
            || !ReferenceEquals(currentBattle, expectedBattle)
            || world.BattleStore.Revision != battleWrite.ExpectedStoreRevision
            || currentBattle.LifecycleState != BattleLifecycleState.Active)
        {
            failureResult = Reject(BattleOutcomeApplicationFailureCode.PlanStale, "The Battle changed after D7 transaction preparation.");
            return false;
        }

        BattleDirectConsequenceValidationReport planReport;
        BattleDirectConsequenceFailure planFailure;
        try
        {
            if (!world.BattleDirectConsequencePlanningService.TryValidateCurrent(plan, out planReport, out planFailure)
                || planReport == null || !planReport.IsCurrent)
            {
                failureResult = new BattleOutcomeApplicationResult(
                    BattleOutcomeApplicationStatus.Rejected,
                    null,
                    new BattleOutcomeApplicationFailure(
                        BattleOutcomeApplicationFailureCode.PlanStale,
                        planReport?.Message ?? planFailure?.Message ?? "The D6B2 plan is not current.",
                        planFailure?.D5Failure,
                        planFailure));
                return false;
            }
        }
        catch (Exception)
        {
            failureResult = Reject(BattleOutcomeApplicationFailureCode.PlanStale, "D6B2 currentness validation failed before the first D7 write.");
            return false;
        }

        if (!world.ContingentManpowerStateStore.IsBattleBatchCurrent(manpowerBatch))
        {
            failureResult = Reject(BattleOutcomeApplicationFailureCode.ManpowerPrepareFailed, "D6A or its Amount mirrors changed after batch preparation.");
            return false;
        }
        foreach (PreparedSourceTransition source in sourceWrites)
        {
            ManpowerSourceConsequenceValidationResult sourceReport;
            try
            {
                sourceReport = world.ManpowerSourceConsequencePlanningService.TryValidateCurrent(source.Proposal);
            }
            catch (Exception)
            {
                failureResult = Reject(BattleOutcomeApplicationFailureCode.SourcePrepareFailed, "A D6B1 source currentness check threw before the first write.");
                return false;
            }
            if (sourceReport == null || sourceReport.Status != ManpowerSourceConsequenceValidationStatus.Current
                || source.Population.Revision != source.PopulationRevisionBefore
                || source.Population.CurrentPopulation != source.PopulationBefore
                || !mutationGuard.CanMutate)
            {
                failureResult = Reject(BattleOutcomeApplicationFailureCode.SourcePrepareFailed, "A prepared D6B1 source transition is no longer current.");
                return false;
            }
        }
        return true;
    }

    private bool Rollback(
        PersistentBattleRecord originalBattle,
        long originalBattleRevision,
        bool battleMayHaveChanged,
        IReadOnlyList<PreparedSourceTransition> sourceWrites,
        BattleManpowerBatchSnapshot manpowerSnapshot,
        bool manpowerMayHaveChanged)
    {
        bool success = true;
        if (battleMayHaveChanged)
        {
            try
            {
                if (!world.BattleStore.RestoreBattleTransactionSnapshot(originalBattle, originalBattleRevision)) success = false;
            }
            catch (Exception) { success = false; }
        }
        for (int index = sourceWrites.Count - 1; index >= 0; index--)
        {
            PreparedSourceTransition source = sourceWrites[index];
            try
            {
                source.Population.RestoreSnapshot(source.PopulationBefore, source.PopulationRevisionBefore);
                if (source.Population.CurrentPopulation != source.PopulationBefore
                    || source.Population.Revision != source.PopulationRevisionBefore)
                    success = false;
            }
            catch (Exception) { success = false; }
        }
        if (manpowerMayHaveChanged)
        {
            if (HasInjection(BattleOutcomeApplicationFailureInjection.RollbackFailure))
            {
                success = false;
            }
            else
            {
                try
                {
                    if (!world.ContingentManpowerStateStore.RestoreBattleBatch(manpowerSnapshot)) success = false;
                }
                catch (Exception) { success = false; }
            }
        }
        return success;
    }

    private bool HasInjection(BattleOutcomeApplicationFailureInjection point)
        => (FailureInjectionForTests & point) == point;

    private static BattleOutcomeApplicationResult MapPlanningFailure(BattleDirectConsequenceFailure failure)
    {
        BattleOutcomeApplicationFailureCode code = BattleOutcomeApplicationFailureCode.PlanningFailed;
        if (failure?.Code == BattleDirectConsequenceFailureCode.PolicyNotConfigured
            || failure?.D5Failure?.Code == BattleOutcomePlanningFailureCode.PolicyNotConfigured)
            code = BattleOutcomeApplicationFailureCode.PolicyNotConfigured;
        else if (failure?.D5Failure?.Code == BattleOutcomePlanningFailureCode.NumericProfileUnsupported)
            code = BattleOutcomeApplicationFailureCode.NumericProfileUnsupported;
        else if (failure?.Code == BattleDirectConsequenceFailureCode.PlanStale
            || failure?.Code == BattleDirectConsequenceFailureCode.ContextBecameStale
            || failure?.Code == BattleDirectConsequenceFailureCode.SourceProposalStale)
            code = BattleOutcomeApplicationFailureCode.PlanStale;
        return new BattleOutcomeApplicationResult(
            BattleOutcomeApplicationStatus.Rejected,
            null,
            new BattleOutcomeApplicationFailure(code, failure?.Message ?? "D5/D6B2 planning failed.", failure?.D5Failure, failure));
    }

    private static BattleOutcomeApplicationResult Reject(BattleOutcomeApplicationFailureCode code, string message)
        => new BattleOutcomeApplicationResult(
            code == BattleOutcomeApplicationFailureCode.RollbackFailed
                ? BattleOutcomeApplicationStatus.TransactionFaulted
                : BattleOutcomeApplicationStatus.Rejected,
            null,
            new BattleOutcomeApplicationFailure(code, message));
}
