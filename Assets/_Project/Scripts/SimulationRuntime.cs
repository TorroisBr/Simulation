using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public enum SimulationRuntimeAdvanceFailure
{
    None = 0,
    RuntimeFaulted = 1,
    InvalidDayCount = 2,
    AbsoluteDayOverflow = 3,
    AdvanceAlreadyInProgress = 4,
    TemporalAdvanceFailed = 5
}

public enum SimulationRuntimeAdmissionProfile
{
    None = 0,
    UnityBootstrapDailyV1 = 1
}

/// <summary>Explicit non-P12 domain composition used only by bounded proving profiles.</summary>
public enum SimulationRuntimeCompositionProfile
{
    Standard = 0,
    P15AProvingStructure = 1,
    P16AOneHopMilitary = 2,
    P17AWithdrawalWar = 3
}

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

/// <summary>Explicit profile and Unity Start-thread identity for the bounded P12 daily adapter.</summary>
public sealed class SimulationRuntimeAdmissionContext
{
    public SimulationRuntimeAdmissionProfile Profile { get; }
    internal Thread ExpectedOwnerThread { get; }
    internal int ExpectedOwnerThreadId { get; }

    public SimulationRuntimeAdmissionContext(
        SimulationRuntimeAdmissionProfile profile,
        Thread expectedOwnerThread,
        int expectedOwnerThreadId)
    {
        if (profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1)
            throw new ArgumentOutOfRangeException(nameof(profile));
        if (expectedOwnerThread == null)
            throw new ArgumentNullException(nameof(expectedOwnerThread));
        if (expectedOwnerThreadId <= 0
            || expectedOwnerThread.ManagedThreadId != expectedOwnerThreadId)
            throw new ArgumentException(
                "The expected managed thread id must match the captured Unity Start thread.",
                nameof(expectedOwnerThreadId));

        Profile = profile;
        ExpectedOwnerThread = expectedOwnerThread;
        ExpectedOwnerThreadId = expectedOwnerThreadId;
    }

    public static SimulationRuntimeAdmissionContext CaptureUnityBootstrapDailyV1()
    {
        Thread ownerThread = Thread.CurrentThread;
        return new SimulationRuntimeAdmissionContext(
            SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1,
            ownerThread,
            ownerThread.ManagedThreadId);
    }

    internal bool IsOwnedByCurrentThread()
    {
        return ReferenceEquals(ExpectedOwnerThread, Thread.CurrentThread)
            && ExpectedOwnerThreadId == Thread.CurrentThread.ManagedThreadId;
    }
}

/// <summary>Adapts the spatial authority's passage child to the P8-C transit resolver seam.</summary>
internal sealed class SpatialPassageTraversalOptionResolver : ISpatialTraversalOptionResolver
{
    private readonly SpatialPassageAuthority passageAuthority;

    public SpatialPassageTraversalOptionResolver(SpatialPassageAuthority passageAuthority)
    {
        this.passageAuthority = passageAuthority ?? throw new ArgumentNullException(nameof(passageAuthority));
    }

    public bool TryResolveTraversalOption(TraversalOptionRef option, HexBoundaryKey boundary, out string failure)
    {
        if (option == null || boundary == null)
        {
            failure = "Traversal option and boundary are required.";
            return false;
        }

        if (!passageAuthority.TryGetTraversalOptions(
                boundary,
                out IReadOnlyList<TraversalOptionRef> options,
                out SpatialAuthorityFailure authorityFailure))
        {
            failure = authorityFailure?.Message ?? "Traversal boundary is not registered.";
            return false;
        }

        foreach (TraversalOptionRef candidate in options)
        {
            if (option.Equals(candidate))
            {
                failure = string.Empty;
                return true;
            }
        }

        failure = "Traversal option is not registered on the supplied boundary.";
        return false;
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

public sealed partial class SimulationRuntime : IFactualReadRuntimeState
{
    private const string NpcMembershipCensusOperationId = "runtime.npc-membership";
    private const string BootstrapPublicationCensusOperationId = "runtime.bootstrap-publication";
    private const string DailyAdvanceCensusOperationId = "runtime.advance-day";
    private const string SoloTravelStartCensusOperationId = "runtime.travel.start";
    private const string TravelPartyAdvanceCensusOperationId = "runtime.travel-party.advance";
    private const string NpcTradeCensusOperationId = "runtime.economy.npc-trade";
    private const string NpcMoneyTransferCensusOperationId = "runtime.economy.money-transfer";
    private const string MarketPurchaseCensusOperationId = "runtime.economy.market-purchase";
    private const string MarketSaleCensusOperationId = "runtime.economy.market-sale";
    private const string MerchantDailyNpcTradeCensusOperationId = "runtime.merchant.advance-npc-trade-state";

    private sealed class NpcMembershipCensusContext
    {
        public readonly long PersonStoreRevisionAtStart;
        public readonly Thread OwnerThread;
        public readonly int OwnerManagedThreadId;
        public SimulationOperationScope ProtocolScope;
        public int NestingDepth;
        public long PersonStoreRevisionDelta;
        public bool RosterChanged;
        public readonly HashSet<string> ChangedCityPresenceSectionIds = new HashSet<string>(StringComparer.Ordinal);

        public NpcMembershipCensusContext(long personStoreRevisionAtStart, Thread ownerThread)
        {
            PersonStoreRevisionAtStart = personStoreRevisionAtStart;
            OwnerThread = ownerThread ?? throw new ArgumentNullException(nameof(ownerThread));
            OwnerManagedThreadId = ownerThread.ManagedThreadId;
            NestingDepth = 1;
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

    private readonly AuthoritativeMutationGuard mutationGuard = new AuthoritativeMutationGuard();
    private bool advanceLeaseHeld;
    private readonly SimulationRuntimeAdmissionContext runtimeAdmissionContext;
    private readonly SimulationRuntimeCompositionProfile compositionProfile;
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
    private volatile bool factualReadWorldPublished;
    private readonly SimulationTime simulationTime;
    private readonly List<CityRuntime> cities;
    private readonly List<NpcRuntime> npcRuntimes;
    private readonly IReadOnlyList<NpcRuntime> npcRuntimeSnapshot;
    private readonly Dictionary<string, NpcRuntime> npcRegistryById;
    private readonly EffectiveSimulationConfiguration configuration;
    private readonly SimulationCalendar calendar;
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
    private readonly Dictionary<MerchantTradePlanRuntime, P12NpcOwnerMutationBinding> npcMerchantPlanMutationBindings =
        new Dictionary<MerchantTradePlanRuntime, P12NpcOwnerMutationBinding>();
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
    private readonly AdventureExpeditionAutonomySystem adventureExpeditionAutonomySystem;
    private readonly SimulationLogger logger;

    public SimulationTime SimulationTime => simulationTime;
    public AuthoritativeMutationHealth MutationHealth => mutationGuard.Health;
    public bool IsMutationFaulted => mutationGuard.Health == AuthoritativeMutationHealth.Faulted;
    public AuthoritativeMutationFaultReason MutationFaultReason => mutationGuard.FaultReason;
    internal FactualReadCoordinator FactualReads => factualReadCoordinator;
    public long CurrentDay => p18dTimeline != null
        ? p18dTimeline.CurrentInstant.AbsoluteDay
        : simulationTime.AbsoluteDay;
    public IReadOnlyList<CityRuntime> Cities => cities;
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
    public WorldId WorldId { get; }

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
        P17AScenarioAuthorityCapability p17AScenarioAuthorityCapability = null)
    {
        WorldId = worldId;
        if (!Enum.IsDefined(typeof(SimulationRuntimeCompositionProfile), compositionProfile))
            throw new ArgumentOutOfRangeException(nameof(compositionProfile));
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

        this.runtimeAdmissionContext = runtimeAdmissionContext;
        this.runtimeIdAllocator = runtimeIdAllocator;
        if (runtimeAdmissionContext != null && runtimeIdAllocator != null)
        {
            runtimeIdAllocatorEventCounterCensusProvider =
                RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(runtimeIdAllocator);
            runtimeIdAllocatorDecisionCounterCensusProvider =
                RuntimeIdAllocatorCensusProvider.CreateDecisionCounterProvider(runtimeIdAllocator);
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

        this.simulationTime = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
        initialAbsoluteDay = this.simulationTime.AbsoluteDay;
        if (this.simulationTime.CanBindMutationGuard(mutationGuard) == false)
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
        SpatialAuthorityStore resolvedSpatialAuthorityStore = CloneSpatialAuthorityStore(spatialAuthorityStore);
        StructureStore resolvedStructureStore = structureStore == null
            ? null
            : structureStore.Clone(resolvedSpatialAuthorityStore);
        ISpatialTraversalOptionResolver resolvedTraversalOptionResolver =
            new SpatialPassageTraversalOptionResolver(resolvedSpatialAuthorityStore.PassageAuthority);
        LegacySpatialAnchorBindingStore resolvedLegacySpatialAnchorBindingStore =
            CloneLegacySpatialAnchorBindingStore(
                legacySpatialAnchorBindingStore,
                resolvedSpatialAuthorityStore,
                resolvedCities,
                explorableSiteStore);
        PersonSpatialPositionStore resolvedPersonSpatialPositionStore =
            ClonePersonSpatialPositionStore(
                personSpatialPositionStore,
                resolvedPersonStore,
                resolvedSpatialAuthorityStore,
                resolvedTraversalOptionResolver);
        SpatialRouteKnowledgeStore resolvedSpatialRouteKnowledgeStore =
            CloneSpatialRouteKnowledgeStore(
                spatialRouteKnowledgeStore,
                resolvedPersonStore,
                this.simulationTime.AbsoluteDay);
        PersonRoutePlanStore resolvedPersonRoutePlanStore =
            ClonePersonRoutePlanStore(
                personRoutePlanStore,
                resolvedPersonStore,
                resolvedSpatialRouteKnowledgeStore,
                actor => resolvedPersonSpatialPositionStore.TryGetPosition(actor, out PersonSpatialPosition currentPosition)
                    && currentPosition.IsInTransit,
                () => this.simulationTime.AbsoluteDay,
                this.simulationTime.AbsoluteDay);
        SpatialRoutePlanningSystem resolvedSpatialRoutePlanningSystem =
            new SpatialRoutePlanningSystem(
                resolvedPersonStore,
                resolvedSpatialAuthorityStore,
                resolvedSpatialRouteKnowledgeStore);
        InstitutionStore resolvedInstitutionStore = ResolveInstitutionStore(
            institutionStore,
            officeStore);
        OfficeStore resolvedOfficeStore = CloneOfficeStore(
            officeStore,
            resolvedInstitutionStore,
            resolvedPersonStore);
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
            ?? armedForceSpatialStateStore?.LocalTopologyStore;
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

        this.configuration = resolvedConfiguration;
        this.calendar = new SimulationCalendar(
            calendarDefinition ?? CalendarDefinition.CreateDefault());
        this.naturalMortalitySamples = naturalMortalitySamples;
        this.aggregateDemographyProvider = aggregateDemographyProvider;
        this.personStore = resolvedPersonStore;
        this.personStoreCensusProviders = PersonStoreCensusProvider.CreateProviders(resolvedPersonStore);
        this.actorChoiceStore = resolvedActorChoiceStore;
        this.spatialAuthorityStore = resolvedSpatialAuthorityStore;
        this.compositionProfile = compositionProfile;
        this.structureStore = resolvedStructureStore;
        this.legacySpatialAnchorBindingStore = resolvedLegacySpatialAnchorBindingStore;
        this.personSpatialPositionStore = resolvedPersonSpatialPositionStore;
        this.spatialRouteKnowledgeStore = resolvedSpatialRouteKnowledgeStore;
        this.personRoutePlanStore = resolvedPersonRoutePlanStore;
        p8eTravelTransactionCoordinator = new P8ETravelTransactionCoordinator(
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
        this.genealogyStore = CloneGenealogyStore(resolvedGenealogyStore);
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
            simulationTime.AbsoluteDay);
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
        this.crimeSocialAppraisalWorldState = new CrimeSocialAppraisalWorldState(
            this.personStore,
            this.institutionStore,
            this.simulationTime);
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
        List<CityRuntime> materialFlowCities = resolvedCities.FindAll(city => city != null && city.HasLocalDailyMaterialFlow);
        if (materialFlowCities.Count > 1)
            throw new LocalDailyMaterialFlowRejectedException("P14-A supports exactly one authored settlement per composed world.");
        HashSet<string> settlementIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (CityRuntime city in materialFlowCities)
        {
            if (!settlementIds.Add(city.CityData.settlementSemanticId))
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

        this.manpowerSourceConsequencePlanningService = new ManpowerSourceConsequencePlanningService(
            this,
            this.settlementManpowerSourceRegistry,
            this.contingentManpowerStateStore.SourceProvider);
        this.battleDirectConsequencePlanningService = new BattleDirectConsequencePlanningService(
            this,
            this.battleDirectConsequencePolicy);

        BindCoreMutationGuardAuthorities();
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
            if (npcRosterCensusProtocol == null
                || !simulationTime.TryBindRuntimeAdvanceDispatcher(TryAdvanceFromRuntimeOwnedClock))
            {
                npcRosterCensusProtocol?.FaultClosed();
                throw new InvalidOperationException(
                    "The P12 runtime-admission adapter could not bind to its initialized census protocol and clock.");
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

    bool IFactualReadRuntimeState.IsWorldPublished => factualReadWorldPublished;

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
            || factualReadWorldPublished
            || !runtimeAdmissionContext.IsOwnedByCurrentThread()
            || !((IFactualReadRuntimeState)this).IsHealthy
            || ((IFactualReadRuntimeState)this).IsBootstrapOrAdvanceActive
            || !((IFactualReadRuntimeState)this).TryReadCompletedLogicalBoundary(out _))
        {
            return false;
        }

        factualReadWorldPublished = true;
        return true;
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
        if (!TryAcquireAdvanceLease(out AdvanceLease lease))
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
        if (!TryAcquireAdvanceLease(out AdvanceLease lease))
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
        if (!TryAcquireAdvanceLease(out AdvanceLease lease))
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
        if (!TryAcquireAdvanceLease(out AdvanceLease lease))
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

    private void InitializeNpcRosterCensusProtocol()
    {
        ContinuationCensusProtocol protocol = new ContinuationCensusProtocol();
        npcRosterCensusProtocol = protocol;

        OwnerSectionContract personMembership = new OwnerSectionContract(
            PersonMembershipCensusProvider.SectionId,
            PersonMembershipCensusProvider.SchemaVersion,
            OwnerSectionRole.Required);
        OwnerSectionContract personBindings = new OwnerSectionContract(
            PersonMaterializationBindingCensusProvider.SectionId,
            PersonMaterializationBindingCensusProvider.SchemaVersion,
            OwnerSectionRole.Required);
        if (!protocol.RegisterExpectedSection(personMembership, out _)
            || !protocol.RegisterExpectedSection(personBindings, out _)
            || !protocol.RegisterCensusProvider(
                PersonMembershipCensusProvider.SectionId,
                personStoreCensusProviders[0],
                out _)
            || !protocol.RegisterCensusProvider(
                PersonMaterializationBindingCensusProvider.SectionId,
                personStoreCensusProviders[1],
                out _)
            || !protocol.RegisterSpatialKnowledgeRosterFamily(npcRuntimeSnapshot, out _)
            || !protocol.RegisterNpcTravelStateRosterFamily(npcRuntimeSnapshot, out _)
            || !protocol.RegisterInventoryRosterFamily(npcRuntimeSnapshot, out _)
            || !protocol.RegisterMoneyAccountRosterFamily(npcRuntimeSnapshot, out _)
            || !protocol.RegisterNpcKnowledgeRosterFamily(npcRuntimeSnapshot, out _)
            || !protocol.RegisterNpcPlanRosterFamily(npcRuntimeSnapshot, out _)
            || (runtimeAdmissionContext != null
                && !TryRegisterSimulationRecordSequenceCensusProvider(protocol))
            || (runtimeAdmissionContext != null && runtimeIdAllocator != null
                && !TryRegisterRuntimeIdAllocatorEventCounterCensusProvider(protocol))
            || (runtimeAdmissionContext != null && runtimeIdAllocator != null
                && !TryRegisterRuntimeIdAllocatorDecisionCounterCensusProvider(protocol))
            || (runtimeAdmissionContext != null && scheduledDirectiveSystem != null
                && !TryRegisterScheduledDirectiveCensusProvider(protocol))
            || (runtimeAdmissionContext != null
                && !TryRegisterActorChoiceP11CensusProvider(protocol))
            || (runtimeAdmissionContext != null
                && !TryRegisterTravelPartyCensusProvider(protocol))
            || (runtimeAdmissionContext != null
                && !TryRegisterCityNpcPresenceCensusProviders(protocol))
            || (runtimeAdmissionContext != null && !TryRegisterCityMarketCensusProviders(protocol))
            || !protocol.SealExpectedSectionInventory(out _)
            || !protocol.SealCensusProviderInventory(out _))
        {
            npcRosterCensusProtocol = null;
            return;
        }

        bool operationsRegistered = protocol.RegisterExpectedOperation(
            NpcMembershipCensusOperationId,
            out _);
        if (operationsRegistered && runtimeAdmissionContext != null)
        {
            operationsRegistered = protocol.RegisterExpectedOperation(
                    BootstrapPublicationCensusOperationId,
                    out _)
                && protocol.RegisterExpectedOperation(
                    DailyAdvanceCensusOperationId,
                    out _)
                && (travelPartySystem == null
                    || protocol.RegisterExpectedOperation(
                        TravelPartyAdvanceCensusOperationId,
                        out _))
                && (travelSystem == null
                    || protocol.RegisterExpectedOperation(
                        SoloTravelStartCensusOperationId,
                        out _))
                && protocol.RegisterExpectedOperation(
                    NpcTradeCensusOperationId,
                    out _)
                && protocol.RegisterExpectedOperation(
                    NpcMoneyTransferCensusOperationId,
                    out _)
                && protocol.RegisterExpectedOperation(
                    MarketPurchaseCensusOperationId,
                    out _)
                && protocol.RegisterExpectedOperation(
                    MarketSaleCensusOperationId,
                    out _)
                && protocol.RegisterExpectedOperation(
                    MerchantDailyNpcTradeCensusOperationId,
                    out _);
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
            npcRosterCensusProtocol = null;
            return;
        }

        if (!protocol.TryAssessOwnerSectionInventory(out _))
        {
            npcRosterCensusProtocol = null;
            return;
        }

        if (runtimeAdmissionContext != null)
        {
            try
            {
                simulationRecordSequence.BindP12MutationBoundary(
                    CanCommitP12SimulationRecordSequenceMutation,
                    NotifyP12SimulationRecordSequenceMutation);

                if (runtimeIdAllocatorEventCounterCensusProvider != null)
                {
                    runtimeIdAllocator.BindP12EventIdMutationBoundary(
                        CanCommitP12RuntimeIdEventCounterMutation,
                        NotifyP12RuntimeIdEventCounterMutation);
                }

                if (runtimeIdAllocatorDecisionCounterCensusProvider != null)
                {
                    runtimeIdAllocator.BindP12DecisionIdMutationBoundary(
                        CanCommitP12RuntimeIdDecisionCounterMutation,
                        NotifyP12RuntimeIdDecisionCounterMutation);
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
            catch
            {
                protocol.FaultClosed();
                npcRosterCensusProtocol = null;
            }
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
                || !ReferenceEquals(witness.OwnerInstanceIdentity, simulationRecordSequence.CensusOwnerIdentity)
                || witness.Revision != simulationRecordSequence.CensusRevision
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
                || !ReferenceEquals(witness.OwnerInstanceIdentity, runtimeIdAllocator.CensusOwnerIdentity)
                || witness.Revision != runtimeIdAllocator.GetCensusRevision(RuntimeIdAllocatorCensusCounter.Events)
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
                || !ReferenceEquals(witness.OwnerInstanceIdentity, runtimeIdAllocator.CensusOwnerIdentity)
                || witness.Revision != runtimeIdAllocator.GetCensusRevision(RuntimeIdAllocatorCensusCounter.Decisions)
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
            for (int i = 0; i < providers.Count; i++)
            {
                IOwnerSectionCensusProvider provider = providers[i];
                OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
                if (!(witness.OwnerInstanceIdentity is MarketRuntime market)
                    || string.IsNullOrWhiteSpace(witness.SectionId)
                    || marketSectionIdsByOwner.ContainsKey(market)
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

                marketSectionIdsByOwner.Add(market, witness.SectionId);
            }

            return true;
        }
        catch
        {
            protocol.FaultClosed();
            return false;
        }
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
                if (witness == null
                    || !(witness.OwnerInstanceIdentity is CityRuntime city)
                    || !cities.Contains(city)
                    || !owners.Add(city)
                    || witness.SchemaVersion != CityNpcPresenceCensusProvider.SchemaVersion
                    || !string.Equals(
                        witness.SectionId,
                        CityNpcPresenceCensusProvider.SectionIdFor(city.RuntimeId),
                        StringComparison.Ordinal)
                    || witness.Cardinality != city.ImportantNpcs.Count
                    || witness.Revision != city.ImportantNpcRevision
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
                    && provider.PlanOwner is MerchantTradePlanRuntime merchantPlan
                    && ReferenceEquals(registeredNpc.ExistingMerchantTradePlan, merchantPlan))
                    AddP12NpcOwnerMutationSection(merchantPlanBindings, merchantPlan, witness.SectionId);
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
                ((MoneyAccountRuntime)binding.Owner).BindP12MutationBoundary(binding.Admission, binding.Committed);
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
        BindP12MerchantOwnerMutationBoundaries(
            merchantPlanBindings,
            npcMerchantPlanMutationBindings,
            (binding, admission, committed) => ((MerchantTradePlanRuntime)binding.Owner)
                .BindP12MutationBoundary(admission, committed));
        BindP12MerchantOwnerMutationBoundaries(
            travelPlanBindings,
            npcTravelPlanMutationBindings,
            (binding, admission, committed) => ((NpcTravelPlanRuntime)binding.Owner)
                .BindP12MutationBoundary(admission, committed));
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
            if (pair.Key.UnbindP12MutationBoundary(pair.Value.Admission, pair.Value.Committed))
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
        foreach (KeyValuePair<MerchantTradePlanRuntime, P12NpcOwnerMutationBinding> pair in
            new List<KeyValuePair<MerchantTradePlanRuntime, P12NpcOwnerMutationBinding>>(npcMerchantPlanMutationBindings))
        {
            if (pair.Key.UnbindP12MutationBoundary(pair.Value.Admission, pair.Value.Committed))
                npcMerchantPlanMutationBindings.Remove(pair.Key);
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
        int activeBatchContexts = (travelContext != null ? 1 : 0)
            + (merchantContext != null ? 1 : 0)
            + (soloTravelContext != null ? 1 : 0);
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
        int activeBatchContexts = (travelContext != null ? 1 : 0)
            + (merchantContext != null ? 1 : 0)
            + (soloTravelContext != null ? 1 : 0);
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
                || !ReferenceEquals(eventCounterWitness.OwnerInstanceIdentity, runtimeIdAllocator.CensusOwnerIdentity)
                || eventCounterWitness.Revision
                    != runtimeIdAllocator.GetCensusRevision(RuntimeIdAllocatorCensusCounter.Events))
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
                || !ReferenceEquals(sequenceWitness.OwnerInstanceIdentity, simulationRecordSequence.CensusOwnerIdentity)
                || sequenceWitness.Revision != simulationRecordSequence.CensusRevision)
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
                SoloTravelStartCensusOperationId,
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
                if (witness == null
                    || !(witness.OwnerInstanceIdentity is CityRuntime city)
                    || !cities.Contains(city)
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
                TravelPartyAdvanceCensusOperationId,
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
            || !npcMerchantPlanMutationBindings.TryGetValue(npc.ExistingMerchantTradePlan, out P12NpcOwnerMutationBinding merchantPlanBinding)
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
            || !TryEnterRuntimeAdmissionOperation(MerchantDailyNpcTradeCensusOperationId, out SimulationOperationScope protocolScope))
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
            BootstrapPublicationCensusOperationId,
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
            ? MarketPurchaseCensusOperationId
            : transactionType == EconomyTransactionType.OpenMarketSale
                ? MarketSaleCensusOperationId
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

        return TryEnterRuntimeAdmissionOperation(NpcTradeCensusOperationId, out scope);
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

        return TryEnterRuntimeAdmissionOperation(NpcMoneyTransferCensusOperationId, out scope);
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

    private NpcMembershipCensusScope BeginNpcMembershipCensusScope()
    {
        NpcMembershipCensusContext activeContext = activeNpcMembershipCensusContext;
        if (activeContext != null)
        {
            if (!activeContext.IsOwnedByCurrentThread())
            {
                FaultNpcMembershipCensusBoundary();
                return new NpcMembershipCensusScope(this, null);
            }

            activeContext.NestingDepth++;
            return new NpcMembershipCensusScope(this, activeContext);
        }

        NpcMembershipCensusContext context = new NpcMembershipCensusContext(
            personStore.Revision,
            Thread.CurrentThread);
        if (npcRosterCensusProtocol != null
            && npcRosterCensusProtocol.TryEnterOperation(
                NpcMembershipCensusOperationId,
                out SimulationOperationScope protocolScope,
                out _))
        {
            context.ProtocolScope = protocolScope;
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
        }
        else if (context.RosterChanged
            || context.PersonStoreRevisionDelta != 0L
            || context.ChangedCityPresenceSectionIds.Count != 0)
        {
            List<string> changedFixedSections = new List<string>();
            if (context.PersonStoreRevisionDelta != 0L)
            {
                changedFixedSections.Add(PersonMembershipCensusProvider.SectionId);
                changedFixedSections.Add(PersonMaterializationBindingCensusProvider.SectionId);
            }
            changedFixedSections.AddRange(context.ChangedCityPresenceSectionIds);

            if (context.ProtocolScope != null && npcRosterCensusProtocol != null)
            {
                bool reconciled = npcRosterCensusProtocol.TryReconcileSpatialKnowledgeRosterAndNotifyCommittedMutations(
                    changedFixedSections,
                    context.PersonStoreRevisionAtStart,
                    out _);
                if (!reconciled)
                {
                    npcRosterCensusProtocol.FaultClosed();
                }
                else if (context.RosterChanged
                    && runtimeAdmissionContext != null
                    && !TryRebindNpcOwnerMutationBoundaries())
                {
                    npcRosterCensusProtocol.FaultClosed();
                }
            }
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
        AddRequiredMutationGuardBinding(authorities, simulationTime, nameof(SimulationTime));
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
        AddRequiredMutationGuardBinding(authorities, genealogyStore, nameof(GenealogyStore));
        AddRequiredMutationGuardBinding(authorities, institutionStore, nameof(InstitutionStore));
        AddRequiredMutationGuardBinding(authorities, officeStore, nameof(OfficeStore));
        AddRequiredMutationGuardBinding(authorities, propertyOwnershipStore, nameof(PropertyOwnershipStore));
        AddRequiredMutationGuardBinding(authorities, estateStore, nameof(EstateStore));
        AddRequiredMutationGuardBinding(authorities, politicalClaimStore, nameof(PoliticalClaimStore));
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

        foreach (IAuthoritativeMutationGuardBindable authority in authorities)
        {
            if (authority == null || authority.CanBindMutationGuard(mutationGuard) == false)
            {
                throw new InvalidOperationException(
                    "A mutable SimulationRuntime input became bound to another runtime during composition.");
            }
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

        if (!isComposingNpcRoster && !npcRuntime.TryBindRuntimeMutationGuard(mutationGuard))
        {
            failure = WorldNpcRegistryFailure.AlreadyOwnedByAnotherRuntime;
            return false;
        }

        npcRegistryById.Add(npcRuntime.RuntimeId, npcRuntime);
        npcRuntimes.Add(npcRuntime);
        if (!isComposingNpcRoster
            && runtimeAdmissionContext != null
            && npcRuntime.CurrentCity != null
            && !cityNpcPresenceSectionIdsByOwner.ContainsKey(npcRuntime.CurrentCity))
        {
            npcRegistryById.Remove(npcRuntime.RuntimeId);
            npcRuntimes.Remove(npcRuntime);
            failure = WorldNpcRegistryFailure.InvalidNpc;
            return false;
        }
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

        if (npcRuntime.CurrentCity != null
            && !npcRuntime.SetCurrentPresence(null, null))
        {
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

    /// <summary>
    /// Computes living represented-resident floors at the world boundary. The
    /// aggregate population system receives the resulting snapshot explicitly.
    /// </summary>
    internal RepresentedResidentFloorSnapshot BuildRepresentedResidentFloorSnapshot()
    {
        Dictionary<string, int> floors = new Dictionary<string, int>(StringComparer.Ordinal);
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
            floors[city.RuntimeId] = presence?.RepresentedResidentCount ?? 0;
        }

        return new RepresentedResidentFloorSnapshot(floors);
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

        bool registered = personStore.TryRegister(person, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
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
        bool registered = institutionStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
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

        bool registered = factionStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
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

        bool applied = FactionAffiliationSystem.TryApplyAdd(factionStore, transition, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
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

        bool applied = FactionAffiliationSystem.TryApplyEnd(factionStore, transition, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
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

        bool registered = politicalClaimStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
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

        return PoliticalClaimSystem.TryProposeRecognition(
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

        bool applied = PoliticalClaimSystem.TryApplyRecognition(
            politicalClaimStore,
            transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

    public bool TryProposePoliticalClaimResolution(
        PoliticalClaimId claimId,
        PoliticalClaimStatus status,
        out PoliticalClaimResolutionTransition transition,
        out PoliticalClaimFailure failure)
    {
        return PoliticalClaimSystem.TryProposeResolution(
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

        bool applied = PoliticalClaimSystem.TryApplyResolution(
            politicalClaimStore,
            transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
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

        bool registered = politicalSupportStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
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
        bool applied = politicalSupportStore.TryApplyAdd(transition, CurrentDay, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
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
        bool applied = politicalSupportStore.TryApplyEnd(transition, CurrentDay, out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
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

        bool registered = propertyOwnershipStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
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
        bool applied = EstateOpeningSystem.TryApplyOpening(
            personStore,
            estateStore,
            transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
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

        bool applied = PropertyTransferSystem.TryApplyTransfer(
            personStore,
            propertyOwnershipStore,
            transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
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
        return EstateSuccessionSystem.TryApply(this, transition, out failure);
    }

    public bool TryRegisterOffice(
        OfficeRecord record,
        out InstitutionFoundationFailure failure)
    {
        bool registered = officeStore.TryRegister(record, out failure);
        if (registered)
        {
            AdvancePoliticalWorldRevision();
        }

        return registered;
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
        return officeStore.TryGetLatestClosedTenure(officeId, out tenure);
    }

    public bool TryAssignIncumbent(
        OfficeId officeId,
        PersonId incumbent,
        long? startAbsoluteDay,
        out InstitutionFoundationFailure failure)
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
        }

        return assigned;
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
        bool vacated = officeStore.TryVacateOffice(officeId, out failure);
        if (vacated)
        {
            AdvancePoliticalWorldRevision();
        }

        return vacated;
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
        bool applied = InstitutionalVacancyRecognitionSystem.TryApply(
            officeStore,
            transition,
            out failure);
        if (applied)
        {
            AdvancePoliticalWorldRevision();
        }

        return applied;
    }

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
        bool added = PersonGenealogySystem.TryAddParentage(this, parentId, childId, out failure);
        if (added)
        {
            AdvancePoliticalWorldRevision();
        }

        return added;
    }

    public bool TryRemoveParentage(
        PersonId parentId,
        PersonId childId,
        out PersonGenealogyFailure failure)
    {
        bool removed = PersonGenealogySystem.TryRemoveParentage(this, parentId, childId, out failure);
        if (removed)
        {
            AdvancePoliticalWorldRevision();
        }

        return removed;
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
        using (NpcMembershipCensusScope censusScope = BeginNpcMembershipCensusScope())
        {
            return PersonMaterializationSystem.TryMaterializePerson(
                this,
                personId,
                npcData,
                runtimeId,
                startingCity,
                initialMoney,
                out npcRuntime,
                out failure);
        }
    }

    public bool TryBindExistingNpcToPerson(
        PersonId personId,
        string npcRuntimeId,
        out PersonMaterializationFailure failure)
    {
        using (NpcMembershipCensusScope censusScope = BeginNpcMembershipCensusScope())
        {
            return PersonMaterializationSystem.TryBindExistingNpcToPerson(
                this,
                personId,
                npcRuntimeId,
                out failure);
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

    public bool TryApplyResidentDeath(
        NpcRuntime npcRuntime,
        CityRuntime settlement,
        out NpcPopulationLifecycleTransition transition,
        out NpcPopulationLifecycleFailure failure)
    {
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

        return travelPartySystem != null && travelPartySystem.TryStartTravelParty(context);
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
        failure = SimulationRuntimeAdvanceFailure.None;
        if (runtimeAdmissionContext != null && !IsRuntimeAdmissionOwnerThreadCurrent())
        {
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        if (TryAcquireAdvanceLease(out AdvanceLease lease) == false)
        {
            failure = SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress;
            return false;
        }

        using (lease)
        {
            if (runtimeAdmissionContext != null)
            {
                if (mutationGuard.CanMutate == false)
                {
                    failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                    return false;
                }

                if (!simulationTime.TryValidateAdvance(out SimulationTimeAdvanceFailure timeFailure))
                {
                    failure = timeFailure == SimulationTimeAdvanceFailure.AbsoluteDayOverflow
                        ? SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow
                        : SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                    return false;
                }

                if (!TryEnterRuntimeAdmissionOperation(
                        DailyAdvanceCensusOperationId,
                        out SimulationOperationScope operationScope))
                {
                    failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                    return false;
                }

                using (operationScope)
                {
                    try
                    {
                        return TryAdvanceDayCore(out failure);
                    }
                    catch
                    {
                        FaultRuntimeAdmission();
                        throw;
                    }
                }
            }

            if (p18dTimeline != null)
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

                return TryAdvanceP18DIntradayToCore(target, out failure);
            }

            return TryAdvanceDayCore(out failure);
        }
    }

    private bool TryAdvanceDayCore(out SimulationRuntimeAdvanceFailure failure)
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

        if (simulationTime.TryAdvanceDayFromRuntime(out SimulationTimeAdvanceFailure timeFailure) == false)
        {
            failure = timeFailure == SimulationTimeAdvanceFailure.RuntimeFaulted
                ? SimulationRuntimeAdvanceFailure.RuntimeFaulted
                : SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow;
            return false;
        }

        AdvanceDayAfterClockAdvance();
        return true;
    }

    /// <summary>
    /// Acquires this runtime's single-writer advance lease for a larger composed
    /// chronological operation. This is reentrancy protection, not a thread lock.
    /// </summary>
    internal bool TryAcquireAdvanceLease(out AdvanceLease lease)
    {
        lease = null;
        if (advanceLeaseHeld)
        {
            return false;
        }

        lease = new AdvanceLease(this);
        advanceLeaseHeld = true;
        return true;
    }

    private void ReleaseAdvanceLease()
    {
        advanceLeaseHeld = false;
    }

    internal sealed class AdvanceLease : IDisposable
    {
        private SimulationRuntime owner;

        internal AdvanceLease(SimulationRuntime owner)
        {
            this.owner = owner;
        }

        public void Dispose()
        {
            SimulationRuntime currentOwner = owner;
            owner = null;
            currentOwner?.ReleaseAdvanceLease();
        }
    }

    private void AdvanceDayAfterClockAdvance()
    {
        placeContentStore?.AdvanceDays(1);
        logger?.BeginDay(CurrentDay);
        lastDailyDemographyReport = DailyDemographicSystem.Advance(
            this,
            naturalMortalitySamples,
            aggregateDemographyProvider);
        BeginSimulationDay();
        scheduledDirectiveSystem?.PrepareDay(CurrentDay);

        if (configuration.Economy.Enabled == true)
        {
            SimulateEconomyDay();
        }

        RefreshLocalKnowledgeAndShare();
        adventureExpeditionAutonomySystem?.AdvanceActiveExpeditions();

        if (adventureExpeditionAutonomySystem != null)
        {
            foreach (NpcRuntime npcRuntime in npcRuntimes)
            {
                if (npcRuntime != null
                    && npcRuntime.IsAlive
                    && npcRuntime.IsTraveling == false
                    && (expeditionSystem == null || expeditionSystem.IsNpcOnActiveExpedition(npcRuntime.RuntimeId) == false))
                {
                    adventureExpeditionAutonomySystem.TryStartAutonomousExpedition(npcRuntime, npcRuntimes);
                }
            }
        }

        for (int actorTurnRosterOrdinal = 0; actorTurnRosterOrdinal < npcRuntimes.Count; actorTurnRosterOrdinal++)
        {
            NpcRuntime npcRuntime = npcRuntimes[actorTurnRosterOrdinal];
            if (npcRuntime == null)
            {
                continue;
            }

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

            if (TryDeferActorChoiceForSpatialTransit(npcRuntime, actorTurnRosterOrdinal))
            {
                continue;
            }

            if (npcRuntime.IsTraveling == true)
            {
                TryProcessScheduledDirective(npcRuntime);
                DeferActorChoiceForPerson(
                    npcRuntime.PersonId,
                    actorTurnRosterOrdinal,
                    ActorChoiceDeferralReason.Traveling);
                continue;
            }

            if (expeditionSystem != null
                && expeditionSystem.IsNpcOnActiveExpedition(npcRuntime.RuntimeId) == true)
            {
                TryProcessScheduledDirective(npcRuntime);
                DeferActorChoiceForPerson(
                    npcRuntime.PersonId,
                    actorTurnRosterOrdinal,
                    ActorChoiceDeferralReason.ExpeditionParticipant);
                continue;
            }

            if (adventureExpeditionAutonomySystem != null
                && adventureExpeditionAutonomySystem.IsReservedToday(npcRuntime.RuntimeId))
            {
                DeferActorChoiceForPerson(
                    npcRuntime.PersonId,
                    actorTurnRosterOrdinal,
                    ActorChoiceDeferralReason.ReservedExpeditionActivity);
                continue;
            }

            EvaluateStatus(npcRuntime);

            if (HasPendingActorChoiceFor(npcRuntime.PersonId))
            {
                if (TryProcessScheduledDirective(npcRuntime) == true)
                {
                    DeferActorChoiceForPerson(
                        npcRuntime.PersonId,
                        actorTurnRosterOrdinal,
                        ActorChoiceDeferralReason.ScheduledDirective);
                    continue;
                }

                if (TryProcessActorChoice(npcRuntime, actorTurnRosterOrdinal))
                {
                    continue;
                }
            }

            if (configuration.MerchantTrade.Enabled)
            {
                if (merchantSystem != null)
                {
                    if (runtimeAdmissionContext == null)
                    {
                        merchantSystem.AdvanceNpcTradeState(npcRuntime);
                    }
                    else if (!TryBeginP12MerchantDailyNpcTradeOperation(
                        npcRuntime,
                        out P12MerchantOperationScope merchantOperationScope))
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
            }

            if (TryProcessScheduledDirective(npcRuntime) == true)
            {
                DeferActorChoiceForPerson(
                    npcRuntime.PersonId,
                    actorTurnRosterOrdinal,
                    ActorChoiceDeferralReason.ScheduledDirective);
                continue;
            }

            if (TryProcessActorChoice(npcRuntime, actorTurnRosterOrdinal))
            {
                continue;
            }

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

        List<NpcRuntime> arrivedNpcs = new List<NpcRuntime>();

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
        {
            arrivedNpcs.AddRange(travelSystem.AdvanceTravels(npcRuntimes));
        }

        foreach (NpcRuntime arrivedNpc in arrivedNpcs)
        {
            ObserveArrivedExplorableSites(arrivedNpc);

            if (arrivedNpc?.CurrentCity != null)
            {
                if (configuration.MerchantTrade.Enabled)
                {
                    merchantSystem?.ObserveCurrentMarket(arrivedNpc);
                }
            }
        }

        expeditionSystem?.ReconcileAfterTravel(arrivedNpcs);
    }

    private InvalidOperationException CreateAdvanceFailureException(
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

    private bool TryProcessActorChoice(NpcRuntime npcRuntime, int actorTurnRosterOrdinal)
    {
        if (npcRuntime?.PersonId == null
            || !actorChoiceStore.TryGetNextPendingForActor(npcRuntime.PersonId, out ActorChoiceInput input))
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
        if (configuredActions != null)
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
            || requestedDefinition.actionType != NpcActionType.SellGoods
            || !IsActionEnabledForRuntime(requestedDefinition))
        {
            RejectActorChoice(input, actorTurnRosterOrdinal, ActorChoiceFailure.ActionUnavailable);
            return true;
        }

        NpcActionRuntime requestedAction = npcDecisionSystem.CreateRequestedAction(npcRuntime, requestedDefinition);
        if (requestedAction == null
            || !ReferenceEquals(requestedAction.Action, requestedDefinition)
            || requestedAction.Action.actionType != NpcActionType.SellGoods
            || requestedAction.TargetCity != npcRuntime.CurrentCity
            || requestedAction.TargetNpc != null
            || requestedAction.TargetItem == null
            || requestedAction.Amount <= 0)
        {
            RejectActorChoice(input, actorTurnRosterOrdinal, ActorChoiceFailure.ActionUnavailable);
            return true;
        }

        NpcDecisionRecord decision = decisionRecorder?.RecordChosenAction(
            npcRuntime,
            requestedAction,
            NpcDecisionOrigin.ActorChoice);
        npcRuntime.SetCurrentActionRuntime(requestedAction);

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
                    result,
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

        return source.Clone(personStore);
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

    private static PoliticalClaimStore ClonePoliticalClaimStore(
        PoliticalClaimStore source,
        PersonStore personStore,
        InstitutionStore institutionStore,
        OfficeStore officeStore,
        PropertyOwnershipStore propertyOwnershipStore,
        long currentDay)
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

            if (copy.TryRegisterRecognition(recognition, out PoliticalClaimFailure recognitionFailure) == false)
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

        return copy;
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
        OfficeStore officeStore)
    {
        if (institutionStore != null
            && officeStore != null
            && ReferenceEquals(
                institutionStore,
                officeStore.InstitutionStoreForWorldBoundary) == false)
        {
            throw new ArgumentException(
                "The SimulationRuntime InstitutionStore and OfficeStore must belong to the same store pair.",
                nameof(officeStore));
        }

        InstitutionStore source = institutionStore
            ?? officeStore?.InstitutionStoreForWorldBoundary;
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
        PersonStore personStore)
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

        foreach (OfficeTenureRecord tenure in source.TenureHistoryInMutationOrder)
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
                tenure.EndReason,
                true);
            if (copy.TryAddHistoricalTenure(clone, out InstitutionFoundationFailure failure) == false)
            {
                throw new ArgumentException(
                    "The SimulationRuntime OfficeStore contains invalid historical tenure: "
                    + failure + ".",
                    nameof(source));
            }
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
        daysAdvanced = 0;
        failure = SimulationRuntimeAdvanceFailure.None;
        if (dayCount < 0)
        {
            failure = SimulationRuntimeAdvanceFailure.InvalidDayCount;
            return false;
        }

        if (dayCount == 0)
        {
            return true;
        }

        if (runtimeAdmissionContext != null && !IsRuntimeAdmissionOwnerThreadCurrent())
        {
            failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
            return false;
        }

        if (TryAcquireAdvanceLease(out AdvanceLease lease) == false)
        {
            failure = SimulationRuntimeAdvanceFailure.AdvanceAlreadyInProgress;
            return false;
        }

        using (lease)
        {
            if (mutationGuard.CanMutate == false)
            {
                failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                return false;
            }

            if (runtimeAdmissionContext != null
                && !simulationTime.TryValidateAdvance(out SimulationTimeAdvanceFailure initialTimeFailure))
            {
                failure = initialTimeFailure == SimulationTimeAdvanceFailure.AbsoluteDayOverflow
                    ? SimulationRuntimeAdvanceFailure.AbsoluteDayOverflow
                    : SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                return false;
            }

            SimulationOperationScope operationScope = null;
            if (runtimeAdmissionContext != null
                && !TryEnterRuntimeAdmissionOperation(
                    DailyAdvanceCensusOperationId,
                    out operationScope))
            {
                failure = SimulationRuntimeAdvanceFailure.RuntimeFaulted;
                return false;
            }

            using (operationScope)
            {
                try
                {
                    for (int i = 0; i < dayCount; i++)
                    {
                        bool advanced;
                        if (p18dTimeline != null)
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

                            advanced = TryAdvanceP18DIntradayToCore(target, out failure);
                        }
                        else
                        {
                            advanced = TryAdvanceDayCore(out failure);
                        }

                        if (advanced == false)
                        {
                            return false;
                        }

                        daysAdvanced++;
                    }
                }
                catch
                {
                    if (runtimeAdmissionContext != null)
                        FaultRuntimeAdmission();
                    throw;
                }
            }
        }

        return true;
    }

    private void BeginSimulationDay()
    {
        if (justiceSystem != null)
        {
            justiceSystem.BeginDay();
        }

        // Hidden-state expiration is a previously established consequence/timer,
        // not autonomous crime origination. It must continue to advance whenever
        // the composed crime system owns the state, independently of policy that
        // filters new criminal decisions.
        crimeSystem?.AdvanceHiddenStatuses(npcRuntimes);

        if (justiceSystem != null)
        {
            justiceSystem.AdvanceSentences(npcRuntimes);
        }

        if (justiceSystem != null)
        {
            justiceSystem.SyncWantedStatuses(npcRuntimes);
        }

        AdvanceMerchantPlanUrgency();
    }

    private void AdvanceMerchantPlanUrgency()
    {
        if (configuration.MerchantTrade.Enabled == false)
        {
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

    private void RefreshLocalKnowledgeAndShare()
    {
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

        NpcActionRuntime chosenAction = npcDecisionSystem.ChooseAction(
            npcRuntime,
            GetConfiguredActionsEnabledForRuntime(),
            CurrentDay);
        decisionRecorder?.RecordChosenAction(npcRuntime, chosenAction, NpcDecisionOrigin.Autonomous);
        npcRuntime.SetCurrentActionRuntime(chosenAction);
    }

    private NpcActionResult TryExecuteCurrentAction(NpcRuntime npcRuntime)
    {
        NpcActionRuntime actionRuntime = npcRuntime.CurrentActionRuntime;
        NpcActionData action = actionRuntime != null ? actionRuntime.Action : npcRuntime.CurrentAction;

        if (action == null)
        {
            return null;
        }

        LogChosenTargetAction(npcRuntime, actionRuntime);
        NpcActionResult actionResult = TryExecuteAction(npcRuntime, actionRuntime, action);

        if (actionResult != null && string.IsNullOrEmpty(actionResult.Message) == false)
        {
            logger?.Log(SimulationLogCategory.NpcAction, actionResult.Message);
        }

        if (actionResult != null && actionResult.Success == true)
        {
            ApplySuccessStatusChanges(npcRuntime, actionRuntime, action);
        }

        return actionResult;
    }

    private bool TryProcessScheduledDirective(NpcRuntime npcRuntime)
    {
        if (scheduledDirectiveSystem == null
            || scheduledDirectiveSystem.TryTakeDirective(npcRuntime, out ScheduledDirective directive) == false)
        {
            return false;
        }

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

        return true;
    }

    private void ProcessRequestedActionDirective(NpcRuntime npcRuntime, ScheduledDirective directive)
    {
        NpcActionRuntime requestedAction = npcDecisionSystem != null
            && IsActionEnabledForRuntime(directive.Action)
            ? npcDecisionSystem.CreateRequestedAction(npcRuntime, directive.Action)
            : null;

        if (requestedAction == null)
        {
            SkipDirective(directive, "Actor is not in a compatible state for the requested action.");
            return;
        }

        decisionRecorder?.RecordChosenAction(npcRuntime, requestedAction, NpcDecisionOrigin.ScheduledDirective);
        npcRuntime.SetCurrentActionRuntime(requestedAction);
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

        if (justiceSystem.IsArrested(npcRuntime) == false)
        {
            SkipDirective(directive, "Actor is not arrested; escape outcome is incompatible with current state.");
            return;
        }

        CrimeActionSettings settings = directive.Action != null && directive.Action.crimeSettings != null
            ? directive.Action.crimeSettings
            : new CrimeActionSettings();

        npcRuntime.SetCurrentActionRuntime(new NpcActionRuntime(directive.Action));

        if (justiceSystem.ApplyEscapeSuccess(npcRuntime, settings.escapeBountyPenalty) == false)
        {
            directive.MarkFailed(CurrentDay, "Canonical escape transition rejected the forced outcome.");
            return;
        }

        ApplySuccessStatusChanges(npcRuntime, npcRuntime.CurrentActionRuntime, directive.Action);
        directive.MarkSucceeded(CurrentDay);
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

    private NpcActionResult TryExecuteAction(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        if (IsActionEnabledForRuntime(action) == false)
        {
            return NpcActionResult.Failed();
        }

        INpcActionProvider actionProvider = action.actionType == NpcActionType.Normal
            ? null
            : npcDecisionSystem?.GetProviderForAction(action);

        if (action.actionType != NpcActionType.Normal && actionProvider == null)
        {
            return NpcActionResult.Failed();
        }

        if (runtimeAdmissionContext != null
            && travelSystem != null
            && action.actionType == NpcActionType.Travel
            && !(actionProvider is TravelActionProvider))
        {
            FaultRuntimeAdmission();
            throw new InvalidOperationException(
                "The selected P12 profile cannot admit a Travel action through an unbound provider.");
        }

        if (RollActionSuccess(npcRuntime, action, actionRuntime) == false)
        {
            if (actionProvider is INpcActionFailureHandler failureHandler)
            {
                NpcActionResult failureResult = failureHandler.HandleActionFailure(npcRuntime, actionRuntime);

                if (failureResult != null)
                {
                    return failureResult;
                }
            }

            return NpcActionResult.Failed(CreateFailureMessage(npcRuntime, actionRuntime, action));
        }

        if (action.actionType == NpcActionType.Normal)
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

        return actionProvider.TryExecuteAction(npcRuntime, actionRuntime);
    }

    private List<NpcActionData> GetConfiguredActionsEnabledForRuntime()
    {
        List<NpcActionData> enabledActions = new List<NpcActionData>();

        if (configuredActions == null)
        {
            return enabledActions;
        }

        foreach (NpcActionData action in configuredActions)
        {
            if (IsActionEnabledForRuntime(action))
            {
                enabledActions.Add(action);
            }
        }

        return enabledActions;
    }

    private bool IsActionEnabledForRuntime(NpcActionData action)
    {
        if (action == null)
        {
            return false;
        }

        switch (action.actionType)
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
        if (action == null || action.canFail == false)
        {
            return true;
        }

        float contextualMultiplier = actionRuntime != null ? actionRuntime.SuccessChanceMultiplier : 1f;
        float effectiveChance = Mathf.Clamp01(action.baseSuccessChance * Mathf.Max(0f, contextualMultiplier));
        string actorKey = npcRuntime?.RuntimeId ?? "unknown-actor";
        string decisionKey = actionRuntime?.OriginDecisionId
            ?? actionRuntime?.Action?.DefinitionId
            ?? "unbound";
        string actionKey = action?.DefinitionId ?? action?.actionType.ToString() ?? "unknown";
        float randomValue = randomSource.NextUnit(
            "action-success|" + actorKey + "|" + decisionKey + "|" + actionKey + "|" + CurrentDay);
        return randomValue <= effectiveChance;
    }

    private void ApplySuccessStatusChanges(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        ApplyStatusChanges(npcRuntime, action.statusToRemove, action.statusToAdd);

        if (actionRuntime != null && actionRuntime.TargetNpc != null)
        {
            ApplyStatusChanges(actionRuntime.TargetNpc, action.targetStatusToRemove, action.targetStatusToAdd);
        }
    }

    private void ApplyStatusChanges(NpcRuntime npcRuntime, List<NpcStatusData> statusToRemove, List<NpcStatusData> statusToAdd)
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
            logger?.Log(SimulationLogCategory.NpcAction, $"{npcRuntime.NpcName} escolheu {GetActionName(actionRuntime.Action)} {actionRuntime.TargetNpc.NpcName}.");
        }
        else if (actionRuntime.TargetCity != null)
        {
            logger?.Log(SimulationLogCategory.NpcAction, $"{npcRuntime.NpcName} escolheu {GetActionName(actionRuntime.Action)} {actionRuntime.TargetCity.CityName}.");
        }
    }

    private string CreateFailureMessage(NpcRuntime npcRuntime, NpcActionRuntime actionRuntime, NpcActionData action)
    {
        string actorName = npcRuntime != null ? npcRuntime.NpcName : "NPC desconhecido";
        string targetName = actionRuntime != null && actionRuntime.TargetNpc != null ? $" {actionRuntime.TargetNpc.NpcName}" : string.Empty;
        string targetCityName = actionRuntime != null && actionRuntime.TargetCity != null ? $" {actionRuntime.TargetCity.CityName}" : string.Empty;
        return $"{actorName} tentou {GetActionName(action)}{targetName}{targetCityName}, mas falhou.";
    }

    private string CreateNormalActionMessage(NpcRuntime npcRuntime, NpcActionData action)
    {
        string actorName = npcRuntime != null ? npcRuntime.NpcName : "NPC desconhecido";

        if (action != null && string.IsNullOrEmpty(action.normalActionLogText) == false)
        {
            return $"{actorName} {action.normalActionLogText}";
        }

        return $"{actorName} realizou {GetActionName(action)}.";
    }

    private string GetActionName(NpcActionData action)
    {
        if (action == null)
        {
            return "acao desconhecida";
        }

        return string.IsNullOrEmpty(action.actionName) == false ? action.actionName : action.actionType.ToString();
    }
}
