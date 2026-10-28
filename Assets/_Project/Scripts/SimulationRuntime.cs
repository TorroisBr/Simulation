using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

/// <summary>Stable host-supplied identity used by the bounded P17-A trusted scenario input path.</summary>
public sealed class P17AScenarioAuthorityCapability
{
    public string AuthorityId { get; }

    public P17AScenarioAuthorityCapability(string authorityId)
    {
        if (string.IsNullOrWhiteSpace(authorityId))
            throw new ArgumentException("P17-A requires a stable scenario/GM authority ID.", nameof(authorityId));
        AuthorityId = authorityId;
    }
}

public enum P17AWithdrawalGoalStatus
{
    Pending = 0,
    Achieved = 1
}

/// <summary>Immutable coherent runtime view of the bounded P17-A War and its P16-A proof.</summary>
public sealed class P17AWarObservation
{
    private readonly IReadOnlyList<WarStrategicParticipant> participants;

    public WarId WarId { get; }
    public long WarStoreRevision { get; }
    public WarLifecycleState LifecycleState { get; }
    public long? EndedAbsoluteDay { get; }
    public IReadOnlyList<WarStrategicParticipant> Participants => participants;
    public WarWithdrawalDemand WithdrawalDemand { get; }
    public string ScenarioAuthorityId { get; }
    public long P16TargetBoundaryDay { get; }
    public string TargetForceId { get; }
    public P17AWithdrawalGoalStatus GoalStatus { get; }
    public P16ACrossingReceipt GoalEvidence { get; }
    public WarTerminalConcession TerminalConcession { get; }
    public string P16PositionStableKey { get; }
    public decimal P16CurrentSupply { get; }
    public long P16OwnerRevision { get; }

    internal P17AWarObservation(PersistentWarRecord war, long warStoreRevision,
        WarParticipantBinding targetBinding, P17AWithdrawalGoalStatus goalStatus,
        P16AStateSnapshot p16State)
    {
        WarId = war.Id;
        WarStoreRevision = warStoreRevision;
        LifecycleState = war.LifecycleState;
        EndedAbsoluteDay = war.EndedAbsoluteDay;
        participants = new System.Collections.ObjectModel.ReadOnlyCollection<WarStrategicParticipant>(
            new List<WarStrategicParticipant>(war.P17A.Participants));
        WithdrawalDemand = war.P17A.WithdrawalDemand;
        ScenarioAuthorityId = war.P17A.ScenarioAuthorityId;
        P16TargetBoundaryDay = p16State.TargetBoundaryDay;
        TargetForceId = targetBinding.ArmedForceId.Value;
        GoalStatus = goalStatus;
        GoalEvidence = goalStatus == P17AWithdrawalGoalStatus.Achieved ? p16State.Receipt : null;
        TerminalConcession = war.P17A.TerminalConcession;
        P16PositionStableKey = p16State.PositionStableKey;
        P16CurrentSupply = p16State.CurrentQuantity;
        P16OwnerRevision = p16State.OwnerRevision;
    }
}

public sealed class SimulationRuntimeSpatialInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;

    internal SimulationRuntimeSpatialInvariantReport(IEnumerable<string> violations)
    {
        List<string> sorted = violations == null ? new List<string>() : new List<string>(violations);
        sorted.Sort(StringComparer.Ordinal);
        Violations = sorted.AsReadOnly();
    }
}

public sealed partial class SimulationRuntime : IFactualReadRuntimeState,
    IDailyV1RuntimeAdvanceBoundary, IDailyV1SelectedRuntimeAdvanceHostPorts
{
    [ThreadStatic]
    private static DailyGameplayExecutionProbeForTest activeDailyGameplayExecutionProbeForTest;

    /// <summary>
    /// Scoped test witness for entry into the normal daily gameplay callback
    /// pipeline. It observes calls only and does not alter day execution.
    /// </summary>
    internal sealed class DailyGameplayExecutionProbeForTest : IDisposable
    {
        private bool disposed;

        internal int AdvanceDayAfterClockAdvanceInvocationCount { get; private set; }

        internal DailyGameplayExecutionProbeForTest()
        {
            if (activeDailyGameplayExecutionProbeForTest != null)
                throw new InvalidOperationException("A daily gameplay execution probe is already active on this thread.");
            activeDailyGameplayExecutionProbeForTest = this;
        }

        internal void RecordAdvanceDayAfterClockAdvanceInvocation()
        {
            AdvanceDayAfterClockAdvanceInvocationCount++;
        }

        public void Dispose()
        {
            if (disposed) return;
            if (!ReferenceEquals(activeDailyGameplayExecutionProbeForTest, this))
                throw new InvalidOperationException("The daily gameplay execution probe is not the active probe on this thread.");
            activeDailyGameplayExecutionProbeForTest = null;
            disposed = true;
        }
    }

    internal static DailyGameplayExecutionProbeForTest BeginDailyGameplayExecutionProbeForTest()
    {
        return new DailyGameplayExecutionProbeForTest();
    }

    private static readonly string[] P12GenealogyOwnerSectionIds =
    {
        GenealogyCensusProvider.SectionId
    };

    private sealed class NpcMembershipCensusContext
    {
        public readonly long PersonStoreRevisionAtStart;
        public readonly Thread OwnerThread;
        public readonly int OwnerManagedThreadId;
        public SimulationOperationScope ProtocolScope;
        public ContinuationMutationEpochReservation EpochReservation;
        public int NestingDepth;
        public long PersonStoreRevisionDelta;
        public bool RosterChanged;
        public readonly HashSet<string> ChangedCityPresenceSectionIds = new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<string> ChangedRuntimeIdentitySectionIds = new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<string> AllowedLifecycleSectionIds = new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<string> ChangedLifecycleSectionIds = new HashSet<string>(StringComparer.Ordinal);

        public NpcMembershipCensusContext(
            long personStoreRevisionAtStart,
            Thread ownerThread,
            IEnumerable<string> allowedLifecycleSectionIds)
        {
            PersonStoreRevisionAtStart = personStoreRevisionAtStart;
            OwnerThread = ownerThread ?? throw new ArgumentNullException(nameof(ownerThread));
            OwnerManagedThreadId = ownerThread.ManagedThreadId;
            NestingDepth = 1;
            if (allowedLifecycleSectionIds != null)
                foreach (string sectionId in allowedLifecycleSectionIds)
                    if (!string.IsNullOrWhiteSpace(sectionId))
                        AllowedLifecycleSectionIds.Add(sectionId);
        }

        public bool IsOwnedByCurrentThread() => ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class NpcMembershipCensusScope : IDisposable
    {
        private readonly SimulationRuntime owner;
        private readonly NpcMembershipCensusContext context;
        private bool disposed;

        public NpcMembershipCensusScope(SimulationRuntime owner, NpcMembershipCensusContext context)
        {
            this.owner = owner;
            this.context = context;
        }

        public bool CanProceed => owner == null
            || owner.isComposingNpcRoster
            || owner.runtimeAdmissionContext == null
            || (context != null && context.ProtocolScope != null && context.EpochReservation != null);

        public void MarkRosterChanged()
        {
            if (context == null) return;
            if (!context.IsOwnedByCurrentThread())
            {
                owner.FaultNpcMembershipCensusBoundary();
                return;
            }
            context.RosterChanged = true;
        }

        public void MarkCityPresenceChanged(CityRuntime city)
        {
            if (context == null || city == null) return;
            if (!context.IsOwnedByCurrentThread())
            {
                owner.FaultNpcMembershipCensusBoundary();
                return;
            }
            if (owner.cityNpcPresenceSectionIdsByOwner.TryGetValue(city, out string sectionId))
                context.ChangedCityPresenceSectionIds.Add(sectionId);
        }

        public void MarkRuntimeIdentityChanged()
        {
            if (context == null) return;
            if (!context.IsOwnedByCurrentThread())
            {
                owner.FaultNpcMembershipCensusBoundary();
                return;
            }
            foreach (string sectionId in P12RuntimeIdentitySectionIds)
                context.ChangedRuntimeIdentitySectionIds.Add(sectionId);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            owner.ExitNpcMembershipCensusScope(context);
        }
    }

    private sealed class P12NpcOwnerMutationBinding
    {
        public readonly object Owner;
        public readonly string[] SectionIds;
        public NpcRuntime OwnerNpc;
        public Func<bool> Admission;
        public Action Committed;
        public Func<bool, IReadOnlyList<CityRuntime>, bool> TravelAdmission;
        public Action<bool, IReadOnlyList<CityRuntime>> TravelCommitted;

        public P12NpcOwnerMutationBinding(object owner, string[] sectionIds)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            SectionIds = sectionIds ?? throw new ArgumentNullException(nameof(sectionIds));
        }
    }

    private sealed class P12MerchantOperationContext
    {
        public readonly Thread OwnerThread;
        public readonly int OwnerManagedThreadId;
        public readonly HashSet<string> ChangedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        public SimulationOperationScope ProtocolScope;

        public P12MerchantOperationContext(Thread ownerThread, SimulationOperationScope protocolScope)
        {
            OwnerThread = ownerThread ?? throw new ArgumentNullException(nameof(ownerThread));
            OwnerManagedThreadId = ownerThread.ManagedThreadId;
            ProtocolScope = protocolScope ?? throw new ArgumentNullException(nameof(protocolScope));
        }

        public bool IsOwnedByCurrentThread() => ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class P12TravelPartyAdvanceOperationContext
    {
        public readonly Thread OwnerThread;
        public readonly int OwnerManagedThreadId;
        public readonly HashSet<string> ChangedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        public SimulationOperationScope ProtocolScope;

        public P12TravelPartyAdvanceOperationContext(Thread ownerThread, SimulationOperationScope protocolScope)
        {
            OwnerThread = ownerThread ?? throw new ArgumentNullException(nameof(ownerThread));
            OwnerManagedThreadId = ownerThread.ManagedThreadId;
            ProtocolScope = protocolScope ?? throw new ArgumentNullException(nameof(protocolScope));
        }

        public bool IsOwnedByCurrentThread() => ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class P12TravelPartyAdvanceOperationScope : IDisposable
    {
        private readonly SimulationRuntime owner;
        private readonly P12TravelPartyAdvanceOperationContext context;
        private bool disposed;

        public P12TravelPartyAdvanceOperationScope(
            SimulationRuntime owner,
            P12TravelPartyAdvanceOperationContext context)
        {
            this.owner = owner;
            this.context = context;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            owner?.ExitP12TravelPartyAdvanceOperation(context);
        }
    }

    private sealed class P12SoloTravelStartOperationContext
    {
        public readonly Thread OwnerThread;
        public readonly int OwnerManagedThreadId;
        public readonly HashSet<string> ChangedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        public SimulationOperationScope ProtocolScope;

        public P12SoloTravelStartOperationContext(Thread ownerThread, SimulationOperationScope protocolScope)
        {
            OwnerThread = ownerThread ?? throw new ArgumentNullException(nameof(ownerThread));
            OwnerManagedThreadId = ownerThread.ManagedThreadId;
            ProtocolScope = protocolScope ?? throw new ArgumentNullException(nameof(protocolScope));
        }

        public bool IsOwnedByCurrentThread() => ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class P12SoloTravelStartOperationScope : IDisposable
    {
        private readonly SimulationRuntime owner;
        private readonly P12SoloTravelStartOperationContext context;
        private bool disposed;

        public P12SoloTravelStartOperationScope(
            SimulationRuntime owner,
            P12SoloTravelStartOperationContext context)
        {
            this.owner = owner;
            this.context = context;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            owner?.ExitP12SoloTravelStartOperation(context);
        }
    }

    private sealed class SelectedSoloTravelActionBoundaryAdapter
        : DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary
    {
        private readonly Func<Func<bool>, bool> executeWithinOperation;
        private readonly Func<bool> canStartTradeReposition;
        private readonly Action logTradeReposition;
        private readonly Func<DailyV1SelectedTravelTransitionPlan, SpatialRouteRuntime, float, bool>
            canCommitPreparedTransition;
        private readonly Func<DailyV1SelectedTravelTransitionPlan, bool> tryPrepareTransitionProjection;
        private readonly Func<DailyV1SelectedTravelTransitionPlan, bool> canAdmitPreparedTransition;
        private readonly Action<DailyV1SelectedTravelTransitionPlan> publishCommittedTransition;
        private readonly Action<DailyV1SelectedActionExecutionStage.SoloTravelResult> publishTravelStarted;
        private readonly Action logCommercialScouting;
        private readonly Func<DailyV1NpcRuntimeOwners, bool> canProjectTravelPlanClear;
        private readonly Func<DailyV1NpcRuntimeOwners, bool> canAdmitTravelPlanClear;
        private readonly Action<DailyV1NpcRuntimeOwners> publishCommittedTravelPlanClear;

        internal SelectedSoloTravelActionBoundaryAdapter(
            Func<Func<bool>, bool> executeWithinOperation,
            Func<bool> canStartTradeReposition,
            Action logTradeReposition,
            Func<DailyV1SelectedTravelTransitionPlan, SpatialRouteRuntime, float, bool>
                canCommitPreparedTransition,
            Func<DailyV1SelectedTravelTransitionPlan, bool> tryPrepareTransitionProjection,
            Func<DailyV1SelectedTravelTransitionPlan, bool> canAdmitPreparedTransition,
            Action<DailyV1SelectedTravelTransitionPlan> publishCommittedTransition,
            Action<DailyV1SelectedActionExecutionStage.SoloTravelResult> publishTravelStarted,
            Action logCommercialScouting,
            Func<DailyV1NpcRuntimeOwners, bool> canProjectTravelPlanClear,
            Func<DailyV1NpcRuntimeOwners, bool> canAdmitTravelPlanClear,
            Action<DailyV1NpcRuntimeOwners> publishCommittedTravelPlanClear)
        {
            this.executeWithinOperation = executeWithinOperation
                ?? throw new ArgumentNullException(nameof(executeWithinOperation));
            this.canStartTradeReposition = canStartTradeReposition
                ?? throw new ArgumentNullException(nameof(canStartTradeReposition));
            this.logTradeReposition = logTradeReposition
                ?? throw new ArgumentNullException(nameof(logTradeReposition));
            this.canCommitPreparedTransition = canCommitPreparedTransition
                ?? throw new ArgumentNullException(nameof(canCommitPreparedTransition));
            this.tryPrepareTransitionProjection = tryPrepareTransitionProjection
                ?? throw new ArgumentNullException(nameof(tryPrepareTransitionProjection));
            this.canAdmitPreparedTransition = canAdmitPreparedTransition
                ?? throw new ArgumentNullException(nameof(canAdmitPreparedTransition));
            this.publishCommittedTransition = publishCommittedTransition
                ?? throw new ArgumentNullException(nameof(publishCommittedTransition));
            this.publishTravelStarted = publishTravelStarted
                ?? throw new ArgumentNullException(nameof(publishTravelStarted));
            this.logCommercialScouting = logCommercialScouting
                ?? throw new ArgumentNullException(nameof(logCommercialScouting));
            this.canProjectTravelPlanClear = canProjectTravelPlanClear
                ?? throw new ArgumentNullException(nameof(canProjectTravelPlanClear));
            this.canAdmitTravelPlanClear = canAdmitTravelPlanClear
                ?? throw new ArgumentNullException(nameof(canAdmitTravelPlanClear));
            this.publishCommittedTravelPlanClear = publishCommittedTravelPlanClear
                ?? throw new ArgumentNullException(nameof(publishCommittedTravelPlanClear));
        }

        public bool ExecuteWithinOperation(Func<bool> operation) => executeWithinOperation(operation);
        public bool CanStartTradeReposition() => canStartTradeReposition();
        public void LogTradeReposition() => logTradeReposition();
        public bool CanCommitPreparedTransition(DailyV1SelectedTravelTransitionPlan plan,
            SpatialRouteRuntime route, float travelCost) =>
            canCommitPreparedTransition(plan, route, travelCost);
        public bool TryPrepareTransitionProjection(DailyV1SelectedTravelTransitionPlan plan) =>
            tryPrepareTransitionProjection(plan);
        public bool CanAdmitPreparedTransition(DailyV1SelectedTravelTransitionPlan plan) =>
            canAdmitPreparedTransition(plan);
        public void PublishCommittedTransition(DailyV1SelectedTravelTransitionPlan plan) =>
            publishCommittedTransition(plan);
        public void PublishTravelStarted(DailyV1SelectedActionExecutionStage.SoloTravelResult result) =>
            publishTravelStarted(result);
        public void LogCommercialScouting() => logCommercialScouting();
        public bool CanProjectTravelPlanClear(DailyV1NpcRuntimeOwners actor) =>
            canProjectTravelPlanClear(actor);
        public bool CanAdmitTravelPlanClear(DailyV1NpcRuntimeOwners actor) =>
            canAdmitTravelPlanClear(actor);
        public void PublishCommittedTravelPlanClear(DailyV1NpcRuntimeOwners actor) =>
            publishCommittedTravelPlanClear(actor);
    }

    /// <summary>Bridges the separate selected status operation to exact Unity
    /// owner projections and the existing P12 mutation scope.</summary>
    private sealed class SelectedActionStatusOperationBoundaryAdapter
        : DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary
    {
        private readonly SimulationRuntime runtime;
        private readonly NpcRuntime actorRuntime;
        private readonly NpcRuntime targetRuntime;
        private readonly bool reuseForcedEscapeScope;

        internal SelectedActionStatusOperationBoundaryAdapter(
            SimulationRuntime runtime,
            NpcRuntime actorRuntime,
            NpcRuntime targetRuntime,
            bool reuseForcedEscapeScope)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.actorRuntime = actorRuntime ?? throw new ArgumentNullException(nameof(actorRuntime));
            this.targetRuntime = targetRuntime;
            this.reuseForcedEscapeScope = reuseForcedEscapeScope;
        }

        public DailyV1SelectedActionStatusPlanResult ExecuteWithinOperation(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcRuntimeOwners target,
            Func<DailyV1SelectedActionStatusPlanResult> operation)
        {
            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            if (operation == null || graph == null
                || !graph.ContainsExactNpcOwner(actor)
                || !ReferenceEquals(actorRuntime.SelectedRuntimeOwnerGraph, graph)
                || !ReferenceEquals(actorRuntime.SelectedRuntimeOwners, actor)
                || !IsExactRuntimeProjection(graph, actor, actorRuntime))
                return DailyV1SelectedActionStatusPlanResult.ActorRejected;

            if (targetRuntime == null
                ? target != null
                : graph.ContainsExactNpcOwner(target) == false
                    || !ReferenceEquals(targetRuntime.SelectedRuntimeOwnerGraph, graph)
                    || !ReferenceEquals(targetRuntime.SelectedRuntimeOwners, target)
                    || !IsExactRuntimeProjection(graph, target, targetRuntime))
                return DailyV1SelectedActionStatusPlanResult.TargetRejected;

            IReadOnlyList<NpcRuntime> participants = targetRuntime == null
                ? new[] { actorRuntime }
                : new[] { actorRuntime, targetRuntime };
            return runtime.ApplySelectedActionStatusOperation(
                participants, reuseForcedEscapeScope, operation);
        }

        public bool CanProjectStatusChanges(
            DailyV1NpcRuntimeOwners owner,
            IReadOnlyList<DailyV1StatusDefinition> statusToAdd) =>
            TryGetExactParticipant(owner, out NpcRuntime exactRuntime)
            && exactRuntime.CanProjectCoreSelectedDailyV1StatusChanges(owner, statusToAdd);

        public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners owner)
        {
            if (!TryGetExactParticipant(owner, out NpcRuntime exactRuntime))
                throw new InvalidOperationException("The selected status owner lost its exact runtime projection.");
            exactRuntime.EnsureCoreSelectedDailyV1OwnerMutationAllowed(owner);
        }

        public void NotifyStatusMutationCommitted(
            DailyV1NpcRuntimeOwners owner,
            DailyV1StatusDefinition status,
            bool added)
        {
            if (!TryGetExactParticipant(owner, out NpcRuntime exactRuntime))
                throw new InvalidOperationException("The selected status owner lost its exact runtime projection after mutation.");
            exactRuntime.NotifyCoreSelectedDailyV1StatusMutationCommitted(owner);
        }

        public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners owner)
        {
            if (!TryGetExactParticipant(owner, out NpcRuntime exactRuntime))
                throw new InvalidOperationException("The selected hidden-state owner lost its exact runtime projection after mutation.");
            exactRuntime.NotifyCoreSelectedDailyV1HiddenMutationCommitted(owner);
        }

        private bool TryGetExactParticipant(
            DailyV1NpcRuntimeOwners owner,
            out NpcRuntime exactRuntime)
        {
            exactRuntime = null;
            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            if (owner == null || graph == null || !graph.ContainsExactNpcOwner(owner)) return false;
            if (ReferenceEquals(owner, actorRuntime.SelectedRuntimeOwners))
            {
                if (!IsExactRuntimeProjection(graph, owner, actorRuntime)) return false;
                exactRuntime = actorRuntime;
                return true;
            }
            if (targetRuntime != null
                && ReferenceEquals(owner, targetRuntime.SelectedRuntimeOwners)
                && IsExactRuntimeProjection(graph, owner, targetRuntime))
            {
                exactRuntime = targetRuntime;
                return true;
            }
            return false;
        }

        private bool IsExactRuntimeProjection(
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners owner,
            NpcRuntime expectedRuntime)
        {
            if (graph == null || owner == null || expectedRuntime == null
                || runtime.npcRuntimes == null || !graph.ContainsExactNpcOwner(owner))
                return false;

            int matches = 0;
            foreach (NpcRuntime candidate in runtime.npcRuntimes)
            {
                if (!ReferenceEquals(candidate?.SelectedRuntimeOwners, owner)) continue;
                if (!ReferenceEquals(candidate, expectedRuntime)) return false;
                matches++;
            }
            return matches == 1;
        }
    }

    /// <summary>
    /// Resolves only the exact admitted Unity projections and existing P12
    /// adapters needed by Core's selected-action dispatcher.
    /// </summary>
    private sealed class SelectedActionDispatchBoundaryAdapter
        : IDailyV1SelectedActionDispatchBoundary
    {
        private readonly SimulationRuntime runtime;
        private readonly NpcRuntime actorRuntime;
        private readonly NpcActionRuntime actionRuntime;
        private readonly NpcActionData actionAsset;
        private SpatialLocationRuntime crossCityTargetLocation;

        internal SelectedActionDispatchBoundaryAdapter(
            SimulationRuntime runtime,
            NpcRuntime actorRuntime,
            NpcActionRuntime actionRuntime,
            NpcActionData actionAsset)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.actorRuntime = actorRuntime ?? throw new ArgumentNullException(nameof(actorRuntime));
            this.actionRuntime = actionRuntime ?? throw new ArgumentNullException(nameof(actionRuntime));
            this.actionAsset = actionAsset ?? throw new ArgumentNullException(nameof(actionAsset));
        }

        public bool TryGetSoloTravelBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedActionExecutionStage.ISelectedSoloTravelActionBoundary boundary)
        {
            boundary = null;
            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            if (!TryGetExactActor(actor, action, out NpcRuntime exactActor)
                || !ReferenceEquals(exactActor, actorRuntime)
                || graph?.TravelParties == null
                || !TryGetExactCityProjection(
                    graph,
                    actor.ActionSlot?.CurrentPayload?.TargetCityRuntimeId,
                    actionRuntime.TargetCity,
                    out _, out _)
                || runtime.npcDecisionSystem?.GetProviderForAction(exactActor, actionAsset)
                    is not TravelActionProvider travelProvider
                || !ReferenceEquals(graph.TravelParties, travelProvider.SelectedTravelPartyStore))
                return false;

            boundary = runtime.CreateSelectedSoloTravelActionBoundary(
                exactActor, travelProvider, actionRuntime);
            return boundary != null;
        }

        public bool TryGetFleeCityActionBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary boundary)
        {
            boundary = null;
            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            if (!TryGetExactActor(actor, action, out NpcRuntime exactActor)
                || !ReferenceEquals(exactActor, actorRuntime)
                || !(runtime.npcDecisionSystem?.GetProviderForAction(exactActor, actionAsset)
                    is CrimeSystem)
                || graph?.TravelParties == null
                || runtime.travelSystem == null
                || !ReferenceEquals(graph.TravelParties, runtime.travelSystem.SelectedTravelPartyStore)
                || !TryGetExactCityProjection(
                    graph,
                    actor.ActionSlot?.CurrentPayload?.TargetCityRuntimeId,
                    actionRuntime.TargetCity,
                    out DailyV1CityRuntimeOwners targetCityOwner,
                    out _))
                return false;

            boundary = new SelectedFleeCityActionBoundaryAdapter(
                this, runtime, exactActor, actionRuntime.TargetCity, targetCityOwner);
            return true;
        }

        public bool TryGetGuardArrestActionBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary boundary)
        {
            boundary = null;
            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            if (!TryGetExactActor(actor, action, out NpcRuntime exactGuard)
                || !ReferenceEquals(exactGuard, actorRuntime)
                || graph == null || graph.JusticeRecordsOwner == null
                || runtime.justiceSystem == null
                || !ReferenceEquals(graph.JusticeRecordsOwner, runtime.justiceSystem.CoreOwner)
                || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.ArrestActionTypeValue
                || !DailyV1SelectedNpcActionDecisionStage.IsEnabledForRuntime(
                    graph.Definitions, action))
                return false;

            NpcRuntime exactTarget = null;
            string targetRuntimeId = actor.ActionSlot?.CurrentPayload?.TargetNpcRuntimeId;
            if (targetRuntimeId == null)
            {
                if (actionRuntime.TargetNpc != null) return false;
            }
            else if (!graph.TryGetNpcOwner(targetRuntimeId, out DailyV1NpcRuntimeOwners targetOwner)
                || !TryFindExactNpcRuntime(targetOwner, out exactTarget)
                || !ReferenceEquals(exactTarget, actionRuntime.TargetNpc))
                return false;

            boundary = new SelectedGuardArrestActionBoundaryAdapter(
                this, runtime, exactGuard, exactTarget, graph.JusticeRecordsOwner);
            return true;
        }

        public bool TryGetStealActionBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary boundary)
        {
            boundary = null;
            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            CrimeSocialAppraisalWorldState socialWorld = runtime.crimeSocialAppraisalWorldState;
            if (!TryGetExactActor(actor, action, out NpcRuntime exactActor)
                || !ReferenceEquals(exactActor, actorRuntime)
                || graph == null || graph.JusticeRecordsOwner == null
                || runtime.justiceSystem == null
                || !ReferenceEquals(graph.JusticeRecordsOwner, runtime.justiceSystem.CoreOwner)
                || runtime.crimeSystem == null
                || runtime.economyTransactionService == null
                || socialWorld == null
                || !ReferenceEquals(socialWorld.PersonStore, runtime.personStore)
                || !ReferenceEquals(socialWorld.SimulationTime, runtime.simulationTime)
                || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.StealActionTypeValue
                || !(runtime.npcDecisionSystem?.GetProviderForAction(exactActor, actionAsset)
                    is CrimeSystem))
                return false;

            DailyV1NpcActionPayloadOwner payload = actor.ActionSlot?.CurrentPayload;
            if (payload == null || string.IsNullOrWhiteSpace(payload.TargetNpcRuntimeId)
                || payload.TargetItem != null || payload.TargetCityRuntimeId != null
                || actionRuntime.Amount != payload.Amount
                || !string.Equals(actionRuntime.StableOccurrenceKey,
                    payload.StableOccurrenceKey, StringComparison.Ordinal)
                || !string.Equals(actionRuntime.OriginDecisionId,
                    payload.OriginDecisionId, StringComparison.Ordinal)
                || !graph.TryGetNpcOwner(payload.TargetNpcRuntimeId,
                    out DailyV1NpcRuntimeOwners targetOwner)
                || !TryFindExactNpcRuntime(targetOwner, out NpcRuntime exactTarget)
                || !ReferenceEquals(exactTarget, actionRuntime.TargetNpc)
                || !ReferenceEquals(exactTarget.SelectedRuntimeOwners, targetOwner)
                || !ReferenceEquals(exactTarget.CurrentCity, exactActor.CurrentCity)
                || !TryGetExactCityProjection(graph,
                    actor.Travel?.CurrentCityRuntimeId,
                    exactActor.CurrentCity,
                    out DailyV1CityRuntimeOwners exactCity,
                    out _))
                return false;

            boundary = new SelectedStealActionBoundaryAdapter(
                this, runtime, exactActor, exactTarget, exactCity,
                graph.JusticeRecordsOwner, socialWorld);
            return true;
        }

        public bool TryGetCrimeActionBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        {
            boundary = null;
            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            if (!TryGetExactActor(actor, action, out NpcRuntime exactActor)
                || !ReferenceEquals(exactActor, actorRuntime)
                || graph == null || graph.JusticeRecordsOwner == null
                || runtime.justiceSystem == null
                || !ReferenceEquals(graph.JusticeRecordsOwner, runtime.justiceSystem.CoreOwner)
                || (action.Values.actionType?.Value != DailyV1NpcDefinitionRules.HideActionTypeValue
                    && action.Values.actionType?.Value != DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue))
                return false;

            DailyV1NpcActionPayloadOwner payload = actor.ActionSlot?.CurrentPayload;
            if (payload == null || payload.TargetNpcRuntimeId != null
                || payload.TargetCityRuntimeId != null || payload.TargetItem != null || payload.Amount != 0
                || actionRuntime.TargetNpc != null || actionRuntime.TargetCity != null
                || actionRuntime.TargetItem != null || actionRuntime.Amount != 0
                || !actionRuntime.SuccessChanceMultiplier.Equals(payload.SuccessChanceMultiplier)
                || !string.Equals(actionRuntime.OriginDecisionId, payload.OriginDecisionId,
                    StringComparison.Ordinal))
                return false;

            boundary = new SelectedCrimeActionBoundaryAdapter(
                this, runtime, exactActor, graph.JusticeRecordsOwner);
            return true;
        }

        public bool TryGetForcedEscapeBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        {
            boundary = null;
            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            if (!TryGetExactActor(actor, action, out NpcRuntime exactActor)
                || !ReferenceEquals(exactActor, actorRuntime)
                || graph == null || graph.JusticeRecordsOwner == null
                || runtime.justiceSystem == null
                || !ReferenceEquals(graph.JusticeRecordsOwner, runtime.justiceSystem.CoreOwner)
                || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue)
                return false;

            DailyV1NpcActionPayloadOwner payload = actor.ActionSlot?.CurrentPayload;
            if (payload == null || payload.TargetNpcRuntimeId != null
                || payload.TargetCityRuntimeId != null || payload.TargetItem != null || payload.Amount != 0
                || actionRuntime.TargetNpc != null || actionRuntime.TargetCity != null
                || actionRuntime.TargetItem != null || actionRuntime.Amount != 0
                || !actionRuntime.SuccessChanceMultiplier.Equals(payload.SuccessChanceMultiplier)
                || !string.Equals(actionRuntime.OriginDecisionId, payload.OriginDecisionId,
                    StringComparison.Ordinal))
                return false;

            boundary = new SelectedCrimeActionBoundaryAdapter(
                this, runtime, exactActor, graph.JusticeRecordsOwner);
            return true;
        }

        public bool TryGetMarketBoundaries(
            DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedOpenMarketSaleBoundary saleBoundary,
            out IDailyV1SelectedOpenMarketPurchaseBoundary purchaseBoundary)
        {
            saleBoundary = null;
            purchaseBoundary = null;
            crossCityTargetLocation = null;
            if (!TryGetExactActor(actor, actor?.ActionSlot?.CurrentAction, out NpcRuntime exactActor)
                || !ReferenceEquals(exactActor, actorRuntime)
                || runtime.economyTransactionService == null)
                return false;

            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            DailyV1NpcActionPayloadOwner payload = actor.ActionSlot?.CurrentPayload;
            if (graph == null || payload == null)
                return false;

            if (actionRuntime.TargetCity != null
                && !ReferenceEquals(actionRuntime.TargetCity, exactActor.CurrentCity))
            {
                if (actor.ActionSlot.CurrentAction.Values.actionType?.Value
                        != DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue
                    || !runtime.economyTransactionService.TryResolveSelectedCrossCityBuyTarget(
                        exactActor, payload, actionRuntime.TargetCity,
                        out crossCityTargetLocation))
                    return false;
            }
            else if (actionRuntime.TargetCity != null
                && !TryGetExactActionCityProjection(graph, payload.TargetCityRuntimeId,
                    actionRuntime.TargetCity, exactActor, actor.ActionSlot.CurrentAction))
                return false;
            else if (actionRuntime.TargetCity == null
                && payload.TargetCityRuntimeId != null)
                return false;

            return runtime.economyTransactionService.TryCreateSelectedActionMarketBoundaries(
                exactActor, out saleBoundary, out purchaseBoundary);
        }

        public bool TryGetNpcTradeBoundary(
            DailyV1NpcRuntimeOwners seller,
            DailyV1NpcRuntimeOwners buyer,
            out IDailyV1SelectedNpcTradeBoundary boundary)
        {
            boundary = null;
            if (!TryGetExactActor(seller, seller?.ActionSlot?.CurrentAction, out NpcRuntime exactSeller)
                || !ReferenceEquals(exactSeller, actorRuntime)
                || runtime.economyTransactionService == null
                || !TryFindExactNpcRuntime(buyer, out NpcRuntime exactBuyer)
                || !ReferenceEquals(exactBuyer, actionRuntime.TargetNpc))
                return false;

            return runtime.economyTransactionService != null
                && runtime.economyTransactionService.TryCreateSelectedActionNpcTradeBoundary(
                    exactSeller, exactBuyer, out boundary);
        }

        public bool CanAdmitMerchantPlanMutation(DailyV1NpcRuntimeOwners actor)
        {
            return TryGetExactActor(actor, actor?.ActionSlot?.CurrentAction,
                    out NpcRuntime exactActor)
                && ReferenceEquals(exactActor, actorRuntime)
                && exactActor.MerchantTradePlan.CanAdmitSelectedPlanOwnerMutation();
        }

        public void PublishMerchantPlanMutation(DailyV1NpcRuntimeOwners actor)
        {
            if (!TryGetExactActor(actor, actor?.ActionSlot?.CurrentAction,
                    out NpcRuntime exactActor)
                || !ReferenceEquals(exactActor, actorRuntime))
                throw new InvalidOperationException(
                    "The selected Merchant plan mutation returned through a mismatched runtime projection.");
            exactActor.MerchantTradePlan.PublishSelectedPlanOwnerMutation();
        }

        public bool CanAdmitTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor)
        {
            return TryGetExactActor(actor, actor?.ActionSlot?.CurrentAction,
                    out NpcRuntime exactActor)
                && ReferenceEquals(exactActor, actorRuntime)
                && exactActor.TravelPlan.CanAdmitSelectedTradeTravelPlanMutation();
        }

        public void PublishTradeTravelPlanMutation(DailyV1NpcRuntimeOwners actor)
        {
            if (!TryGetExactActor(actor, actor?.ActionSlot?.CurrentAction,
                    out NpcRuntime exactActor)
                || !ReferenceEquals(exactActor, actorRuntime))
                throw new InvalidOperationException(
                    "The selected Travel plan mutation returned through a mismatched runtime projection.");
            exactActor.TravelPlan.PublishSelectedTradeTravelPlanMutation();
        }

        public void CompleteCrossCityBuy(
            DailyV1NpcRuntimeOwners actor,
            DailyV1SelectedCrossCityBuyResult result)
        {
            if (!TryGetExactActor(actor, actor?.ActionSlot?.CurrentAction, out NpcRuntime exactActor)
                || !ReferenceEquals(exactActor, actorRuntime)
                || runtime.economyTransactionService == null)
                throw new InvalidOperationException(
                    "The selected cross-city buy returned through a mismatched runtime projection.");

            runtime.economyTransactionService.CompleteSelectedCrossCityBuyDispatch(
                exactActor, result, crossCityTargetLocation, actionRuntime.TargetCity);
        }

        private bool TryGetExactActor(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition expectedAction,
            out NpcRuntime exactRuntime)
        {
            exactRuntime = null;
            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            if (actor == null || expectedAction == null || graph == null
                || !ReferenceEquals(graph, actorRuntime.SelectedRuntimeOwnerGraph)
                || !graph.ContainsExactNpcOwner(actor)
                || !ReferenceEquals(actorRuntime.SelectedRuntimeOwners, actor)
                || !ReferenceEquals(actor.ActionSlot?.CurrentAction, expectedAction)
                || !ReferenceEquals(actor.ActionSlot?.CurrentPayload, actionRuntime.SelectedOwner)
                || !ReferenceEquals(actionRuntime.Action, actionAsset)
                || !actionRuntime.MatchesSelectedOwner()
                || !actorRuntime.TryGetSelectedDailyV1ActionAsset(expectedAction,
                    out NpcActionData exactActionAsset)
                || !ReferenceEquals(exactActionAsset, actionAsset)
                || !TryFindExactNpcRuntime(actor, out exactRuntime)
                || !ReferenceEquals(exactRuntime, actorRuntime))
                return false;

            DailyV1NpcActionPayloadOwner payload = actor.ActionSlot.CurrentPayload;
            if (payload.TargetNpcRuntimeId == null)
            {
                if (actionRuntime.TargetNpc != null) return false;
            }
            else if (!TryFindExactNpcRuntime(
                    FindNpcOwner(graph, payload.TargetNpcRuntimeId),
                    out NpcRuntime targetNpc)
                || !ReferenceEquals(targetNpc, actionRuntime.TargetNpc))
                return false;

            if (payload.TargetCityRuntimeId == null)
            {
                if (actionRuntime.TargetCity != null) return false;
            }
            else if (!TryGetExactActionCityProjection(graph, payload.TargetCityRuntimeId,
                    actionRuntime.TargetCity, actorRuntime, expectedAction))
                return false;

            if (payload.TargetItem == null)
            {
                if (actionRuntime.TargetItem != null) return false;
            }
            else if (!actorRuntime.TryGetSelectedDailyV1Item(
                    actionRuntime.TargetItem, out DailyV1ItemDefinition exactItem)
                || !ReferenceEquals(exactItem, payload.TargetItem))
                return false;

            return true;
        }

        private bool TryGetExactActionCityProjection(
            DailyV1SelectedRuntimeOwnerGraph graph,
            string runtimeId,
            CityRuntime projectedCity,
            NpcRuntime exactActor,
            DailyV1ActionDefinition action)
        {
            int? actionType = action?.Values.actionType?.Value;
            bool isLocalMarketAction = (actionType == DailyV1NpcDefinitionRules.BuyGoodsActionTypeValue
                    || actionType == DailyV1NpcDefinitionRules.SellGoodsActionTypeValue)
                && exactActor != null && projectedCity != null
                && ReferenceEquals(projectedCity, exactActor.CurrentCity);
            if (isLocalMarketAction
                && !TryGetExactLocalMarketActionCityProjection(
                    graph, runtimeId, projectedCity, exactActor))
                return false;

            if (TryGetExactCityProjection(graph, runtimeId, projectedCity, out _, out _))
                return true;
            return isLocalMarketAction;
        }

        private bool TryGetExactLocalMarketActionCityProjection(
            DailyV1SelectedRuntimeOwnerGraph graph,
            string runtimeId,
            CityRuntime projectedCity,
            NpcRuntime exactActor)
        {
            if (graph == null || exactActor == null || projectedCity == null
                || !ReferenceEquals(projectedCity, exactActor.CurrentCity)
                || !ReferenceEquals(exactActor.CurrentLocation, projectedCity.Location)
                || exactActor.SelectedRuntimeOwners?.Travel == null
                || !string.Equals(runtimeId,
                    exactActor.SelectedRuntimeOwners.Travel.CurrentCityRuntimeId, StringComparison.Ordinal)
                || !string.Equals(exactActor.SelectedRuntimeOwners.Travel.CurrentLocationRuntimeId,
                    projectedCity.Location?.RuntimeId, StringComparison.Ordinal)
                || !string.Equals(projectedCity.RuntimeId, runtimeId, StringComparison.Ordinal)
                || !IsExactCityProjection(projectedCity.SelectedRuntimeOwners, projectedCity))
                return false;

            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
                if (candidate != null
                    && string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
                    return graph.ContainsExactCityOwner(candidate)
                        && ReferenceEquals(candidate, projectedCity.SelectedRuntimeOwners)
                        && string.Equals(candidate.LocationRuntimeId,
                            exactActor.SelectedRuntimeOwners.Travel.CurrentLocationRuntimeId,
                            StringComparison.Ordinal);
            return false;
        }

        private bool TryFindExactNpcRuntime(
            DailyV1NpcRuntimeOwners owner,
            out NpcRuntime exactRuntime)
        {
            exactRuntime = null;
            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            if (owner == null || graph == null
                || !ReferenceEquals(graph, actorRuntime.SelectedRuntimeOwnerGraph)
                || !graph.ContainsExactNpcOwner(owner))
                return false;

            foreach (NpcRuntime candidate in runtime.npcRuntimes)
            {
                if (!ReferenceEquals(candidate?.SelectedRuntimeOwners, owner)) continue;
                if (exactRuntime != null) return false;
                exactRuntime = candidate;
            }
            return exactRuntime != null;
        }

        private static DailyV1NpcRuntimeOwners FindNpcOwner(
            DailyV1SelectedRuntimeOwnerGraph graph,
            string runtimeId)
        {
            if (graph == null || string.IsNullOrWhiteSpace(runtimeId)) return null;
            foreach (DailyV1NpcRuntimeOwners candidate in graph.Npcs)
                if (candidate != null
                    && string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
                    return graph.ContainsExactNpcOwner(candidate) ? candidate : null;
            return null;
        }

        private bool TryGetExactCityProjection(
            DailyV1SelectedRuntimeOwnerGraph graph,
            string runtimeId,
            CityRuntime projectedCity,
            out DailyV1CityRuntimeOwners exactOwner,
            out SpatialLocationRuntime exactLocation)
        {
            exactOwner = null;
            exactLocation = null;
            if (graph == null || string.IsNullOrWhiteSpace(runtimeId) || projectedCity == null)
                return false;

            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            {
                if (candidate == null
                    || !string.Equals(candidate.RuntimeId, runtimeId, StringComparison.Ordinal))
                    continue;
                if (exactOwner != null || !graph.ContainsExactCityOwner(candidate)) return false;
                exactOwner = candidate;
            }
            if (exactOwner == null
                || !ReferenceEquals(projectedCity.SelectedRuntimeOwners, exactOwner)
                || !IsExactCityProjection(exactOwner, projectedCity)
                || graph.SpatialNetwork == null
                || !graph.SpatialNetwork.TryGetLocation(
                    exactOwner.LocationRuntimeId, out exactLocation)
                || !ReferenceEquals(projectedCity.Location, exactLocation))
                return false;
            return true;
        }

        private bool IsExactCityProjection(
            DailyV1CityRuntimeOwners exactOwner,
            CityRuntime projectedCity)
        {
            if (exactOwner == null || projectedCity == null || runtime.cities == null)
                return false;

            foreach (CityRuntime candidate in runtime.cities)
            {
                if (!ReferenceEquals(candidate?.SelectedRuntimeOwners, exactOwner))
                    continue;
                return ReferenceEquals(candidate, projectedCity);
            }

            return false;
        }

        private sealed class SelectedFleeCityActionBoundaryAdapter
            : DailyV1SelectedActionExecutionStage.ISelectedFleeCityActionBoundary
        {
            private readonly SelectedActionDispatchBoundaryAdapter dispatchBoundary;
            private readonly SimulationRuntime runtime;
            private readonly NpcRuntime actorRuntime;
            private readonly CityRuntime targetCityProjection;
            private readonly DailyV1CityRuntimeOwners targetCityOwner;
            private DailyV1NpcRuntimeOwners preparedActorOwner;
            private NpcTravelPlanState preparedTravelPlanState;

            internal SelectedFleeCityActionBoundaryAdapter(
                SelectedActionDispatchBoundaryAdapter dispatchBoundary,
                SimulationRuntime runtime,
                NpcRuntime actorRuntime,
                CityRuntime targetCityProjection,
                DailyV1CityRuntimeOwners targetCityOwner)
            {
                this.dispatchBoundary = dispatchBoundary
                    ?? throw new ArgumentNullException(nameof(dispatchBoundary));
                this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
                this.actorRuntime = actorRuntime ?? throw new ArgumentNullException(nameof(actorRuntime));
                this.targetCityProjection = targetCityProjection
                    ?? throw new ArgumentNullException(nameof(targetCityProjection));
                this.targetCityOwner = targetCityOwner
                    ?? throw new ArgumentNullException(nameof(targetCityOwner));
            }

            public bool ExecuteWithinOperation(Func<bool> operation)
            {
                if (operation == null) return false;
                if (runtime.runtimeAdmissionContext == null) return operation();
                if (!runtime.TryBeginP12CrimeJusticeActionScope(
                        new[] { actorRuntime }, includeJustice: true,
                        out P12CrimeJusticeMutationScope scope))
                    throw new InvalidOperationException(
                        "P12 Crime/Justice FleeCity action could not be admitted.");
                using (scope) return operation();
            }

            public bool CanPrepareTravelPlanProjection(
                DailyV1NpcRuntimeOwners actor,
                DailyV1CityRuntimeOwners destination,
                float travelCost,
                string originDecisionId)
            {
                preparedActorOwner = null;
                preparedTravelPlanState = null;
                if (!ReferenceEquals(destination, targetCityOwner)
                    || !dispatchBoundary.TryGetExactActor(
                        actor, actor?.ActionSlot?.CurrentAction, out NpcRuntime exactActor)
                    || !ReferenceEquals(exactActor, actorRuntime)
                    || !dispatchBoundary.TryGetExactCityProjection(
                        runtime.SelectedRuntimeOwnerGraph,
                        destination.RuntimeId,
                        targetCityProjection,
                        out DailyV1CityRuntimeOwners exactCityOwner,
                        out SpatialLocationRuntime exactLocation)
                    || !ReferenceEquals(exactCityOwner, destination))
                    return false;

                NpcTravelPlanState next = new NpcTravelPlanState(
                    exactLocation,
                    targetCityProjection,
                    NpcTravelReason.Flee,
                    80f,
                    travelCost,
                    string.IsNullOrWhiteSpace(originDecisionId) ? null : originDecisionId);
                if (!exactActor.TravelPlan.CanProjectOwnerState(next)) return false;
                preparedActorOwner = actor;
                preparedTravelPlanState = next;
                return true;
            }

            public bool CanAdmitTravelPlanMutation() => preparedTravelPlanState != null
                && ReferenceEquals(preparedActorOwner, actorRuntime.SelectedRuntimeOwners)
                && dispatchBoundary.TryGetExactActor(
                    preparedActorOwner,
                    preparedActorOwner?.ActionSlot?.CurrentAction,
                    out NpcRuntime exactActor)
                && ReferenceEquals(exactActor, actorRuntime)
                && exactActor.TravelPlan.CanAdmitSelectedOwnerMutation();

            public void PublishCommittedTravelPlan(
                DailyV1NpcRuntimeOwners actor,
                DailyV1CityRuntimeOwners destination,
                float travelCost,
                string originDecisionId)
            {
                NpcTravelPlanState expected = preparedTravelPlanState;
                string normalizedDecisionId = string.IsNullOrWhiteSpace(originDecisionId)
                    ? null
                    : originDecisionId;
                if (expected == null
                    || !ReferenceEquals(actor, preparedActorOwner)
                    || !ReferenceEquals(actor, actorRuntime.SelectedRuntimeOwners)
                    || !ReferenceEquals(destination, targetCityOwner)
                    || !ReferenceEquals(expected.TargetLocation, targetCityProjection.Location)
                    || !ReferenceEquals(expected.TargetCity, targetCityProjection)
                    || expected.Reason != NpcTravelReason.Flee
                    || !expected.Utility.Equals(80f)
                    || !expected.ExpectedCost.Equals(travelCost)
                    || !string.Equals(expected.OriginDecisionId, normalizedDecisionId,
                        StringComparison.Ordinal)
                    || !actorRuntime.TravelPlan.PublishCommittedOwnerState(expected))
                    throw new InvalidOperationException(
                        "A committed selected FleeCity TravelPlan could not publish its Unity projection.");

                preparedActorOwner = null;
                preparedTravelPlanState = null;
            }
        }

        private sealed class SelectedGuardArrestActionBoundaryAdapter
            : DailyV1SelectedActionExecutionStage.ISelectedGuardArrestActionBoundary
        {
            private readonly SelectedActionDispatchBoundaryAdapter dispatchBoundary;
            private readonly SimulationRuntime runtime;
            private readonly NpcRuntime guardRuntime;
            private readonly NpcRuntime targetRuntime;

            public DailyV1JusticeRecordsOwner JusticeRecordsOwner { get; }

            internal SelectedGuardArrestActionBoundaryAdapter(
                SelectedActionDispatchBoundaryAdapter dispatchBoundary,
                SimulationRuntime runtime,
                NpcRuntime guardRuntime,
                NpcRuntime targetRuntime,
                DailyV1JusticeRecordsOwner justiceRecordsOwner)
            {
                this.dispatchBoundary = dispatchBoundary
                    ?? throw new ArgumentNullException(nameof(dispatchBoundary));
                this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
                this.guardRuntime = guardRuntime ?? throw new ArgumentNullException(nameof(guardRuntime));
                this.targetRuntime = targetRuntime;
                JusticeRecordsOwner = justiceRecordsOwner
                    ?? throw new ArgumentNullException(nameof(justiceRecordsOwner));
            }

            public bool ExecuteWithinOperation(Func<bool> operation)
            {
                if (operation == null) return false;
                if (runtime.runtimeAdmissionContext == null) return operation();
                IReadOnlyList<NpcRuntime> participants = targetRuntime == null
                    ? new[] { guardRuntime }
                    : new[] { guardRuntime, targetRuntime };
                if (!runtime.TryBeginP12CrimeJusticeActionScope(
                        participants, includeJustice: true,
                        out P12CrimeJusticeMutationScope scope))
                    throw new InvalidOperationException(
                        "P12 Crime/Justice Guard Arrest could not be admitted.");
                using (scope) return operation();
            }

            public bool TryPrepareSentenceProjection(
                DailyV1NpcRuntimeOwners target,
                DailyV1CityRuntimeOwners city,
                DailyV1JusticeWantedRecordOwner warrant,
                DailyV1JusticeSentenceOwner sentence)
            {
                DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
                if (!ReferenceEquals(graph?.JusticeRecordsOwner, JusticeRecordsOwner)
                    || runtime.justiceSystem == null
                    || !ReferenceEquals(runtime.justiceSystem.CoreOwner, JusticeRecordsOwner)
                    || targetRuntime == null || city == null || warrant == null || sentence == null
                    || !dispatchBoundary.TryGetExactActor(
                        guardRuntime.SelectedRuntimeOwners,
                        guardRuntime.SelectedRuntimeOwners?.ActionSlot?.CurrentAction,
                        out NpcRuntime exactGuard)
                    || !ReferenceEquals(exactGuard, guardRuntime)
                    || !dispatchBoundary.TryFindExactNpcRuntime(target, out NpcRuntime exactTarget)
                    || !ReferenceEquals(exactTarget, targetRuntime)
                    || !ReferenceEquals(exactTarget.SelectedRuntimeOwners, target)
                    || !graph.TryGetNpcOwner(target.RuntimeId, out DailyV1NpcRuntimeOwners exactTargetOwner)
                    || !ReferenceEquals(exactTargetOwner, target)
                    || !dispatchBoundary.TryGetExactCityProjection(
                        graph, city.RuntimeId, guardRuntime.CurrentCity,
                        out DailyV1CityRuntimeOwners exactCity, out _)
                    || !ReferenceEquals(exactCity, city)
                    || !graph.JusticeRecordsOwner.ContainsWantedRecord(warrant)
                    || !ReferenceEquals(sentence.Warrant, warrant))
                    return false;

                WantedRecordRuntime exactWarrant = runtime.justiceSystem.GetActiveWarrant(
                    exactTarget, guardRuntime.CurrentCity);
                return exactWarrant != null && ReferenceEquals(exactWarrant.CoreOwner, warrant)
                    && runtime.justiceSystem.TryPrepareSelectedArrestSentenceProjection(
                        exactTarget, guardRuntime.CurrentCity, exactWarrant, sentence);
            }

            public void DiscardPreparedSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
                runtime.justiceSystem?.DiscardPreparedSelectedArrestSentenceProjection(sentence);

            public bool CommitPreparedSentenceProjection(DailyV1JusticeSentenceOwner sentence) =>
                runtime.justiceSystem != null
                && ReferenceEquals(runtime.justiceSystem.CoreOwner, JusticeRecordsOwner)
                && runtime.justiceSystem.CommitPreparedSelectedArrestSentenceProjection(sentence);

            public bool CanProjectStatusChanges(
                DailyV1NpcRuntimeOwners target,
                IReadOnlyList<DailyV1StatusDefinition> statusToAdd)
            {
                return TryGetExactTarget(target, out NpcRuntime exactTarget)
                    && exactTarget.CanProjectCoreSelectedDailyV1StatusChanges(target, statusToAdd);
            }

            public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners target)
            {
                if (!TryGetExactTarget(target, out NpcRuntime exactTarget))
                    throw new InvalidOperationException("The selected Guard owner lost its exact runtime projection.");
                exactTarget.EnsureCoreSelectedDailyV1OwnerMutationAllowed(target);
            }

            public void NotifyStatusMutationCommitted(
                DailyV1NpcRuntimeOwners target,
                DailyV1StatusDefinition status,
                bool added)
            {
                if (!TryGetExactTarget(target, out NpcRuntime exactTarget))
                    throw new InvalidOperationException("The selected Guard status owner lost its exact runtime projection after mutation.");
                exactTarget.NotifyCoreSelectedDailyV1StatusMutationCommitted(target);
            }

            public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners target)
            {
                if (!TryGetExactTarget(target, out NpcRuntime exactTarget))
                    throw new InvalidOperationException("The selected Guard hidden-state owner lost its exact runtime projection after mutation.");
                exactTarget.NotifyCoreSelectedDailyV1HiddenMutationCommitted(target);
            }

            public void PublishArrest(
                DailyV1NpcRuntimeOwners guard,
                DailyV1NpcRuntimeOwners target,
                DailyV1CityRuntimeOwners city,
                string originDecisionId)
            {
                DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
                if (!TryGetExactTarget(guard, out NpcRuntime exactGuard)
                    || !ReferenceEquals(exactGuard, guardRuntime)
                    || !TryGetExactTarget(target, out NpcRuntime exactTarget)
                    || !ReferenceEquals(exactTarget, targetRuntime)
                    || graph == null || !ReferenceEquals(graph.JusticeRecordsOwner, JusticeRecordsOwner)
                    || !dispatchBoundary.TryGetExactCityProjection(
                        graph, city?.RuntimeId, exactGuard.CurrentCity,
                        out DailyV1CityRuntimeOwners exactCity, out _)
                    || !ReferenceEquals(exactCity, city)
                    || !ReferenceEquals(exactTarget.CurrentCity, exactGuard.CurrentCity)
                    || !string.Equals(originDecisionId,
                        guard.ActionSlot?.CurrentPayload?.OriginDecisionId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The selected Guard Arrest returned through a mismatched Unity projection.");

                runtime.justiceSystem.PublishSelectedArrest(
                    exactGuard, exactTarget, exactGuard.CurrentCity, originDecisionId);
            }

            private bool TryGetExactTarget(
                DailyV1NpcRuntimeOwners target,
                out NpcRuntime exactTarget)
            {
                exactTarget = null;
                DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
                return target != null
                    && ReferenceEquals(graph?.JusticeRecordsOwner, JusticeRecordsOwner)
                    && graph.ContainsExactNpcOwner(target)
                    && dispatchBoundary.TryFindExactNpcRuntime(target, out exactTarget)
                    && (ReferenceEquals(exactTarget, guardRuntime)
                        || ReferenceEquals(exactTarget, targetRuntime));
            }
        }

        private sealed class SelectedCrimeActionBoundaryAdapter
            : DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary
        {
            private readonly SelectedActionDispatchBoundaryAdapter dispatchBoundary;
            private readonly SimulationRuntime runtime;
            private readonly NpcRuntime actorRuntime;
            public DailyV1JusticeRecordsOwner JusticeRecordsOwner { get; }

            internal SelectedCrimeActionBoundaryAdapter(
                SelectedActionDispatchBoundaryAdapter dispatchBoundary,
                SimulationRuntime runtime,
                NpcRuntime actorRuntime,
                DailyV1JusticeRecordsOwner justiceRecordsOwner)
            {
                this.dispatchBoundary = dispatchBoundary
                    ?? throw new ArgumentNullException(nameof(dispatchBoundary));
                this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
                this.actorRuntime = actorRuntime ?? throw new ArgumentNullException(nameof(actorRuntime));
                JusticeRecordsOwner = justiceRecordsOwner
                    ?? throw new ArgumentNullException(nameof(justiceRecordsOwner));
            }

            public bool ExecuteWithinOperation(Func<bool> operation)
            {
                if (operation == null) return false;
                if (runtime.runtimeAdmissionContext == null) return operation();
                if (!runtime.TryBeginP12CrimeJusticeActionScope(
                        new[] { actorRuntime }, includeJustice: true,
                        out P12CrimeJusticeMutationScope scope))
                    throw new InvalidOperationException(
                        "P12 Crime/Justice selected Hide or EscapePrison could not be admitted.");
                using (scope) return operation();
            }

            public bool CanProjectStatusChanges(
                DailyV1NpcRuntimeOwners actor,
                IReadOnlyList<DailyV1StatusDefinition> statusToAdd)
            {
                return TryGetExactActor(actor, out NpcRuntime exactActor)
                    && exactActor.CanProjectCoreSelectedDailyV1StatusChanges(actor, statusToAdd);
            }

            public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners actor)
            {
                if (!TryGetExactActor(actor, out NpcRuntime exactActor))
                    throw new InvalidOperationException("The selected Crime owner lost its exact runtime projection.");
                exactActor.EnsureCoreSelectedDailyV1OwnerMutationAllowed(actor);
            }

            public void NotifyStatusMutationCommitted(
                DailyV1NpcRuntimeOwners actor,
                DailyV1StatusDefinition status,
                bool added)
            {
                if (!TryGetExactActor(actor, out NpcRuntime exactActor))
                    throw new InvalidOperationException("The selected Crime status owner lost its exact runtime projection after mutation.");
                exactActor.NotifyCoreSelectedDailyV1StatusMutationCommitted(actor);
            }

            public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners actor)
            {
                if (!TryGetExactActor(actor, out NpcRuntime exactActor))
                    throw new InvalidOperationException("The selected Crime hidden-state owner lost its exact runtime projection after mutation.");
                exactActor.NotifyCoreSelectedDailyV1HiddenMutationCommitted(actor);
            }

            public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence)
            {
                DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
                return graph != null
                    && ReferenceEquals(graph.JusticeRecordsOwner, JusticeRecordsOwner)
                    && runtime.justiceSystem != null
                    && ReferenceEquals(runtime.justiceSystem.CoreOwner, JusticeRecordsOwner)
                    && sentence != null
                    && string.Equals(sentence.TargetRuntimeId, actorRuntime.RuntimeId,
                        StringComparison.Ordinal)
                    && JusticeRecordsOwner.ContainsSentence(sentence)
                    && runtime.justiceSystem.CanRemoveSelectedSentenceProjection(sentence);
            }

            public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence)
            {
                if (runtime.justiceSystem == null
                    || !ReferenceEquals(runtime.justiceSystem.CoreOwner, JusticeRecordsOwner))
                    throw new InvalidOperationException(
                        "The selected Crime sentence lost its exact Justice projection owner after Core removal.");
                runtime.justiceSystem.CommitSelectedSentenceProjectionRemoval(sentence);
            }

            public void PublishHidden(DailyV1NpcRuntimeOwners actor)
            {
                if (!TryGetExactActor(actor, out NpcRuntime exactActor))
                    throw new InvalidOperationException("Selected Hide lost its exact NPC projection.");
                runtime.logger?.Log(SimulationLogCategory.Crime,
                    $"{exactActor.NpcName} esta escondido.");
            }

            public void PublishFailedEscape(DailyV1NpcRuntimeOwners actor, int sentencePenalty)
            {
                if (!TryGetExactActor(actor, out NpcRuntime exactActor))
                    throw new InvalidOperationException("Failed EscapePrison lost its exact NPC projection.");
                runtime.logger?.Log(SimulationLogCategory.Crime,
                    $"{exactActor.NpcName} tentou fugir da prisao e falhou. Pena aumentou em {sentencePenalty} dias. Vigilancia aumentou.");
            }

            public void PublishEscaped(
                DailyV1NpcRuntimeOwners actor,
                DailyV1CityRuntimeOwners eventCity,
                bool hadActiveSentence,
                float bountyAfterPenalty,
                string originDecisionId)
            {
                DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
                if (!TryGetExactActor(actor, out NpcRuntime exactActor)
                    || !string.Equals(originDecisionId,
                        actor.ActionSlot?.CurrentPayload?.OriginDecisionId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Selected EscapePrison returned through a mismatched Unity projection.");
                CityRuntime exactCity = ResolveCityProjection(eventCity);
                if (eventCity == null
                    ? (actor.Travel?.CurrentCityRuntimeId != null || exactActor.CurrentCity != null)
                    : !dispatchBoundary.TryGetExactCityProjection(
                        graph, eventCity.RuntimeId, exactCity, out _, out _))
                    throw new InvalidOperationException(
                        "Selected EscapePrison lost its exact event City projection.");
                runtime.justiceSystem.PublishSelectedEscape(
                    exactActor, exactCity, hadActiveSentence, bountyAfterPenalty, originDecisionId);
            }

            private CityRuntime ResolveCityProjection(DailyV1CityRuntimeOwners cityOwner)
            {
                if (cityOwner == null || runtime.cities == null) return null;
                CityRuntime match = null;
                foreach (CityRuntime candidate in runtime.cities)
                    if (ReferenceEquals(candidate?.SelectedRuntimeOwners, cityOwner))
                    {
                        if (match != null) return null;
                        match = candidate;
                    }
                return match;
            }

            private bool TryGetExactActor(
                DailyV1NpcRuntimeOwners actor,
                out NpcRuntime exactActor)
            {
                exactActor = null;
                DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
                return ReferenceEquals(graph?.JusticeRecordsOwner, JusticeRecordsOwner)
                    && graph.ContainsExactNpcOwner(actor)
                    && ReferenceEquals(actor, actorRuntime.SelectedRuntimeOwners)
                    && dispatchBoundary.TryFindExactNpcRuntime(actor, out exactActor)
                    && ReferenceEquals(exactActor, actorRuntime);
            }
        }

        private sealed class SelectedStealActionBoundaryAdapter
            : DailyV1SelectedStealActionExecutionStage.ISelectedStealActionBoundary
        {
            private readonly SelectedActionDispatchBoundaryAdapter dispatchBoundary;
            private readonly SimulationRuntime runtime;
            private readonly NpcRuntime perpetratorRuntime;
            private readonly NpcRuntime victimRuntime;
            private readonly DailyV1CityRuntimeOwners cityOwner;

            public CrimeSocialAppraisalWorldState SocialAppraisalWorldState { get; }
            public ITheftOutcomeSink TheftOutcomeSink => runtime.crimeSystem?.TheftOutcomeSink;
            public DailyV1JusticeRecordsOwner JusticeRecordsOwner { get; }

            internal SelectedStealActionBoundaryAdapter(
                SelectedActionDispatchBoundaryAdapter dispatchBoundary,
                SimulationRuntime runtime,
                NpcRuntime perpetratorRuntime,
                NpcRuntime victimRuntime,
                DailyV1CityRuntimeOwners cityOwner,
                DailyV1JusticeRecordsOwner justiceRecordsOwner,
                CrimeSocialAppraisalWorldState socialAppraisalWorldState)
            {
                this.dispatchBoundary = dispatchBoundary
                    ?? throw new ArgumentNullException(nameof(dispatchBoundary));
                this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
                this.perpetratorRuntime = perpetratorRuntime
                    ?? throw new ArgumentNullException(nameof(perpetratorRuntime));
                this.victimRuntime = victimRuntime
                    ?? throw new ArgumentNullException(nameof(victimRuntime));
                this.cityOwner = cityOwner ?? throw new ArgumentNullException(nameof(cityOwner));
                JusticeRecordsOwner = justiceRecordsOwner
                    ?? throw new ArgumentNullException(nameof(justiceRecordsOwner));
                SocialAppraisalWorldState = socialAppraisalWorldState
                    ?? throw new ArgumentNullException(nameof(socialAppraisalWorldState));
            }

            public bool ExecuteWithinOperation(Func<bool> operation)
            {
                if (operation == null) return false;
                if (runtime.runtimeAdmissionContext == null) return operation();
                if (!runtime.TryBeginP12CrimeJusticeActionScope(
                        new[] { perpetratorRuntime, victimRuntime }, includeJustice: true,
                        out P12CrimeJusticeMutationScope scope))
                    throw new InvalidOperationException(
                        "P12 Crime/Justice Steal action could not be admitted.");
                using (scope) return operation();
            }

            public EconomyTransactionResult ExecuteMoneyTransferWithinOperation(
                DailyV1NpcRuntimeOwners source,
                DailyV1NpcRuntimeOwners destination,
                Func<EconomyTransactionResult> operation)
            {
                if (operation == null
                    || !TryResolveParticipant(source, out NpcRuntime exactSource)
                    || !TryResolveParticipant(destination, out NpcRuntime exactDestination)
                    || ReferenceEquals(exactSource, exactDestination))
                    return EconomyTransactionResult.CreateFailure(
                        EconomyTransactionType.MoneyTransfer,
                        MoneyEffect.Transfer,
                        EconomyTransactionFailureReason.InvalidInput,
                        source?.RuntimeId,
                        destination?.RuntimeId);

                if (runtime.runtimeAdmissionContext == null) return operation();
                if (!runtime.TryBeginNpcMoneyTransferCensusOperation(
                        exactSource,
                        exactDestination,
                        out SimulationOperationScope scope,
                        out _,
                        out _))
                    return EconomyTransactionResult.CreateFailure(
                        EconomyTransactionType.MoneyTransfer,
                        MoneyEffect.Transfer,
                        EconomyTransactionFailureReason.TransactionCommitFailed,
                        source.RuntimeId,
                        destination.RuntimeId);

                using (scope) return operation();
            }

            public bool TryProjectWantedRecord(
                DailyV1NpcRuntimeOwners target,
                DailyV1CityRuntimeOwners city,
                DailyV1JusticeWantedRecordOwner record)
            {
                DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
                return ReferenceEquals(graph?.JusticeRecordsOwner, JusticeRecordsOwner)
                    && ReferenceEquals(runtime.justiceSystem?.CoreOwner, JusticeRecordsOwner)
                    && ReferenceEquals(target, perpetratorRuntime.SelectedRuntimeOwners)
                    && ReferenceEquals(city, cityOwner)
                    && TryResolveParticipant(target, out NpcRuntime exactTarget)
                    && ReferenceEquals(exactTarget, perpetratorRuntime)
                    && dispatchBoundary.TryGetExactCityProjection(
                        graph, city.RuntimeId, perpetratorRuntime.CurrentCity,
                        out DailyV1CityRuntimeOwners exactCity, out _)
                    && ReferenceEquals(exactCity, city)
                    && runtime.justiceSystem.TryProjectSelectedCoreWantedRecord(
                        record, exactTarget, perpetratorRuntime.CurrentCity);
            }

            public bool TryPrepareWantedRecordProjection(
                DailyV1NpcRuntimeOwners target,
                DailyV1CityRuntimeOwners city,
                DailyV1JusticeWantedRecordOwner record)
            {
                DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
                return ReferenceEquals(graph?.JusticeRecordsOwner, JusticeRecordsOwner)
                    && ReferenceEquals(runtime.justiceSystem?.CoreOwner, JusticeRecordsOwner)
                    && ReferenceEquals(target, perpetratorRuntime.SelectedRuntimeOwners)
                    && ReferenceEquals(city, cityOwner)
                    && TryResolveParticipant(target, out NpcRuntime exactTarget)
                    && ReferenceEquals(exactTarget, perpetratorRuntime)
                    && dispatchBoundary.TryGetExactCityProjection(
                        graph, city.RuntimeId, perpetratorRuntime.CurrentCity,
                        out DailyV1CityRuntimeOwners exactCity, out _)
                    && ReferenceEquals(exactCity, city)
                    && runtime.justiceSystem.TryPrepareSelectedCoreWantedRecordProjection(
                        record, exactTarget, perpetratorRuntime.CurrentCity);
            }

            public void DiscardPreparedWantedRecordProjection(
                DailyV1JusticeWantedRecordOwner record) =>
                runtime.justiceSystem?.DiscardPreparedSelectedCoreWantedRecordProjection(record);

            public bool CanProjectStatusChanges(
                DailyV1NpcRuntimeOwners target,
                IReadOnlyList<DailyV1StatusDefinition> statusToAdd)
            {
                if (!TryResolveParticipant(target, out NpcRuntime exactTarget)
                    || !ReferenceEquals(exactTarget, perpetratorRuntime)
                    || !exactTarget.CanProjectCoreSelectedDailyV1StatusChanges(
                        target, statusToAdd))
                    return false;
                return true;
            }

            public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners target)
            {
                if (!TryResolveParticipant(target, out NpcRuntime exactTarget)
                    || !ReferenceEquals(exactTarget, perpetratorRuntime))
                    throw new InvalidOperationException("The selected Steal owner lost its exact runtime projection.");
                exactTarget.EnsureCoreSelectedDailyV1OwnerMutationAllowed(target);
            }

            public void NotifyStatusMutationCommitted(
                DailyV1NpcRuntimeOwners target,
                DailyV1StatusDefinition status,
                bool added)
            {
                if (!TryResolveParticipant(target, out NpcRuntime exactTarget)
                    || !ReferenceEquals(exactTarget, perpetratorRuntime))
                    throw new InvalidOperationException("The selected Steal status owner lost its exact runtime projection after mutation.");
                exactTarget.NotifyCoreSelectedDailyV1StatusMutationCommitted(target);
            }

            public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners target)
            {
                if (!TryResolveParticipant(target, out NpcRuntime exactTarget)
                    || !ReferenceEquals(exactTarget, perpetratorRuntime))
                    throw new InvalidOperationException("The selected Steal hidden-state owner lost its exact runtime projection after mutation.");
                exactTarget.NotifyCoreSelectedDailyV1HiddenMutationCommitted(target);
            }

            public void PublishTheft(
                DailyV1NpcRuntimeOwners perpetrator,
                DailyV1NpcRuntimeOwners victim,
                int amount)
            {
                if (!TryResolveParticipant(perpetrator, out NpcRuntime exactPerpetrator)
                    || !ReferenceEquals(exactPerpetrator, perpetratorRuntime)
                    || !TryResolveParticipant(victim, out NpcRuntime exactVictim)
                    || !ReferenceEquals(exactVictim, victimRuntime))
                    throw new InvalidOperationException(
                        "The selected Steal log returned through mismatched runtime projections.");
                runtime.logger?.Log(SimulationLogCategory.Crime,
                    $"{exactPerpetrator.NpcName} roubou {exactVictim.NpcName} e levou {amount} moedas.");
            }

            public void LogCompensationFailure() => runtime.logger?.LogWarning(
                "O outcome social do roubo falhou e a reversao monetaria tambem falhou.");

            public void PublishWantedRecord(
                DailyV1NpcRuntimeOwners target,
                DailyV1CityRuntimeOwners city,
                DailyV1JusticeWantedRecordOwner record)
            {
                if (!TryResolveParticipant(target, out NpcRuntime exactTarget)
                    || !ReferenceEquals(exactTarget, perpetratorRuntime)
                    || !ReferenceEquals(city, cityOwner)
                    || record == null
                    || !JusticeRecordsOwner.ContainsWantedRecord(record))
                    throw new InvalidOperationException(
                        "The selected Steal warrant log returned through mismatched owner projections.");
                runtime.logger?.Log(SimulationLogCategory.Justice,
                    $"{exactTarget.NpcName} agora possui mandado em {perpetratorRuntime.CurrentCity.CityName}. Recompensa: {record.Bounty:0.##}. Pena: {record.SentenceDays} dias.");
            }

            private bool TryResolveParticipant(
                DailyV1NpcRuntimeOwners owner,
                out NpcRuntime exactRuntime)
            {
                exactRuntime = null;
                DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
                return owner != null
                    && ReferenceEquals(graph?.JusticeRecordsOwner, JusticeRecordsOwner)
                    && graph.ContainsExactNpcOwner(owner)
                    && dispatchBoundary.TryFindExactNpcRuntime(owner, out exactRuntime)
                    && (ReferenceEquals(exactRuntime, perpetratorRuntime)
                        || ReferenceEquals(exactRuntime, victimRuntime));
            }
        }
    }

    private sealed class P12MerchantOperationScope : IDisposable
    {
        private readonly SimulationRuntime owner;
        private readonly P12MerchantOperationContext context;
        private bool disposed;

        public P12MerchantOperationScope(SimulationRuntime owner, P12MerchantOperationContext context)
        {
            this.owner = owner;
            this.context = context;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            owner?.ExitP12MerchantOperation(context);
        }
    }

    // Unity forwards legacy callers to the same guard owned by Core's runtime
    // execution boundary; this is not a second mutation authority.
    private AuthoritativeMutationGuard mutationGuard => dailyExecutionBoundary.MutationGuard;
    private bool coreMutationGuardAuthoritiesBound;
    private readonly DailyV1ExecutionBoundaryState dailyExecutionBoundary;
    private readonly object dailyV1ApplicationAdvanceAuthorityGate = new object();
    private DailyV1Application dailyV1ApplicationAdvanceAuthority;
    private DailyV1ActiveRuntimeSession dailyV1ApplicationAdvanceSession;
    private Func<DailyV1SelectedRuntimeOwnerGraph> selectedRuntimeOwnerGraphProvider;
    private Func<string, NpcRuntime> selectedNpcPresenceProjectionResolver;
    private bool advanceLeaseHeld => dailyExecutionBoundary.AdvanceLeaseHeld;
    private long completedDailyCoreSequence => dailyExecutionBoundary.CompletedCoreSequence;
    private DailyCaptureEligibilityToken currentDailyCaptureToken => dailyExecutionBoundary.CurrentCaptureToken;
    private SimulationRuntimeAdmissionContext runtimeAdmissionContext => dailyExecutionBoundary.AdmissionContext;
    private SimulationRuntimeCompositionProfile compositionProfile => dailyExecutionBoundary.CompositionProfile;
    private string npcRosterCensusProtocolInitializationFailure;
    private readonly int p16AOwnerThreadId;
    private readonly P17AScenarioAuthorityCapability p17AScenarioAuthorityCapability;
    private readonly long initialAbsoluteDay;
    private bool p15AInitialPublicationComplete;
    private readonly SimulationRecordSequence simulationRecordSequence;
    private readonly SimulationRecordSequenceCensusProvider simulationRecordSequenceCensusProvider;
    private readonly RuntimeIdAllocator runtimeIdAllocator;
    private readonly IOwnerSectionCensusProvider runtimeIdAllocatorEventCounterCensusProvider;
    private readonly IOwnerSectionCensusProvider runtimeIdAllocatorDecisionCounterCensusProvider;
    private ScheduledDirectiveCensusProvider scheduledDirectiveCensusProvider;
    private ActorChoiceP11CensusProvider actorChoiceP11CensusProvider;
    private readonly FactualReadCoordinator factualReadCoordinator;
    private bool factualReadWorldPublished => dailyExecutionBoundary.IsFactualReadWorldPublished;
    private SimulationTime simulationTime => dailyExecutionBoundary.SimulationTime;
    private readonly List<CityRuntime> cities;
    private readonly IReadOnlyList<CityRuntime> cityRuntimeSnapshot;
    private readonly List<NpcRuntime> npcRuntimes;
    private readonly IReadOnlyList<NpcRuntime> npcRuntimeSnapshot;
    private readonly Dictionary<string, NpcRuntime> npcRegistryById;
    private EffectiveSimulationConfiguration configuration => dailyExecutionBoundary.ConfigurationIdentity;
    private SimulationCalendar calendar => dailyExecutionBoundary.CalendarIdentity;
    private readonly IPersonNaturalMortalitySampleProvider naturalMortalitySamples;
    private readonly IAggregateDemographyProvider aggregateDemographyProvider;
    private DailyDemographyReport lastDailyDemographyReport;
    private readonly PersonStore personStore;
    private readonly IReadOnlyList<IOwnerSectionCensusProvider> personStoreCensusProviders;
    private ContinuationCensusProtocol npcRosterCensusProtocol;
    private readonly Dictionary<MarketRuntime, string> marketSectionIdsByOwner =
        new Dictionary<MarketRuntime, string>();
    private readonly Dictionary<MoneyAccountRuntime, P12NpcOwnerMutationBinding> npcMoneyAccountMutationBindings =
        new Dictionary<MoneyAccountRuntime, P12NpcOwnerMutationBinding>();
    private readonly Dictionary<InventoryRuntime, P12NpcOwnerMutationBinding> npcInventoryMutationBindings =
        new Dictionary<InventoryRuntime, P12NpcOwnerMutationBinding>();
    private readonly Dictionary<SpatialKnowledgeRuntime, P12NpcOwnerMutationBinding> npcSpatialKnowledgeMutationBindings =
        new Dictionary<SpatialKnowledgeRuntime, P12NpcOwnerMutationBinding>();
    private readonly Dictionary<CommercialKnowledgeRuntime, P12NpcOwnerMutationBinding> npcCommercialKnowledgeMutationBindings =
        new Dictionary<CommercialKnowledgeRuntime, P12NpcOwnerMutationBinding>();
    private readonly Dictionary<object, P12NpcOwnerMutationBinding> npcMerchantPlanMutationBindings =
        new Dictionary<object, P12NpcOwnerMutationBinding>();
    private readonly Dictionary<NpcTravelPlanRuntime, P12NpcOwnerMutationBinding> npcTravelPlanMutationBindings =
        new Dictionary<NpcTravelPlanRuntime, P12NpcOwnerMutationBinding>();
    private readonly Dictionary<NpcRuntime, P12NpcOwnerMutationBinding> npcTravelStateMutationBindings =
        new Dictionary<NpcRuntime, P12NpcOwnerMutationBinding>();
    private readonly Dictionary<CityRuntime, string> cityNpcPresenceSectionIdsByOwner =
        new Dictionary<CityRuntime, string>();
    private IReadOnlyList<IOwnerSectionCensusProvider> cityNpcPresenceCensusProviders =
        Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    private TravelPartyCensusProvider travelPartyCensusProvider;
    private P12NpcOwnerMutationBinding travelPartyStoreMutationBinding;
    private volatile NpcMembershipCensusContext activeNpcMembershipCensusContext;
    private volatile P12MerchantOperationContext activeP12MerchantOperationContext;
    private volatile P12TravelPartyAdvanceOperationContext activeP12TravelPartyAdvanceOperationContext;
    private volatile P12SoloTravelStartOperationContext activeP12SoloTravelStartOperationContext;
    private readonly ActorChoiceStore actorChoiceStore;
    private readonly SpatialAuthorityStore spatialAuthorityStore;
    private readonly StructureStore structureStore;
    private readonly LegacySpatialAnchorBindingStore legacySpatialAnchorBindingStore;
    private readonly PersonSpatialPositionStore personSpatialPositionStore;
    private readonly SpatialRouteKnowledgeStore spatialRouteKnowledgeStore;
    private readonly PersonRoutePlanStore personRoutePlanStore;
    private readonly P8ETravelTransactionCoordinator p8eTravelTransactionCoordinator;
    private readonly SpatialRoutePlanningSystem spatialRoutePlanningSystem;
    private readonly ArmedForceStore armedForceStore;
    private readonly ContingentManpowerStateStore contingentManpowerStateStore;
    private readonly SettlementManpowerSourceRegistry settlementManpowerSourceRegistry;
    private readonly ManpowerSourceConsequencePlanningService manpowerSourceConsequencePlanningService;
    private readonly BattleDirectConsequencePolicy battleDirectConsequencePolicy;
    private readonly BattleDirectConsequencePlanningService battleDirectConsequencePlanningService;
    private readonly BattleOutcomeApplicationService battleOutcomeApplicationService;
    private readonly ArmedForceSpatialStateStore armedForceSpatialStateStore;
    private readonly LocalTopologyStore localTopologyStore;
    private readonly PersistentConflictStore conflictStore;
    private readonly PersistentWarStore warStore;
    private readonly PersistentBattleStore battleStore;
    private readonly BattleExecutionContextBuilder battleExecutionContextBuilder;
    private readonly BattleResolutionPolicy battleResolutionPolicy;
    private readonly BattleOutcomePlanningService battleOutcomePlanningService;
    private readonly GenealogyStore genealogyStore;
    private readonly InstitutionStore institutionStore;
    private readonly OfficeStore officeStore;
    private readonly PropertyOwnershipStore propertyOwnershipStore;
    private readonly EstateStore estateStore;
    private readonly PoliticalClaimStore politicalClaimStore;
    private readonly FactionStore factionStore;
    private readonly PoliticalSupportStore politicalSupportStore;
    private readonly PoliticalKnowledgeStore politicalKnowledgeStore;
    private readonly PoliticalDecisionStore politicalDecisionStore;
    private long politicalWorldRevision;
    private long lastPoliticalTruthFingerprint;
    private bool hasPoliticalTruthFingerprint;
    private bool isComposingNpcRoster;
    private readonly List<NpcActionData> configuredActions;
    private readonly ScheduledDirectiveSystem scheduledDirectiveSystem;
    private readonly JusticeSystem justiceSystem;
    private readonly CrimeSystem crimeSystem;
    private readonly CrimeSocialAppraisalWorldState crimeSocialAppraisalWorldState;
    private readonly NpcDecisionSystem npcDecisionSystem;
    private readonly TravelSystem travelSystem;
    private readonly TravelPartySystem travelPartySystem;
    private readonly MerchantSystem merchantSystem;
    private readonly CommercialKnowledgeSharingSystem commercialKnowledgeSharingSystem;
    private readonly IAuthoritativeRandomSource randomSource;
    private readonly ExplorableSiteStore explorableSiteStore;
    private readonly ExplorableSiteKnowledgeSystem explorableSiteKnowledgeSystem;
    private readonly ExpeditionSystem expeditionSystem;
    private readonly PlaceContentStore placeContentStore;
    private readonly NpcDecisionRecorder decisionRecorder;
    private readonly EconomyTransactionService economyTransactionService;
    private readonly bool requireP12ReceiptCensusOwners;
    private readonly AdventureExpeditionAutonomySystem adventureExpeditionAutonomySystem;
    private readonly SimulationLogger logger;

    public SimulationTime SimulationTime => simulationTime;
    internal CrimeSystem CrimeSystemForWorldBoundary => crimeSystem;
    internal JusticeSystem JusticeSystemForWorldBoundary => justiceSystem;
    public AuthoritativeMutationHealth MutationHealth => mutationGuard.Health;
    public bool IsMutationFaulted => mutationGuard.Health == AuthoritativeMutationHealth.Faulted;
    public AuthoritativeMutationFaultReason MutationFaultReason => mutationGuard.FaultReason;
    internal FactualReadCoordinator FactualReads => factualReadCoordinator;
    public long CurrentDay => p18dTimeline != null
        ? p18dTimeline.CurrentInstant.AbsoluteDay
        : simulationTime.AbsoluteDay;
    public IReadOnlyList<CityRuntime> Cities => cityRuntimeSnapshot;
    internal DailyV1SelectedRuntimeOwnerGraph SelectedRuntimeOwnerGraph => dailyExecutionBoundary.SelectedRuntimeOwnerGraph;
    internal DailyV1ExecutionBoundaryState ExecutionBoundaryState => dailyExecutionBoundary;
    public EffectiveSimulationConfiguration Configuration => configuration;
    public SimulationCalendar Calendar => calendar;
    public DailyDemographyReport LastDailyDemographyReport => lastDailyDemographyReport;
    public PersonStore PersonStore => personStore;
    /// <summary>Latest reconciled passive per-NPC SpatialKnowledge witness snapshot.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> SpatialKnowledgeCensusProviders =>
        npcRosterCensusProtocol != null
            ? npcRosterCensusProtocol.SpatialKnowledgeFamilyProviders
            : Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    public IReadOnlyList<IOwnerSectionCensusProvider> InventoryCensusProviders =>
        npcRosterCensusProtocol != null ? npcRosterCensusProtocol.InventoryFamilyProviders : Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    public IReadOnlyList<IOwnerSectionCensusProvider> MoneyAccountCensusProviders =>
        npcRosterCensusProtocol != null ? npcRosterCensusProtocol.MoneyAccountFamilyProviders : Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    /// <summary>Latest reconciled exact-zero witnesses for excluded P18 NPC receipt owners.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> NpcReceiptOwnerCensusProviders =>
        npcRosterCensusProtocol != null
            ? npcRosterCensusProtocol.NpcReceiptOwnerFamilyProviders
            : Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    /// <summary>Latest reconciled passive per-NPC Knowledge witness snapshot.</summary>
    public IReadOnlyList<IOwnerSectionCensusProvider> NpcKnowledgeCensusProviders =>
        npcRosterCensusProtocol != null
            ? npcRosterCensusProtocol.NpcKnowledgeFamilyProviders
            : Array.AsReadOnly(new IOwnerSectionCensusProvider[0]);
    public ActorChoiceStore ActorChoiceStore => actorChoiceStore;
    public SpatialAuthorityStore SpatialAuthorityStore => spatialAuthorityStore;
    /// <summary>Present only in the explicit P15-A proving composition; excluded from P12 daily.</summary>
    public StructureStore StructureStore => structureStore;
    public LegacySpatialAnchorBindingStore LegacySpatialAnchorBindingStore => legacySpatialAnchorBindingStore;
    public PersonSpatialPositionStore PersonSpatialPositionStore => personSpatialPositionStore;
    public SpatialRouteKnowledgeStore SpatialRouteKnowledgeStore => spatialRouteKnowledgeStore;
    public PersonRoutePlanStore PersonRoutePlanStore => personRoutePlanStore;
    public P8ETravelTransactionCoordinator P8ETravelTransactionCoordinator => p8eTravelTransactionCoordinator;
    public SpatialRoutePlanningSystem SpatialRoutePlanningSystem => spatialRoutePlanningSystem;
    public ArmedForceStore ArmedForceStore => armedForceStore;
    public ContingentManpowerStateStore ContingentManpowerStateStore => contingentManpowerStateStore;

    /// <summary>Validates the composed spatial authority and its dependent factual child stores.</summary>
    public SimulationRuntimeSpatialInvariantReport ValidateSpatialInvariants()
    {
        List<string> violations = new List<string>();
        foreach (string violation in spatialAuthorityStore.ValidateInvariants().Violations)
            violations.Add("SpatialAuthority: " + violation);
        foreach (string violation in legacySpatialAnchorBindingStore.ValidateInvariants().Violations)
            violations.Add("LegacySpatialAnchorBindings: " + violation);
        foreach (string violation in personSpatialPositionStore.ValidateInvariants().Violations)
            violations.Add("PersonSpatialPositions: " + violation);
        if (structureStore != null)
        {
            foreach (string violation in structureStore.ValidateInvariants(CurrentDay).Violations)
                violations.Add("Structures: " + violation);
        }
        if (armedForceSpatialStateStore != null)
        {
            foreach (string violation in armedForceSpatialStateStore.ValidateInvariants().Violations)
                violations.Add("ArmedForceSpatialPositions: " + violation);
        }
        foreach (string violation in spatialRouteKnowledgeStore.ValidateInvariants().Violations)
            violations.Add("SpatialRouteKnowledge: " + violation);
        foreach (PersonId actor in spatialRouteKnowledgeStore.Actors)
        {
            foreach (SpatialObservation observation in spatialRouteKnowledgeStore.GetObservations(actor))
            {
                if (observation == null) continue;
                if (observation.ObservedDay > CurrentDay)
                    violations.Add("SpatialRouteKnowledge: Observation " + observation.StableIdentity
                        + " was observed after current SimulationTime.AbsoluteDay.");
                if (observation.ReceivedDay > CurrentDay)
                    violations.Add("SpatialRouteKnowledge: Observation " + observation.StableIdentity
                        + " was received after current SimulationTime.AbsoluteDay.");
            }
        }
        foreach (string violation in personRoutePlanStore.ValidateInvariants().Violations)
            violations.Add("PersonRoutePlans: " + violation);
        foreach (PersonRoutePlan plan in personRoutePlanStore.History)
        {
            if (plan != null && plan.AcceptedDay > CurrentDay)
                violations.Add("PersonRoutePlans: Plan " + plan.StableKey
                    + " was accepted after current SimulationTime.AbsoluteDay.");
        }
        return new SimulationRuntimeSpatialInvariantReport(violations);
    }

    /// <summary>Records actor-provided spatial observations against the current runtime day.</summary>
    public bool TryRecordSpatialObservations(
        PersonId actor,
        IEnumerable<SpatialObservation> observations,
        out SpatialKnowledgeFailure failure)
    {
        return spatialRouteKnowledgeStore.TryRecordObservations(actor, observations, CurrentDay, out failure);
    }

    /// <summary>Builds a selection using the current runtime day and explicit endpoints/policy.</summary>
    public SpatialRoutePlanningOutcome SelectKnownSpatialRoute(
        PersonId actor,
        StablePositionReference origin,
        StablePositionReference destination,
        SpatialRouteSelectionPolicy policy,
        long? maximumOptionBeliefAgeDays = null)
    {
        SpatialRoutePlanningRequest request = new SpatialRoutePlanningRequest(
            actor, origin, destination, CurrentDay, maximumOptionBeliefAgeDays);
        return spatialRoutePlanningSystem.SelectKnownRoute(request, policy);
    }

    /// <summary>Accepts a selected route only for the current runtime day.</summary>
    public bool TryAcceptSpatialRoutePlan(
        SpatialRoutePlanningOutcome selectedOutcome,
        string decisionIdentity,
        long expectedActorPlanRevision,
        out PersonRoutePlanFailure failure)
    {
        return personRoutePlanStore.TryAcceptPlan(
            selectedOutcome,
            decisionIdentity,
            expectedActorPlanRevision,
            CurrentDay,
            out failure);
    }

    public ManpowerSourceConsequencePlanningService ManpowerSourceConsequencePlanningService
        => manpowerSourceConsequencePlanningService;
    public BattleDirectConsequencePolicy BattleDirectConsequencePolicy => battleDirectConsequencePolicy;
    public BattleDirectConsequencePlanningService BattleDirectConsequencePlanningService
        => battleDirectConsequencePlanningService;
    public BattleOutcomeApplicationService BattleOutcomeApplicationService
        => battleOutcomeApplicationService;
    public ArmedForceSpatialStateStore ArmedForceSpatialStateStore => armedForceSpatialStateStore;
    public LocalTopologyStore LocalTopologyStore => localTopologyStore;
    public PersistentConflictStore ConflictStore => conflictStore;
    public PersistentWarStore WarStore => warStore;
    public PersistentBattleStore BattleStore => battleStore;
    public BattleExecutionContextBuilder BattleExecutionContextBuilder => battleExecutionContextBuilder;
    public BattleResolutionPolicy BattleResolutionPolicy => battleResolutionPolicy;
    public BattleOutcomePlanningService BattleOutcomePlanningService => battleOutcomePlanningService;
    public IReadOnlyList<ParentageRecord> GenealogyRecords => genealogyStore.Records;
    public IReadOnlyList<InstitutionRecord> InstitutionRecords => institutionStore.Institutions;
    public IReadOnlyList<OfficeRecord> OfficeRecords => officeStore.Offices;
    public IReadOnlyList<OfficeIncumbency> OfficeIncumbencies => officeStore.Incumbencies;
    public IReadOnlyList<OfficeTenureRecord> OfficeTenureHistory => officeStore.TenureHistory;
    public PropertyOwnershipStore PropertyOwnershipStore => propertyOwnershipStore;
    public EstateStore EstateStore => estateStore;
    public IReadOnlyList<PropertyOwnershipRecord> PropertyOwnershipRecords => propertyOwnershipStore.Records;
    public IReadOnlyList<EstateRecord> EstateRecords => estateStore.Records;
    public IReadOnlyList<PoliticalClaimRecord> PoliticalClaimRecords => politicalClaimStore.Records;
    public IReadOnlyList<PoliticalClaimRecognitionRecord> PoliticalClaimRecognitionRecords => politicalClaimStore.RecognitionRecords;
    public IReadOnlyList<FactionRecord> FactionRecords => factionStore.Factions;
    public IReadOnlyList<FactionAffiliationRecord> FactionAffiliationRecords => factionStore.Affiliations;
    public IReadOnlyList<PoliticalSupportRelationRecord> PoliticalSupportRecords => politicalSupportStore.Records;
    public int PoliticalKnowledgeHolderCount => politicalKnowledgeStore.Count;
    public long PoliticalKnowledgeRevision => politicalKnowledgeStore.Revision;
    public IReadOnlyList<PoliticalKnowledgeRuntime> PoliticalKnowledgeRuntimes => politicalKnowledgeStore.Runtimes;
    internal PoliticalKnowledgeStore PoliticalKnowledgeStoreForWorldBoundary => politicalKnowledgeStore;
    public long PoliticalWorldRevision
    {
        get
        {
            if (mutationGuard.CanMutate)
            {
                RefreshPoliticalWorldRevisionFromExposedStores();
            }

            return politicalWorldRevision;
        }
    }
    public IReadOnlyList<PoliticalDecisionRecord> PoliticalDecisionRecords => politicalDecisionStore.Records;
    /// <summary>
    /// Read-only view of every named NPC registered with this world. Registration is
    /// explicit; death and emigration do not remove an NPC from this world roster.
    /// </summary>
    public IReadOnlyList<NpcRuntime> NpcRuntimes => npcRuntimeSnapshot;
    public PlaceContentStore PlaceContentStore => placeContentStore;
    public CrimeSocialAppraisalWorldState CrimeSocialAppraisal => crimeSocialAppraisalWorldState;
    /// <summary>The causal world identity retained by a published composition, when supplied.</summary>
    public WorldId WorldId => dailyExecutionBoundary.WorldId;

    public SimulationRuntime(

        SimulationTime simulationTime,
        IEnumerable<CityRuntime> cities,
        IEnumerable<NpcRuntime> npcRuntimes,
        bool? economyEnabled = null,
        IReadOnlyList<NpcActionData> configuredActions = null,
        ScheduledDirectiveSystem scheduledDirectiveSystem = null,
        JusticeSystem justiceSystem = null,
        CrimeSystem crimeSystem = null,
        NpcDecisionSystem npcDecisionSystem = null,
        TravelSystem travelSystem = null,
        TravelPartySystem travelPartySystem = null,
        MerchantSystem merchantSystem = null,
        CommercialKnowledgeSharingSystem commercialKnowledgeSharingSystem = null,
        NpcDecisionRecorder decisionRecorder = null,
        SimulationLogger logger = null,
        bool? guardCrimeEnabled = null,
        ExplorableSiteStore explorableSiteStore = null,
        ExplorableSiteKnowledgeSystem explorableSiteKnowledgeSystem = null,
        ExpeditionSystem expeditionSystem = null,
        PlaceContentStore placeContentStore = null,
        AdventureExpeditionAutonomySystem adventureExpeditionAutonomySystem = null,
        EffectiveSimulationConfiguration configuration = null,
        IAuthoritativeRandomSource randomSource = null,
        PersonStore personStore = null,
        GenealogyStore genealogyStore = null,
        InstitutionStore institutionStore = null,
        OfficeStore officeStore = null,
        CalendarDefinition calendarDefinition = null,
        IPersonNaturalMortalitySampleProvider naturalMortalitySamples = null,
        IAggregateDemographyProvider aggregateDemographyProvider = null,
        PropertyOwnershipStore propertyOwnershipStore = null,
        EstateStore estateStore = null,
        PoliticalClaimStore politicalClaimStore = null,
        FactionStore factionStore = null,
        PoliticalSupportStore politicalSupportStore = null,
        PoliticalKnowledgeStore politicalKnowledgeStore = null,
        PoliticalDecisionStore politicalDecisionStore = null,
        long? politicalWorldRevision = null,
        ArmedForceStore armedForceStore = null,
        PersistentConflictStore conflictStore = null,
        PersistentWarStore warStore = null,
        PersistentBattleStore battleStore = null,
        SpatialAuthorityStore spatialAuthorityStore = null,
        ArmedForceSpatialStateStore armedForceSpatialStateStore = null,
        LocalTopologyStore localTopologyStore = null,
        BattleResolutionPolicy battleResolutionPolicy = null,
        ContingentManpowerStateStore contingentManpowerStateStore = null,
        IManpowerSourceSnapshotProvider manpowerSourceProvider = null,
        IEnumerable<SettlementManpowerSourceRegistration> settlementManpowerSourceRegistrations = null,
        BattleDirectConsequencePolicy battleDirectConsequencePolicy = null,
        IDomainEventRecorder battleResolvedEventRecorder = null,
        LegacySpatialAnchorBindingStore legacySpatialAnchorBindingStore = null,
        PersonSpatialPositionStore personSpatialPositionStore = null,
        SpatialRouteKnowledgeStore spatialRouteKnowledgeStore = null,
        PersonRoutePlanStore personRoutePlanStore = null,
        ActorChoiceStore actorChoiceStore = null,
        P18DIntradayProfile p18dIntradayProfile = null,
        SimulationRuntimeAdmissionContext runtimeAdmissionContext = null,
        SimulationRecordSequence recordSequence = null,
        WorldId worldId = null,
        RuntimeIdAllocator runtimeIdAllocator = null,
        SimulationRuntimeCompositionProfile compositionProfile = SimulationRuntimeCompositionProfile.Standard,
        StructureStore structureStore = null,
        P17AScenarioAuthorityCapability p17AScenarioAuthorityCapability = null,
        EconomyTransactionService economyTransactionService = null,
        bool requireP12ReceiptCensusOwners = false,
        RuntimeIdentityRegistry runtimeIdentityRegistry = null,
        SpatialNetworkRuntime spatialNetworkRuntime = null,
        bool requireP12RuntimeIdentitySpatialCensusOwners = false,
        CrimeSocialAppraisalWorldState p12CrimeSocialAppraisalWorldState = null
    )
        : this(
            null,
            null,
            simulationTime,
            cities,
            npcRuntimes,
            economyEnabled,
            configuredActions,
            scheduledDirectiveSystem,
            justiceSystem,
            crimeSystem,
            npcDecisionSystem,
            travelSystem,
            travelPartySystem,
            merchantSystem,
            commercialKnowledgeSharingSystem,
            decisionRecorder,
            logger,
            guardCrimeEnabled,
            explorableSiteStore,
            explorableSiteKnowledgeSystem,
            expeditionSystem,
            placeContentStore,
            adventureExpeditionAutonomySystem,
            configuration,
            randomSource,
            personStore,
            genealogyStore,
            institutionStore,
            officeStore,
            calendarDefinition,
            naturalMortalitySamples,
            aggregateDemographyProvider,
            propertyOwnershipStore,
            estateStore,
            politicalClaimStore,
            factionStore,
            politicalSupportStore,
            politicalKnowledgeStore,
            politicalDecisionStore,
            politicalWorldRevision,
            armedForceStore,
            conflictStore,
            warStore,
            battleStore,
            spatialAuthorityStore,
            armedForceSpatialStateStore,
            localTopologyStore,
            battleResolutionPolicy,
            contingentManpowerStateStore,
            manpowerSourceProvider,
            settlementManpowerSourceRegistrations,
            battleDirectConsequencePolicy,
            battleResolvedEventRecorder,
            legacySpatialAnchorBindingStore,
            personSpatialPositionStore,
            spatialRouteKnowledgeStore,
            personRoutePlanStore,
            actorChoiceStore,
            p18dIntradayProfile,
            runtimeAdmissionContext,
            recordSequence,
            worldId,
            runtimeIdAllocator,
            compositionProfile,
            structureStore,
            p17AScenarioAuthorityCapability,
            economyTransactionService,
            requireP12ReceiptCensusOwners,
            runtimeIdentityRegistry,
            spatialNetworkRuntime,
            requireP12RuntimeIdentitySpatialCensusOwners,
            p12CrimeSocialAppraisalWorldState)
    {
    }

    internal SimulationRuntime(
        DailyV1ExecutionBoundaryState selectedExecutionBoundaryState,
        SimulationCalendar selectedCalendar,
        SimulationTime simulationTime,
        IEnumerable<CityRuntime> cities,
        IEnumerable<NpcRuntime> npcRuntimes,
        bool? economyEnabled = null,
        IReadOnlyList<NpcActionData> configuredActions = null,
        ScheduledDirectiveSystem scheduledDirectiveSystem = null,
        JusticeSystem justiceSystem = null,
        CrimeSystem crimeSystem = null,
        NpcDecisionSystem npcDecisionSystem = null,
        TravelSystem travelSystem = null,
        TravelPartySystem travelPartySystem = null,
        MerchantSystem merchantSystem = null,
        CommercialKnowledgeSharingSystem commercialKnowledgeSharingSystem = null,
        NpcDecisionRecorder decisionRecorder = null,
        SimulationLogger logger = null,
        bool? guardCrimeEnabled = null,
        ExplorableSiteStore explorableSiteStore = null,
        ExplorableSiteKnowledgeSystem explorableSiteKnowledgeSystem = null,
        ExpeditionSystem expeditionSystem = null,
        PlaceContentStore placeContentStore = null,
        AdventureExpeditionAutonomySystem adventureExpeditionAutonomySystem = null,
        EffectiveSimulationConfiguration configuration = null,
        IAuthoritativeRandomSource randomSource = null,
        PersonStore personStore = null,
        GenealogyStore genealogyStore = null,
        InstitutionStore institutionStore = null,
        OfficeStore officeStore = null,
        CalendarDefinition calendarDefinition = null,
        IPersonNaturalMortalitySampleProvider naturalMortalitySamples = null,
        IAggregateDemographyProvider aggregateDemographyProvider = null,
        PropertyOwnershipStore propertyOwnershipStore = null,
        EstateStore estateStore = null,
        PoliticalClaimStore politicalClaimStore = null,
        FactionStore factionStore = null,
        PoliticalSupportStore politicalSupportStore = null,
        PoliticalKnowledgeStore politicalKnowledgeStore = null,
        PoliticalDecisionStore politicalDecisionStore = null,
        long? politicalWorldRevision = null,
        ArmedForceStore armedForceStore = null,
        PersistentConflictStore conflictStore = null,
        PersistentWarStore warStore = null,
        PersistentBattleStore battleStore = null,
        SpatialAuthorityStore spatialAuthorityStore = null,
        ArmedForceSpatialStateStore armedForceSpatialStateStore = null,
        LocalTopologyStore localTopologyStore = null,
        BattleResolutionPolicy battleResolutionPolicy = null,
        ContingentManpowerStateStore contingentManpowerStateStore = null,
        IManpowerSourceSnapshotProvider manpowerSourceProvider = null,
        IEnumerable<SettlementManpowerSourceRegistration> settlementManpowerSourceRegistrations = null,
        BattleDirectConsequencePolicy battleDirectConsequencePolicy = null,
        IDomainEventRecorder battleResolvedEventRecorder = null,
        LegacySpatialAnchorBindingStore legacySpatialAnchorBindingStore = null,
        PersonSpatialPositionStore personSpatialPositionStore = null,
        SpatialRouteKnowledgeStore spatialRouteKnowledgeStore = null,
        PersonRoutePlanStore personRoutePlanStore = null,
        ActorChoiceStore actorChoiceStore = null,
        P18DIntradayProfile p18dIntradayProfile = null,
        SimulationRuntimeAdmissionContext runtimeAdmissionContext = null,
        SimulationRecordSequence recordSequence = null,
        WorldId worldId = null,
        RuntimeIdAllocator runtimeIdAllocator = null,
        SimulationRuntimeCompositionProfile compositionProfile = SimulationRuntimeCompositionProfile.Standard,
        StructureStore structureStore = null,
        P17AScenarioAuthorityCapability p17AScenarioAuthorityCapability = null,
        EconomyTransactionService economyTransactionService = null,
        bool requireP12ReceiptCensusOwners = false,
        RuntimeIdentityRegistry runtimeIdentityRegistry = null,
        SpatialNetworkRuntime spatialNetworkRuntime = null,
        bool requireP12RuntimeIdentitySpatialCensusOwners = false,
        CrimeSocialAppraisalWorldState p12CrimeSocialAppraisalWorldState = null)
    {
        if (!Enum.IsDefined(typeof(SimulationRuntimeCompositionProfile), compositionProfile))
            throw new ArgumentOutOfRangeException(nameof(compositionProfile));
        if (selectedExecutionBoundaryState != null
            && (!ReferenceEquals(selectedExecutionBoundaryState.SimulationTime, simulationTime)
                || !ReferenceEquals(selectedExecutionBoundaryState.AdmissionContext, runtimeAdmissionContext)
                || selectedExecutionBoundaryState.CompositionProfile != compositionProfile
                || !ReferenceEquals(selectedExecutionBoundaryState.WorldId, worldId)
                || selectedExecutionBoundaryState.CompletedCoreSequence != 0L
                || selectedExecutionBoundaryState.AdvanceLeaseHeld))
            throw new ArgumentException(
                "An injected Core execution boundary must be the exact unpublished selected runtime identity.",
                nameof(selectedExecutionBoundaryState));
        if (selectedCalendar != null
            && runtimeAdmissionContext?.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
            throw new ArgumentException(
                "A precomposed Core calendar may be injected only for the selected Daily-v1 runtime.",
                nameof(selectedCalendar));
        dailyExecutionBoundary = selectedExecutionBoundaryState
            ?? new DailyV1ExecutionBoundaryState(
                simulationTime,
                runtimeAdmissionContext,
                compositionProfile,
                worldId);
        bool hasP16AProfileState = armedForceSpatialStateStore?.P16Profile != null;
        bool p16AComposition = compositionProfile == SimulationRuntimeCompositionProfile.P16AOneHopMilitary;
        bool p17AComposition = compositionProfile == SimulationRuntimeCompositionProfile.P17AWithdrawalWar;
        bool hasP17AState = warStore?.Records.Any(record => record?.P17A != null) == true;
        p16AOwnerThreadId = p16AComposition || p17AComposition
            ? Thread.CurrentThread.ManagedThreadId
            : 0;
        this.p17AScenarioAuthorityCapability = p17AScenarioAuthorityCapability;
        if (runtimeAdmissionContext != null
            && (structureStore != null || hasP16AProfileState
                || compositionProfile != SimulationRuntimeCompositionProfile.Standard))
        {
            throw new ArgumentException(
                "UnityBootstrap-Daily-v1 does not admit P15-A or P16-A proving state/compositions.",
                nameof(compositionProfile));
        }
        if (compositionProfile == SimulationRuntimeCompositionProfile.P15AProvingStructure
            ? structureStore == null || spatialAuthorityStore == null
            : structureStore != null)
        {
            throw new ArgumentException(
                "StructureStore is allowed only with an explicit P15AProvingStructure composition and an existing SpatialAuthorityStore.",
                nameof(structureStore));
        }
        if (p16AComposition
            ? !hasP16AProfileState || armedForceStore == null || spatialAuthorityStore == null
            : (!p17AComposition && hasP16AProfileState))
        {
            throw new ArgumentException(
                "P16-A state requires the explicit P16AOneHopMilitary composition and its force/spatial authorities.",
                nameof(armedForceSpatialStateStore));
        }
        if (p16AComposition
            && armedForceSpatialStateStore.CaptureP16AState()?.RequiresP17AProvenance == true)
        {
            throw new ArgumentException(
                "P16-A standalone composition cannot admit P17-A-provenance movement state.",
                nameof(armedForceSpatialStateStore));
        }
        if (hasP17AState && !p17AComposition)
        {
            throw new ArgumentException(
                "P17-A War state requires the explicit P17AWithdrawalWar composition.",
                nameof(warStore));
        }
        if (p17AComposition)
        {
            PersistentWarRecord[] p17AWars = warStore?.Records
                .Where(record => record?.P17A != null)
                .ToArray() ?? Array.Empty<PersistentWarRecord>();
            if (!hasP16AProfileState || armedForceStore == null || spatialAuthorityStore == null
                || factionStore == null || p17AWars.Length != 1
                || p17AScenarioAuthorityCapability == null
                || p17AWars[0].P17A.ScenarioAuthorityId != p17AScenarioAuthorityCapability.AuthorityId
                || armedForceSpatialStateStore.CaptureP16AState()?.RequiresP17AProvenance != true
                || armedForceSpatialStateStore.CaptureP16AState()?.TrustedAuthorityId != p17AScenarioAuthorityCapability.AuthorityId)
            {
                throw new ArgumentException(
                    "P17-A composition requires one fully configured P17-A War, its matching P16 provenance profile and host authority capability.",
                    nameof(compositionProfile));
            }
        }
        else if (p17AScenarioAuthorityCapability != null)
        {
            throw new ArgumentException(
                "A P17-A scenario authority capability is allowed only in the P17AWithdrawalWar composition.",
                nameof(p17AScenarioAuthorityCapability));
        }
        if ((p16AComposition || p17AComposition) && p18dIntradayProfile != null)
        {
            throw new ArgumentException(
                "P16-A one-boundary movement and P17-A War do not compose with a P18 intraday profile.",
                nameof(p18dIntradayProfile));
        }
        if (runtimeAdmissionContext != null)
        {
            if (p18dIntradayProfile != null)
            {
                throw new ArgumentException(
                    "The P12 daily runtime-admission adapter cannot be combined with a P18 timeline profile.",
                    nameof(runtimeAdmissionContext));
            }

            if (!runtimeAdmissionContext.IsOwnedByCurrentThread())
            {
                throw new ArgumentException(
                    "The P12 runtime must be constructed on its captured Unity Start thread.",
                    nameof(runtimeAdmissionContext));
            }
        }

        if (requireP12ReceiptCensusOwners
            && (runtimeAdmissionContext == null
                || runtimeAdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1))
        {
            throw new ArgumentException(
                "The full P12 receipt-owner inventory is valid only for the selected UnityBootstrap-Daily-v1 profile.",
                nameof(requireP12ReceiptCensusOwners));
        }

        if (requireP12RuntimeIdentitySpatialCensusOwners
            && (runtimeAdmissionContext == null
                || runtimeAdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || runtimeIdentityRegistry == null
                || spatialNetworkRuntime == null
                || explorableSiteStore == null
                || !spatialNetworkRuntime.UsesIdentityRegistry(runtimeIdentityRegistry)))
        {
            throw new ArgumentException(
                "The selected UnityBootstrap-Daily-v1 profile requires its exact RuntimeIdentityRegistry, legacy SpatialNetworkRuntime, and ExplorableSiteStore census owners.",
                nameof(requireP12RuntimeIdentitySpatialCensusOwners));
        }

        this.runtimeIdAllocator = runtimeIdAllocator;
        this.economyTransactionService = economyTransactionService;
        this.requireP12ReceiptCensusOwners = requireP12ReceiptCensusOwners;
        p12RuntimeIdentityRegistry = runtimeIdentityRegistry;
        p12SpatialNetworkRuntime = spatialNetworkRuntime;
        this.requireP12RuntimeIdentitySpatialCensusOwners = requireP12RuntimeIdentitySpatialCensusOwners;
        if (runtimeAdmissionContext != null && runtimeIdAllocator != null)
        {
            runtimeIdAllocatorEventCounterCensusProvider =
                RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(runtimeIdAllocator);
            runtimeIdAllocatorDecisionCounterCensusProvider =
                RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(runtimeIdAllocator);
            if (travelPartySystem != null)
            {
                runtimeIdAllocatorTravelPartyCounterCensusProvider =
                    RuntimeIdAllocatorCensusProvider.CreateTravelPartyCounterProvider(runtimeIdAllocator);
            }
        }
        if (runtimeAdmissionContext != null)
        {
            simulationRecordSequence = recordSequence
                ?? decisionRecorder?.RecordSequence
                ?? new SimulationRecordSequence();
            if (recordSequence != null
                && decisionRecorder != null
                && !ReferenceEquals(recordSequence, decisionRecorder.RecordSequence))
            {
                throw new ArgumentException(
                    "The selected P12 runtime and decision recorder must share the exact SimulationRecordSequence owner.",
                    nameof(recordSequence));
            }
            simulationRecordSequenceCensusProvider =
                new SimulationRecordSequenceCensusProvider(simulationRecordSequence);
        }
        List<CityRuntime> resolvedCities = cities != null
            ? new List<CityRuntime>(cities)
            : new List<CityRuntime>();
        List<NpcRuntime> resolvedNpcRuntimes = npcRuntimes != null
            ? new List<NpcRuntime>(npcRuntimes)
            : new List<NpcRuntime>();

        initialAbsoluteDay = this.simulationTime.AbsoluteDay;
        if (!SimulationTimeOwnerBridge.CanBindMutationStatus(this.simulationTime, mutationGuard))
        {
            throw new ArgumentException(
                "The supplied SimulationTime is already bound to another SimulationRuntime.",
                nameof(simulationTime));
        }

        foreach (CityRuntime city in resolvedCities)
        {
            if (city != null && !city.CanBindRuntimeMutationGuard(mutationGuard))
            {
                throw new ArgumentException(
                    "A CityRuntime or its population authority is already owned by another SimulationRuntime.",
                    nameof(cities));
            }
        }

        foreach (NpcRuntime npc in resolvedNpcRuntimes)
        {
            if (npc != null && !npc.CanBindRuntimeMutationGuard(mutationGuard))
            {
                throw new ArgumentException("An NpcRuntime is already owned by another SimulationRuntime.", nameof(npcRuntimes));
            }
        }

        PreflightMutationGuardBinding(scheduledDirectiveSystem, mutationGuard, nameof(scheduledDirectiveSystem));
        PreflightMutationGuardBinding(justiceSystem, mutationGuard, nameof(justiceSystem));
        PreflightMutationGuardBinding(crimeSystem, mutationGuard, nameof(crimeSystem));
        PreflightMutationGuardBinding(npcDecisionSystem, mutationGuard, nameof(npcDecisionSystem));
        PreflightMutationGuardBinding(travelSystem, mutationGuard, nameof(travelSystem));
        PreflightMutationGuardBinding(travelPartySystem, mutationGuard, nameof(travelPartySystem));
        PreflightMutationGuardBinding(merchantSystem, mutationGuard, nameof(merchantSystem));
        PreflightMutationGuardBinding(commercialKnowledgeSharingSystem, mutationGuard, nameof(commercialKnowledgeSharingSystem));
        PreflightMutationGuardBinding(explorableSiteStore, mutationGuard, nameof(explorableSiteStore));
        PreflightMutationGuardBinding(explorableSiteKnowledgeSystem, mutationGuard, nameof(explorableSiteKnowledgeSystem));
        PreflightMutationGuardBinding(expeditionSystem, mutationGuard, nameof(expeditionSystem));
        PreflightMutationGuardBinding(placeContentStore, mutationGuard, nameof(placeContentStore));
        PreflightMutationGuardBinding(adventureExpeditionAutonomySystem, mutationGuard, nameof(adventureExpeditionAutonomySystem));

        object manpowerSourceWorldCompositionIdentity = new object();
        resolvedCities.Sort((left, right) => string.CompareOrdinal(
            left?.RuntimeId ?? string.Empty,
            right?.RuntimeId ?? string.Empty));

        if (!SettlementManpowerSourceRegistry.TryCreate(
                settlementManpowerSourceRegistrations,
                resolvedCities.AsReadOnly(),
                manpowerSourceWorldCompositionIdentity,
                out SettlementManpowerSourceRegistry resolvedSettlementManpowerSourceRegistry,
                out string settlementManpowerSourceFailure))
        {
            throw new ArgumentException(
                "The settlement manpower source composition is invalid: " + settlementManpowerSourceFailure,
                nameof(settlementManpowerSourceRegistrations));
        }

        IManpowerSourceSnapshotProvider existingManpowerSourceProvider = manpowerSourceProvider
            ?? contingentManpowerStateStore?.SourceProvider;
        if (existingManpowerSourceProvider is SettlementManpowerSourceRegistry existingSettlementRegistry
            && !existingSettlementRegistry.IsComposedFor(manpowerSourceWorldCompositionIdentity))
        {
            throw new ArgumentException(
                "A settlement manpower source registry cannot be reused across SimulationRuntime compositions.",
                nameof(manpowerSourceProvider));
        }
        if (resolvedSettlementManpowerSourceRegistry != null
            && existingManpowerSourceProvider != null
            && !ReferenceEquals(existingManpowerSourceProvider, resolvedSettlementManpowerSourceRegistry))
        {
            throw new ArgumentException(
                "Settlement manpower source registrations are the D6A/D6B1 authority and cannot be combined with a different source provider.",
                nameof(manpowerSourceProvider));
        }
        IManpowerSourceSnapshotProvider resolvedManpowerSourceProvider =
            resolvedSettlementManpowerSourceRegistry ?? existingManpowerSourceProvider;

        if (configuration != null && (economyEnabled.HasValue || guardCrimeEnabled.HasValue))
        {
            throw new ArgumentException(
                "Provide EffectiveSimulationConfiguration or legacy feature flags, not both.",
                nameof(configuration));
        }

        EffectiveSimulationConfiguration resolvedConfiguration = configuration
            ?? SimulationConfigurationDefaults.CreateForRuntime(
                economyEnabled ?? true,
                guardCrimeEnabled ?? false);
        SimulationConfigurationValidationResult configurationValidation =
            SimulationConfigurationValidator.Validate(resolvedConfiguration);
        if (configurationValidation.IsValid == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime configuration is invalid: "
                + string.Join("; ", configurationValidation.Errors),
                nameof(configuration));
        }

        PersonStore resolvedPersonStore = personStore ?? new PersonStore();
        if (resolvedPersonStore.CanBindMutationGuard(mutationGuard) == false)
        {
            throw new ArgumentException(
                "The supplied PersonStore is already bound to another SimulationRuntime.",
                nameof(personStore));
        }

        ActorChoiceStore resolvedActorChoiceStore = actorChoiceStore == null
            ? new ActorChoiceStore(resolvedPersonStore)
            : runtimeAdmissionContext?.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                && ReferenceEquals(actorChoiceStore.PersonStore, resolvedPersonStore)
                    ? actorChoiceStore
                    : actorChoiceStore.Clone(resolvedPersonStore);

        if (politicalWorldRevision.HasValue && politicalWorldRevision.Value < 0L)
        {
            throw new ArgumentOutOfRangeException(
                nameof(politicalWorldRevision),
                politicalWorldRevision.Value,
                "politicalWorldRevision cannot be negative.");
        }

        if (politicalDecisionStore != null
            && politicalDecisionStore.Count > 0
            && politicalWorldRevision.HasValue == false)
        {
            throw new ArgumentException(
                "A SimulationRuntime composing political decision history must provide the captured politicalWorldRevision.",
                nameof(politicalWorldRevision));
        }

        GenealogyStore resolvedGenealogyStore = genealogyStore ?? new GenealogyStore();
        ValidateGenealogyStore(resolvedPersonStore, resolvedGenealogyStore);
        // The admitted Core Daily-v1 session already owns this exact P8
        // authority as a causal root. Retaining it is required for one
        // authoritative owner across genesis, runtime execution, and P12-C
        // capture. Compatibility runtimes keep the historical defensive
        // clone behavior.
        SpatialAuthorityStore resolvedSpatialAuthorityStore =
            selectedExecutionBoundaryState != null
            && selectedExecutionBoundaryState.HasAdmittedSelectedDailyExecution
                ? RetainSelectedSpatialAuthorityStore(spatialAuthorityStore)
                : CloneSpatialAuthorityStore(spatialAuthorityStore);
        StructureStore resolvedStructureStore = structureStore == null
            ? null
            : structureStore.Clone(resolvedSpatialAuthorityStore);
        DailyV1SelectedRuntimeOwnerGraph selectedCoreOwnerGraph =
            GetAdmittedSelectedCoreOwnerGraph(selectedExecutionBoundaryState);
        ISpatialTraversalOptionResolver resolvedTraversalOptionResolver =
            selectedCoreOwnerGraph?.PersonSpatialPositionStore?.TraversalOptionResolver
            ?? new SpatialPassageTraversalOptionResolver(resolvedSpatialAuthorityStore.PassageAuthority);
        LegacySpatialAnchorBindingStore resolvedLegacySpatialAnchorBindingStore =
            selectedCoreOwnerGraph?.LegacySpatialAnchors != null
                ? RetainSelectedCoreLegacySpatialAnchorBindingStore(
                    legacySpatialAnchorBindingStore, selectedCoreOwnerGraph, resolvedSpatialAuthorityStore)
                : CloneLegacySpatialAnchorBindingStore(
                    legacySpatialAnchorBindingStore,
                    resolvedSpatialAuthorityStore,
                    resolvedCities,
                    explorableSiteStore);
        PersonSpatialPositionStore resolvedPersonSpatialPositionStore =
            selectedCoreOwnerGraph?.PersonSpatialPositionStore != null
                ? RetainSelectedCorePersonSpatialPositionStore(
                    personSpatialPositionStore, selectedCoreOwnerGraph,
                    resolvedPersonStore, resolvedSpatialAuthorityStore)
                : ClonePersonSpatialPositionStore(
                    personSpatialPositionStore,
                    resolvedPersonStore,
                    resolvedSpatialAuthorityStore,
                    resolvedTraversalOptionResolver);
        SpatialRouteKnowledgeStore resolvedSpatialRouteKnowledgeStore =
            selectedCoreOwnerGraph?.SpatialRouteKnowledgeStore != null
                ? RetainSelectedCoreSpatialRouteKnowledgeStore(
                    spatialRouteKnowledgeStore, selectedCoreOwnerGraph, resolvedPersonStore)
                : CloneSpatialRouteKnowledgeStore(
                    spatialRouteKnowledgeStore,
                    resolvedPersonStore,
                    this.simulationTime.AbsoluteDay);
        PersonRoutePlanStore resolvedPersonRoutePlanStore =
            selectedCoreOwnerGraph?.PersonRoutePlanStore != null
                ? RetainSelectedCorePersonRoutePlanStore(
                    personRoutePlanStore, selectedCoreOwnerGraph,
                    resolvedPersonStore, resolvedSpatialRouteKnowledgeStore)
                : ClonePersonRoutePlanStore(
                    personRoutePlanStore,
                    resolvedPersonStore,
                    resolvedSpatialRouteKnowledgeStore,
                    actor => resolvedPersonSpatialPositionStore.TryGetPosition(actor, out PersonSpatialPosition currentPosition)
                        && currentPosition.IsInTransit,
                    () => this.simulationTime.AbsoluteDay,
                    this.simulationTime.AbsoluteDay);
        SpatialRoutePlanningSystem resolvedSpatialRoutePlanningSystem =
            selectedCoreOwnerGraph?.SpatialRoutePlanningSystem != null
                ? RetainSelectedCoreSpatialRoutePlanningSystem(
                    selectedCoreOwnerGraph, resolvedPersonStore,
                    resolvedSpatialAuthorityStore, resolvedSpatialRouteKnowledgeStore)
                : new SpatialRoutePlanningSystem(
                    resolvedPersonStore,
                    resolvedSpatialAuthorityStore,
                    resolvedSpatialRouteKnowledgeStore);
        InstitutionStore resolvedInstitutionStore = ResolveInstitutionStore(
            institutionStore,
            officeStore,
            p12CrimeSocialAppraisalWorldState);
        OfficeStore resolvedOfficeStore = CloneOfficeStore(
            officeStore,
            resolvedInstitutionStore,
            resolvedPersonStore,
            runtimeAdmissionContext?.IsRestoredContinuation == true);
        PropertyOwnershipStore resolvedPropertyOwnershipStore = ClonePropertyOwnershipStore(
            propertyOwnershipStore,
            resolvedPersonStore,
            simulationTime.AbsoluteDay);
        EstateStore resolvedEstateStore = CloneEstateStore(
            estateStore,
            resolvedPersonStore,
            simulationTime.AbsoluteDay);
        FactionStore resolvedFactionStore = CloneFactionStore(
            factionStore,
            resolvedPersonStore,
            simulationTime.AbsoluteDay);
        ArmedForceStore resolvedArmedForceStore = CloneArmedForceStore(
            armedForceStore,
            resolvedPersonStore);
        if (contingentManpowerStateStore != null
            && !ReferenceEquals(contingentManpowerStateStore.ArmedForceStore, armedForceStore))
        {
            throw new ArgumentException(
                "The supplied manpower state must belong to the supplied ArmedForceStore.",
                nameof(contingentManpowerStateStore));
        }
        ContingentManpowerStateStore resolvedManpowerStateStore = contingentManpowerStateStore == null
            ? ContingentManpowerStateStore.CreateLegacyBootstrap(
                resolvedArmedForceStore,
                resolvedManpowerSourceProvider)
            : contingentManpowerStateStore.CloneForRuntime(
                resolvedArmedForceStore,
                resolvedManpowerSourceProvider);
        resolvedManpowerStateStore.AttachToArmedForceStore();
        LocalTopologyStore resolvedLocalTopologyStore = localTopologyStore
            ?? armedForceSpatialStateStore?.LocalTopologyStore as LocalTopologyStore;
        ArmedForceSpatialStateStore resolvedArmedForceSpatialStateStore =
            CloneArmedForceSpatialStateStore(
                armedForceSpatialStateStore,
                resolvedArmedForceStore,
                resolvedSpatialAuthorityStore,
                resolvedLocalTopologyStore);
        PersistentConflictStore resolvedConflictStore = CloneConflictStore(
            conflictStore,
            resolvedArmedForceStore);
        PersistentWarStore resolvedWarStore = CloneWarStore(
            warStore,
            resolvedArmedForceStore,
            resolvedConflictStore,
            resolvedFactionStore,
            resolvedSpatialAuthorityStore,
            resolvedArmedForceSpatialStateStore);
        PersistentBattleStore resolvedBattleStore = CloneBattleStore(
            battleStore,
            resolvedArmedForceStore,
            resolvedConflictStore,
            resolvedWarStore,
            resolvedSpatialAuthorityStore);

        SimulationCalendar resolvedCalendar = selectedCalendar
            ?? new SimulationCalendar(calendarDefinition ?? CalendarDefinition.CreateDefault());
        if (selectedCalendar != null
            && !string.Equals(
                selectedCalendar.SemanticVersion,
                new SimulationCalendar(calendarDefinition ?? CalendarDefinition.CreateDefault()).SemanticVersion,
                StringComparison.Ordinal))
            throw new ArgumentException(
                "The injected Core calendar does not match the selected CalendarDefinition values.",
                nameof(selectedCalendar));
        if (!dailyExecutionBoundary.TryBindRuntimeConfiguration(
                resolvedConfiguration, resolvedCalendar))
            throw new ArgumentException(
                "The Core runtime identity could not bind its exact effective configuration and calendar.",
                nameof(configuration));
        this.naturalMortalitySamples = naturalMortalitySamples;
        this.aggregateDemographyProvider = aggregateDemographyProvider;
        this.personStore = resolvedPersonStore;
        this.personStoreCensusProviders = PersonStoreCensusProvider.CreateProviders(resolvedPersonStore);
        this.actorChoiceStore = resolvedActorChoiceStore;
        this.spatialAuthorityStore = resolvedSpatialAuthorityStore;
        this.structureStore = resolvedStructureStore;
        this.legacySpatialAnchorBindingStore = resolvedLegacySpatialAnchorBindingStore;
        this.personSpatialPositionStore = resolvedPersonSpatialPositionStore;
        this.spatialRouteKnowledgeStore = resolvedSpatialRouteKnowledgeStore;
        this.personRoutePlanStore = resolvedPersonRoutePlanStore;
        p8eTravelTransactionCoordinator = selectedCoreOwnerGraph?.P8ETravelCoordinator != null
            ? RetainSelectedCoreP8ETravelCoordinator(
                selectedCoreOwnerGraph, resolvedPersonSpatialPositionStore,
                resolvedPersonRoutePlanStore, resolvedSpatialRouteKnowledgeStore,
                resolvedSpatialAuthorityStore.PassageAuthority)
            : new P8ETravelTransactionCoordinator(
                resolvedPersonSpatialPositionStore, resolvedPersonRoutePlanStore, resolvedSpatialRouteKnowledgeStore,
                resolvedSpatialAuthorityStore.PassageAuthority,
                () => CurrentDay);
        this.spatialRoutePlanningSystem = resolvedSpatialRoutePlanningSystem;
        this.armedForceStore = resolvedArmedForceStore;
        this.contingentManpowerStateStore = resolvedManpowerStateStore;
        this.settlementManpowerSourceRegistry = resolvedSettlementManpowerSourceRegistry;
        this.armedForceSpatialStateStore = resolvedArmedForceSpatialStateStore;
        this.localTopologyStore = resolvedLocalTopologyStore;
        this.conflictStore = resolvedConflictStore;
        this.warStore = resolvedWarStore;
        this.battleStore = resolvedBattleStore;
        this.battleExecutionContextBuilder = new BattleExecutionContextBuilder(
            this.battleStore,
            this.armedForceStore,
            this.armedForceSpatialStateStore,
            this.spatialAuthorityStore,
            this.localTopologyStore,
            this.personStore,
            this.contingentManpowerStateStore);
        this.battleResolutionPolicy = battleResolutionPolicy;
        this.battleDirectConsequencePolicy = battleDirectConsequencePolicy;
        this.battleOutcomePlanningService = new BattleOutcomePlanningService(
            this.battleExecutionContextBuilder,
            this.simulationTime,
            this.battleResolutionPolicy);
        this.genealogyStore = runtimeAdmissionContext?.IsRestoredContinuation == true
            ? CloneRestoredGenealogyStore(resolvedGenealogyStore)
            : CloneGenealogyStore(resolvedGenealogyStore);
        this.institutionStore = resolvedInstitutionStore;
        this.officeStore = resolvedOfficeStore;
        this.propertyOwnershipStore = resolvedPropertyOwnershipStore;
        this.estateStore = resolvedEstateStore;
        this.politicalClaimStore = ClonePoliticalClaimStore(
            politicalClaimStore,
            resolvedPersonStore,
            resolvedInstitutionStore,
            resolvedOfficeStore,
            resolvedPropertyOwnershipStore,
            simulationTime.AbsoluteDay,
            runtimeAdmissionContext?.IsRestoredContinuation == true);
        this.factionStore = resolvedFactionStore;
        this.politicalSupportStore = ClonePoliticalSupportStore(
            politicalSupportStore,
            resolvedPersonStore,
            this.factionStore,
            this.politicalClaimStore,
            simulationTime.AbsoluteDay);
        this.politicalKnowledgeStore = ClonePoliticalKnowledgeStore(
            politicalKnowledgeStore,
            resolvedPersonStore,
            resolvedInstitutionStore,
            simulationTime.AbsoluteDay,
            this.politicalClaimStore,
            this.factionStore,
            this.officeStore,
            resolvedPropertyOwnershipStore);
        long initialPoliticalWorldRevision = politicalWorldRevision ?? 0L;
        this.politicalWorldRevision = initialPoliticalWorldRevision;
        lastPoliticalTruthFingerprint = ComputePoliticalTruthFingerprint();
        hasPoliticalTruthFingerprint = true;
        this.politicalDecisionStore = ClonePoliticalDecisionStore(
            politicalDecisionStore,
            this.politicalKnowledgeStore,
            simulationTime.AbsoluteDay,
            this.personStore,
            this.institutionStore,
            this.officeStore,
            this.politicalClaimStore,
            initialPoliticalWorldRevision,
            this.politicalKnowledgeStore.Revision);
        if (p12CrimeSocialAppraisalWorldState != null)
        {
            if (runtimeAdmissionContext == null)
            {
                throw new ArgumentException(
                    "A P12 Crime/Social continuation owner requires the selected Daily-v1 runtime.",
                    nameof(p12CrimeSocialAppraisalWorldState));
            }

            if (!ReferenceEquals(p12CrimeSocialAppraisalWorldState.PersonStore, this.personStore)
                || !ReferenceEquals(p12CrimeSocialAppraisalWorldState.InstitutionStore, this.institutionStore)
                || !ReferenceEquals(p12CrimeSocialAppraisalWorldState.SimulationTime, this.simulationTime))
            {
                throw new ArgumentException(
                    "The P12 Crime/Social owner must use the exact installed Person, Institution, and time owners.",
                    nameof(p12CrimeSocialAppraisalWorldState));
            }

            this.crimeSocialAppraisalWorldState = p12CrimeSocialAppraisalWorldState;
        }
        else
        {
            this.crimeSocialAppraisalWorldState = new CrimeSocialAppraisalWorldState(
                this.personStore,
                this.institutionStore,
                this.simulationTime);
        }
        if (crimeSystem != null
            && crimeSystem.SimulationTime != null
            && !ReferenceEquals(crimeSystem.SimulationTime, this.simulationTime))
        {
            throw new ArgumentException(
                "CrimeSystem must belong to the SimulationRuntime time boundary.",
                nameof(crimeSystem));
        }
        if (crimeSystem != null
            && crimeSystem.TheftOutcomeSink != null
            && !ReferenceEquals(crimeSystem.TheftOutcomeSink, this.crimeSocialAppraisalWorldState.Integration))
        {
            throw new ArgumentException(
                "CrimeSystem outcome sink must belong to the SimulationRuntime crime appraisal world.",
                nameof(crimeSystem));
        }
        this.cities = resolvedCities;
        this.cityRuntimeSnapshot = this.cities.AsReadOnly();
        List<CityRuntime> materialFlowCities = resolvedCities.FindAll(city => city != null && city.HasLocalDailyMaterialFlow);
        if (materialFlowCities.Count > 1)
            throw new LocalDailyMaterialFlowRejectedException("P14-A supports exactly one authored settlement per composed world.");
        HashSet<string> settlementIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (CityRuntime city in materialFlowCities)
        {
            if (!settlementIds.Add(city.SettlementSemanticId))
                throw new LocalDailyMaterialFlowRejectedException("P14-A settlement semantic identities must be unique.");
            city.ValidateLocalDailyMaterialFlowAnchor(this.legacySpatialAnchorBindingStore, this.spatialAuthorityStore);
        }
        this.npcRuntimes = new List<NpcRuntime>();
        this.npcRuntimeSnapshot = this.npcRuntimes.AsReadOnly();
        this.npcRegistryById = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        this.configuredActions = configuredActions != null
            ? new List<NpcActionData>(configuredActions)
            : null;
        this.scheduledDirectiveSystem = scheduledDirectiveSystem;
        this.justiceSystem = justiceSystem;
        this.crimeSystem = crimeSystem;
        this.npcDecisionSystem = npcDecisionSystem;
        this.travelSystem = travelSystem;
        this.travelPartySystem = travelPartySystem;
        this.merchantSystem = merchantSystem;
        this.commercialKnowledgeSharingSystem = commercialKnowledgeSharingSystem;
        this.randomSource = randomSource ?? new DeterministicRandomSource();
        this.explorableSiteStore = explorableSiteStore;
        this.explorableSiteKnowledgeSystem = explorableSiteKnowledgeSystem;
        this.expeditionSystem = expeditionSystem;
        this.placeContentStore = placeContentStore;
        this.decisionRecorder = decisionRecorder;
        this.adventureExpeditionAutonomySystem = adventureExpeditionAutonomySystem;
        this.logger = logger;

        IReadOnlyList<string> compositionErrors = SimulationCompositionValidator.Validate(
            this.configuration,
            new SimulationCompositionCapabilities(
                merchantTradeAvailable: merchantSystem != null,
                crimeAvailable: crimeSystem != null,
                guardCrimeAvailable: justiceSystem != null
                    && npcDecisionSystem != null
                    && npcDecisionSystem.HasProvider<GuardSystem>()));
        if (compositionErrors.Count > 0)
        {
            throw new ArgumentException(
                "The SimulationRuntime composition is invalid: "
                + string.Join("; ", compositionErrors),
                nameof(configuration));
        }

        isComposingNpcRoster = true;
        if (resolvedNpcRuntimes != null)
        {
            foreach (NpcRuntime npcRuntime in resolvedNpcRuntimes)
            {
                if (TryRegisterNpc(npcRuntime, out WorldNpcRegistryFailure failure) == false)
                {
                    throw new ArgumentException(
                        "The SimulationRuntime NPC roster is invalid: " + failure + ".",
                        nameof(npcRuntimes));
                }
            }
        }

        this.npcRuntimes.Sort((left, right) => string.CompareOrdinal(
            left?.RuntimeId ?? string.Empty,
            right?.RuntimeId ?? string.Empty));
        DailyV1SelectedRuntimeOwnerGraph selectedOwnerGraph =
            TryCreateSelectedRuntimeOwnerGraph(
                this.cities, this.npcRuntimes, spatialNetworkRuntime?.SelectedOwner,
                this.justiceSystem?.CoreOwner,
                this.travelPartySystem?.Store ?? travelSystem?.SelectedTravelPartyStore,
                this.personStore);
        if (selectedOwnerGraph != null
            && this.scheduledDirectiveSystem?.Store != null
            && !selectedOwnerGraph.TryBindScheduledDirectives(this.scheduledDirectiveSystem.Store))
            throw new InvalidOperationException(
                "The selected runtime owner graph could not bind the exact ScheduledDirectiveStore.");
        if (selectedOwnerGraph != null && this.expeditionSystem != null
            && !selectedOwnerGraph.TryBindExpeditionStore(this.expeditionSystem.Store))
            throw new InvalidOperationException("The selected runtime owner graph could not bind the exact ExpeditionStore.");
        DailyV1SelectedRuntimeOwnerGraph preboundSelectedOwnerGraph =
            dailyExecutionBoundary.SelectedRuntimeOwnerGraph;
        if (preboundSelectedOwnerGraph != null)
        {
            if (selectedOwnerGraph == null)
                throw new InvalidOperationException("The Core-prebound selected graph has no runtime owner graph to compare.");
            if (!ReferenceEquals(preboundSelectedOwnerGraph.Definitions, selectedOwnerGraph.Definitions))
                throw new InvalidOperationException("The Core-prebound selected graph has a different definition graph.");
            if (!ReferenceEquals(preboundSelectedOwnerGraph.SpatialNetwork, selectedOwnerGraph.SpatialNetwork))
                throw new InvalidOperationException("The Core-prebound selected graph has a different spatial owner.");
            if (!ReferenceEquals(preboundSelectedOwnerGraph.JusticeRecordsOwner, selectedOwnerGraph.JusticeRecordsOwner))
                throw new InvalidOperationException("The Core-prebound selected graph has a different Justice owner.");
            if (!ReferenceEquals(preboundSelectedOwnerGraph.TravelParties, selectedOwnerGraph.TravelParties))
                throw new InvalidOperationException("The Core-prebound selected graph has a different TravelParty owner.");
            if (!ReferenceEquals(preboundSelectedOwnerGraph.ScheduledDirectives, selectedOwnerGraph.ScheduledDirectives))
                throw new InvalidOperationException("The Core-prebound selected graph has a different directive owner.");
            if (!ReferenceEquals(preboundSelectedOwnerGraph.RuntimeIdentityIndex, selectedOwnerGraph.RuntimeIdentityIndex))
                throw new InvalidOperationException("The Core-prebound selected graph has a different runtime identity index.");
            if (!ReferenceEquals(preboundSelectedOwnerGraph.PersonStore, selectedOwnerGraph.PersonStore))
                throw new InvalidOperationException("The Core-prebound selected graph has a different PersonStore.");
            if (!ReferenceEquals(preboundSelectedOwnerGraph.Expeditions, selectedOwnerGraph.Expeditions))
                throw new InvalidOperationException("The Core-prebound selected graph has a different ExpeditionStore.");
            if (!preboundSelectedOwnerGraph.MatchesExactOwnerInstances(
                    this.cities.Select(city => city?.SelectedRuntimeOwners),
                    this.npcRuntimes.Select(npc => npc?.SelectedRuntimeOwners).Where(owner => owner != null)))
                throw new InvalidOperationException("The Core-prebound selected graph City/NPC owner order differs from the projected runtime roster.");
            selectedOwnerGraph = preboundSelectedOwnerGraph;
        }
        else if (!dailyExecutionBoundary.TryBindSelectedRuntimeOwnerGraph(selectedOwnerGraph))
        {
            throw new InvalidOperationException("The selected owner graph could not be bound to this runtime boundary.");
        }
        if (selectedOwnerGraph != null
            && !selectedOwnerGraph.TryBindP8DRouteOwners(
                this.spatialRouteKnowledgeStore, this.personRoutePlanStore))
            throw new InvalidOperationException(
                "The selected runtime graph could not bind its exact P8-D route owners.");
        if (selectedOwnerGraph != null && this.politicalKnowledgeStore != null
            && !selectedOwnerGraph.TryBindPoliticalKnowledgeStore(this.politicalKnowledgeStore))
            throw new InvalidOperationException(
                "The selected owner graph could not bind the exact PoliticalKnowledge authority installed by this runtime.");
        if (selectedOwnerGraph != null
            && !selectedOwnerGraph.TryBindSpatialAuthorityStore(this.spatialAuthorityStore))
            throw new InvalidOperationException(
                "The selected owner graph could not bind the exact SpatialAuthorityStore installed by this runtime.");
        if (selectedOwnerGraph != null
            && !selectedOwnerGraph.TryBindPersonSpatialPositionStore(resolvedPersonSpatialPositionStore))
            throw new InvalidOperationException(
                "The selected owner graph could not bind the exact P8-C Person-position authority installed by this runtime.");
        if (selectedOwnerGraph != null
            && !selectedOwnerGraph.TryBindLegacySpatialAnchorBindingStore(this.legacySpatialAnchorBindingStore))
            throw new InvalidOperationException(
                "The selected owner graph could not bind the exact P8 City-anchor authority installed by this runtime.");
        if (selectedOwnerGraph != null
            && !selectedOwnerGraph.TryBindP8ETravelComponents(
                this.spatialRoutePlanningSystem, p8eTravelTransactionCoordinator))
            throw new InvalidOperationException(
                "The selected owner graph could not bind the exact P8-E planner and travel coordinator installed by this runtime.");
        if (selectedOwnerGraph != null
            && !dailyExecutionBoundary.TryBindOrValidateSelectedRuntimeActorRoster(
                this.npcRuntimes.Select(npc => npc == null
                    ? null
                    : new DailyV1ActorTurnRosterProjection(npc.RuntimeId, npc.PersonId))))
            throw new InvalidOperationException(
                "The Unity NPC projection differs from the exact Core-owned ordered actor identity roster.");
        bool hasCompleteSelectedRuntimeExecutionRoots = this.runtimeIdAllocator != null
            && this.randomSource != null
            && simulationRecordSequence != null
            && this.decisionRecorder?.DecisionStore != null
            && this.actorChoiceStore != null;
        if (runtimeAdmissionContext != null && selectedOwnerGraph != null
            && (selectedExecutionBoundaryState != null || hasCompleteSelectedRuntimeExecutionRoots)
            && !dailyExecutionBoundary.TryBindSelectedRuntimeExecutionRoots(
                this.runtimeIdAllocator,
                this.randomSource,
                simulationRecordSequence,
                this.decisionRecorder?.DecisionStore,
                this.actorChoiceStore))
            throw new InvalidOperationException(
                "The selected runtime execution roots could not be bound to the exact Core owner graph.");
        selectedRuntimeOwnerGraphProvider = () => dailyExecutionBoundary.SelectedRuntimeOwnerGraph;
        foreach (NpcRuntime npcRuntime in this.npcRuntimes)
            if (npcRuntime != null && npcRuntime.UsesSelectedDailyV1Definitions
                && !npcRuntime.TryBindSelectedRuntimeOwnerGraphProvider(selectedRuntimeOwnerGraphProvider))
                throw new ArgumentException(
                    "A selected NPC owner graph is already bound to another runtime.",
                    nameof(npcRuntimes));

        if (selectedOwnerGraph != null)
        {
            selectedNpcPresenceProjectionResolver = ResolveRegisteredNpcPresenceProjection;
            foreach (CityRuntime cityRuntime in this.cities)
                if (cityRuntime?.SelectedRuntimeOwners != null
                    && !cityRuntime.CanBindSelectedNpcPresenceProjectionResolver(
                        selectedNpcPresenceProjectionResolver))
                    throw new ArgumentException(
                        "A selected City presence owner could not resolve its exact registered NPC projections.",
                        nameof(cities));
            foreach (CityRuntime cityRuntime in this.cities)
                if (cityRuntime?.SelectedRuntimeOwners != null
                    && !cityRuntime.TryBindSelectedNpcPresenceProjectionResolver(
                        selectedNpcPresenceProjectionResolver))
                    throw new InvalidOperationException(
                        "A selected City presence owner changed after its runtime projection preflight.");
        }

        this.manpowerSourceConsequencePlanningService = new ManpowerSourceConsequencePlanningService(
            this,
            this.settlementManpowerSourceRegistry,
            this.contingentManpowerStateStore.SourceProvider);
        this.battleDirectConsequencePlanningService = new BattleDirectConsequencePlanningService(
            this,
            this.battleDirectConsequencePolicy);

        if (runtimeAdmissionContext?.IsRestoredContinuation != true)
        {
            BindCoreMutationGuardAuthorities();
            coreMutationGuardAuthoritiesBound = true;
        }
        if (this.crimeSystem != null
            && this.crimeSystem.TryBindSimulationTime(this.simulationTime) == false)
        {
            throw new ArgumentException(
                "CrimeSystem must belong to the SimulationRuntime time boundary.",
                nameof(crimeSystem));
        }
        if (this.crimeSystem != null
            && this.crimeSystem.TryBindTheftOutcomeSink(this.crimeSocialAppraisalWorldState.Integration) == false
            && ReferenceEquals(
                this.crimeSystem.TheftOutcomeSink,
                this.crimeSocialAppraisalWorldState.Integration) == false)
        {
            throw new ArgumentException(
                "CrimeSystem outcome sink must belong to the SimulationRuntime crime appraisal world.",
                nameof(crimeSystem));
        }
        this.expeditionSystem?.BindWorldRuntime(this);
        this.battleOutcomeApplicationService = new BattleOutcomeApplicationService(
            this,
            mutationGuard,
            battleResolvedEventRecorder);
        isComposingNpcRoster = false;

        InitializeP18DIntradayProfile(p18dIntradayProfile);
        InitializeNpcRosterCensusProtocol();

        FactualReadAdmission factualReadAdmission = new FactualReadAdmission(this);
        if (runtimeAdmissionContext != null)
        {
            if (npcRosterCensusProtocol == null)
            {
                throw new InvalidOperationException(
                    "The P12 runtime-admission adapter could not bind because its census protocol failed to initialize: "
                    + (npcRosterCensusProtocolInitializationFailure ?? "no diagnostic was recorded") + ".");
            }
            // A P18-D timeline is the admitted clock authority. Its projected
            // SimulationTime intentionally rejects independent runtime-clock
            // dispatch, while Core's execution-boundary state advances the
            // timeline boundary directly. Bind the legacy runtime dispatcher
            // only when no timeline projection owns this clock.
            if (p18dTimeline == null
                && !SimulationTimeOwnerBridge.TryBindRuntimeAdvanceDispatcher(
                    simulationTime, TryAdvanceFromRuntimeOwnedClock))
            {
                npcRosterCensusProtocol.FaultClosed();
                throw new InvalidOperationException(
                    "The P12 runtime-admission adapter could not bind to the SimulationTime advance dispatcher.");
            }

            if (!factualReadAdmission.TryBindStores(this.factionStore, this.personStore))
            {
                throw new InvalidOperationException(
                    "The FR-B factual-read admission could not bind to the composed FactionStore and PersonStore.");
            }
        }

        factualReadCoordinator = new FactualReadCoordinator(
            factualReadAdmission,
            new IFactualReader[]
            {
                new FactionFactualReader(this.factionStore, this.personStore)
            });

    }

    SimulationRuntimeAdmissionContext IFactualReadRuntimeState.AdmissionContext => runtimeAdmissionContext;

    bool IFactualReadRuntimeState.IsWorldPublished => dailyExecutionBoundary.IsFactualReadWorldPublished;

    bool IFactualReadRuntimeState.IsHealthy
    {
        get
        {
            // This only checks the selected runtime's health. Factual coherence
            // is bounded by the two store revisions below, not the partial P12 epoch.
            return runtimeAdmissionContext != null
                && npcRosterCensusProtocol != null
                && mutationGuard.CanMutate
                && npcRosterCensusProtocol.TryReadMutationEpoch(out _, out _);
        }
    }

    bool IFactualReadRuntimeState.IsBootstrapOrAdvanceActive
    {
        get
        {
            if (advanceLeaseHeld
                || activeNpcMembershipCensusContext != null
                || activeP12MerchantOperationContext != null
                || runtimeAdmissionContext == null
                || npcRosterCensusProtocol == null
                || !npcRosterCensusProtocol.TryReadActiveOperationCount(out int activeOperationCount, out _))
            {
                return true;
            }

            return activeOperationCount != 0;
        }
    }

    bool IFactualReadRuntimeState.TryReadCompletedLogicalBoundary(out long logicalBoundary)
    {
        logicalBoundary = 0L;
        if (runtimeAdmissionContext == null
            || runtimeAdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !runtimeAdmissionContext.IsOwnedByCurrentThread()
            || ((IFactualReadRuntimeState)this).IsBootstrapOrAdvanceActive)
        {
            return false;
        }

        logicalBoundary = CurrentDay;
        return logicalBoundary >= 0L;
    }

    internal bool TryMarkWorldPublishedForFactualRead()
    {
        if (runtimeAdmissionContext == null)
            return true;

        if (factualReadCoordinator == null
            || dailyExecutionBoundary.IsFactualReadWorldPublished
            || !runtimeAdmissionContext.IsOwnedByCurrentThread()
            || !((IFactualReadRuntimeState)this).IsHealthy
            || ((IFactualReadRuntimeState)this).IsBootstrapOrAdvanceActive
            || !((IFactualReadRuntimeState)this).TryReadCompletedLogicalBoundary(out _))
        {
            return false;
        }

        return dailyExecutionBoundary.TryMarkFactualReadWorldPublished();
    }

    internal bool TryCompleteP15AProvingPublication()
    {
        if (compositionProfile != SimulationRuntimeCompositionProfile.P15AProvingStructure
            || structureStore == null
            || runtimeAdmissionContext != null
            || p15AInitialPublicationComplete
            || !mutationGuard.CanMutate
            || CurrentDay != initialAbsoluteDay
            || spatialAuthorityStore.LocationCount == 0
            || !spatialAuthorityStore.ValidateInvariants().IsValid
            || !structureStore.ValidateInvariants(CurrentDay).IsValid)
        {
            return false;
        }

        p15AInitialPublicationComplete = true;
        return true;
    }

    public bool TryCreateP15AProvingStructure(
        StructureId structureId,
        LocationId locationId,
        out StructureStoreFailure failure)
    {
        failure = StructureStoreFailure.None;
        if (compositionProfile != SimulationRuntimeCompositionProfile.P15AProvingStructure || structureStore == null)
        {
            failure = StructureStoreFailure.Create(
                StructureStoreFailureCode.ProfileNotSelected,
                "The P15-A proving composition is not selected.");
            return false;
        }
        if (!p15AInitialPublicationComplete)
        {
            failure = StructureStoreFailure.Create(
                StructureStoreFailureCode.InitialPublicationIncomplete,
                "The P15-A proving composition has not completed initial publication.");
            return false;
        }
        if (CurrentDay <= initialAbsoluteDay)
        {
            failure = StructureStoreFailure.Create(
                StructureStoreFailureCode.InvalidBoundary,
                "P15-A structure creation requires the first simulated boundary to have completed.");
            return false;
        }

        return structureStore.TryCreateStructure(
            new StructureRecord(
                structureId,
                StructureDefinitionReference.P15AProving,
                locationId,
                CurrentDay,
                0L),
            CurrentDay,
            initialPublicationComplete: true,
            out failure);
    }

    public bool TryExecuteP16AMilitaryCrossing(
        ArmedForceId forceId,
        HexId sourceHexId,
        HexId destinationHexId,
        TraversalOptionRef option,
        string operationId,
        out P16ACrossingReceipt receipt,
        out ArmedForceSpatialFailure failure)
    {
        receipt = null;
        failure = ArmedForceSpatialFailure.None;
        if (compositionProfile != SimulationRuntimeCompositionProfile.P16AOneHopMilitary
            || armedForceSpatialStateStore?.P16Profile == null)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.MovementProfileNotConfigured,
                "The P16-A one-hop military composition is not selected.");
            return false;
        }
        if (Thread.CurrentThread.ManagedThreadId != p16AOwnerThreadId)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.RuntimeOperationInProgress,
                "P16-A movement must run on the SimulationRuntime owner thread.");
            return false;
        }
        if (CurrentDay != armedForceSpatialStateStore.P16Profile.TargetBoundaryDay)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.MovementStateStale,
                "P16-A movement is accepted only at its prebound logical boundary.");
            return false;
        }
        if (!mutationGuard.CanMutate)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.RuntimeFaulted,
                "The SimulationRuntime is faulted.");
            return false;
        }
        if (!TryAcquireAdvanceLease(out DailyV1AdvanceLease lease))
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.RuntimeOperationInProgress,
                "Another SimulationRuntime advance/operation is in progress.");
            return false;
        }

        using (lease)
        {
            TraversalCostContext context = new TraversalCostContext(
                P16AMilitaryMovementProfile.TraversalContextIdentity,
                P16AMilitaryMovementProfile.TraversalContextRevision,
                1m, 1m, 1m);
            return armedForceSpatialStateStore.TryExecuteP16ACrossing(
                forceId,
                sourceHexId,
                destinationHexId,
                option,
                context,
                operationId,
                armedForceSpatialStateStore.Revision,
                armedForceStore.Revision,
                spatialAuthorityStore.Revision,
                CurrentDay,
                0L,
                out receipt,
                out failure);
        }
    }

    /// <summary>Executes the one P16 crossing that can satisfy the configured P17-A withdrawal demand.</summary>
    public bool TryExecuteP17AWithdrawalCrossing(
        WarId warId,
        ArmedForceId forceId,
        HexId sourceHexId,
        HexId destinationHexId,
        TraversalOptionRef option,
        string operationId,
        WorldCommandOrigin acceptedOrigin,
        P17AScenarioAuthorityCapability capability,
        out P16ACrossingReceipt receipt,
        out ArmedForceSpatialFailure failure)
    {
        receipt = null;
        failure = ArmedForceSpatialFailure.None;
        if (compositionProfile != SimulationRuntimeCompositionProfile.P17AWithdrawalWar
            || armedForceSpatialStateStore?.P16Profile == null)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.MovementProfileNotConfigured,
                "The P17-A withdrawal War composition is not selected.");
            return false;
        }
        if (Thread.CurrentThread.ManagedThreadId != p16AOwnerThreadId)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.RuntimeOperationInProgress,
                "P17-A commands must run on the SimulationRuntime owner thread.");
            return false;
        }
        if (!IsP17ACommandCapabilityValid(acceptedOrigin, capability))
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.InvalidMovementInput,
                "P17-A commands require the composed scenario/GM capability and its accepted origin.");
            return false;
        }
        if (warId == null || !warStore.TryGet(warId, out PersistentWarRecord war)
            || war.P17A == null || !war.IsActive)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.InvalidMovementInput,
                "The requested War is not the active P17-A withdrawal War.");
            return false;
        }
        WarWithdrawalDemand demand = war.P17A.WithdrawalDemand;
        WarParticipantBinding targetBinding = null;
        foreach (WarParticipantBinding binding in war.ParticipantBindings)
        {
            if (binding?.BindingId == demand.TargetBindingId)
            {
                targetBinding = binding;
                break;
            }
        }
        if (targetBinding == null || forceId != targetBinding.ArmedForceId
            || sourceHexId != demand.SourceHexId || destinationHexId == null
            || destinationHexId == demand.SourceHexId)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.InvalidMovementInput,
                "The P17-A crossing must move the demanded selected force out of its specified source Hex.");
            return false;
        }
        if (CurrentDay != armedForceSpatialStateStore.P16Profile.TargetBoundaryDay)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.MovementStateStale,
                "P17-A movement is accepted only at its prebound P16 logical boundary.");
            return false;
        }
        if (!mutationGuard.CanMutate)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.RuntimeFaulted,
                "The SimulationRuntime is faulted.");
            return false;
        }
        if (!TryAcquireAdvanceLease(out DailyV1AdvanceLease lease))
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.RuntimeOperationInProgress,
                "Another SimulationRuntime advance/operation is in progress.");
            return false;
        }

        using (lease)
        {
            TraversalCostContext context = new TraversalCostContext(
                P16AMilitaryMovementProfile.TraversalContextIdentity,
                P16AMilitaryMovementProfile.TraversalContextRevision,
                1m, 1m, 1m);
            return armedForceSpatialStateStore.TryExecuteP17ACrossing(
                forceId,
                sourceHexId,
                destinationHexId,
                option,
                context,
                operationId,
                armedForceSpatialStateStore.Revision,
                armedForceStore.Revision,
                spatialAuthorityStore.Revision,
                CurrentDay,
                0L,
                acceptedOrigin,
                p17AScenarioAuthorityCapability.AuthorityId,
                out receipt,
                out failure);
        }
    }

    /// <summary>Captures the current P17-A War and P16 crossing proof under the runtime's serialized owner window.</summary>
    public bool TryCaptureP17AWarObservation(
        WarId warId,
        out P17AWarObservation observation,
        out PersistentStateFailure failure)
    {
        observation = null;
        failure = PersistentStateFailure.None;
        if (compositionProfile != SimulationRuntimeCompositionProfile.P17AWithdrawalWar)
        {
            failure = PersistentStateFailure.Create(
                PersistentStateFailureCode.InvalidLifecycle,
                "The P17-A withdrawal War composition is not selected.");
            return false;
        }
        if (Thread.CurrentThread.ManagedThreadId != p16AOwnerThreadId)
        {
            failure = PersistentStateFailure.Create(
                PersistentStateFailureCode.InvalidLifecycle,
                "P17-A observations must run on the SimulationRuntime owner thread.");
            return false;
        }
        if (!TryAcquireAdvanceLease(out DailyV1AdvanceLease lease))
        {
            failure = PersistentStateFailure.Create(
                PersistentStateFailureCode.InvalidLifecycle,
                "Another SimulationRuntime advance/operation is in progress.");
            return false;
        }

        using (lease)
        {
            if (warId == null || !warStore.TryGet(warId, out PersistentWarRecord war) || war.P17A == null)
            {
                failure = PersistentStateFailure.Create(
                    PersistentStateFailureCode.NotRegistered,
                    "The requested War is not configured for P17-A.");
                return false;
            }

            WarWithdrawalDemand demand = war.P17A.WithdrawalDemand;
            WarParticipantBinding targetBinding = null;
            foreach (WarParticipantBinding binding in war.ParticipantBindings)
            {
                if (binding?.BindingId == demand.TargetBindingId)
                {
                    targetBinding = binding;
                    break;
                }
            }
            if (targetBinding == null)
            {
                failure = PersistentStateFailure.Create(
                    PersistentStateFailureCode.InvalidRecord,
                    "The P17-A demand no longer resolves to its target force binding.");
                return false;
            }

            PersistentWarStoreSnapshot warSnapshot = warStore.CaptureState();
            PersistentWarRecord capturedWar = warSnapshot.Records.FirstOrDefault(record => record?.Id == warId);
            P16AStateSnapshot p16State = armedForceSpatialStateStore.CaptureP16AState();
            if (capturedWar == null || p16State == null)
            {
                failure = PersistentStateFailure.Create(
                    PersistentStateFailureCode.InvalidRecord,
                    "P17-A owner state could not be captured coherently.");
                return false;
            }

            P16ACrossingReceipt receipt = p16State.Receipt;
            bool achieved = receipt != null
                && receipt.ForceId == targetBinding.ArmedForceId.Value
                && receipt.ForceId == p16State.ForceId
                && receipt.SourceHexId == demand.SourceHexId.Value
                && receipt.DestinationHexId != demand.SourceHexId.Value
                && p16State.PositionStableKey == "hex:" + receipt.DestinationHexId
                && receipt.LogicalBoundary > demand.ActivatedAbsoluteDay
                && receipt.LogicalBoundary == armedForceSpatialStateStore.P16Profile.TargetBoundaryDay
                && receipt.AcceptedOrder == 0L
                && receipt.OwnerRevision == p16State.OwnerRevision
                && receipt.SupplyDebited == armedForceSpatialStateStore.P16Profile.QuantityPerCrossing
                && p16State.CurrentQuantity
                    == p16State.InitialQuantity - armedForceSpatialStateStore.P16Profile.QuantityPerCrossing
                && receipt.AcceptedOrigin.HasValue
                && (receipt.AcceptedOrigin.Value == WorldCommandOrigin.GM
                    || receipt.AcceptedOrigin.Value == WorldCommandOrigin.Scenario)
                && receipt.AuthorityId == p17AScenarioAuthorityCapability.AuthorityId
                && p16State.RequiresP17AProvenance
                && p16State.TrustedAuthorityId == p17AScenarioAuthorityCapability.AuthorityId;
            observation = new P17AWarObservation(
                capturedWar,
                warSnapshot.Revision,
                targetBinding,
                achieved ? P17AWithdrawalGoalStatus.Achieved : P17AWithdrawalGoalStatus.Pending,
                p16State);
            return true;
        }
    }

    /// <summary>Ends the bounded P17-A War only through participant B's explicit concession.</summary>
    public bool TryConcedeP17AWar(
        WarId warId,
        WarStrategicParticipantId concedingParticipantId,
        string operationId,
        WorldCommandOrigin acceptedOrigin,
        P17AScenarioAuthorityCapability capability,
        out PersistentStateFailure failure)
    {
        failure = PersistentStateFailure.None;
        if (compositionProfile != SimulationRuntimeCompositionProfile.P17AWithdrawalWar
            || armedForceSpatialStateStore?.P16Profile == null)
        {
            failure = PersistentStateFailure.Create(
                PersistentStateFailureCode.InvalidLifecycle,
                "The P17-A withdrawal War composition is not selected.");
            return false;
        }
        if (Thread.CurrentThread.ManagedThreadId != p16AOwnerThreadId)
        {
            failure = PersistentStateFailure.Create(
                PersistentStateFailureCode.InvalidLifecycle,
                "P17-A commands must run on the SimulationRuntime owner thread.");
            return false;
        }
        if (!IsP17ACommandCapabilityValid(acceptedOrigin, capability))
        {
            failure = PersistentStateFailure.Create(
                PersistentStateFailureCode.InvalidRecord,
                "P17-A commands require the composed scenario/GM capability and its accepted origin.");
            return false;
        }
        long expectedWarRevision = warStore.Revision;
        long acceptedDay = CurrentDay;
        if (warId == null || !warStore.TryGet(warId, out PersistentWarRecord war) || war.P17A == null)
        {
            failure = PersistentStateFailure.Create(
                PersistentStateFailureCode.NotRegistered,
                "The requested War is not configured for P17-A.");
            return false;
        }
        if (concedingParticipantId != war.P17A.WithdrawalDemand.TargetParticipantId)
        {
            failure = PersistentStateFailure.Create(
                PersistentStateFailureCode.InvalidRecord,
                "Only the target participant may issue the bounded P17-A concession.");
            return false;
        }
        if (acceptedDay <= armedForceSpatialStateStore.P16Profile.TargetBoundaryDay)
        {
            failure = PersistentStateFailure.Create(
                PersistentStateFailureCode.InvalidDay,
                "P17-A concession is accepted only after the P16 crossing boundary has passed.");
            return false;
        }
        if (!mutationGuard.CanMutate)
        {
            failure = PersistentStateFailure.Create(
                PersistentStateFailureCode.RuntimeFaulted,
                "The SimulationRuntime is faulted.");
            return false;
        }
        if (!TryAcquireAdvanceLease(out DailyV1AdvanceLease lease))
        {
            failure = PersistentStateFailure.Create(
                PersistentStateFailureCode.InvalidLifecycle,
                "Another SimulationRuntime advance/operation is in progress.");
            return false;
        }

        using (lease)
        {
            if (warStore.Revision != expectedWarRevision || CurrentDay != acceptedDay)
            {
                failure = PersistentStateFailure.Create(
                    PersistentStateFailureCode.InvalidRecord,
                    "The P17-A War or logical day changed before concession commit.");
                return false;
            }
            WarTerminalConcession concession = new WarTerminalConcession(
                operationId,
                concedingParticipantId,
                WarConcessionReason.Concession,
                acceptedOrigin,
                p17AScenarioAuthorityCapability.AuthorityId,
                acceptedDay,
                0L);
            return warStore.TryConcedeP17A(
                warId,
                expectedWarRevision,
                acceptedDay,
                concession,
                out failure);
        }
    }

    private bool IsP17ACommandCapabilityValid(
        WorldCommandOrigin acceptedOrigin,
        P17AScenarioAuthorityCapability capability)
    {
        return capability != null
            && ReferenceEquals(capability, p17AScenarioAuthorityCapability)
            && capability.AuthorityId == p17AScenarioAuthorityCapability.AuthorityId
            && (acceptedOrigin == WorldCommandOrigin.GM || acceptedOrigin == WorldCommandOrigin.Scenario);
    }

    private bool TryRegisterP12GateOneFixedOwnerSections(ContinuationCensusProtocol protocol)
    {
        if (protocol == null
            || genealogyStore == null
            || politicalKnowledgeStore == null
            || politicalDecisionStore == null)
            return false;

        // Bootstrap admission proves that these required owner sections start
        // empty. A restored continuation instead registers the reconstructed
        // owner state as its fresh baseline; requiring zero here would reject
        // valid non-empty Genealogy/PoliticalKnowledge/PoliticalDecision data.
        bool requireEmptyBootstrapState = runtimeAdmissionContext?.IsRestoredContinuation != true;

        return TryRegisterP12FixedOwnerSection(
                protocol,
                new GenealogyCensusProvider(genealogyStore),
                GenealogyCensusProvider.SectionId,
                GenealogyCensusProvider.SchemaVersion,
                OwnerSectionRole.Required,
                genealogyStore,
                requireZeroRevision: requireEmptyBootstrapState,
                expectedInitialCardinality: requireEmptyBootstrapState ? 0 : (int?)null)
            && TryRegisterP12FixedOwnerSection(
                protocol,
                new PoliticalKnowledgeStoreCensusProvider(politicalKnowledgeStore),
                PoliticalKnowledgeStoreCensusProvider.SectionId,
                PoliticalKnowledgeStoreCensusProvider.SchemaVersion,
                OwnerSectionRole.Required,
                politicalKnowledgeStore,
                requireZeroRevision: requireEmptyBootstrapState,
                expectedInitialCardinality: requireEmptyBootstrapState ? 0 : (int?)null)
            && TryRegisterP12FixedOwnerSection(
                protocol,
                new PoliticalDecisionStoreCensusProvider(politicalDecisionStore),
                PoliticalDecisionStoreCensusProvider.SectionId,
                PoliticalDecisionStoreCensusProvider.SchemaVersion,
                OwnerSectionRole.Required,
                politicalDecisionStore,
                requireZeroRevision: requireEmptyBootstrapState,
                expectedInitialCardinality: requireEmptyBootstrapState ? 0 : (int?)null);
    }

    private void InitializeNpcRosterCensusProtocol()
    {
        npcRosterCensusProtocolInitializationFailure = null;
        ContinuationCensusProtocol protocol = runtimeAdmissionContext != null
            ? new ContinuationCensusProtocol(dailyExecutionBoundary)
            : new ContinuationCensusProtocol();
        npcRosterCensusProtocol = protocol;
        bool RecordRegistration(bool registered, string registration)
        {
            if (!registered && npcRosterCensusProtocolInitializationFailure == null)
                npcRosterCensusProtocolInitializationFailure = registration;
            return registered;
        }

        OwnerSectionContract personMembership = new OwnerSectionContract(
            PersonMembershipCensusProvider.SectionId,
            PersonMembershipCensusProvider.SchemaVersion,
            OwnerSectionRole.Required);
        OwnerSectionContract personBindings = new OwnerSectionContract(
            PersonMaterializationBindingCensusProvider.SectionId,
            PersonMaterializationBindingCensusProvider.SchemaVersion,
            OwnerSectionRole.Required);
        if (!RecordRegistration(protocol.RegisterExpectedSection(personMembership, out _), "PersonMembership section")
            || !RecordRegistration(protocol.RegisterExpectedSection(personBindings, out _), "PersonMaterializationBinding section")
            || !RecordRegistration(protocol.RegisterCensusProvider(
                PersonMembershipCensusProvider.SectionId,
                personStoreCensusProviders[0],
                out _), "PersonMembership provider")
            || !RecordRegistration(protocol.RegisterCensusProvider(
                PersonMaterializationBindingCensusProvider.SectionId,
                personStoreCensusProviders[1],
                out _), "PersonMaterializationBinding provider")
            || !RecordRegistration(protocol.RegisterSpatialKnowledgeRosterFamily(npcRuntimeSnapshot, out _), "SpatialKnowledge roster family")
            || !RecordRegistration(protocol.RegisterNpcTravelStateRosterFamily(npcRuntimeSnapshot, out _), "NpcTravelState roster family")
            || !RecordRegistration(protocol.RegisterInventoryRosterFamily(npcRuntimeSnapshot, out _), "Inventory roster family")
            || !RecordRegistration(protocol.RegisterMoneyAccountRosterFamily(npcRuntimeSnapshot, out _), "MoneyAccount roster family")
            || !RecordRegistration(protocol.RegisterNpcKnowledgeRosterFamily(npcRuntimeSnapshot, out _), "NpcKnowledge roster family")
            || !RecordRegistration(protocol.RegisterNpcPlanRosterFamily(npcRuntimeSnapshot, out _), "NpcPlan roster family")
            || (runtimeAdmissionContext != null
                && !RecordRegistration(protocol.RegisterNpcReceiptOwnerRosterFamily(npcRuntimeSnapshot, out _), "NpcReceipt owner roster family"))
            || (runtimeAdmissionContext != null
                && !RecordRegistration(TryRegisterSimulationRecordSequenceCensusProvider(protocol), "SimulationRecordSequence provider"))
            || (runtimeAdmissionContext != null && runtimeIdAllocator != null
                && !RecordRegistration(TryRegisterRuntimeIdAllocatorEventCounterCensusProvider(protocol), "RuntimeIdAllocator event counter provider"))
            || (runtimeAdmissionContext != null && runtimeIdAllocator != null
                && !RecordRegistration(TryRegisterRuntimeIdAllocatorDecisionCounterCensusProvider(protocol), "RuntimeIdAllocator decision counter provider"))
            || (runtimeAdmissionContext != null && runtimeIdAllocator != null
                && travelPartySystem != null
                && !RecordRegistration(TryRegisterRuntimeIdAllocatorTravelPartyCounterCensusProvider(protocol), "RuntimeIdAllocator TravelParty counter provider"))
            || (runtimeAdmissionContext != null && scheduledDirectiveSystem != null
                && !RecordRegistration(TryRegisterScheduledDirectiveCensusProvider(protocol), "ScheduledDirective provider"))
            || (runtimeAdmissionContext != null
                && !RecordRegistration(TryRegisterActorChoiceP11CensusProvider(protocol), "ActorChoice provider"))
            || (runtimeAdmissionContext != null
                && !RecordRegistration(TryRegisterTravelPartyCensusProvider(protocol), "TravelParty provider"))
            || (runtimeAdmissionContext != null
                && !RecordRegistration(TryRegisterExpeditionCensusProvider(protocol), "Expedition provider"))
            || (runtimeAdmissionContext != null
                && !RecordRegistration(TryRegisterCityNpcPresenceCensusProviders(protocol), "City NPC-presence providers"))
            || (runtimeAdmissionContext != null && !RecordRegistration(TryRegisterCityMarketCensusProviders(protocol), "City Market providers"))
            || (runtimeAdmissionContext != null && !RecordRegistration(TryRegisterInstitutionOfficeCensusProviders(protocol), "Institution/Office providers"))
            || (runtimeAdmissionContext != null && !RecordRegistration(TryRegisterFactionStoreCensusProviders(protocol), "Faction providers"))
            || (runtimeAdmissionContext != null && !RecordRegistration(TryRegisterPoliticalClaimCensusProviders(protocol), "PoliticalClaim providers"))
            || (runtimeAdmissionContext != null && !RecordRegistration(TryRegisterPoliticalSupportCensusProviders(protocol), "PoliticalSupport providers"))
            || (runtimeAdmissionContext != null && !RecordRegistration(TryRegisterPropertyEstateCensusProviders(protocol), "Property/Estate providers"))
            || (runtimeAdmissionContext != null && !RecordRegistration(TryRegisterP12PopulationCensusProviders(protocol), "P12 population providers"))
            || (runtimeAdmissionContext != null && !RecordRegistration(TryRegisterP12GateOneFixedOwnerSections(protocol), "P12 Gate One fixed owner sections"))
            || (runtimeAdmissionContext != null && !RecordRegistration(TryRegisterP12CrimeSocialAppraisalOwnerSections(protocol), "P12 Crime/SocialAppraisal owner sections"))
            || (requireP12RuntimeIdentitySpatialCensusOwners
                && !RecordRegistration(TryRegisterP12RuntimeIdentitySpatialCensusOwners(protocol), "P12 RuntimeIdentity/Spatial owners"))
            || (requireP12ReceiptCensusOwners && !RecordRegistration(TryRegisterP12ExactZeroReceiptOwners(protocol), "P12 exact-zero receipt owners"))
            || (runtimeAdmissionContext != null
                && !RecordRegistration(TryRegisterP12P8DExactZeroOwnerSections(protocol), "P12 P8-D exact-zero owner sections"))
            || (runtimeAdmissionContext != null
                && !RecordRegistration(TryRegisterP12EMilitaryOwnerSections(protocol), "P12 P8-E military owner sections"))
            || !RecordRegistration(protocol.SealExpectedSectionInventory(out _), "expected section inventory seal")
            || !RecordRegistration(protocol.SealCensusProviderInventory(out _), "census provider inventory seal"))
        {
            npcRosterCensusProtocolInitializationFailure ??= "owner section/provider registration failed";
            npcRosterCensusProtocol = null;
            return;
        }

        IReadOnlyList<string> expectedOperationContractIds =
            DailyV1RuntimeProtocolOperationContracts.GetExpectedOperationContractIds(
                runtimeAdmissionContext != null,
                travelPartySystem != null,
                travelSystem != null);
        bool operationsRegistered = true;
        foreach (string operationContractId in expectedOperationContractIds)
        {
            if (protocol.RegisterExpectedOperation(operationContractId, out _)) continue;
            operationsRegistered = false;
            break;
        }

        bool ownerThreadBound = false;
        if (operationsRegistered && protocol.SealOperationInventory(out _))
        {
            ownerThreadBound = runtimeAdmissionContext == null
                ? protocol.BindOwnerThread(out _)
                : protocol.BindOwnerThread(
                    runtimeAdmissionContext.ExpectedOwnerThread,
                    runtimeAdmissionContext.ExpectedOwnerThreadId,
                    out _);
        }
        if (!operationsRegistered || !ownerThreadBound)
        {
            npcRosterCensusProtocolInitializationFailure =
                "operation registration or owner-thread binding failed";
            npcRosterCensusProtocol = null;
            return;
        }

        if (!protocol.TryAssessOwnerSectionInventory(out _))
        {
            npcRosterCensusProtocolInitializationFailure = "initial owner-section census failed";
            npcRosterCensusProtocol = null;
            return;
        }

        if (runtimeAdmissionContext != null)
        {
            try
            {
                SimulationRecordSequenceOwnerBridge.BindP12MutationBoundary(
                    simulationRecordSequence,
                    CanCommitP12SimulationRecordSequenceMutation,
                    NotifyP12SimulationRecordSequenceMutation);

                if (runtimeIdAllocatorEventCounterCensusProvider != null)
                {
                    RuntimeIdAllocatorOwnerBridge.BindP12EventIdMutationBoundary(
                        runtimeIdAllocator,
                        CanCommitP12RuntimeIdEventCounterMutation,
                        NotifyP12RuntimeIdEventCounterMutation);
                }

                if (runtimeIdAllocatorDecisionCounterCensusProvider != null)
                {
                    RuntimeIdAllocatorOwnerBridge.BindP12DecisionIdMutationBoundary(
                        runtimeIdAllocator,
                        CanCommitP12RuntimeIdDecisionCounterMutation,
                        NotifyP12RuntimeIdDecisionCounterMutation);
                }

                if (runtimeIdAllocatorTravelPartyCounterCensusProvider != null)
                {
                    RuntimeIdAllocatorOwnerBridge.BindP12TravelPartyIdMutationBoundary(
                        runtimeIdAllocator,
                        CanCommitP12RuntimeIdTravelPartyCounterMutation,
                        NotifyP12RuntimeIdTravelPartyCounterMutation);
                }

                if (scheduledDirectiveCensusProvider != null)
                {
                    scheduledDirectiveSystem.Store.BindP12MutationBoundary(
                        CanCommitP12ScheduledDirectiveMutation,
                        NotifyP12ScheduledDirectiveMutation);
                }

                if (actorChoiceP11CensusProvider != null)
                {
                    actorChoiceStore.BindP12MutationBoundary(
                        CanCommitP12ActorChoiceMutation,
                        NotifyP12ActorChoiceMutation);
                }

                if (!TryRebindNpcOwnerMutationBoundaries())
                    throw new InvalidOperationException("The P12 NPC owner mutation boundaries could not bind to the accepted owner census.");
                if (!TryBindP12LifecycleMutationBoundaries())
                    throw new InvalidOperationException("The P12 population and lifecycle mutation boundaries could not bind to the accepted owner census.");

                if (travelPartyCensusProvider != null)
                {
                    TravelPartyStore store = travelPartySystem?.Store;
                    travelPartyStoreMutationBinding = new P12NpcOwnerMutationBinding(
                        store,
                        new[] { TravelPartyCensusProvider.SectionId });
                    travelPartyStoreMutationBinding.Admission = CanCommitP12TravelPartyStoreMutation;
                    travelPartyStoreMutationBinding.Committed = NotifyP12TravelPartyStoreMutation;
                    store.BindP12MutationBoundary(
                        travelPartyStoreMutationBinding.Admission,
                        travelPartyStoreMutationBinding.Committed);
                }

                if (travelPartySystem != null
                    && !travelPartySystem.BindP12StartOperationAdmission(
                        TryAdmitP12TravelPartyStartPreparation))
                {
                    throw new InvalidOperationException(
                        "The selected TravelPartySystem could not bind its P12 start-operation admission boundary.");
                }

                foreach (KeyValuePair<CityRuntime, string> pair in cityNpcPresenceSectionIdsByOwner)
                {
                    CityRuntime city = pair.Key;
                    city.BindP12PresenceMutationBoundary(
                        () => CanCommitP12CityPresenceMutation(city),
                        () => NotifyP12CityPresenceMutation(city));
                }

                foreach (KeyValuePair<MarketRuntime, string> pair in marketSectionIdsByOwner)
                {
                    MarketRuntime market = pair.Key;
                    string sectionId = pair.Value;
                    market.BindP12MutationBoundary(
                        () => CanCommitP12MarketOwnerMutation(market, sectionId),
                        () => NotifyP12MarketOwnerMutation(sectionId));
                }
            }
            catch (Exception exception)
            {
                npcRosterCensusProtocolInitializationFailure =
                    "P12 mutation-boundary binding failed (" + exception.GetType().Name + ")";
                protocol.FaultClosed();
                npcRosterCensusProtocol = null;
            }
        }
    }

    private bool TryRegisterInstitutionOfficeCensusProviders(ContinuationCensusProtocol protocol)
    {
        if (protocol == null || institutionStore == null || officeStore == null
            || !InstitutionOfficeOwnerBridge.BelongsTo(officeStore, institutionStore))
        {
            protocol?.FaultClosed();
            return false;
        }

        bool requireEmptyBootstrapState = runtimeAdmissionContext?.IsRestoredContinuation != true;
        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> providers =
                InstitutionOfficeCensusProvider.CreateProviders(institutionStore, officeStore);
            string[] sectionIds =
            {
                InstitutionOfficeCensusProvider.InstitutionsSectionId,
                InstitutionOfficeCensusProvider.OfficesSectionId,
                InstitutionOfficeCensusProvider.IncumbenciesSectionId,
                InstitutionOfficeCensusProvider.TenuresSectionId
            };
            int[] cardinalities =
            {
                institutionStore.Count,
                officeStore.Count,
                officeStore.IncumbencyCount,
                officeStore.TenureCount
            };
            if (providers == null || providers.Count != sectionIds.Length)
            {
                protocol.FaultClosed();
                return false;
            }

            for (int i = 0; i < sectionIds.Length; i++)
            {
                IOwnerSectionCensusProvider provider = providers[i];
                OwnerSectionCensusWitness witness = provider?.GetCurrentCensus();
                object expectedOwner = i == 0 ? institutionStore : officeStore;
                long expectedRevision = i == 0 ? institutionStore.Revision : officeStore.Revision;
                if (witness == null
                    || !string.Equals(witness.SectionId, sectionIds[i], StringComparison.Ordinal)
                    || witness.SchemaVersion != InstitutionOfficeCensusProvider.SchemaVersion
                    || witness.Cardinality != cardinalities[i]
                    || (requireEmptyBootstrapState && cardinalities[i] != 0)
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, expectedOwner)
                    || witness.Revision != expectedRevision
                    || witness.Revision < 0L
                    || !protocol.RegisterExpectedSection(
                        new OwnerSectionContract(
                            sectionIds[i],
                            InstitutionOfficeCensusProvider.SchemaVersion,
                            OwnerSectionRole.Required),
                        out _)
                    || !protocol.RegisterCensusProvider(sectionIds[i], provider, out _))
                {
                    protocol.FaultClosed();
                    return false;
                }
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterFactionStoreCensusProviders(ContinuationCensusProtocol protocol)
    {
        if (protocol == null || factionStore == null)
        {
            protocol?.FaultClosed();
            return false;
        }

        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> providers =
                FactionStoreCensusProvider.CreateProviders(factionStore);
            string[] sectionIds =
            {
                FactionStoreCensusProvider.FactionsSectionId,
                FactionStoreCensusProvider.AffiliationsSectionId
            };
            int[] cardinalities = { factionStore.Count, factionStore.AffiliationCount };
            bool requireEmptyBootstrapState = runtimeAdmissionContext?.IsRestoredContinuation != true;
            if (providers == null || providers.Count != sectionIds.Length
                || (requireEmptyBootstrapState && (cardinalities[0] != 0 || cardinalities[1] != 0)))
            {
                protocol.FaultClosed();
                return false;
            }

            for (int i = 0; i < sectionIds.Length; i++)
            {
                IOwnerSectionCensusProvider provider = providers[i];
                OwnerSectionCensusWitness witness = provider?.GetCurrentCensus();
                if (witness == null
                    || !string.Equals(witness.SectionId, sectionIds[i], StringComparison.Ordinal)
                    || witness.SchemaVersion != FactionStoreCensusProvider.SchemaVersion
                    || witness.Cardinality != cardinalities[i]
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, factionStore)
                    || witness.Revision != factionStore.Revision
                    || witness.Revision < 0L
                    || !protocol.RegisterExpectedSection(
                        new OwnerSectionContract(
                            sectionIds[i],
                            FactionStoreCensusProvider.SchemaVersion,
                            OwnerSectionRole.Required),
                        out _)
                    || !protocol.RegisterCensusProvider(sectionIds[i], provider, out _))
                {
                    protocol.FaultClosed();
                    return false;
                }
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterPoliticalClaimCensusProviders(ContinuationCensusProtocol protocol)
    {
        if (protocol == null || politicalClaimStore == null)
        {
            protocol?.FaultClosed();
            return false;
        }

        bool requireEmptyBootstrapState = runtimeAdmissionContext?.IsRestoredContinuation != true;

        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> providers =
                PoliticalClaimStoreCensusProvider.CreateProviders(politicalClaimStore);
            string[] sectionIds =
            {
                PoliticalClaimStoreCensusProvider.ClaimsSectionId,
                PoliticalClaimStoreCensusProvider.RecognitionsSectionId
            };
            int[] cardinalities = { politicalClaimStore.Count, politicalClaimStore.RecognitionCount };
            for (int i = 0; i < sectionIds.Length; i++)
            {
                IOwnerSectionCensusProvider provider = providers != null && providers.Count > i
                    ? providers[i]
                    : null;
                OwnerSectionCensusWitness witness = provider?.GetCurrentCensus();
                if (providers == null || providers.Count != sectionIds.Length
                    || (requireEmptyBootstrapState && cardinalities[i] != 0)
                    || witness == null
                    || !string.Equals(witness.SectionId, sectionIds[i], StringComparison.Ordinal)
                    || witness.SchemaVersion != PoliticalClaimStoreCensusProvider.SchemaVersion
                    || witness.Cardinality != cardinalities[i]
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, politicalClaimStore)
                    || witness.Revision != politicalClaimStore.Revision
                    || witness.Revision < 0L
                    || !protocol.RegisterExpectedSection(
                        new OwnerSectionContract(
                            sectionIds[i],
                            PoliticalClaimStoreCensusProvider.SchemaVersion,
                            OwnerSectionRole.Required),
                        out _)
                    || !protocol.RegisterCensusProvider(sectionIds[i], provider, out _))
                {
                    protocol.FaultClosed();
                    return false;
                }
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterPoliticalSupportCensusProviders(ContinuationCensusProtocol protocol)
    {
        if (protocol == null || politicalSupportStore == null)
        {
            protocol?.FaultClosed();
            return false;
        }

        bool requireEmptyBootstrapState = runtimeAdmissionContext?.IsRestoredContinuation != true;

        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> providers =
                PoliticalSupportStoreCensusProvider.CreateProviders(politicalSupportStore);
            IOwnerSectionCensusProvider provider = providers != null && providers.Count == 1
                ? providers[0]
                : null;
            OwnerSectionCensusWitness witness = provider?.GetCurrentCensus();
            if (witness == null
                || !string.Equals(
                    witness.SectionId,
                    PoliticalSupportStoreCensusProvider.RelationsSectionId,
                    StringComparison.Ordinal)
                || witness.SchemaVersion != PoliticalSupportStoreCensusProvider.SchemaVersion
                || witness.Cardinality != politicalSupportStore.Count
                || (requireEmptyBootstrapState && politicalSupportStore.Count != 0)
                || !ReferenceEquals(witness.OwnerInstanceIdentity, politicalSupportStore)
                || witness.Revision != politicalSupportStore.Revision
                || witness.Revision < 0L
                || !protocol.RegisterExpectedSection(
                    new OwnerSectionContract(
                        PoliticalSupportStoreCensusProvider.RelationsSectionId,
                        PoliticalSupportStoreCensusProvider.SchemaVersion,
                        OwnerSectionRole.Required),
                    out _)
                || !protocol.RegisterCensusProvider(
                    PoliticalSupportStoreCensusProvider.RelationsSectionId,
                    provider,
                    out _))
            {
                protocol.FaultClosed();
                return false;
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterPropertyEstateCensusProviders(ContinuationCensusProtocol protocol)
    {
        if (protocol == null || propertyOwnershipStore == null || estateStore == null)
        {
            protocol?.FaultClosed();
            return false;
        }

        bool requireEmptyBootstrapState = runtimeAdmissionContext?.IsRestoredContinuation != true;
        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> propertyProviders =
                PropertyOwnershipCensusProvider.CreateProviders(propertyOwnershipStore);
            IOwnerSectionCensusProvider estateProvider = new EstateCensusProvider(estateStore);
            IOwnerSectionCensusProvider[] providers =
            {
                propertyProviders != null && propertyProviders.Count > 0 ? propertyProviders[0] : null,
                propertyProviders != null && propertyProviders.Count > 1 ? propertyProviders[1] : null,
                estateProvider
            };
            string[] sectionIds =
            {
                PropertyOwnershipCensusProvider.OwnershipSectionId,
                PropertyOwnershipCensusProvider.TransferHistorySectionId,
                EstateCensusProvider.SectionId
            };
            object[] expectedOwners = { propertyOwnershipStore, propertyOwnershipStore, estateStore };
            int[] schemaVersions =
            {
                PropertyOwnershipCensusProvider.SchemaVersion,
                PropertyOwnershipCensusProvider.SchemaVersion,
                EstateCensusProvider.SchemaVersion
            };
            long[] expectedRevisions =
            {
                propertyOwnershipStore.Revision,
                propertyOwnershipStore.Revision,
                estateStore.Revision
            };

            if (propertyProviders == null || propertyProviders.Count != 2)
            {
                protocol.FaultClosed();
                return false;
            }

            for (int i = 0; i < providers.Length; i++)
            {
                OwnerSectionCensusWitness witness = providers[i]?.GetCurrentCensus();
                int expectedCardinality = i == 0
                    ? propertyOwnershipStore.Count
                    : i == 1
                        ? propertyOwnershipStore.TransferHistory.Count
                        : estateStore.Count;
                if (witness == null
                    || !string.Equals(witness.SectionId, sectionIds[i], StringComparison.Ordinal)
                    || witness.SchemaVersion != schemaVersions[i]
                    || witness.Cardinality != expectedCardinality
                    || (requireEmptyBootstrapState && expectedCardinality != 0)
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, expectedOwners[i])
                    || witness.Revision != expectedRevisions[i]
                    || witness.Revision < 0L
                    || !protocol.RegisterExpectedSection(
                        new OwnerSectionContract(
                            sectionIds[i],
                            schemaVersions[i],
                            OwnerSectionRole.Required),
                        out _)
                    || !protocol.RegisterCensusProvider(sectionIds[i], providers[i], out _))
                {
                    protocol.FaultClosed();
                    return false;
                }
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterSimulationRecordSequenceCensusProvider(ContinuationCensusProtocol protocol)
    {
        if (protocol == null
            || simulationRecordSequence == null
            || simulationRecordSequenceCensusProvider == null)
        {
            protocol?.FaultClosed();
            return false;
        }

        try
        {
            OwnerSectionCensusWitness witness = simulationRecordSequenceCensusProvider.GetCurrentCensus();
            if (witness == null
                || !string.Equals(
                    witness.SectionId,
                    SimulationRecordSequenceCensusProvider.SectionId,
                    StringComparison.Ordinal)
                || witness.SchemaVersion != SimulationRecordSequenceCensusProvider.SchemaVersion
                || witness.Cardinality != 1
                || !SimulationRecordSequenceOwnerBridge.TryMatchCurrentCensus(
                    simulationRecordSequence,
                    witness.OwnerInstanceIdentity,
                    witness.Revision,
                    out _)
                || !protocol.RegisterExpectedSection(
                    new OwnerSectionContract(
                        SimulationRecordSequenceCensusProvider.SectionId,
                        SimulationRecordSequenceCensusProvider.SchemaVersion,
                        OwnerSectionRole.Required),
                    out _)
                || !protocol.RegisterCensusProvider(
                    SimulationRecordSequenceCensusProvider.SectionId,
                    simulationRecordSequenceCensusProvider,
                    out _))
            {
                protocol.FaultClosed();
                return false;
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterActorChoiceP11CensusProvider(ContinuationCensusProtocol protocol)
    {
        if (protocol == null || actorChoiceStore == null)
        {
            protocol?.FaultClosed();
            return false;
        }

        try
        {
            ActorChoiceP11CensusProvider provider = new ActorChoiceP11CensusProvider(actorChoiceStore);
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (witness == null
                || !string.Equals(
                    witness.SectionId,
                    ActorChoiceP11CensusProvider.SectionId,
                    StringComparison.Ordinal)
                || witness.SchemaVersion != ActorChoiceP11CensusProvider.SchemaVersion
                || witness.Cardinality < 0
                || witness.Cardinality != actorChoiceStore.P11InputCount
                || !ReferenceEquals(witness.OwnerInstanceIdentity, actorChoiceStore.CensusOwnerIdentity)
                || witness.Revision != actorChoiceStore.CensusRevision
                || !protocol.RegisterExpectedSection(
                    new OwnerSectionContract(
                        ActorChoiceP11CensusProvider.SectionId,
                        ActorChoiceP11CensusProvider.SchemaVersion,
                        OwnerSectionRole.Required),
                    out _)
                || !protocol.RegisterCensusProvider(
                    ActorChoiceP11CensusProvider.SectionId,
                    provider,
                    out _))
            {
                protocol.FaultClosed();
                return false;
            }

            actorChoiceP11CensusProvider = provider;
            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterRuntimeIdAllocatorEventCounterCensusProvider(ContinuationCensusProtocol protocol)
    {
        if (protocol == null
            || runtimeIdAllocator == null
            || runtimeIdAllocatorEventCounterCensusProvider == null)
        {
            protocol?.FaultClosed();
            return false;
        }

        try
        {
            OwnerSectionCensusWitness witness = runtimeIdAllocatorEventCounterCensusProvider.GetCurrentCensus();
            if (witness == null
                || !string.Equals(
                    witness.SectionId,
                    RuntimeIdAllocatorCensusProvider.EventsSectionId,
                    StringComparison.Ordinal)
                || witness.SchemaVersion != RuntimeIdAllocatorCensusProvider.SchemaVersion
                || witness.Cardinality != 1
                || !RuntimeIdAllocatorOwnerBridge.TryMatchCurrentCensus(
                    runtimeIdAllocator,
                    RuntimeIdAllocatorCensusCounter.Events,
                    witness.OwnerInstanceIdentity,
                    witness.Revision,
                    out _)
                || !protocol.RegisterExpectedSection(
                    new OwnerSectionContract(
                        RuntimeIdAllocatorCensusProvider.EventsSectionId,
                        RuntimeIdAllocatorCensusProvider.SchemaVersion,
                        OwnerSectionRole.Required),
                    out _)
                || !protocol.RegisterCensusProvider(
                    RuntimeIdAllocatorCensusProvider.EventsSectionId,
                    runtimeIdAllocatorEventCounterCensusProvider,
                    out _))
            {
                protocol.FaultClosed();
                return false;
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterRuntimeIdAllocatorDecisionCounterCensusProvider(ContinuationCensusProtocol protocol)
    {
        if (protocol == null
            || runtimeIdAllocator == null
            || runtimeIdAllocatorDecisionCounterCensusProvider == null)
        {
            protocol?.FaultClosed();
            return false;
        }

        try
        {
            OwnerSectionCensusWitness witness = runtimeIdAllocatorDecisionCounterCensusProvider.GetCurrentCensus();
            if (witness == null
                || !string.Equals(
                    witness.SectionId,
                    RuntimeIdAllocatorCensusProvider.DecisionsSectionId,
                    StringComparison.Ordinal)
                || witness.SchemaVersion != RuntimeIdAllocatorCensusProvider.SchemaVersion
                || witness.Cardinality != 1
                || !RuntimeIdAllocatorOwnerBridge.TryMatchCurrentCensus(
                    runtimeIdAllocator,
                    RuntimeIdAllocatorCensusCounter.Decisions,
                    witness.OwnerInstanceIdentity,
                    witness.Revision,
                    out _)
                || !protocol.RegisterExpectedSection(
                    new OwnerSectionContract(
                        RuntimeIdAllocatorCensusProvider.DecisionsSectionId,
                        RuntimeIdAllocatorCensusProvider.SchemaVersion,
                        OwnerSectionRole.Required),
                    out _)
                || !protocol.RegisterCensusProvider(
                    RuntimeIdAllocatorCensusProvider.DecisionsSectionId,
                    runtimeIdAllocatorDecisionCounterCensusProvider,
                    out _))
            {
                protocol.FaultClosed();
                return false;
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterScheduledDirectiveCensusProvider(ContinuationCensusProtocol protocol)
    {
        ScheduledDirectiveStore store = scheduledDirectiveSystem?.Store;
        if (protocol == null || store == null)
        {
            protocol?.FaultClosed();
            return false;
        }

        try
        {
            ScheduledDirectiveCensusProvider provider = new ScheduledDirectiveCensusProvider(store);
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (witness == null
                || !string.Equals(
                    witness.SectionId,
                    ScheduledDirectiveCensusProvider.SectionId,
                    StringComparison.Ordinal)
                || witness.SchemaVersion != ScheduledDirectiveCensusProvider.SchemaVersion
                || witness.Cardinality < 0
                || !ReferenceEquals(witness.OwnerInstanceIdentity, store)
                || witness.Revision != store.Revision
                || !protocol.RegisterExpectedSection(
                    new OwnerSectionContract(
                        ScheduledDirectiveCensusProvider.SectionId,
                        ScheduledDirectiveCensusProvider.SchemaVersion,
                        OwnerSectionRole.Required),
                    out _)
                || !protocol.RegisterCensusProvider(
                    ScheduledDirectiveCensusProvider.SectionId,
                    provider,
                    out _))
            {
                protocol.FaultClosed();
                return false;
            }

            scheduledDirectiveCensusProvider = provider;
            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterCityMarketCensusProviders(ContinuationCensusProtocol protocol)
    {
        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> providers =
                CityMarketCensusProvider.CreateProviders(cities);
            HashSet<CityRuntime> owners = new HashSet<CityRuntime>();
            for (int i = 0; i < providers.Count; i++)
            {
                IOwnerSectionCensusProvider provider = providers[i];
                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                if (!TryResolveCurrentCityMarketWitness(witness, out CityRuntime city)
                    || !owners.Add(city)
                    || marketSectionIdsByOwner.ContainsKey(city.Market)
                    || !protocol.RegisterExpectedSection(
                        new OwnerSectionContract(
                            witness.SectionId,
                            witness.SchemaVersion,
                            OwnerSectionRole.Required),
                        out _)
                    || !protocol.RegisterCensusProvider(witness.SectionId, provider, out _))
                {
                    protocol.FaultClosed();
                    return false;
                }

                marketSectionIdsByOwner.Add(city.Market, witness.SectionId);
            }

            if (providers.Count != cities.Count || owners.Count != cities.Count)
            {
                protocol.FaultClosed();
                return false;
            }
            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterP12ExactZeroReceiptOwners(ContinuationCensusProtocol protocol)
    {
        if (protocol == null
            || decisionRecorder == null
            || economyTransactionService == null)
        {
            protocol?.FaultClosed();
            return false;
        }

        return TryRegisterP12ExplicitlyEmptyReceiptOwner(
                protocol,
                decisionRecorder,
                NpcDecisionRecorder.OccurrenceReceiptSectionId,
                NpcDecisionRecorder.OccurrenceReceiptSectionSchemaVersion)
            && TryRegisterP12ExplicitlyEmptyReceiptOwner(
                protocol,
                economyTransactionService,
                EconomyTransactionService.KeyedSaleReceiptSectionId,
                EconomyTransactionService.KeyedSaleReceiptSectionSchemaVersion);
    }

    private static bool TryRegisterP12ExplicitlyEmptyReceiptOwner(
        ContinuationCensusProtocol protocol,
        IOwnerSectionCensusProvider provider,
        string expectedSectionId,
        int expectedSchemaVersion)
    {
        if (protocol == null) return false;

        try
        {
            OwnerSectionContract contract = new OwnerSectionContract(
                expectedSectionId,
                expectedSchemaVersion,
                OwnerSectionRole.ExplicitlyEmpty);
            OwnerSectionCensusWitness first = provider?.GetCurrentCensus();
            OwnerSectionCensusWitness second = provider?.GetCurrentCensus();
            if (!IsValidP12ExplicitlyEmptyReceiptWitness(contract, first)
                || !IsValidP12ExplicitlyEmptyReceiptWitness(contract, second)
                || !ReferenceEquals(first.OwnerInstanceIdentity, second.OwnerInstanceIdentity)
                || first.Cardinality != second.Cardinality
                || first.Revision != second.Revision
                || !protocol.RegisterExpectedSection(contract, out _)
                || !protocol.RegisterCensusProvider(expectedSectionId, provider, out _))
            {
                protocol.FaultClosed();
                return false;
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private static bool IsValidP12ExplicitlyEmptyReceiptWitness(
        OwnerSectionContract contract,
        OwnerSectionCensusWitness witness)
    {
        return contract != null
            && witness != null
            && string.Equals(witness.SectionId, contract.SectionId, StringComparison.Ordinal)
            && witness.SchemaVersion == contract.SchemaVersion
            && witness.OwnerInstanceIdentity != null
            && witness.Cardinality == 0
            && witness.Revision >= 0L;
    }

    private bool TryRegisterTravelPartyCensusProvider(ContinuationCensusProtocol protocol)
    {
        if (protocol == null) return false;
        if (travelPartySystem == null) return true;
        try
        {
            TravelPartyStore store = travelPartySystem.Store;
            TravelPartyCensusProvider provider = new TravelPartyCensusProvider(store);
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (witness == null
                || !string.Equals(witness.SectionId, TravelPartyCensusProvider.SectionId, StringComparison.Ordinal)
                || witness.SchemaVersion != TravelPartyCensusProvider.SchemaVersion
                || !ReferenceEquals(witness.OwnerInstanceIdentity, store)
                || witness.Cardinality != store.ActiveParties.Count
                || witness.Revision != store.Revision
                || !protocol.RegisterExpectedSection(
                    new OwnerSectionContract(
                        TravelPartyCensusProvider.SectionId,
                        TravelPartyCensusProvider.SchemaVersion,
                        OwnerSectionRole.Required),
                    out _)
                || !protocol.RegisterCensusProvider(
                    TravelPartyCensusProvider.SectionId,
                    provider,
                    out _))
            {
                protocol.FaultClosed();
                return false;
            }

            travelPartyCensusProvider = provider;
            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterExpeditionCensusProvider(ContinuationCensusProtocol protocol)
    {
        if (protocol == null) return false;
        if (expeditionSystem == null) return true;
        try
        {
            ExpeditionStore store = expeditionSystem.Store;
            ExpeditionCensusProvider provider = new ExpeditionCensusProvider(store);
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (witness == null
                || !string.Equals(witness.SectionId, ExpeditionCensusProvider.SectionId, StringComparison.Ordinal)
                || witness.SchemaVersion != ExpeditionCensusProvider.SchemaVersion
                || !ReferenceEquals(witness.OwnerInstanceIdentity, store)
                || !protocol.RegisterExpectedSection(
                    new OwnerSectionContract(
                        ExpeditionCensusProvider.SectionId,
                        ExpeditionCensusProvider.SchemaVersion,
                        OwnerSectionRole.Required),
                    out _)
                || !protocol.RegisterCensusProvider(
                    ExpeditionCensusProvider.SectionId,
                    provider,
                    out _))
            {
                protocol.FaultClosed();
                return false;
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryRegisterCityNpcPresenceCensusProviders(ContinuationCensusProtocol protocol)
    {
        if (protocol == null) return false;
        try
        {
            IReadOnlyList<IOwnerSectionCensusProvider> providers =
                CityNpcPresenceCensusProvider.CreateProviders(cities, npcRuntimeSnapshot);
            Dictionary<CityRuntime, string> stagedSections = new Dictionary<CityRuntime, string>();
            HashSet<string> sectionIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<CityRuntime> owners = new HashSet<CityRuntime>();
            for (int i = 0; i < providers.Count; i++)
            {
                IOwnerSectionCensusProvider provider = providers[i];
                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                if (!TryResolveCurrentCityNpcPresenceWitness(witness, out CityRuntime city)
                    || !owners.Add(city)
                    || !sectionIds.Add(witness.SectionId)
                    || !protocol.RegisterExpectedSection(
                        new OwnerSectionContract(
                            witness.SectionId,
                            witness.SchemaVersion,
                            OwnerSectionRole.Required),
                        out _)
                    || !protocol.RegisterCensusProvider(witness.SectionId, provider, out _))
                {
                    protocol.FaultClosed();
                    return false;
                }
                stagedSections.Add(city, witness.SectionId);
            }

            if (providers.Count != cities.Count)
            {
                protocol.FaultClosed();
                return false;
            }
            cityNpcPresenceSectionIdsByOwner.Clear();
            foreach (KeyValuePair<CityRuntime, string> pair in stagedSections)
                cityNpcPresenceSectionIdsByOwner.Add(pair.Key, pair.Value);
            cityNpcPresenceCensusProviders = providers;
            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
    }

    private bool TryResolveCurrentCityMarketWitness(
        OwnerSectionCensusWitness witness,
        out CityRuntime city)
    {
        city = null;
        if (witness == null || string.IsNullOrWhiteSpace(witness.SectionId)) return false;

        CityRuntime match = null;
        for (int index = 0; index < cities.Count; index++)
        {
            CityRuntime candidate = cities[index];
            if (candidate == null
                || !string.Equals(witness.SectionId,
                    CityMarketCensusProvider.SectionIdFor(candidate.RuntimeId),
                    StringComparison.Ordinal))
                continue;
            if (match != null) return false;
            match = candidate;
        }

        if (match == null
            || !ReferenceEquals(match.Market.SelectedDailyV1Owner,
                match.SelectedRuntimeOwners?.Market))
            return false;

        DailyV1MarketOwner selectedOwner = match.SelectedRuntimeOwners?.Market;
        object expectedIdentity = (object)selectedOwner ?? match.Market;
        int expectedCardinality = selectedOwner != null
            ? selectedOwner.Items.Count
            : match.Market.Items.Count;
        long expectedRevision = selectedOwner != null
            ? selectedOwner.Revision
            : match.Market.Revision;
        if (!ReferenceEquals(witness.OwnerInstanceIdentity, expectedIdentity)
            || witness.SchemaVersion != CityMarketCensusProvider.SchemaVersion
            || witness.Cardinality != expectedCardinality
            || witness.Revision != expectedRevision)
            return false;

        city = match;
        return true;
    }

    private bool TryResolveCurrentCityNpcPresenceWitness(
        OwnerSectionCensusWitness witness,
        out CityRuntime city)
    {
        city = null;
        if (witness == null || string.IsNullOrWhiteSpace(witness.SectionId)) return false;

        CityRuntime match = null;
        for (int index = 0; index < cities.Count; index++)
        {
            CityRuntime candidate = cities[index];
            if (candidate == null
                || !string.Equals(witness.SectionId,
                    CityNpcPresenceCensusProvider.SectionIdFor(candidate.RuntimeId),
                    StringComparison.Ordinal))
                continue;
            if (match != null) return false;
            match = candidate;
        }

        if (match == null) return false;
        DailyV1CityNpcPresenceOwner selectedOwner = match.SelectedRuntimeOwners?.NpcPresence;
        object expectedIdentity = (object)selectedOwner ?? match;
        int expectedCardinality = selectedOwner != null
            ? selectedOwner.OrderedNpcRuntimeIds.Count
            : match.ImportantNpcs.Count;
        long expectedRevision = selectedOwner != null
            ? selectedOwner.Revision
            : match.ImportantNpcRevision;
        if (!ReferenceEquals(witness.OwnerInstanceIdentity, expectedIdentity)
            || witness.SchemaVersion != CityNpcPresenceCensusProvider.SchemaVersion
            || witness.Cardinality != expectedCardinality
            || witness.Revision != expectedRevision
            || (selectedOwner != null && !match.HasConsistentSelectedNpcPresence))
            return false;

        city = match;
        return true;
    }

    private bool TryRebindNpcOwnerMutationBoundaries()
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent() || npcRosterCensusProtocol == null)
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (!TryUnbindNpcOwnerMutationBoundaries()) return false;

        try
        {
            List<P12NpcOwnerMutationBinding> accountBindings = new List<P12NpcOwnerMutationBinding>();
            HashSet<string> accountSectionIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<MoneyAccountRuntime> accountOwners = new HashSet<MoneyAccountRuntime>();
            IReadOnlyList<IOwnerSectionCensusProvider> accountProviders = MoneyAccountCensusProviders;
            if (accountProviders.Count != npcRuntimes.Count) throw new InvalidOperationException("NPC account census does not match the installed roster.");
            foreach (IOwnerSectionCensusProvider censusProvider in accountProviders)
            {
                if (!(censusProvider is NpcMoneyAccountCensusProvider.INpcMoneyAccountSectionCensusProvider provider)
                    || string.IsNullOrWhiteSpace(provider.RuntimeId)
                    || !npcRegistryById.TryGetValue(provider.RuntimeId, out NpcRuntime registeredNpc)
                    || !ReferenceEquals(registeredNpc, provider.NpcOwner)
                    || !ReferenceEquals(registeredNpc.MoneyAccount, provider.MoneyAccountOwner))
                    throw new InvalidOperationException("NPC account census identity does not match the installed roster.");

                OwnerSectionCensusWitness witness = censusProvider.GetCurrentCensus();
                string sectionId = NpcMoneyAccountCensusProvider.SectionPrefix + provider.RuntimeId;
                if (!accountSectionIds.Add(sectionId)
                    || !accountOwners.Add(provider.MoneyAccountOwner)
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, provider.MoneyAccountOwner)
                    || !string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal)
                    || witness.SchemaVersion != NpcMoneyAccountCensusProvider.SchemaVersion
                    || witness.Cardinality != 1
                    || witness.Revision != provider.MoneyAccountOwner.Revision)
                    throw new InvalidOperationException("NPC account census witness is not an exact current owner witness.");

                accountBindings.Add(new P12NpcOwnerMutationBinding(provider.MoneyAccountOwner, new[] { sectionId }));
            }

            List<P12NpcOwnerMutationBinding> inventoryBindings = new List<P12NpcOwnerMutationBinding>();
            HashSet<string> inventorySectionIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<IOwnerSectionCensusProvider> inventoryProviders = InventoryCensusProviders;
            if (inventoryProviders.Count != npcRuntimes.Count) throw new InvalidOperationException("NPC Inventory census does not match the installed roster.");
            foreach (IOwnerSectionCensusProvider censusProvider in inventoryProviders)
            {
                if (!(censusProvider is NpcInventoryCensusProvider.INpcInventorySectionCensusProvider provider)
                    || string.IsNullOrWhiteSpace(provider.RuntimeId)
                    || !npcRegistryById.TryGetValue(provider.RuntimeId, out NpcRuntime registeredNpc)
                    || !ReferenceEquals(registeredNpc, provider.NpcOwner)
                    || !ReferenceEquals(registeredNpc.ExistingInventory, provider.InventoryOwner))
                    throw new InvalidOperationException("NPC Inventory census identity does not match the installed roster.");

                OwnerSectionCensusWitness witness = censusProvider.GetCurrentCensus();
                string sectionId = NpcInventoryCensusProvider.SectionPrefix + provider.RuntimeId;
                if (!inventorySectionIds.Add(sectionId)
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, provider.InventoryOwner)
                    || !string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal)
                    || witness.SchemaVersion != NpcInventoryCensusProvider.SchemaVersion
                    || !provider.InventoryOwner.TryGetCensusCardinality(out int cardinality)
                    || witness.Cardinality != cardinality
                    || witness.Revision != provider.InventoryOwner.Revision)
                    throw new InvalidOperationException("NPC Inventory census witness is not an exact current owner witness.");

                P12NpcOwnerMutationBinding existing = null;
                foreach (P12NpcOwnerMutationBinding candidate in inventoryBindings)
                {
                    if (ReferenceEquals(candidate.Owner, provider.InventoryOwner))
                    {
                        existing = candidate;
                        break;
                    }
                }

                if (existing == null)
                    inventoryBindings.Add(new P12NpcOwnerMutationBinding(provider.InventoryOwner, new[] { sectionId }));
                else
                {
                    string[] expanded = new string[existing.SectionIds.Length + 1];
                    Array.Copy(existing.SectionIds, expanded, existing.SectionIds.Length);
                    expanded[expanded.Length - 1] = sectionId;
                    inventoryBindings.Remove(existing);
                    inventoryBindings.Add(new P12NpcOwnerMutationBinding(provider.InventoryOwner, expanded));
                }
            }

            List<P12NpcOwnerMutationBinding> spatialBindings = new List<P12NpcOwnerMutationBinding>();
            IReadOnlyList<IOwnerSectionCensusProvider> spatialProviders =
                SpatialKnowledgeCensusProvider.CreateProviders(npcRuntimes);
            foreach (IOwnerSectionCensusProvider censusProvider in spatialProviders)
            {
                if (!(censusProvider is SpatialKnowledgeCensusProvider.ISpatialKnowledgeSectionCensusProvider provider)
                    || string.IsNullOrWhiteSpace(provider.RuntimeId)
                    || !npcRegistryById.TryGetValue(provider.RuntimeId, out NpcRuntime registeredNpc)
                    || !ReferenceEquals(registeredNpc, provider.NpcOwner)
                    || !ReferenceEquals(registeredNpc.ExistingSpatialKnowledge, provider.SpatialKnowledgeOwner))
                    throw new InvalidOperationException("Spatial Knowledge census identity does not match the installed roster.");

                OwnerSectionCensusWitness witness = censusProvider.GetCurrentCensus();
                if (!ReferenceEquals(witness.OwnerInstanceIdentity, provider.SpatialKnowledgeOwner)
                    || witness.SchemaVersion != SpatialKnowledgeCensusProvider.SchemaVersion
                    || witness.Revision != provider.SpatialKnowledgeOwner.Revision)
                    throw new InvalidOperationException("Spatial Knowledge census witness is not an exact current owner witness.");

                AddP12NpcOwnerMutationSection(
                    spatialBindings,
                    provider.SpatialKnowledgeOwner,
                    witness.SectionId);
            }
            foreach (P12NpcOwnerMutationBinding binding in spatialBindings)
                if (binding.SectionIds.Length != 2)
                    throw new InvalidOperationException("Each SpatialKnowledge owner must bind its location and route sections.");

            List<P12NpcOwnerMutationBinding> travelStateBindings = new List<P12NpcOwnerMutationBinding>();
            IReadOnlyList<IOwnerSectionCensusProvider> travelStateProviders =
                npcRosterCensusProtocol.NpcTravelStateFamilyProviders;
            if (travelStateProviders.Count != npcRuntimes.Count)
                throw new InvalidOperationException("NPC travel-state census does not match the installed roster.");
            foreach (IOwnerSectionCensusProvider censusProvider in travelStateProviders)
            {
                if (!(censusProvider is NpcTravelStateCensusProvider provider)
                    || string.IsNullOrWhiteSpace(provider.RuntimeId)
                    || !npcRegistryById.TryGetValue(provider.RuntimeId, out NpcRuntime registeredNpc)
                    || !ReferenceEquals(registeredNpc, provider.NpcOwner))
                    throw new InvalidOperationException("NPC travel-state census identity does not match the installed roster.");

                OwnerSectionCensusWitness witness = censusProvider.GetCurrentCensus();
                string sectionId = NpcTravelStateCensusProvider.SectionIdFor(provider.RuntimeId);
                if (!ReferenceEquals(witness.OwnerInstanceIdentity, provider.NpcOwner)
                    || !string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal)
                    || witness.SchemaVersion != NpcTravelStateCensusProvider.SchemaVersion
                    || witness.Cardinality != 1
                    || witness.Revision != provider.NpcOwner.TravelStateRevision)
                    throw new InvalidOperationException("NPC travel-state census witness is not an exact current owner witness.");

                travelStateBindings.Add(new P12NpcOwnerMutationBinding(
                    provider.NpcOwner,
                    new[] { sectionId }));
            }

            List<P12NpcOwnerMutationBinding> commercialBindings = new List<P12NpcOwnerMutationBinding>();
            IReadOnlyList<IOwnerSectionCensusProvider> knowledgeProviders =
                NpcKnowledgeCensusProvider.CreateProviders(npcRuntimes);
            foreach (IOwnerSectionCensusProvider censusProvider in knowledgeProviders)
            {
                if (!(censusProvider is NpcKnowledgeCensusProvider.INpcKnowledgeSectionCensusProvider provider)
                    || provider.SectionKind < 7 || provider.SectionKind > 9)
                    continue;
                if (string.IsNullOrWhiteSpace(provider.RuntimeId)
                    || !npcRegistryById.TryGetValue(provider.RuntimeId, out NpcRuntime registeredNpc)
                    || !ReferenceEquals(registeredNpc, provider.NpcOwner)
                    || !(provider.TypedOwner is CommercialKnowledgeRuntime commercialOwner)
                    || !ReferenceEquals(registeredNpc.ExistingCommercialKnowledge, commercialOwner))
                    throw new InvalidOperationException("Commercial Knowledge census identity does not match the installed roster.");

                OwnerSectionCensusWitness witness = censusProvider.GetCurrentCensus();
                if (!ReferenceEquals(witness.OwnerInstanceIdentity, commercialOwner)
                    || witness.SchemaVersion != NpcKnowledgeCensusProvider.SchemaVersion
                    || witness.Revision != commercialOwner.Revision
                    || !string.Equals(witness.SectionId,
                        NpcKnowledgeCensusProvider.SectionIdFor(provider.SectionKind, provider.RuntimeId),
                        StringComparison.Ordinal))
                    throw new InvalidOperationException("Commercial Knowledge census witness is not an exact current owner witness.");

                AddP12NpcOwnerMutationSection(commercialBindings, commercialOwner, witness.SectionId);
            }
            foreach (P12NpcOwnerMutationBinding binding in commercialBindings)
                if (binding.SectionIds.Length != 3)
                    throw new InvalidOperationException("Each CommercialKnowledge owner must bind markets, liquidity, and share receipts.");

            List<P12NpcOwnerMutationBinding> merchantPlanBindings = new List<P12NpcOwnerMutationBinding>();
            List<P12NpcOwnerMutationBinding> travelPlanBindings = new List<P12NpcOwnerMutationBinding>();
            IReadOnlyList<IOwnerSectionCensusProvider> planProviders = npcRosterCensusProtocol.NpcPlanFamilyProviders;
            foreach (IOwnerSectionCensusProvider censusProvider in planProviders)
            {
                if (!(censusProvider is NpcPlanCensusProvider.INpcPlanSectionCensusProvider provider)
                    || string.IsNullOrWhiteSpace(provider.RuntimeId)
                    || !npcRegistryById.TryGetValue(provider.RuntimeId, out NpcRuntime registeredNpc)
                    || !ReferenceEquals(registeredNpc, provider.NpcOwner))
                    throw new InvalidOperationException("NPC plan census identity does not match the installed roster.");

                OwnerSectionCensusWitness witness = censusProvider.GetCurrentCensus();
                if (!ReferenceEquals(witness.OwnerInstanceIdentity, provider.PlanOwner)
                    || witness.Cardinality != 1
                    || witness.SchemaVersion != NpcPlanCensusProvider.SchemaVersion
                    || !string.Equals(witness.SectionId,
                        NpcPlanCensusProvider.SectionIdFor(provider.Kind, provider.RuntimeId),
                        StringComparison.Ordinal))
                    throw new InvalidOperationException("NPC plan census witness is not an exact current owner witness.");

                if (provider.Kind == NpcPlanCensusProvider.MerchantTradePlanKind
                    && (provider.PlanOwner is MerchantTradePlanRuntime legacyMerchantPlan
                        && ReferenceEquals(registeredNpc.ExistingMerchantTradePlan, legacyMerchantPlan)
                        || provider.PlanOwner is DailyV1NpcMerchantTradePlanOwner selectedMerchantPlan
                        && ReferenceEquals(registeredNpc.SelectedRuntimeOwners?.MerchantTradePlan,
                            selectedMerchantPlan)))
                    AddP12NpcOwnerMutationSection(merchantPlanBindings, provider.PlanOwner, witness.SectionId);
                else if (provider.Kind == NpcPlanCensusProvider.TravelPlanKind
                    && provider.PlanOwner is NpcTravelPlanRuntime travelPlan
                    && ReferenceEquals(registeredNpc.ExistingTravelPlan, travelPlan))
                    AddP12NpcOwnerMutationSection(travelPlanBindings, travelPlan, witness.SectionId);
                else
                    throw new InvalidOperationException("NPC plan owner does not match its exact embedded field.");
            }
            foreach (P12NpcOwnerMutationBinding binding in merchantPlanBindings)
                if (binding.SectionIds.Length != 1)
                    throw new InvalidOperationException("An embedded NPC plan owner must bind exactly one RuntimeId section.");
            foreach (P12NpcOwnerMutationBinding binding in travelPlanBindings)
                if (binding.SectionIds.Length != 1)
                    throw new InvalidOperationException("An embedded NPC plan owner must bind exactly one RuntimeId section.");

            if (accountSectionIds.Count != npcRuntimes.Count || inventorySectionIds.Count != npcRuntimes.Count)
                throw new InvalidOperationException("The NPC owner census does not cover every installed NPC.");

            foreach (P12NpcOwnerMutationBinding binding in accountBindings)
            {
                binding.Admission = () => CanCommitP12NpcOwnerMutation(binding);
                binding.Committed = () => NotifyP12NpcOwnerMutation(binding);
                MoneyAccountOwnerBridge.BindP12MutationBoundary((MoneyAccountRuntime)binding.Owner, binding.Admission, binding.Committed);
                npcMoneyAccountMutationBindings.Add((MoneyAccountRuntime)binding.Owner, binding);
            }

            foreach (P12NpcOwnerMutationBinding binding in inventoryBindings)
            {
                Array.Sort(binding.SectionIds, StringComparer.Ordinal);
                binding.Admission = () => CanCommitP12NpcOwnerMutation(binding);
                binding.Committed = () => NotifyP12NpcOwnerMutation(binding);
                ((InventoryRuntime)binding.Owner).BindP12MutationBoundary(binding.Admission, binding.Committed);
                npcInventoryMutationBindings.Add((InventoryRuntime)binding.Owner, binding);
            }

            BindP12MerchantOwnerMutationBoundaries(
                spatialBindings,
                commercialBindings,
                merchantPlanBindings,
                travelPlanBindings);

            foreach (P12NpcOwnerMutationBinding binding in travelStateBindings)
            {
                binding.TravelAdmission = (travelChanged, changedCities) =>
                    CanCommitP12NpcTravelMutation(binding, travelChanged, changedCities);
                binding.TravelCommitted = (travelChanged, changedCities) =>
                    NotifyP12NpcTravelMutation(binding, travelChanged, changedCities);
                ((NpcRuntime)binding.Owner).BindP12TravelStateMutationBoundary(
                    binding.TravelAdmission,
                    binding.TravelCommitted);
                npcTravelStateMutationBindings.Add((NpcRuntime)binding.Owner, binding);
            }

            return true;
        }
        catch
        {
            TryUnbindNpcOwnerMutationBoundaries();
            FaultRuntimeAdmission();
            return false;
        }
    }

    private static void AddP12NpcOwnerMutationSection(
        List<P12NpcOwnerMutationBinding> bindings,
        object owner,
        string sectionId)
    {
        if (bindings == null || owner == null || string.IsNullOrWhiteSpace(sectionId))
            throw new ArgumentException("A P12 owner binding requires an exact owner and section.");
        P12NpcOwnerMutationBinding existing = null;
        foreach (P12NpcOwnerMutationBinding candidate in bindings)
            if (ReferenceEquals(candidate.Owner, owner)) { existing = candidate; break; }
        if (existing == null)
        {
            bindings.Add(new P12NpcOwnerMutationBinding(owner, new[] { sectionId }));
            return;
        }
        if (Array.IndexOf(existing.SectionIds, sectionId) >= 0)
            throw new InvalidOperationException("A P12 owner section is duplicated.");
        string[] expanded = new string[existing.SectionIds.Length + 1];
        Array.Copy(existing.SectionIds, expanded, existing.SectionIds.Length);
        expanded[expanded.Length - 1] = sectionId;
        bindings.Remove(existing);
        bindings.Add(new P12NpcOwnerMutationBinding(owner, expanded));
    }

    private void BindP12MerchantOwnerMutationBoundaries(
        IEnumerable<P12NpcOwnerMutationBinding> spatialBindings,
        IEnumerable<P12NpcOwnerMutationBinding> commercialBindings,
        IEnumerable<P12NpcOwnerMutationBinding> merchantPlanBindings,
        IEnumerable<P12NpcOwnerMutationBinding> travelPlanBindings)
    {
        BindP12MerchantOwnerMutationBoundaries(
            spatialBindings,
            npcSpatialKnowledgeMutationBindings,
            (binding, admission, committed) => ((SpatialKnowledgeRuntime)binding.Owner)
                .BindP12MutationBoundary(admission, committed));
        BindP12MerchantOwnerMutationBoundaries(
            commercialBindings,
            npcCommercialKnowledgeMutationBindings,
            (binding, admission, committed) => ((CommercialKnowledgeRuntime)binding.Owner)
                .BindP12MutationBoundary(admission, committed));
        foreach (P12NpcOwnerMutationBinding binding in merchantPlanBindings)
        {
            Array.Sort(binding.SectionIds, StringComparer.Ordinal);
            binding.Admission = () => CanCommitP12MerchantOwnerMutation(binding);
            binding.Committed = () => NotifyP12MerchantOwnerMutation(binding);
            NpcRuntime ownerNpc = FindNpcForMerchantPlanAuthority(binding.Owner);
            if (ownerNpc == null || !ReferenceEquals(ownerNpc.MerchantTradePlanAuthority, binding.Owner))
                throw new InvalidOperationException("The merchant-plan census owner has no exact NPC projection.");
            binding.OwnerNpc = ownerNpc;
            ownerNpc.MerchantTradePlan.BindP12MutationBoundary(binding.Admission, binding.Committed);
            npcMerchantPlanMutationBindings.Add(binding.Owner, binding);
        }
        BindP12MerchantOwnerMutationBoundaries(
            travelPlanBindings,
            npcTravelPlanMutationBindings,
            (binding, admission, committed) => ((NpcTravelPlanRuntime)binding.Owner)
                .BindP12MutationBoundary(admission, committed));
    }

    private NpcRuntime FindNpcForMerchantPlanAuthority(object authority)
    {
        if (authority == null) return null;
        foreach (NpcRuntime npc in npcRuntimes)
            if (npc != null && ReferenceEquals(npc.MerchantTradePlanAuthority, authority)) return npc;
        return null;
    }

    private void BindP12MerchantOwnerMutationBoundaries<TOwner>(
        IEnumerable<P12NpcOwnerMutationBinding> bindings,
        Dictionary<TOwner, P12NpcOwnerMutationBinding> destination,
        Action<P12NpcOwnerMutationBinding, Func<bool>, Action> bind)
        where TOwner : class
    {
        foreach (P12NpcOwnerMutationBinding binding in bindings)
        {
            Array.Sort(binding.SectionIds, StringComparer.Ordinal);
            binding.Admission = () => CanCommitP12MerchantOwnerMutation(binding);
            binding.Committed = () => NotifyP12MerchantOwnerMutation(binding);
            bind(binding, binding.Admission, binding.Committed);
            destination.Add((TOwner)binding.Owner, binding);
        }
    }

    private bool TryUnbindNpcOwnerMutationBoundaries()
    {
        bool succeeded = true;
        foreach (KeyValuePair<MoneyAccountRuntime, P12NpcOwnerMutationBinding> pair in
            new List<KeyValuePair<MoneyAccountRuntime, P12NpcOwnerMutationBinding>>(npcMoneyAccountMutationBindings))
        {
            if (MoneyAccountOwnerBridge.UnbindP12MutationBoundary(pair.Key, pair.Value.Admission, pair.Value.Committed))
                npcMoneyAccountMutationBindings.Remove(pair.Key);
            else
                succeeded = false;
        }
        foreach (KeyValuePair<InventoryRuntime, P12NpcOwnerMutationBinding> pair in
            new List<KeyValuePair<InventoryRuntime, P12NpcOwnerMutationBinding>>(npcInventoryMutationBindings))
        {
            if (pair.Key.UnbindP12MutationBoundary(pair.Value.Admission, pair.Value.Committed))
                npcInventoryMutationBindings.Remove(pair.Key);
            else
                succeeded = false;
        }
        foreach (KeyValuePair<SpatialKnowledgeRuntime, P12NpcOwnerMutationBinding> pair in
            new List<KeyValuePair<SpatialKnowledgeRuntime, P12NpcOwnerMutationBinding>>(npcSpatialKnowledgeMutationBindings))
        {
            if (pair.Key.UnbindP12MutationBoundary(pair.Value.Admission, pair.Value.Committed))
                npcSpatialKnowledgeMutationBindings.Remove(pair.Key);
            else
                succeeded = false;
        }
        foreach (KeyValuePair<CommercialKnowledgeRuntime, P12NpcOwnerMutationBinding> pair in
            new List<KeyValuePair<CommercialKnowledgeRuntime, P12NpcOwnerMutationBinding>>(npcCommercialKnowledgeMutationBindings))
        {
            if (pair.Key.UnbindP12MutationBoundary(pair.Value.Admission, pair.Value.Committed))
                npcCommercialKnowledgeMutationBindings.Remove(pair.Key);
            else
                succeeded = false;
        }
        foreach (KeyValuePair<object, P12NpcOwnerMutationBinding> pair in
            new List<KeyValuePair<object, P12NpcOwnerMutationBinding>>(npcMerchantPlanMutationBindings))
        {
            NpcRuntime ownerNpc = pair.Value.OwnerNpc;
            if (ownerNpc != null
                && ReferenceEquals(ownerNpc.MerchantTradePlanAuthority, pair.Key)
                && ownerNpc.MerchantTradePlan.UnbindP12MutationBoundary(pair.Value.Admission, pair.Value.Committed))
            {
                pair.Value.OwnerNpc = null;
                npcMerchantPlanMutationBindings.Remove(pair.Key);
            }
            else
                succeeded = false;
        }
        foreach (KeyValuePair<NpcTravelPlanRuntime, P12NpcOwnerMutationBinding> pair in
            new List<KeyValuePair<NpcTravelPlanRuntime, P12NpcOwnerMutationBinding>>(npcTravelPlanMutationBindings))
        {
            if (pair.Key.UnbindP12MutationBoundary(pair.Value.Admission, pair.Value.Committed))
                npcTravelPlanMutationBindings.Remove(pair.Key);
            else
                succeeded = false;
        }
        foreach (KeyValuePair<NpcRuntime, P12NpcOwnerMutationBinding> pair in
            new List<KeyValuePair<NpcRuntime, P12NpcOwnerMutationBinding>>(npcTravelStateMutationBindings))
        {
            if (pair.Key.UnbindP12TravelStateMutationBoundary(
                    pair.Value.TravelAdmission,
                    pair.Value.TravelCommitted))
                npcTravelStateMutationBindings.Remove(pair.Key);
            else
                succeeded = false;
        }
        if (!succeeded) FaultRuntimeAdmission();
        return succeeded;
    }

    private bool CanCommitP12NpcOwnerMutation(P12NpcOwnerMutationBinding binding)
    {
        if (runtimeAdmissionContext == null) return true;
        if (binding == null
            || !IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null)
        {
            FaultRuntimeAdmission();
            return false;
        }

        bool exactBinding;
        if (binding.Owner is MoneyAccountRuntime account)
        {
            exactBinding = npcMoneyAccountMutationBindings.TryGetValue(account, out P12NpcOwnerMutationBinding registeredAccountBinding)
                && ReferenceEquals(registeredAccountBinding, binding)
                && binding.SectionIds.Length == 1;
            if (exactBinding)
            {
                string sectionId = binding.SectionIds[0];
                string prefix = NpcMoneyAccountCensusProvider.SectionPrefix;
                string runtimeId = sectionId.StartsWith(prefix, StringComparison.Ordinal)
                    ? sectionId.Substring(prefix.Length)
                    : null;
                exactBinding = !string.IsNullOrWhiteSpace(runtimeId)
                    && npcRegistryById.TryGetValue(runtimeId, out NpcRuntime registeredNpcForAccount)
                    && ReferenceEquals(registeredNpcForAccount.MoneyAccount, account)
                    && string.Equals(registeredNpcForAccount.RuntimeId, runtimeId, StringComparison.Ordinal);
                if (exactBinding)
                {
                    int ownerCount = 0;
                    foreach (NpcRuntime rosterNpc in npcRuntimes)
                        if (rosterNpc != null && ReferenceEquals(rosterNpc.MoneyAccount, account)) ownerCount++;
                    exactBinding = ownerCount == 1;
                }
            }
        }
        else if (binding.Owner is InventoryRuntime inventory)
        {
            exactBinding = npcInventoryMutationBindings.TryGetValue(inventory, out P12NpcOwnerMutationBinding registeredInventoryBinding)
                && ReferenceEquals(registeredInventoryBinding, binding);
            if (exactBinding)
            {
                List<string> currentSectionIds = new List<string>();
                foreach (NpcRuntime npc in npcRuntimes)
                {
                    if (npc == null || !ReferenceEquals(npc.ExistingInventory, inventory)) continue;
                    if (string.IsNullOrWhiteSpace(npc.RuntimeId)
                        || !npcRegistryById.TryGetValue(npc.RuntimeId, out NpcRuntime registeredNpc)
                        || !ReferenceEquals(registeredNpc, npc))
                    {
                        exactBinding = false;
                        break;
                    }
                    currentSectionIds.Add(NpcInventoryCensusProvider.SectionPrefix + npc.RuntimeId);
                }
                currentSectionIds.Sort(StringComparer.Ordinal);
                if (currentSectionIds.Count != binding.SectionIds.Length)
                    exactBinding = false;
                for (int i = 0; exactBinding && i < currentSectionIds.Count; i++)
                    exactBinding = string.Equals(currentSectionIds[i], binding.SectionIds[i], StringComparison.Ordinal);
                if (currentSectionIds.Count == 0) exactBinding = false;
            }
        }
        else
        {
            exactBinding = false;
        }

        if (!exactBinding
            || !CanCommitP12MutationSections(binding.SectionIds))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private bool CanCommitP12MerchantOwnerMutation(P12NpcOwnerMutationBinding binding)
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || !IsCurrentP12MerchantOwnerBinding(binding))
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (!CanCommitP12MutationSections(binding.SectionIds))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private bool IsCurrentP12MerchantOwnerBinding(P12NpcOwnerMutationBinding binding)
    {
        if (binding == null || binding.Owner == null || binding.SectionIds.Length == 0) return false;
        bool registered;
        string prefix;
        int expectedSectionCount;
        if (binding.Owner is SpatialKnowledgeRuntime spatial)
        {
            registered = npcSpatialKnowledgeMutationBindings.TryGetValue(spatial, out P12NpcOwnerMutationBinding spatialKnown)
                && ReferenceEquals(spatialKnown, binding);
            prefix = null;
            expectedSectionCount = 2;
        }
        else if (binding.Owner is CommercialKnowledgeRuntime commercial)
        {
            registered = npcCommercialKnowledgeMutationBindings.TryGetValue(commercial, out P12NpcOwnerMutationBinding commercialKnown)
                && ReferenceEquals(commercialKnown, binding);
            prefix = null;
            expectedSectionCount = 3;
        }
        else if (binding.Owner is MerchantTradePlanRuntime merchantPlan)
        {
            registered = npcMerchantPlanMutationBindings.TryGetValue(merchantPlan, out P12NpcOwnerMutationBinding merchantKnown)
                && ReferenceEquals(merchantKnown, binding);
            prefix = NpcPlanCensusProvider.MerchantTradePlanSectionPrefix;
            expectedSectionCount = 1;
        }
        else if (binding.Owner is DailyV1NpcMerchantTradePlanOwner selectedMerchantPlan)
        {
            registered = npcMerchantPlanMutationBindings.TryGetValue(selectedMerchantPlan,
                    out P12NpcOwnerMutationBinding merchantKnown)
                && ReferenceEquals(merchantKnown, binding);
            prefix = NpcPlanCensusProvider.MerchantTradePlanSectionPrefix;
            expectedSectionCount = 1;
        }
        else if (binding.Owner is NpcTravelPlanRuntime travelPlan)
        {
            registered = npcTravelPlanMutationBindings.TryGetValue(travelPlan, out P12NpcOwnerMutationBinding travelKnown)
                && ReferenceEquals(travelKnown, binding);
            prefix = NpcPlanCensusProvider.TravelPlanSectionPrefix;
            expectedSectionCount = 1;
        }
        else
        {
            return false;
        }

        if (!registered || binding.SectionIds.Length != expectedSectionCount) return false;
        string[] expected = new string[expectedSectionCount];
        int matchingNpcCount = 0;
        foreach (NpcRuntime npc in npcRuntimes)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !npcRegistryById.TryGetValue(npc.RuntimeId, out NpcRuntime installed)
                || !ReferenceEquals(installed, npc)) return false;

            bool owns = binding.Owner is SpatialKnowledgeRuntime spatialOwner
                ? ReferenceEquals(npc.ExistingSpatialKnowledge, spatialOwner)
                : binding.Owner is CommercialKnowledgeRuntime commercialOwner
                    ? ReferenceEquals(npc.ExistingCommercialKnowledge, commercialOwner)
                    : binding.Owner is MerchantTradePlanRuntime merchantOwner
                        ? ReferenceEquals(npc.ExistingMerchantTradePlan, merchantOwner)
                        : binding.Owner is DailyV1NpcMerchantTradePlanOwner selectedMerchantOwner
                            ? ReferenceEquals(npc.SelectedRuntimeOwners?.MerchantTradePlan, selectedMerchantOwner)
                        : binding.Owner is NpcTravelPlanRuntime travelOwner
                            && ReferenceEquals(npc.ExistingTravelPlan, travelOwner);
            if (!owns) continue;
            matchingNpcCount++;
            if (binding.Owner is SpatialKnowledgeRuntime)
            {
                expected[0] = SpatialKnowledgeCensusProvider.LocationsSectionPrefix + npc.RuntimeId;
                expected[1] = SpatialKnowledgeCensusProvider.RoutesSectionPrefix + npc.RuntimeId;
            }
            else if (binding.Owner is CommercialKnowledgeRuntime)
            {
                expected[0] = NpcKnowledgeCensusProvider.SectionIdFor(7, npc.RuntimeId);
                expected[1] = NpcKnowledgeCensusProvider.SectionIdFor(8, npc.RuntimeId);
                expected[2] = NpcKnowledgeCensusProvider.SectionIdFor(9, npc.RuntimeId);
            }
            else
            {
                expected[0] = prefix + npc.RuntimeId;
            }
        }

        if (matchingNpcCount != 1) return false;
        Array.Sort(expected, StringComparer.Ordinal);
        for (int i = 0; i < expected.Length; i++)
            if (!string.Equals(expected[i], binding.SectionIds[i], StringComparison.Ordinal)) return false;
        return true;
    }

    private bool CanCommitP12MutationSections(IEnumerable<string> sectionIds)
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent() || npcRosterCensusProtocol == null)
        {
            FaultRuntimeAdmission();
            return false;
        }

        HashSet<string> requested = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            if (sectionIds == null) return false;
            foreach (string sectionId in sectionIds)
            {
                if (string.IsNullOrWhiteSpace(sectionId) || !requested.Add(sectionId))
                {
                    FaultRuntimeAdmission();
                    return false;
                }
            }
        }
        catch
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (requested.Count == 0) return true;

        P12TravelPartyAdvanceOperationContext travelContext =
            activeP12TravelPartyAdvanceOperationContext;
        P12MerchantOperationContext merchantContext = activeP12MerchantOperationContext;
        P12SoloTravelStartOperationContext soloTravelContext =
            activeP12SoloTravelStartOperationContext;
        P12TravelPartyStartOperationContext travelPartyStartContext =
            activeP12TravelPartyStartOperationContext;
        int activeBatchContexts = (travelContext != null ? 1 : 0)
            + (merchantContext != null ? 1 : 0)
            + (soloTravelContext != null ? 1 : 0)
            + (travelPartyStartContext != null ? 1 : 0);
        if (activeBatchContexts > 1)
        {
            FaultRuntimeAdmission();
            return false;
        }

        NpcMembershipCensusContext membershipContext = activeNpcMembershipCensusContext;
        if (membershipContext != null)
        {
            if (!membershipContext.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return false;
            }
            requested.RemoveWhere(membershipContext.ChangedCityPresenceSectionIds.Contains);
        }

        HashSet<string> alreadyChanged = null;
        if (travelContext != null)
        {
            if (!travelContext.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return false;
            }
            alreadyChanged = travelContext.ChangedSectionIds;
        }
        else if (merchantContext != null)
        {
            if (!merchantContext.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return false;
            }
            alreadyChanged = merchantContext.ChangedSectionIds;
        }
        else if (soloTravelContext != null)
        {
            if (!soloTravelContext.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return false;
            }
            alreadyChanged = soloTravelContext.ChangedSectionIds;
        }
        else if (travelPartyStartContext != null)
        {
            if (!travelPartyStartContext.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return false;
            }
            alreadyChanged = travelPartyStartContext.ChangedSectionIds;
        }

        if (alreadyChanged != null)
            requested.RemoveWhere(alreadyChanged.Contains);
        return requested.Count == 0
            || npcRosterCensusProtocol.TryValidateUnchangedSections(requested, out _);
    }

    private bool NotifyP12MutationSections(IEnumerable<string> sectionIds)
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent() || npcRosterCensusProtocol == null)
        {
            FaultRuntimeAdmission();
            return false;
        }

        HashSet<string> changed = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            if (sectionIds == null) return false;
            foreach (string sectionId in sectionIds)
            {
                if (string.IsNullOrWhiteSpace(sectionId) || !changed.Add(sectionId))
                {
                    FaultRuntimeAdmission();
                    return false;
                }
            }
        }
        catch
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (changed.Count == 0) return true;

        P12TravelPartyAdvanceOperationContext travelContext =
            activeP12TravelPartyAdvanceOperationContext;
        P12MerchantOperationContext merchantContext = activeP12MerchantOperationContext;
        P12SoloTravelStartOperationContext soloTravelContext =
            activeP12SoloTravelStartOperationContext;
        P12TravelPartyStartOperationContext travelPartyStartContext =
            activeP12TravelPartyStartOperationContext;
        int activeBatchContexts = (travelContext != null ? 1 : 0)
            + (merchantContext != null ? 1 : 0)
            + (soloTravelContext != null ? 1 : 0)
            + (travelPartyStartContext != null ? 1 : 0);
        if (activeBatchContexts > 1)
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (travelContext != null)
        {
            if (!travelContext.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return false;
            }
            travelContext.ChangedSectionIds.UnionWith(changed);
            return true;
        }
        if (merchantContext != null)
        {
            if (!merchantContext.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return false;
            }
            merchantContext.ChangedSectionIds.UnionWith(changed);
            return true;
        }
        if (soloTravelContext != null)
        {
            if (!soloTravelContext.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return false;
            }
            soloTravelContext.ChangedSectionIds.UnionWith(changed);
            return true;
        }
        if (travelPartyStartContext != null)
        {
            if (!travelPartyStartContext.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return false;
            }
            travelPartyStartContext.ChangedSectionIds.UnionWith(changed);
            return true;
        }
        return npcRosterCensusProtocol.NotifyCommittedMutations(changed, out _);
    }

    private string[] GetP12TravelMutationSectionIds(
        P12NpcOwnerMutationBinding binding,
        bool travelStateChanged,
        IReadOnlyList<CityRuntime> changedCities)
    {
        if (binding == null || !(binding.Owner is NpcRuntime npc)
            || !npcTravelStateMutationBindings.TryGetValue(npc, out P12NpcOwnerMutationBinding current)
            || !ReferenceEquals(current, binding)
            || binding.SectionIds.Length != 1
            || !npcRegistryById.TryGetValue(npc.RuntimeId, out NpcRuntime installed)
            || !ReferenceEquals(installed, npc)
            || !string.Equals(binding.SectionIds[0],
                NpcTravelStateCensusProvider.SectionIdFor(npc.RuntimeId),
                StringComparison.Ordinal))
            return null;

        List<string> ids = new List<string>();
        if (travelStateChanged) ids.Add(binding.SectionIds[0]);
        HashSet<CityRuntime> seenCities = new HashSet<CityRuntime>();
        if (changedCities != null)
        {
            foreach (CityRuntime city in changedCities)
            {
                if (city == null || !seenCities.Add(city)
                    || !cityNpcPresenceSectionIdsByOwner.TryGetValue(city, out string sectionId)
                    || !cities.Contains(city)) return null;
                ids.Add(sectionId);
            }
        }
        return ids.ToArray();
    }

    private bool CanCommitP12NpcTravelMutation(
        P12NpcOwnerMutationBinding binding,
        bool travelStateChanged,
        IReadOnlyList<CityRuntime> changedCities)
    {
        if (runtimeAdmissionContext == null) return true;
        string[] ids = GetP12TravelMutationSectionIds(binding, travelStateChanged, changedCities);
        if (ids == null)
        {
            FaultRuntimeAdmission();
            return false;
        }
        if (ids.Length == 0) return true;
        if (!CanCommitP12MutationSections(ids))
        {
            FaultRuntimeAdmission();
            return false;
        }
        return true;
    }

    private void NotifyP12NpcTravelMutation(
        P12NpcOwnerMutationBinding binding,
        bool travelStateChanged,
        IReadOnlyList<CityRuntime> changedCities)
    {
        if (runtimeAdmissionContext == null) return;
        NpcMembershipCensusContext membershipContext = activeNpcMembershipCensusContext;
        if (membershipContext != null)
        {
            if (!membershipContext.IsOwnedByCurrentThread() || travelStateChanged)
            {
                FaultRuntimeAdmission();
                return;
            }

            string[] sectionIds = GetP12TravelMutationSectionIds(binding, false, changedCities);
            if (sectionIds == null)
            {
                FaultRuntimeAdmission();
                return;
            }
            foreach (CityRuntime city in changedCities ?? Array.Empty<CityRuntime>())
            {
                if (city == null
                    || !cityNpcPresenceSectionIdsByOwner.TryGetValue(city, out string sectionId))
                {
                    FaultRuntimeAdmission();
                    return;
                }
                membershipContext.ChangedCityPresenceSectionIds.Add(sectionId);
            }
            return;
        }

        string[] ids = GetP12TravelMutationSectionIds(binding, travelStateChanged, changedCities);
        if (ids == null || !NotifyP12MutationSections(ids))
            FaultRuntimeAdmission();
    }

    private bool CanCommitP12TravelPartyStoreMutation()
    {
        if (runtimeAdmissionContext == null) return true;
        if (travelPartyStoreMutationBinding == null
            || travelPartySystem == null
            || !ReferenceEquals(travelPartyStoreMutationBinding.Owner, travelPartySystem.Store)
            || travelPartyCensusProvider == null)
        {
            FaultRuntimeAdmission();
            return false;
        }
        return CanCommitP12MutationSections(travelPartyStoreMutationBinding.SectionIds);
    }

    private void NotifyP12TravelPartyStoreMutation()
    {
        if (runtimeAdmissionContext == null) return;
        if (travelPartyStoreMutationBinding == null
            || !ReferenceEquals(travelPartyStoreMutationBinding.Owner, travelPartySystem?.Store)
            || !NotifyP12MutationSections(travelPartyStoreMutationBinding.SectionIds))
            FaultRuntimeAdmission();
    }

    private bool TryBeginP12SoloTravelStartOperation(
        NpcRuntime npc,
        TravelActionProvider actionProvider,
        out P12SoloTravelStartOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null) return false;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || npc == null
            || string.IsNullOrWhiteSpace(npc.RuntimeId)
            || !npcRegistryById.TryGetValue(npc.RuntimeId, out NpcRuntime installedNpc)
            || !ReferenceEquals(installedNpc, npc)
            || travelSystem == null
            || actionProvider == null
            || !actionProvider.IsBoundTo(travelSystem)
            || activeNpcMembershipCensusContext != null
            || activeP12MerchantOperationContext != null
            || activeP12TravelPartyAdvanceOperationContext != null
            || activeP12SoloTravelStartOperationContext != null)
        {
            FaultRuntimeAdmission();
            return false;
        }

        List<string> sectionIds = new List<string>();
        try
        {
            if (!TryResolveNpcMoneyAccountSection(npc, out string accountSectionId)
                || !npcTravelStateMutationBindings.TryGetValue(
                    npc,
                    out P12NpcOwnerMutationBinding travelStateBinding))
                throw new InvalidOperationException();
            sectionIds.Add(accountSectionId);

            List<CityRuntime> changedCities = new List<CityRuntime>();
            CityRuntime sourceCity = npc.CurrentCity;
            if (sourceCity != null && sourceCity.ContainsImportantNpc(npc))
            {
                if (!cities.Contains(sourceCity)
                    || !cityNpcPresenceSectionIdsByOwner.TryGetValue(sourceCity, out string citySectionId)
                    || !string.Equals(
                        citySectionId,
                        CityNpcPresenceCensusProvider.SectionIdFor(sourceCity.RuntimeId),
                        StringComparison.Ordinal))
                    throw new InvalidOperationException();
                changedCities.Add(sourceCity);
            }

            string[] travelStateSectionIds = GetP12TravelMutationSectionIds(
                travelStateBinding,
                true,
                changedCities);
            if (travelStateSectionIds == null || travelStateSectionIds.Length == 0)
                throw new InvalidOperationException();
            sectionIds.AddRange(travelStateSectionIds);

            NpcTravelPlanRuntime travelPlan = npc.ExistingTravelPlan;
            if (travelPlan == null
                || !npcTravelPlanMutationBindings.TryGetValue(
                    travelPlan,
                    out P12NpcOwnerMutationBinding travelPlanBinding)
                || !ReferenceEquals(travelPlanBinding.Owner, travelPlan)
                || travelPlanBinding.SectionIds.Length != 1
                || !IsCurrentP12MerchantOwnerBinding(travelPlanBinding))
                throw new InvalidOperationException();
            sectionIds.AddRange(travelPlanBinding.SectionIds);

            SpatialKnowledgeRuntime spatialKnowledge = npc.ExistingSpatialKnowledge;
            if (spatialKnowledge == null
                || !npcSpatialKnowledgeMutationBindings.TryGetValue(
                    spatialKnowledge,
                    out P12NpcOwnerMutationBinding spatialKnowledgeBinding)
                || !ReferenceEquals(spatialKnowledgeBinding.Owner, spatialKnowledge)
                || spatialKnowledgeBinding.SectionIds.Length != 2
                || !IsCurrentP12MerchantOwnerBinding(spatialKnowledgeBinding))
                throw new InvalidOperationException();
            sectionIds.AddRange(spatialKnowledgeBinding.SectionIds);

            OwnerSectionCensusWitness eventCounterWitness =
                runtimeIdAllocatorEventCounterCensusProvider?.GetCurrentCensus();
            if (runtimeIdAllocator == null
                || eventCounterWitness == null
                || !string.Equals(
                    eventCounterWitness.SectionId,
                    RuntimeIdAllocatorCensusProvider.EventsSectionId,
                    StringComparison.Ordinal)
                || eventCounterWitness.SchemaVersion != RuntimeIdAllocatorCensusProvider.SchemaVersion
                || eventCounterWitness.Cardinality != 1
                || !RuntimeIdAllocatorOwnerBridge.TryMatchCurrentCensus(
                    runtimeIdAllocator,
                    RuntimeIdAllocatorCensusCounter.Events,
                    eventCounterWitness.OwnerInstanceIdentity,
                    eventCounterWitness.Revision,
                    out _))
                throw new InvalidOperationException();
            sectionIds.Add(RuntimeIdAllocatorCensusProvider.EventsSectionId);

            OwnerSectionCensusWitness sequenceWitness = simulationRecordSequenceCensusProvider?.GetCurrentCensus();
            if (simulationRecordSequence == null
                || sequenceWitness == null
                || !string.Equals(
                    sequenceWitness.SectionId,
                    SimulationRecordSequenceCensusProvider.SectionId,
                    StringComparison.Ordinal)
                || sequenceWitness.SchemaVersion != SimulationRecordSequenceCensusProvider.SchemaVersion
                || sequenceWitness.Cardinality != 1
                || !SimulationRecordSequenceOwnerBridge.TryMatchCurrentCensus(
                    simulationRecordSequence,
                    sequenceWitness.OwnerInstanceIdentity,
                    sequenceWitness.Revision,
                    out _))
                throw new InvalidOperationException();
            sectionIds.Add(SimulationRecordSequenceCensusProvider.SectionId);
        }
        catch
        {
            FaultRuntimeAdmission();
            return false;
        }

        HashSet<string> distinctSections = new HashSet<string>(sectionIds, StringComparer.Ordinal);
        if (distinctSections.Count != sectionIds.Count
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(sectionIds, out _)
            || !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _)
            || !TryEnterRuntimeAdmissionOperation(
                DailyV1RuntimeProtocolOperationContracts.SoloTravelStart,
                out SimulationOperationScope protocolScope))
        {
            FaultRuntimeAdmission();
            return false;
        }

        P12SoloTravelStartOperationContext context =
            new P12SoloTravelStartOperationContext(Thread.CurrentThread, protocolScope);
        activeP12SoloTravelStartOperationContext = context;
        scope = new P12SoloTravelStartOperationScope(this, context);
        return true;
    }

    private void ExitP12SoloTravelStartOperation(P12SoloTravelStartOperationContext context)
    {
        if (context == null) return;
        try
        {
            if (!ReferenceEquals(activeP12SoloTravelStartOperationContext, context)
                || !context.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return;
            }

            if (context.ChangedSectionIds.Count != 0
                && !npcRosterCensusProtocol.NotifyCommittedMutations(context.ChangedSectionIds, out _))
                FaultRuntimeAdmission();
        }
        catch
        {
            FaultRuntimeAdmission();
        }
        finally
        {
            if (ReferenceEquals(activeP12SoloTravelStartOperationContext, context))
                activeP12SoloTravelStartOperationContext = null;
            SimulationOperationScope operation = context.ProtocolScope;
            context.ProtocolScope = null;
            operation?.Dispose();
        }
    }

    private bool CanCommitP12CityPresenceMutation(CityRuntime city)
    {
        if (runtimeAdmissionContext == null) return true;
        if (city == null || !cityNpcPresenceSectionIdsByOwner.TryGetValue(city, out string sectionId)
            || !cities.Contains(city))
        {
            FaultRuntimeAdmission();
            return false;
        }
        return CanCommitP12MutationSections(new[] { sectionId });
    }

    private void NotifyP12CityPresenceMutation(CityRuntime city)
    {
        if (runtimeAdmissionContext == null) return;
        NpcMembershipCensusContext membershipContext = activeNpcMembershipCensusContext;
        if (membershipContext != null)
        {
            if (!membershipContext.IsOwnedByCurrentThread()
                || city == null
                || !cityNpcPresenceSectionIdsByOwner.ContainsKey(city))
            {
                FaultRuntimeAdmission();
                return;
            }
            if (!cityNpcPresenceSectionIdsByOwner.TryGetValue(city, out string membershipSectionId))
            {
                FaultRuntimeAdmission();
                return;
            }
            membershipContext.ChangedCityPresenceSectionIds.Add(membershipSectionId);
            return;
        }

        if (city == null || !cityNpcPresenceSectionIdsByOwner.TryGetValue(city, out string sectionId)
            || !NotifyP12MutationSections(new[] { sectionId }))
            FaultRuntimeAdmission();
    }

    private void NotifyP12MerchantOwnerMutation(P12NpcOwnerMutationBinding binding)
    {
        if (runtimeAdmissionContext == null || npcRosterCensusProtocol == null) return;
        try
        {
            if (!IsRuntimeAdmissionOwnerThreadCurrent() || !IsCurrentP12MerchantOwnerBinding(binding))
            {
                FaultRuntimeAdmission();
                return;
            }

            if (!NotifyP12MutationSections(binding.SectionIds))
                FaultRuntimeAdmission();
        }
        catch
        {
            FaultRuntimeAdmission();
        }
    }

    private bool TryBeginP12TravelPartyAdvanceOperation(
        out P12TravelPartyAdvanceOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null || travelPartySystem == null) return false;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || activeP12TravelPartyAdvanceOperationContext != null
            || activeP12MerchantOperationContext != null
            || travelPartyCensusProvider == null
            || travelPartyStoreMutationBinding == null
            || !ReferenceEquals(travelPartyStoreMutationBinding.Owner, travelPartySystem.Store))
        {
            FaultRuntimeAdmission();
            return false;
        }

        List<string> sectionIds = new List<string>();
        try
        {
            OwnerSectionCensusWitness partyWitness = travelPartyCensusProvider.GetCurrentCensus();
            if (partyWitness == null
                || !ReferenceEquals(partyWitness.OwnerInstanceIdentity, travelPartySystem.Store)
                || partyWitness.Cardinality != travelPartySystem.Store.ActiveParties.Count
                || partyWitness.Revision != travelPartySystem.Store.Revision)
            {
                FaultRuntimeAdmission();
                return false;
            }
            sectionIds.Add(TravelPartyCensusProvider.SectionId);

            if (cityNpcPresenceCensusProviders.Count != cities.Count
                || cityNpcPresenceSectionIdsByOwner.Count != cities.Count)
            {
                FaultRuntimeAdmission();
                return false;
            }
            foreach (IOwnerSectionCensusProvider provider in cityNpcPresenceCensusProviders)
            {
                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                if (!TryResolveCurrentCityNpcPresenceWitness(witness, out CityRuntime city)
                    || !cityNpcPresenceSectionIdsByOwner.TryGetValue(city, out string sectionId)
                    || !string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal))
                {
                    FaultRuntimeAdmission();
                    return false;
                }
                sectionIds.Add(sectionId);
            }

            IReadOnlyList<IOwnerSectionCensusProvider> travelProviders =
                npcRosterCensusProtocol.NpcTravelStateFamilyProviders;
            if (travelProviders.Count != npcRuntimes.Count) throw new InvalidOperationException();
            foreach (IOwnerSectionCensusProvider provider in travelProviders)
            {
                if (!(provider is NpcTravelStateCensusProvider travelProvider)
                    || !npcRegistryById.TryGetValue(travelProvider.RuntimeId, out NpcRuntime installed)
                    || !ReferenceEquals(installed, travelProvider.NpcOwner))
                    throw new InvalidOperationException();
                sectionIds.Add(travelProvider.SectionId);
            }

            IReadOnlyList<IOwnerSectionCensusProvider> spatialProviders =
                npcRosterCensusProtocol.SpatialKnowledgeFamilyProviders;
            if (spatialProviders.Count != npcRuntimes.Count * 2) throw new InvalidOperationException();
            foreach (IOwnerSectionCensusProvider provider in spatialProviders)
            {
                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                if (witness == null) throw new InvalidOperationException();
                sectionIds.Add(witness.SectionId);
            }

            if (simulationRecordSequenceCensusProvider == null)
                throw new InvalidOperationException();
            sectionIds.Add(SimulationRecordSequenceCensusProvider.SectionId);
        }
        catch
        {
            FaultRuntimeAdmission();
            return false;
        }

        HashSet<string> distinct = new HashSet<string>(sectionIds, StringComparer.Ordinal);
        if (distinct.Count != sectionIds.Count
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(sectionIds, out _)
            || !TryEnterRuntimeAdmissionOperation(
                DailyV1RuntimeProtocolOperationContracts.TravelPartyAdvance,
                out SimulationOperationScope protocolScope))
        {
            FaultRuntimeAdmission();
            return false;
        }

        P12TravelPartyAdvanceOperationContext context =
            new P12TravelPartyAdvanceOperationContext(Thread.CurrentThread, protocolScope);
        activeP12TravelPartyAdvanceOperationContext = context;
        scope = new P12TravelPartyAdvanceOperationScope(this, context);
        return true;
    }

    private void ExitP12TravelPartyAdvanceOperation(P12TravelPartyAdvanceOperationContext context)
    {
        if (context == null) return;
        try
        {
            if (!ReferenceEquals(activeP12TravelPartyAdvanceOperationContext, context)
                || !context.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return;
            }

            if (context.ChangedSectionIds.Count != 0
                && !npcRosterCensusProtocol.NotifyCommittedMutations(context.ChangedSectionIds, out _))
                FaultRuntimeAdmission();
        }
        catch
        {
            FaultRuntimeAdmission();
        }
        finally
        {
            if (ReferenceEquals(activeP12TravelPartyAdvanceOperationContext, context))
                activeP12TravelPartyAdvanceOperationContext = null;
            SimulationOperationScope operation = context.ProtocolScope;
            context.ProtocolScope = null;
            operation?.Dispose();
        }
    }

    private bool TryBeginP12MerchantDailyNpcTradeOperation(
        NpcRuntime npc,
        out P12MerchantOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null) return false;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || npc == null
            || string.IsNullOrWhiteSpace(npc.RuntimeId)
            || !npcRegistryById.TryGetValue(npc.RuntimeId, out NpcRuntime installedNpc)
            || !ReferenceEquals(installedNpc, npc)
            || activeP12MerchantOperationContext != null
            || npc.ExistingMerchantTradePlan == null
            || npc.ExistingTravelPlan == null
            || npc.ExistingSpatialKnowledge == null
            || npc.ExistingCommercialKnowledge == null
            || !npcMerchantPlanMutationBindings.TryGetValue(npc.MerchantTradePlanAuthority, out P12NpcOwnerMutationBinding merchantPlanBinding)
            || !npcTravelPlanMutationBindings.TryGetValue(npc.ExistingTravelPlan, out P12NpcOwnerMutationBinding travelPlanBinding)
            || !npcSpatialKnowledgeMutationBindings.TryGetValue(npc.ExistingSpatialKnowledge, out P12NpcOwnerMutationBinding spatialBinding)
            || !npcCommercialKnowledgeMutationBindings.TryGetValue(npc.ExistingCommercialKnowledge, out P12NpcOwnerMutationBinding commercialBinding)
            || !IsCurrentP12MerchantOwnerBinding(merchantPlanBinding)
            || !IsCurrentP12MerchantOwnerBinding(travelPlanBinding)
            || !IsCurrentP12MerchantOwnerBinding(spatialBinding)
            || !IsCurrentP12MerchantOwnerBinding(commercialBinding))
        {
            FaultRuntimeAdmission();
            return false;
        }

        List<string> sectionIds = new List<string>(
            merchantPlanBinding.SectionIds.Length + travelPlanBinding.SectionIds.Length
            + spatialBinding.SectionIds.Length + commercialBinding.SectionIds.Length);
        sectionIds.AddRange(merchantPlanBinding.SectionIds);
        sectionIds.AddRange(travelPlanBinding.SectionIds);
        sectionIds.AddRange(spatialBinding.SectionIds);
        sectionIds.AddRange(commercialBinding.SectionIds);
        HashSet<string> distinctSections = new HashSet<string>(sectionIds, StringComparer.Ordinal);
        if (distinctSections.Count != sectionIds.Count
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(sectionIds, out _)
            || !TryEnterRuntimeAdmissionOperation(DailyV1RuntimeProtocolOperationContracts.MerchantDailyNpcTrade, out SimulationOperationScope protocolScope))
        {
            FaultRuntimeAdmission();
            return false;
        }

        P12MerchantOperationContext context = new P12MerchantOperationContext(Thread.CurrentThread, protocolScope);
        activeP12MerchantOperationContext = context;
        scope = new P12MerchantOperationScope(this, context);
        return true;
    }

    private void ExitP12MerchantOperation(P12MerchantOperationContext context)
    {
        if (context == null) return;
        try
        {
            if (!ReferenceEquals(activeP12MerchantOperationContext, context)
                || !context.IsOwnedByCurrentThread())
            {
                FaultRuntimeAdmission();
                return;
            }

            if (context.ChangedSectionIds.Count != 0
                && !npcRosterCensusProtocol.NotifyCommittedMutations(context.ChangedSectionIds, out _))
                FaultRuntimeAdmission();
        }
        catch
        {
            FaultRuntimeAdmission();
        }
        finally
        {
            if (ReferenceEquals(activeP12MerchantOperationContext, context))
                activeP12MerchantOperationContext = null;
            SimulationOperationScope operation = context.ProtocolScope;
            context.ProtocolScope = null;
            operation?.Dispose();
        }
    }

    private void NotifyP12NpcOwnerMutation(P12NpcOwnerMutationBinding binding)
    {
        if (runtimeAdmissionContext == null || npcRosterCensusProtocol == null) return;
        try
        {
            bool current = binding != null
                && ((binding.Owner is MoneyAccountRuntime account
                        && npcMoneyAccountMutationBindings.TryGetValue(account, out P12NpcOwnerMutationBinding accountBinding)
                        && ReferenceEquals(accountBinding, binding))
                    || (binding.Owner is InventoryRuntime inventory
                        && npcInventoryMutationBindings.TryGetValue(inventory, out P12NpcOwnerMutationBinding inventoryBinding)
                        && ReferenceEquals(inventoryBinding, binding)));
            bool accepted = current && NotifyP12MutationSections(binding.SectionIds);
            if (!accepted) npcRosterCensusProtocol.FaultClosed();
        }
        catch
        {
            npcRosterCensusProtocol.FaultClosed();
        }
    }

    private bool ValidateP12CurrentActionReferences(NpcRuntime owner, NpcActionRuntime actionRuntime)
    {
        if (owner == null
            || actionRuntime == null
            || !npcRegistryById.TryGetValue(owner.RuntimeId ?? string.Empty, out NpcRuntime registeredOwner)
            || !ReferenceEquals(registeredOwner, owner)
            || actionRuntime.Action == null
            || string.IsNullOrWhiteSpace(actionRuntime.Action.DefinitionId))
            return false;

        NpcRuntime targetNpc = actionRuntime.TargetNpc;
        if (targetNpc != null
            && (string.IsNullOrWhiteSpace(targetNpc.RuntimeId)
                || !npcRegistryById.TryGetValue(targetNpc.RuntimeId, out NpcRuntime registeredTarget)
                || !ReferenceEquals(registeredTarget, targetNpc)))
            return false;

        CityRuntime targetCity = actionRuntime.TargetCity;
        if (targetCity != null
            && (string.IsNullOrWhiteSpace(targetCity.RuntimeId)
                || !cities.Exists(city => ReferenceEquals(city, targetCity))))
            return false;

        ItemData targetItem = actionRuntime.TargetItem;
        if (targetItem == null) return true;
        string targetItemId = targetItem.DefinitionId;
        if (string.IsNullOrWhiteSpace(targetItemId)) return false;

        Dictionary<string, ItemData> admittedItems = new Dictionary<string, ItemData>(StringComparer.Ordinal);
        HashSet<string> ambiguousItemIds = new HashSet<string>(StringComparer.Ordinal);
        Action<ItemData> includeItem = item =>
        {
            if (item == null || string.IsNullOrWhiteSpace(item.DefinitionId)) return;
            if (admittedItems.TryGetValue(item.DefinitionId, out ItemData existing)
                && !ReferenceEquals(existing, item))
                ambiguousItemIds.Add(item.DefinitionId);
            else
                admittedItems[item.DefinitionId] = item;
        };

        foreach (CityRuntime city in cities)
            foreach (MarketItemRuntime marketItem in city?.Market?.Items ?? Array.Empty<MarketItemRuntime>())
                includeItem(marketItem?.Item);
        foreach (NpcRuntime npc in npcRuntimeSnapshot)
            foreach (InventoryItemRuntime inventoryItem in npc?.ExistingInventory?.Items ?? Array.Empty<InventoryItemRuntime>())
                includeItem(inventoryItem?.Item);

        return !ambiguousItemIds.Contains(targetItemId)
            && admittedItems.TryGetValue(targetItemId, out ItemData admittedItem)
            && ReferenceEquals(admittedItem, targetItem);
    }

    /// <summary>Assesses this partial passive NPC/Person census only.</summary>
    public bool TryAssessNpcRosterCensus(out ContinuationCensusFailure failure)
    {
        if (npcRosterCensusProtocol == null)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        return npcRosterCensusProtocol.TryAssessOwnerSectionInventory(out failure);
    }

    /// <summary>Reads the shared epoch for this partial passive NPC/Person census only.</summary>
    public bool TryReadNpcRosterCensusMutationEpoch(
        out long epoch,
        out ContinuationCensusFailure failure)
    {
        epoch = 0L;
        if (npcRosterCensusProtocol == null)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }
        return npcRosterCensusProtocol.TryReadMutationEpoch(out epoch, out failure);
    }

    internal bool HasSameP12DNpcReceiptOwnerCensusProviders(
        IReadOnlyList<IOwnerSectionCensusProvider> otherProviders)
    {
        if (otherProviders == null) return false;
        if (runtimeAdmissionContext == null) return otherProviders.Count == 0;
        if (npcRosterCensusProtocol == null) return false;

        IReadOnlyList<IOwnerSectionCensusProvider> installedProviders =
            npcRosterCensusProtocol.NpcReceiptOwnerFamilyProviders;
        return ReferenceEquals(installedProviders, otherProviders)
            && P12DNpcReceiptOwnerCensusProvider.IsExactCoverage(
                npcRuntimeSnapshot,
                installedProviders);
    }

    internal bool HasSameSimulationRecordSequenceOwner(IOwnerSectionCensusProvider otherProvider)
    {
        if (runtimeAdmissionContext == null) return true;
        if (simulationRecordSequenceCensusProvider == null || otherProvider == null) return false;

        try
        {
            OwnerSectionCensusWitness runtimeWitness = simulationRecordSequenceCensusProvider.GetCurrentCensus();
            OwnerSectionCensusWitness otherWitness = otherProvider.GetCurrentCensus();
            return runtimeWitness != null
                && otherWitness != null
                && string.Equals(runtimeWitness.SectionId, SimulationRecordSequenceCensusProvider.SectionId, StringComparison.Ordinal)
                && string.Equals(otherWitness.SectionId, runtimeWitness.SectionId, StringComparison.Ordinal)
                && otherWitness.SchemaVersion == runtimeWitness.SchemaVersion
                && otherWitness.Cardinality == 1
                && runtimeWitness.Cardinality == 1
                && ReferenceEquals(otherWitness.OwnerInstanceIdentity, runtimeWitness.OwnerInstanceIdentity)
                && otherWitness.Revision == runtimeWitness.Revision;
        }
        catch
        {
            return false;
        }
    }

    internal bool HasSameScheduledDirectiveOwner(IOwnerSectionCensusProvider otherProvider)
    {
        if (runtimeAdmissionContext == null) return true;
        if (scheduledDirectiveCensusProvider == null || otherProvider == null) return false;

        try
        {
            OwnerSectionCensusWitness runtimeWitness = scheduledDirectiveCensusProvider.GetCurrentCensus();
            OwnerSectionCensusWitness otherWitness = otherProvider.GetCurrentCensus();
            ScheduledDirectiveStore installedStore = scheduledDirectiveSystem?.Store;
            return runtimeWitness != null
                && otherWitness != null
                && installedStore != null
                && string.Equals(
                    runtimeWitness.SectionId,
                    ScheduledDirectiveCensusProvider.SectionId,
                    StringComparison.Ordinal)
                && string.Equals(otherWitness.SectionId, runtimeWitness.SectionId, StringComparison.Ordinal)
                && runtimeWitness.SchemaVersion == ScheduledDirectiveCensusProvider.SchemaVersion
                && otherWitness.SchemaVersion == runtimeWitness.SchemaVersion
                && runtimeWitness.Cardinality >= 0
                && otherWitness.Cardinality == runtimeWitness.Cardinality
                && ReferenceEquals(runtimeWitness.OwnerInstanceIdentity, installedStore)
                && ReferenceEquals(otherWitness.OwnerInstanceIdentity, runtimeWitness.OwnerInstanceIdentity)
                && otherWitness.Revision == runtimeWitness.Revision;
        }
        catch
        {
            return false;
        }
    }

    internal bool HasSameRuntimeIdAllocatorEventCounterOwner(IOwnerSectionCensusProvider otherProvider)
    {
        if (runtimeAdmissionContext == null) return true;
        if (runtimeIdAllocatorEventCounterCensusProvider == null || otherProvider == null) return false;

        try
        {
            OwnerSectionCensusWitness runtimeWitness = runtimeIdAllocatorEventCounterCensusProvider.GetCurrentCensus();
            OwnerSectionCensusWitness otherWitness = otherProvider.GetCurrentCensus();
            return runtimeWitness != null
                && otherWitness != null
                && string.Equals(runtimeWitness.SectionId, RuntimeIdAllocatorCensusProvider.EventsSectionId, StringComparison.Ordinal)
                && string.Equals(otherWitness.SectionId, runtimeWitness.SectionId, StringComparison.Ordinal)
                && otherWitness.SchemaVersion == runtimeWitness.SchemaVersion
                && otherWitness.Cardinality == 1
                && runtimeWitness.Cardinality == 1
                && ReferenceEquals(otherWitness.OwnerInstanceIdentity, runtimeWitness.OwnerInstanceIdentity)
                && otherWitness.Revision == runtimeWitness.Revision;
        }
        catch
        {
            return false;
        }
    }

    internal bool HasSameRuntimeIdAllocatorDecisionCounterOwner(IOwnerSectionCensusProvider otherProvider)
    {
        if (runtimeAdmissionContext == null) return true;
        if (runtimeIdAllocatorDecisionCounterCensusProvider == null || otherProvider == null) return false;

        try
        {
            OwnerSectionCensusWitness runtimeWitness = runtimeIdAllocatorDecisionCounterCensusProvider.GetCurrentCensus();
            OwnerSectionCensusWitness otherWitness = otherProvider.GetCurrentCensus();
            return runtimeWitness != null
                && otherWitness != null
                && string.Equals(runtimeWitness.SectionId, RuntimeIdAllocatorCensusProvider.DecisionsSectionId, StringComparison.Ordinal)
                && string.Equals(otherWitness.SectionId, runtimeWitness.SectionId, StringComparison.Ordinal)
                && otherWitness.SchemaVersion == runtimeWitness.SchemaVersion
                && otherWitness.Cardinality == 1
                && runtimeWitness.Cardinality == 1
                && ReferenceEquals(otherWitness.OwnerInstanceIdentity, runtimeWitness.OwnerInstanceIdentity)
                && otherWitness.Revision == runtimeWitness.Revision;
        }
        catch
        {
            return false;
        }
    }

    internal bool HasSameActorChoiceP11Owner(IOwnerSectionCensusProvider otherProvider)
    {
        if (runtimeAdmissionContext == null) return true;
        if (actorChoiceP11CensusProvider == null || actorChoiceStore == null || otherProvider == null)
            return false;

        try
        {
            OwnerSectionCensusWitness runtimeWitness = actorChoiceP11CensusProvider.GetCurrentCensus();
            OwnerSectionCensusWitness otherWitness = otherProvider.GetCurrentCensus();
            return runtimeWitness != null
                && otherWitness != null
                && string.Equals(
                    runtimeWitness.SectionId,
                    ActorChoiceP11CensusProvider.SectionId,
                    StringComparison.Ordinal)
                && string.Equals(otherWitness.SectionId, runtimeWitness.SectionId, StringComparison.Ordinal)
                && runtimeWitness.SchemaVersion == ActorChoiceP11CensusProvider.SchemaVersion
                && otherWitness.SchemaVersion == runtimeWitness.SchemaVersion
                && runtimeWitness.Cardinality == actorChoiceStore.P11InputCount
                && otherWitness.Cardinality == runtimeWitness.Cardinality
                && ReferenceEquals(runtimeWitness.OwnerInstanceIdentity, actorChoiceStore.CensusOwnerIdentity)
                && ReferenceEquals(otherWitness.OwnerInstanceIdentity, runtimeWitness.OwnerInstanceIdentity)
                && runtimeWitness.Revision == actorChoiceStore.CensusRevision
                && otherWitness.Revision == runtimeWitness.Revision;
        }
        catch
        {
            return false;
        }
    }

    private bool CanCommitP12SimulationRecordSequenceMutation()
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || simulationRecordSequenceCensusProvider == null
            || !CanCommitP12MutationSections(
                new[] { SimulationRecordSequenceCensusProvider.SectionId }))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private void NotifyP12SimulationRecordSequenceMutation()
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (npcRosterCensusProtocol == null
                || !NotifyP12MutationSections(
                    new[] { SimulationRecordSequenceCensusProvider.SectionId }))
            {
                FaultRuntimeAdmission();
                throw new InvalidOperationException(
                    "The committed P12 record-sequence allocation could not advance the mutation epoch.");
            }
        }
        catch (InvalidOperationException)
        {
            FaultRuntimeAdmission();
            throw;
        }
        catch (Exception exception)
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException(
                "The committed P12 record-sequence allocation could not be reported to its census protocol.",
                exception);
        }
    }

    private bool CanCommitP12RuntimeIdEventCounterMutation()
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || runtimeIdAllocatorEventCounterCensusProvider == null
            || !CanCommitP12MutationSections(
                new[] { RuntimeIdAllocatorCensusProvider.EventsSectionId }))
        {
            FaultRuntimeAdmission();
            return false;
        }

        if (!npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private void NotifyP12RuntimeIdEventCounterMutation()
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (npcRosterCensusProtocol == null
                || !NotifyP12MutationSections(
                    new[] { RuntimeIdAllocatorCensusProvider.EventsSectionId }))
            {
                FaultRuntimeAdmission();
                throw new InvalidOperationException(
                    "The committed P12 Event-ID allocation could not advance the mutation epoch.");
            }
        }
        catch (InvalidOperationException)
        {
            FaultRuntimeAdmission();
            throw;
        }
        catch (Exception exception)
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException(
                "The committed P12 Event-ID allocation could not be reported to its census protocol.",
                exception);
        }
    }

    private bool CanCommitP12RuntimeIdDecisionCounterMutation()
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || runtimeIdAllocatorDecisionCounterCensusProvider == null
            || !CanCommitP12MutationSections(
                new[] { RuntimeIdAllocatorCensusProvider.DecisionsSectionId }))
        {
            FaultRuntimeAdmission();
            return false;
        }

        if (!npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private bool CanCommitP12ScheduledDirectiveMutation()
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || scheduledDirectiveSystem?.Store == null
            || scheduledDirectiveCensusProvider == null
            || !IsCurrentP12ScheduledDirectiveOwner()
            || !CanCommitP12MutationSections(
                new[] { ScheduledDirectiveCensusProvider.SectionId }))
        {
            FaultRuntimeAdmission();
            return false;
        }

        bool hasReservedBatchEpoch = activeP12TravelPartyAdvanceOperationContext != null
            || activeP12MerchantOperationContext != null
            || activeP12SoloTravelStartOperationContext != null;
        if (!hasReservedBatchEpoch
            && !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private bool IsCurrentP12ScheduledDirectiveOwner()
    {
        ScheduledDirectiveStore store = scheduledDirectiveSystem?.Store;
        if (store == null || scheduledDirectiveCensusProvider == null) return false;

        try
        {
            OwnerSectionCensusWitness witness = scheduledDirectiveCensusProvider.GetCurrentCensus();
            return witness != null
                && string.Equals(
                    witness.SectionId,
                    ScheduledDirectiveCensusProvider.SectionId,
                    StringComparison.Ordinal)
                && witness.SchemaVersion == ScheduledDirectiveCensusProvider.SchemaVersion
                && witness.Cardinality >= 0
                && ReferenceEquals(witness.OwnerInstanceIdentity, store)
                && witness.Revision == store.Revision;
        }
        catch
        {
            return false;
        }
    }

    private void NotifyP12ScheduledDirectiveMutation()
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (!IsRuntimeAdmissionOwnerThreadCurrent()
                || npcRosterCensusProtocol == null
                || !IsCurrentP12ScheduledDirectiveOwner()
                || !NotifyP12MutationSections(
                    new[] { ScheduledDirectiveCensusProvider.SectionId }))
            {
                FaultRuntimeAdmission();
                throw new InvalidOperationException(
                    "The committed P12 ScheduledDirective mutation could not advance its census epoch.");
            }
        }
        catch (InvalidOperationException)
        {
            FaultRuntimeAdmission();
            throw;
        }
        catch (Exception exception)
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException(
                "The committed P12 ScheduledDirective mutation could not be reported to its census protocol.",
                exception);
        }
    }

    private bool CanCommitP12ActorChoiceMutation()
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || actorChoiceStore == null
            || actorChoiceP11CensusProvider == null
            || !IsCurrentP12ActorChoiceP11Owner()
            || !CanCommitP12MutationSections(
                new[] { ActorChoiceP11CensusProvider.SectionId }))
        {
            FaultRuntimeAdmission();
            return false;
        }

        bool hasReservedBatchEpoch = activeP12TravelPartyAdvanceOperationContext != null
            || activeP12MerchantOperationContext != null
            || activeP12SoloTravelStartOperationContext != null;
        if (!hasReservedBatchEpoch
            && !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private bool IsCurrentP12ActorChoiceP11Owner()
    {
        try
        {
            OwnerSectionCensusWitness witness = actorChoiceP11CensusProvider?.GetCurrentCensus();
            return witness != null
                && string.Equals(
                    witness.SectionId,
                    ActorChoiceP11CensusProvider.SectionId,
                    StringComparison.Ordinal)
                && witness.SchemaVersion == ActorChoiceP11CensusProvider.SchemaVersion
                && witness.Cardinality >= 0
                && witness.Cardinality == actorChoiceStore.P11InputCount
                && ReferenceEquals(witness.OwnerInstanceIdentity, actorChoiceStore.CensusOwnerIdentity)
                && witness.Revision == actorChoiceStore.CensusRevision;
        }
        catch
        {
            return false;
        }
    }

    private void NotifyP12ActorChoiceMutation()
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (!IsRuntimeAdmissionOwnerThreadCurrent()
                || npcRosterCensusProtocol == null
                || !IsCurrentP12ActorChoiceP11Owner()
                || !NotifyP12MutationSections(
                    new[] { ActorChoiceP11CensusProvider.SectionId }))
            {
                FaultRuntimeAdmission();
                throw new InvalidOperationException(
                    "The committed P12 ActorChoice mutation could not advance its census epoch.");
            }
        }
        catch (InvalidOperationException)
        {
            FaultRuntimeAdmission();
            throw;
        }
        catch (Exception exception)
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException(
                "The committed P12 ActorChoice mutation could not be reported to its census protocol.",
                exception);
        }
    }

    private void NotifyP12RuntimeIdDecisionCounterMutation()
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (npcRosterCensusProtocol == null
                || !NotifyP12MutationSections(
                    new[] { RuntimeIdAllocatorCensusProvider.DecisionsSectionId }))
            {
                FaultRuntimeAdmission();
                throw new InvalidOperationException(
                    "The committed P12 Decision-ID allocation could not advance the mutation epoch.");
            }
        }
        catch (InvalidOperationException)
        {
            FaultRuntimeAdmission();
            throw;
        }
        catch (Exception exception)
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException(
                "The committed P12 Decision-ID allocation could not be reported to its census protocol.",
                exception);
        }
    }

    internal bool TryBeginBootstrapPublicationScope(out SimulationOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null)
            return false;

        return TryEnterRuntimeAdmissionOperation(
            DailyV1RuntimeProtocolOperationContracts.BootstrapPublication,
            out scope);
    }

    internal bool HasNpcTradeCensusAdapter => runtimeAdmissionContext != null;
    internal bool HasP12MarketOperationAdapter => runtimeAdmissionContext != null;

    internal void BindP12EconomyTransactionService(EconomyTransactionService service)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        service.BindP12CensusRuntime(this);
        if (runtimeAdmissionContext == null) return;
        if (npcRosterCensusProtocol == null)
            throw new InvalidOperationException("The P12 Market owner census is unavailable.");

        foreach (MarketRuntime market in marketSectionIdsByOwner.Keys)
            market.BindP12TransactionService(service);
    }

    internal bool TryBeginP12MarketOperation(
        NpcRuntime npc,
        MarketRuntime market,
        EconomyTransactionType transactionType,
        out SimulationOperationScope scope,
        out string accountSectionId,
        out string inventorySectionId,
        out string marketSectionId)
    {
        scope = null;
        accountSectionId = null;
        inventorySectionId = null;
        marketSectionId = null;
        if (runtimeAdmissionContext == null) return false;

        string operationContractId = transactionType == EconomyTransactionType.OpenMarketPurchase
            ? DailyV1RuntimeProtocolOperationContracts.MarketPurchase
            : transactionType == EconomyTransactionType.OpenMarketSale
                ? DailyV1RuntimeProtocolOperationContracts.MarketSale
                : null;

        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || string.IsNullOrWhiteSpace(operationContractId)
            || !TryResolveNpcTradeParticipantSections(npc, out accountSectionId, out inventorySectionId)
            || market == null
            || !marketSectionIdsByOwner.TryGetValue(market, out marketSectionId))
        {
            FaultRuntimeAdmission();
            return false;
        }

        if (!npcRosterCensusProtocol.TryValidateUnchangedSections(
                new[] { accountSectionId, inventorySectionId, marketSectionId },
                out _))
        {
            FaultRuntimeAdmission();
            return false;
        }

        if (!TryEnterRuntimeAdmissionOperation(operationContractId, out scope))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private bool CanCommitP12MarketOwnerMutation(MarketRuntime market, string sectionId)
    {
        if (runtimeAdmissionContext == null) return true;
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || market == null
            || !marketSectionIdsByOwner.TryGetValue(market, out string registeredSectionId)
            || !string.Equals(registeredSectionId, sectionId, StringComparison.Ordinal)
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(new[] { sectionId }, out _))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private void NotifyP12MarketOwnerMutation(string sectionId)
    {
        if (runtimeAdmissionContext == null || npcRosterCensusProtocol == null) return;
        try
        {
            if (!npcRosterCensusProtocol.NotifyCommittedMutation(sectionId, out _))
                npcRosterCensusProtocol.FaultClosed();
        }
        catch
        {
            npcRosterCensusProtocol.FaultClosed();
        }
    }

    internal void NotifyP12MarketOperationOwnerMutation(string sectionId)
    {
        NotifyP12MarketOwnerMutation(sectionId);
    }

    internal bool TryBeginNpcTradeCensusOperation(
        NpcRuntime buyer,
        NpcRuntime seller,
        out SimulationOperationScope scope,
        out string buyerAccountSectionId,
        out string sellerAccountSectionId,
        out string buyerInventorySectionId,
        out string sellerInventorySectionId)
    {
        scope = null;
        buyerAccountSectionId = null;
        sellerAccountSectionId = null;
        buyerInventorySectionId = null;
        sellerInventorySectionId = null;
        if (runtimeAdmissionContext == null) return false;

        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || !TryResolveNpcTradeParticipantSections(
                buyer,
                out buyerAccountSectionId,
                out buyerInventorySectionId)
            || !TryResolveNpcTradeParticipantSections(
                seller,
                out sellerAccountSectionId,
                out sellerInventorySectionId))
        {
            FaultRuntimeAdmission();
            return false;
        }

        if (!npcRosterCensusProtocol.TryValidateUnchangedSections(
                new[]
                {
                    buyerAccountSectionId,
                    sellerAccountSectionId,
                    buyerInventorySectionId,
                    sellerInventorySectionId
                },
                out _))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return TryEnterRuntimeAdmissionOperation(DailyV1RuntimeProtocolOperationContracts.NpcTrade, out scope);
    }

    internal bool TryBeginNpcMoneyTransferCensusOperation(
        NpcRuntime source,
        NpcRuntime destination,
        out SimulationOperationScope scope,
        out string sourceAccountSectionId,
        out string destinationAccountSectionId)
    {
        scope = null;
        sourceAccountSectionId = null;
        destinationAccountSectionId = null;
        if (runtimeAdmissionContext == null) return false;

        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || !TryResolveNpcMoneyAccountSection(source, out sourceAccountSectionId)
            || !TryResolveNpcMoneyAccountSection(destination, out destinationAccountSectionId))
        {
            FaultRuntimeAdmission();
            return false;
        }

        if (!npcRosterCensusProtocol.TryValidateUnchangedSections(
                new[] { sourceAccountSectionId, destinationAccountSectionId },
                out _))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return TryEnterRuntimeAdmissionOperation(DailyV1RuntimeProtocolOperationContracts.NpcMoneyTransfer, out scope);
    }

    internal void NotifyNpcTradeOwnerMutation(string sectionId)
    {
        if (runtimeAdmissionContext == null || npcRosterCensusProtocol == null) return;
        try
        {
            if (!npcRosterCensusProtocol.NotifyCommittedMutation(sectionId, out _))
                npcRosterCensusProtocol.FaultClosed();
        }
        catch
        {
            npcRosterCensusProtocol.FaultClosed();
        }
    }

    private bool TryResolveNpcTradeParticipantSections(
        NpcRuntime npc,
        out string accountSectionId,
        out string inventorySectionId)
    {
        accountSectionId = null;
        inventorySectionId = null;
        if (!TryResolveNpcMoneyAccountSection(npc, out accountSectionId)) return false;
        if (npc == null
            || npc.ExistingInventory == null)
        {
            accountSectionId = null;
            return false;
        }

        string expectedInventorySectionId = NpcInventoryCensusProvider.SectionPrefix + npc.RuntimeId;
        bool inventoryFound = false;
        try
        {
            foreach (IOwnerSectionCensusProvider provider in InventoryCensusProviders)
            {
                if (!(provider is NpcInventoryCensusProvider.INpcInventorySectionCensusProvider inventoryProvider)
                    || !string.Equals(inventoryProvider.RuntimeId, npc.RuntimeId, StringComparison.Ordinal))
                {
                    continue;
                }

                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                if (!ReferenceEquals(inventoryProvider.NpcOwner, npc)
                    || !ReferenceEquals(inventoryProvider.InventoryOwner, npc.ExistingInventory)
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, npc.ExistingInventory)
                    || !string.Equals(witness.SectionId, expectedInventorySectionId, StringComparison.Ordinal)
                    || witness.SchemaVersion != NpcInventoryCensusProvider.SchemaVersion
                    || witness.Revision != npc.ExistingInventory.Revision)
                {
                    return false;
                }

                inventoryFound = true;
            }
        }
        catch
        {
            accountSectionId = null;
            return false;
        }

        if (!inventoryFound)
        {
            accountSectionId = null;
            return false;
        }
        inventorySectionId = expectedInventorySectionId;
        return true;
    }

    private bool TryResolveNpcMoneyAccountSection(NpcRuntime npc, out string accountSectionId)
    {
        accountSectionId = null;
        if (npc == null
            || string.IsNullOrWhiteSpace(npc.RuntimeId)
            || !npcRegistryById.TryGetValue(npc.RuntimeId, out NpcRuntime registeredNpc)
            || !ReferenceEquals(registeredNpc, npc)
            || npc.MoneyAccount == null)
        {
            return false;
        }

        string expectedAccountSectionId = NpcMoneyAccountCensusProvider.SectionPrefix + npc.RuntimeId;
        bool accountFound = false;
        try
        {
            foreach (IOwnerSectionCensusProvider provider in MoneyAccountCensusProviders)
            {
                if (!(provider is NpcMoneyAccountCensusProvider.INpcMoneyAccountSectionCensusProvider accountProvider)
                    || !string.Equals(accountProvider.RuntimeId, npc.RuntimeId, StringComparison.Ordinal))
                {
                    continue;
                }

                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                if (accountFound
                    || !ReferenceEquals(accountProvider.NpcOwner, npc)
                    || !ReferenceEquals(accountProvider.MoneyAccountOwner, npc.MoneyAccount)
                    || !ReferenceEquals(witness.OwnerInstanceIdentity, npc.MoneyAccount)
                    || !string.Equals(witness.SectionId, expectedAccountSectionId, StringComparison.Ordinal)
                    || witness.SchemaVersion != NpcMoneyAccountCensusProvider.SchemaVersion
                    || witness.Cardinality != 1
                    || witness.Revision != npc.MoneyAccount.Revision)
                {
                    return false;
                }

                accountFound = true;
            }
        }
        catch
        {
            return false;
        }

        if (!accountFound) return false;
        accountSectionId = expectedAccountSectionId;
        return true;
    }

    internal bool IsRuntimeAdmissionOwnerThreadCurrent()
    {
        if (runtimeAdmissionContext == null)
            return true;

        if (runtimeAdmissionContext.IsOwnedByCurrentThread())
            return true;

        npcRosterCensusProtocol?.FaultClosed();
        return false;
    }

    internal void FaultRuntimeAdmission()
    {
        npcRosterCensusProtocol?.FaultClosed();
    }

    private bool TryBeginP12InstitutionOfficeOwnerCommit(
        IEnumerable<string> sectionIds,
        out SimulationOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null) return true;

        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(sectionIds, out _)
            || !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _)
            || !TryEnterRuntimeAdmissionOperation(
                DailyV1RuntimeProtocolOperationContracts.InstitutionOfficeOwnerCommit,
                out scope))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private bool TryBeginP12GenealogyOwnerCommit(out SimulationOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null) return true;

        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(P12GenealogyOwnerSectionIds, out _)
            || !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _)
            || !TryEnterRuntimeAdmissionOperation(DailyV1RuntimeProtocolOperationContracts.PersonParentage, out scope))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private void NotifyP12GenealogyOwnerCommit()
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (npcRosterCensusProtocol == null
                || !NotifyP12MutationSections(P12GenealogyOwnerSectionIds))
                FaultRuntimeAdmission();
        }
        catch
        {
            FaultRuntimeAdmission();
        }
    }

    private bool TryBeginP12PropertyEstateOwnerCommit(
        string operationContractId,
        IEnumerable<string> sectionIds,
        out SimulationOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null) return true;

        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(sectionIds, out _)
            || !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _)
            || !TryEnterRuntimeAdmissionOperation(operationContractId, out scope))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private void NotifyP12PropertyEstateOwnerCommit(IEnumerable<string> sectionIds)
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (npcRosterCensusProtocol == null || !NotifyP12MutationSections(sectionIds))
                FaultRuntimeAdmission();
        }
        catch
        {
            FaultRuntimeAdmission();
        }
    }

    private void NotifyP12InstitutionOfficeOwnerCommit(IEnumerable<string> sectionIds)
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (npcRosterCensusProtocol == null || !NotifyP12MutationSections(sectionIds))
                FaultRuntimeAdmission();
        }
        catch
        {
            FaultRuntimeAdmission();
        }
    }

    private bool TryBeginP12FactionStoreOwnerCommit(out SimulationOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null) return true;

        string[] sectionIds =
        {
            FactionStoreCensusProvider.FactionsSectionId,
            FactionStoreCensusProvider.AffiliationsSectionId
        };
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(sectionIds, out _)
            || !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _)
            || !TryEnterRuntimeAdmissionOperation(
                DailyV1RuntimeProtocolOperationContracts.FactionStoreOwnerCommit,
                out scope))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private void NotifyP12FactionStoreOwnerCommit()
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (npcRosterCensusProtocol == null
                || !NotifyP12MutationSections(new[]
                {
                    FactionStoreCensusProvider.FactionsSectionId,
                    FactionStoreCensusProvider.AffiliationsSectionId
                }))
                FaultRuntimeAdmission();
        }
        catch
        {
            FaultRuntimeAdmission();
        }
    }

    private bool TryBeginP12PoliticalClaimOwnerCommit(out SimulationOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null) return true;

        string[] sectionIds =
        {
            PoliticalClaimStoreCensusProvider.ClaimsSectionId,
            PoliticalClaimStoreCensusProvider.RecognitionsSectionId
        };
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(sectionIds, out _)
            || !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _)
            || !TryEnterRuntimeAdmissionOperation(
                DailyV1RuntimeProtocolOperationContracts.PoliticalClaimOwnerCommit,
                out scope))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private void NotifyP12PoliticalClaimOwnerCommit()
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (npcRosterCensusProtocol == null
                || !NotifyP12MutationSections(new[]
                {
                    PoliticalClaimStoreCensusProvider.ClaimsSectionId,
                    PoliticalClaimStoreCensusProvider.RecognitionsSectionId
                }))
                FaultRuntimeAdmission();
        }
        catch
        {
            FaultRuntimeAdmission();
        }
    }

    private bool TryBeginP12PoliticalSupportOwnerCommit(out SimulationOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null) return true;

        string[] sectionIds =
        {
            PoliticalSupportStoreCensusProvider.RelationsSectionId
        };
        if (!IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || !npcRosterCensusProtocol.TryValidateUnchangedSections(sectionIds, out _)
            || !npcRosterCensusProtocol.TryValidateMutationEpochCapacity(out _)
            || !TryEnterRuntimeAdmissionOperation(
                DailyV1RuntimeProtocolOperationContracts.PoliticalSupportOwnerCommit,
                out scope))
        {
            FaultRuntimeAdmission();
            return false;
        }

        return true;
    }

    private void NotifyP12PoliticalSupportOwnerCommit()
    {
        if (runtimeAdmissionContext == null) return;
        try
        {
            if (npcRosterCensusProtocol == null
                || !NotifyP12MutationSections(new[]
                {
                    PoliticalSupportStoreCensusProvider.RelationsSectionId
                }))
                FaultRuntimeAdmission();
        }
        catch
        {
            FaultRuntimeAdmission();
        }
    }

    private bool TryEnterRuntimeAdmissionOperation(
        string operationContractId,
        out SimulationOperationScope scope)
    {
        scope = null;
        if (runtimeAdmissionContext == null
            || !IsRuntimeAdmissionOwnerThreadCurrent()
            || npcRosterCensusProtocol == null
            || !npcRosterCensusProtocol.TryEnterOperation(
                operationContractId,
                out scope,
                out _))
        {
            npcRosterCensusProtocol?.FaultClosed();
            return false;
        }

        return true;
    }

    private bool TryAdvanceFromRuntimeOwnedClock(out SimulationTimeAdvanceFailure failure)
    {
        if (TryAdvanceDay(out SimulationRuntimeAdvanceFailure runtimeFailure))
        {
            failure = SimulationTimeAdvanceFailure.None;
            return true;
        }

        failure = runtimeFailure == SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow
            ? SimulationTimeAdvanceFailure.AbsoluteDayOverflow
            : SimulationTimeAdvanceFailure.RuntimeFaulted;
        return false;
    }

    private NpcMembershipCensusScope BeginNpcMembershipCensusScope() =>
        BeginNpcMembershipCensusScopeCore(null);

    private NpcMembershipCensusScope BeginNpcMembershipCensusScopeWithLifecycleSections(
        IEnumerable<string> allowedLifecycleSectionIds) =>
        BeginNpcMembershipCensusScopeCore(allowedLifecycleSectionIds);

    private NpcMembershipCensusScope BeginNpcMembershipCensusScopeCore(
        IEnumerable<string> allowedLifecycleSectionIds)
    {
        NpcMembershipCensusContext activeContext = activeNpcMembershipCensusContext;
        if (activeContext != null)
        {
            if (!activeContext.IsOwnedByCurrentThread())
            {
                FaultNpcMembershipCensusBoundary();
                return new NpcMembershipCensusScope(this, null);
            }

            if (allowedLifecycleSectionIds != null)
            {
                foreach (string sectionId in allowedLifecycleSectionIds)
                {
                    if (string.IsNullOrWhiteSpace(sectionId)
                        || !activeContext.AllowedLifecycleSectionIds.Contains(sectionId))
                    {
                        FaultNpcMembershipCensusBoundary();
                        return new NpcMembershipCensusScope(this, null);
                    }
                }
            }

            activeContext.NestingDepth++;
            return new NpcMembershipCensusScope(this, activeContext);
        }

        NpcMembershipCensusContext context = new NpcMembershipCensusContext(
            personStore.Revision,
            Thread.CurrentThread,
            allowedLifecycleSectionIds);
        if (npcRosterCensusProtocol != null
            && TryAssessP12LifecycleOwnerRoster()
            && TryValidateP12RuntimeIdentitySpatialOwnerBaselines()
            && npcRosterCensusProtocol.TryEnterOperation(
                DailyV1RuntimeProtocolOperationContracts.NpcMembership,
                out SimulationOperationScope protocolScope,
                out _))
        {
            context.ProtocolScope = protocolScope;
            if (!npcRosterCensusProtocol.TryReserveMutationEpochCapacity(
                    out ContinuationMutationEpochReservation reservation,
                    out _))
            {
                npcRosterCensusProtocol.FaultClosed();
                context.ProtocolScope.Dispose();
                context.ProtocolScope = null;
            }
            else
            {
                context.EpochReservation = reservation;
            }
        }
        else if (npcRosterCensusProtocol != null)
        {
            npcRosterCensusProtocol.FaultClosed();
        }
        activeNpcMembershipCensusContext = context;
        return new NpcMembershipCensusScope(this, context);
    }

    private void ExitNpcMembershipCensusScope(NpcMembershipCensusContext context)
    {
        if (context == null) return;
        if (!context.IsOwnedByCurrentThread())
        {
            FaultNpcMembershipCensusBoundary();
            return;
        }

        if (!ReferenceEquals(activeNpcMembershipCensusContext, context)
            || context.NestingDepth <= 0)
        {
            return;
        }

        context.NestingDepth--;
        if (context.NestingDepth != 0) return;

        long personStoreRevision = personStore.Revision;
        bool revisionDeltaFits = context.PersonStoreRevisionDelta >= 0L
            && context.PersonStoreRevisionAtStart <= long.MaxValue - context.PersonStoreRevisionDelta;
        long expectedPersonStoreRevision = revisionDeltaFits
            ? context.PersonStoreRevisionAtStart + context.PersonStoreRevisionDelta
            : -1L;
        if (!revisionDeltaFits || personStoreRevision != expectedPersonStoreRevision)
        {
            npcRosterCensusProtocol?.FaultClosed();
            if (context.EpochReservation != null)
                npcRosterCensusProtocol?.ReleaseMutationEpochReservation(context.EpochReservation);
        }
        else if (context.RosterChanged
            || context.PersonStoreRevisionDelta != 0L
            || context.ChangedCityPresenceSectionIds.Count != 0
            || context.ChangedRuntimeIdentitySectionIds.Count != 0
            || context.ChangedLifecycleSectionIds.Count != 0)
        {
            List<string> changedFixedSections = new List<string>();
            if (context.PersonStoreRevisionDelta != 0L)
            {
                changedFixedSections.Add(PersonMembershipCensusProvider.SectionId);
                changedFixedSections.Add(PersonMaterializationBindingCensusProvider.SectionId);
            }
            changedFixedSections.AddRange(context.ChangedCityPresenceSectionIds);
            changedFixedSections.AddRange(context.ChangedRuntimeIdentitySectionIds);
            changedFixedSections.AddRange(context.ChangedLifecycleSectionIds);

            if (context.ProtocolScope != null && npcRosterCensusProtocol != null)
            {
                List<IOwnerSectionCensusProvider> lifecycleProviders = new List<IOwnerSectionCensusProvider>();
                if (runtimeAdmissionContext != null)
                {
                    lifecycleProviders.AddRange(
                        PersonLifeResidenceCensusProvider.CreateProviders(personStore.Persons));
                    lifecycleProviders.AddRange(
                        NpcLifecycleCensusProvider.CreateProviders(npcRuntimeSnapshot));
                    lifecycleProviders.AddRange(
                        P12CrimeJusticeCensusProvider.CreateNpcStatusProviders(npcRuntimeSnapshot));
                    lifecycleProviders.AddRange(
                        NpcCurrentActionCensusProvider.CreateProviders(
                            npcRuntimeSnapshot,
                            ValidateP12CurrentActionReferences));
                }
                bool reconciled = npcRosterCensusProtocol.TryReconcileSpatialKnowledgeRosterAndNotifyCommittedMutations(
                    changedFixedSections,
                    context.PersonStoreRevisionAtStart,
                    lifecycleProviders,
                    context.EpochReservation,
                    out _);
                if (!reconciled)
                {
                    npcRosterCensusProtocol.FaultClosed();
                    if (context.EpochReservation != null)
                        npcRosterCensusProtocol.ReleaseMutationEpochReservation(context.EpochReservation);
                }
                else if ((context.RosterChanged || context.PersonStoreRevisionDelta != 0L)
                    && runtimeAdmissionContext != null
                    && (!TryRebindNpcOwnerMutationBoundaries()
                        || !TryBindP12LifecycleMutationBoundaries()))
                {
                    npcRosterCensusProtocol.FaultClosed();
                }
            }
        }
        else if (context.EpochReservation != null && npcRosterCensusProtocol != null)
        {
            npcRosterCensusProtocol.ReleaseMutationEpochReservation(context.EpochReservation);
        }

        context.ProtocolScope?.Dispose();
        activeNpcMembershipCensusContext = null;
    }

    internal void MarkNpcMembershipPersonStoreRevisionCommitted()
    {
        NpcMembershipCensusContext context = activeNpcMembershipCensusContext;
        if (context == null
            || !context.IsOwnedByCurrentThread()
            || context.PersonStoreRevisionDelta == long.MaxValue)
        {
            FaultNpcMembershipCensusBoundary();
            return;
        }

        context.PersonStoreRevisionDelta++;
    }

    private void FaultNpcMembershipCensusBoundary() => npcRosterCensusProtocol?.FaultClosed();

    private void BindCoreMutationGuardAuthorities()
    {
        List<IAuthoritativeMutationGuardBindable> authorities = new List<IAuthoritativeMutationGuardBindable>();
        AddRequiredMutationGuardBinding(authorities, personStore, nameof(PersonStore));
        AddRequiredMutationGuardBinding(authorities, actorChoiceStore, nameof(ActorChoiceStore));
        AddRequiredMutationGuardBinding(authorities, spatialAuthorityStore, nameof(SpatialAuthorityStore));
        AddRequiredMutationGuardBinding(authorities, structureStore, nameof(StructureStore));
        AddRequiredMutationGuardBinding(authorities, legacySpatialAnchorBindingStore, nameof(LegacySpatialAnchorBindingStore));
        AddRequiredMutationGuardBinding(authorities, personSpatialPositionStore, nameof(PersonSpatialPositionStore));
        AddRequiredMutationGuardBinding(authorities, spatialRouteKnowledgeStore, nameof(SpatialRouteKnowledgeStore));
        AddRequiredMutationGuardBinding(authorities, personRoutePlanStore, nameof(PersonRoutePlanStore));
        AddRequiredMutationGuardBinding(authorities, armedForceStore, nameof(ArmedForceStore));
        AddRequiredMutationGuardBinding(authorities, contingentManpowerStateStore, nameof(ContingentManpowerStateStore));
        AddRequiredMutationGuardBinding(authorities, armedForceSpatialStateStore, nameof(ArmedForceSpatialStateStore));
        AddRequiredMutationGuardBinding(authorities, localTopologyStore, nameof(LocalTopologyStore));
        AddRequiredMutationGuardBinding(authorities, conflictStore, nameof(PersistentConflictStore));
        AddRequiredMutationGuardBinding(authorities, warStore, nameof(PersistentWarStore));
        AddRequiredMutationGuardBinding(authorities, battleStore, nameof(PersistentBattleStore));
        AddRequiredMutationGuardBinding(
            authorities,
            new GenealogyMutationGuardBinding(genealogyStore),
            nameof(GenealogyStore));
        AddRequiredMutationGuardBinding(
            authorities,
            new CoreOwnerMutationGuardBindable(
                guard => InstitutionOfficeOwnerBridge.CanBindMutationStatus(institutionStore, guard),
                guard => InstitutionOfficeOwnerBridge.TryBindMutationStatus(institutionStore, guard)),
            nameof(InstitutionStore));
        AddRequiredMutationGuardBinding(
            authorities,
            new CoreOwnerMutationGuardBindable(
                guard => InstitutionOfficeOwnerBridge.CanBindMutationStatus(officeStore, guard),
                guard => InstitutionOfficeOwnerBridge.TryBindMutationStatus(officeStore, guard)),
            nameof(OfficeStore));
        AddRequiredMutationGuardBinding(authorities, propertyOwnershipStore, nameof(PropertyOwnershipStore));
        AddRequiredMutationGuardBinding(authorities, estateStore, nameof(EstateStore));
        AddRequiredMutationGuardBinding(
            authorities,
            new CoreOwnerMutationGuardBindable(
                guard => PoliticalClaimOwnerBridge.CanBindMutationStatus(politicalClaimStore, guard),
                guard => PoliticalClaimOwnerBridge.TryBindMutationStatus(politicalClaimStore, guard)),
            nameof(PoliticalClaimStore));
        AddRequiredMutationGuardBinding(authorities, factionStore, nameof(FactionStore));
        AddRequiredMutationGuardBinding(authorities, politicalSupportStore, nameof(PoliticalSupportStore));
        AddRequiredMutationGuardBinding(authorities, politicalKnowledgeStore, nameof(PoliticalKnowledgeStore));
        AddRequiredMutationGuardBinding(authorities, politicalDecisionStore, nameof(PoliticalDecisionStore));
        AddRequiredMutationGuardBinding(authorities, crimeSocialAppraisalWorldState, nameof(CrimeSocialAppraisalWorldState));
        AddRequiredMutationGuardBinding(authorities, scheduledDirectiveSystem, nameof(ScheduledDirectiveSystem));
        AddRequiredMutationGuardBinding(authorities, justiceSystem, nameof(JusticeSystem));
        AddRequiredMutationGuardBinding(authorities, crimeSystem, nameof(CrimeSystem));
        AddRequiredMutationGuardBinding(authorities, npcDecisionSystem, nameof(NpcDecisionSystem));
        AddRequiredMutationGuardBinding(authorities, travelSystem, nameof(TravelSystem));
        AddRequiredMutationGuardBinding(authorities, travelPartySystem, nameof(TravelPartySystem));
        AddRequiredMutationGuardBinding(authorities, merchantSystem, nameof(MerchantSystem));
        AddRequiredMutationGuardBinding(authorities, commercialKnowledgeSharingSystem, nameof(CommercialKnowledgeSharingSystem));
        AddRequiredMutationGuardBinding(authorities, explorableSiteStore, nameof(ExplorableSiteStore));
        AddRequiredMutationGuardBinding(authorities, explorableSiteKnowledgeSystem, nameof(ExplorableSiteKnowledgeSystem));
        AddRequiredMutationGuardBinding(authorities, expeditionSystem, nameof(ExpeditionSystem));
        AddRequiredMutationGuardBinding(authorities, placeContentStore, nameof(PlaceContentStore));
        AddRequiredMutationGuardBinding(authorities, adventureExpeditionAutonomySystem, nameof(AdventureExpeditionAutonomySystem));

        foreach (CityRuntime city in cities)
        {
            if (city != null && !city.CanBindRuntimeMutationGuard(mutationGuard))
            {
                throw new InvalidOperationException(
                    "A CityRuntime or its population authority is already owned by another SimulationRuntime.");
            }
        }

        foreach (NpcRuntime npc in npcRuntimes)
        {
            if (npc != null && !npc.CanBindRuntimeMutationGuard(mutationGuard))
            {
                throw new InvalidOperationException("An NpcRuntime is already owned by another SimulationRuntime.");
            }
        }

        if (!SimulationTimeOwnerBridge.CanBindMutationStatus(simulationTime, mutationGuard))
        {
            throw new InvalidOperationException(
                "A mutable SimulationRuntime input became bound to another runtime during composition.");
        }

        foreach (IAuthoritativeMutationGuardBindable authority in authorities)
        {
            if (authority == null || authority.CanBindMutationGuard(mutationGuard) == false)
            {
                throw new InvalidOperationException(
                    "A mutable SimulationRuntime input became bound to another runtime during composition.");
            }
        }

        if (!SimulationTimeOwnerBridge.TryBindMutationStatus(simulationTime, mutationGuard))
        {
            throw new InvalidOperationException(
                "A mutable SimulationRuntime input could not bind to its runtime mutation guard.");
        }

        foreach (IAuthoritativeMutationGuardBindable authority in authorities)
        {
            if (authority.TryBindMutationGuard(mutationGuard) == false)
            {
                throw new InvalidOperationException(
                    "A mutable SimulationRuntime input could not bind to its runtime mutation guard.");
            }
        }

        foreach (CityRuntime city in cities)
        {
            if (city != null && !city.TryBindRuntimeMutationGuard(mutationGuard))
            {
                throw new InvalidOperationException("A CityRuntime could not bind to its SimulationRuntime.");
            }
        }

        foreach (NpcRuntime npc in npcRuntimes)
        {
            if (npc != null && !npc.TryBindRuntimeMutationGuard(mutationGuard))
            {
                throw new InvalidOperationException("An NpcRuntime could not bind to its SimulationRuntime.");
            }
        }

        coreMutationGuardAuthoritiesBound = true;
    }

    internal bool AreCoreMutationGuardAuthoritiesBound => coreMutationGuardAuthoritiesBound;

    internal bool TryBindRestoredCandidateMutationGuard(out string diagnostic)
    {
        diagnostic = null;
        if (runtimeAdmissionContext == null
            || !runtimeAdmissionContext.IsRestoredContinuation
            || !IsRuntimeAdmissionOwnerThreadCurrent()
            || coreMutationGuardAuthoritiesBound
            || !mutationGuard.CanMutate
            || factualReadWorldPublished
            || currentDailyCaptureToken != null
            || completedDailyCoreSequence != 0L
            || advanceLeaseHeld
            || HasActiveDailyOperationContext())
        {
            diagnostic = "The private restored candidate is not at its unbound, healthy owner-thread boundary.";
            return false;
        }

        try
        {
            BindCoreMutationGuardAuthorities();
            if (!coreMutationGuardAuthoritiesBound)
            {
                diagnostic = "The private restored candidate mutation guard did not bind to its complete owner graph.";
                return false;
            }
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException)
        {
            diagnostic = "The private restored candidate mutation guard could not bind: " + exception.Message;
            return false;
        }
    }

    private sealed class CoreOwnerMutationGuardBindable : IAuthoritativeMutationGuardBindable
    {
        private readonly Func<AuthoritativeMutationGuard, bool> canBind;
        private readonly Func<AuthoritativeMutationGuard, bool> tryBind;

        internal CoreOwnerMutationGuardBindable(
            Func<AuthoritativeMutationGuard, bool> canBind,
            Func<AuthoritativeMutationGuard, bool> tryBind)
        {
            this.canBind = canBind ?? throw new ArgumentNullException(nameof(canBind));
            this.tryBind = tryBind ?? throw new ArgumentNullException(nameof(tryBind));
        }

        public bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => canBind(guard);
        public bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => tryBind(guard);
    }

    private static void AddRequiredMutationGuardBinding(
        List<IAuthoritativeMutationGuardBindable> authorities,
        object authority,
        string name)
    {
        if (authority == null)
        {
            return;
        }

        if (!(authority is IAuthoritativeMutationGuardBindable bindable))
        {
            throw new InvalidOperationException(
                name + " is retained by SimulationRuntime but does not support one-way mutation-guard binding.");
        }

        authorities.Add(bindable);
    }

    private static void PreflightMutationGuardBinding(
        object authority,
        AuthoritativeMutationGuard guard,
        string name)
    {
        if (authority == null)
        {
            return;
        }

        if (!(authority is IAuthoritativeMutationGuardBindable bindable)
            || bindable.CanBindMutationGuard(guard) == false)
        {
            throw new ArgumentException(
                name + " is already bound to another SimulationRuntime or does not support guard binding.",
                name);
        }
    }

    internal AuthoritativeMutationGuard MutationGuard => mutationGuard;

    internal void MarkAuthoritativeMutationFaulted(AuthoritativeMutationFaultReason reason)
    {
        mutationGuard.MarkFaulted(reason);
    }

    internal void MarkNpcMembershipCensusCompensationFailed()
    {
        mutationGuard.MarkFaulted(AuthoritativeMutationFaultReason.RollbackRestoreFailed);
        npcRosterCensusProtocol?.FaultClosed();
    }

    /// <summary>
    /// Registers one named NPC in the world-owned roster. The operation rejects null,
    /// unidentified, and duplicate RuntimeIds and never changes population aggregates.
    /// </summary>
    public bool TryRegisterNpc(NpcRuntime npcRuntime, out WorldNpcRegistryFailure failure)
    {
        using (NpcMembershipCensusScope censusScope = BeginNpcMembershipCensusScope())
        {
            return TryRegisterNpcWithinMembershipCensus(npcRuntime, censusScope, out failure);
        }
    }

    private bool TryRegisterNpcWithinMembershipCensus(
        NpcRuntime npcRuntime,
        NpcMembershipCensusScope censusScope,
        out WorldNpcRegistryFailure failure)
    {
        failure = WorldNpcRegistryFailure.None;

        if (censusScope == null || !censusScope.CanProceed)
        {
            failure = WorldNpcRegistryFailure.RuntimeFaulted;
            return false;
        }

        if (mutationGuard.CanMutate == false)
        {
            failure = WorldNpcRegistryFailure.RuntimeFaulted;
            return false;
        }

        if (npcRuntime == null)
        {
            failure = WorldNpcRegistryFailure.InvalidNpc;
            return false;
        }

        if (!npcRuntime.CanBindRuntimeMutationGuard(mutationGuard))
        {
            failure = WorldNpcRegistryFailure.AlreadyOwnedByAnotherRuntime;
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcRuntime.RuntimeId) == true)
        {
            failure = WorldNpcRegistryFailure.InvalidRuntimeId;
            return false;
        }

        if (npcRegistryById.ContainsKey(npcRuntime.RuntimeId) == true)
        {
            failure = WorldNpcRegistryFailure.DuplicateRuntimeId;
            return false;
        }

        bool identityAlreadyRegistered = false;
        if (requireP12RuntimeIdentitySpatialCensusOwners)
        {
            if (p12RuntimeIdentityRegistry.TryGetNpcForMembership(
                    npcRuntime.RuntimeId,
                    out NpcRuntime registeredIdentity))
            {
                if (!ReferenceEquals(registeredIdentity, npcRuntime))
                {
                    failure = WorldNpcRegistryFailure.DuplicateRuntimeId;
                    return false;
                }
                identityAlreadyRegistered = true;
            }
            else if (isComposingNpcRoster)
            {
                failure = WorldNpcRegistryFailure.RuntimeFaulted;
                return false;
            }
            else if (!p12RuntimeIdentityRegistry.CanRegisterNpcForMembership(
                         npcRuntime,
                         out bool duplicateIdentity))
            {
                failure = duplicateIdentity
                    ? WorldNpcRegistryFailure.DuplicateRuntimeId
                    : WorldNpcRegistryFailure.RuntimeFaulted;
                return false;
            }
        }

        if (!isComposingNpcRoster
            && runtimeAdmissionContext != null
            && npcRuntime.CurrentCity != null
            && !cityNpcPresenceSectionIdsByOwner.ContainsKey(npcRuntime.CurrentCity))
        {
            failure = WorldNpcRegistryFailure.InvalidNpc;
            return false;
        }

        if (npcRuntime.PersonId != null
            && (personStore.TryGet(npcRuntime.PersonId, out PersonRuntime person) == false
                || person.IsMaterialized == false
                || string.Equals(person.MaterializedNpcRuntimeId, npcRuntime.RuntimeId, StringComparison.Ordinal) == false))
        {
            failure = WorldNpcRegistryFailure.NpcPersonBindingInvalid;
            return false;
        }

        if (npcRuntime.PersonId != null
            && personStore.TryGet(npcRuntime.PersonId, out PersonRuntime lifeStatePerson)
            && lifeStatePerson.IsDeadAt(CurrentDay) != npcRuntime.IsDead)
        {
            failure = WorldNpcRegistryFailure.NpcPersonBindingInvalid;
            return false;
        }

        if (npcRuntime.PersonId != null
            && (personStore.TryGet(npcRuntime.PersonId, out PersonRuntime boundPerson) == false
                || npcRuntime.TryBindPersonRuntime(boundPerson) == false))
        {
            failure = WorldNpcRegistryFailure.NpcPersonBindingInvalid;
            return false;
        }

        if (!isComposingNpcRoster
            && npcRuntime.UsesSelectedDailyV1Definitions
            && selectedRuntimeOwnerGraphProvider != null
            && !npcRuntime.CanBindSelectedRuntimeOwnerGraphProvider(selectedRuntimeOwnerGraphProvider))
        {
            failure = WorldNpcRegistryFailure.AlreadyOwnedByAnotherRuntime;
            return false;
        }

        DailyV1SelectedRuntimeOwnerGraph selectedGraphBeforeMembership = null;
        DailyV1SelectedRuntimeOwnerGraph selectedGraphAfterMembership = null;
        IReadOnlyList<DailyV1ActorTurnRosterProjection> selectedRosterBeforeMembership = null;
        IReadOnlyList<DailyV1ActorTurnRosterProjection> selectedRosterAfterMembership = null;
        DailyV1SelectedRuntimeOwnerGraph selectedGraphForMembership = null;
        bool replaceSelectedOwnerGraph = false;
        if (!isComposingNpcRoster)
        {
            selectedGraphBeforeMembership = SelectedRuntimeOwnerGraph;
            selectedRosterBeforeMembership = dailyExecutionBoundary.SelectedRuntimeActorRoster;
            selectedGraphForMembership = selectedGraphBeforeMembership;
            if (selectedGraphBeforeMembership != null && selectedRosterBeforeMembership == null)
            {
                FaultNpcMembershipCensusBoundary();
                failure = WorldNpcRegistryFailure.RuntimeFaulted;
                return false;
            }
            if (npcRuntime.UsesSelectedDailyV1Definitions)
            {
                if (selectedGraphBeforeMembership == null
                    || !selectedGraphBeforeMembership.TryCreateWithAddedNpc(
                        npcRuntime.SelectedRuntimeOwners,
                        out selectedGraphAfterMembership))
                {
                    failure = WorldNpcRegistryFailure.InvalidNpc;
                    return false;
                }
                replaceSelectedOwnerGraph = true;
            }
            if (selectedGraphAfterMembership != null)
                selectedGraphForMembership = selectedGraphAfterMembership;
            if (selectedRosterBeforeMembership != null
                && !dailyExecutionBoundary.TryCreateSelectedRuntimeActorRosterWithAppendedMember(
                    new DailyV1ActorTurnRosterProjection(npcRuntime.RuntimeId, npcRuntime.PersonId),
                    selectedGraphForMembership,
                    out selectedRosterAfterMembership))
            {
                failure = WorldNpcRegistryFailure.InvalidNpc;
                return false;
            }
        }

        if (!isComposingNpcRoster && !npcRuntime.TryBindRuntimeMutationGuard(mutationGuard))
        {
            failure = WorldNpcRegistryFailure.AlreadyOwnedByAnotherRuntime;
            return false;
        }

        bool selectedMembershipPublished = false;
        if (selectedRosterBeforeMembership != null)
        {
            selectedMembershipPublished = dailyExecutionBoundary.TryReplaceSelectedRuntimeMembership(
                selectedGraphBeforeMembership,
                selectedGraphForMembership,
                selectedRosterBeforeMembership,
                selectedRosterAfterMembership);
        }
        else if (replaceSelectedOwnerGraph)
        {
            selectedMembershipPublished = dailyExecutionBoundary.TryReplaceSelectedRuntimeOwnerGraph(
                selectedGraphBeforeMembership,
                selectedGraphAfterMembership);
        }
        if ((selectedRosterBeforeMembership != null || replaceSelectedOwnerGraph)
            && !selectedMembershipPublished)
        {
            FaultNpcMembershipCensusBoundary();
            failure = WorldNpcRegistryFailure.RuntimeFaulted;
            return false;
        }
        if (selectedMembershipPublished && selectedRosterBeforeMembership != null)
            selectedRosterAfterMembership = dailyExecutionBoundary.SelectedRuntimeActorRoster;

        bool boundSelectedOwnerGraphProvider = false;
        if (!isComposingNpcRoster
            && replaceSelectedOwnerGraph
            && selectedRuntimeOwnerGraphProvider != null)
        {
            if (!npcRuntime.TryBindSelectedRuntimeOwnerGraphProvider(selectedRuntimeOwnerGraphProvider))
            {
                bool restored = selectedRosterBeforeMembership != null
                    ? dailyExecutionBoundary.TryReplaceSelectedRuntimeMembership(
                        selectedGraphForMembership,
                        selectedGraphBeforeMembership,
                        selectedRosterAfterMembership,
                        selectedRosterBeforeMembership)
                    : dailyExecutionBoundary.TryReplaceSelectedRuntimeOwnerGraph(
                        selectedGraphAfterMembership,
                        selectedGraphBeforeMembership);
                if (!restored)
                    mutationGuard.MarkFaulted(AuthoritativeMutationFaultReason.RollbackRestoreFailed);
                failure = WorldNpcRegistryFailure.AlreadyOwnedByAnotherRuntime;
                return false;
            }
            boundSelectedOwnerGraphProvider = true;
        }

        bool insertedIdentity = requireP12RuntimeIdentitySpatialCensusOwners
            && !identityAlreadyRegistered;
        if (insertedIdentity && !p12RuntimeIdentityRegistry.RegisterNpc(npcRuntime))
        {
            if (boundSelectedOwnerGraphProvider)
                npcRuntime.UnbindSelectedRuntimeOwnerGraphProvider(selectedRuntimeOwnerGraphProvider);
            bool restored = selectedRosterBeforeMembership != null
                ? dailyExecutionBoundary.TryReplaceSelectedRuntimeMembership(
                    selectedGraphForMembership,
                    selectedGraphBeforeMembership,
                    selectedRosterAfterMembership,
                    selectedRosterBeforeMembership)
                : replaceSelectedOwnerGraph
                    ? dailyExecutionBoundary.TryReplaceSelectedRuntimeOwnerGraph(
                        selectedGraphAfterMembership,
                        selectedGraphBeforeMembership)
                    : true;
            if (!restored)
                mutationGuard.MarkFaulted(AuthoritativeMutationFaultReason.RollbackRestoreFailed);
            FaultNpcMembershipCensusBoundary();
            failure = WorldNpcRegistryFailure.RuntimeFaulted;
            return false;
        }
        if (insertedIdentity)
            censusScope.MarkRuntimeIdentityChanged();

        npcRegistryById.Add(npcRuntime.RuntimeId, npcRuntime);
        npcRuntimes.Add(npcRuntime);
        if (!isComposingNpcRoster && npcRuntime.CurrentCity != null)
            censusScope.MarkCityPresenceChanged(npcRuntime.CurrentCity);
        censusScope.MarkRosterChanged();
        return true;
    }

    /// <summary>
    /// Explicitly unregisters an NPC from the world. A resident must first emigrate
    /// through the population lifecycle so the aggregate cannot retain a named member
    /// that disappeared from the authoritative roster.
    /// </summary>
    public bool TryUnregisterNpc(string runtimeId, out WorldNpcRegistryFailure failure)
    {
        using (NpcMembershipCensusScope censusScope = BeginNpcMembershipCensusScope())
        {
            return TryUnregisterNpcWithinMembershipCensus(runtimeId, censusScope, out failure);
        }
    }

    private bool TryUnregisterNpcWithinMembershipCensus(
        string runtimeId,
        NpcMembershipCensusScope censusScope,
        out WorldNpcRegistryFailure failure)
    {
        failure = WorldNpcRegistryFailure.None;

        if (censusScope == null || !censusScope.CanProceed)
        {
            failure = WorldNpcRegistryFailure.RuntimeFaulted;
            return false;
        }

        if (mutationGuard.CanMutate == false)
        {
            failure = WorldNpcRegistryFailure.RuntimeFaulted;
            return false;
        }

        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            failure = WorldNpcRegistryFailure.InvalidRuntimeId;
            return false;
        }

        if (npcRegistryById.TryGetValue(runtimeId, out NpcRuntime npcRuntime) == false)
        {
            failure = WorldNpcRegistryFailure.NpcNotRegistered;
            return false;
        }

        if (personStore.TryGetByMaterializedNpcRuntimeId(runtimeId, out _))
        {
            failure = WorldNpcRegistryFailure.NpcBoundToPerson;
            return false;
        }

        if (string.IsNullOrWhiteSpace(npcRuntime.ResidenceSettlementRuntimeId) == false)
        {
            failure = WorldNpcRegistryFailure.NpcHasResidence;
            return false;
        }

        DailyV1SelectedRuntimeOwnerGraph selectedGraphBeforeMembership = null;
        DailyV1SelectedRuntimeOwnerGraph selectedGraphAfterMembership = null;
        IReadOnlyList<DailyV1ActorTurnRosterProjection> selectedRosterBeforeMembership = null;
        IReadOnlyList<DailyV1ActorTurnRosterProjection> selectedRosterAfterMembership = null;
        DailyV1SelectedRuntimeOwnerGraph selectedGraphForMembership = null;
        bool replaceSelectedOwnerGraph = false;
        selectedGraphBeforeMembership = SelectedRuntimeOwnerGraph;
        selectedRosterBeforeMembership = dailyExecutionBoundary.SelectedRuntimeActorRoster;
        selectedGraphForMembership = selectedGraphBeforeMembership;
        if (selectedGraphBeforeMembership != null && selectedRosterBeforeMembership == null)
        {
            FaultNpcMembershipCensusBoundary();
            failure = WorldNpcRegistryFailure.RuntimeFaulted;
            return false;
        }
        if (npcRuntime.UsesSelectedDailyV1Definitions)
        {
            if (selectedGraphBeforeMembership == null
                || !selectedGraphBeforeMembership.TryCreateWithoutNpc(
                    npcRuntime.SelectedRuntimeOwners,
                    out selectedGraphAfterMembership))
            {
                failure = WorldNpcRegistryFailure.RuntimeFaulted;
                return false;
            }
            replaceSelectedOwnerGraph = true;
            selectedGraphForMembership = selectedGraphAfterMembership;
        }
        if (selectedRosterBeforeMembership != null
            && !dailyExecutionBoundary.TryCreateSelectedRuntimeActorRosterWithoutMember(
                runtimeId,
                selectedGraphForMembership,
                out selectedRosterAfterMembership))
        {
            FaultNpcMembershipCensusBoundary();
            failure = WorldNpcRegistryFailure.RuntimeFaulted;
            return false;
        }

        bool selectedMembershipPublished = false;
        if (selectedRosterBeforeMembership != null)
        {
            selectedMembershipPublished = dailyExecutionBoundary.TryReplaceSelectedRuntimeMembership(
                selectedGraphBeforeMembership,
                selectedGraphForMembership,
                selectedRosterBeforeMembership,
                selectedRosterAfterMembership);
        }
        else if (replaceSelectedOwnerGraph)
        {
            selectedMembershipPublished = dailyExecutionBoundary.TryReplaceSelectedRuntimeOwnerGraph(
                selectedGraphBeforeMembership,
                selectedGraphAfterMembership);
        }
        if ((selectedRosterBeforeMembership != null || replaceSelectedOwnerGraph)
            && !selectedMembershipPublished)
        {
            FaultNpcMembershipCensusBoundary();
            failure = WorldNpcRegistryFailure.RuntimeFaulted;
            return false;
        }
        if (selectedMembershipPublished && selectedRosterBeforeMembership != null)
            selectedRosterAfterMembership = dailyExecutionBoundary.SelectedRuntimeActorRoster;

        if (npcRuntime.CurrentCity != null
            && !npcRuntime.SetCurrentPresence(null, null))
        {
            bool restored = selectedRosterBeforeMembership != null
                ? dailyExecutionBoundary.TryReplaceSelectedRuntimeMembership(
                    selectedGraphForMembership,
                    selectedGraphBeforeMembership,
                    selectedRosterAfterMembership,
                    selectedRosterBeforeMembership)
                : replaceSelectedOwnerGraph
                    ? dailyExecutionBoundary.TryReplaceSelectedRuntimeOwnerGraph(
                        selectedGraphAfterMembership,
                        selectedGraphBeforeMembership)
                    : true;
            if (!restored)
                mutationGuard.MarkFaulted(AuthoritativeMutationFaultReason.RollbackRestoreFailed);
            failure = WorldNpcRegistryFailure.RuntimeFaulted;
            return false;
        }

        npcRegistryById.Remove(runtimeId);
        npcRuntimes.Remove(npcRuntime);
        censusScope.MarkRosterChanged();
        return true;
    }

    public bool TryGetNpcRuntime(string runtimeId, out NpcRuntime npcRuntime)
    {
        npcRuntime = null;
        return string.IsNullOrWhiteSpace(runtimeId) == false
            && npcRegistryById.TryGetValue(runtimeId, out npcRuntime);
    }

    private NpcRuntime ResolveRegisteredNpcPresenceProjection(string runtimeId)
    {
        return !string.IsNullOrWhiteSpace(runtimeId)
            && npcRegistryById.TryGetValue(runtimeId, out NpcRuntime npcRuntime)
                ? npcRuntime
                : null;
    }

    /// <summary>
    /// Computes living represented-resident floors at the world boundary. The
    /// aggregate population system receives the resulting snapshot explicitly.
    /// </summary>
    internal RepresentedResidentFloorSnapshot BuildRepresentedResidentFloorSnapshot()
    {
        Dictionary<string, int> floors = new Dictionary<string, int>(StringComparer.Ordinal);
        Dictionary<string, int> hostSupplements = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (CityRuntime city in cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId))
            {
                continue;
            }

            SettlementPopulationPresenceSummary presence =
                SettlementPopulationPresenceQuery.BuildSummary(
                    city,
                    npcRuntimeSnapshot,
                    personStore.Persons);
            int representedResidents = presence?.RepresentedResidentCount ?? 0;
            int corePersonResidents = 0;
            foreach (PersonRuntime person in personStore.Persons)
            {
                if (person == null || person.PersonId == null || person.DeathAbsoluteDay.HasValue
                    || !string.Equals(person.ResidenceSettlementRuntimeId, city.RuntimeId, StringComparison.Ordinal))
                    continue;
                if (corePersonResidents < int.MaxValue) corePersonResidents++;
            }

            floors[city.RuntimeId] = representedResidents;
            hostSupplements[city.RuntimeId] = representedResidents >= corePersonResidents
                ? representedResidents - corePersonResidents
                : -1;
        }

        return new RepresentedResidentFloorSnapshot(floors, hostSupplements);
    }

    internal bool TryGetManpowerSourceSettlementContext(
        CityRuntime city,
        SettlementPopulationRuntime expectedPopulation,
        out int representedResidentFloor)
    {
        representedResidentFloor = 0;
        if (city == null
            || expectedPopulation == null
            || !SettlementManpowerSourceRegistry.ContainsExactCity(cities, city)
            || !ReferenceEquals(city.Population, expectedPopulation)
            || !string.Equals(city.RuntimeId, expectedPopulation.SettlementRuntimeId, StringComparison.Ordinal))
        {
            return false;
        }

        SettlementPopulationPresenceSummary presence = SettlementPopulationPresenceQuery.BuildSummary(
            city,
            npcRuntimeSnapshot,
            personStore.Persons);
        if (presence == null
            || presence.ResidentPopulation != expectedPopulation.CurrentPopulation
            || presence.RepresentedResidentCount < 0
            || presence.RepresentedResidentCount > expectedPopulation.CurrentPopulation)
        {
            return false;
        }

        representedResidentFloor = presence.RepresentedResidentCount;
        return true;
    }

    public bool TryRegisterPerson(PersonRuntime person, out PersonStoreFailure failure)
    {
        if (person != null
            && person.BirthAbsoluteDay.HasValue
            && person.BirthAbsoluteDay.Value > CurrentDay)
        {
            failure = PersonStoreFailure.BirthAbsoluteDayInFuture;
            return false;
        }

        if (person != null
            && person.DeathAbsoluteDay.HasValue
            && person.DeathAbsoluteDay.Value > CurrentDay)
        {
            failure = PersonStoreFailure.DeathAbsoluteDayInFuture;
            return false;
        }

        using (NpcMembershipCensusScope censusScope = BeginNpcMembershipCensusScope())
        {
            if (!censusScope.CanProceed)
            {
                failure = PersonStoreFailure.RuntimeFaulted;
                return false;
            }

            bool registered = personStore.TryRegister(person, out failure);
            if (registered)
            {
                MarkNpcMembershipPersonStoreRevisionCommitted();
                AdvancePoliticalWorldRevision();
            }

            return registered;
        }
    }

    public bool TryProposePersonDeath(
        PersonId personId,
        out PersonDeathTransition transition,
        out PersonDeathLifecycleFailure failure)
    {
        return PersonDeathLifecycleSystem.TryProposeDeath(
            this,
            personId,
            out transition,
            out failure);
    }

    public bool TryApplyPersonDeath(
        PersonDeathTransition transition,
        out PersonDeathLifecycleFailure failure)
    {
        if (runtimeAdmissionContext != null)
        {
            if (transition == null || !string.IsNullOrWhiteSpace(transition.OperationIdentity))
            {
                failure = PersonDeathLifecycleFailure.RuntimeFaulted;
                return false;
            }
            if (!PersonDeathLifecycleSystem.TryValidateDeath(
                    this,
                    transition,
                    false,
                    out NpcRuntime materializedNpc,
                    out failure))
                return false;

            bool resident = transition.ExpectsResident;
            CityRuntime settlement = null;
            if (resident && !TryResolveCityByRuntimeId(
                    transition.ExpectedResidenceSettlementRuntimeId,
                    out settlement))
            {
                failure = PersonDeathLifecycleFailure.ResidenceSettlementMissing;
                return false;
            }
            if (!TryBuildPersonLifecycleSectionIds(
                    transition.ExpectedPerson,
                    resident,
                    materializedNpc,
                    settlement,
                    out List<string> sectionIds))
            {
                failure = PersonDeathLifecycleFailure.RuntimeFaulted;
                return false;
            }

            int personRevisionIncrements = resident ? 2 : 1;
            bool actionChanged = materializedNpc?.CurrentActionRuntime != null;
            if (!TryBeginP12PopulationOperation(
                    DailyV1RuntimeProtocolOperationContracts.PersonDeath,
                    sectionIds,
                    () => PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(
                        transition.ExpectedPerson,
                        personRevisionIncrements)
                        && (materializedNpc == null
                            || materializedNpc.CanAdvanceP12LifecycleRevisions(
                                1, 0, actionChanged ? 1 : 0))
                        && (materializedNpc == null
                            || materializedNpc.CanCommitSelectedPersonDeathAfterValidation(
                                NpcInjurySeverity.None, out _))
                        && (!resident || PopulationOwnerBridge.CanAdvanceP12AggregateRevision(settlement.Population)),
                    out P12PopulationOperationScope p12Scope))
            {
                failure = PersonDeathLifecycleFailure.RuntimeFaulted;
                return false;
            }

            using (p12Scope)
            {
                bool admitted = PersonDeathLifecycleSystem.TryApplyDeath(this, transition, out failure);
                if (admitted) AdvancePoliticalWorldRevision();
                p12Scope?.Complete(admitted);
                return admitted;
            }
        }

        bool applied = PersonDeathLifecycleSystem.TryApplyDeath(this, transition, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryApplyPersonDeathWithConflictInjury(
        PersonDeathTransition transition,
        NpcInjurySeverity injurySeverity,
        out PersonDeathLifecycleFailure failure)
    {
        if (runtimeAdmissionContext != null)
        {
            failure = PersonDeathLifecycleFailure.RuntimeFaulted;
            return false;
        }

        bool applied = PersonDeathLifecycleSystem.TryApplyDeathWithConflictInjury(
            this,
            transition,
            injurySeverity,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryApplyPersonDeath(
        PersonId personId,
        out PersonDeathTransition transition,
        out PersonDeathLifecycleFailure failure)
    {
        if (runtimeAdmissionContext != null)
        {
            if (!PersonDeathLifecycleSystem.TryProposeDeath(
                    this,
                    personId,
                    out transition,
                    out failure))
                return false;
            return TryApplyPersonDeath(transition, out failure);
        }

        bool applied = PersonDeathLifecycleSystem.TryApplyDeath(
            this,
            personId,
            out transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryRegisterInstitution(
        InstitutionRecord record,
        out InstitutionFoundationFailure failure)
    {
        SimulationOperationScope p12Scope = null;
        if (!TryBeginP12InstitutionOfficeOwnerCommit(
                new[] { InstitutionOfficeCensusProvider.InstitutionsSectionId },
                out p12Scope))
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.RuntimeFaulted,
                "The P12 Institution census operation could not be admitted.");
            return false;
        }

        try
        {
            bool registered = institutionStore.TryRegister(record, out failure);
            if (registered)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12InstitutionOfficeOwnerCommit(
                    new[] { InstitutionOfficeCensusProvider.InstitutionsSectionId });
            }

            return registered;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryRegisterFaction(
        FactionRecord record,
        out FactionFoundationFailure failure)
    {
        if (record == null || record.Id == null)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.InvalidFaction,
                "A faction with a stable FactionId is required.");
            return false;
        }

        if (record.CreatedAbsoluteDay > CurrentDay)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.InvalidCreationAbsoluteDay,
                "Faction creation must be within the current world timeline.");
            return false;
        }

        if (!TryBeginP12FactionStoreOwnerCommit(out SimulationOperationScope p12Scope))
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.RuntimeFaulted,
                "The P12 FactionStore census operation could not be admitted.");
            return false;
        }

        try
        {
            bool registered = factionStore.TryRegister(record, out failure);
            if (registered)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12FactionStoreOwnerCommit();
            }

            return registered;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryProposeFactionAffiliation(
        FactionId factionId,
        PersonId personId,
        out FactionAffiliationAddTransition transition,
        out FactionFoundationFailure failure)
    {
        transition = null;
        if (factionId == null || factionStore.TryGet(factionId, out _) == false)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.FactionNotRegistered,
                "The faction must be registered in this world.");
            return false;
        }

        if (personId == null || personStore.TryGet(personId, out _) == false)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.PersonNotRegistered,
                "The affiliated Person must be registered in this world.");
            return false;
        }

        return FactionAffiliationSystem.TryProposeAdd(
            factionStore,
            factionId,
            personId,
            CurrentDay,
            out transition,
            out failure);
    }

    public bool TryApplyFactionAffiliation(
        FactionAffiliationAddTransition transition,
        out FactionFoundationFailure failure)
    {
        if (transition == null || transition.ExpectedWorldDay != CurrentDay)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.StaleAffiliation,
                "The faction affiliation proposal was created for a different world day.");
            return false;
        }

        if (!TryBeginP12FactionStoreOwnerCommit(out SimulationOperationScope p12Scope))
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.RuntimeFaulted,
                "The P12 FactionStore census operation could not be admitted.");
            return false;
        }

        try
        {
            bool applied = FactionAffiliationSystem.TryApplyAdd(factionStore, transition, out failure);
            if (applied)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12FactionStoreOwnerCommit();
            }

            return applied;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryProposeFactionAffiliationEnd(
        FactionId factionId,
        PersonId personId,
        out FactionAffiliationEndTransition transition,
        out FactionFoundationFailure failure)
    {
        transition = null;
        if (factionId == null || personId == null)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.InvalidTransition,
                "A faction and PersonId are required.");
            return false;
        }

        return FactionAffiliationSystem.TryProposeEnd(
            factionStore,
            factionId,
            personId,
            CurrentDay,
            out transition,
            out failure);
    }

    public bool TryProposeFactionAffiliationExpulsion(
        FactionId factionId,
        PersonId personId,
        out FactionAffiliationEndTransition transition,
        out FactionFoundationFailure failure)
    {
        transition = null;
        if (factionId == null || personId == null)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.InvalidTransition,
                "A faction and PersonId are required.");
            return false;
        }

        return FactionAffiliationSystem.TryProposeEnd(
            factionStore,
            factionId,
            personId,
            CurrentDay,
            true,
            out transition,
            out failure);
    }

    public bool TryApplyFactionAffiliationEnd(
        FactionAffiliationEndTransition transition,
        out FactionFoundationFailure failure)
    {
        if (transition == null || transition.ExpectedWorldDay != CurrentDay)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.StaleAffiliation,
                "The faction affiliation end proposal was created for a different world day.");
            return false;
        }

        if (transition.EndedAbsoluteDay > CurrentDay)
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.InvalidEndAbsoluteDay,
                "Affiliation end must be within the current world timeline.");
            return false;
        }

        if (!TryBeginP12FactionStoreOwnerCommit(out SimulationOperationScope p12Scope))
        {
            failure = FactionFoundationFailure.Create(
                FactionFoundationFailureCode.RuntimeFaulted,
                "The P12 FactionStore census operation could not be admitted.");
            return false;
        }

        try
        {
            bool applied = FactionAffiliationSystem.TryApplyEnd(factionStore, transition, out failure);
            if (applied)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12FactionStoreOwnerCommit();
            }

            return applied;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryRegisterPoliticalClaim(
        PoliticalClaimRecord record,
        out PoliticalClaimFailure failure)
    {
        failure = PoliticalClaimFailure.None;
        if (record == null || record.ClaimId == null || record.ClaimantPersonId == null)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidClaim,
                "A political claim with a claimant and claim id is required.");
            return false;
        }

        if (record.CreatedAbsoluteDay < 0L || record.CreatedAbsoluteDay > CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidCreationAbsoluteDay,
                "Claim creation must be within the current world timeline.");
            return false;
        }

        if (personStore.TryGet(record.ClaimantPersonId, out _) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.ClaimantNotRegistered,
                "The political claim claimant must be registered in this world.");
            return false;
        }

        if (TryValidatePoliticalClaimTarget(record, out failure) == false)
        {
            return false;
        }

        if (record.ResolutionAbsoluteDay.HasValue
            && record.ResolutionAbsoluteDay.Value > CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidResolutionAbsoluteDay,
                "Claim resolution must be within the current world timeline.");
            return false;
        }

        if (!TryBeginP12PoliticalClaimOwnerCommit(out SimulationOperationScope p12Scope))
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.RuntimeFaulted,
                "The runtime admission boundary rejected the political claim commit.");
            return false;
        }

        try
        {
            bool registered = politicalClaimStore.TryRegister(record, out failure);
            if (registered)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12PoliticalClaimOwnerCommit();
            }

            return registered;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryProposePoliticalClaimRecognition(
        PoliticalClaimId claimId,
        InstitutionId recognizingInstitutionId,
        PoliticalClaimRecognitionState recognitionState,
        string reason,
        out PoliticalClaimRecognitionTransition transition,
        out PoliticalClaimFailure failure)
    {
        transition = null;
        if (recognizingInstitutionId == null
            || institutionStore.TryGet(recognizingInstitutionId, out _) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.ClaimTargetNotFound,
                "The recognizing institution must be registered in this world.");
            return false;
        }

        return PoliticalClaimOwnerBridge.TryProposeRecognition(
            politicalClaimStore,
            claimId,
            recognizingInstitutionId,
            recognitionState,
            CurrentDay,
            CurrentDay,
            reason,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalClaimRecognition(
        PoliticalClaimRecognitionTransition transition,
        out PoliticalClaimFailure failure)
    {
        if (transition == null || transition.ExpectedWorldDay != CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.StaleClaim,
                "The political claim recognition proposal was created for a different world day.");
            return false;
        }

        if (transition.RecognitionAbsoluteDay > CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidRecognitionAbsoluteDay,
                "Claim recognition must be applied within the current world timeline.");
            return false;
        }

        if (!TryBeginP12PoliticalClaimOwnerCommit(out SimulationOperationScope p12Scope))
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.RuntimeFaulted,
                "The runtime admission boundary rejected the political claim recognition commit.");
            return false;
        }

        try
        {
            bool applied = PoliticalClaimOwnerBridge.TryApplyRecognition(
                politicalClaimStore,
                transition,
                out failure);
            if (applied)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12PoliticalClaimOwnerCommit();
            }

            return applied;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryProposePoliticalClaimResolution(
        PoliticalClaimId claimId,
        PoliticalClaimStatus status,
        out PoliticalClaimResolutionTransition transition,
        out PoliticalClaimFailure failure)
    {
        return PoliticalClaimOwnerBridge.TryProposeResolution(
            politicalClaimStore,
            claimId,
            status,
            CurrentDay,
            CurrentDay,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalClaimResolution(
        PoliticalClaimResolutionTransition transition,
        out PoliticalClaimFailure failure)
    {
        if (transition == null || transition.ExpectedWorldDay != CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.StaleClaim,
                "The political claim resolution proposal was created for a different world day.");
            return false;
        }

        if (transition.ResolutionAbsoluteDay > CurrentDay)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.InvalidResolutionAbsoluteDay,
                "Claim resolution must be applied within the current world timeline.");
            return false;
        }

        if (!TryBeginP12PoliticalClaimOwnerCommit(out SimulationOperationScope p12Scope))
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.RuntimeFaulted,
                "The runtime admission boundary rejected the political claim resolution commit.");
            return false;
        }

        try
        {
            bool applied = PoliticalClaimOwnerBridge.TryApplyResolution(
                politicalClaimStore,
                transition,
                out failure);
            if (applied)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12PoliticalClaimOwnerCommit();
            }

            return applied;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    /// <summary>
    /// Registers an existing support relation while loading or composing world state.
    /// New in-world additions must use the proposal/apply pair below so the current
    /// world day and store revision are revalidated atomically.
    /// </summary>
    public bool TryRegisterPoliticalSupport(
        PoliticalSupportRelationRecord record,
        out PoliticalSupportFailure failure)
    {
        if (record == null)
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.InvalidRelation,
                "A political support relation is required.");
            return false;
        }

        if (record.StartedAbsoluteDay > CurrentDay
            || (record.EndedAbsoluteDay.HasValue && record.EndedAbsoluteDay.Value > CurrentDay))
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.StaleWorldDay,
                "Political support history cannot extend into the future of the world timeline.");
            return false;
        }

        if (!TryBeginP12PoliticalSupportOwnerCommit(out SimulationOperationScope p12Scope))
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.RuntimeFaulted,
                "The runtime admission boundary rejected the political support commit.");
            return false;
        }

        try
        {
            bool registered = politicalSupportStore.TryRegister(record, out failure);
            if (registered)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12PoliticalSupportOwnerCommit();
            }

            return registered;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryProposePoliticalSupportAdd(
        PoliticalSupportRelationRecord record,
        out PoliticalSupportAddTransition transition,
        out PoliticalSupportFailure failure)
    {
        return politicalSupportStore.TryProposeAdd(
            record,
            CurrentDay,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalSupportAdd(
        PoliticalSupportAddTransition transition,
        out PoliticalSupportFailure failure)
    {
        if (!TryBeginP12PoliticalSupportOwnerCommit(out SimulationOperationScope p12Scope))
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.RuntimeFaulted,
                "The runtime admission boundary rejected the political support add.");
            return false;
        }

        try
        {
            bool applied = politicalSupportStore.TryApplyAdd(transition, CurrentDay, out failure);
            if (applied)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12PoliticalSupportOwnerCommit();
            }

            return applied;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryProposePoliticalSupportEnd(
        PoliticalSupportRelationId relationId,
        out PoliticalSupportEndTransition transition,
        out PoliticalSupportFailure failure)
    {
        return politicalSupportStore.TryProposeEnd(
            relationId,
            CurrentDay,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalSupportEnd(
        PoliticalSupportEndTransition transition,
        out PoliticalSupportFailure failure)
    {
        if (!TryBeginP12PoliticalSupportOwnerCommit(out SimulationOperationScope p12Scope))
        {
            failure = PoliticalSupportFailure.Create(
                PoliticalSupportFailureCode.RuntimeFaulted,
                "The runtime admission boundary rejected the political support end.");
            return false;
        }

        try
        {
            bool applied = politicalSupportStore.TryApplyEnd(transition, CurrentDay, out failure);
            if (applied)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12PoliticalSupportOwnerCommit();
            }

            return applied;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryRecordPoliticalKnowledge(
        PoliticalKnowledgeHolder holder,
        PoliticalKnowledgeObservation observation,
        out PoliticalKnowledgeFailure failure)
    {
        return politicalKnowledgeStore.TryRecordObservation(
            holder,
            observation,
            CurrentDay,
            out failure);
    }

    public bool TryRegisterPoliticalKnowledgeHolder(
        PoliticalKnowledgeHolder holder,
        out PoliticalKnowledgeFailure failure)
    {
        return politicalKnowledgeStore.TryRegisterHolder(
            holder,
            CurrentDay,
            out failure);
    }

    public bool TryGetPoliticalKnowledge(
        PoliticalKnowledgeHolder holder,
        out PoliticalKnowledgeRuntime runtime)
    {
        return politicalKnowledgeStore.TryGet(holder, out runtime);
    }

    /// <summary>
    /// Registers an immutable political decision record in the world-owned
    /// decision history. Decision registration never executes its outcome.
    /// </summary>
    public bool TryRegisterPoliticalDecision(
        PoliticalDecisionRecord record,
        out PoliticalDecisionFailure failure)
    {
        failure = PoliticalDecisionFailure.None;
        if (record == null || record.DecisionId == null || record.Decider == null)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "A political decision with an id and decider is required.");
            return false;
        }

        if (record.DecisionAbsoluteDay > CurrentDay)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "A political decision cannot be registered in the future of the world timeline.");
            return false;
        }

        if (politicalKnowledgeStore.TryGet(record.Decider, out _) == false)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "The political decision decider must be registered as a knowledge holder in this world.");
            return false;
        }

        if (record.ExpectedWorldRevision != PoliticalWorldRevision
            || record.ExpectedKnowledgeRevision != PoliticalKnowledgeRevision)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.StaleDecision,
                "The political decision was captured against a stale world or knowledge revision.");
            return false;
        }

        if ((record.DecisionKind == PoliticalDecisionKind.SuccessionSelection
                || record.DecisionKind == PoliticalDecisionKind.OfficeSelection)
            && (record.OfficeId == null || officeStore.TryGet(record.OfficeId, out _) == false))
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "An office decision must identify an office registered in this world.");
            return false;
        }

        if (record.DecisionKind == PoliticalDecisionKind.ClaimRecognitionProposal
            && (record.RecognizingInstitutionId == null
                || institutionStore.TryGet(record.RecognizingInstitutionId, out _) == false))
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "A claim recognition decision must identify an institution registered in this world.");
            return false;
        }

        foreach (PersonId candidatePersonId in record.CandidatePersonIds)
        {
            if (candidatePersonId == null || personStore.TryGet(candidatePersonId, out _) == false)
            {
                failure = PoliticalDecisionFailure.Create(
                    PoliticalDecisionFailureCode.InvalidDecision,
                    "Every political decision candidate must be registered in this world.");
                return false;
            }
        }

        if (record.Outcome.ReferencedClaimId != null
            && politicalClaimStore.TryGet(record.Outcome.ReferencedClaimId, out _) == false)
        {
            failure = PoliticalDecisionFailure.Create(
                PoliticalDecisionFailureCode.InvalidDecision,
                "A political decision claim reference must be registered in this world.");
            return false;
        }

        return politicalDecisionStore.TryRegister(record, out failure);
    }

    public bool TryGetPoliticalDecision(
        PoliticalDecisionId decisionId,
        out PoliticalDecisionRecord record)
    {
        return politicalDecisionStore.TryGet(decisionId, out record);
    }

    public bool TryRegisterPropertyOwnership(
        PropertyOwnershipRecord record,
        out PropertyFoundationFailure failure)
    {
        if (record == null || record.OwnerPersonId == null)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.InvalidOwnershipRecord,
                "A property ownership record with a registered Person owner is required.");
            return false;
        }

        if (personStore.TryGet(record.OwnerPersonId, out _) == false)
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.PersonNotRegistered,
                "The property owner PersonId must be registered in this world.");
            return false;
        }

        string[] changedSections =
        {
            PropertyOwnershipCensusProvider.OwnershipSectionId,
            PropertyOwnershipCensusProvider.TransferHistorySectionId
        };
        SimulationOperationScope p12Scope = null;
        if (!TryBeginP12PropertyEstateOwnerCommit(
                DailyV1RuntimeProtocolOperationContracts.PropertyOwnerCommit,
                changedSections,
                out p12Scope))
        {
            failure = PropertyFoundationFailure.Create(
                PropertyFoundationFailureCode.RuntimeFaulted,
                "The P12 property census operation could not be admitted.");
            return false;
        }

        try
        {
            bool registered = propertyOwnershipStore.TryRegister(record, out failure);
            if (registered)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12PropertyEstateOwnerCommit(changedSections);
            }

            return registered;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public IReadOnlyList<PropertyOwnershipRecord> GetPropertiesOwnedBy(PersonId ownerPersonId)
    {
        return propertyOwnershipStore.GetOwnedBy(ownerPersonId);
    }

    public bool TryProposeEstateOpening(
        EstateId estateId,
        PersonId deceasedPersonId,
        long openingAbsoluteDay,
        out EstateOpeningTransition transition,
        out EstateFoundationFailure failure)
    {
        transition = null;
        if (openingAbsoluteDay > CurrentDay)
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.InvalidOpeningDay,
                "An estate cannot be opened in the future relative to the world day.");
            return false;
        }

        return EstateOpeningSystem.TryProposeOpening(
            personStore,
            estateStore,
            estateId,
            deceasedPersonId,
            openingAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyEstateOpening(
        EstateOpeningTransition transition,
        out EstateFoundationFailure failure)
    {
        string[] changedSections = { EstateCensusProvider.SectionId };
        SimulationOperationScope p12Scope = null;
        if (!TryBeginP12PropertyEstateOwnerCommit(
                DailyV1RuntimeProtocolOperationContracts.EstateOwnerCommit,
                changedSections,
                out p12Scope))
        {
            failure = EstateFoundationFailure.Create(
                EstateFoundationFailureCode.RuntimeFaulted,
                "The P12 estate census operation could not be admitted.");
            return false;
        }

        try
        {
            bool applied = EstateOpeningSystem.TryApplyOpening(
                personStore,
                estateStore,
                transition,
                out failure);
            if (applied)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12PropertyEstateOwnerCommit(changedSections);
            }

            return applied;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryOpenEstate(
        EstateId estateId,
        PersonId deceasedPersonId,
        long openingAbsoluteDay,
        out EstateRecord estate,
        out EstateFoundationFailure failure)
    {
        estate = null;
        if (TryProposeEstateOpening(
                estateId,
                deceasedPersonId,
                openingAbsoluteDay,
                out EstateOpeningTransition transition,
                out failure) == false
            || TryApplyEstateOpening(transition, out failure) == false)
        {
            return false;
        }

        estateStore.TryGet(estateId, out estate);
        return estate != null;
    }

    public bool TryBuildSuccessionCandidates(
        PersonId subjectPersonId,
        out SuccessionCandidateSnapshot snapshot,
        out SuccessionCandidateQueryFailure failure)
    {
        SuccessionSubject subject = subjectPersonId == null
            ? null
            : new SuccessionSubject(subjectPersonId);
        return SuccessionCandidateSystem.TryBuildCandidates(
            personStore,
            genealogyStore,
            subject,
            CurrentDay,
            calendar,
            configuration.Population.MaturityAgeYears,
            out snapshot,
            out failure);
    }

    public bool TryProposePropertyTransfer(
        PropertyId propertyId,
        PersonId newOwnerPersonId,
        long transferAbsoluteDay,
        out PropertyOwnershipTransferTransition transition,
        out PropertyTransferFailure failure)
    {
        transition = null;
        if (transferAbsoluteDay < 0L || transferAbsoluteDay > CurrentDay)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidTransferDay,
                "TransferAbsoluteDay must be within the current world timeline.");
            return false;
        }

        return PropertyTransferSystem.TryProposeTransfer(
            personStore,
            propertyOwnershipStore,
            propertyId,
            newOwnerPersonId,
            transferAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyPropertyTransfer(
        PropertyOwnershipTransferTransition transition,
        out PropertyTransferFailure failure)
    {
        if (transition == null)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidTransition,
                "A valid property transfer transition is required.");
            return false;
        }

        if (transition.TransferAbsoluteDay < 0L
            || transition.TransferAbsoluteDay > CurrentDay)
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.InvalidTransferDay,
                "TransferAbsoluteDay must be within the current world timeline.");
            return false;
        }

        string[] changedSections =
        {
            PropertyOwnershipCensusProvider.OwnershipSectionId,
            PropertyOwnershipCensusProvider.TransferHistorySectionId
        };
        SimulationOperationScope p12Scope = null;
        if (!TryBeginP12PropertyEstateOwnerCommit(
                DailyV1RuntimeProtocolOperationContracts.PropertyOwnerCommit,
                changedSections,
                out p12Scope))
        {
            failure = PropertyTransferFailure.Create(
                PropertyTransferFailureCode.RuntimeFaulted,
                "The P12 property census operation could not be admitted.");
            return false;
        }

        try
        {
            bool applied = PropertyTransferSystem.TryApplyTransfer(
                personStore,
                propertyOwnershipStore,
                transition,
                out failure);
            if (applied)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12PropertyEstateOwnerCommit(changedSections);
            }

            return applied;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryTransferProperty(
        PropertyId propertyId,
        PersonId newOwnerPersonId,
        long transferAbsoluteDay,
        out PropertyTransferFailure failure)
    {
        if (TryProposePropertyTransfer(
                propertyId,
                newOwnerPersonId,
                transferAbsoluteDay,
                out PropertyOwnershipTransferTransition transition,
                out failure) == false)
        {
            return false;
        }

        return TryApplyPropertyTransfer(transition, out failure);
    }

    public bool TryProposeOfficeSuccession(
        OfficeId officeId,
        PersonId selectedCandidateId,
        long startAbsoluteDay,
        out OfficeSuccessionTransition transition,
        out OfficeSuccessionFailure failure)
    {
        return OfficeSuccessionSystem.TryPropose(
            this,
            officeId,
            selectedCandidateId,
            startAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyOfficeSuccession(
        OfficeSuccessionTransition transition,
        out OfficeSuccessionFailure failure)
    {
        bool applied = OfficeSuccessionSystem.TryApply(this, transition, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryProposePoliticalOfficeSuccession(
        PoliticalDecisionId decisionId,
        OfficeId officeId,
        long startAbsoluteDay,
        out PoliticalOfficeSuccessionTransition transition,
        out PoliticalSuccessionFailure failure)
    {
        return PoliticalSuccessionSystem.TryPropose(
            this,
            decisionId,
            officeId,
            startAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalOfficeSuccession(
        PoliticalOfficeSuccessionTransition transition,
        out PoliticalSuccessionFailure failure)
    {
        return PoliticalSuccessionSystem.TryApply(this, transition, out failure);
    }

    public bool TryProposePoliticalSuccession(
        PoliticalDecisionId decisionId,
        OfficeId officeId,
        long startAbsoluteDay,
        out PoliticalOfficeSuccessionTransition transition,
        out PoliticalSuccessionFailure failure)
    {
        return TryProposePoliticalOfficeSuccession(
            decisionId,
            officeId,
            startAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyPoliticalSuccession(
        PoliticalOfficeSuccessionTransition transition,
        out PoliticalSuccessionFailure failure)
    {
        return TryApplyPoliticalOfficeSuccession(transition, out failure);
    }

    public bool TryProposeEstateSuccession(
        EstateId estateId,
        PropertyId propertyId,
        PersonId selectedCandidateId,
        long transferAbsoluteDay,
        out EstateSuccessionTransition transition,
        out EstateSuccessionFailure failure)
    {
        return EstateSuccessionSystem.TryPropose(
            this,
            estateId,
            propertyId,
            selectedCandidateId,
            transferAbsoluteDay,
            out transition,
            out failure);
    }

    public bool TryApplyEstateSuccession(
        EstateSuccessionTransition transition,
        out EstateSuccessionFailure failure)
    {
        string[] changedSections =
        {
            PropertyOwnershipCensusProvider.OwnershipSectionId,
            PropertyOwnershipCensusProvider.TransferHistorySectionId
        };
        SimulationOperationScope p12Scope = null;
        if (!TryBeginP12PropertyEstateOwnerCommit(
                DailyV1RuntimeProtocolOperationContracts.PropertyOwnerCommit,
                changedSections,
                out p12Scope))
        {
            failure = EstateSuccessionFailure.Create(
                EstateSuccessionFailureCode.RuntimeFaulted,
                "The P12 property census operation could not be admitted.");
            return false;
        }

        try
        {
            bool applied = EstateSuccessionSystem.TryApply(this, transition, out failure);
            if (applied)
                NotifyP12PropertyEstateOwnerCommit(changedSections);
            return applied;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryRegisterOffice(
        OfficeRecord record,
        out InstitutionFoundationFailure failure)
    {
        string[] changedSections = GetP12OfficeCensusSectionIds();
        SimulationOperationScope p12Scope = null;
        if (!TryBeginP12InstitutionOfficeOwnerCommit(changedSections, out p12Scope))
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.RuntimeFaulted,
                "The P12 Office census operation could not be admitted.");
            return false;
        }

        try
        {
            bool registered = officeStore.TryRegister(record, out failure);
            if (registered)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12InstitutionOfficeOwnerCommit(changedSections);
            }

            return registered;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryGetInstitution(
        InstitutionId institutionId,
        out InstitutionRecord record)
    {
        return institutionStore.TryGet(institutionId, out record);
    }

    public bool TryGetOffice(
        OfficeId officeId,
        out OfficeRecord record)
    {
        return officeStore.TryGet(officeId, out record);
    }

    public bool TryGetOfficeIncumbency(
        OfficeId officeId,
        out OfficeIncumbency incumbency)
    {
        return officeStore.TryGetIncumbency(officeId, out incumbency);
    }

    public bool TryGetCurrentOfficeIncumbent(
        OfficeId officeId,
        out PersonId incumbent)
    {
        return officeStore.TryGetCurrentIncumbent(officeId, out incumbent);
    }

    public bool IsOfficeVacant(OfficeId officeId)
    {
        return officeStore.IsVacant(officeId);
    }

    internal bool TryGetLatestClosedOfficeTenure(
        OfficeId officeId,
        out OfficeTenureRecord tenure)
    {
        return InstitutionOfficeOwnerBridge.TryGetLatestClosedTenure(officeStore, officeId, out tenure);
    }

    public bool TryAssignIncumbent(
        OfficeId officeId,
        PersonId incumbent,
        long? startAbsoluteDay,
        out InstitutionFoundationFailure failure)
    {
        string[] changedSections = GetP12OfficeCensusSectionIds();
        SimulationOperationScope p12Scope = null;
        if (!TryBeginP12InstitutionOfficeOwnerCommit(changedSections, out p12Scope))
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.RuntimeFaulted,
                "The P12 Office census operation could not be admitted.");
            return false;
        }

        try
        {
            if (officeStore.TryGet(officeId, out _) == false
                || incumbent == null
                || (startAbsoluteDay.HasValue && startAbsoluteDay.Value < 0L)
                || officeStore.TryGetIncumbency(officeId, out _))
            {
                return officeStore.TryAssignIncumbent(
                    officeId,
                    incumbent,
                    startAbsoluteDay,
                    out failure);
            }

            if (personStore.TryGet(incumbent, out _) == false)
            {
                failure = InstitutionFoundationFailure.Create(
                    InstitutionFoundationFailureCode.PersonNotRegistered,
                    "The incumbent PersonId must be registered in this world.");
                return false;
            }

            bool assigned = officeStore.TryAssignIncumbent(
                officeId,
                incumbent,
                startAbsoluteDay,
                out failure);
            if (assigned)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12InstitutionOfficeOwnerCommit(changedSections);
            }

            return assigned;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryAssignIncumbent(
        OfficeId officeId,
        PersonId incumbent,
        out InstitutionFoundationFailure failure)
    {
        return TryAssignIncumbent(
            officeId,
            incumbent,
            CurrentDay,
            out failure);
    }

    public bool TryVacateOffice(
        OfficeId officeId,
        out InstitutionFoundationFailure failure)
    {
        string[] changedSections = GetP12OfficeCensusSectionIds();
        SimulationOperationScope p12Scope = null;
        if (!TryBeginP12InstitutionOfficeOwnerCommit(changedSections, out p12Scope))
        {
            failure = InstitutionFoundationFailure.Create(
                InstitutionFoundationFailureCode.RuntimeFaulted,
                "The P12 Office census operation could not be admitted.");
            return false;
        }

        try
        {
            bool vacated = officeStore.TryVacateOffice(officeId, out failure);
            if (vacated)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12InstitutionOfficeOwnerCommit(changedSections);
            }

            return vacated;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool TryProposeInstitutionalVacancyRecognition(
        OfficeId officeId,
        InstitutionalVacancyRecognitionReason reason,
        out InstitutionalVacancyRecognitionTransition transition,
        out InstitutionalVacancyRecognitionFailure failure)
    {
        if (reason == InstitutionalVacancyRecognitionReason.FactualDeath)
        {
            transition = null;
            if (officeStore.TryGetIncumbency(officeId, out OfficeIncumbency incumbency) == false)
            {
                failure = InstitutionalVacancyRecognitionFailure.StaleIncumbency;
                return false;
            }

            if (personStore.TryGet(incumbency.Incumbent, out PersonRuntime incumbent) == false)
            {
                failure = InstitutionalVacancyRecognitionFailure.IncumbentNotRegistered;
                return false;
            }

            if (incumbent.DeathAbsoluteDay.HasValue == false
                || incumbent.DeathAbsoluteDay.Value > CurrentDay)
            {
                failure = InstitutionalVacancyRecognitionFailure.IncumbentNotFactuallyDead;
                return false;
            }
        }

        return InstitutionalVacancyRecognitionSystem.TryPropose(
            officeStore,
            officeId,
            CurrentDay,
            reason,
            out transition,
            out failure);
    }

    public bool TryApplyInstitutionalVacancyRecognition(
        InstitutionalVacancyRecognitionTransition transition,
        out InstitutionalVacancyRecognitionFailure failure)
    {
        string[] changedSections = GetP12OfficeCensusSectionIds();
        SimulationOperationScope p12Scope = null;
        if (!TryBeginP12InstitutionOfficeOwnerCommit(changedSections, out p12Scope))
        {
            failure = InstitutionalVacancyRecognitionFailure.RuntimeFaulted;
            return false;
        }

        try
        {
            bool applied = InstitutionalVacancyRecognitionSystem.TryApply(
                officeStore,
                transition,
                out failure);
            if (applied)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12InstitutionOfficeOwnerCommit(changedSections);
            }

            return applied;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    private static string[] GetP12OfficeCensusSectionIds() => new[]
    {
        InstitutionOfficeCensusProvider.OfficesSectionId,
        InstitutionOfficeCensusProvider.IncumbenciesSectionId,
        InstitutionOfficeCensusProvider.TenuresSectionId
    };

    public IReadOnlyList<OfficeRecord> GetVacantOffices()
    {
        return officeStore.GetVacantOffices();
    }

    public IReadOnlyList<OfficeRecord> GetOfficesForInstitution(
        InstitutionId institutionId)
    {
        return officeStore.GetOfficesForInstitution(institutionId);
    }

    public IReadOnlyList<OfficeRecord> GetOfficesHeldBy(PersonId personId)
    {
        return officeStore.GetOfficesHeldBy(personId);
    }

    public bool TryProposeNamedBirth(
        CityRuntime settlement,
        PersonId personId,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        return PersonBirthLifecycleSystem.TryProposeNamedBirth(
            this,
            settlement,
            personId,
            out transition,
            out failure);
    }

    public bool TryProposeNamedBirth(
        CityRuntime settlement,
        PersonId personId,
        System.Collections.Generic.IEnumerable<PersonId> parentIds,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        return PersonBirthLifecycleSystem.TryProposeNamedBirth(
            this,
            settlement,
            personId,
            parentIds,
            out transition,
            out failure);
    }

    public bool TryApplyNamedBirth(
        PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        return PersonBirthLifecycleSystem.TryApplyNamedBirth(this, transition, out failure);
    }

    public bool TryApplyNamedBirth(
        CityRuntime settlement,
        PersonId personId,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        return PersonBirthLifecycleSystem.TryApplyNamedBirth(
            this,
            settlement,
            personId,
            out transition,
            out failure);
    }

    public bool TryApplyNamedBirth(
        CityRuntime settlement,
        PersonId personId,
        System.Collections.Generic.IEnumerable<PersonId> parentIds,
        out PersonBirthTransition transition,
        out PersonBirthLifecycleFailure failure)
    {
        bool applied = PersonBirthLifecycleSystem.TryApplyNamedBirth(
            this,
            settlement,
            personId,
            parentIds,
            out transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryAddParentage(
        PersonId parentId,
        PersonId childId,
        out PersonGenealogyFailure failure)
    {
        return TryApplyP12GenealogyMutation(true, parentId, childId, out failure);
    }

    public bool TryRemoveParentage(
        PersonId parentId,
        PersonId childId,
        out PersonGenealogyFailure failure)
    {
        return TryApplyP12GenealogyMutation(false, parentId, childId, out failure);
    }

    internal bool TryApplyP12GenealogyMutation(
        bool add,
        PersonId parentId,
        PersonId childId,
        out PersonGenealogyFailure failure)
    {
        SimulationOperationScope p12Scope;
        if (!TryBeginP12GenealogyOwnerCommit(out p12Scope))
        {
            failure = PersonGenealogyFailure.RuntimeFaulted;
            return false;
        }

        try
        {
            bool committed = add
                ? PersonGenealogySystem.TryAddParentage(this, parentId, childId, out failure)
                : PersonGenealogySystem.TryRemoveParentage(this, parentId, childId, out failure);
            if (committed)
            {
                AdvancePoliticalWorldRevision();
                NotifyP12GenealogyOwnerCommit();
            }

            return committed;
        }
        catch
        {
            FaultRuntimeAdmission();
            throw;
        }
        finally
        {
            p12Scope?.Dispose();
        }
    }

    public bool ContainsParentage(PersonId parentId, PersonId childId)
    {
        return genealogyStore.ContainsParentage(parentId, childId);
    }

    public IReadOnlyList<PersonId> GetGenealogyParents(PersonId childId)
    {
        return genealogyStore.GetParents(childId);
    }

    public IReadOnlyList<PersonId> GetGenealogyChildren(PersonId parentId)
    {
        return genealogyStore.GetChildren(parentId);
    }

    public IReadOnlyList<PersonId> GetGenealogyAncestors(PersonId personId)
    {
        return genealogyStore.GetAncestors(personId);
    }

    public IReadOnlyList<PersonId> GetGenealogyDescendants(PersonId personId)
    {
        return genealogyStore.GetDescendants(personId);
    }

    public bool IsGenealogyDirectParent(PersonId parentId, PersonId childId)
    {
        return genealogyStore.IsDirectParent(parentId, childId);
    }

    public bool IsGenealogyAncestorOf(PersonId ancestorId, PersonId descendantId)
    {
        return genealogyStore.IsAncestorOf(ancestorId, descendantId);
    }

    public bool TryMaterializePerson(
        PersonId personId,
        NpcData npcData,
        string runtimeId,
        CityRuntime startingCity,
        float initialMoney,
        out NpcRuntime npcRuntime,
        out PersonMaterializationFailure failure)
    {
        IReadOnlyList<string> allowedLifecycleIds = personId == null
            ? Array.Empty<string>()
            : new[] { PersonLifeResidenceCensusProvider.SectionIdFor(personId) };
        using (NpcMembershipCensusScope censusScope = BeginNpcMembershipCensusScopeWithLifecycleSections(allowedLifecycleIds))
        {
            if (!censusScope.CanProceed)
            {
                npcRuntime = null;
                failure = PersonMaterializationFailure.RuntimeFaulted;
                return false;
            }
            bool materialized = PersonMaterializationSystem.TryMaterializePerson(
                this,
                personId,
                npcData,
                runtimeId,
                startingCity,
                initialMoney,
                out npcRuntime,
                out failure);
            if (materialized && startingCity != null)
                censusScope.MarkCityPresenceChanged(startingCity);
            return materialized;
        }
    }

    internal bool HasSameJusticeSystemOwner(JusticeSystem otherJusticeSystem)
    {
        return ReferenceEquals(justiceSystem, otherJusticeSystem);
    }

    internal bool HasSameLoggerOwner(SimulationLogger otherLogger)
    {
        return ReferenceEquals(logger, otherLogger);
    }

    internal bool HasSameRuntimeAdmissionContext(SimulationRuntimeAdmissionContext otherContext)
    {
        return ReferenceEquals(runtimeAdmissionContext, otherContext);
    }

    public bool TryBindExistingNpcToPerson(
        PersonId personId,
        string npcRuntimeId,
        out PersonMaterializationFailure failure)
    {
        IReadOnlyList<string> allowedLifecycleIds = personId == null
            ? Array.Empty<string>()
            : new[] { PersonLifeResidenceCensusProvider.SectionIdFor(personId) };
        using (NpcMembershipCensusScope censusScope = BeginNpcMembershipCensusScopeWithLifecycleSections(allowedLifecycleIds))
        {
            if (!censusScope.CanProceed)
            {
                failure = PersonMaterializationFailure.RuntimeFaulted;
                return false;
            }

            IReadOnlyList<DailyV1ActorTurnRosterProjection> expectedRoster = null;
            IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster = null;
            DailyV1SelectedRuntimeOwnerGraph selectedGraph = SelectedRuntimeOwnerGraph;
            if (selectedGraph != null
                && personId != null
                && personStore.TryGet(personId, out PersonRuntime registeredPerson)
                && TryGetNpcRuntime(npcRuntimeId, out NpcRuntime registeredNpc)
                && registeredNpc.PersonId == null)
            {
                if (!dailyExecutionBoundary.TryCreateSelectedRuntimeActorRosterWithPersonBinding(
                        npcRuntimeId,
                        registeredPerson.PersonId,
                        out expectedRoster,
                        out replacementRoster)
                    || !dailyExecutionBoundary.TryCommitSelectedRuntimeActorRoster(
                        expectedRoster, replacementRoster))
                {
                    FaultNpcMembershipCensusBoundary();
                    failure = PersonMaterializationFailure.RuntimeFaulted;
                    return false;
                }
            }

            bool bound;
            try
            {
                bound = PersonMaterializationSystem.TryBindExistingNpcToPerson(
                    this,
                    personId,
                    npcRuntimeId,
                    out failure);
            }
            catch
            {
                if (replacementRoster != null)
                {
                    // The binder may have thrown after changing one or more
                    // authoritative owners. Restoring only the derived roster
                    // projection cannot prove that those owner writes were
                    // rolled back, so fail closed for both census and mutation.
                    dailyExecutionBoundary.TryCommitSelectedRuntimeActorRoster(
                        replacementRoster, expectedRoster);
                    FaultNpcMembershipCensusBoundary();
                    mutationGuard.MarkFaulted(
                        AuthoritativeMutationFaultReason.RollbackRestoreFailed);
                }
                throw;
            }

            if (!bound && replacementRoster != null
                && !dailyExecutionBoundary.TryCommitSelectedRuntimeActorRoster(
                    replacementRoster, expectedRoster))
            {
                FaultNpcMembershipCensusBoundary();
                failure = PersonMaterializationFailure.RuntimeFaulted;
                return false;
            }

            return bound;
        }
    }

    public bool TryBindExistingPersonResident(
        PersonId personId,
        CityRuntime settlement,
        out PersonResidenceMembershipFailure failure)
    {
        if (personId == null || personStore.TryGet(personId, out PersonRuntime person) == false)
        {
            failure = PersonResidenceMembershipFailure.PersonNotRegistered;
            return false;
        }

        if (runtimeAdmissionContext != null)
        {
            string sectionId = PersonLifeResidenceCensusProvider.SectionIdFor(personId);
            if (!TryBeginP12PopulationOperation(
                    DailyV1RuntimeProtocolOperationContracts.PersonResidenceBind,
                    new[] { sectionId },
                    () => PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(person, 1),
                    out P12PopulationOperationScope p12Scope))
            {
                failure = PersonResidenceMembershipFailure.RuntimeFaulted;
                return false;
            }

            using (p12Scope)
            {
                bool bound = PersonResidenceMembershipSystem.TryBindExistingResident(
                    person,
                    settlement,
                    this,
                    out failure);
                p12Scope?.Complete(bound);
                return bound;
            }
        }

        return PersonResidenceMembershipSystem.TryBindExistingResident(
            person,
            settlement,
            this,
            out failure);
    }

    /// <summary>
    /// Creates a fresh immutable authoritative roster from the world-owned registry.
    /// Callers never provide the source collection.
    /// </summary>
    public AuthoritativeNpcRoster GetAuthoritativeNpcRoster()
    {
        return new AuthoritativeNpcRoster(npcRuntimeSnapshot);
    }

    public bool TryApplyImmigration(
        NpcRuntime npcRuntime,
        CityRuntime settlement,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (runtimeAdmissionContext != null
            && TryBuildP12NpcPopulationOperation(
                npcRuntime,
                settlement,
                residentDeath: false,
                out List<string> sectionIds,
                out Func<bool> localRevisionCapacity))
        {
            if (!TryBeginP12PopulationOperation(
                    DailyV1RuntimeProtocolOperationContracts.PopulationImmigration,
                    sectionIds,
                    localRevisionCapacity,
                    out P12PopulationOperationScope p12Scope))
            {
                transition = null;
                failure = NpcPopulationLifecycleFailure.RuntimeFaulted;
                return false;
            }

            using (p12Scope)
            {
                bool admitted = NpcPopulationLifecycleSystem.TryApplyImmigration(
                    npcRuntime,
                    settlement,
                    GetAuthoritativeNpcRoster(),
                    out transition,
                    out failure);
                if (admitted) AdvancePoliticalWorldRevision();
                p12Scope?.Complete(admitted);
                return admitted;
            }
        }

        bool applied = NpcPopulationLifecycleSystem.TryApplyImmigration(
            npcRuntime,
            settlement,
            GetAuthoritativeNpcRoster(),
            out transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryApplyEmigration(
        NpcRuntime npcRuntime,
        CityRuntime settlement,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (runtimeAdmissionContext != null
            && TryBuildP12NpcPopulationOperation(
                npcRuntime,
                settlement,
                residentDeath: false,
                out List<string> sectionIds,
                out Func<bool> localRevisionCapacity))
        {
            if (!TryBeginP12PopulationOperation(
                    DailyV1RuntimeProtocolOperationContracts.PopulationEmigration,
                    sectionIds,
                    localRevisionCapacity,
                    out P12PopulationOperationScope p12Scope))
            {
                transition = null;
                failure = NpcPopulationLifecycleFailure.RuntimeFaulted;
                return false;
            }

            using (p12Scope)
            {
                bool admitted = NpcPopulationLifecycleSystem.TryApplyEmigration(
                    npcRuntime,
                    settlement,
                    GetAuthoritativeNpcRoster(),
                    out transition,
                    out failure);
                if (admitted) AdvancePoliticalWorldRevision();
                p12Scope?.Complete(admitted);
                return admitted;
            }
        }

        bool applied = NpcPopulationLifecycleSystem.TryApplyEmigration(
            npcRuntime,
            settlement,
            GetAuthoritativeNpcRoster(),
            out transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryApplyResidenceMigration(
        NpcRuntime npcRuntime,
        CityRuntime origin,
        CityRuntime destination,
        NpcResidenceMigrationTransition transition,
        out NpcResidenceMigrationFailure failure)
    {
        if (runtimeAdmissionContext != null
            && TryBuildP12ResidenceMigrationOperation(
                npcRuntime,
                origin,
                destination,
                out List<string> sectionIds,
                out Func<bool> localRevisionCapacity))
        {
            if (!TryBeginP12PopulationOperation(
                    DailyV1RuntimeProtocolOperationContracts.PopulationResidenceMigration,
                    sectionIds,
                    localRevisionCapacity,
                    out P12PopulationOperationScope p12Scope))
            {
                failure = NpcResidenceMigrationFailure.RuntimeFaulted;
                return false;
            }

            using (p12Scope)
            {
                bool admitted = NpcResidenceMigrationSystem.TryApply(
                    npcRuntime,
                    origin,
                    destination,
                    GetAuthoritativeNpcRoster(),
                    transition,
                    out failure);
                if (admitted) AdvancePoliticalWorldRevision();
                p12Scope?.Complete(admitted);
                return admitted;
            }
        }

        return NpcResidenceMigrationSystem.TryApply(
            npcRuntime,
            origin,
            destination,
            GetAuthoritativeNpcRoster(),
            transition,
            out failure);
    }

    public bool TryApplyResidentDeath(
        NpcRuntime npcRuntime,
        CityRuntime settlement,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
        if (runtimeAdmissionContext != null
            && TryBuildP12NpcPopulationOperation(
                npcRuntime,
                settlement,
                residentDeath: true,
                out List<string> sectionIds,
                out Func<bool> localRevisionCapacity))
        {
            if (!TryBeginP12PopulationOperation(
                    DailyV1RuntimeProtocolOperationContracts.PopulationResidentDeath,
                    sectionIds,
                    localRevisionCapacity,
                    out P12PopulationOperationScope p12Scope))
            {
                transition = null;
                failure = NpcPopulationLifecycleFailure.RuntimeFaulted;
                return false;
            }

            using (p12Scope)
            {
                bool admitted;
                if (npcRuntime.BoundPersonRuntime != null)
                {
                    admitted = NpcPopulationLifecycleSystem.TryApplyResidentPersonDeath(
                        this,
                        npcRuntime,
                        settlement,
                        GetAuthoritativeNpcRoster(),
                        out transition,
                        out failure);
                }
                else
                {
                    admitted = NpcPopulationLifecycleSystem.TryApplyResidentDeath(
                        npcRuntime,
                        settlement,
                        GetAuthoritativeNpcRoster(),
                        out transition,
                        out failure);
                }
                if (admitted) AdvancePoliticalWorldRevision();
                p12Scope?.Complete(admitted);
                return admitted;
            }
        }

        if (npcRuntime?.BoundPersonRuntime != null)
        {
            bool applied = NpcPopulationLifecycleSystem.TryApplyResidentPersonDeath(
                this,
                npcRuntime,
                settlement,
                GetAuthoritativeNpcRoster(),
                out transition,
                out failure);
            if (applied)
            {
                AdvancePoliticalWorldRevision();
            }

            return applied;
        }

        bool residentDeathApplied = NpcPopulationLifecycleSystem.TryApplyResidentDeath(
            npcRuntime,
            settlement,
            GetAuthoritativeNpcRoster(),
            out transition,
            out failure);
        if (residentDeathApplied)
        {
            AdvancePoliticalWorldRevision();
        }

        return residentDeathApplied;
    }

    public bool TryStartTravelParty(ActionExecutionContext context)
    {
        if (mutationGuard.CanMutate == false)
        {
            return false;
        }

        if (context == null)
        {
            return false;
        }

        if (expeditionSystem != null)
        {
            foreach (ActionExecutionParticipant participant in context.Participants)
            {
                if (participant != null && IsDeadNpc(participant.RuntimeId) == true)
                {
                    return false;
                }

                if (participant != null
                    && expeditionSystem.IsNpcOnActiveExpedition(participant.RuntimeId) == true)
                {
                    return false;
                }
            }
        }

        if (travelPartySystem == null) return false;
        return runtimeAdmissionContext == null
            ? travelPartySystem.TryStartTravelParty(context)
            : TryStartP12TravelParty(context);
    }

    private bool IsDailyCaptureProfile => runtimeAdmissionContext != null
        && runtimeAdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1;

    internal bool HasExactSelectedRuntimeOwnerGraph(DailyV1EffectiveDefinitions definitions) =>
        dailyExecutionBoundary.HasExactSelectedRuntimeOwnerGraph(
            definitions,
            cities.Select(city => city?.SelectedRuntimeOwners),
            npcRuntimes.Select(npc => npc?.SelectedRuntimeOwners).Where(owner => owner != null));

    /// <summary>
    /// Adopts the exact Core graph used during unpublished genesis composition.
    /// The runtime constructor has already bound NPC memberships, so the graph
    /// can complete its one-time PersonStore binding without rebuilding the
    /// owner join. The runtime must not have crossed a completed boundary.
    /// </summary>
    internal bool TryAdoptInitialSelectedRuntimeOwnerGraph(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        DailyV1SelectedRuntimeOwnerGraph current = dailyExecutionBoundary.SelectedRuntimeOwnerGraph;
        DailyV1JusticeRecordsOwner exactJusticeOwner = justiceSystem?.CoreOwner;
        if (graph == null || current == null
            || runtimeAdmissionContext?.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || dailyExecutionBoundary.CompletedCoreSequence != 0L
            || dailyExecutionBoundary.AdvanceLeaseHeld
            || !ReferenceEquals(current.Definitions, graph.Definitions)
            || !ReferenceEquals(current.SpatialNetwork, graph.SpatialNetwork)
            || !ReferenceEquals(graph.JusticeRecordsOwner, exactJusticeOwner)
            || !ReferenceEquals(graph.TravelParties,
                travelPartySystem?.Store ?? travelSystem?.SelectedTravelPartyStore)
            || !ReferenceEquals(graph.ScheduledDirectives,
                scheduledDirectiveSystem?.Store)
            || !ReferenceEquals(current.RuntimeIdentityIndex,
                p12RuntimeIdentityRegistry?.CoreIdentityIndex)
            || !ReferenceEquals(graph.RuntimeIdentityIndex, current.RuntimeIdentityIndex)
            || !ReferenceEquals(current.PersonStore, personStore)
            || !graph.MatchesExactOwnerInstances(
                cities.Select(city => city?.SelectedRuntimeOwners),
                npcRuntimes.Select(npc => npc?.SelectedRuntimeOwners).Where(owner => owner != null)))
            return false;

        if (!graph.TryBindPersonStore(personStore))
            return false;
        if (!graph.TryBindPersonSpatialPositionStore(personSpatialPositionStore)
            || !graph.TryBindLegacySpatialAnchorBindingStore(legacySpatialAnchorBindingStore))
            return false;
        if (current.Expeditions != null && !graph.TryBindExpeditionStore(current.Expeditions))
            return false;

        return dailyExecutionBoundary.TryReplaceSelectedRuntimeOwnerGraph(current, graph);
    }

    private DailyV1SelectedRuntimeOwnerGraph TryCreateSelectedRuntimeOwnerGraph(
        IReadOnlyList<CityRuntime> cityRuntimes,
        IReadOnlyList<NpcRuntime> npcRuntimes,
        SpatialNetworkOwner spatialNetwork,
        DailyV1JusticeRecordsOwner justiceRecordsOwner,
        TravelPartyStore travelParties,
        PersonStore personStore)
    {
        if (cityRuntimes == null || npcRuntimes == null) return null;
        if (spatialNetwork != null)
        {
            foreach (CityRuntime city in cityRuntimes)
            {
                DailyV1CityRuntimeOwners cityOwners = city?.SelectedRuntimeOwners;
                if (cityOwners == null) continue;
                SpatialLocationRuntime projectedLocation = city.Location;
                if (projectedLocation == null
                    || !string.Equals(cityOwners.LocationRuntimeId, projectedLocation.RuntimeId, StringComparison.Ordinal)
                    || !spatialNetwork.TryGetLocation(projectedLocation.RuntimeId, out SpatialLocationRuntime ownedLocation)
                    || !ReferenceEquals(projectedLocation, ownedLocation))
                    return null;
            }
        }
        return DailyV1SelectedRuntimeOwnerGraph.TryCreateFromExactOwners(
            cityRuntimes.Select(city => city?.SelectedRuntimeOwners),
            npcRuntimes.Select(npc => npc?.SelectedRuntimeOwners).Where(owner => owner != null),
            spatialNetwork,
            justiceRecordsOwner,
            travelParties,
            p12RuntimeIdentityRegistry?.CoreIdentityIndex,
            personStore,
            out DailyV1SelectedRuntimeOwnerGraph graph) ? graph : null;
    }

    internal bool RequiresSelectedDailyV1DefinitionBindings => IsDailyCaptureProfile;

    private bool HasActiveDailyOperationContext()
    {
        return activeNpcMembershipCensusContext != null
            || activeP12MerchantOperationContext != null
            || activeP12TravelPartyAdvanceOperationContext != null
            || activeP12SoloTravelStartOperationContext != null
            || activeP12TravelPartyStartOperationContext != null;
    }

    private bool TryReadDailyCaptureEvidence(
        bool finalizingHeldAdvance,
        DailyV1CaptureScopeAllowance allowedOuterScope,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure)
    {
        ownerSections = null;
        mutationEpoch = 0L;
        failure = DailyCaptureEligibilityFailure.None;
        if (!IsDailyCaptureProfile)
        {
            failure = DailyCaptureEligibilityFailure.UnsupportedProfile;
            return false;
        }

        if (!IsRuntimeAdmissionOwnerThreadCurrent())
        {
            failure = DailyCaptureEligibilityFailure.WrongOwnerThread;
            return false;
        }

        if (advanceLeaseHeld != finalizingHeldAdvance
            || finalizingHeldAdvance != (allowedOuterScope == DailyV1CaptureScopeAllowance.DailyAdvance)
            || HasActiveDailyOperationContext())
        {
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        if (!dailyExecutionBoundary.IsFactualReadWorldPublished || WorldId == null)
        {
            failure = DailyCaptureEligibilityFailure.WorldNotPublished;
            return false;
        }

        if (!mutationGuard.CanMutate || npcRosterCensusProtocol == null)
        {
            failure = DailyCaptureEligibilityFailure.RuntimeFaulted;
            return false;
        }

        if (!npcRosterCensusProtocol.TryCaptureQuiescentOwnerSectionSnapshot(
            allowedOuterScope,
            out ownerSections,
            out mutationEpoch,
            out ContinuationCensusFailure censusFailure))
        {
            failure = MapDailyCaptureFailure(censusFailure);
            return false;
        }

        if (advanceLeaseHeld != finalizingHeldAdvance
            || finalizingHeldAdvance != (allowedOuterScope == DailyV1CaptureScopeAllowance.DailyAdvance)
            || HasActiveDailyOperationContext())
        {
            ownerSections = null;
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        return true;
    }

    private static DailyCaptureEligibilityFailure MapDailyCaptureFailure(
        ContinuationCensusFailure failure)
    {
        if (failure == ContinuationCensusFailure.WrongOwnerThread
            || failure == ContinuationCensusFailure.OwnerThreadUnbound)
            return DailyCaptureEligibilityFailure.WrongOwnerThread;
        if (failure == ContinuationCensusFailure.OperationInProgress)
            return DailyCaptureEligibilityFailure.OperationInProgress;
        if (failure == ContinuationCensusFailure.OwnerCoverageIncomplete)
            return DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
        return DailyCaptureEligibilityFailure.RuntimeFaulted;
    }

    private bool TryCompareDailyCaptureToken(
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> currentOwnerSections,
        long currentMutationEpoch)
    {
        return dailyExecutionBoundary.TryCompareCurrentCaptureToken(
            token,
            runtimeAdmissionContext,
            configuration,
            calendar,
            compositionProfile,
            WorldId,
            CurrentDay,
            currentOwnerSections,
            currentMutationEpoch);
    }

    private bool TryCaptureCompletedDailyCaptureEvidence(
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure) =>
        TryReadDailyCaptureEvidence(
            finalizingHeldAdvance: true,
            DailyV1CaptureScopeAllowance.DailyAdvance,
            out ownerSections,
            out mutationEpoch,
            out failure);

    internal bool TryAdmitRestoredDailyBoundary(
        WorldId expectedWorldId,
        long preservedAbsoluteDay,
        long preservedCompletedCoreSequence,
        out DailyCaptureEligibilityFailure failure)
    {
        return TryAdmitRestoredDailyBoundary(
            expectedWorldId,
            preservedAbsoluteDay,
            preservedCompletedCoreSequence,
            null,
            -1L,
            out failure);
    }

    /// <summary>
    /// Admits a private restored candidate only if its fresh owner census still
    /// exactly matches the vector and mutation epoch that passed P12-G graph
    /// validation. The token retains that validated vector instance.
    /// </summary>
    internal bool TryAdmitRestoredDailyBoundary(
        WorldId expectedWorldId,
        long preservedAbsoluteDay,
        long preservedCompletedCoreSequence,
        IReadOnlyList<OwnerSectionCensusSnapshot> validatedTargetOwnerSections,
        long validatedTargetMutationEpoch,
        out DailyCaptureEligibilityFailure failure)
    {
        if (!TryPrepareRestoredDailyBoundaryAdmission(
                expectedWorldId, preservedAbsoluteDay, preservedCompletedCoreSequence,
                validatedTargetOwnerSections, validatedTargetMutationEpoch,
                out IReadOnlyList<OwnerSectionCensusSnapshot> currentOwnerSections,
                out long currentMutationEpoch, out failure))
            return false;

        IReadOnlyList<OwnerSectionCensusSnapshot> tokenOwnerSections =
            validatedTargetOwnerSections ?? currentOwnerSections;
        if (!dailyExecutionBoundary.TryAdmitRestoredContinuationBoundary(
                runtimeAdmissionContext,
                configuration,
                calendar,
                compositionProfile,
                expectedWorldId,
                preservedAbsoluteDay,
                preservedCompletedCoreSequence,
                currentMutationEpoch,
                tokenOwnerSections,
                out DailyCaptureEligibilityToken token))
        {
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        failure = DailyCaptureEligibilityFailure.None;
        return true;
    }

    /// <summary>
    /// Performs host-specific quiescence and runtime-readiness checks without
    /// creating a Core capture token. Core compares this final owner vector,
    /// then installs the restored continuation token itself.
    /// </summary>
    internal bool TryPrepareRestoredDailyBoundaryAdmission(
        WorldId expectedWorldId,
        long preservedAbsoluteDay,
        long preservedCompletedCoreSequence,
        IReadOnlyList<OwnerSectionCensusSnapshot> validatedTargetOwnerSections,
        long validatedTargetMutationEpoch,
        out IReadOnlyList<OwnerSectionCensusSnapshot> currentOwnerSections,
        out long currentMutationEpoch,
        out DailyCaptureEligibilityFailure failure)
    {
        currentOwnerSections = null;
        currentMutationEpoch = -1L;
        failure = DailyCaptureEligibilityFailure.None;
        if (!IsDailyCaptureProfile)
        {
            failure = DailyCaptureEligibilityFailure.UnsupportedProfile;
            return false;
        }

        if (!IsRuntimeAdmissionOwnerThreadCurrent())
        {
            failure = DailyCaptureEligibilityFailure.WrongOwnerThread;
            return false;
        }

        if (expectedWorldId == null
            || !ReferenceEquals(expectedWorldId, WorldId)
            || preservedAbsoluteDay < 0L
            || initialAbsoluteDay != preservedAbsoluteDay
            || currentDailyCaptureToken != null
            || completedDailyCoreSequence != 0L
            || dailyExecutionBoundary.IsFactualReadWorldPublished)
        {
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        if (preservedCompletedCoreSequence <= 0L)
        {
            failure = DailyCaptureEligibilityFailure.NoCompletedBoundary;
            return false;
        }

        if (advanceLeaseHeld || HasActiveDailyOperationContext())
        {
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        if (!coreMutationGuardAuthoritiesBound
            || !mutationGuard.CanMutate
            || npcRosterCensusProtocol == null
            || factualReadCoordinator == null
            || configuration == null
            || calendar == null)
        {
            failure = DailyCaptureEligibilityFailure.RuntimeFaulted;
            return false;
        }

        if (!npcRosterCensusProtocol.TryCaptureQuiescentOwnerSectionSnapshot(
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out ContinuationCensusFailure censusFailure))
        {
            failure = MapDailyCaptureFailure(censusFailure);
            return false;
        }

        if (validatedTargetOwnerSections != null
            && !P12GDailyV1OwnerVector.TryMatchCurrentTargetSnapshot(
                validatedTargetOwnerSections,
                validatedTargetMutationEpoch,
                ownerSections,
                mutationEpoch,
                out _,
                out _))
        {
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        if (!IsRuntimeAdmissionOwnerThreadCurrent())
        {
            failure = DailyCaptureEligibilityFailure.WrongOwnerThread;
            return false;
        }

        if (advanceLeaseHeld || HasActiveDailyOperationContext())
        {
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        if (!mutationGuard.CanMutate || dailyExecutionBoundary.IsFactualReadWorldPublished)
        {
            failure = DailyCaptureEligibilityFailure.RuntimeFaulted;
            return false;
        }

        if (!ReferenceEquals(expectedWorldId, WorldId)
            || initialAbsoluteDay != preservedAbsoluteDay
            || currentDailyCaptureToken != null
            || completedDailyCoreSequence != 0L)
        {
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        currentOwnerSections = ownerSections;
        currentMutationEpoch = mutationEpoch;
        return true;
    }

    internal bool TryGetCompletedDailyCaptureToken(
        out DailyCaptureEligibilityToken token,
        out DailyCaptureEligibilityFailure failure) =>
        TryGetCompletedDailyCaptureTokenCore(
            DailyV1CaptureScopeAllowance.None,
            out token,
            out failure);

    internal bool TryGetCompletedDailyCaptureTokenForContinuationSave(
        out DailyCaptureEligibilityToken token,
        out DailyCaptureEligibilityFailure failure) =>
        TryGetCompletedDailyCaptureTokenCore(
            DailyV1CaptureScopeAllowance.ContinuationSave,
            out token,
            out failure);

    internal bool TryGetCompletedDailyCaptureTokenForSessionReplacement(
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyCaptureEligibilityToken token,
        out DailyCaptureEligibilityFailure failure)
    {
        if (replacementOperation == null
            || !replacementOperation.AuthorizesSessionReplacementCapture(dailyExecutionBoundary))
        {
            token = null;
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        return TryGetCompletedDailyCaptureTokenCore(
            DailyV1CaptureScopeAllowance.SessionReplacementValidation,
            out token,
            out failure);
    }

    private bool TryGetCompletedDailyCaptureTokenCore(
        DailyV1CaptureScopeAllowance allowedOuterScope,
        out DailyCaptureEligibilityToken token,
        out DailyCaptureEligibilityFailure failure)
    {
        token = null;
        if (!TryReadDailyCaptureEvidence(
            finalizingHeldAdvance: false,
            allowedOuterScope,
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out failure))
            return false;

        DailyCaptureEligibilityToken current = currentDailyCaptureToken;
        if (current == null)
        {
            failure = DailyCaptureEligibilityFailure.NoCompletedBoundary;
            return false;
        }

        if (!TryCompareDailyCaptureToken(current, ownerSections, mutationEpoch))
        {
            dailyExecutionBoundary.InvalidateCurrentCaptureToken();
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        token = current;
        failure = DailyCaptureEligibilityFailure.None;
        return true;
    }

    /// <summary>
    /// Captures the private Daily-v1 candidate's own exact owner vector before
    /// restored-boundary admission. This is read-only evidence for P12-G to
    /// compare against its staged owner graph; it does not publish factual
    /// reads or issue a completed-boundary token.
    /// </summary>
    internal bool TryCaptureUnadmittedRestoredDailyOwnerVector(
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out ContinuationCensusFailure failure)
    {
        ownerSections = null;
        mutationEpoch = 0L;

        if (!IsDailyCaptureProfile || npcRosterCensusProtocol == null)
        {
            failure = ContinuationCensusFailure.OwnerCoverageIncomplete;
            return false;
        }

        if (!IsRuntimeAdmissionOwnerThreadCurrent())
        {
            failure = ContinuationCensusFailure.WrongOwnerThread;
            return false;
        }

        if (advanceLeaseHeld || HasActiveDailyOperationContext())
        {
            failure = ContinuationCensusFailure.OperationInProgress;
            return false;
        }

        if (factualReadWorldPublished
            || currentDailyCaptureToken != null
            || completedDailyCoreSequence != 0L
            || !mutationGuard.CanMutate)
        {
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        if (!npcRosterCensusProtocol.TryCaptureQuiescentOwnerSectionSnapshot(
                out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
                out long capturedMutationEpoch,
                out failure))
            return false;

        if (!IsRuntimeAdmissionOwnerThreadCurrent())
        {
            failure = ContinuationCensusFailure.WrongOwnerThread;
            return false;
        }

        if (advanceLeaseHeld || HasActiveDailyOperationContext())
        {
            failure = ContinuationCensusFailure.OperationInProgress;
            return false;
        }

        if (factualReadWorldPublished
            || currentDailyCaptureToken != null
            || completedDailyCoreSequence != 0L
            || !mutationGuard.CanMutate)
        {
            failure = ContinuationCensusFailure.ProtocolFaulted;
            return false;
        }

        ownerSections = captured;
        mutationEpoch = capturedMutationEpoch;
        failure = ContinuationCensusFailure.None;
        return true;
    }

    internal bool IsHealthyDailyOwnerThreadBoundary()
    {
        return IsDailyCaptureProfile
            && IsRuntimeAdmissionOwnerThreadCurrent()
            && !advanceLeaseHeld
            && !HasActiveDailyOperationContext()
            && dailyExecutionBoundary.IsFactualReadWorldPublished
            && mutationGuard.CanMutate;
    }

    internal bool HasValidCompletedDailyBoundaryAdmission()
    {
        return dailyExecutionBoundary.HasValidCompletedDailyBoundaryAdmission(
            this, sessionReplacement: true);
    }

    internal bool HasValidRestoredDailyBoundaryAdmission()
    {
        return dailyExecutionBoundary.HasValidRestoredDailyBoundaryAdmission(this);
    }

    internal bool TryValidateCompletedDailyCaptureToken(
        DailyCaptureEligibilityToken token,
        out DailyCaptureEligibilityFailure failure) =>
        TryValidateCompletedDailyCaptureTokenCore(
            token,
            DailyV1CaptureScopeAllowance.None,
            out failure);

    internal bool TryValidateCompletedDailyCaptureTokenForContinuationSave(
        DailyCaptureEligibilityToken token,
        out DailyCaptureEligibilityFailure failure) =>
        TryValidateCompletedDailyCaptureTokenCore(
            token,
            DailyV1CaptureScopeAllowance.ContinuationSave,
            out failure);

    internal bool TryValidateCompletedDailyCaptureTokenForSessionReplacement(
        DailyCaptureEligibilityToken token,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyCaptureEligibilityFailure failure)
    {
        if (replacementOperation == null
            || !replacementOperation.AuthorizesSessionReplacementCapture(dailyExecutionBoundary))
        {
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        return TryValidateCompletedDailyCaptureTokenCore(
            token,
            DailyV1CaptureScopeAllowance.SessionReplacementValidation,
            out failure);
    }

    /// <summary>
    /// Validates source evidence for P12 owner capture at an idle boundary or
    /// under the one explicitly admitted outer continuation-save scope.
    /// Other active application/runtime scopes remain ineligible.
    /// </summary>
    internal bool TryValidateCompletedDailyCaptureTokenForP12Capture(
        DailyCaptureEligibilityToken token,
        out DailyCaptureEligibilityFailure failure)
    {
        return dailyExecutionBoundary.TryAssessCaptureScope(
                DailyV1CaptureScopeAllowance.ContinuationSave)
            ? TryValidateCompletedDailyCaptureTokenForContinuationSave(token, out failure)
            : TryValidateCompletedDailyCaptureToken(token, out failure);
    }

    internal bool TryValidateCompletedDailyCaptureTokenForP12Capture(
        DailyCaptureEligibilityToken token,
        DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.ExclusiveOperation replacementOperation,
        out DailyCaptureEligibilityFailure failure)
    {
        if (replacementOperation == null)
            return TryValidateCompletedDailyCaptureTokenForP12Capture(token, out failure);
        return TryValidateCompletedDailyCaptureTokenForSessionReplacement(
            token, replacementOperation, out failure);
    }

    private bool TryValidateCompletedDailyCaptureTokenCore(
        DailyCaptureEligibilityToken token,
        DailyV1CaptureScopeAllowance allowedOuterScope,
        out DailyCaptureEligibilityFailure failure)
    {
        if (!TryReadDailyCaptureEvidence(
            finalizingHeldAdvance: false,
            allowedOuterScope,
            out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
            out long mutationEpoch,
            out failure))
            return false;

        if (token == null || !ReferenceEquals(currentDailyCaptureToken, token))
        {
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        if (!TryCompareDailyCaptureToken(token, ownerSections, mutationEpoch))
        {
            dailyExecutionBoundary.InvalidateCurrentCaptureToken();
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        failure = DailyCaptureEligibilityFailure.None;
        return true;
    }

    public void AdvanceDay()
    {
        if (TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure) == false)
        {
            throw CreateAdvanceFailureException(failure);
        }
    }

    public bool TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure)
    {
        if (TryGetBoundDailyV1ApplicationAdvanceAuthority(
                out DailyV1Application application,
                out DailyV1ActiveRuntimeSession session))
            return application.TryAdvanceDay(session, out failure);

        return dailyExecutionBoundary.TryAdvanceDay(this, out failure);
    }

    private bool TryGetBoundDailyV1ApplicationAdvanceAuthority(
        out DailyV1Application application,
        out DailyV1ActiveRuntimeSession session)
    {
        lock (dailyV1ApplicationAdvanceAuthorityGate)
        {
            application = dailyV1ApplicationAdvanceAuthority;
            session = dailyV1ApplicationAdvanceSession;
            return application != null && session != null;
        }
    }

    private bool TryAdvanceDayCore(out SimulationRuntimeAdvanceFailure failure)
    {
        if (!TryAdvanceSimulationClockCore(out failure)) return false;
        AdvanceDayAfterClockAdvance();
        return true;
    }

    private bool TryAdvanceSimulationClockCore(out SimulationRuntimeAdvanceFailure failure)
    {
        failure = SimulationRuntimeAdvanceFailure.None;
        if ((compositionProfile == SimulationRuntimeCompositionProfile.P16AOneHopMilitary
                || compositionProfile == SimulationRuntimeCompositionProfile.P17AWithdrawalWar)
            && Thread.CurrentThread.ManagedThreadId != p16AOwnerThreadId)
        {
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }
        if (mutationGuard.CanMutate == false)
        {
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        if (!SimulationTimeOwnerBridge.TryAdvanceDayFromRuntime(
                simulationTime, out SimulationTimeAdvanceFailure timeFailure))
        {
            failure = timeFailure == SimulationTimeAdvanceFailure.RuntimeFaulted
                ? SimulationRuntimeAdvanceFailure.RuntimeFaulted
                : SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow;
            return false;
        }
        return true;
    }

    DailyV1ExecutionBoundaryState IDailyV1RuntimeAdvanceEvidenceBoundary.ExecutionState => dailyExecutionBoundary;
    bool IDailyV1RuntimeAdvanceBoundary.HasIntradayTimeline => p18dTimeline != null;

    bool IDailyV1RuntimeAdvanceEvidenceBoundary.TryEnterDailyAdvanceOperation(
        out IDisposable operationScope)
    {
        operationScope = null;
        if (!TryEnterRuntimeAdmissionOperation(
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance,
                out SimulationOperationScope hostScope))
            return false;
        operationScope = hostScope;
        return true;
    }

    bool IDailyV1RuntimeAdvanceEvidenceBoundary.TryCreateSelectedDailyExecutionBoundary(
        out IDailyV1SelectedDailyExecutionBoundary executionBoundary)
    {
        DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
        executionBoundary = graph == null || graph.Definitions == null
            ? null
            : new SelectedDailyV1ExecutionBoundary(this);
        return executionBoundary != null;
    }

    bool IDailyV1RuntimeAdvanceBoundary.TryAdvanceDayCore(
        out SimulationRuntimeAdvanceFailure failure)
    {
        bool advanced = TryAdvanceDayCore(out SimulationRuntimeAdvanceFailure hostFailure);
        failure = hostFailure;
        return advanced;
    }

    bool IDailyV1RuntimeAdvanceBoundary.TryAdvanceIntradayToNextDayBoundary(
        out SimulationRuntimeAdvanceFailure failure)
    {
        LogicalTick target;
        try
        {
            target = p18dTimeline.CurrentInstant.NextDayBoundary;
        }
        catch (OverflowException)
        {
            failure = SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow;
            return false;
        }

        bool advanced = TryAdvanceP18DIntradayToCore(
            target, out SimulationRuntimeAdvanceFailure hostFailure);
        failure = hostFailure;
        return advanced;
    }

    bool IDailyV1RuntimeAdvanceEvidenceBoundary.TryCaptureCompletedDailyCaptureEvidence(
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure) =>
        TryCaptureCompletedDailyCaptureEvidence(out ownerSections, out mutationEpoch, out failure);

    bool IDailyV1RuntimeAdvanceEvidenceBoundary.TryCaptureDailyBoundaryAdmissionEvidence(
        bool sessionReplacement,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure) =>
        TryReadDailyCaptureEvidence(
            finalizingHeldAdvance: false,
            sessionReplacement
                ? DailyV1CaptureScopeAllowance.SessionReplacementValidation
                : DailyV1CaptureScopeAllowance.None,
            out ownerSections,
            out mutationEpoch,
            out failure);

    bool IDailyV1RuntimeAdvanceEvidenceBoundary.TryCaptureContinuationSaveEvidence(
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure) =>
        TryReadDailyCaptureEvidence(
            finalizingHeldAdvance: false,
            DailyV1CaptureScopeAllowance.ContinuationSave,
            out ownerSections,
            out mutationEpoch,
            out failure);

    void IDailyV1RuntimeAdvanceEvidenceBoundary.FaultRuntimeAdmission() => FaultRuntimeAdmission();

    // Selected Daily-v1 sessions bind a Core-owned boundary to these narrow
    // P12/projection ports. The Unity runtime's legacy day and intraday
    // methods remain available only to the non-selected compatibility path.
    DailyV1ExecutionBoundaryState IDailyV1SelectedRuntimeAdvanceHostPorts.ExecutionState =>
        dailyExecutionBoundary;

    bool IDailyV1SelectedRuntimeAdvanceHostPorts.TryBindApplicationAdvanceAuthority(
        DailyV1Application application,
        DailyV1ActiveRuntimeSession session)
    {
        if (application == null || session == null
            || !ReferenceEquals(session.ExecutionBoundaryState, dailyExecutionBoundary)
            || !dailyExecutionBoundary.HasAdmittedSelectedDailyExecution
            || dailyExecutionBoundary.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
            return false;

        lock (dailyV1ApplicationAdvanceAuthorityGate)
        {
            if (dailyV1ApplicationAdvanceAuthority == null
                && dailyV1ApplicationAdvanceSession == null)
            {
                dailyV1ApplicationAdvanceAuthority = application;
                dailyV1ApplicationAdvanceSession = session;
                return true;
            }

            return ReferenceEquals(dailyV1ApplicationAdvanceAuthority, application)
                && ReferenceEquals(dailyV1ApplicationAdvanceSession, session);
        }
    }

    bool IDailyV1SelectedRuntimeAdvanceHostPorts.TryEnterRuntimeProtocolOperation(
        string operationContractId,
        out IDisposable operationScope)
    {
        operationScope = null;
        if (!TryEnterRuntimeAdmissionOperation(
                operationContractId, out SimulationOperationScope hostScope))
            return false;
        operationScope = hostScope;
        return true;
    }

    bool IDailyV1SelectedRuntimeAdvanceHostPorts.TryCreateSelectedDailyExecutionBoundary(
        out IDailyV1SelectedDailyExecutionBoundary executionBoundary) =>
        ((IDailyV1RuntimeAdvanceEvidenceBoundary)this).TryCreateSelectedDailyExecutionBoundary(
            out executionBoundary);

    bool IDailyV1SelectedRuntimeAdvanceHostPorts.TryCaptureOwnerSectionCensus(
        DailyV1CaptureScopeAllowance allowedOuterScope,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure)
    {
        if (!Enum.IsDefined(typeof(DailyV1CaptureScopeAllowance), allowedOuterScope))
        {
            ownerSections = null;
            mutationEpoch = 0L;
            failure = DailyCaptureEligibilityFailure.RuntimeFaulted;
            return false;
        }

        // Core chooses the exact capture scope; Unity remains the adapter for
        // the live census providers and the existing protocol-level P12 checks.
        return TryReadDailyCaptureEvidence(
            finalizingHeldAdvance: allowedOuterScope == DailyV1CaptureScopeAllowance.DailyAdvance,
            allowedOuterScope,
            out ownerSections,
            out mutationEpoch,
            out failure);
    }

    void IDailyV1SelectedRuntimeAdvanceHostPorts.FaultRuntimeAdmission() =>
        ((IDailyV1RuntimeAdvanceEvidenceBoundary)this).FaultRuntimeAdmission();

    /// <summary>
    /// Acquires this runtime's single-writer advance lease for a larger composed
    /// chronological operation. This is reentrancy protection, not a thread lock.
    /// </summary>
    internal bool TryAcquireAdvanceLease(out DailyV1AdvanceLease lease)
    {
        return dailyExecutionBoundary.TryAcquireAdvanceLease(out lease);
    }

    private void AdvanceDayAfterClockAdvance()
    {
        activeDailyGameplayExecutionProbeForTest?.RecordAdvanceDayAfterClockAdvanceInvocation();
        if (dailyExecutionBoundary.HasAdmittedSelectedDailyExecution)
        {
            DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
            if (graph == null || graph.Definitions == null)
                throw new InvalidOperationException(
                    "The selected Daily-v1 runtime has no admitted effective definitions for its daily execution.");
            DailyV1SelectedDailyExecutionStage.Advance(
                dailyExecutionBoundary,
                graph,
                new SelectedDailyV1ExecutionBoundary(this));
            return;
        }

        AdvanceDayAfterClockAdvanceLegacy();
    }

    private void AdvanceDayAfterClockAdvanceLegacy()
    {
        placeContentStore?.AdvanceDays(1);
        logger?.BeginDay(CurrentDay);
        if (dailyExecutionBoundary.HasAdmittedSelectedDailyExecution)
        {
            lastDailyDemographyReport = DailyDemographicSystem.AdvanceSelected(
                this,
                naturalMortalitySamples,
                aggregateDemographyProvider);
        }
        else
        {
            lastDailyDemographyReport = DailyDemographicSystem.Advance(
                this,
                naturalMortalitySamples,
                aggregateDemographyProvider);
        }
        BeginSimulationDay();
        scheduledDirectiveSystem?.PrepareDay(CurrentDay);

        if (configuration.Economy.Enabled == true)
        {
            SimulateEconomyDay();
        }

        RefreshLocalKnowledgeAndShare();
        adventureExpeditionAutonomySystem?.AdvanceActiveExpeditions();

        StartAutonomousExpeditions();

        AdvanceActorTurnsForCurrentRuntime();

        AdvanceTravelAndObserve();
    }

    private sealed class SelectedDailyV1ExecutionBoundary
        : IDailyV1SelectedDailyExecutionBoundary
    {
        private readonly SimulationRuntime runtime;

        internal SelectedDailyV1ExecutionBoundary(SimulationRuntime runtime)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public void BeginLoggerDay(long absoluteDay)
        {
            activeDailyGameplayExecutionProbeForTest?.RecordAdvanceDayAfterClockAdvanceInvocation();
            runtime.logger?.BeginDay(absoluteDay);
        }

        public bool TryCreateSelectedDemographyContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDemographyExecutionContext context)
        {
            context = null;
            IDailyV1SelectedDemographyBoundary boundary =
                DailyDemographicSystem.CreateSelectedDemographyBoundary(runtime, graph);
            if (boundary == null) return false;
            context = new SelectedDemographyExecutionContext(runtime, boundary);
            return true;
        }

        public void PublishSelectedDemographyResult(DailyV1SelectedDemographyResult result)
        {
            runtime.lastDailyDemographyReport = DailyDemographicSystem.CreateSelectedDemographyReport(result);
        }

        public bool TryCreateSelectedDayStartContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedDayStartExecutionContext context) =>
            runtime.TryCreateSelectedDailyV1DayStartContext(graph, out context);

        public void BeginSimulationDay() => runtime.BeginSimulationDay();
        public void EnsureSelectedEconomyCanRun(DailyV1SelectedRuntimeOwnerGraph graph) =>
            runtime.EnsureSelectedEconomyCanRun(graph);
        public void LogSelectedDailyEconomy(
            DailyV1CityRuntimeOwners owner,
            SimulationLogCategory category,
            string message) => runtime.LogSelectedDailyEconomy(owner, category, message);
        public void EnsureSelectedLocalKnowledgeCanRun(DailyV1SelectedRuntimeOwnerGraph graph) =>
            runtime.EnsureSelectedLocalKnowledgeCanRun(graph);
        public bool TryCreateSelectedActorTurnContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedActorTurnExecutionContext context) =>
            runtime.TryCreateSelectedDailyV1ActorTurnContext(graph, out context);
        public bool TryCreateSelectedActorTurnBoundary(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1ActorTurnExecutionBoundary boundary)
        {
            boundary = null;
            if (!runtime.TryCreateSelectedDailyV1ActorTurnContext(
                    graph, out IDailyV1SelectedActorTurnExecutionContext context)
                || context?.Boundary == null)
                return false;
            boundary = context.Boundary;
            return true;
        }
        public bool TryCreateSelectedTravelDayContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            out IDailyV1SelectedTravelDayExecutionContext context) =>
            runtime.TryCreateSelectedTravelDayExecutionContext(graph, out context);
    }

    private sealed class SelectedDemographyExecutionContext
        : IDailyV1SelectedDemographyExecutionContext
    {
        private readonly SimulationRuntime runtime;

        internal SelectedDemographyExecutionContext(
            SimulationRuntime runtime,
            IDailyV1SelectedDemographyBoundary boundary)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        }

        public SimulationCalendar Calendar => runtime.Calendar;
        public IPersonNaturalMortalitySampleProvider MortalitySamples => runtime.naturalMortalitySamples;
        public IAggregateDemographyProvider AggregateProvider => runtime.aggregateDemographyProvider;
        public IDailyV1SelectedDemographyBoundary Boundary { get; }

    }

    private void StartAutonomousExpeditions()
    {
        if (adventureExpeditionAutonomySystem == null) return;
        foreach (NpcRuntime npcRuntime in npcRuntimes)
        {
            if (npcRuntime != null
                && npcRuntime.IsAlive
                && npcRuntime.IsTraveling == false
                && (expeditionSystem == null
                    || expeditionSystem.IsNpcOnActiveExpedition(npcRuntime.RuntimeId) == false))
            {
                adventureExpeditionAutonomySystem.TryStartAutonomousExpedition(npcRuntime, npcRuntimes);
            }
        }
    }

    private void AdvanceActorTurnsForCurrentRuntime()
    {
        if (TryAdvanceSelectedDailyV1ActorTurns()) return;
        if (SelectedRuntimeOwnerGraph != null && runtimeAdmissionContext != null)
            throw new InvalidOperationException(
                "The admitted Daily-v1 actor turn could not bind its exact Core owner graph.");

        for (int actorTurnRosterOrdinal = 0; actorTurnRosterOrdinal < npcRuntimes.Count; actorTurnRosterOrdinal++)
        {
            NpcRuntime npcRuntime = npcRuntimes[actorTurnRosterOrdinal];
            if (npcRuntime == null) continue;

            if (npcRuntime.IsAlive == false)
            {
                RejectActorChoicesForPerson(npcRuntime.PersonId, actorTurnRosterOrdinal);
                continue;
            }

            if (HasPendingActorChoiceFor(npcRuntime.PersonId)
                && IsCurrentMaterializedPersonActor(npcRuntime) == false)
            {
                RejectActorChoicesForPerson(npcRuntime.PersonId, actorTurnRosterOrdinal);
                continue;
            }

            if (TryDeferActorChoiceForSpatialTransit(npcRuntime, actorTurnRosterOrdinal)) continue;

            if (npcRuntime.IsTraveling == true)
            {
                TryProcessScheduledDirective(npcRuntime);
                DeferActorChoiceForPerson(npcRuntime.PersonId, actorTurnRosterOrdinal,
                    ActorChoiceDeferralReason.Traveling);
                continue;
            }

            if (expeditionSystem != null
                && expeditionSystem.IsNpcOnActiveExpedition(npcRuntime.RuntimeId) == true)
            {
                TryProcessScheduledDirective(npcRuntime);
                DeferActorChoiceForPerson(npcRuntime.PersonId, actorTurnRosterOrdinal,
                    ActorChoiceDeferralReason.ExpeditionParticipant);
                continue;
            }

            if (adventureExpeditionAutonomySystem != null
                && adventureExpeditionAutonomySystem.IsReservedToday(npcRuntime.RuntimeId))
            {
                DeferActorChoiceForPerson(npcRuntime.PersonId, actorTurnRosterOrdinal,
                    ActorChoiceDeferralReason.ReservedExpeditionActivity);
                continue;
            }

            EvaluateStatus(npcRuntime);
            if (HasPendingActorChoiceFor(npcRuntime.PersonId))
            {
                if (TryProcessScheduledDirective(npcRuntime) == true)
                {
                    DeferActorChoiceForPerson(npcRuntime.PersonId, actorTurnRosterOrdinal,
                        ActorChoiceDeferralReason.ScheduledDirective);
                    continue;
                }
                if (TryProcessActorChoice(npcRuntime, actorTurnRosterOrdinal)) continue;
            }

            if (configuration.MerchantTrade.Enabled && merchantSystem != null)
            {
                if (runtimeAdmissionContext == null)
                {
                    merchantSystem.AdvanceNpcTradeState(npcRuntime);
                }
                else if (!TryBeginP12MerchantDailyNpcTradeOperation(
                    npcRuntime, out P12MerchantOperationScope merchantOperationScope))
                {
                    throw new InvalidOperationException(
                        "The P12 daily Merchant owner operation could not be admitted.");
                }
                else
                {
                    using (merchantOperationScope)
                        merchantSystem.AdvanceNpcTradeState(npcRuntime);
                }
            }

            if (TryProcessScheduledDirective(npcRuntime) == true)
            {
                DeferActorChoiceForPerson(npcRuntime.PersonId, actorTurnRosterOrdinal,
                    ActorChoiceDeferralReason.ScheduledDirective);
                continue;
            }
            if (TryProcessActorChoice(npcRuntime, actorTurnRosterOrdinal)) continue;

            EvaluateAction(npcRuntime);
            if (npcRuntime.CurrentAction?.actionType == NpcActionType.SellGoods
                && (npcRuntime.CurrentCity == null || !IsActorAtCurrentCityLocation(npcRuntime)))
            {
                npcRuntime.SetCurrentActionRuntime(null);
                continue;
            }
            TryExecuteCurrentAction(npcRuntime);
        }

        RejectActorChoicesWithoutMaterializedTurn(npcRuntimes.Count);
    }

    private void AdvanceTravelAndObserve()
    {
        List<NpcRuntime> arrivedNpcs = new List<NpcRuntime>();
        DailyV1SelectedRuntimeOwnerGraph selectedGraph = SelectedRuntimeOwnerGraph;
        if (selectedGraph != null)
        {
            if (!TryAdvanceSelectedTravelDayStage(out IReadOnlyList<NpcRuntime> selectedArrivals))
                throw new InvalidOperationException(
                    "The selected Core ordered TravelParty/solo-travel stages could not admit their exact runtime owner graph.");
            arrivedNpcs.AddRange(selectedArrivals);
        }
        else
        {
            if (travelPartySystem != null)
            {
                if (runtimeAdmissionContext == null)
                {
                    arrivedNpcs.AddRange(travelPartySystem.AdvanceParties());
                }
                else if (!TryBeginP12TravelPartyAdvanceOperation(
                    out P12TravelPartyAdvanceOperationScope travelPartyAdvanceScope))
                {
                    throw new InvalidOperationException(
                        "The P12 TravelParty advance owner operation could not be admitted.");
                }
                else
                {
                    using (travelPartyAdvanceScope)
                        arrivedNpcs.AddRange(travelPartySystem.AdvanceParties());
                }
            }

            if (travelSystem != null)
                arrivedNpcs.AddRange(travelSystem.AdvanceTravels(npcRuntimes));
        }

        ObserveTravelArrivals(arrivedNpcs);
    }

    private bool TryCreateSelectedDailyV1ActorTurnContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedActorTurnExecutionContext context)
    {
        context = null;
        if (graph == null || actorChoiceStore == null || personStore == null)
            return false;
        EffectiveMerchantTradeConfiguration selectedMerchantConfiguration =
            graph.Definitions?.EffectiveConfiguration?.MerchantTrade;
        if (selectedMerchantConfiguration == null)
            throw new InvalidOperationException(
                "The selected actor-turn stage requires the admitted Core Merchant configuration.");

        if (!TryReadCoreSelectedRuntimeActorRoster(
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRoster))
            return false;
        Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> runtimeByOwner =
            new Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime>();
        for (int rosterOrdinal = 0; rosterOrdinal < npcRuntimes.Count; rosterOrdinal++)
        {
            NpcRuntime runtime = npcRuntimes[rosterOrdinal];
            DailyV1ActorTurnRosterProjection rosterMember = runtimeRoster[rosterOrdinal];
            if (runtime == null)
            {
                if (rosterMember != null) return false;
                continue;
            }

            if (!runtime.UsesSelectedDailyV1Definitions)
            {
                // P12 permits an unbound dynamic NPC for owner-census purposes.
                // It has no admitted Daily-v1 actor owner, so preserve its roster
                // ordinal while excluding it from shared domain execution.
                continue;
            }

            DailyV1NpcRuntimeOwners owner = runtime.SelectedRuntimeOwners;
            if (owner == null
                || !graph.ContainsExactNpcOwner(owner)
                || !ReferenceEquals(graph.Definitions, owner.Definitions)
                || !ReferenceEquals(runtime.SelectedRuntimeOwnerGraph, graph)
                || !string.Equals(runtime.RuntimeId, owner.RuntimeId, StringComparison.Ordinal)
                || runtimeByOwner.ContainsKey(owner))
                return false;

            runtimeByOwner.Add(owner, runtime);
        }

        context = new SelectedDailyV1ActorTurnExecutionContext(
            runtimeRoster,
            personStore,
            actorChoiceStore,
            new SelectedDailyV1ActorTurnExecutionBoundary(this, graph, runtimeByOwner));
        return true;
    }

    /// <summary>
    /// Core owns actor ordering and slot identities. Unity reads this immutable
    /// snapshot only to bind existing projections and reject a divergent host
    /// roster before selected execution.
    /// </summary>
    private bool TryReadCoreSelectedRuntimeActorRoster(
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRoster)
    {
        runtimeRoster = dailyExecutionBoundary.SelectedRuntimeActorRoster;
        if (runtimeRoster == null || npcRuntimes == null
            || runtimeRoster.Count != npcRuntimes.Count)
        {
            runtimeRoster = null;
            return false;
        }

        for (int ordinal = 0; ordinal < runtimeRoster.Count; ordinal++)
        {
            NpcRuntime runtime = npcRuntimes[ordinal];
            DailyV1ActorTurnRosterProjection member = runtimeRoster[ordinal];
            if (runtime == null)
            {
                if (member != null) { runtimeRoster = null; return false; }
                continue;
            }

            if (member == null
                || !string.Equals(member.RuntimeId, runtime.RuntimeId, StringComparison.Ordinal)
                || !ReferenceEquals(member.PersonId, runtime.PersonId))
            {
                runtimeRoster = null;
                return false;
            }
        }

        return true;
    }

    private bool TryCreateSelectedDailyV1DayStartContext(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out IDailyV1SelectedDayStartExecutionContext context)
    {
        context = null;
        if (graph == null) return false;
        bool hasUnselectedLiveRuntimeMember = npcRuntimes != null
            && npcRuntimes.Any(runtime => runtime != null
                && !runtime.UsesSelectedDailyV1Definitions);
        if (hasUnselectedLiveRuntimeMember)
        {
            // Report only the compatibility fact. Core owns the choice to
            // delegate this day-start to the established Unity lifecycle.
            context = new SelectedDailyV1DayStartExecutionContext(
                null,
                true,
                false,
                false,
                null);
            return true;
        }

        if (!TryReadCoreSelectedRuntimeActorRoster(
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRoster))
            throw new InvalidOperationException(
                "The selected Daily-v1 day-start roster differs from the exact Core owner state.");

        // Keep the established whole-stage Unity behavior for dynamic or
        // otherwise unselected roster members. The selected path is available
        // only when every live runtime member has an exact Core graph owner.
        List<DailyV1NpcRuntimeOwners> exactNpcOwners =
            new List<DailyV1NpcRuntimeOwners>(npcRuntimes.Count);
        Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> runtimeByOwner =
            new Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime>();
        for (int rosterOrdinal = 0; rosterOrdinal < npcRuntimes.Count; rosterOrdinal++)
        {
            NpcRuntime runtime = npcRuntimes[rosterOrdinal];
            DailyV1ActorTurnRosterProjection rosterMember = runtimeRoster[rosterOrdinal];
            if (runtime == null)
            {
                if (rosterMember != null) return false;
                continue;
            }
            if (!runtime.UsesSelectedDailyV1Definitions)
                throw new InvalidOperationException(
                    "The selected Daily-v1 day-start roster changed during owner binding.");

            DailyV1NpcRuntimeOwners owner = runtime.SelectedRuntimeOwners;
            if (owner == null
                || !ReferenceEquals(runtime.SelectedRuntimeOwnerGraph, graph)
                || !graph.ContainsExactNpcOwner(owner)
                || !ReferenceEquals(graph.Definitions, owner.Definitions)
                || !string.Equals(runtime.RuntimeId, owner.RuntimeId, StringComparison.Ordinal)
                || !runtimeByOwner.TryAdd(owner, runtime))
                throw new InvalidOperationException(
                    "The selected Daily-v1 day-start stage requires each exact Core NPC owner instance.");

            exactNpcOwners.Add(owner);
        }
        if (!graph.MatchesExactOwnerInstances(graph.Cities, exactNpcOwners))
            throw new InvalidOperationException(
                "The selected Daily-v1 day-start stage received an incomplete Core owner roster.");

        SelectedDailyV1ActorTurnExecutionBoundary selectedBoundary =
            new SelectedDailyV1ActorTurnExecutionBoundary(this, graph, runtimeByOwner);

        EffectiveSimulationConfiguration effective = graph.Definitions?.EffectiveConfiguration;
        if (effective?.Crime == null || effective.MerchantTrade == null)
            throw new InvalidOperationException(
                "The selected Daily-v1 day-start stage requires admitted Crime and Merchant configuration.");

        DailyV1JusticeRecordsOwner justiceOwner = graph.JusticeRecordsOwner;
        bool justiceSystemPresent = justiceOwner != null
            && this.justiceSystem != null
            && ReferenceEquals(this.justiceSystem.CoreOwner, justiceOwner);
        if (this.justiceSystem != null
            && !ReferenceEquals(this.justiceSystem.CoreOwner, justiceOwner))
            throw new InvalidOperationException(
                "Selected daily Justice execution requires the exact Justice owner from the admitted Core graph.");

        bool crimeSystemPresent = justiceOwner != null
            && this.crimeSystem != null
            && ReferenceEquals(this.crimeSystem.JusticeOwner?.CoreOwner, justiceOwner);
        if (this.crimeSystem != null && !crimeSystemPresent)
            throw new InvalidOperationException(
                "Selected daily Crime execution requires the exact Justice owner from the admitted Core graph.");

        context = new SelectedDailyV1DayStartExecutionContext(
            runtimeRoster,
            false,
            crimeSystemPresent,
            justiceSystemPresent,
            selectedBoundary);
        return true;
    }

    private sealed class SelectedDailyV1DayStartExecutionContext
        : IDailyV1SelectedDayStartExecutionContext
    {
        internal SelectedDailyV1DayStartExecutionContext(
            IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRoster,
            bool hasUnselectedLiveRuntimeMember,
            bool crimeSystemPresent,
            bool justiceSystemPresent,
            SelectedDailyV1ActorTurnExecutionBoundary boundary)
        {
            RuntimeRoster = runtimeRoster;
            HasUnselectedLiveRuntimeMember = hasUnselectedLiveRuntimeMember;
            CrimeSystemPresent = crimeSystemPresent;
            JusticeSystemPresent = justiceSystemPresent;
            CrimeJusticeBoundary = boundary;
            MerchantTradeBoundary = boundary;
        }

        public IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
        public bool HasUnselectedLiveRuntimeMember { get; }
        public bool CrimeSystemPresent { get; }
        public bool JusticeSystemPresent { get; }
        public IDailyV1SelectedCrimeJusticeDailyBoundary CrimeJusticeBoundary { get; }
        public IDailyV1SelectedMerchantTradeStateBoundary MerchantTradeBoundary { get; }
    }

    private bool TryAdvanceSelectedDailyV1ActorTurns()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
        if (!TryCreateSelectedDailyV1ActorTurnContext(graph,
                out IDailyV1SelectedActorTurnExecutionContext context)
            || context == null
            || !DailyV1ActorTurnExecutionStage.TryBuildRoster(
                graph, context.RuntimeRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterEntry> roster))
            return false;

        return DailyV1ActorTurnExecutionStage.TryRunOrdered(
            graph,
            roster,
            context.PersonStore,
            context.ActorChoiceStore,
            scheduledDirectiveSystem?.Store,
            CurrentDay,
            graph.Definitions.EffectiveConfiguration.MerchantTrade.Enabled,
            context.Boundary);
    }

    private sealed class SelectedDailyV1ActorTurnExecutionContext
        : IDailyV1SelectedActorTurnExecutionContext
    {
        internal SelectedDailyV1ActorTurnExecutionContext(
            IReadOnlyList<DailyV1ActorTurnRosterProjection> runtimeRoster,
            PersonStore personStore,
            ActorChoiceStore actorChoiceStore,
            IDailyV1ActorTurnExecutionBoundary boundary)
        {
            RuntimeRoster = runtimeRoster ?? throw new ArgumentNullException(nameof(runtimeRoster));
            PersonStore = personStore ?? throw new ArgumentNullException(nameof(personStore));
            ActorChoiceStore = actorChoiceStore ?? throw new ArgumentNullException(nameof(actorChoiceStore));
            Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        }

        public IReadOnlyList<DailyV1ActorTurnRosterProjection> RuntimeRoster { get; }
        public PersonStore PersonStore { get; }
        public ActorChoiceStore ActorChoiceStore { get; }
        public DailyV1ActorTurnCoreRoots CoreRoots => null;
        public IDailyV1ActorTurnExecutionBoundary Boundary { get; }
    }

    private sealed class SelectedDailyV1ActorTurnExecutionBoundary
        : IDailyV1ActorTurnExecutionBoundary,
            IDailyV1SelectedActorChoiceExecutionContext,
            IDailyV1CoreActorChoiceExecutionBoundary,
            IDailyV1SelectedMerchantTradeStateBoundary,
            IDailyV1SelectedMerchantTradeStateAdmissionBoundary,
            IDailyV1SelectedActorActionExecutionBoundary,
            IDailyV1SelectedCrimeJusticeDailyBoundary
    {
        private readonly SimulationRuntime runtime;
        private readonly DailyV1SelectedRuntimeOwnerGraph graph;
        private readonly Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> runtimeByOwner;
        private NpcActionRuntime preparedActionRuntime;
        private DailyV1NpcActionPayloadOwner preparedActionPayload;
        private DailyV1NpcRuntimeOwners preparedActionActor;
        private NpcActionRuntime preparedClearActionRuntime;
        private DailyV1NpcActionPayloadOwner preparedClearActionPayload;
        private DailyV1NpcRuntimeOwners preparedClearActionActor;

        internal SelectedDailyV1ActorTurnExecutionBoundary(
            SimulationRuntime runtime,
            DailyV1SelectedRuntimeOwnerGraph graph,
            Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> runtimeByOwner)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
            this.runtimeByOwner = runtimeByOwner ?? throw new ArgumentNullException(nameof(runtimeByOwner));
        }

        public bool IsReservedToday(DailyV1NpcRuntimeOwners actor)
        {
            RequireExactActor(actor);
            // UnityBootstrap-Daily-v1 admits no site, place, notable item, or
            // expedition-autonomy producer. Reservation is therefore exactly
            // empty for this selected graph; a future profile that admits such
            // producers must add their Core owner/read before changing this.
            return false;
        }

        public void ExecuteWithinDailyMutation(
            DailyV1SelectedCrimeJusticeDailyPhase phase,
            Action mutation)
        {
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            runtime.RunP12CrimeJusticeDailyMutation(graph, phase, mutation);
        }

        public DailyV1JusticeRecordsOwner JusticeRecordsOwner => graph.JusticeRecordsOwner;

        public bool CanProjectStatusChanges(
            DailyV1NpcRuntimeOwners owner,
            IReadOnlyList<DailyV1StatusDefinition> statusToAdd)
        {
            return TryGetExactActor(owner, out NpcRuntime exactRuntime)
                && exactRuntime.CanProjectCoreSelectedDailyV1StatusChanges(owner, statusToAdd);
        }

        public void EnsureMutationAllowed(DailyV1NpcRuntimeOwners owner)
        {
            if (!TryGetExactActor(owner, out NpcRuntime exactRuntime))
                throw new InvalidOperationException(
                    "The selected Crime/Justice stage lost its exact NPC runtime projection.");
            exactRuntime.EnsureCoreSelectedDailyV1OwnerMutationAllowed(owner);
        }

        public void NotifyStatusMutationCommitted(
            DailyV1NpcRuntimeOwners owner,
            DailyV1StatusDefinition status,
            bool added)
        {
            if (!TryGetExactActor(owner, out NpcRuntime exactRuntime))
                throw new InvalidOperationException(
                    "The selected Crime/Justice status owner lost its exact runtime projection after mutation.");
            exactRuntime.NotifyCoreSelectedDailyV1StatusMutationCommitted(owner);
        }

        public void NotifyHiddenMutationCommitted(DailyV1NpcRuntimeOwners owner)
        {
            if (!TryGetExactActor(owner, out NpcRuntime exactRuntime))
                throw new InvalidOperationException(
                    "The selected Crime/Justice hidden-state owner lost its exact runtime projection after mutation.");
            exactRuntime.NotifyCoreSelectedDailyV1HiddenMutationCommitted(owner);
        }

        public bool CanRemoveSentenceProjection(DailyV1JusticeSentenceOwner sentence)
        {
            return graph.JusticeRecordsOwner != null
                && ReferenceEquals(runtime.justiceSystem?.CoreOwner, graph.JusticeRecordsOwner)
                && sentence != null && graph.JusticeRecordsOwner.ContainsSentence(sentence)
                && runtime.justiceSystem.CanRemoveSelectedSentenceProjection(sentence);
        }

        public void CommitSentenceProjectionRemoval(DailyV1JusticeSentenceOwner sentence)
        {
            if (graph.JusticeRecordsOwner == null
                || !ReferenceEquals(runtime.justiceSystem?.CoreOwner, graph.JusticeRecordsOwner))
                throw new InvalidOperationException(
                    "The selected Justice sentence lost its exact host owner after Core removal.");
            runtime.justiceSystem.CommitSelectedSentenceProjectionRemoval(sentence);
        }

        public void Log(SimulationLogCategory category, string message) =>
            runtime.logger?.Log(category, message);

        public void LogWarning(string message) => runtime.logger?.LogWarning(message);

        public void ProcessScheduledDirective(DailyV1NpcRuntimeOwners actor, ScheduledDirective directive) =>
            runtime.ProcessScheduledDirective(RequireExactActor(actor), directive);

        public bool TryProcessActorChoice(DailyV1NpcRuntimeOwners actor, int rosterOrdinal) =>
            runtime.TryProcessActorChoice(RequireExactActor(actor), rosterOrdinal);

        public bool TryGetActorChoiceExecutionContext(
            DailyV1NpcRuntimeOwners actor,
            out IDailyV1ActorChoiceExecutionBoundary executionBoundary)
        {
            RequireExactActor(actor);
            executionBoundary = this;
            return true;
        }

        public bool TryPrepareRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (preparedActionRuntime != null || preparedActionPayload != null
                || payload == null || runtime.npcDecisionSystem == null
                || !runtime.npcDecisionSystem.TryProjectSelectedLocalSale(
                    exactRuntime, payload, out NpcActionRuntime projectedAction)
                || projectedAction == null
                || !ReferenceEquals(projectedAction.SelectedOwner, payload)
                || !projectedAction.MatchesSelectedOwner())
                return false;

            preparedActionRuntime = projectedAction;
            preparedActionPayload = payload;
            preparedActionActor = actor;
            return true;
        }

        public bool TryRefreshRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            RequireExactActor(actor);
            return ReferenceEquals(preparedActionActor, actor)
                && ReferenceEquals(preparedActionPayload, payload)
                && preparedActionRuntime != null
                && preparedActionRuntime.TryUseSelectedPayload(
                    payload, graph.Definitions);
        }

        public bool CanCommitRequestedActionSlot(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            return ReferenceEquals(preparedActionActor, actor)
                && ReferenceEquals(preparedActionPayload, payload)
                && preparedActionRuntime != null
                && exactRuntime.CanCommitP12LifecycleMutation(
                    false, false, true, preparedActionRuntime);
        }

        public bool TryPublishRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (!ReferenceEquals(preparedActionActor, actor)
                || !ReferenceEquals(preparedActionPayload, payload)
                || preparedActionRuntime == null
                || !ReferenceEquals(actor.ActionSlot?.CurrentAction, payload?.Action)
                || !ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
                || !preparedActionRuntime.MatchesSelectedOwner())
                return false;

            try
            {
                if (!exactRuntime.TryInstallPrecommittedCurrentActionRuntime(preparedActionRuntime))
                    return false;
                return true;
            }
            finally
            {
                ClearPreparedActionProjection();
            }
        }

        public void CancelPreparedRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            RequireExactActor(actor);
            if (ReferenceEquals(preparedActionActor, actor)
                && ReferenceEquals(preparedActionPayload, payload))
                ClearPreparedActionProjection();
        }

        public void LogRequestedActionDecisionFailure(string message) =>
            runtime.logger?.LogError(message);

        public NpcActionResultType? ExecuteCurrentAction(DailyV1NpcRuntimeOwners actor)
        {
            RequireExactActor(actor);
            if (!DailyV1SelectedActorActionExecutionStage.TryExecuteInstalledAction(
                    graph, actor, runtime.randomSource, runtime.CurrentDay, this,
                    out _, out NpcActionResultType? resultType, out _))
                throw new InvalidOperationException(
                    "The selected actor-choice action could not execute through its exact Core owner graph.");
            return resultType;
        }

        public void AdvanceMerchantTradeState(DailyV1NpcRuntimeOwners actor)
        {
            if (!ExecuteWithinSelectedMerchantDailyOperation(actor,
                    () => DailyV1SelectedMerchantTradeStateOperation.TryAdvance(
                        graph, actor, runtime.simulationTime.AbsoluteDay, this)))
                throw new InvalidOperationException(
                    "The selected Daily-v1 Merchant owner graph could not advance its daily trade state.");
        }

        public bool ExecuteWithinSelectedMerchantDailyOperation(
            DailyV1NpcRuntimeOwners actor,
            Func<bool> operation)
        {
            if (operation == null) return false;
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (runtime.merchantSystem == null) return true;
            runtime.merchantSystem.EnsureCanAdvanceSelectedTradeState();
            if (runtime.runtimeAdmissionContext == null)
                return operation();

            if (!runtime.TryBeginP12MerchantDailyNpcTradeOperation(
                exactRuntime, out P12MerchantOperationScope merchantOperationScope))
                throw new InvalidOperationException(
                    "The P12 daily Merchant owner operation could not be admitted.");

            using (merchantOperationScope)
                return operation();
        }

        public bool TryInstallPlanState(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ItemDefinition item,
            string originCityRuntimeId,
            string targetCityRuntimeId,
            int plannedAmount,
            int remainingAmount,
            float purchasePricePerItem,
            int waitDaysAtDestination,
            int pendingTravelDays,
            string originDecisionId)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            DailyV1NpcMerchantTradePlanOwner owner = actor.MerchantTradePlan;
            if (owner.MatchesState(item, originCityRuntimeId, targetCityRuntimeId,
                    plannedAmount, remainingAmount, purchasePricePerItem,
                    waitDaysAtDestination, pendingTravelDays, originDecisionId))
                return true;
            if (!exactRuntime.MerchantTradePlan.CanAdmitSelectedPlanOwnerMutation()
                || !owner.TryInstallState(item, originCityRuntimeId, targetCityRuntimeId,
                    plannedAmount, remainingAmount, purchasePricePerItem,
                    waitDaysAtDestination, pendingTravelDays, originDecisionId))
                return false;
            exactRuntime.MerchantTradePlan.PublishSelectedPlanOwnerMutation();
            return true;
        }

        public bool TrySetTradeTravelPlan(
            DailyV1NpcRuntimeOwners actor,
            string targetCityRuntimeId,
            float utility,
            string originDecisionId)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            long priorRevision = actor.TravelPlan.Revision;
            if (!graph.TryCommitMerchantTradeTravelPlan(actor, targetCityRuntimeId,
                    utility, originDecisionId,
                    exactRuntime.TravelPlan.CanAdmitSelectedTradeTravelPlanMutation,
                    out _)) return false;
            if (actor.TravelPlan.Revision != priorRevision)
                exactRuntime.TravelPlan.PublishSelectedTradeTravelPlanMutation();
            return true;
        }

        public void ClearTradeTravelPlan(DailyV1NpcRuntimeOwners actor)
        {
            RequireExactActor(actor).ClearTravelPlan();
        }

        public NpcDecisionRecord RecordTradeRedirect(
            DailyV1NpcRuntimeOwners actor,
            string targetLocationRuntimeId,
            CommercialDecisionEvidence evidence)
        {
            RequireExactActor(actor);
            return runtime.decisionRecorder?.Record(actor.RuntimeId,
                NpcDecisionType.TradeRedirect, NpcDecisionOrigin.Autonomous,
                null, null, targetLocationRuntimeId, evidence);
        }

        public void LogTradeDecisionFailure(string message)
        {
            runtime.logger?.LogError(message);
        }

        public void LogTrade(string message)
        {
            runtime.merchantSystem?.LogSelectedTradeState(message);
        }

        public void EvaluateAndExecuteAction(DailyV1NpcRuntimeOwners actor)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            runtime.EvaluateAction(exactRuntime);
            if (exactRuntime.CurrentAction?.actionType == NpcActionType.SellGoods
                && (exactRuntime.CurrentCity == null
                    || !runtime.IsActorAtCurrentCityLocation(exactRuntime)))
            {
                exactRuntime.SetCurrentActionRuntime(null);
                return;
            }
            runtime.TryExecuteCurrentAction(exactRuntime);
        }

        public IAuthoritativeRandomSource RandomSource => runtime.randomSource;
        public bool CanExecuteWithoutActionProjection => false;

        public bool TryCreateActionProjectionBoundary(
            DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedActionProjectionBoundary projectionBoundary)
        {
            projectionBoundary = null;
            NpcRuntime exactRuntime = RequireExactActor(actor);
            return runtime.npcDecisionSystem != null
                && runtime.npcDecisionSystem.TryCreateSelectedDailyV1ActionProjectionBoundary(
                    exactRuntime, out projectionBoundary);
        }

        public bool TryGetDecisionRecordOwners(
            DailyV1NpcRuntimeOwners actor,
            out RuntimeIdAllocator idAllocator,
            out SimulationRecordSequence recordSequence,
            out NpcDecisionStore decisionStore)
        {
            RequireExactActor(actor);
            idAllocator = runtime.decisionRecorder?.DecisionIdAllocator;
            recordSequence = runtime.decisionRecorder?.RecordSequence;
            decisionStore = runtime.decisionRecorder?.DecisionStore;
            return idAllocator != null && recordSequence != null && decisionStore != null;
        }

        public bool TryGetConfiguredActionIds(
            DailyV1NpcRuntimeOwners actor,
            out IReadOnlyList<string> actionIds)
        {
            actionIds = null;
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (!ReferenceEquals(exactRuntime.SelectedRuntimeOwnerGraph, graph))
                return false;

            List<DailyV1ActionDefinition> configuredActions =
                runtime.GetSelectedConfiguredActionsEnabledForRuntime(exactRuntime);
            if (configuredActions == null) return false;
            List<string> resolvedIds = new List<string>(configuredActions.Count);
            foreach (DailyV1ActionDefinition action in configuredActions)
            {
                if (action == null) return false;
                resolvedIds.Add(action.DefinitionId);
            }
            actionIds = resolvedIds.AsReadOnly();
            return true;
        }

        public bool TryPrepareSelectedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionCandidate selected,
            IDailyV1SelectedActionProjectionBoundary projectionBoundary,
            DailyV1NpcActionPayloadOwner payload)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (preparedActionRuntime != null || preparedActionPayload != null
                || preparedClearActionRuntime != null || preparedClearActionPayload != null
                || selected?.PreparedPayload == null
                || !ReferenceEquals(selected.PreparedPayload, payload))
                return false;

            if (!runtime.npcDecisionSystem.TryProjectSelectedDailyV1ActionCandidate(
                    exactRuntime, selected, projectionBoundary,
                    out NpcActionRuntime actionRuntime)
                || actionRuntime == null
                || !ReferenceEquals(actionRuntime.SelectedOwner, payload)
                || !actionRuntime.MatchesSelectedOwner())
            {
                return false;
            }

            preparedActionRuntime = actionRuntime;
            preparedActionPayload = payload;
            preparedActionActor = actor;
            return true;
        }

        public bool CanCommitSelectedActionSlot(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            return ReferenceEquals(preparedActionPayload, payload)
                && preparedActionRuntime != null
                && exactRuntime.CanCommitP12LifecycleMutation(
                    false, false, true, preparedActionRuntime);
        }

        public bool TryPublishSelectedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (!ReferenceEquals(preparedActionPayload, payload)
                || !ReferenceEquals(preparedActionActor, actor)
                || preparedActionRuntime == null
                || !ReferenceEquals(actor.ActionSlot?.CurrentAction, payload.Action)
                || !ReferenceEquals(actor.ActionSlot?.CurrentPayload, payload)
                || !preparedActionRuntime.MatchesSelectedOwner())
                return false;

            try
            {
                if (!exactRuntime.TryInstallPrecommittedCurrentActionRuntime(preparedActionRuntime))
                    throw new InvalidOperationException(
                        "A committed selected action slot could not publish its Unity action projection.");
                return true;
            }
            finally
            {
                ClearPreparedActionProjection();
            }
        }

        public void CancelPreparedSelectedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            RequireExactActor(actor);
            if (ReferenceEquals(preparedActionActor, actor)
                && ReferenceEquals(preparedActionPayload, payload))
                ClearPreparedActionProjection();
        }

        private void ClearPreparedActionProjection()
        {
            preparedActionRuntime = null;
            preparedActionPayload = null;
            preparedActionActor = null;
        }

        public void LogSelectedActionDecisionFailure(
            DailyV1NpcRuntimeOwners actor,
            string message)
        {
            RequireExactActor(actor);
            runtime.logger?.LogError(message);
        }

        public bool TryPrepareCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (preparedActionRuntime != null || preparedActionPayload != null
                || preparedClearActionRuntime != null || preparedClearActionPayload != null)
                return false;

            NpcActionRuntime current = exactRuntime.CurrentActionRuntime;
            DailyV1NpcActionPayloadOwner payload = actor.ActionSlot?.CurrentPayload;
            if (current == null || payload == null
                || !ReferenceEquals(actor.ActionSlot.CurrentAction, payload.Action)
                || !ReferenceEquals(current.SelectedOwner, payload)
                || !current.MatchesSelectedOwner()
                || !ReferenceEquals(exactRuntime.CurrentAction, current.Action))
                return false;

            preparedClearActionRuntime = current;
            preparedClearActionPayload = payload;
            preparedClearActionActor = actor;
            return true;
        }

        public bool CanCommitCurrentActionClear(DailyV1NpcRuntimeOwners actor)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            return ReferenceEquals(preparedClearActionActor, actor)
                && preparedClearActionRuntime != null
                && ReferenceEquals(exactRuntime.CurrentActionRuntime, preparedClearActionRuntime)
                && ReferenceEquals(actor.ActionSlot?.CurrentAction,
                    preparedClearActionPayload?.Action)
                && ReferenceEquals(actor.ActionSlot?.CurrentPayload,
                    preparedClearActionPayload)
                && exactRuntime.CanCommitP12LifecycleMutation(false, false, true);
        }

        public bool TryPublishCurrentActionClearProjection(DailyV1NpcRuntimeOwners actor)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (!ReferenceEquals(preparedClearActionActor, actor)
                || preparedClearActionRuntime == null
                || preparedClearActionPayload == null
                || actor.ActionSlot?.CurrentAction != null
                || actor.ActionSlot?.CurrentPayload != null)
                return false;

            try
            {
                return exactRuntime.TryInstallPrecommittedCurrentActionClear(
                    preparedClearActionRuntime, preparedClearActionPayload);
            }
            finally
            {
                ClearPreparedCurrentActionClearProjection();
            }
        }

        public void CancelPreparedCurrentActionClearProjection(
            DailyV1NpcRuntimeOwners actor)
        {
            RequireExactActor(actor);
            if (ReferenceEquals(preparedClearActionActor, actor))
                ClearPreparedCurrentActionClearProjection();
        }

        private void ClearPreparedCurrentActionClearProjection()
        {
            preparedClearActionRuntime = null;
            preparedClearActionPayload = null;
            preparedClearActionActor = null;
        }

        // The legacy actor-choice boundary retains this compatibility method;
        // the admitted selected path uses the Core slot transaction above.
        public void ClearCurrentAction(DailyV1NpcRuntimeOwners actor) =>
            RequireExactActor(actor).SetCurrentActionRuntime(null);

        public void LogChosenActionTarget(DailyV1NpcRuntimeOwners actor)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (IsProjectionIndependentNormalActionSlot(actor)
                && !HasExactCurrentActionProjection(actor))
            {
                // A selected Normal action can execute from its exact Core
                // slot without a Unity action projection. Never let a stale
                // projection contribute target text to that Core-owned turn.
                return;
            }

            runtime.LogChosenTargetAction(
                exactRuntime, exactRuntime.CurrentActionRuntime);
        }

        private bool HasExactCurrentActionProjection(DailyV1NpcRuntimeOwners actor)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            NpcActionRuntime actionRuntime = exactRuntime.CurrentActionRuntime;
            if (actionRuntime == null
                || !ReferenceEquals(actionRuntime.Action, exactRuntime.CurrentAction)
                || !actionRuntime.MatchesSelectedOwner()
                || !ReferenceEquals(exactRuntime.SelectedRuntimeOwnerGraph, graph)
                || !graph.ContainsExactNpcOwner(actor)
                || !exactRuntime.TryGetSelectedDailyV1ActionNode(
                    actionRuntime.Action, out DailyV1ActionDefinition actionNode)
                || !ReferenceEquals(actor.ActionSlot?.CurrentAction, actionNode)
                || !ReferenceEquals(actor.ActionSlot?.CurrentPayload,
                    actionRuntime.SelectedOwner))
                return false;

            DailyV1NpcActionPayloadOwner payload = actor.ActionSlot?.CurrentPayload;
            if (string.IsNullOrWhiteSpace(payload?.TargetNpcRuntimeId))
                return actionRuntime.TargetNpc == null;

            return graph.TryGetNpcOwner(payload.TargetNpcRuntimeId,
                    out DailyV1NpcRuntimeOwners exactTargetOwner)
                && graph.ContainsExactNpcOwner(exactTargetOwner)
                && runtimeByOwner.TryGetValue(exactTargetOwner,
                    out NpcRuntime exactTargetRuntime)
                && ReferenceEquals(actionRuntime.TargetNpc, exactTargetRuntime);
        }

        private bool IsProjectionIndependentNormalActionSlot(DailyV1NpcRuntimeOwners actor)
        {
            DailyV1ActionDefinition action = actor?.ActionSlot?.CurrentAction;
            if (DailyV1SelectedActorActionExecutionStage.IsProjectionIndependentNormalAction(
                    graph, actor, action))
                return true;

            DailyV1NpcActionPayloadOwner payload = actor?.ActionSlot?.CurrentPayload;
            return !string.IsNullOrWhiteSpace(payload?.TargetNpcRuntimeId)
                && graph.TryGetNpcOwner(payload.TargetNpcRuntimeId,
                    out DailyV1NpcRuntimeOwners exactTarget)
                && DailyV1SelectedActorActionExecutionStage.IsProjectionIndependentNormalActionForExactTarget(
                    graph, actor, action, exactTarget);
        }

        public bool TryGetDispatchBoundary(
            DailyV1NpcRuntimeOwners actor,
            out IDailyV1SelectedActionDispatchBoundary dispatchBoundary)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            dispatchBoundary = null;
            // Unity keeps its compatibility projection check at the Unity
            // adapter boundary. Core dispatch itself depends on its exact
            // action slot and the host operation ports, not NpcActionRuntime.
            if (!HasExactCurrentActionProjection(actor)) return false;
            NpcActionRuntime actionRuntime = exactRuntime.CurrentActionRuntime;
            NpcActionData action = exactRuntime.CurrentAction;
            if (actionRuntime == null || action == null) return false;
            dispatchBoundary = new SelectedActionDispatchBoundaryAdapter(
                runtime, exactRuntime, actionRuntime, action);
            return true;
        }

        public bool TryCompleteDispatch(
            DailyV1NpcRuntimeOwners actor,
            DailyV1SelectedActionDispatchResult dispatch,
            out string resultMessage)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (dispatch?.Kind == DailyV1SelectedActionDispatchKind.Normal)
            {
                if (dispatch.ActionRollSucceeded && dispatch.Succeeded)
                {
                    if (!DailyV1SelectedActionDispatchStage.TryFormatNormalActionResultMessage(
                            actor, dispatch, out resultMessage))
                        return false;
                    if (!string.IsNullOrEmpty(resultMessage))
                        runtime.logger?.Log(SimulationLogCategory.NpcAction, resultMessage);
                    return true;
                }

                // Failure reporting still uses the established Unity failure
                // formatter; its provider-specific details are not part of a
                // successful Core-owned Normal dispatch.
                NpcActionResult failedNormalResult = runtime.CompleteSelectedActionDispatch(
                    exactRuntime, null, null, dispatch);
                if (failedNormalResult != null
                    && !string.IsNullOrEmpty(failedNormalResult.Message))
                    runtime.logger?.Log(SimulationLogCategory.NpcAction,
                        failedNormalResult.Message);
                resultMessage = failedNormalResult?.Message;
                return true;
            }

            NpcActionRuntime actionRuntime = exactRuntime.CurrentActionRuntime;
            NpcActionData action = exactRuntime.CurrentAction;
            NpcActionResult actionResult = runtime.CompleteSelectedActionDispatch(
                exactRuntime, actionRuntime, action, dispatch);
            if (actionResult != null && !string.IsNullOrEmpty(actionResult.Message))
                runtime.logger?.Log(SimulationLogCategory.NpcAction,
                    actionResult.Message);
            resultMessage = actionResult?.Message;
            return actionRuntime != null && action != null;
        }

        public bool TryGetActionStatusBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcRuntimeOwners target,
            bool reuseExistingOperation,
            out DailyV1SelectedActionExecutionStage.ISelectedActionStatusOperationBoundary boundary)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            boundary = null;
            NpcRuntime targetRuntime = null;
            if (target != null
                && (!runtimeByOwner.TryGetValue(target, out targetRuntime)
                    || !ReferenceEquals(targetRuntime.SelectedRuntimeOwners, target)))
                return false;

            NpcActionRuntime actionRuntime = exactRuntime.CurrentActionRuntime;
            bool exactProjectedAction = actionRuntime != null
                && ReferenceEquals(actionRuntime.TargetNpc, targetRuntime);
            bool projectionIndependentNormal = target == null
                ? DailyV1SelectedActorActionExecutionStage.IsProjectionIndependentNormalAction(
                    graph, actor, actor.ActionSlot?.CurrentAction)
                : DailyV1SelectedActorActionExecutionStage.IsProjectionIndependentNormalActionForExactTarget(
                    graph, actor, actor.ActionSlot?.CurrentAction, target);
            if (!exactProjectedAction && !projectionIndependentNormal)
                return false;

            boundary = new SelectedActionStatusOperationBoundaryAdapter(
                runtime, exactRuntime, targetRuntime,
                reuseForcedEscapeScope: reuseExistingOperation);
            return true;
        }

        public bool TryPrepareForcedOutcomeActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            DailyV1NpcActionPayloadOwner payload)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (preparedActionRuntime != null || preparedActionPayload != null
                || preparedClearActionRuntime != null || preparedClearActionPayload != null
                || action == null || payload == null
                || !graph.Definitions.Actions.TryGetValue(
                    action.DefinitionId, out DailyV1ActionDefinition admittedAction)
                || !ReferenceEquals(action, admittedAction)
                || action.Values.actionType?.Value != DailyV1NpcDefinitionRules.EscapePrisonActionTypeValue
                || !ReferenceEquals(payload.Action, action)
                || !exactRuntime.TryGetSelectedDailyV1ActionAsset(action, out NpcActionData actionAsset))
                return false;

            NpcActionRuntime actionRuntime = new NpcActionRuntime(actionAsset);
            if (!actionRuntime.TryUseSelectedPayload(payload, graph.Definitions)
                || !actionRuntime.MatchesSelectedOwner())
                return false;

            preparedActionRuntime = actionRuntime;
            preparedActionPayload = payload;
            preparedActionActor = actor;
            return true;
        }

        public bool UsesSelectedDailyV1ActionExecution(DailyV1NpcRuntimeOwners actor)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            return exactRuntime.UsesSelectedDailyV1Definitions
                && ReferenceEquals(exactRuntime.SelectedRuntimeOwnerGraph, graph)
                && ReferenceEquals(graph.Definitions, actor.Definitions);
        }

        public void LogSkippedDirective(ScheduledDirective directive, string reason)
        {
            runtime.logger?.LogWarning(
                $"Scheduled directive '{directive?.DirectiveId}' was skipped: {reason}");
        }

        public bool TryGetForcedEscapeBoundary(
            DailyV1NpcRuntimeOwners actor,
            DailyV1ActionDefinition action,
            out DailyV1SelectedCrimeActionExecutionStage.ISelectedCrimeActionBoundary boundary)
        {
            boundary = null;
            if (!TryGetDispatchBoundary(actor,
                    out IDailyV1SelectedActionDispatchBoundary dispatchBoundary)
                || dispatchBoundary == null)
                return false;
            return dispatchBoundary.TryGetForcedEscapeBoundary(actor, action, out boundary);
        }

        private NpcRuntime RequireExactActor(DailyV1NpcRuntimeOwners actor)
        {
            if (actor == null
                || !runtimeByOwner.TryGetValue(actor, out NpcRuntime exactRuntime)
                || !ReferenceEquals(exactRuntime.SelectedRuntimeOwners, actor)
                || !ReferenceEquals(runtime.SelectedRuntimeOwnerGraph, graph)
                || !graph.ContainsExactNpcOwner(actor))
                throw new InvalidOperationException(
                    "Selected actor-turn adapter did not receive the exact runtime owner instance.");
            return exactRuntime;
        }

        private bool TryGetExactActor(
            DailyV1NpcRuntimeOwners actor,
            out NpcRuntime exactRuntime)
        {
            exactRuntime = null;
            if (actor == null || !runtimeByOwner.TryGetValue(actor, out NpcRuntime candidate)
                || candidate == null || !ReferenceEquals(candidate.SelectedRuntimeOwners, actor)
                || !ReferenceEquals(runtime.SelectedRuntimeOwnerGraph, graph)
                || !graph.ContainsExactNpcOwner(actor)) return false;
            exactRuntime = candidate;
            return true;
        }
    }

    internal InvalidOperationException CreateAdvanceFailureException(
        SimulationRuntimeAdvanceFailure failure)
    {
        if (failure == SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow)
        {
            return new InvalidOperationException("SimulationTime cannot advance beyond the maximum AbsoluteDay.");
        }

        if (failure == SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress)
        {
            return new InvalidOperationException("A SimulationRuntime advance is already in progress.");
        }

        if (failure == SimulationRuntimeAdvanceFailure.TemporalAdvanceFailed)
        {
            return new InvalidOperationException(
                "The P18-D intraday advance failed: " + p18dLastTimelineFailure + ".");
        }

        if (failure == SimulationRuntimeAdvanceFailure.CompletedBoundarySequenceOverflow)
        {
            return new InvalidOperationException(
                "The P12 completed-day sequence cannot advance beyond its maximum value.");
        }

        return new InvalidOperationException("A faulted SimulationRuntime cannot advance its world.");
    }

    internal bool TryCaptureActorChoiceInput(
        string worldCommandId,
        PersonId personId,
        string actionDefinitionId,
        WorldCommandOrigin origin,
        WorldCommandAuthorityMode authority,
        out ActorChoiceStoreFailureCode failure)
    {
        return actorChoiceStore.TryCapture(
            worldCommandId,
            personId,
            actionDefinitionId,
            origin,
            authority,
            CurrentDay,
            out _,
            out failure);
    }

    private bool TryDeferActorChoiceForSpatialTransit(NpcRuntime npcRuntime, int actorTurnRosterOrdinal)
    {
        if (npcRuntime == null
            || !HasPendingActorChoiceFor(npcRuntime.PersonId)
            || !personSpatialPositionStore.TryGetPosition(npcRuntime.PersonId, out PersonSpatialPosition position)
            || !position.IsInTransit)
        {
            return false;
        }

        TryProcessScheduledDirective(npcRuntime);
        DeferActorChoiceForPerson(
            npcRuntime.PersonId,
            actorTurnRosterOrdinal,
            ActorChoiceDeferralReason.Traveling);
        return true;
    }

    private bool HasPendingActorChoiceFor(PersonId personId)
    {
        return personId != null
            && actorChoiceStore.TryGetNextPendingForActor(personId, out _);
    }

    private bool IsCurrentMaterializedPersonActor(NpcRuntime npcRuntime)
    {
        if (npcRuntime?.PersonId == null
            || !personStore.TryGet(npcRuntime.PersonId, out PersonRuntime person)
            || person.IsDeadAt(CurrentDay)
            || !person.IsMaterialized
            || !string.Equals(person.MaterializedNpcRuntimeId, npcRuntime.RuntimeId, StringComparison.Ordinal)
            || !personStore.TryGetByMaterializedNpcRuntimeId(npcRuntime.RuntimeId, out PersonRuntime byNpcId)
            || !ReferenceEquals(person, byNpcId))
        {
            return false;
        }

        return true;
    }

    private void RejectActorChoicesForPerson(PersonId personId, int actorTurnRosterOrdinal)
    {
        if (personId == null)
        {
            return;
        }

        foreach (ActorChoiceInput input in actorChoiceStore.PendingInputs)
        {
            if (input.PersonId.Equals(personId))
            {
                RejectActorChoice(input, actorTurnRosterOrdinal, ActorChoiceFailure.ActorUnavailable);
            }
        }
    }

    private void DeferActorChoiceForPerson(
        PersonId personId,
        int actorTurnRosterOrdinal,
        ActorChoiceDeferralReason reason)
    {
        if (actorChoiceStore.TryGetNextPendingForActor(personId, out ActorChoiceInput input))
        {
            if (!actorChoiceStore.TryDefer(
                    input.InputId,
                    CurrentDay,
                    actorTurnRosterOrdinal,
                    reason,
                    out ActorChoiceStoreFailureCode failure))
            {
                throw new InvalidOperationException(
                    "Actor choice could not record its required deferral: " + failure + ".");
            }
        }
    }

    private void RejectActorChoice(
        ActorChoiceInput input,
        int actorTurnRosterOrdinal,
        ActorChoiceFailure reason)
    {
        if (input == null)
        {
            throw new InvalidOperationException(
                "Actor choice could not record its required rejection: missing input.");
        }

        if (!actorChoiceStore.TryReject(
                input.InputId,
                CurrentDay,
                actorTurnRosterOrdinal,
                reason,
                out ActorChoiceStoreFailureCode failure))
        {
            throw new InvalidOperationException(
                "Actor choice could not record its required rejection: " + failure + ".");
        }
    }

    private void RejectActorChoicesWithoutMaterializedTurn(int actorTurnRosterOrdinal)
    {
        HashSet<PersonId> presentActors = new HashSet<PersonId>();
        foreach (NpcRuntime npcRuntime in npcRuntimes)
        {
            if (npcRuntime?.PersonId != null)
            {
                presentActors.Add(npcRuntime.PersonId);
            }
        }

        foreach (ActorChoiceInput input in actorChoiceStore.PendingInputs)
        {
            if (!presentActors.Contains(input.PersonId))
            {
                RejectActorChoice(input, actorTurnRosterOrdinal, ActorChoiceFailure.ActorUnavailable);
            }
        }
    }

    private sealed class SelectedActorChoiceExecutionBoundary : IDailyV1ActorChoiceExecutionBoundary
    {
        private readonly SimulationRuntime runtime;
        private readonly NpcRuntime npcRuntime;
        private DailyV1NpcActionPayloadOwner preparedPayload;
        private NpcActionRuntime preparedAction;

        internal SelectedActorChoiceExecutionBoundary(SimulationRuntime runtime, NpcRuntime npcRuntime)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.npcRuntime = npcRuntime ?? throw new ArgumentNullException(nameof(npcRuntime));
        }

        public void ClearCurrentAction(DailyV1NpcRuntimeOwners actor)
        {
            RequireExactActor(actor).SetCurrentActionRuntime(null);
        }

        public bool IsActionEnabled(DailyV1NpcRuntimeOwners actor, DailyV1ActionDefinition action)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            return action != null
                && exactRuntime.TryGetSelectedDailyV1ActionAsset(action, out NpcActionData actionAsset)
                && exactRuntime.GetConfiguredActionType(actionAsset) == NpcActionType.SellGoods
                && DailyV1SelectedNpcActionDecisionStage.IsEnabledForRuntime(
                    exactRuntime.SelectedDailyV1DefinitionGraph, action);
        }

        public bool TryGetDecisionRecordOwners(
            DailyV1NpcRuntimeOwners actor,
            out RuntimeIdAllocator idAllocator,
            out SimulationRecordSequence recordSequence,
            out NpcDecisionStore decisionStore)
        {
            RequireExactActor(actor);
            NpcDecisionRecorder recorder = runtime.decisionRecorder;
            idAllocator = recorder?.DecisionIdAllocator;
            recordSequence = recorder?.RecordSequence;
            decisionStore = recorder?.DecisionStore;
            return idAllocator != null && recordSequence != null && decisionStore != null;
        }

        public bool TryPrepareRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            preparedPayload = null;
            preparedAction = null;
            if (runtime.npcDecisionSystem == null
                || !runtime.npcDecisionSystem.TryProjectSelectedLocalSale(
                    exactRuntime, payload, out NpcActionRuntime projectedAction))
                return false;

            preparedPayload = payload;
            preparedAction = projectedAction;
            return true;
        }

        public bool TryRefreshRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            return ReferenceEquals(preparedPayload, payload)
                && preparedAction != null
                && preparedAction.TryUseSelectedPayload(
                    payload, exactRuntime.SelectedDailyV1DefinitionGraph);
        }

        public bool CanCommitRequestedActionSlot(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            return ReferenceEquals(preparedPayload, payload)
                && preparedAction != null
                && exactRuntime.CanCommitP12LifecycleMutation(
                    false, false, true, preparedAction);
        }

        public bool TryPublishRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            if (!ReferenceEquals(preparedPayload, payload) || preparedAction == null
                || !exactRuntime.TryInstallPrecommittedCurrentActionRuntime(preparedAction))
                return false;

            preparedPayload = null;
            preparedAction = null;
            return true;
        }

        public void CancelPreparedRequestedActionProjection(
            DailyV1NpcRuntimeOwners actor,
            DailyV1NpcActionPayloadOwner payload)
        {
            RequireExactActor(actor);
            if (ReferenceEquals(preparedPayload, payload))
            {
                preparedPayload = null;
                preparedAction = null;
            }
        }

        public void LogRequestedActionDecisionFailure(string message)
        {
            runtime.logger?.LogError(message);
        }

        public NpcActionResultType? ExecuteCurrentAction(DailyV1NpcRuntimeOwners actor)
        {
            NpcRuntime exactRuntime = RequireExactActor(actor);
            DailyV1SelectedRuntimeOwnerGraph graph = runtime.SelectedRuntimeOwnerGraph;
            Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> runtimeByOwner =
                new Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime>();
            foreach (NpcRuntime candidate in runtime.npcRuntimes)
            {
                DailyV1NpcRuntimeOwners candidateOwner = candidate?.SelectedRuntimeOwners;
                if (candidateOwner != null && graph.ContainsExactNpcOwner(candidateOwner))
                    runtimeByOwner.Add(candidateOwner, candidate);
            }

            SelectedDailyV1ActorTurnExecutionBoundary boundary =
                new SelectedDailyV1ActorTurnExecutionBoundary(runtime, graph, runtimeByOwner);
            if (!DailyV1SelectedActorActionExecutionStage.TryExecuteInstalledAction(
                    graph, actor, runtime.randomSource, runtime.CurrentDay, boundary,
                    out _, out NpcActionResultType? resultType, out _))
                throw new InvalidOperationException(
                    "The selected actor-choice action could not execute through its exact Core owner graph.");
            return resultType;
        }

        private NpcRuntime RequireExactActor(DailyV1NpcRuntimeOwners actor)
        {
            if (actor == null
                || !ReferenceEquals(npcRuntime.SelectedRuntimeOwners, actor)
                || !runtime.SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(actor))
                throw new InvalidOperationException(
                    "Selected actor-choice adapter did not receive the exact runtime owner instance.");
            return npcRuntime;
        }
    }

    private bool TryProcessActorChoice(NpcRuntime npcRuntime, int actorTurnRosterOrdinal)
    {
        if (npcRuntime?.PersonId == null)
        {
            return false;
        }

        if (npcRuntime.UsesSelectedDailyV1Definitions
            && npcRuntime.SelectedRuntimeOwners != null
            && SelectedRuntimeOwnerGraph != null)
        {
            DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
            return DailyV1ActorChoiceExecutionStage.TryProcessPending(
                graph,
                npcRuntime.SelectedRuntimeOwners,
                npcRuntime.PersonId,
                actorChoiceStore,
                CurrentDay,
                actorTurnRosterOrdinal,
                graph.IsActorAtCurrentCityLocation(npcRuntime.SelectedRuntimeOwners),
                graph.Definitions.EffectiveConfiguration?.MerchantTrade?.Enabled == true,
                new SelectedActorChoiceExecutionBoundary(this, npcRuntime));
        }

        if (!actorChoiceStore.TryGetNextPendingForActor(npcRuntime.PersonId, out ActorChoiceInput input))
        {
            return false;
        }

        // A captured choice replaces this actor's ordinary decision slot. Clear
        // a prior day's action even when today's current-truth checks reject it.
        npcRuntime.SetCurrentActionRuntime(null);

        if (npcRuntime.CurrentCity == null
            || !IsActorAtCurrentCityLocation(npcRuntime))
        {
            RejectActorChoice(input, actorTurnRosterOrdinal, ActorChoiceFailure.ActionUnavailable);
            return true;
        }

        if (configuration.MerchantTrade.Enabled == false
            || npcRuntime.MerchantTradePlan.IsActive
            || npcDecisionSystem == null)
        {
            RejectActorChoice(input, actorTurnRosterOrdinal, ActorChoiceFailure.ActionUnavailable);
            return true;
        }

        NpcActionData requestedDefinition = null;
        int matchingDefinitions = 0;
        if (npcRuntime.UsesSelectedDailyV1Definitions)
        {
            if (npcRuntime.TryGetSelectedDailyV1ActionByDefinitionId(input.ActionDefinitionId, out requestedDefinition))
                matchingDefinitions = 1;
        }
        else if (configuredActions != null)
        {
            foreach (NpcActionData configuredAction in configuredActions)
            {
                if (configuredAction != null
                    && string.Equals(
                        configuredAction.DefinitionId,
                        input.ActionDefinitionId,
                        StringComparison.Ordinal))
                {
                    requestedDefinition = configuredAction;
                    matchingDefinitions++;
                }
            }
        }

        if (matchingDefinitions != 1
            || requestedDefinition == null
            || npcRuntime.GetConfiguredActionType(requestedDefinition) != NpcActionType.SellGoods
            || !IsActionEnabledForRuntime(npcRuntime, requestedDefinition))
        {
            RejectActorChoice(input, actorTurnRosterOrdinal, ActorChoiceFailure.ActionUnavailable);
            return true;
        }

        NpcActionRuntime requestedAction = npcDecisionSystem.CreateRequestedAction(
            npcRuntime, requestedDefinition, CurrentDay);
        if (requestedAction == null
            || !ReferenceEquals(requestedAction.Action, requestedDefinition)
            || npcRuntime.GetConfiguredActionType(requestedAction.Action) != NpcActionType.SellGoods
            || requestedAction.TargetCity != npcRuntime.CurrentCity
            || requestedAction.TargetNpc != null
            || requestedAction.TargetItem == null
            || requestedAction.Amount <= 0)
        {
            RejectActorChoice(input, actorTurnRosterOrdinal, ActorChoiceFailure.ActionUnavailable);
            return true;
        }

        NpcDecisionRecord decision = RecordAndInstallChosenAction(
            npcRuntime, requestedAction, NpcDecisionOrigin.ActorChoice);

        if (!actorChoiceStore.TryMarkDispatchStarted(
                input.InputId,
                CurrentDay,
                actorTurnRosterOrdinal,
                decision?.DecisionId,
                out ActorChoiceStoreFailureCode startFailure))
        {
            throw new InvalidOperationException(
                "Actor choice could not cross its dispatch boundary: " + startFailure + ".");
        }

        try
        {
            NpcActionResult result = TryExecuteCurrentAction(npcRuntime);
            if (!actorChoiceStore.TryRecordAttemptReturned(
                    input.InputId,
                    CurrentDay,
                    actorTurnRosterOrdinal,
                    result?.ResultType,
                    out ActorChoiceStoreFailureCode returnFailure))
            {
                throw new InvalidOperationException(
                    "Actor choice could not record its returned attempt: " + returnFailure + ".");
            }
        }
        catch (Exception exception)
        {
            if (!actorChoiceStore.TryRecordAttemptThrew(
                    input.InputId,
                    CurrentDay,
                    actorTurnRosterOrdinal,
                    out ActorChoiceStoreFailureCode throwFailure))
            {
                throw new InvalidOperationException(
                    "Actor choice threw and its terminal attempt could not be recorded: "
                    + throwFailure
                    + ".",
                    exception);
            }

            throw;
        }

        return true;
    }

    private bool IsActorAtCurrentCityLocation(NpcRuntime npcRuntime)
    {
        if (!personSpatialPositionStore.TryGetPosition(npcRuntime.PersonId, out PersonSpatialPosition position))
        {
            // The P8-C position is optional for legacy actors. Preserve their
            // existing CurrentCity contract when no factual position is stored.
            return true;
        }

        if (position.IsInTransit)
        {
            return false;
        }

        if (npcRuntime.CurrentCity == null)
        {
            return false;
        }

        SpatialAnchorOwnerId cityOwner = new SpatialAnchorOwnerId(
            SpatialAnchorOwnerKind.City,
            npcRuntime.CurrentCity.RuntimeId);
        if (!legacySpatialAnchorBindingStore.TryGet(cityOwner, out LocationId cityLocationId)
            || !spatialAuthorityStore.TryGet(cityLocationId, out _))
        {
            return false;
        }

        return position.Position != null
            && position.Position.Kind == StablePositionReferenceKind.Location
            && position.Position.LocationId.Equals(cityLocationId);
    }

    internal GenealogyStore GenealogyStoreForWorldBoundary => genealogyStore;

    internal InstitutionStore InstitutionStoreForWorldBoundary => institutionStore;

    internal OfficeStore OfficeStoreForWorldBoundary => officeStore;

    private bool TryValidatePoliticalClaimTarget(
        PoliticalClaimRecord record,
        out PoliticalClaimFailure failure)
    {
        failure = PoliticalClaimFailure.None;
        if (record.Target == null
            || PoliticalClaimRecord.IsTargetCompatible(record.ClaimType, record.Target.Kind) == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.ClaimTargetTypeMismatch,
                "The political claim target kind does not match the claim type.");
            return false;
        }

        bool found;
        switch (record.Target.Kind)
        {
            case PoliticalClaimTargetKind.Office:
                found = officeStore.TryGet(new OfficeId(record.Target.TargetId), out _);
                break;
            case PoliticalClaimTargetKind.Property:
                found = propertyOwnershipStore.TryGet(new PropertyId(record.Target.TargetId), out _);
                break;
            case PoliticalClaimTargetKind.Institution:
                found = institutionStore.TryGet(new InstitutionId(record.Target.TargetId), out _);
                break;
            case PoliticalClaimTargetKind.Person:
                found = personStore.TryGet(new PersonId(record.Target.TargetId), out _);
                break;
            default:
                found = false;
                break;
        }

        if (found == false)
        {
            failure = PoliticalClaimFailure.Create(
                PoliticalClaimFailureCode.ClaimTargetNotFound,
                "The political claim target must exist in this world.");
            return false;
        }

        return true;
    }

    private static void ValidateGenealogyStore(PersonStore persons, GenealogyStore genealogy)
    {
        foreach (ParentageRecord record in genealogy.Records)
        {
            if (record == null
                || persons.TryGet(record.ParentId, out _) == false
                || persons.TryGet(record.ChildId, out _) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime GenealogyStore contains parentage endpoints absent from PersonStore.",
                    nameof(genealogy));
            }
        }
    }

    private static ArmedForceStore CloneArmedForceStore(
        ArmedForceStore source,
        PersonStore personStore)
    {
        if (source == null)
        {
            return new ArmedForceStore(personStore);
        }

        ArmedForceInvariantReport report = source.ValidateInvariants();
        if (report.IsValid == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime ArmedForceStore contains invalid world state: "
                + string.Join("; ", report.Violations),
                nameof(source));
        }

        return source.CloneForRuntime(personStore);
    }

    private static SpatialAuthorityStore CloneSpatialAuthorityStore(SpatialAuthorityStore source)
    {
        if (source == null)
        {
            return new SpatialAuthorityStore();
        }

        SpatialAuthorityInvariantReport report = source.ValidateInvariants();
        if (report.IsValid == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime SpatialAuthorityStore contains invalid world state: "
                + string.Join("; ", report.Violations),
                nameof(source));
        }

        return source.Clone();
    }

    private static SpatialAuthorityStore RetainSelectedSpatialAuthorityStore(SpatialAuthorityStore source)
    {
        if (source == null)
            throw new ArgumentException(
                "An admitted Core Daily-v1 runtime requires its exact P8 SpatialAuthorityStore owner.",
                nameof(source));

        SpatialAuthorityInvariantReport report = source.ValidateInvariants();
        if (report.IsValid == false)
        {
            throw new ArgumentException(
                "The Core-owned SimulationRuntime SpatialAuthorityStore contains invalid world state: "
                + string.Join("; ", report.Violations),
                nameof(source));
        }

        return source;
    }

    private static DailyV1SelectedRuntimeOwnerGraph GetAdmittedSelectedCoreOwnerGraph(
        DailyV1ExecutionBoundaryState executionBoundaryState)
    {
        if (executionBoundaryState == null
            || !executionBoundaryState.HasAdmittedSelectedDailyExecution
            || executionBoundaryState.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
            return null;
        return executionBoundaryState.SelectedRuntimeOwnerGraph;
    }

    private static LegacySpatialAnchorBindingStore RetainSelectedCoreLegacySpatialAnchorBindingStore(
        LegacySpatialAnchorBindingStore supplied,
        DailyV1SelectedRuntimeOwnerGraph selectedGraph,
        SpatialAuthorityStore spatialAuthorityStore)
    {
        LegacySpatialAnchorBindingStore owner = selectedGraph?.LegacySpatialAnchors;
        if (owner == null || (supplied != null && !ReferenceEquals(supplied, owner))
            || !ReferenceEquals(owner.SpatialLocationLookup, spatialAuthorityStore))
            throw new ArgumentException(
                "The selected Core graph must retain the exact P8 legacy-anchor owner bound to its spatial authority.",
                nameof(supplied));

        SpatialAnchorBindingInvariantReport report = owner.ValidateInvariants();
        if (!report.IsValid)
            throw new ArgumentException(
                "The selected Core P8 legacy-anchor owner is invalid: "
                + string.Join("; ", report.Violations), nameof(supplied));
        return owner;
    }

    private static PersonSpatialPositionStore RetainSelectedCorePersonSpatialPositionStore(
        PersonSpatialPositionStore supplied,
        DailyV1SelectedRuntimeOwnerGraph selectedGraph,
        PersonStore personStore,
        SpatialAuthorityStore spatialAuthorityStore)
    {
        PersonSpatialPositionStore owner = selectedGraph?.PersonSpatialPositionStore;
        if (owner == null || (supplied != null && !ReferenceEquals(supplied, owner))
            || !ReferenceEquals(owner.PersonStore, personStore)
            || !ReferenceEquals(owner.SpatialAuthorityStore, spatialAuthorityStore))
            throw new ArgumentException(
                "The selected Core graph must retain the exact P8 Person-position owner bound to its Person and spatial authorities.",
                nameof(supplied));

        PersonSpatialPositionInvariantReport report = owner.ValidateInvariants();
        if (!report.IsValid)
            throw new ArgumentException(
                "The selected Core P8 Person-position owner is invalid: "
                + string.Join("; ", report.Violations), nameof(supplied));
        return owner;
    }
    private static SpatialRouteKnowledgeStore RetainSelectedCoreSpatialRouteKnowledgeStore(
        SpatialRouteKnowledgeStore supplied,
        DailyV1SelectedRuntimeOwnerGraph selectedGraph,
        PersonStore personStore)
    {
        SpatialRouteKnowledgeStore owner = selectedGraph?.SpatialRouteKnowledgeStore;
        if (owner == null || (supplied != null && !ReferenceEquals(supplied, owner))
            || !owner.IsBoundToPersonStore(personStore))
            throw new ArgumentException(
                "The selected Core graph must retain the exact P8-D route Knowledge owner bound to its PersonStore.",
                nameof(supplied));
        return owner;
    }

    private static PersonRoutePlanStore RetainSelectedCorePersonRoutePlanStore(
        PersonRoutePlanStore supplied,
        DailyV1SelectedRuntimeOwnerGraph selectedGraph,
        PersonStore personStore,
        SpatialRouteKnowledgeStore knowledgeStore)
    {
        PersonRoutePlanStore owner = selectedGraph?.PersonRoutePlanStore;
        if (owner == null || (supplied != null && !ReferenceEquals(supplied, owner))
            || !owner.IsBoundTo(personStore, knowledgeStore))
            throw new ArgumentException(
                "The selected Core graph must retain the exact P8-D PersonRoutePlanStore bound to its Person and Knowledge owners.",
                nameof(supplied));
        return owner;
    }

    private static SpatialRoutePlanningSystem RetainSelectedCoreSpatialRoutePlanningSystem(
        DailyV1SelectedRuntimeOwnerGraph selectedGraph,
        PersonStore personStore,
        SpatialAuthorityStore spatialAuthorityStore,
        SpatialRouteKnowledgeStore knowledgeStore)
    {
        SpatialRoutePlanningSystem planner = selectedGraph?.SpatialRoutePlanningSystem;
        if (planner == null || !planner.IsBoundTo(personStore, spatialAuthorityStore, knowledgeStore))
            throw new ArgumentException(
                "The selected Core graph must retain its exact P8-E route planner over the admitted owners.",
                nameof(selectedGraph));
        return planner;
    }

    private static P8ETravelTransactionCoordinator RetainSelectedCoreP8ETravelCoordinator(
        DailyV1SelectedRuntimeOwnerGraph selectedGraph,
        PersonSpatialPositionStore positions,
        PersonRoutePlanStore plans,
        SpatialRouteKnowledgeStore knowledge,
        SpatialPassageAuthority passageAuthority)
    {
        P8ETravelTransactionCoordinator coordinator = selectedGraph?.P8ETravelCoordinator;
        if (coordinator == null
            || !coordinator.IsBoundTo(positions, plans, knowledge, passageAuthority))
            throw new ArgumentException(
                "The selected Core graph must retain its exact P8-E transaction coordinator over the admitted owners.",
                nameof(selectedGraph));
        return coordinator;
    }

    private static LegacySpatialAnchorBindingStore CloneLegacySpatialAnchorBindingStore(
        LegacySpatialAnchorBindingStore source,
        SpatialAuthorityStore spatialAuthorityStore,
        IReadOnlyList<CityRuntime> cities,
        ExplorableSiteStore sites)
    {
        Func<SpatialAnchorOwnerId, bool> ownerIsComposed = owner =>
        {
            if (owner == null) return false;
            if (owner.Kind == SpatialAnchorOwnerKind.City)
            {
                foreach (CityRuntime city in cities ?? Array.Empty<CityRuntime>())
                    if (city != null && string.Equals(city.RuntimeId, owner.Value, StringComparison.Ordinal)) return true;
                return false;
            }
            return owner.Kind == SpatialAnchorOwnerKind.ExplorableSite
                && sites != null
                && sites.TryGetByRuntimeId(owner.Value, out _);
        };

        if (source == null)
        {
            return new LegacySpatialAnchorBindingStore(spatialAuthorityStore, ownerIsComposed);
        }

        SpatialAnchorBindingInvariantReport report = source.ValidateInvariants();
        if (!report.IsValid)
        {
            throw new ArgumentException(
                "The SimulationRuntime LegacySpatialAnchorBindingStore contains invalid world state: "
                + string.Join("; ", report.Violations),
                nameof(source));
        }

        foreach (SpatialAnchorBinding binding in source.Bindings)
        {
            bool ownerExists = false;
            if (binding.OwnerId.Kind == SpatialAnchorOwnerKind.City)
            {
                foreach (CityRuntime city in cities ?? Array.Empty<CityRuntime>())
                {
                    if (city != null && string.Equals(city.RuntimeId, binding.OwnerId.Value, StringComparison.Ordinal))
                    {
                        ownerExists = true;
                        break;
                    }
                }
            }
            else if (binding.OwnerId.Kind == SpatialAnchorOwnerKind.ExplorableSite)
            {
                ownerExists = sites != null && sites.TryGetByRuntimeId(binding.OwnerId.Value, out _);
            }

            if (!ownerExists)
            {
                throw new ArgumentException(
                    "The SimulationRuntime LegacySpatialAnchorBindingStore references a City/Site absent from the composed world: "
                    + binding.OwnerId.StableKey + ".",
                    nameof(source));
            }
        }

        return source.Clone(spatialAuthorityStore, ownerIsComposed);
    }

    private static PersonSpatialPositionStore ClonePersonSpatialPositionStore(
        PersonSpatialPositionStore source,
        PersonStore personStore,
        SpatialAuthorityStore spatialAuthorityStore,
        ISpatialTraversalOptionResolver traversalOptionResolver)
    {
        if (source == null)
        {
            return new PersonSpatialPositionStore(personStore, spatialAuthorityStore, traversalOptionResolver);
        }

        PersonSpatialPositionInvariantReport report = source.ValidateInvariants();
        if (!report.IsValid)
        {
            throw new ArgumentException(
                "The SimulationRuntime PersonSpatialPositionStore contains invalid world state: "
                + string.Join("; ", report.Violations),
                nameof(source));
        }

        return source.Clone(personStore, spatialAuthorityStore, traversalOptionResolver);
    }

    private static SpatialRouteKnowledgeStore CloneSpatialRouteKnowledgeStore(
        SpatialRouteKnowledgeStore source,
        PersonStore personStore,
        long currentWorldDay)
    {
        if (source == null)
        {
            return new SpatialRouteKnowledgeStore(personStore);
        }

        SpatialKnowledgeInvariantReport report = source.ValidateInvariants();
        if (!report.IsValid)
        {
            throw new ArgumentException(
                "The SimulationRuntime SpatialRouteKnowledgeStore contains invalid world state: "
                + string.Join("; ", report.Violations),
                nameof(source));
        }

        foreach (PersonId actor in source.Actors)
        {
            foreach (SpatialObservation observation in source.GetObservations(actor))
            {
                if (observation != null
                    && (observation.ObservedDay > currentWorldDay
                        || observation.ReceivedDay > currentWorldDay))
                {
                    throw new ArgumentException(
                        "The SimulationRuntime SpatialRouteKnowledgeStore contains an observation later than the target SimulationTime.AbsoluteDay.",
                        nameof(source));
                }
            }
        }

        return source.Clone(personStore);
    }

    private static PersonRoutePlanStore ClonePersonRoutePlanStore(
        PersonRoutePlanStore source,
        PersonStore personStore,
        SpatialRouteKnowledgeStore knowledgeStore,
        Func<PersonId, bool> actorInTransitProvider,
        Func<long> currentWorldDayProvider,
        long currentWorldDay)
    {
        if (source == null)
        {
            return new PersonRoutePlanStore(personStore, knowledgeStore, currentWorldDayProvider, actorInTransitProvider);
        }

        PersonRoutePlanInvariantReport report = source.ValidateInvariants();
        if (!report.IsValid)
        {
            throw new ArgumentException(
                "The SimulationRuntime PersonRoutePlanStore contains invalid world state: "
                + string.Join("; ", report.Violations),
                nameof(source));
        }

        foreach (PersonRoutePlan plan in source.History)
        {
            if (plan != null && plan.AcceptedDay > currentWorldDay)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PersonRoutePlanStore contains a plan accepted later than the target SimulationTime.AbsoluteDay.",
                    nameof(source));
            }
        }

        return source.CloneForRuntime(personStore, knowledgeStore, currentWorldDayProvider, actorInTransitProvider);
    }

    private static ArmedForceSpatialStateStore CloneArmedForceSpatialStateStore(
        ArmedForceSpatialStateStore source,
        ArmedForceStore armedForceStore,
        SpatialAuthorityStore spatialAuthorityStore,
        LocalTopologyStore localTopologyStore)
    {
        if (source == null)
        {
            return new ArmedForceSpatialStateStore(
                armedForceStore,
                spatialAuthorityStore,
                localTopologyStore);
        }

        ArmedForceSpatialInvariantReport report = source.ValidateInvariants();
        if (report.IsValid == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime ArmedForce spatial state contains invalid world state: "
                + string.Join("; ", report.Violations),
                nameof(source));
        }

        return source.Clone(
            armedForceStore,
            spatialAuthorityStore,
            localTopologyStore);
    }

    private static PersistentConflictStore CloneConflictStore(
        PersistentConflictStore source,
        ArmedForceStore armedForceStore)
    {
        if (source == null)
        {
            return new PersistentConflictStore(armedForceStore);
        }

        PersistentStateInvariantReport report = source.ValidateInvariants();
        if (report.IsValid == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime ConflictStore contains invalid world state: "
                + string.Join("; ", report.Violations),
                nameof(source));
        }

        return source.Clone(armedForceStore);
    }

    private static PersistentWarStore CloneWarStore(
        PersistentWarStore source,
        ArmedForceStore armedForceStore,
        PersistentConflictStore conflictStore,
        FactionStore factionStore,
        SpatialAuthorityStore spatialAuthorityStore,
        ArmedForceSpatialStateStore p16StateStore)
    {
        if (source == null)
        {
            return new PersistentWarStore(armedForceStore, conflictStore);
        }

        PersistentStateInvariantReport report = source.ValidateInvariants();
        if (report.IsValid == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime WarStore contains invalid world state: "
                + string.Join("; ", report.Violations),
                nameof(source));
        }

        return source.Clone(
            armedForceStore,
            conflictStore,
            factionStore,
            spatialAuthorityStore,
            p16StateStore);
    }

    private static PersistentBattleStore CloneBattleStore(
        PersistentBattleStore source,
        ArmedForceStore armedForceStore,
        PersistentConflictStore conflictStore,
        PersistentWarStore warStore,
        SpatialAuthorityStore spatialAuthorityStore)
    {
        if (source == null)
        {
            return new PersistentBattleStore(armedForceStore, conflictStore, warStore, spatialAuthorityStore);
        }

        PersistentStateInvariantReport report = source.ValidateInvariants();
        if (report.IsValid == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime BattleStore contains invalid world state: "
                + string.Join("; ", report.Violations),
                nameof(source));
        }

        return source.Clone(armedForceStore, conflictStore, warStore, spatialAuthorityStore);
    }

    private static GenealogyStore CloneGenealogyStore(GenealogyStore source)
    {
        GenealogyStore copy = new GenealogyStore();
        foreach (ParentageRecord record in source.Records)
        {
            if (copy.TryAddParentage(record, out GenealogyFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime GenealogyStore contains an invalid parentage record.",
                    nameof(source));
            }
        }

        return copy;
    }

    private static GenealogyStore CloneRestoredGenealogyStore(GenealogyStore source)
    {
        GenealogyOwnerExport ownerExport = GenealogyOwnerBridge.CaptureOwnerSnapshot(source);
        if (!GenealogyOwnerBridge.TryCreateStagedOwner(
                ownerExport,
                out GenealogyStore copy,
                out GenealogyFailure failure)
            || copy == null)
        {
            throw new ArgumentException(
                "The restored SimulationRuntime GenealogyStore could not preserve its owner export: "
                + (failure?.ToString() ?? "unknown failure"),
                nameof(source));
        }

        return copy;
    }

    private static PoliticalClaimStore ClonePoliticalClaimStore(
        PoliticalClaimStore source,
        PersonStore personStore,
        InstitutionStore institutionStore,
        OfficeStore officeStore,
        PropertyOwnershipStore propertyOwnershipStore,
        long currentDay,
        bool isRestoredContinuation)
    {
        PoliticalClaimStore copy = new PoliticalClaimStore();
        if (source == null)
        {
            return copy;
        }

        foreach (PoliticalClaimRecord record in source.Records)
        {
            if (record == null
                || record.CreatedAbsoluteDay > currentDay
                || personStore.TryGet(record.ClaimantPersonId, out _) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalClaimStore contains a claim inconsistent with PersonStore or world time.",
                    nameof(source));
            }

            bool targetExists;
            switch (record.Target.Kind)
            {
                case PoliticalClaimTargetKind.Office:
                    targetExists = officeStore.TryGet(new OfficeId(record.Target.TargetId), out _);
                    break;
                case PoliticalClaimTargetKind.Property:
                    targetExists = propertyOwnershipStore.TryGet(new PropertyId(record.Target.TargetId), out _);
                    break;
                case PoliticalClaimTargetKind.Institution:
                    targetExists = institutionStore.TryGet(new InstitutionId(record.Target.TargetId), out _);
                    break;
                case PoliticalClaimTargetKind.Person:
                    targetExists = personStore.TryGet(new PersonId(record.Target.TargetId), out _);
                    break;
                default:
                    targetExists = false;
                    break;
            }

            if (targetExists == false
                || (record.ResolutionAbsoluteDay.HasValue && record.ResolutionAbsoluteDay.Value > currentDay))
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalClaimStore contains a claim target or recognition state inconsistent with the world.",
                    nameof(source));
            }

            if (copy.TryRegister(record, out PoliticalClaimFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalClaimStore contains an invalid claim: " + failure + ".",
                    nameof(source));
            }
        }

        foreach (PoliticalClaimRecognitionRecord recognition in source.RecognitionRecords)
        {
            if (copy.TryGet(recognition.ClaimId, out PoliticalClaimRecord claim) == false
                || institutionStore.TryGet(recognition.InstitutionId, out _) == false
                || recognition.RecognitionAbsoluteDay > currentDay
                || recognition.RecognitionAbsoluteDay < claim.CreatedAbsoluteDay)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalClaimStore contains recognition inconsistent with world truth or time.",
                    nameof(source));
            }

            if (PoliticalClaimOwnerBridge.TryRegisterRecognitionForRuntimeClone(
                    copy,
                    recognition,
                    out PoliticalClaimFailure recognitionFailure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalClaimStore contains invalid recognition: " + recognitionFailure + ".",
                    nameof(source));
            }
        }

        if (copy.Count != source.Count
            || copy.RecognitionRecords.Count != source.RecognitionRecords.Count)
        {
            throw new ArgumentException(
                "The SimulationRuntime PoliticalClaimStore was not copied completely.",
                nameof(source));
        }

        return isRestoredContinuation
            ? PoliticalClaimOwnerBridge.CloneForRestoredContinuation(source)
            : copy;
    }

    private static FactionStore CloneFactionStore(
        FactionStore source,
        PersonStore personStore,
        long currentDay)
    {
        FactionStore copy = new FactionStore(personStore);
        if (source == null)
        {
            return copy;
        }

        foreach (FactionRecord faction in source.Factions)
        {
            if (faction == null || faction.CreatedAbsoluteDay > currentDay)
            {
                throw new ArgumentException(
                    "The SimulationRuntime FactionStore contains a faction inconsistent with world time.",
                    nameof(source));
            }

            if (copy.TryRegister(faction, out FactionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime FactionStore contains an invalid faction: " + failure + ".",
                    nameof(source));
            }
        }

        foreach (FactionAffiliationRecord affiliation in source.Affiliations)
        {
            if (affiliation == null
                || source.TryGet(affiliation.FactionId, out FactionRecord faction) == false
                || personStore.TryGet(affiliation.PersonId, out _) == false
                || faction.CreatedAbsoluteDay > affiliation.JoinedAbsoluteDay
                || affiliation.JoinedAbsoluteDay > currentDay
                || (affiliation.EndedAbsoluteDay.HasValue && affiliation.EndedAbsoluteDay.Value > currentDay))
            {
                throw new ArgumentException(
                    "The SimulationRuntime FactionStore contains an affiliation inconsistent with world truth or time.",
                    nameof(source));
            }

            if (copy.TryRegisterAffiliation(affiliation, out FactionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime FactionStore contains an invalid affiliation: " + failure + ".",
                    nameof(source));
            }
        }

        if (copy.Count != source.Count || copy.AffiliationCount != source.AffiliationCount)
        {
            throw new ArgumentException(
                "The SimulationRuntime FactionStore was not copied completely.",
                nameof(source));
        }

        return source.Clone(personStore);
    }

    private static PoliticalSupportStore ClonePoliticalSupportStore(
        PoliticalSupportStore source,
        PersonStore personStore,
        FactionStore factionStore,
        PoliticalClaimStore politicalClaimStore,
        long currentDay)
    {
        PoliticalSupportStore empty = new PoliticalSupportStore(
            personStore,
            factionStore,
            politicalClaimStore);
        if (source == null)
        {
            return empty;
        }

        PoliticalSupportStore validation = new PoliticalSupportStore(
            personStore,
            factionStore,
            politicalClaimStore);
        foreach (PoliticalSupportRelationRecord record in source.Records)
        {
            if (record == null
                || record.StartedAbsoluteDay > currentDay
                || (record.EndedAbsoluteDay.HasValue && record.EndedAbsoluteDay.Value > currentDay)
                || validation.TryRegister(record, out PoliticalSupportFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalSupportStore contains an invalid relation or future history.",
                    nameof(source));
            }
        }

        return source.Clone(personStore, factionStore, politicalClaimStore);
    }

    private static PoliticalKnowledgeStore ClonePoliticalKnowledgeStore(
        PoliticalKnowledgeStore source,
        PersonStore personStore,
        InstitutionStore institutionStore,
        long currentDay,
        PoliticalClaimStore politicalClaimStore,
        FactionStore factionStore,
        OfficeStore officeStore,
        PropertyOwnershipStore propertyOwnershipStore)
    {
        if (source == null)
        {
            return new PoliticalKnowledgeStore(
                personStore,
                institutionStore,
                politicalClaimStore,
                factionStore,
                officeStore,
                propertyOwnershipStore);
        }

        return source.Clone(
            personStore,
            institutionStore,
            currentDay,
            politicalClaimStore,
            factionStore,
            officeStore,
            propertyOwnershipStore);
    }

    private static PoliticalDecisionStore ClonePoliticalDecisionStore(
        PoliticalDecisionStore source,
        PoliticalKnowledgeStore knowledgeStore,
        long currentDay,
        PersonStore personStore,
        InstitutionStore institutionStore,
        OfficeStore officeStore,
        PoliticalClaimStore politicalClaimStore,
        long currentWorldRevision,
        long currentKnowledgeRevision)
    {
        PoliticalDecisionStore copy = new PoliticalDecisionStore();
        if (source == null)
        {
            if (copy.TryBindToPersonStore(personStore) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalDecisionStore could not bind to the resolved PersonStore.",
                    nameof(personStore));
            }

            return copy;
        }

        if (source.IsCompatibleWithPersonStore(personStore) == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime PoliticalDecisionStore belongs to a different PersonStore/world.",
                nameof(source));
        }

        if (copy.TryBindToPersonStore(personStore) == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime PoliticalDecisionStore could not bind to the resolved PersonStore.",
                nameof(personStore));
        }

        foreach (PoliticalDecisionRecord record in source.Records)
        {
            if (record == null
                || record.IsCompatibleWithPersonStore(personStore) == false
                || record.DecisionAbsoluteDay > currentDay
                || record.ExpectedWorldRevision > currentWorldRevision
                || record.ExpectedKnowledgeRevision > currentKnowledgeRevision
                || record.Decider == null
                || knowledgeStore.TryGet(record.Decider, out _) == false
                || HasUnregisteredPoliticalDecisionReference(
                    record,
                    personStore,
                    institutionStore,
                    officeStore,
                    politicalClaimStore)
                || copy.TryRegister(record.Clone(), out PoliticalDecisionFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PoliticalDecisionStore contains an invalid decision, future history, or unregistered decider.",
                    nameof(source));
            }
        }

        if (copy.Revision != source.Revision)
        {
            throw new ArgumentException(
                "The SimulationRuntime PoliticalDecisionStore revision is inconsistent with its state.",
                nameof(source));
        }

        return copy;
    }

    private static bool HasUnregisteredPoliticalDecisionReference(
        PoliticalDecisionRecord record,
        PersonStore personStore,
        InstitutionStore institutionStore,
        OfficeStore officeStore,
        PoliticalClaimStore politicalClaimStore)
    {
        if (record == null || record.Outcome == null || record.CandidatePersonIds == null)
        {
            return true;
        }

        foreach (PersonId candidate in record.CandidatePersonIds)
        {
            if (candidate == null || personStore.TryGet(candidate, out _) == false)
            {
                return true;
            }
        }

        if ((record.DecisionKind == PoliticalDecisionKind.SuccessionSelection
                || record.DecisionKind == PoliticalDecisionKind.OfficeSelection)
            && (record.OfficeId == null || officeStore.TryGet(record.OfficeId, out _) == false))
        {
            return true;
        }

        if (record.DecisionKind == PoliticalDecisionKind.ClaimRecognitionProposal
            && (record.RecognizingInstitutionId == null
                || institutionStore.TryGet(record.RecognizingInstitutionId, out _) == false))
        {
            return true;
        }

        return record.Outcome.ReferencedClaimId != null
            && politicalClaimStore.TryGet(record.Outcome.ReferencedClaimId, out _) == false;
    }

    private void AdvancePoliticalWorldRevision()
    {
        if (politicalWorldRevision < long.MaxValue)
        {
            politicalWorldRevision++;
        }

        lastPoliticalTruthFingerprint = ComputePoliticalTruthFingerprint();
        hasPoliticalTruthFingerprint = true;
    }

    private void RefreshPoliticalWorldRevisionFromExposedStores()
    {
        long currentFingerprint = ComputePoliticalTruthFingerprint();
        if (hasPoliticalTruthFingerprint == false)
        {
            lastPoliticalTruthFingerprint = currentFingerprint;
            hasPoliticalTruthFingerprint = true;
            return;
        }

        if (currentFingerprint == lastPoliticalTruthFingerprint)
        {
            return;
        }

        if (politicalWorldRevision < long.MaxValue)
        {
            politicalWorldRevision++;
        }

        lastPoliticalTruthFingerprint = currentFingerprint;
    }

    private long ComputePoliticalTruthFingerprint()
    {
        unchecked
        {
            long fingerprint = 17L;
            List<PersonRuntime> people = new List<PersonRuntime>(personStore.Persons);
            people.Sort((left, right) => StringComparer.Ordinal.Compare(
                left?.PersonId?.Value,
                right?.PersonId?.Value));
            foreach (PersonRuntime person in people)
            {
                AppendStableString(ref fingerprint, person?.PersonId?.Value);
                fingerprint = fingerprint * 31L
                    + (person != null && person.DeathAbsoluteDay.HasValue
                        ? person.DeathAbsoluteDay.Value
                        : -1L);
            }

            foreach (ParentageRecord parentage in genealogyStore.Records)
            {
                AppendStableString(ref fingerprint, parentage?.ParentId?.Value);
                AppendStableString(ref fingerprint, parentage?.ChildId?.Value);
            }

            fingerprint = fingerprint * 31L + propertyOwnershipStore.Revision;
            fingerprint = fingerprint * 31L + estateStore.Revision;
            return fingerprint & long.MaxValue;
        }
    }

    private static void AppendStableString(ref long hash, string value)
    {
        unchecked
        {
            hash = hash * 31L + (value == null ? 0L : value.Length);
            if (value != null)
            {
                foreach (char character in value)
                {
                    hash = hash * 31L + character;
                }
            }
        }

    }

    private static InstitutionStore ResolveInstitutionStore(
        InstitutionStore institutionStore,
        OfficeStore officeStore,
        CrimeSocialAppraisalWorldState p12CrimeSocialAppraisalWorldState = null)
    {
        if (institutionStore != null
            && officeStore != null
            && !InstitutionOfficeOwnerBridge.BelongsTo(officeStore, institutionStore))
        {
            throw new ArgumentException(
                "The SimulationRuntime InstitutionStore and OfficeStore must belong to the same store pair.",
                nameof(officeStore));
        }

        InstitutionStore source = institutionStore
            ?? (officeStore == null ? null : InstitutionOfficeOwnerBridge.GetInstitutionOwner(officeStore));
        if (p12CrimeSocialAppraisalWorldState != null)
        {
            if (!ReferenceEquals(source, p12CrimeSocialAppraisalWorldState.InstitutionStore))
            {
                throw new ArgumentException(
                    "The supplied P12 Crime/Social state must share the selected InstitutionStore owner.",
                    nameof(p12CrimeSocialAppraisalWorldState));
            }

            return source;
        }

        return CloneInstitutionStore(source ?? new InstitutionStore());
    }

    private static InstitutionStore CloneInstitutionStore(InstitutionStore source)
    {
        InstitutionStore copy = new InstitutionStore();
        foreach (InstitutionRecord record in source.Institutions)
        {
            InstitutionRecord clone = new InstitutionRecord(
                new InstitutionId(record.Id.Value),
                record.DisplayName);
            if (copy.TryRegister(clone, out InstitutionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime InstitutionStore contains an invalid institution record: "
                    + failure + ".",
                    nameof(source));
            }
        }

        return copy;
    }

    private static OfficeStore CloneOfficeStore(
        OfficeStore source,
        InstitutionStore institutionStore,
        PersonStore personStore,
        bool isRestoredContinuation)
    {
        OfficeStore copy = new OfficeStore(institutionStore);
        if (source == null)
        {
            return copy;
        }

        foreach (OfficeRecord record in source.Offices)
        {
            OfficeRecord clone = new OfficeRecord(
                new OfficeId(record.Id.Value),
                new InstitutionId(record.InstitutionId.Value),
                record.DisplayName);
            if (copy.TryRegister(clone, out InstitutionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime OfficeStore contains an invalid office record: "
                    + failure + ".",
                    nameof(source));
            }
        }

        foreach (OfficeIncumbency incumbency in source.Incumbencies)
        {
            if (incumbency == null
                || personStore.TryGet(incumbency.Incumbent, out _) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime OfficeStore contains an incumbent absent from PersonStore.",
                    nameof(source));
            }

            if (copy.TryAssignIncumbent(
                    new OfficeId(incumbency.OfficeId.Value),
                    new PersonId(incumbency.Incumbent.Value),
                    incumbency.StartAbsoluteDay,
                    out InstitutionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime OfficeStore contains an invalid incumbency: "
                    + failure + ".",
                    nameof(source));
            }
        }

        foreach (OfficeTenureRecord tenure in InstitutionOfficeOwnerBridge.GetTenureHistoryInMutationOrder(source))
        {
            if (tenure == null || tenure.IsOpen)
            {
                continue;
            }

            OfficeTenureRecord clone = new OfficeTenureRecord(
                new OfficeId(tenure.OfficeId.Value),
                new PersonId(tenure.Incumbent.Value),
                tenure.StartAbsoluteDay,
                tenure.EndAbsoluteDay,
                tenure.EndReason);
            if (InstitutionOfficeOwnerBridge.TryAddHistoricalTenureForRuntimeOfficeClone(
                    copy,
                    clone,
                    out InstitutionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime OfficeStore contains invalid historical tenure: "
                    + failure + ".",
                    nameof(source));
            }
        }

        if (isRestoredContinuation)
        {
            if (!InstitutionOfficeOwnerBridge.TryCloneForRestoredContinuation(
                source,
                institutionStore,
                out OfficeStore restoredClone))
            {
                throw new ArgumentException(
                    "The restored SimulationRuntime OfficeStore could not preserve its validated source revision and tenure order.",
                    nameof(source));
            }

            return restoredClone;
        }

        return copy;
    }

    private static PropertyOwnershipStore ClonePropertyOwnershipStore(
        PropertyOwnershipStore source,
        PersonStore personStore,
        long currentDay)
    {
        if (source != null
            && source.PersonStoreForWorldBoundary != null
            && ReferenceEquals(source.PersonStoreForWorldBoundary, personStore) == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime PropertyOwnershipStore must belong to the resolved PersonStore.",
                nameof(source));
        }

        PropertyOwnershipStore copy = new PropertyOwnershipStore(personStore);
        if (source == null)
        {
            return copy;
        }

        foreach (PropertyOwnershipRecord record in source.Records)
        {
            if (record == null
                || record.OwnerPersonId == null
                || personStore.TryGet(record.OwnerPersonId, out _) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PropertyOwnershipStore contains an owner absent from PersonStore.",
                    nameof(source));
            }

            PropertyOwnershipRecord clone = new PropertyOwnershipRecord(
                new PropertyId(record.PropertyId.Value),
                new PersonId(record.OwnerPersonId.Value));
            if (copy.TryRegister(clone, out PropertyFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PropertyOwnershipStore contains an invalid record: "
                    + failure + ".",
                    nameof(source));
            }
        }

        foreach (PropertyOwnershipTransferHistoryRecord history in source.TransferHistory)
        {
            if (history == null
                || history.PropertyId == null
                || history.PreviousOwnerPersonId == null
                || history.NewOwnerPersonId == null)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PropertyOwnershipStore contains invalid transfer history.",
                    nameof(source));
            }

            if (history.TransferAbsoluteDay > currentDay
                || source.TryGet(history.PropertyId, out _) == false
                || personStore.TryGet(history.PreviousOwnerPersonId, out _) == false
                || personStore.TryGet(history.NewOwnerPersonId, out _) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PropertyOwnershipStore contains transfer history inconsistent with the world.",
                    nameof(source));
            }

            PropertyOwnershipTransferHistoryRecord clone =
                new PropertyOwnershipTransferHistoryRecord(
                    new PropertyId(history.PropertyId.Value),
                    new PersonId(history.PreviousOwnerPersonId.Value),
                    new PersonId(history.NewOwnerPersonId.Value),
                    history.TransferAbsoluteDay);
            if (copy.TryAddHistoricalTransfer(
                    clone,
                    out PropertyTransferFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime PropertyOwnershipStore contains invalid transfer history: "
                    + failure + ".",
                    nameof(source));
            }
        }

        if (copy.Revision != source.Revision)
        {
            throw new ArgumentException(
                "The SimulationRuntime PropertyOwnershipStore revision is inconsistent with its state.",
                nameof(source));
        }

        return copy;
    }

    private static EstateStore CloneEstateStore(
        EstateStore source,
        PersonStore personStore,
        long currentDay)
    {
        if (source != null
            && ReferenceEquals(source.PersonStoreForWorldBoundary, personStore) == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime EstateStore must belong to the resolved PersonStore.",
                nameof(source));
        }

        EstateStore copy = new EstateStore(personStore);
        if (source == null)
        {
            return copy;
        }

        foreach (EstateRecord record in source.Records)
        {
            if (record == null
                || personStore.TryGet(record.DeceasedPersonId, out PersonRuntime deceased) == false
                || deceased.DeathAbsoluteDay.HasValue == false
                || record.OpenedAbsoluteDay < deceased.DeathAbsoluteDay.Value
                || record.OpenedAbsoluteDay > currentDay)
            {
                throw new ArgumentException(
                    "The SimulationRuntime EstateStore contains an estate inconsistent with PersonStore or world time.",
                    nameof(source));
            }

            EstateRecord clone = new EstateRecord(
                new EstateId(record.EstateId.Value),
                new PersonId(record.DeceasedPersonId.Value),
                record.OpenedAbsoluteDay);
            if (copy.TryRegister(clone, out EstateFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime EstateStore contains an invalid record: "
                    + failure + ".",
                    nameof(source));
            }
        }

        return copy;
    }

    public void AdvanceDays(int dayCount)
    {
        if (dayCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dayCount), dayCount, "dayCount cannot be negative.");
        }

        if (dayCount == 0)
        {
            return;
        }

        if (TryAdvanceDays(
                dayCount,
                out _,
                out SimulationRuntimeAdvanceFailure failure) == false)
        {
            throw CreateAdvanceFailureException(failure);
        }
    }

    public bool TryAdvanceDays(
        int dayCount,
        out int daysAdvanced,
        out SimulationRuntimeAdvanceFailure failure)
    {
        if (TryGetBoundDailyV1ApplicationAdvanceAuthority(
                out DailyV1Application application,
                out DailyV1ActiveRuntimeSession session))
            return application.TryAdvanceDays(session, dayCount, out daysAdvanced, out failure);

        return dailyExecutionBoundary.TryAdvanceDays(this, dayCount, out daysAdvanced, out failure);
    }

    private void BeginSimulationDay()
    {
        DailyV1SelectedRuntimeOwnerGraph selectedGraph = SelectedRuntimeOwnerGraph;
        if (selectedGraph != null && TryAdvanceSelectedCrimeJusticeDailyStage(selectedGraph))
        {
            AdvanceMerchantPlanUrgency();
            return;
        }

        if (justiceSystem != null)
        {
            RunP12CrimeJusticeDailyMutation(justiceSystem.BeginDay);
        }

        // Hidden-state expiration is a previously established consequence/timer,
        // not autonomous crime origination. It must continue to advance whenever
        // the composed crime system owns the state, independently of policy that
        // filters new criminal decisions.
        if (crimeSystem != null)
            RunP12CrimeJusticeDailyMutation(() => crimeSystem.AdvanceHiddenStatuses(npcRuntimes));

        if (justiceSystem != null)
        {
            RunP12CrimeJusticeDailyMutation(() => justiceSystem.AdvanceSentences(npcRuntimes));
        }

        if (justiceSystem != null)
        {
            RunP12CrimeJusticeDailyMutation(() => justiceSystem.SyncWantedStatuses(npcRuntimes));
        }

        AdvanceMerchantPlanUrgency();
    }

    private bool TryAdvanceSelectedCrimeJusticeDailyStage(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph == null) return false;
        if (justiceSystem != null
            && !ReferenceEquals(graph.JusticeRecordsOwner, justiceSystem.CoreOwner))
            throw new InvalidOperationException(
                "Selected Crime/Justice daily execution requires the exact Justice owner composed by the runtime.");
        List<DailyV1ActorTurnRosterEntry> roster = new List<DailyV1ActorTurnRosterEntry>(npcRuntimes.Count);
        Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> runtimeByOwner =
            new Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime>();
        List<DailyV1NpcRuntimeOwners> owners = new List<DailyV1NpcRuntimeOwners>();
        for (int rosterOrdinal = 0; rosterOrdinal < npcRuntimes.Count; rosterOrdinal++)
        {
            NpcRuntime runtime = npcRuntimes[rosterOrdinal];
            if (runtime == null)
            {
                roster.Add(new DailyV1ActorTurnRosterEntry(rosterOrdinal, null, null));
                continue;
            }

            // Until the P12 dynamic/unbound owner closure is part of the shared
            // runtime composition, any unselected member keeps the complete
            // roster on its existing Unity lifecycle. Core owns this stage only
            // when every non-null runtime has an exact selected graph owner.
            if (!runtime.UsesSelectedDailyV1Definitions) return false;

            DailyV1NpcRuntimeOwners owner = runtime.SelectedRuntimeOwners;
            if (owner == null || !graph.ContainsExactNpcOwner(owner)
                || !ReferenceEquals(graph.Definitions, owner.Definitions)
                || !string.Equals(runtime.RuntimeId, owner.RuntimeId, StringComparison.Ordinal)
                || !runtimeByOwner.TryAdd(owner, runtime))
                throw new InvalidOperationException(
                    "Selected Crime/Justice daily execution requires each exact Core NPC owner instance.");

            owners.Add(owner);
            roster.Add(new DailyV1ActorTurnRosterEntry(rosterOrdinal, runtime.PersonId, owner));
        }

        if (!graph.MatchesExactOwnerInstances(graph.Cities, owners))
            throw new InvalidOperationException(
                "Selected Crime/Justice daily execution received an incomplete Core owner roster.");
        SelectedDailyV1ActorTurnExecutionBoundary boundary =
            new SelectedDailyV1ActorTurnExecutionBoundary(this, graph, runtimeByOwner);
        if (!DailyV1SelectedCrimeJusticeDailyStage.TryRun(
                graph, roster, crimeSystem != null, justiceSystem != null, boundary))
            throw new InvalidOperationException(
                "Selected Crime/Justice daily execution rejected its exact Core owner graph or admitted Justice links.");
        return true;
    }

    private void AdvanceMerchantPlanUrgency()
    {
        if (configuration.MerchantTrade.Enabled == false)
        {
            return;
        }

        DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
        if (graph != null)
        {
            List<DailyV1NpcRuntimeOwners> owners = new List<DailyV1NpcRuntimeOwners>();
            Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> runtimeByOwner =
                new Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime>();
            foreach (NpcRuntime runtime in npcRuntimes)
            {
                if (runtime == null || !runtime.UsesSelectedDailyV1Definitions) continue;
                DailyV1NpcRuntimeOwners owner = runtime.SelectedRuntimeOwners;
                if (owner == null
                    || !ReferenceEquals(runtime.SelectedRuntimeOwnerGraph, graph)
                    || !graph.ContainsExactNpcOwner(owner)
                    || !ReferenceEquals(owner.Definitions, graph.Definitions)
                    || !string.Equals(runtime.RuntimeId, owner.RuntimeId, StringComparison.Ordinal)
                    || !runtimeByOwner.TryAdd(owner, runtime))
                    throw new InvalidOperationException(
                        "Selected Merchant plan urgency requires exact Core NPC owners.");
                owners.Add(owner);
            }

            if (!graph.MatchesExactOwnerInstances(graph.Cities, owners))
                throw new InvalidOperationException(
                    "Selected Merchant plan urgency received an incomplete NPC owner roster.");

            SelectedDailyV1ActorTurnExecutionBoundary boundary =
                new SelectedDailyV1ActorTurnExecutionBoundary(this, graph, runtimeByOwner);
            if (!DailyV1SelectedMerchantTradeStateOperation.TryAdvancePendingTravelDays(
                    graph, owners, boundary))
                throw new InvalidOperationException(
                    "The selected Core Merchant plan urgency stage could not use its exact owner graph.");
            return;
        }

        foreach (NpcRuntime npcRuntime in npcRuntimes)
        {
            if (npcRuntime == null
                || npcRuntime.IsAlive == false
                || npcRuntime.IsTraveling == true
                || npcRuntime.CurrentCity == null)
            {
                continue;
            }

            MerchantTradePlanRuntime tradePlan = npcRuntime.MerchantTradePlan;

            if (tradePlan.IsActive == true
                && tradePlan.TargetCity != null
                && tradePlan.TargetCity != npcRuntime.CurrentCity)
            {
                tradePlan.IncrementPendingTravelDay();
            }
        }
    }

    private void SimulateEconomyDay()
    {
        if (SelectedRuntimeOwnerGraph != null)
        {
            if (!TrySimulateSelectedDailyEconomyDay())
                throw new InvalidOperationException(
                    "The selected Core economy stage could not admit its exact runtime owner graph.");
            return;
        }

        foreach (CityRuntime cityRuntime in cities)
        {
            if (cityRuntime != null && cityRuntime.HasLocalDailyMaterialFlow)
                cityRuntime.SimulateLocalDailyMaterialFlow(CurrentDay, calendar.SemanticVersion);
        }

        foreach (CityRuntime cityRuntime in cities)
        {
            if (cityRuntime != null && !cityRuntime.HasLocalDailyMaterialFlow)
            {
                cityRuntime.SimulateProductionDay();
            }
        }

        foreach (CityRuntime cityRuntime in cities)
        {
            if (cityRuntime == null)
            {
                continue;
            }

            if (!cityRuntime.HasLocalDailyMaterialFlow)
                cityRuntime.SimulateConsumptionDay();
            cityRuntime.UpdateMarketPrices();
        }
    }

    private bool TrySimulateSelectedDailyEconomyDay()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
        if (!CanRunSelectedEconomy(graph)) return false;

        return graph.TrySimulateDailyEconomyDay(
            graph.Cities,
            CurrentDay,
            calendar?.SemanticVersion,
            (owner, category, message) => LogSelectedDailyEconomy(owner, category, message));
    }

    private void EnsureSelectedEconomyCanRun(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (!CanRunSelectedEconomy(graph))
            throw new InvalidOperationException(
                "The selected Core economy stage could not admit its exact runtime owner graph.");
    }

    private bool CanRunSelectedEconomy(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph == null || !ReferenceEquals(graph, SelectedRuntimeOwnerGraph)
            || cities == null || cities.Count != graph.Cities.Count
            || !HasExactSelectedRuntimeOwnerGraph(graph.Definitions))
            return false;
        if (calendar == null && graph.Cities.Any(city => city.HasLocalDailyMaterialFlow))
            return false;

        for (int index = 0; index < cities.Count; index++)
        {
            CityRuntime city = cities[index];
            if (city == null || !ReferenceEquals(city.SelectedRuntimeOwners, graph.Cities[index]))
                return false;
        }

        return true;
    }

    private void LogSelectedDailyEconomy(
        DailyV1CityRuntimeOwners owner,
        SimulationLogCategory category,
        string message)
    {
        if (owner == null || !SelectedRuntimeOwnerGraph.ContainsExactCityOwner(owner))
            throw new InvalidOperationException("A selected City economy log referenced an owner outside the runtime graph.");
        logger?.Log(category, message);
    }

    private void RefreshLocalKnowledgeAndShare()
    {
        if (TryRefreshSelectedLocalKnowledgeAndShare()) return;

        foreach (NpcRuntime npcRuntime in npcRuntimes)
        {
            if (npcRuntime == null || npcRuntime.IsAlive == false || npcRuntime.CurrentLocation == null || npcRuntime.IsTraveling == true)
            {
                continue;
            }

            npcRuntime.SpatialKnowledge.DiscoverLocation(npcRuntime.CurrentLocation.RuntimeId);

            if (npcRuntime.CurrentCity != null)
            {
                if (configuration.MerchantTrade.Enabled)
                {
                    merchantSystem?.ObserveCurrentMarket(npcRuntime);
                }
            }
        }

        if (configuration.MerchantTrade.Enabled)
        {
            commercialKnowledgeSharingSystem?.ShareAmongPresentMerchants(npcRuntimes);
        }
    }

    private bool TryRefreshSelectedLocalKnowledgeAndShare()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
        if (graph == null) return false;

        EnsureSelectedLocalKnowledgeCanRun(graph);
        if (!DailyV1SelectedLocalKnowledgeRefreshStage.TryRun(graph, CurrentDay))
            throw new InvalidOperationException(
                "The selected Core local-knowledge stage could not complete against its exact runtime owner graph.");

        return true;
    }

    private void EnsureSelectedLocalKnowledgeCanRun(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph == null || !ReferenceEquals(graph, SelectedRuntimeOwnerGraph))
            throw new InvalidOperationException(
                "The selected local-knowledge stage requires the current Core owner graph.");

        List<DailyV1NpcRuntimeOwners> owners = new List<DailyV1NpcRuntimeOwners>();
        foreach (NpcRuntime runtime in npcRuntimes)
        {
            if (runtime == null || !runtime.UsesSelectedDailyV1Definitions) continue;
            DailyV1NpcRuntimeOwners owner = runtime.SelectedRuntimeOwners;
            if (owner == null
                || !ReferenceEquals(runtime.SelectedRuntimeOwnerGraph, graph)
                || !graph.ContainsExactNpcOwner(owner)
                || !string.Equals(runtime.RuntimeId, owner.RuntimeId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The selected local-knowledge stage requires the exact runtime NPC owner roster.");
            owners.Add(owner);
        }

        if (!graph.MatchesExactOwnerInstances(graph.Cities, owners))
            throw new InvalidOperationException(
                "The selected local-knowledge stage received an incomplete runtime NPC owner roster.");

        bool merchantTradeEnabled = graph.Definitions.EffectiveConfiguration?.MerchantTrade?.Enabled == true;
        if (merchantTradeEnabled && merchantSystem != null)
            merchantSystem.EnsureCanAdvanceSelectedTradeState();
        if (merchantTradeEnabled && commercialKnowledgeSharingSystem != null)
            commercialKnowledgeSharingSystem.EnsureCanShare();
    }

    private bool IsDeadNpc(string runtimeId)
    {
        foreach (NpcRuntime npcRuntime in npcRuntimes)
        {
            if (npcRuntime != null && string.Equals(npcRuntime.RuntimeId, runtimeId, StringComparison.Ordinal) == true)
            {
                return npcRuntime.IsDead;
            }
        }

        return false;
    }

    private void ObserveArrivedExplorableSites(NpcRuntime npcRuntime)
    {
        if (npcRuntime?.CurrentLocation == null
            || explorableSiteStore == null
            || explorableSiteKnowledgeSystem == null)
        {
            return;
        }

        foreach (ExplorableSiteRuntime siteRuntime in explorableSiteStore.GetForLocation(npcRuntime.CurrentLocation))
        {
            explorableSiteKnowledgeSystem.RecordDirectObservation(
                npcRuntime,
                siteRuntime,
                CurrentDay);
        }
    }

    private void EvaluateStatus(NpcRuntime npcRuntime)
    {
    }

    private void EvaluateAction(NpcRuntime npcRuntime)
    {
        if (npcDecisionSystem == null)
        {
            return;
        }

        NpcActionRuntime chosenAction = npcRuntime != null && npcRuntime.UsesSelectedDailyV1Definitions
            ? npcDecisionSystem.ChooseSelectedDailyV1Action(
                npcRuntime,
                GetSelectedConfiguredActionsEnabledForRuntime(npcRuntime),
                CurrentDay,
                personStore)
            : npcDecisionSystem.ChooseAction(
                npcRuntime,
                GetConfiguredActionsEnabledForRuntime(npcRuntime),
                CurrentDay);
        RecordAndInstallChosenAction(npcRuntime, chosenAction, NpcDecisionOrigin.Autonomous);
    }

    private NpcDecisionRecord RecordAndInstallChosenAction(
        NpcRuntime npcRuntime,
        NpcActionRuntime actionRuntime,
        NpcDecisionOrigin origin)
    {
        if (decisionRecorder != null
            && decisionRecorder.TryRecordAndInstallSelectedAction(
                npcRuntime, actionRuntime, origin,
                out NpcDecisionRecord selectedDecision, out bool handled)
            && handled)
            return selectedDecision;

        NpcDecisionRecord decision = decisionRecorder?.RecordChosenAction(
            npcRuntime, actionRuntime, origin);
        npcRuntime?.SetCurrentActionRuntime(actionRuntime);
        return decision;
    }

    private NpcActionResult TryExecuteCurrentAction(NpcRuntime npcRuntime)
    {
        NpcActionRuntime actionRuntime = npcRuntime.CurrentActionRuntime;
        NpcActionData action = actionRuntime != null ? actionRuntime.Action : npcRuntime.CurrentAction;

        if (action == null)
        {
            return null;
        }

        if (npcRuntime.UsesSelectedDailyV1Definitions
            && npcRuntime.SelectedRuntimeOwners != null)
            return TryExecuteSelectedCurrentAction(npcRuntime);

        LogChosenTargetAction(npcRuntime, actionRuntime);
        NpcActionResult actionResult = TryExecuteAction(npcRuntime, actionRuntime, action);

        if (actionResult != null && string.IsNullOrEmpty(actionResult.Message) == false)
        {
            logger?.Log(SimulationLogCategory.NpcAction, actionResult.Message);
        }

        if (npcRuntime.UsesSelectedDailyV1Definitions)
        {
            ApplySuccessStatusChanges(
                npcRuntime, actionRuntime, action,
                actionResult?.Success == true);
        }
        else if (actionResult != null && actionResult.Success == true)
        {
            IReadOnlyList<NpcRuntime> participants = actionRuntime?.TargetNpc == null
                ? new[] { npcRuntime }
                : new[] { npcRuntime, actionRuntime.TargetNpc };
            if (!TryBeginP12CrimeJusticeActionScope(
                    participants,
                    includeJustice: false,
                    out P12CrimeJusticeMutationScope statusScope))
                throw new InvalidOperationException("P12 action status effects could not be admitted.");
            using (statusScope)
                ApplySuccessStatusChanges(npcRuntime, actionRuntime, action, actionSucceeded: true);
        }

        return actionResult;
    }

    private NpcActionResult TryExecuteSelectedCurrentAction(NpcRuntime npcRuntime)
    {
        if (npcRuntime == null
            || !npcRuntime.UsesSelectedDailyV1Definitions
            || npcRuntime.SelectedRuntimeOwners == null
            || !TryCreateSelectedActorActionExecutionBoundary(
                npcRuntime, out DailyV1SelectedRuntimeOwnerGraph graph,
                out SelectedDailyV1ActorTurnExecutionBoundary boundary))
            throw new InvalidOperationException(
                "The selected Daily-v1 action has no exact Core runtime owner boundary.");

        if (!DailyV1SelectedActorActionExecutionStage.TryExecuteInstalledAction(
                graph, npcRuntime.SelectedRuntimeOwners, randomSource, CurrentDay,
                boundary, out _, out NpcActionResultType? resultType, out _))
            throw new InvalidOperationException(
                "The selected Daily-v1 action could not execute through its exact Core owner graph.");

        if (!resultType.HasValue) return null;
        return resultType.Value == NpcActionResultType.Success
            ? NpcActionResult.Succeeded()
            : NpcActionResult.Failed();
    }

    private bool TryCreateSelectedActorActionExecutionBoundary(
        NpcRuntime actorRuntime,
        out DailyV1SelectedRuntimeOwnerGraph graph,
        out SelectedDailyV1ActorTurnExecutionBoundary boundary)
    {
        graph = SelectedRuntimeOwnerGraph;
        boundary = null;
        if (graph == null || actorRuntime?.SelectedRuntimeOwners == null
            || !ReferenceEquals(actorRuntime.SelectedRuntimeOwnerGraph, graph)
            || !graph.ContainsExactNpcOwner(actorRuntime.SelectedRuntimeOwners))
            return false;

        Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime> runtimeByOwner =
            new Dictionary<DailyV1NpcRuntimeOwners, NpcRuntime>();
        foreach (NpcRuntime runtime in npcRuntimes)
        {
            DailyV1NpcRuntimeOwners owner = runtime?.SelectedRuntimeOwners;
            if (owner == null) continue;
            if (!graph.ContainsExactNpcOwner(owner)
                || !ReferenceEquals(runtime.SelectedRuntimeOwnerGraph, graph)
                || !runtimeByOwner.TryAdd(owner, runtime))
                return false;
        }

        if (!runtimeByOwner.TryGetValue(actorRuntime.SelectedRuntimeOwners,
                out NpcRuntime exactActorRuntime)
            || !ReferenceEquals(exactActorRuntime, actorRuntime))
            return false;

        boundary = new SelectedDailyV1ActorTurnExecutionBoundary(
            this, graph, runtimeByOwner);
        return true;
    }

    private bool TryProcessScheduledDirective(NpcRuntime npcRuntime)
    {
        if (scheduledDirectiveSystem == null
            || scheduledDirectiveSystem.TryTakeDirective(npcRuntime, out ScheduledDirective directive) == false)
        {
            return false;
        }

        ProcessScheduledDirective(npcRuntime, directive);
        return true;
    }

    private void ProcessScheduledDirective(NpcRuntime npcRuntime, ScheduledDirective directive)
    {
        npcRuntime.SetCurrentActionRuntime(null);

        if (directive.Mode == ScheduledDirectiveMode.RequestAction)
        {
            ProcessRequestedActionDirective(npcRuntime, directive);
        }
        else if (directive.Mode == ScheduledDirectiveMode.ForceOutcome)
        {
            ProcessForcedOutcomeDirective(npcRuntime, directive);
        }
        else
        {
            SkipDirective(directive, "Directive mode is not supported.");
        }
    }

    private void ProcessRequestedActionDirective(NpcRuntime npcRuntime, ScheduledDirective directive)
    {
        if (!TryResolveScheduledDirectiveAction(npcRuntime, directive, out NpcActionData action))
        {
            SkipDirective(directive, "Actor is not in a compatible state for the requested action.");
            return;
        }

        NpcActionRuntime requestedAction = npcDecisionSystem != null
            && IsActionEnabledForRuntime(npcRuntime, action)
            ? npcDecisionSystem.CreateRequestedAction(npcRuntime, action)
            : null;

        if (requestedAction == null)
        {
            SkipDirective(directive, "Actor is not in a compatible state for the requested action.");
            return;
        }

        RecordAndInstallChosenAction(
            npcRuntime, requestedAction, NpcDecisionOrigin.ScheduledDirective);
        NpcActionResult result = TryExecuteCurrentAction(npcRuntime);

        if (result != null && result.Success == true)
        {
            directive.MarkSucceeded(CurrentDay);
            return;
        }

        string reason = result != null && string.IsNullOrEmpty(result.Message) == false
            ? result.Message
            : "Requested action was attempted and failed.";
        directive.MarkFailed(CurrentDay, reason);
    }

    private void ProcessForcedOutcomeDirective(NpcRuntime npcRuntime, ScheduledDirective directive)
    {
        if (directive.Operation != ScheduledDirectiveOperation.EscapePrison || justiceSystem == null)
        {
            SkipDirective(directive, "Escape domain operation is unavailable.");
            return;
        }

        if (!TryResolveScheduledDirectiveAction(npcRuntime, directive, out NpcActionData action))
        {
            SkipDirective(directive, "Admitted Escape status effects are unavailable.");
            return;
        }

        if (justiceSystem.IsArrested(npcRuntime) == false)
        {
            SkipDirective(directive, "Actor is not arrested; escape outcome is incompatible with current state.");
            return;
        }

        if (npcRuntime.UsesSelectedDailyV1Definitions
                ? !npcRuntime.TryGetConfiguredActionStatusNodes(action, out _, out _, out _, out _)
                : !npcRuntime.TryGetConfiguredActionStatusChanges(action, out _, out _, out _, out _))
        {
            SkipDirective(directive, "Admitted Escape status effects are unavailable.");
            return;
        }
        CrimeActionSettings settings = npcRuntime.GetConfiguredCrimeActionSettings(action);

        npcRuntime.SetCurrentActionRuntime(new NpcActionRuntime(action));

        if (!TryBeginP12CrimeJusticeActionScope(
                new[] { npcRuntime },
                includeJustice: true,
                out P12CrimeJusticeMutationScope escapeScope))
            throw new InvalidOperationException("P12 forced Escape could not be admitted.");

        bool escaped;
        using (escapeScope)
        {
            escaped = justiceSystem.ApplyEscapeSuccess(npcRuntime, settings.escapeBountyPenalty);
            if (escaped)
                ApplySuccessStatusChanges(
                    npcRuntime, npcRuntime.CurrentActionRuntime, action,
                    actionSucceeded: true,
                    reuseForcedEscapeScope: true);
        }

        if (!escaped)
        {
            directive.MarkFailed(CurrentDay, "Canonical escape transition rejected the forced outcome.");
            return;
        }
        directive.MarkSucceeded(CurrentDay);
    }

    private bool TryResolveScheduledDirectiveAction(
        NpcRuntime npcRuntime,
        ScheduledDirective directive,
        out NpcActionData action)
    {
        action = null;
        if (npcRuntime == null || directive == null
            || string.IsNullOrWhiteSpace(directive.ActionDefinitionId)) return false;

        if (npcRuntime.UsesSelectedDailyV1Definitions)
        {
            return npcRuntime.TryGetSelectedDailyV1ActionByDefinitionId(
                    directive.ActionDefinitionId, out action)
                && action != null
                && action.actionType == NpcActionType.EscapePrison;
        }

        int matches = 0;
        if (configuredActions != null)
        {
            foreach (NpcActionData configuredAction in configuredActions)
            {
                if (configuredAction != null
                    && string.Equals(configuredAction.DefinitionId,
                        directive.ActionDefinitionId, StringComparison.Ordinal))
                {
                    action = configuredAction;
                    matches++;
                }
            }
        }
        return matches == 1 && action != null
            && npcRuntime.GetConfiguredActionType(action) == NpcActionType.EscapePrison;
    }

    private void SkipDirective(ScheduledDirective directive, string reason)
    {
        if (directive == null)
        {
            return;
        }

        directive.MarkSkipped(CurrentDay, reason);
        logger?.LogWarning($"Scheduled directive '{directive.DirectiveId}' was skipped: {reason}");
    }

    private SelectedSoloTravelActionBoundaryAdapter CreateSelectedSoloTravelActionBoundary(
        NpcRuntime exactActor,
        TravelActionProvider travelProvider,
        NpcActionRuntime actionRuntime)
    {
        DailyV1SelectedRuntimeOwnerGraph graph = SelectedRuntimeOwnerGraph;
        if (exactActor == null || travelProvider == null || actionRuntime == null
            || graph?.TravelParties == null
            || !ReferenceEquals(exactActor.SelectedRuntimeOwnerGraph, graph)
            || !graph.ContainsExactNpcOwner(exactActor.SelectedRuntimeOwners)
            || !ReferenceEquals(graph.TravelParties, travelProvider.SelectedTravelPartyStore)
            || actionRuntime.TargetCity?.SelectedRuntimeOwners == null
            || !graph.ContainsExactCityOwner(actionRuntime.TargetCity.SelectedRuntimeOwners)
            || actionRuntime.TargetCity.Location == null
            || !graph.TryGetCityLocationRuntimeId(
                actionRuntime.TargetCity.RuntimeId, out string targetLocationRuntimeId)
            || !string.Equals(targetLocationRuntimeId,
                actionRuntime.TargetCity.Location.RuntimeId, StringComparison.Ordinal))
            return null;

        DailyV1NpcRuntimeOwners actor = exactActor.SelectedRuntimeOwners;
        DailyV1SelectedTravelTransitionPlan preparedTransition = null;
        CityRuntime[] preparedChangedCities = null;
        NpcTravelPlanState clearedTravelPlan = new NpcTravelPlanState(
            null, null, NpcTravelReason.None, 0f, 0f, null);

        return new SelectedSoloTravelActionBoundaryAdapter(
            operation =>
            {
                if (runtimeAdmissionContext == null) return operation();
                if (!TryBeginP12SoloTravelStartOperation(
                        exactActor, travelProvider,
                        out P12SoloTravelStartOperationScope travelStartScope))
                    throw new InvalidOperationException(
                        "The P12 solo travel-start owner operation could not be admitted.");
                using (travelStartScope) return operation();
            },
            () => travelSystem != null
                && travelSystem.CanStartTravel(exactActor, actionRuntime.TargetCity, out _, out _),
            () => merchantSystem?.LogTradeRepositionDecision(
                exactActor, exactActor.CurrentCity, actionRuntime),
            (plan, route, travelCost) => travelSystem != null
                && travelSystem.CanCommitSelectedSoloTravelTransition(
                    exactActor, plan, route, travelCost, actionRuntime.TargetCity),
            plan =>
            {
                if (plan == null
                    || !ReferenceEquals(plan.Npc, actor)
                    || !string.Equals(plan.DestinationCityRuntimeId,
                        actionRuntime.TargetCity.RuntimeId, StringComparison.Ordinal)
                    || !string.Equals(plan.DestinationLocationRuntimeId,
                        actionRuntime.TargetCity.Location.RuntimeId, StringComparison.Ordinal))
                    return false;

                IReadOnlyList<CityRuntime> candidates = exactActor.CurrentCity == null
                    ? Array.Empty<CityRuntime>()
                    : new[] { exactActor.CurrentCity };
                if (!exactActor.CanPrepareSelectedTravelProjection(
                        plan, candidates, out preparedChangedCities))
                    return false;
                preparedTransition = plan;
                return true;
            },
            plan => ReferenceEquals(preparedTransition, plan)
                && exactActor.CanAdmitPreparedSelectedTravelDay(plan, preparedChangedCities),
            plan =>
            {
                if (!ReferenceEquals(preparedTransition, plan)
                    || !plan.IsCommitted
                    || !exactActor.TryProjectCommittedSelectedTravelTransition(
                        plan, preparedChangedCities,
                        null, null,
                        actionRuntime.TargetCity.Location, actionRuntime.TargetCity))
                    throw new InvalidOperationException(
                        "A committed selected solo travel transition could not publish its Unity projection.");

                if (!plan.IsNoOp
                    && (preparedChangedCities.Length != 0 || plan.TravelRevisionChanges))
                    exactActor.RecordSelectedTravelOwnerMutationCommitted(
                        plan, preparedChangedCities);
            },
            travel => travelSystem?.PublishSelectedSoloTravelStarted(
                exactActor, actionRuntime.TargetCity, travel),
            () => merchantSystem?.LogCommercialScoutingDecision(exactActor, actionRuntime),
            owner => ReferenceEquals(owner, actor)
                && exactActor.TravelPlan.CanProjectOwnerState(clearedTravelPlan),
            owner => ReferenceEquals(owner, actor)
                && exactActor.TravelPlan.CanAdmitSelectedOwnerMutation(),
            owner =>
            {
                if (!ReferenceEquals(owner, actor)
                    || !exactActor.TravelPlan.PublishCommittedOwnerState(clearedTravelPlan))
                    throw new InvalidOperationException(
                        "A committed selected TravelPlan clear could not publish its Unity projection.");
            });
    }

    private NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        if (npcRuntime == null || action == null || IsActionEnabledForRuntime(npcRuntime, action) == false)
        {
            return NpcActionResult.Failed();
        }

        NpcActionType actionType = npcRuntime.GetConfiguredActionType(action);
        if (npcRuntime.UsesSelectedDailyV1Definitions
                ? !npcRuntime.TryGetConfiguredActionStatusNodes(action, out _, out _, out _, out _)
                : !npcRuntime.TryGetConfiguredActionStatusChanges(action, out _, out _, out _, out _))
            return NpcActionResult.Failed();
        bool selectedMerchantAction = npcRuntime.UsesSelectedDailyV1Definitions
            && (actionType == NpcActionType.BuyGoods || actionType == NpcActionType.SellGoods);
        bool selectedGuardArrestAction = npcRuntime.UsesSelectedDailyV1Definitions
            && actionType == NpcActionType.Arrest;
        bool selectedCrimeStealAction = npcRuntime.UsesSelectedDailyV1Definitions
            && actionType == NpcActionType.Steal;
        bool selectedCrimeAction = npcRuntime.UsesSelectedDailyV1Definitions
            && (actionType == NpcActionType.Hide || actionType == NpcActionType.EscapePrison);
        bool exactSelectedGraphActor = npcRuntime.UsesSelectedDailyV1Definitions
            && npcRuntime.SelectedRuntimeOwners != null
            && SelectedRuntimeOwnerGraph != null
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwnerGraph, SelectedRuntimeOwnerGraph)
            && SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(npcRuntime.SelectedRuntimeOwners);
        INpcActionProvider actionProvider = actionType == NpcActionType.Normal
            ? null
            : selectedMerchantAction
                ? merchantSystem
                : npcDecisionSystem?.GetProviderForAction(npcRuntime, action);

        if (actionType != NpcActionType.Normal && actionProvider == null
            && !exactSelectedGraphActor)
        {
            return NpcActionResult.Failed();
        }

        if (runtimeAdmissionContext != null
            && travelSystem != null
            && actionType == NpcActionType.Travel
            && !(actionProvider is TravelActionProvider))
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException(
                "The selected P12 profile cannot admit a Travel action through an unbound provider.");
        }

        // Core owns the supported selected-action route. Enter it only when
        // the Unity action and every projected payload target are the exact
        // projection of the graph-owned current slot.
        bool selectedActionProjectionMatches = npcRuntime.UsesSelectedDailyV1Definitions
            && actionRuntime != null
            && ReferenceEquals(actionRuntime.Action, action)
            && actionRuntime.MatchesSelectedOwner()
            && npcRuntime.SelectedRuntimeOwners != null
            && SelectedRuntimeOwnerGraph != null
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwnerGraph, SelectedRuntimeOwnerGraph)
            && SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(npcRuntime.SelectedRuntimeOwners)
            && npcRuntime.TryGetSelectedDailyV1ActionNode(
                action, out DailyV1ActionDefinition selectedActionNode)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentAction,
                selectedActionNode)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentPayload,
                actionRuntime.SelectedOwner);

        if (selectedActionProjectionMatches)
        {
            SelectedActionDispatchBoundaryAdapter dispatchBoundary =
                new SelectedActionDispatchBoundaryAdapter(
                    this, npcRuntime, actionRuntime, action);
            if (DailyV1SelectedActionDispatchStage.TryDispatch(
                    SelectedRuntimeOwnerGraph,
                    npcRuntime.SelectedRuntimeOwners,
                    randomSource,
                    CurrentDay,
                    dispatchBoundary,
                    out DailyV1SelectedActionDispatchResult selectedDispatch))
                return CompleteSelectedActionDispatch(
                    npcRuntime, actionRuntime, action, selectedDispatch);
            if (selectedGuardArrestAction)
            {
                if (runtimeAdmissionContext != null)
                {
                    FaultRuntimeAdmission();
                    throw new InvalidOperationException(
                        "The selected P12 profile cannot execute Guard Arrest without its exact Core Justice and NPC owner boundary.");
                }
                return NpcActionResult.Failed();
            }
            if (selectedCrimeStealAction)
            {
                if (runtimeAdmissionContext != null)
                {
                    FaultRuntimeAdmission();
                    throw new InvalidOperationException(
                        "The selected P12 profile cannot execute Steal without its exact Core social, Justice and NPC owner boundary.");
                }
                return NpcActionResult.Failed();
            }
            if (selectedCrimeAction)
            {
                if (runtimeAdmissionContext != null)
                {
                    FaultRuntimeAdmission();
                    throw new InvalidOperationException(
                        "The selected P12 profile cannot execute Hide or EscapePrison without its exact Core Justice and NPC owner boundary.");
                }
                return NpcActionResult.Failed();
            }
        }
        else if (selectedMerchantAction || selectedGuardArrestAction
            || selectedCrimeStealAction || selectedCrimeAction)
        {
            // Selected Daily-v1 actions without their exact Core projection
            // must not fall through to a second Unity-owned authority.
            if (runtimeAdmissionContext != null)
            {
                FaultRuntimeAdmission();
                throw new InvalidOperationException(
                    selectedGuardArrestAction
                        ? "The selected P12 profile cannot execute Guard Arrest without its exact Core action projection."
                        : selectedCrimeStealAction
                            ? "The selected P12 profile cannot execute Steal without its exact Core action projection."
                            : selectedCrimeAction
                                ? "The selected P12 profile cannot execute Hide or EscapePrison without its exact Core action projection."
                        : "The selected P12 profile cannot execute a Merchant action without its exact Core action projection.");
            }
            return NpcActionResult.Failed();
        }

        if (npcRuntime.UsesSelectedDailyV1Definitions
            && actionType == NpcActionType.Travel
            && actionProvider is TravelActionProvider selectedTravelProvider
            && npcRuntime.SelectedRuntimeOwners != null
            && SelectedRuntimeOwnerGraph != null
            && SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(npcRuntime.SelectedRuntimeOwners)
            && npcRuntime.TryGetSelectedDailyV1ActionNode(action,
                out DailyV1ActionDefinition selectedTravelAction)
            && actionRuntime != null
            && ReferenceEquals(actionRuntime.Action, action)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentAction,
                selectedTravelAction)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentPayload?.Action,
                selectedTravelAction)
            && string.Equals(npcRuntime.SelectedRuntimeOwners.ActionSlot.CurrentPayload.TargetCityRuntimeId,
                actionRuntime.TargetCity?.RuntimeId, StringComparison.Ordinal)
            && npcRuntime.SelectedRuntimeOwners.ActionSlot.CurrentPayload.TravelReason == actionRuntime.TravelReason
            && string.Equals(npcRuntime.SelectedRuntimeOwners.ActionSlot.CurrentPayload.OriginDecisionId,
                actionRuntime.OriginDecisionId, StringComparison.Ordinal))
        {
            SelectedSoloTravelActionBoundaryAdapter boundary =
                CreateSelectedSoloTravelActionBoundary(
                    npcRuntime, selectedTravelProvider, actionRuntime);

            if (boundary != null
                && DailyV1SelectedActionExecutionStage.TryExecuteSelectedSoloTravelActionSlot(
                    SelectedRuntimeOwnerGraph,
                    npcRuntime.SelectedRuntimeOwners,
                    selectedTravelAction,
                    randomSource,
                    CurrentDay,
                    boundary,
                    out DailyV1SelectedActionExecutionStage.SelectedSoloTravelActionResult selectedTravelResult))
            {
                if (selectedTravelResult?.Succeeded == true)
                    return NpcActionResult.Succeeded();
                return selectedTravelResult?.ActionRollSucceeded == true
                    ? NpcActionResult.Failed()
                    : NpcActionResult.Failed(CreateFailureMessage(npcRuntime, actionRuntime, action));
            }
        }

        if (selectedMerchantAction
            && actionType == NpcActionType.SellGoods
            && actionRuntime?.SelectedOwner != null
            && actionRuntime.TargetNpc != null
            && actionRuntime.TargetCity == null
            && actionRuntime.TargetItem != null
            && ReferenceEquals(actionRuntime.Action, action)
            && npcRuntime.TryGetSelectedDailyV1ActionNode(
                action, out DailyV1ActionDefinition selectedPlannedNpcAction)
            && npcRuntime.TryGetSelectedDailyV1Item(
                actionRuntime.TargetItem, out DailyV1ItemDefinition selectedPlannedNpcItem)
            && npcRuntime.SelectedRuntimeOwners != null
            && actionRuntime.TargetNpc.SelectedRuntimeOwners != null
            && SelectedRuntimeOwnerGraph != null
            && SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(npcRuntime.SelectedRuntimeOwners)
            && SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(
                actionRuntime.TargetNpc.SelectedRuntimeOwners)
            && ReferenceEquals(actionRuntime.TargetNpc.SelectedRuntimeOwnerGraph,
                SelectedRuntimeOwnerGraph)
            && ReferenceEquals(actionRuntime.TargetNpc.CurrentCity, npcRuntime.CurrentCity)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentAction,
                selectedPlannedNpcAction)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentPayload,
                actionRuntime.SelectedOwner)
            && ReferenceEquals(actionRuntime.SelectedOwner.Action, selectedPlannedNpcAction)
            && ReferenceEquals(actionRuntime.SelectedOwner.TargetItem, selectedPlannedNpcItem)
            && string.Equals(actionRuntime.SelectedOwner.TargetNpcRuntimeId,
                actionRuntime.TargetNpc.RuntimeId, StringComparison.Ordinal)
            && actionRuntime.SelectedOwner.TargetCityRuntimeId == null
            && actionRuntime.SelectedOwner.Amount == actionRuntime.Amount
            && actionRuntime.SelectedOwner.SuccessChanceMultiplier.Equals(
                actionRuntime.SuccessChanceMultiplier)
            && string.Equals(actionRuntime.SelectedOwner.OriginDecisionId,
                actionRuntime.OriginDecisionId, StringComparison.Ordinal)
            && npcRuntime.SelectedRuntimeOwners.MerchantTradePlan?.IsActive == true)
        {
            float referencePrice = npcRuntime.MerchantTradePlan.PurchasePricePerItem;
            if (economyTransactionService.TryExecuteSelectedPlannedNpcSaleActionSlot(
                    npcRuntime, actionRuntime.TargetNpc, selectedPlannedNpcAction,
                    randomSource, CurrentDay, out bool actionRollSucceeded,
                    out DailyV1SelectedActionExecutionStage.PlannedNpcSaleResult sale))
            {
                if (!actionRollSucceeded)
                    return NpcActionResult.Failed(CreateFailureMessage(npcRuntime, actionRuntime, action));
                if (sale == null || !sale.Succeeded)
                    return NpcActionResult.Failed();

                float profit = (sale.UnitPrice - referencePrice) * sale.Quantity;
                if (sale.SoldToNpc)
                {
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} vendeu {sale.Quantity} {actionRuntime.TargetItem.itemName} para {actionRuntime.TargetNpc.NpcName} em {npcRuntime.CurrentCity.CityName} por {sale.UnitPrice:0.##} cada");
                    logger?.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {profit:0.##}");
                }
                else
                {
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} vendeu {sale.Quantity} {actionRuntime.TargetItem.itemName} ao mercado de {npcRuntime.CurrentCity.CityName} por {sale.UnitPrice:0.##} cada");
                    logger?.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {profit:0.##}");
                }
                if (sale.PlanCompleted)
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} concluiu o plano comercial.");
                return NpcActionResult.Succeeded();
            }
        }

        if (selectedMerchantAction
            && actionType == NpcActionType.SellGoods
            && actionRuntime?.SelectedOwner != null
            && actionRuntime.TargetNpc == null
            && actionRuntime.TargetCity != null
            && ReferenceEquals(actionRuntime.TargetCity, npcRuntime.CurrentCity)
            && actionRuntime.TargetItem != null
            && ReferenceEquals(actionRuntime.Action, action)
            && npcRuntime.TryGetSelectedDailyV1ActionNode(
                action, out DailyV1ActionDefinition selectedPlannedMarketAction)
            && npcRuntime.TryGetSelectedDailyV1Item(
                actionRuntime.TargetItem, out DailyV1ItemDefinition selectedPlannedMarketItem)
            && npcRuntime.SelectedRuntimeOwners != null
            && SelectedRuntimeOwnerGraph != null
            && SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(npcRuntime.SelectedRuntimeOwners)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentAction,
                selectedPlannedMarketAction)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentPayload,
                actionRuntime.SelectedOwner)
            && ReferenceEquals(actionRuntime.SelectedOwner.Action, selectedPlannedMarketAction)
            && ReferenceEquals(actionRuntime.SelectedOwner.TargetItem, selectedPlannedMarketItem)
            && actionRuntime.SelectedOwner.TargetNpcRuntimeId == null
            && actionRuntime.SelectedOwner.Amount == actionRuntime.Amount
            && actionRuntime.SelectedOwner.SuccessChanceMultiplier.Equals(
                actionRuntime.SuccessChanceMultiplier)
            && string.Equals(actionRuntime.SelectedOwner.TargetCityRuntimeId,
                npcRuntime.CurrentCity.RuntimeId, StringComparison.Ordinal)
            && string.Equals(actionRuntime.SelectedOwner.OriginDecisionId,
                actionRuntime.OriginDecisionId, StringComparison.Ordinal)
            && npcRuntime.SelectedRuntimeOwners.MerchantTradePlan?.IsActive == true)
        {
            float referencePrice = npcRuntime.MerchantTradePlan.PurchasePricePerItem;
            if (economyTransactionService.TryExecuteSelectedPlannedMarketSaleActionSlot(
                    npcRuntime, selectedPlannedMarketAction, randomSource, CurrentDay,
                    out bool actionRollSucceeded,
                    out DailyV1SelectedActionExecutionStage.PlannedMarketSaleResult sale))
            {
                if (!actionRollSucceeded)
                    return NpcActionResult.Failed(CreateFailureMessage(npcRuntime, actionRuntime, action));
                if (sale == null || !sale.Succeeded)
                    return NpcActionResult.Failed();

                float approximateProfit = (sale.UnitPrice - referencePrice) * sale.Quantity;
                logger?.Log(SimulationLogCategory.Trade,
                    $"{npcRuntime.NpcName} vendeu {sale.Quantity} {actionRuntime.TargetItem.itemName} ao mercado de {npcRuntime.CurrentCity.CityName} por {sale.UnitPrice:0.##} cada");
                logger?.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {approximateProfit:0.##}");
                if (sale.PlanCompleted)
                    logger?.Log(SimulationLogCategory.Trade, $"{npcRuntime.NpcName} concluiu o plano comercial.");
                return NpcActionResult.Succeeded();
            }
        }

        if (selectedMerchantAction
            && (actionType == NpcActionType.BuyGoods || actionType == NpcActionType.SellGoods)
            && actionRuntime?.SelectedOwner != null
            && actionRuntime.TargetNpc == null
            && actionRuntime.TargetCity != null
            && ReferenceEquals(actionRuntime.TargetCity, npcRuntime.CurrentCity)
            && actionRuntime.TargetItem != null
            && ReferenceEquals(actionRuntime.Action, action)
            && npcRuntime.TryGetSelectedDailyV1ActionNode(
                action, out DailyV1ActionDefinition selectedMarketAction)
            && npcRuntime.TryGetSelectedDailyV1Item(
                actionRuntime.TargetItem, out DailyV1ItemDefinition selectedMarketItem)
            && npcRuntime.SelectedRuntimeOwners != null
            && SelectedRuntimeOwnerGraph != null
            && SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(npcRuntime.SelectedRuntimeOwners)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentAction,
                selectedMarketAction)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentPayload,
                actionRuntime.SelectedOwner)
            && ReferenceEquals(actionRuntime.SelectedOwner.Action, selectedMarketAction)
            && ReferenceEquals(actionRuntime.SelectedOwner.TargetItem, selectedMarketItem)
            && actionRuntime.SelectedOwner.TargetNpcRuntimeId == null
            && actionRuntime.SelectedOwner.Amount == actionRuntime.Amount
            && actionRuntime.SelectedOwner.SuccessChanceMultiplier.Equals(
                actionRuntime.SuccessChanceMultiplier)
            && string.Equals(actionRuntime.SelectedOwner.TargetCityRuntimeId,
                npcRuntime.CurrentCity.RuntimeId, StringComparison.Ordinal)
            && string.Equals(actionRuntime.SelectedOwner.OriginDecisionId,
                actionRuntime.OriginDecisionId, StringComparison.Ordinal)
            && (actionType != NpcActionType.SellGoods
                || npcRuntime.SelectedRuntimeOwners.MerchantTradePlan?.IsActive != true))
        {
            float preSaleAverageUnitCost = actionType == NpcActionType.SellGoods
                ? npcRuntime.SelectedRuntimeOwners.Inventory.GetAverageUnitCost(selectedMarketItem)
                : 0f;
            if (economyTransactionService.TryExecuteSelectedOpenMarketActionSlot(
                    npcRuntime, selectedMarketAction, randomSource, CurrentDay,
                    out bool actionRollSucceeded, out EconomyTransactionResult transaction))
            {
                if (!actionRollSucceeded)
                    return NpcActionResult.Failed(CreateFailureMessage(npcRuntime, actionRuntime, action));
                if (!transaction.Success || transaction.Quantity <= 0)
                    return NpcActionResult.Failed();

                if (actionType == NpcActionType.BuyGoods)
                {
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} comprou {transaction.Quantity} {actionRuntime.TargetItem.itemName} em {npcRuntime.CurrentCity.CityName} por {transaction.UnitPrice:0.##} cada para estoque local");
                }
                else
                {
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} vendeu {transaction.Quantity} {actionRuntime.TargetItem.itemName} ao mercado de {npcRuntime.CurrentCity.CityName} por {transaction.UnitPrice:0.##} cada");
                    float approximateProfit = DailyV1SelectedMarketSaleProfit.Calculate(
                        preSaleAverageUnitCost, transaction.UnitPrice, transaction.Quantity);
                    logger?.Log(SimulationLogCategory.Trade,
                        $"Lucro aproximado: {approximateProfit:0.##}");
                }
                return NpcActionResult.Succeeded();
            }
        }

        if (selectedMerchantAction
            && actionType == NpcActionType.BuyGoods
            && actionRuntime?.SelectedOwner != null
            && actionRuntime.TargetNpc == null
            && actionRuntime.TargetCity != null
            && !ReferenceEquals(actionRuntime.TargetCity, npcRuntime.CurrentCity)
            && actionRuntime.TargetItem != null
            && ReferenceEquals(actionRuntime.Action, action)
            && npcRuntime.TryGetSelectedDailyV1ActionNode(
                action, out DailyV1ActionDefinition selectedCrossCityBuyAction)
            && npcRuntime.TryGetSelectedDailyV1Item(
                actionRuntime.TargetItem, out DailyV1ItemDefinition selectedCrossCityBuyItem)
            && npcRuntime.SelectedRuntimeOwners != null
            && SelectedRuntimeOwnerGraph != null
            && SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(npcRuntime.SelectedRuntimeOwners)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentAction,
                selectedCrossCityBuyAction)
            && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.ActionSlot?.CurrentPayload,
                actionRuntime.SelectedOwner)
            && ReferenceEquals(actionRuntime.SelectedOwner.Action, selectedCrossCityBuyAction)
            && ReferenceEquals(actionRuntime.SelectedOwner.TargetItem, selectedCrossCityBuyItem)
            && actionRuntime.SelectedOwner.TargetNpcRuntimeId == null
            && actionRuntime.SelectedOwner.Amount == actionRuntime.Amount
            && actionRuntime.SelectedOwner.SuccessChanceMultiplier.Equals(
                actionRuntime.SuccessChanceMultiplier)
            && string.Equals(actionRuntime.SelectedOwner.TargetCityRuntimeId,
                actionRuntime.TargetCity.RuntimeId, StringComparison.Ordinal)
            && string.Equals(actionRuntime.SelectedOwner.OriginDecisionId,
                actionRuntime.OriginDecisionId, StringComparison.Ordinal))
        {
            if (economyTransactionService.TryExecuteSelectedCrossCityBuyActionSlot(
                    npcRuntime, selectedCrossCityBuyAction, actionRuntime.SelectedOwner,
                    actionRuntime.TargetCity, randomSource, CurrentDay,
                    out bool actionRollSucceeded, out EconomyTransactionResult purchase))
            {
                if (!actionRollSucceeded)
                    return NpcActionResult.Failed(CreateFailureMessage(npcRuntime, actionRuntime, action));
                if (!purchase.Success || purchase.Quantity <= 0)
                    return NpcActionResult.Failed();

                logger?.Log(SimulationLogCategory.Trade,
                    $"{npcRuntime.NpcName} comprou {purchase.Quantity} {actionRuntime.TargetItem.itemName} em {npcRuntime.CurrentCity.CityName} por {purchase.UnitPrice:0.##} cada para vender em {actionRuntime.TargetCity.CityName}");
                return NpcActionResult.Succeeded();
            }
        }

        // Once an actor belongs to the selected authoritative owner graph,
        // action execution must stay on the graph-backed Core route above.
        // Reaching the provider calls below means Core could not admit or
        // execute the exact current slot; falling through would let a Unity
        // provider become a second action authority for the same actor.
        if (exactSelectedGraphActor)
        {
            if (runtimeAdmissionContext != null)
            {
                FaultRuntimeAdmission();
                throw new InvalidOperationException(
                    "The selected P12 profile cannot execute an action outside its exact Core owner-slot dispatch.");
            }

            return NpcActionResult.Failed();
        }

        if (RollActionSuccess(npcRuntime, action, actionRuntime) == false)
        {
            if (actionProvider is INpcActionFailureHandler failureHandler)
            {
                NpcActionResult failureResult;
                if (runtimeAdmissionContext != null
                    && (actionProvider is CrimeSystem || actionProvider is GuardSystem))
                {
                    IReadOnlyList<NpcRuntime> participants = actionRuntime?.TargetNpc == null
                        ? new[] { npcRuntime }
                        : new[] { npcRuntime, actionRuntime.TargetNpc };
                    if (!TryBeginP12CrimeJusticeActionScope(
                            participants,
                            includeJustice: true,
                            out P12CrimeJusticeMutationScope failureScope))
                        throw new InvalidOperationException("P12 Crime/Guard failure handling could not be admitted.");
                    using (failureScope)
                        failureResult = failureHandler.HandleActionFailure(npcRuntime, actionRuntime);
                }
                else
                {
                    failureResult = failureHandler.HandleActionFailure(npcRuntime, actionRuntime);
                }

                if (failureResult != null)
                {
                    return failureResult;
                }
            }

            return NpcActionResult.Failed(CreateFailureMessage(npcRuntime, actionRuntime, action));
        }

        if (selectedMerchantAction
            && actionType == NpcActionType.SellGoods
            && actionRuntime?.SelectedOwner != null
            && actionRuntime.TargetNpc != null
            && npcRuntime.SelectedRuntimeOwners?.MerchantTradePlan?.IsActive == true)
        {
            if (economyTransactionService.TryExecuteSelectedPlannedNpcSale(
                    npcRuntime, actionRuntime.TargetNpc,
                    out DailyV1SelectedActionExecutionStage.PlannedNpcSaleResult sale))
            {
                if (!sale.Succeeded)
                    return NpcActionResult.Failed();
                float referencePrice = npcRuntime.MerchantTradePlan.PurchasePricePerItem;
                float profit = (sale.UnitPrice - referencePrice) * sale.Quantity;
                if (sale.SoldToNpc)
                {
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} vendeu {sale.Quantity} {actionRuntime.TargetItem.itemName} para {actionRuntime.TargetNpc.NpcName} em {npcRuntime.CurrentCity.CityName} por {sale.UnitPrice:0.##} cada");
                    logger?.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {profit:0.##}");
                }
                else
                {
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} vendeu {sale.Quantity} {actionRuntime.TargetItem.itemName} ao mercado de {npcRuntime.CurrentCity.CityName} por {sale.UnitPrice:0.##} cada");
                    logger?.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {profit:0.##}");
                }
                if (sale.PlanCompleted)
                    logger?.Log(SimulationLogCategory.Trade, $"{npcRuntime.NpcName} concluiu o plano comercial.");
                return NpcActionResult.Succeeded();
            }
        }

        if (selectedMerchantAction
            && actionType == NpcActionType.SellGoods
            && actionRuntime?.SelectedOwner != null
            && actionRuntime.TargetNpc == null
            && npcRuntime.SelectedRuntimeOwners?.MerchantTradePlan?.IsActive == true
            && actionRuntime.TargetCity == npcRuntime.CurrentCity)
        {
            if (actionRuntime.TargetItem == null || npcRuntime.CurrentCity == null)
                return NpcActionResult.Failed();

            float planPurchasePrice = npcRuntime.MerchantTradePlan.PurchasePricePerItem;
            if (!economyTransactionService.TryExecuteSelectedPlannedMarketSale(
                    npcRuntime,
                    out DailyV1SelectedActionExecutionStage.PlannedMarketSaleResult sale)
                || sale == null || !sale.Succeeded)
                return NpcActionResult.Failed();

            float approximateProfit = (sale.UnitPrice - planPurchasePrice) * sale.Quantity;
            logger?.Log(SimulationLogCategory.Trade,
                $"{npcRuntime.NpcName} vendeu {sale.Quantity} {actionRuntime.TargetItem.itemName} ao mercado de {npcRuntime.CurrentCity.CityName} por {sale.UnitPrice:0.##} cada");
            logger?.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {approximateProfit:0.##}");
            if (sale.PlanCompleted)
                logger?.Log(SimulationLogCategory.Trade, $"{npcRuntime.NpcName} concluiu o plano comercial.");
            return NpcActionResult.Succeeded();
        }

        if (actionType == NpcActionType.Normal)
        {
            return NpcActionResult.Succeeded(CreateNormalActionMessage(npcRuntime, action));
        }

        if (runtimeAdmissionContext != null
            && actionProvider is TravelActionProvider travelActionProvider)
        {
            if (!TryBeginP12SoloTravelStartOperation(
                npcRuntime,
                travelActionProvider,
                out P12SoloTravelStartOperationScope travelStartScope))
                throw new InvalidOperationException(
                    "The P12 solo travel-start owner operation could not be admitted.");

            using (travelStartScope)
                return travelActionProvider.TryExecuteAction(npcRuntime, actionRuntime);
        }

        if (runtimeAdmissionContext != null
            && (actionProvider is CrimeSystem || actionProvider is GuardSystem))
        {
            IReadOnlyList<NpcRuntime> participants = actionRuntime?.TargetNpc == null
                ? new[] { npcRuntime }
                : new[] { npcRuntime, actionRuntime.TargetNpc };
            if (!TryBeginP12CrimeJusticeActionScope(
                    participants,
                    includeJustice: true,
                    out P12CrimeJusticeMutationScope crimeJusticeScope))
                throw new InvalidOperationException("P12 Crime/Guard action could not be admitted.");
            using (crimeJusticeScope)
                return actionProvider.TryExecuteAction(npcRuntime, actionRuntime);
        }

        if (selectedMerchantAction
            && actionRuntime?.SelectedOwner != null
            && (actionType == NpcActionType.BuyGoods || actionType == NpcActionType.SellGoods)
            && actionRuntime.TargetNpc == null
            && npcRuntime.CurrentCity != null
            && npcRuntime.SelectedRuntimeOwnerGraph != null
            && npcRuntime.SelectedRuntimeOwnerGraph.ContainsExactNpcOwner(npcRuntime.SelectedRuntimeOwners)
            && (actionType != NpcActionType.SellGoods
                || npcRuntime.SelectedRuntimeOwners?.MerchantTradePlan?.IsActive != true)
            && actionRuntime.TargetCity == npcRuntime.CurrentCity)
        {
            float preSaleAverageUnitCost = 0f;
            if (actionType == NpcActionType.SellGoods
                && npcRuntime.SelectedRuntimeOwners.Inventory != null
                && ReferenceEquals(npcRuntime.SelectedRuntimeOwners.Inventory.Definitions,
                    npcRuntime.SelectedRuntimeOwnerGraph.Definitions)
                && actionRuntime.SelectedOwner.TargetItem != null
                && npcRuntime.SelectedRuntimeOwnerGraph.Definitions.Items.TryGetValue(
                    actionRuntime.SelectedOwner.TargetItem.DefinitionId,
                    out DailyV1ItemDefinition selectedSaleItem)
                && ReferenceEquals(selectedSaleItem, actionRuntime.SelectedOwner.TargetItem))
                preSaleAverageUnitCost = npcRuntime.SelectedRuntimeOwners.Inventory.GetAverageUnitCost(
                    actionRuntime.SelectedOwner.TargetItem);

            EconomyTransactionResult selectedPurchase = economyTransactionService.TryExecuteSelectedOpenMarketAction(
                npcRuntime, actionRuntime.SelectedOwner);
            if (!selectedPurchase.Success || selectedPurchase.Quantity <= 0)
                return NpcActionResult.Failed();

            if (actionType == NpcActionType.BuyGoods)
            {
                logger?.Log(SimulationLogCategory.Trade,
                    $"{npcRuntime.NpcName} comprou {selectedPurchase.Quantity} {actionRuntime.TargetItem.itemName} em {npcRuntime.CurrentCity.CityName} por {selectedPurchase.UnitPrice:0.##} cada para estoque local");
            }
            else
            {
                logger?.Log(SimulationLogCategory.Trade,
                    $"{npcRuntime.NpcName} vendeu {selectedPurchase.Quantity} {actionRuntime.TargetItem.itemName} ao mercado de {npcRuntime.CurrentCity.CityName} por {selectedPurchase.UnitPrice:0.##} cada");
                float approximateProfit = DailyV1SelectedMarketSaleProfit.Calculate(
                    preSaleAverageUnitCost, selectedPurchase.UnitPrice, selectedPurchase.Quantity);
                logger?.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {approximateProfit:0.##}");
            }
            return NpcActionResult.Succeeded();
        }

        if (selectedMerchantAction)
            return merchantSystem.TryExecuteAction(npcRuntime, actionRuntime);

        return actionProvider.TryExecuteAction(npcRuntime, actionRuntime);
    }

    private NpcActionResult CompleteSelectedActionDispatch(
        NpcRuntime npcRuntime,
        NpcActionRuntime actionRuntime,
        NpcActionData action,
        DailyV1SelectedActionDispatchResult dispatch)
    {
        if (dispatch == null) return NpcActionResult.Failed();

        if (dispatch.Kind == DailyV1SelectedActionDispatchKind.PlannedMarketSale)
            economyTransactionService?.FaultSelectedPlannedMarketSaleIfPartial(
                dispatch.PlannedMarketSale?.Sale);
        else if (dispatch.Kind == DailyV1SelectedActionDispatchKind.PlannedNpcSale)
            economyTransactionService?.FaultSelectedPlannedNpcSaleIfPartial(
                dispatch.PlannedNpcSale?.Sale);

        if (!dispatch.ActionRollSucceeded)
            return NpcActionResult.Failed(CreateFailureMessage(npcRuntime, actionRuntime, action));
        if (!dispatch.Succeeded)
            return NpcActionResult.Failed();

        switch (dispatch.Kind)
        {
            case DailyV1SelectedActionDispatchKind.Normal:
            {
                string actorName = npcRuntime != null ? npcRuntime.NpcName : "NPC desconhecido";
                if (!string.IsNullOrEmpty(dispatch.NormalActionLogText))
                    return NpcActionResult.Succeeded($"{actorName} {dispatch.NormalActionLogText}");
                return NpcActionResult.Succeeded(
                    $"{actorName} realizou {dispatch.NormalActionName ?? string.Empty}.");
            }

            case DailyV1SelectedActionDispatchKind.SoloTravel:
                return NpcActionResult.Succeeded();

            case DailyV1SelectedActionDispatchKind.GuardArrest:
                return dispatch.GuardArrest?.Succeeded == true
                    ? NpcActionResult.Succeeded()
                    : NpcActionResult.Failed();

            case DailyV1SelectedActionDispatchKind.Steal:
                return dispatch.Steal?.Succeeded == true
                    ? NpcActionResult.Succeeded()
                    : NpcActionResult.Failed();

            case DailyV1SelectedActionDispatchKind.Crime:
            {
                if (dispatch.Crime?.Succeeded != true)
                    return NpcActionResult.Failed();

                if (npcRuntime != null
                    && action != null
                    && npcRuntime.GetConfiguredActionType(action) == NpcActionType.EscapePrison)
                {
                    string wantedText = justiceSystem?.HasAnyActiveWarrant(npcRuntime) == true
                        ? "continua procurado"
                        : "nao possui mandado ativo";
                    logger?.Log(SimulationLogCategory.Crime,
                        $"{npcRuntime.NpcName} esta livre e {wantedText}.");
                }

                return NpcActionResult.Succeeded();
            }

            case DailyV1SelectedActionDispatchKind.FleeCity:
            {
                if (dispatch.FleeCity?.Succeeded != true || actionRuntime.TargetCity == null)
                    return NpcActionResult.Failed();
                logger?.Log(SimulationLogCategory.Crime,
                    $"{npcRuntime.NpcName} decidiu fugir para {actionRuntime.TargetCity.CityName}. Custo de viagem: {dispatch.FleeCity.TravelCost:0.##}.");
                return NpcActionResult.Succeeded();
            }

            case DailyV1SelectedActionDispatchKind.PlannedNpcSale:
            {
                DailyV1SelectedActionExecutionStage.PlannedNpcSaleResult sale =
                    dispatch.PlannedNpcSale?.Sale;
                if (sale == null || !sale.Succeeded || actionRuntime.TargetItem == null
                    || actionRuntime.TargetNpc == null || npcRuntime.CurrentCity == null)
                    return NpcActionResult.Failed();

                float profit = (sale.UnitPrice - dispatch.ReferencePrice) * sale.Quantity;
                if (sale.SoldToNpc)
                {
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} vendeu {sale.Quantity} {actionRuntime.TargetItem.itemName} para {actionRuntime.TargetNpc.NpcName} em {npcRuntime.CurrentCity.CityName} por {sale.UnitPrice:0.##} cada");
                    logger?.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {profit:0.##}");
                }
                else
                {
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} vendeu {sale.Quantity} {actionRuntime.TargetItem.itemName} ao mercado de {npcRuntime.CurrentCity.CityName} por {sale.UnitPrice:0.##} cada");
                    logger?.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {profit:0.##}");
                }
                if (sale.PlanCompleted)
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} concluiu o plano comercial.");
                return NpcActionResult.Succeeded();
            }

            case DailyV1SelectedActionDispatchKind.PlannedMarketSale:
            {
                DailyV1SelectedActionExecutionStage.PlannedMarketSaleResult sale =
                    dispatch.PlannedMarketSale?.Sale;
                if (sale == null || !sale.Succeeded || actionRuntime.TargetItem == null
                    || npcRuntime.CurrentCity == null)
                    return NpcActionResult.Failed();

                float profit = (sale.UnitPrice - dispatch.ReferencePrice) * sale.Quantity;
                logger?.Log(SimulationLogCategory.Trade,
                    $"{npcRuntime.NpcName} vendeu {sale.Quantity} {actionRuntime.TargetItem.itemName} ao mercado de {npcRuntime.CurrentCity.CityName} por {sale.UnitPrice:0.##} cada");
                logger?.Log(SimulationLogCategory.Trade, $"Lucro aproximado: {profit:0.##}");
                if (sale.PlanCompleted)
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} concluiu o plano comercial.");
                return NpcActionResult.Succeeded();
            }

            case DailyV1SelectedActionDispatchKind.OpenMarket:
            {
                if (actionRuntime.TargetItem == null || npcRuntime.CurrentCity == null)
                    return NpcActionResult.Failed();
                if (npcRuntime.GetConfiguredActionType(action) == NpcActionType.BuyGoods)
                {
                    DailyV1SelectedOpenMarketPurchasePlan purchase =
                        dispatch.OpenMarket?.Purchase?.Plan;
                    if (purchase == null) return NpcActionResult.Failed();
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} comprou {purchase.Quantity} {actionRuntime.TargetItem.itemName} em {npcRuntime.CurrentCity.CityName} por {purchase.UnitPrice:0.##} cada para estoque local");
                }
                else
                {
                    DailyV1SelectedOpenMarketSalePlan sale = dispatch.OpenMarket?.Sale?.Plan;
                    if (sale == null) return NpcActionResult.Failed();
                    logger?.Log(SimulationLogCategory.Trade,
                        $"{npcRuntime.NpcName} vendeu {sale.Quantity} {actionRuntime.TargetItem.itemName} ao mercado de {npcRuntime.CurrentCity.CityName} por {sale.UnitPrice:0.##} cada");
                    float profit = DailyV1SelectedMarketSaleProfit.Calculate(
                        dispatch.ReferenceUnitCost, sale.UnitPrice, sale.Quantity);
                    logger?.Log(SimulationLogCategory.Trade,
                        $"Lucro aproximado: {profit:0.##}");
                }
                return NpcActionResult.Succeeded();
            }

            case DailyV1SelectedActionDispatchKind.CrossCityBuy:
            {
                DailyV1SelectedCrossCityBuyResult purchase =
                    dispatch.CrossCityBuy?.Purchase;
                if (purchase?.Succeeded != true || actionRuntime.TargetItem == null
                    || actionRuntime.TargetCity == null || npcRuntime.CurrentCity == null)
                    return NpcActionResult.Failed();
                logger?.Log(SimulationLogCategory.Trade,
                    $"{npcRuntime.NpcName} comprou {purchase.Quantity} {actionRuntime.TargetItem.itemName} em {npcRuntime.CurrentCity.CityName} por {purchase.UnitPrice:0.##} cada para vender em {actionRuntime.TargetCity.CityName}");
                return NpcActionResult.Succeeded();
            }

            default:
                return NpcActionResult.Failed();
        }
    }

    private List<NpcActionData> GetConfiguredActionsEnabledForRuntime(NpcRuntime npcRuntime)
    {
        List<NpcActionData> enabledActions = new List<NpcActionData>();

        List<NpcActionData> actionDefinitions = new List<NpcActionData>();
        if (npcRuntime != null && npcRuntime.UsesSelectedDailyV1Definitions)
        {
            // The typed graph includes dependency-closed action definitions.
            // Runtime availability still comes from the selected profile's
            // captured action allowlist, projected through exact asset/node
            // bindings; do not expose every referenced definition as an action.
            if (configuredActions == null)
                return enabledActions;
            actionDefinitions.AddRange(configuredActions);
        }
        else if (configuredActions != null)
        {
            actionDefinitions.AddRange(configuredActions);
        }

        foreach (NpcActionData action in actionDefinitions)
        {
            if (npcRuntime != null && npcRuntime.UsesSelectedDailyV1Definitions
                && !npcRuntime.TryGetSelectedDailyV1ActionNode(action, out _))
                continue;
            if (IsActionEnabledForRuntime(npcRuntime, action))
            {
                enabledActions.Add(action);
            }
        }

        return enabledActions;
    }

    private List<DailyV1ActionDefinition> GetSelectedConfiguredActionsEnabledForRuntime(NpcRuntime npcRuntime)
    {
        List<DailyV1ActionDefinition> enabledActions = new List<DailyV1ActionDefinition>();
        if (npcRuntime == null || !npcRuntime.UsesSelectedDailyV1Definitions || configuredActions == null)
            return enabledActions;

        foreach (NpcActionData configuredAction in configuredActions)
        {
            if (!npcRuntime.TryGetSelectedDailyV1ActionNode(
                    configuredAction, out DailyV1ActionDefinition action))
                continue;
            enabledActions.Add(action);
        }
        return enabledActions;
    }

    private bool IsActionEnabledForRuntime(NpcRuntime npcRuntime, NpcActionData action)
    {
        if (action == null)
        {
            return false;
        }

        if (npcRuntime?.UsesSelectedDailyV1Definitions == true)
        {
            return npcRuntime.TryGetSelectedDailyV1ActionNode(
                    action, out DailyV1ActionDefinition selectedAction)
                && DailyV1SelectedNpcActionDecisionStage.IsEnabledForRuntime(
                    npcRuntime.SelectedDailyV1DefinitionGraph, selectedAction);
        }

        switch (npcRuntime != null ? npcRuntime.GetConfiguredActionType(action) : action.actionType)
        {
            case NpcActionType.BuyGoods:
            case NpcActionType.SellGoods:
                return configuration.MerchantTrade.Enabled;
            case NpcActionType.Steal:
            case NpcActionType.Hide:
            case NpcActionType.FleeCity:
            case NpcActionType.EscapePrison:
                return configuration.Crime.Enabled;
            case NpcActionType.Arrest:
                return configuration.GuardCrime.Enabled;
            default:
                return true;
        }
    }

    private bool RollActionSuccess(
        NpcRuntime npcRuntime,
        NpcActionData action,
        NpcActionRuntime actionRuntime)
    {
        if (npcRuntime != null && npcRuntime.UsesSelectedDailyV1Definitions)
        {
            string selectedDecisionId = actionRuntime?.OriginDecisionId
                ?? actionRuntime?.Action?.DefinitionId
                ?? "unbound";
            return npcRuntime.TryRollSelectedDailyV1ActionSuccess(
                action,
                selectedDecisionId,
                actionRuntime != null ? actionRuntime.SuccessChanceMultiplier : 1f,
                CurrentDay,
                randomSource,
                out bool succeeded)
                && succeeded;
        }

        if (action == null || (npcRuntime != null
            ? npcRuntime.GetConfiguredActionCanFail(action)
            : action.canFail) == false)
        {
            return true;
        }

        float contextualMultiplier = actionRuntime != null ? actionRuntime.SuccessChanceMultiplier : 1f;
        float configuredSuccessChance = npcRuntime != null
            ? npcRuntime.GetConfiguredActionBaseSuccessChance(action)
            : action.baseSuccessChance;
        float effectiveChance = UnityMathfCompatibility.Clamp01(
            configuredSuccessChance * UnityMathfCompatibility.Max(0f, contextualMultiplier));
        string actorKey = npcRuntime?.RuntimeId ?? "unknown-actor";
        string decisionKey = actionRuntime?.OriginDecisionId
            ?? actionRuntime?.Action?.DefinitionId
            ?? "unbound";
        string actionKey = action?.DefinitionId
            ?? (npcRuntime != null ? npcRuntime.GetConfiguredActionType(action).ToString() : action?.actionType.ToString())
            ?? "unknown";
        float randomValue = randomSource.NextUnit(
            "action-success|" + actorKey + "|" + decisionKey + "|" + actionKey + "|" + CurrentDay);
        return randomValue <= effectiveChance;
    }

    private void ApplySuccessStatusChanges(
        NpcRuntime npcRuntime,
        NpcActionRuntime actionRuntime,
        NpcActionData action,
        bool actionSucceeded,
        bool reuseForcedEscapeScope = false)
    {
        if (npcRuntime == null)
            throw new InvalidOperationException("The admitted action status effects could not be resolved from the selected definition graph.");

        if (npcRuntime.UsesSelectedDailyV1Definitions)
        {
            NpcRuntime targetRuntime = actionRuntime != null ? actionRuntime.TargetNpc : null;
            DailyV1ActionDefinition selectedAction = null;
            if (actionSucceeded
                && !npcRuntime.TryGetSelectedDailyV1ActionNode(action, out selectedAction))
                throw new InvalidOperationException("The admitted action status effects could not be resolved from the selected definition graph.");

            if (!DailyV1SelectedActionExecutionStage.TryApplyActionStatusPlanAfterDispatch(
                    actionSucceeded,
                    SelectedRuntimeOwnerGraph,
                    npcRuntime.SelectedRuntimeOwners,
                    selectedAction,
                    targetRuntime != null,
                    targetRuntime != null ? targetRuntime.SelectedRuntimeOwners : null,
                    new SelectedActionStatusOperationBoundaryAdapter(
                        this, npcRuntime, targetRuntime, reuseForcedEscapeScope),
                    out DailyV1SelectedActionStatusPlanResult statusResult))
                throw new InvalidOperationException("The admitted action status effects could not be resolved from the selected definition graph.");

            if (statusResult == DailyV1SelectedActionStatusPlanResult.ActionDidNotSucceed)
                return;
            if (statusResult == DailyV1SelectedActionStatusPlanResult.ActorRejected)
                throw new InvalidOperationException("The admitted action status effects could not be resolved from the selected definition graph.");
            if (statusResult == DailyV1SelectedActionStatusPlanResult.TargetRejected)
                throw new InvalidOperationException("The selected target has no matching admitted status owner.");
            return;
        }

        if (!npcRuntime.TryGetConfiguredActionStatusChanges(
                action,
                out IReadOnlyList<NpcStatusData> statusToRemove,
                out IReadOnlyList<NpcStatusData> statusToAdd,
                out IReadOnlyList<NpcStatusData> targetStatusToRemove,
                out IReadOnlyList<NpcStatusData> targetStatusToAdd))
            throw new InvalidOperationException(
                "The admitted action status effects could not be resolved from the selected definition graph "
                + "(NPC '" + npcRuntime.RuntimeId + "', action '" + action.DefinitionId + "').");

        ApplyStatusChanges(npcRuntime, statusToRemove, statusToAdd);

        if (actionRuntime != null && actionRuntime.TargetNpc != null)
        {
            ApplyStatusChanges(actionRuntime.TargetNpc, targetStatusToRemove, targetStatusToAdd);
        }
    }

    private void ApplyStatusChanges(NpcRuntime npcRuntime, IReadOnlyList<NpcStatusData> statusToRemove, IReadOnlyList<NpcStatusData> statusToAdd)
    {
        if (npcRuntime == null)
        {
            return;
        }

        if (statusToRemove != null)
        {
            foreach (NpcStatusData status in statusToRemove)
            {
                npcRuntime.RemoveStatus(status);
            }
        }

        if (statusToAdd != null)
        {
            foreach (NpcStatusData status in statusToAdd)
            {
                npcRuntime.AddStatus(status);
            }
        }
    }

    private void LogChosenTargetAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime)
    {
        if (npcRuntime == null || actionRuntime == null || actionRuntime.Action == null)
        {
            return;
        }

        if (actionRuntime.TargetNpc != null)
        {
            logger?.Log(SimulationLogCategory.NpcAction, $"{npcRuntime.NpcName} escolheu {GetActionName(npcRuntime, actionRuntime.Action)} {actionRuntime.TargetNpc.NpcName}.");
        }
        else if (actionRuntime.TargetCity != null)
        {
            logger?.Log(SimulationLogCategory.NpcAction, $"{npcRuntime.NpcName} escolheu {GetActionName(npcRuntime, actionRuntime.Action)} {actionRuntime.TargetCity.CityName}.");
        }
    }

    private string CreateFailureMessage(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        string actorName = npcRuntime != null ? npcRuntime.NpcName : "NPC desconhecido";
        string targetName = actionRuntime != null && actionRuntime.TargetNpc != null ? $" {actionRuntime.TargetNpc.NpcName}" : string.Empty;
        string targetCityName = actionRuntime != null && actionRuntime.TargetCity != null ? $" {actionRuntime.TargetCity.CityName}" : string.Empty;
        return $"{actorName} tentou {GetActionName(npcRuntime, action)}{targetName}{targetCityName}, mas falhou.";
    }

    private string CreateNormalActionMessage(NpcRuntime npcRuntime, NpcActionData action)
    {
        string actorName = npcRuntime != null ? npcRuntime.NpcName : "NPC desconhecido";

        string normalActionLogText = npcRuntime != null
            ? npcRuntime.GetConfiguredNormalActionLogText(action)
            : action?.normalActionLogText;
        if (string.IsNullOrEmpty(normalActionLogText) == false)
        {
            return $"{actorName} {normalActionLogText}";
        }

        return $"{actorName} realizou {GetActionName(npcRuntime, action)}.";
    }

    private string GetActionName(NpcRuntime npcRuntime, NpcActionData action)
    {
        if (action == null)
        {
            return "acao desconhecida";
        }

        string actionName = npcRuntime != null ? npcRuntime.GetConfiguredActionName(action) : action.actionName;
        return string.IsNullOrEmpty(actionName) == false
            ? actionName
            : (npcRuntime != null ? npcRuntime.GetConfiguredActionType(action) : action.actionType).ToString();
    }
}
