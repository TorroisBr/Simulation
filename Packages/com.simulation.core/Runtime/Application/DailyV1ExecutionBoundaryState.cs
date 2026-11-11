using System;
using System.Collections.Generic;
using System.Threading;

internal sealed class DailyV1OwnerSectionContractIdentity
{
    internal string SectionId { get; }
    internal int SchemaVersion { get; }
    internal OwnerSectionRole Role { get; }

    internal DailyV1OwnerSectionContractIdentity(string sectionId, int schemaVersion, OwnerSectionRole role)
    {
        SectionId = sectionId;
        SchemaVersion = schemaVersion;
        Role = role;
    }
}


internal enum DailyV1OperationScopeKind : byte
{
    Application = 0,
    RuntimeProtocol = 1,
    ContinuationSave = 2,
    DailyAdvance = 3
}

internal enum DailyV1CaptureScopeAllowance : byte
{
    None = 0,
    ContinuationSave = 1,
    DailyAdvance = 2,
    SessionReplacementValidation = 3
}

/// <summary>
/// Result returned by the composition-bound owner census source. The Core
/// execution state selects the only permitted capture scope and validates that
/// the returned epoch still matches its own mutation authority.
/// </summary>
internal sealed class DailyV1OwnerSectionCensusCaptureResult
{
    internal IReadOnlyList<OwnerSectionCensusSnapshot> OwnerSections { get; }
    internal long MutationEpoch { get; }
    internal DailyCaptureEligibilityFailure Failure { get; }
    internal string Diagnostic { get; }
    internal bool Succeeded => Failure == DailyCaptureEligibilityFailure.None && OwnerSections != null;

    private DailyV1OwnerSectionCensusCaptureResult(
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        long mutationEpoch,
        DailyCaptureEligibilityFailure failure,
        string diagnostic)
    {
        OwnerSections = ownerSections;
        MutationEpoch = mutationEpoch;
        Failure = failure;
        Diagnostic = diagnostic;
    }

    internal static DailyV1OwnerSectionCensusCaptureResult Captured(
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections, long mutationEpoch) =>
        new DailyV1OwnerSectionCensusCaptureResult(
            ownerSections, mutationEpoch, DailyCaptureEligibilityFailure.None, null);

    internal static DailyV1OwnerSectionCensusCaptureResult Rejected(
        DailyCaptureEligibilityFailure failure, string diagnostic) =>
        new DailyV1OwnerSectionCensusCaptureResult(null, 0L,
            failure == DailyCaptureEligibilityFailure.None
                ? DailyCaptureEligibilityFailure.RuntimeFaulted
                : failure,
            diagnostic);
}

internal enum DailyV1RestoredBoundaryFinalizationStage : byte
{
    MutationGuardBound = 1,
    OwnerVectorRecaptured = 2,
    BoundaryAdmitted = 3
}

internal enum DailyV1RestoredBoundaryFinalizationFailure : byte
{
    None = 0,
    StagingAuthorityUnavailable = 1,
    CandidateHostBoundaryUnavailable = 2,
    MutationGuardBindingFailed = 3,
    OwnerVectorValidationFailed = 4,
    BoundaryAdmissionFailed = 5
}

/// <summary>
/// Existing continuation-census operations required by one selected solo
/// travel start. The Core session owns the exact section set, serialized
/// operation lifetime, and committed-section receipt; the host only binds
/// those operations to its registered live census protocol.
/// </summary>
internal interface IDailyV1SelectedP12SoloTravelStartProtocol
{
    bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds);
    bool TryValidateMutationEpochCapacity();
    bool TryEnterOperation(out IDisposable operationScope);
    bool NotifyCommittedMutations(IReadOnlyCollection<string> sectionIds);
    void FaultClosed();
}

/// <summary>
/// Live continuation-census operations required by one selected TravelParty
/// start. Core owns the selected owner cut and serialized operation lifetime;
/// the host binds these calls to its already-registered census protocol.
/// </summary>
internal interface IDailyV1SelectedP12TravelPartyStartProtocol
{
    bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds);
    bool TryValidateMutationEpochCapacity();
    bool TryEnterOperation(out IDisposable operationScope);
    bool NotifyCommittedMutations(IReadOnlyCollection<string> sectionIds);
    void FaultClosed();
}

/// <summary>
/// Existing P12 census operations required for one selected Merchant daily
/// state pass. Core derives the seven-row owner cut, owns the serialized
/// operation lifetime, and publishes one union receipt when the pass ends.
/// The host confirms its exact projections and binds the calls to its already
/// registered census protocol.
/// </summary>
internal interface IDailyV1SelectedP12MerchantDailyTradeProtocol
{
    bool TryValidateOwnerSections(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IReadOnlyList<string> sectionIds);
    bool TryValidateUnchangedSections(IReadOnlyList<string> sectionIds);
    bool TryValidateMutationEpochCapacity();
    bool TryEnterOperation(out IDisposable operationScope);
    bool NotifyCommittedMutations(IReadOnlyCollection<string> sectionIds);
    void FaultClosed();
}

/// <summary>
/// Owns the per-runtime single-writer advance lease and completed Daily-v1
/// core sequence. This is execution state, not a thread lock or persisted data.
/// </summary>
internal sealed partial class DailyV1ExecutionBoundaryState
{
    private readonly object runtimeInstanceIdentity = new object();
    private readonly object operationSyncRoot = new object();
    private readonly object applicationAdvanceAuthorityGate = new object();
    private readonly List<DailyV1OperationScopeKind> activeOperationScopes =
        new List<DailyV1OperationScopeKind>();
    private Dictionary<string, DailyV1OwnerSectionContractIdentity> expectedOwnerSectionContracts =
        new Dictionary<string, DailyV1OwnerSectionContractIdentity>(StringComparer.Ordinal);
    private bool expectedOwnerSectionContractInventorySealed;
    private long expectedOwnerSectionContractInventoryRevision;
    private readonly HashSet<string> expectedRuntimeProtocolOperationContracts =
        new HashSet<string>(StringComparer.Ordinal);
    private readonly List<string> activeRuntimeProtocolOperationContracts =
        new List<string>();
    private readonly DailyV1MutationEpochState mutationEpochState =
        new DailyV1MutationEpochState();
    private readonly AuthoritativeMutationGuard mutationGuard =
        new AuthoritativeMutationGuard();
    private bool selectedCoreOwnerMutationGuardAuthoritiesBound;
    private bool coreMutationGuardAuthoritiesBound;
    private volatile bool factualReadWorldPublished;
    private bool runtimeIdentityBound;
    private bool runtimeConfigurationBound;
    private bool runtimeProtocolOperationInventorySealed;
    private bool selectedRuntimeCensusProtocolReady;
    private bool selectedRuntimeCensusProtocolFaulted;
    private bool exclusiveSessionReplacementInProgress;
    private DailyV1AdvanceLease advanceLease;
    private long advanceLeaseInitialCompletedSequence;
    private long completedCoreSequence;
    private DailyCaptureEligibilityToken currentCaptureToken;
    private bool selectedRuntimeOwnerGraphBound;
    private DailyV1SelectedRuntimeOwnerGraph selectedRuntimeOwnerGraph;
    private object selectedRuntimeOwnerCensusSourceIdentity;
    private Func<DailyV1CaptureScopeAllowance, DailyV1OwnerSectionCensusCaptureResult> selectedRuntimeOwnerCensusSource;
    private IReadOnlyList<DailyV1ActorTurnRosterProjection> selectedRuntimeActorRoster;
    private bool selectedRuntimeExecutionRootsBound;
    private RuntimeIdAllocator selectedRuntimeIdAllocator;
    private IAuthoritativeRandomSource selectedRandomSource;
    private SimulationRecordSequence selectedRecordSequence;
    private NpcDecisionStore selectedDecisionStore;
    private ActorChoiceStore selectedActorChoiceStore;
    private DomainEventRecorder selectedTravelPartyEventRecorder;
    private DomainEventRecorder selectedSoloTravelEventRecorder;
    private bool selectedTravelEventRecordersBound;
    private DailyV1ActorTurnCoreRoots selectedRuntimeActorTurnCoreRoots;
    private DailyV1P12CrimeJusticeMutationCoordinator selectedP12CrimeJusticeMutationCoordinator;
    private IDailyV1P12CrimeJusticeMutationProtocol selectedP12CrimeJusticeMutationProtocol;
    private DailyV1SelectedCrimeJusticeDailyMutationHostBinding selectedCrimeJusticeDailyMutationHostBinding;
    private SelectedP12TravelPartyAdvanceContext selectedP12TravelPartyAdvanceContext;
    private SelectedP12TravelPartyStartContext selectedP12TravelPartyStartContext;
    private SelectedP12SoloTravelStartContext selectedP12SoloTravelStartContext;
    private SelectedP12MerchantDailyTradeContext selectedP12MerchantDailyTradeContext;
    private DailyV1Application applicationAdvanceAuthority;
    private DailyV1ActiveRuntimeSession applicationAdvanceSession;

    private sealed class SelectedP12TravelPartyAdvanceContext
    {
        internal readonly DailyV1SelectedRuntimeOwnerGraph Graph;
        internal readonly IDailyV1SelectedP12TravelPartyAdvanceProtocol Protocol;
        internal readonly Thread OwnerThread;
        internal readonly int OwnerManagedThreadId;
        internal readonly HashSet<string> AllowedSectionIds;
        internal readonly Dictionary<string, SelectedP12OwnerSectionBaseline> CoreOwnerSectionBaselines;
        internal readonly HashSet<string> ChangedSectionIds =
            new HashSet<string>(StringComparer.Ordinal);
        internal int CoreTravelPartyCardinality;
        internal long CoreTravelPartyRevision;
        internal IDisposable RuntimeProtocolScope;

        internal SelectedP12TravelPartyAdvanceContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            IDailyV1SelectedP12TravelPartyAdvanceProtocol protocol,
            IEnumerable<string> sectionIds,
            Dictionary<string, SelectedP12OwnerSectionBaseline> coreOwnerSectionBaselines,
            int travelPartyCardinality,
            long travelPartyRevision,
            IDisposable runtimeProtocolScope)
        {
            Graph = graph;
            Protocol = protocol;
            OwnerThread = Thread.CurrentThread;
            OwnerManagedThreadId = OwnerThread.ManagedThreadId;
            AllowedSectionIds = new HashSet<string>(sectionIds, StringComparer.Ordinal);
            CoreOwnerSectionBaselines = coreOwnerSectionBaselines
                ?? throw new ArgumentNullException(nameof(coreOwnerSectionBaselines));
            CoreTravelPartyCardinality = travelPartyCardinality;
            CoreTravelPartyRevision = travelPartyRevision;
            RuntimeProtocolScope = runtimeProtocolScope;
        }

        internal bool IsOwnedByCurrentThread() =>
            ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class SelectedP12MerchantDailyTradeContext
    {
        internal readonly DailyV1SelectedRuntimeOwnerGraph Graph;
        internal readonly DailyV1NpcRuntimeOwners Actor;
        internal readonly IDailyV1SelectedP12MerchantDailyTradeProtocol Protocol;
        internal readonly Thread OwnerThread;
        internal readonly int OwnerManagedThreadId;
        internal readonly HashSet<string> AllowedSectionIds;
        internal readonly HashSet<string> ChangedSectionIds =
            new HashSet<string>(StringComparer.Ordinal);
        internal IDisposable ProtocolScope;

        internal SelectedP12MerchantDailyTradeContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners actor,
            IDailyV1SelectedP12MerchantDailyTradeProtocol protocol,
            IReadOnlyList<string> sectionIds,
            IDisposable protocolScope)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));
            Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            OwnerThread = Thread.CurrentThread;
            OwnerManagedThreadId = OwnerThread.ManagedThreadId;
            AllowedSectionIds = new HashSet<string>(sectionIds ??
                throw new ArgumentNullException(nameof(sectionIds)), StringComparer.Ordinal);
            ProtocolScope = protocolScope ?? throw new ArgumentNullException(nameof(protocolScope));
        }

        internal bool IsOwnedByCurrentThread() => ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class SelectedP12OwnerSectionBaseline
    {
        internal readonly IOwnerSectionCensusProvider Provider;
        internal readonly object Owner;
        internal readonly int SchemaVersion;
        internal int Cardinality;
        internal long Revision;

        internal SelectedP12OwnerSectionBaseline(
            IOwnerSectionCensusProvider provider,
            OwnerSectionCensusWitness witness)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Owner = witness?.OwnerInstanceIdentity ?? throw new ArgumentNullException(nameof(witness));
            SchemaVersion = witness.SchemaVersion;
            Cardinality = witness.Cardinality;
            Revision = witness.Revision;
        }
    }

    private sealed class SelectedCityPresenceCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly DailyV1CityNpcPresenceOwner owner;

        internal SelectedCityPresenceCensusProvider(DailyV1CityNpcPresenceOwner owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            if (string.IsNullOrWhiteSpace(owner.CityRuntimeId)
                || owner.OrderedNpcRuntimeIds == null
                || owner.Revision < 0L)
                throw new InvalidOperationException("The selected City presence owner has no valid census state.");

            return new OwnerSectionCensusWitness(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(owner.CityRuntimeId),
                DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion,
                owner,
                owner.OrderedNpcRuntimeIds.Count,
                owner.Revision);
        }
    }

    private sealed class SelectedEventCounterCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly RuntimeIdAllocator allocator;
        private readonly IOwnerSectionCensusProvider provider;

        internal SelectedEventCounterCensusProvider(RuntimeIdAllocator allocator)
        {
            this.allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
            provider = RuntimeIdAllocatorCensusProvider.CreateEventCounterProvider(allocator);
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (witness == null
                || !string.Equals(witness.SectionId, RuntimeIdAllocatorCensusProvider.EventsSectionId,
                    StringComparison.Ordinal)
                || witness.SchemaVersion != RuntimeIdAllocatorCensusProvider.SchemaVersion
                || witness.Cardinality != 1
                || witness.Revision < 0L
                || !RuntimeIdAllocatorOwnerBridge.TryMatchCurrentCensus(
                    allocator, RuntimeIdAllocatorCensusCounter.Events,
                    witness.OwnerInstanceIdentity, witness.Revision, out _))
                throw new InvalidOperationException("The selected event-counter census does not match its exact allocator owner.");
            return witness;
        }
    }

    private sealed class SelectedTravelPartyCounterCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly RuntimeIdAllocator allocator;
        private readonly IOwnerSectionCensusProvider provider;

        internal SelectedTravelPartyCounterCensusProvider(RuntimeIdAllocator allocator)
        {
            this.allocator = allocator ?? throw new ArgumentNullException(nameof(allocator));
            provider = RuntimeIdAllocatorCensusProvider.CreateTravelPartyCounterProvider(allocator);
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (witness == null
                || !string.Equals(witness.SectionId,
                    RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId, StringComparison.Ordinal)
                || witness.SchemaVersion != RuntimeIdAllocatorCensusProvider.SchemaVersion
                || witness.Cardinality != 1
                || witness.Revision < 0L
                || !RuntimeIdAllocatorOwnerBridge.TryMatchCurrentCensus(
                    allocator, RuntimeIdAllocatorCensusCounter.TravelParties,
                    witness.OwnerInstanceIdentity, witness.Revision, out _))
                throw new InvalidOperationException(
                    "The selected TravelParty-counter census does not match its exact allocator owner.");
            return witness;
        }
    }

    private sealed class SelectedRecordSequenceCensusProvider : IOwnerSectionCensusProvider
    {
        private readonly SimulationRecordSequence sequence;
        private readonly IOwnerSectionCensusProvider provider;

        internal SelectedRecordSequenceCensusProvider(SimulationRecordSequence sequence)
        {
            this.sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            provider = new SimulationRecordSequenceCensusProvider(sequence);
        }

        public OwnerSectionCensusWitness GetCurrentCensus()
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (witness == null
                || !string.Equals(witness.SectionId, SimulationRecordSequenceCensusProvider.SectionId,
                    StringComparison.Ordinal)
                || witness.SchemaVersion != SimulationRecordSequenceCensusProvider.SchemaVersion
                || witness.Cardinality != 1
                || witness.Revision < 0L
                || !SimulationRecordSequenceOwnerBridge.TryMatchCurrentCensus(
                    sequence, witness.OwnerInstanceIdentity, witness.Revision, out _))
                throw new InvalidOperationException("The selected record-sequence census does not match its exact owner.");
            return witness;
        }
    }

    private sealed class SelectedP12TravelPartyRuntimeProtocolScope : IDisposable
    {
        private readonly DailyV1ExecutionBoundaryState executionState;
        private readonly IDailyV1SelectedP12TravelPartyAdvanceProtocol protocol;
        private int disposed;

        internal SelectedP12TravelPartyRuntimeProtocolScope(
            DailyV1ExecutionBoundaryState executionState,
            IDailyV1SelectedP12TravelPartyAdvanceProtocol protocol)
        {
            this.executionState = executionState
                ?? throw new ArgumentNullException(nameof(executionState));
            this.protocol = protocol
                ?? throw new ArgumentNullException(nameof(protocol));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            if (executionState.TryEndRuntimeProtocolOperation()) return;

            executionState.InvalidateCurrentCaptureToken();
            try { protocol.FaultClosed(); }
            catch { }
        }
    }

    private sealed class SelectedP12SoloTravelStartContext
    {
        internal readonly DailyV1SelectedRuntimeOwnerGraph Graph;
        internal readonly DailyV1NpcRuntimeOwners Actor;
        internal readonly IDailyV1SelectedP12SoloTravelStartProtocol Protocol;
        internal readonly Thread OwnerThread;
        internal readonly int OwnerManagedThreadId;
        internal readonly HashSet<string> AllowedSectionIds;
        internal readonly Dictionary<string, SelectedP12OwnerSectionBaseline> CoreOwnerSectionBaselines;
        internal readonly HashSet<string> ChangedSectionIds =
            new HashSet<string>(StringComparer.Ordinal);
        internal IDisposable ProtocolScope;

        internal SelectedP12SoloTravelStartContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            DailyV1NpcRuntimeOwners actor,
            IDailyV1SelectedP12SoloTravelStartProtocol protocol,
            IEnumerable<string> sectionIds,
            Dictionary<string, SelectedP12OwnerSectionBaseline> coreOwnerSectionBaselines,
            IDisposable protocolScope)
        {
            Graph = graph;
            Actor = actor;
            Protocol = protocol;
            OwnerThread = Thread.CurrentThread;
            OwnerManagedThreadId = OwnerThread.ManagedThreadId;
            AllowedSectionIds = new HashSet<string>(sectionIds, StringComparer.Ordinal);
            CoreOwnerSectionBaselines = coreOwnerSectionBaselines
                ?? throw new ArgumentNullException(nameof(coreOwnerSectionBaselines));
            ProtocolScope = protocolScope;
        }

        internal bool IsOwnedByCurrentThread() =>
            ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class SelectedP12SoloTravelStartScope : IDisposable
    {
        private DailyV1ExecutionBoundaryState owner;
        private readonly SelectedP12SoloTravelStartContext context;

        internal SelectedP12SoloTravelStartScope(
            DailyV1ExecutionBoundaryState owner,
            SelectedP12SoloTravelStartContext context)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Dispose()
        {
            DailyV1ExecutionBoundaryState current = Interlocked.Exchange(ref owner, null);
            current?.ExitSelectedP12SoloTravelStart(context);
        }
    }

    private enum SelectedP12TravelPartyStoreReceipt
    {
        None,
        Added,
        CompensatingRemoval
    }

    private sealed class SelectedP12TravelPartyStartContext
    {
        internal readonly DailyV1SelectedRuntimeOwnerGraph Graph;
        internal readonly SimulationRecordSequence RecordSequence;
        internal readonly HashSet<string> MemberRuntimeIds;
        internal readonly IDailyV1SelectedP12TravelPartyStartProtocol Protocol;
        internal readonly Thread OwnerThread;
        internal readonly int OwnerManagedThreadId;
        internal readonly HashSet<string> AllowedSectionIds;
        internal readonly Dictionary<string, SelectedP12OwnerSectionBaseline> CoreOwnerSectionBaselines;
        internal readonly HashSet<string> ChangedSectionIds = new HashSet<string>(StringComparer.Ordinal);
        internal readonly int InitialTravelPartyStoreCardinality;
        internal bool TravelPartyStoreAdditionReceipted;
        internal bool TravelPartyStoreCompensationReceipted;
        internal IDisposable ProtocolScope;

        internal SelectedP12TravelPartyStartContext(
            DailyV1SelectedRuntimeOwnerGraph graph,
            IEnumerable<DailyV1NpcRuntimeOwners> members,
            SimulationRecordSequence recordSequence,
            IDailyV1SelectedP12TravelPartyStartProtocol protocol,
            IEnumerable<string> sectionIds,
            Dictionary<string, SelectedP12OwnerSectionBaseline> coreOwnerSectionBaselines,
            IDisposable protocolScope)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            RecordSequence = recordSequence ?? throw new ArgumentNullException(nameof(recordSequence));
            MemberRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DailyV1NpcRuntimeOwners member in members ?? throw new ArgumentNullException(nameof(members)))
                if (member == null || string.IsNullOrWhiteSpace(member.RuntimeId)
                    || !MemberRuntimeIds.Add(member.RuntimeId))
                    throw new ArgumentException("TravelParty members must have unique exact runtime identities.", nameof(members));
            Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            OwnerThread = Thread.CurrentThread;
            OwnerManagedThreadId = OwnerThread.ManagedThreadId;
            AllowedSectionIds = new HashSet<string>(sectionIds ?? throw new ArgumentNullException(nameof(sectionIds)), StringComparer.Ordinal);
            CoreOwnerSectionBaselines = coreOwnerSectionBaselines
                ?? throw new ArgumentNullException(nameof(coreOwnerSectionBaselines));
            if (!CoreOwnerSectionBaselines.TryGetValue(
                    TravelPartyCensusProvider.SectionId,
                    out SelectedP12OwnerSectionBaseline partyStoreBaseline)
                || partyStoreBaseline.Cardinality < 0)
                throw new ArgumentException(
                    "TravelParty start requires its exact initial Core store baseline.",
                    nameof(coreOwnerSectionBaselines));
            InitialTravelPartyStoreCardinality = partyStoreBaseline.Cardinality;
            ProtocolScope = protocolScope ?? throw new ArgumentNullException(nameof(protocolScope));
        }

        internal bool IsOwnedByCurrentThread() =>
            ReferenceEquals(OwnerThread, Thread.CurrentThread)
            && OwnerManagedThreadId == Thread.CurrentThread.ManagedThreadId;
    }

    private sealed class SelectedP12TravelPartyStartScope : IDisposable
    {
        private DailyV1ExecutionBoundaryState owner;
        private readonly SelectedP12TravelPartyStartContext context;

        internal SelectedP12TravelPartyStartScope(
            DailyV1ExecutionBoundaryState owner,
            SelectedP12TravelPartyStartContext context)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Dispose()
        {
            DailyV1ExecutionBoundaryState current = Interlocked.Exchange(ref owner, null);
            current?.ExitSelectedP12TravelPartyStart(context);
        }
    }

    internal SimulationTime SimulationTime { get; private set; }
    internal SimulationRuntimeAdmissionContext AdmissionContext { get; private set; }
    internal SimulationRuntimeCompositionProfile CompositionProfile { get; private set; }
    internal WorldId WorldId { get; private set; }
    internal EffectiveSimulationConfiguration ConfigurationIdentity { get; private set; }
    internal SimulationCalendar CalendarIdentity { get; private set; }
    internal AuthoritativeMutationGuard MutationGuard => mutationGuard;
    internal bool CanMutate => mutationGuard.CanMutate;
    internal bool CoreMutationGuardAuthoritiesBound => coreMutationGuardAuthoritiesBound;
    internal bool SelectedCoreOwnerMutationGuardAuthoritiesBound =>
        selectedCoreOwnerMutationGuardAuthoritiesBound;
    internal bool IsFactualReadWorldPublished => factualReadWorldPublished;

    /// <summary>
    /// Binds the exact selected Core graph before the host is asked to bind
    /// remaining non-graph authorities and Unity projections. Core owns these
    /// owner instances and the shared execution guard; the host cannot select
    /// or substitute the roots that have already been bound here.
    /// </summary>
    internal bool TryBindSelectedCoreOwnerMutationGuardAuthorities(
        out string diagnostic)
    {
        diagnostic = null;
        DailyV1SelectedRuntimeOwnerGraph graph = selectedRuntimeOwnerGraph;
        if (!HasAdmittedSelectedDailyExecution
            || graph == null
            || AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !AdmissionContext.IsRestoredContinuation
            || !AdmissionContext.IsOwnedByCurrentThread()
            || coreMutationGuardAuthoritiesBound
            || !mutationGuard.CanMutate
            || factualReadWorldPublished
            || currentCaptureToken != null
            || completedCoreSequence != 0L
            || AdvanceLeaseHeld
            || ActiveOperationCount != 0)
        {
            diagnostic = "The selected Core owner graph is not at its private, healthy restored boundary.";
            return false;
        }

        if (!SimulationTimeOwnerBridge.CanBindMutationStatus(SimulationTime, mutationGuard)
            || !((IAuthoritativeMutationGuardBindable)selectedActorChoiceStore)
                .CanBindMutationGuard(mutationGuard)
            || !graph.CanBindMutationGuard(mutationGuard))
        {
            diagnostic = "A selected Core owner root is already bound to another mutation guard.";
            return false;
        }

        if (!SimulationTimeOwnerBridge.TryBindMutationStatus(SimulationTime, mutationGuard)
            || !((IAuthoritativeMutationGuardBindable)selectedActorChoiceStore)
                .TryBindMutationGuard(mutationGuard)
            || !graph.TryBindMutationGuard(mutationGuard))
        {
            diagnostic = "The selected Core owner graph could not bind its shared mutation guard.";
            return false;
        }

        selectedCoreOwnerMutationGuardAuthoritiesBound = true;
        return true;
    }

    internal bool TryMarkCoreMutationGuardAuthoritiesBound()
    {
        if (coreMutationGuardAuthoritiesBound || !mutationGuard.CanMutate)
            return false;
        coreMutationGuardAuthoritiesBound = true;
        return true;
    }

    internal bool TryMarkFactualReadWorldPublished()
    {
        if (factualReadWorldPublished)
            return false;
        factualReadWorldPublished = true;
        return true;
    }

    internal DailyV1ExecutionBoundaryState() { }

    /// <summary>
    /// Constructs the one Core execution state used by a selected runtime.
    /// Clock, admission, composition and world identities live beside that
    /// runtime's lease, completed-boundary sequence, graph and capture token;
    /// a Unity adapter must not keep parallel copies as runtime authority.
    /// </summary>
    internal DailyV1ExecutionBoundaryState(
        SimulationTime simulationTime,
        SimulationRuntimeAdmissionContext admissionContext,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId)
    {
        if (simulationTime == null) throw new ArgumentNullException(nameof(simulationTime));
        if (!Enum.IsDefined(typeof(SimulationRuntimeCompositionProfile), compositionProfile))
            throw new ArgumentOutOfRangeException(nameof(compositionProfile));
        SimulationTime = simulationTime;
        AdmissionContext = admissionContext;
        CompositionProfile = compositionProfile;
        WorldId = worldId;
        runtimeIdentityBound = true;
    }

    internal bool TryBindRuntimeConfiguration(
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity)
    {
        if (!runtimeIdentityBound || configurationIdentity == null || calendarIdentity == null)
            return false;
        if (runtimeConfigurationBound)
            return ReferenceEquals(ConfigurationIdentity, configurationIdentity)
                && ReferenceEquals(CalendarIdentity, calendarIdentity);
        ConfigurationIdentity = configurationIdentity;
        CalendarIdentity = calendarIdentity;
        runtimeConfigurationBound = true;
        return true;
    }

    /// <summary>
    /// Binds the admitted Core execution state to its one application/session
    /// advance authority. This routing identity lives with the runtime state;
    /// Unity may request the bound application, but does not own a duplicate.
    /// </summary>
    internal bool TryBindApplicationAdvanceAuthority(
        DailyV1Application application,
        DailyV1ActiveRuntimeSession session)
    {
        if (application == null || session == null
            || !ReferenceEquals(session.ExecutionBoundaryState, this)
            || !HasAdmittedSelectedDailyExecution
            || AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !SelectedRuntimeExecutionRootsBound
            || SelectedRuntimeOwnerGraph == null)
            return false;

        lock (applicationAdvanceAuthorityGate)
        {
            if (applicationAdvanceAuthority == null && applicationAdvanceSession == null)
            {
                applicationAdvanceAuthority = application;
                applicationAdvanceSession = session;
                return true;
            }

            return ReferenceEquals(applicationAdvanceAuthority, application)
                && ReferenceEquals(applicationAdvanceSession, session);
        }
    }

    /// <summary>
    /// Copies the exact bound application/session pair while holding only the
    /// state binding lock. Callers must release this lock before entering the
    /// application's session gate.
    /// </summary>
    internal bool TryGetApplicationAdvanceAuthority(
        out DailyV1Application application,
        out DailyV1ActiveRuntimeSession session)
    {
        lock (applicationAdvanceAuthorityGate)
        {
            application = applicationAdvanceAuthority;
            session = applicationAdvanceSession;
            return application != null
                && session != null
                && ReferenceEquals(session.ExecutionBoundaryState, this);
        }
    }

    private bool MatchesBoundRuntimeIdentity(
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay)
    {
        return !runtimeIdentityBound
            || (runtimeConfigurationBound
                && ReferenceEquals(AdmissionContext, admissionContext)
                && ReferenceEquals(ConfigurationIdentity, configurationIdentity)
                && ReferenceEquals(CalendarIdentity, calendarIdentity)
                && CompositionProfile == compositionProfile
                && ReferenceEquals(WorldId, worldId)
                && absoluteDay == SimulationTime.AbsoluteDay);
    }

    internal bool AdvanceLeaseHeld => advanceLease != null;

    /// <summary>
    /// The single operation count shared by application admission and the P12
    /// census protocol for this runtime. It is deliberately stored beside the
    /// runtime lease and capture token, rather than mirrored by a host gate.
    /// </summary>
    internal int ActiveOperationCount
    {
        get { lock (operationSyncRoot) return activeOperationCount; }
    }

    internal int ActiveRuntimeOperationCount
    {
        get
        {
            lock (operationSyncRoot)
            {
                int count = 0;
                foreach (DailyV1OperationScopeKind scope in activeOperationScopes)
                    if (scope == DailyV1OperationScopeKind.RuntimeProtocol) count++;
                return count;
            }
        }
    }

    internal bool HasRegisteredRuntimeProtocolOperationContracts
    {
        get
        {
            lock (operationSyncRoot)
                return expectedRuntimeProtocolOperationContracts.Count > 0;
        }
    }

    internal bool HasRuntimeProtocolOperationInventory
    {
        get
        {
            lock (operationSyncRoot)
                return runtimeProtocolOperationInventorySealed
                    && expectedRuntimeProtocolOperationContracts.Count > 0;
        }
    }

    /// <summary>
    /// Binds the host census protocol's sealed readiness to this selected Core
    /// runtime. The protocol owns provider composition; Core owns the bound
    /// owner-thread readiness, operation count, and fault latch.
    /// </summary>
    internal bool TryMarkSelectedRuntimeCensusProtocolReady()
    {
        lock (operationSyncRoot)
        {
            if (selectedRuntimeCensusProtocolReady
                || selectedRuntimeCensusProtocolFaulted
                || AdmissionContext == null
                || AdmissionContext.Profile
                    != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !runtimeIdentityBound
                || !runtimeConfigurationBound
                || !runtimeProtocolOperationInventorySealed
                || expectedRuntimeProtocolOperationContracts.Count == 0
                || activeOperationScopes.Count != 0
                || exclusiveSessionReplacementInProgress)
                return false;

            selectedRuntimeCensusProtocolReady = true;
            return true;
        }
    }

    internal bool TryReadSelectedRuntimeCensusOperationCount(out int count)
    {
        count = 0;
        lock (operationSyncRoot)
        {
            if (!selectedRuntimeCensusProtocolReady
                || selectedRuntimeCensusProtocolFaulted
                || AdmissionContext == null
                || AdmissionContext.Profile
                    != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !runtimeIdentityBound
                || !runtimeConfigurationBound
                || !runtimeProtocolOperationInventorySealed
                || exclusiveSessionReplacementInProgress)
                return false;

            foreach (DailyV1OperationScopeKind scope in activeOperationScopes)
                if (scope == DailyV1OperationScopeKind.RuntimeProtocol) count++;
            return true;
        }
    }

    /// <summary>
    /// Confirms that a restored target has a healthy selected-runtime census
    /// protocol and no Core operation scope before its mutation guard is bound.
    /// Unlike a host context flag, this assessment is owned by the authoritative
    /// execution state and also rejects a faulted protocol with no registered
    /// operation scope.
    /// </summary>
    internal bool TryAssessRestoredBoundaryQuiescence()
    {
        lock (operationSyncRoot)
        {
            return selectedRuntimeCensusProtocolReady
                && !selectedRuntimeCensusProtocolFaulted
                && AdmissionContext != null
                && AdmissionContext.Profile
                    == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                && AdmissionContext.IsOwnedByCurrentThread()
                && runtimeIdentityBound
                && runtimeConfigurationBound
                && runtimeProtocolOperationInventorySealed
                && !exclusiveSessionReplacementInProgress
                && activeOperationScopes.Count == 0;
        }
    }

    internal bool IsSelectedRuntimeCensusProtocolFaulted
    {
        get
        {
            lock (operationSyncRoot)
                return selectedRuntimeCensusProtocolFaulted;
        }
    }

    internal void MarkSelectedRuntimeCensusProtocolFaulted()
    {
        lock (operationSyncRoot)
        {
            selectedRuntimeCensusProtocolFaulted = true;
            selectedRuntimeCensusProtocolReady = false;
        }
    }
    internal bool HasRuntimeProtocolOperationContract(string operationContractId)
    {
        if (string.IsNullOrWhiteSpace(operationContractId)) return false;
        lock (operationSyncRoot)
            return runtimeProtocolOperationInventorySealed
                && expectedRuntimeProtocolOperationContracts.Contains(operationContractId);
    }

    internal IReadOnlyList<string> GetRuntimeProtocolOperationContractIds()
    {
        lock (operationSyncRoot)
        {
            List<string> operationContractIds =
                new List<string>(expectedRuntimeProtocolOperationContracts);
            operationContractIds.Sort(StringComparer.Ordinal);
            return operationContractIds.AsReadOnly();
        }
    }

    /// <summary>
    /// Owns the logical P12 section contracts attached to this runtime.
    /// This is only the declared inventory; provider registration and exact
    /// selected-profile completeness remain separately assessed by the census
    /// adapter and its composition evidence.
    /// </summary>
    internal bool ExpectedOwnerSectionContractInventorySealed
    {
        get { lock (operationSyncRoot) return expectedOwnerSectionContractInventorySealed; }
    }

    internal long ExpectedOwnerSectionContractInventoryRevision
    {
        get { lock (operationSyncRoot) return expectedOwnerSectionContractInventoryRevision; }
    }

    internal IReadOnlyList<DailyV1OwnerSectionContractIdentity> GetExpectedOwnerSectionContractInventory(
        out long revision)
    {
        lock (operationSyncRoot)
        {
            List<DailyV1OwnerSectionContractIdentity> contracts =
                new List<DailyV1OwnerSectionContractIdentity>(expectedOwnerSectionContracts.Values);
            contracts.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.SectionId, right.SectionId));
            revision = expectedOwnerSectionContractInventoryRevision;
            return contracts.AsReadOnly();
        }
    }

    internal bool TryRegisterExpectedOwnerSectionContract(
        string sectionId,
        int schemaVersion,
        OwnerSectionRole role)
    {
        if (string.IsNullOrWhiteSpace(sectionId)
            || schemaVersion <= 0
            || !Enum.IsDefined(typeof(OwnerSectionRole), role))
            return false;

        lock (operationSyncRoot)
        {
            if (expectedOwnerSectionContractInventorySealed
                || activeOperationScopes.Count != 0
                || exclusiveSessionReplacementInProgress
                || expectedOwnerSectionContractInventoryRevision == long.MaxValue
                || expectedOwnerSectionContracts.ContainsKey(sectionId))
                return false;

            Dictionary<string, DailyV1OwnerSectionContractIdentity> staged =
                new Dictionary<string, DailyV1OwnerSectionContractIdentity>(
                    expectedOwnerSectionContracts, StringComparer.Ordinal);
            staged.Add(sectionId,
                new DailyV1OwnerSectionContractIdentity(sectionId, schemaVersion, role));
            expectedOwnerSectionContracts = staged;
            expectedOwnerSectionContractInventoryRevision++;
            return true;
        }
    }

    internal bool TryReplaceExpectedOwnerSectionContractInventoryBeforeSeal(
        IEnumerable<DailyV1OwnerSectionContractIdentity> contracts)
    {
        if (!TryCreateExpectedOwnerSectionContractMap(contracts, out var staged))
            return false;

        lock (operationSyncRoot)
        {
            if (expectedOwnerSectionContractInventorySealed
                || activeOperationScopes.Count != 0
                || exclusiveSessionReplacementInProgress
                || expectedOwnerSectionContractInventoryRevision == long.MaxValue)
                return false;

            expectedOwnerSectionContracts = staged;
            expectedOwnerSectionContractInventoryRevision++;
            return true;
        }
    }

    /// <summary>
    /// Applies the validated census roster transition's section identity set
    /// while the runtime is inside an owner-thread operation. This does not
    /// advance the mutation epoch; the owning operation retains that commit.
    /// </summary>
    internal bool TryReplaceExpectedOwnerSectionContractInventoryDuringRuntimeOperation(
        IEnumerable<DailyV1OwnerSectionContractIdentity> contracts)
    {
        if (!TryCreateExpectedOwnerSectionContractMap(contracts, out var staged))
            return false;

        lock (operationSyncRoot)
        {
            if (!expectedOwnerSectionContractInventorySealed
                || exclusiveSessionReplacementInProgress
                || !activeRuntimeProtocolOperationContracts.Contains(
                    DailyV1RuntimeProtocolOperationContracts.NpcMembership)
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || expectedOwnerSectionContractInventoryRevision == long.MaxValue)
                return false;

            expectedOwnerSectionContracts = staged;
            expectedOwnerSectionContractInventoryRevision++;
            return true;
        }
    }

    internal bool TrySealExpectedOwnerSectionContractInventory()
    {
        lock (operationSyncRoot)
        {
            if (expectedOwnerSectionContractInventorySealed
                || expectedOwnerSectionContracts.Count == 0
                || activeOperationScopes.Count != 0
                || exclusiveSessionReplacementInProgress)
                return false;

            expectedOwnerSectionContractInventorySealed = true;
            return true;
        }
    }

    private static bool TryCreateExpectedOwnerSectionContractMap(
        IEnumerable<DailyV1OwnerSectionContractIdentity> contracts,
        out Dictionary<string, DailyV1OwnerSectionContractIdentity> inventory)
    {
        inventory = null;
        if (contracts == null) return false;

        Dictionary<string, DailyV1OwnerSectionContractIdentity> staged =
            new Dictionary<string, DailyV1OwnerSectionContractIdentity>(StringComparer.Ordinal);
        try
        {
            foreach (DailyV1OwnerSectionContractIdentity contract in contracts)
            {
                if (contract == null
                    || string.IsNullOrWhiteSpace(contract.SectionId)
                    || contract.SchemaVersion <= 0
                    || !Enum.IsDefined(typeof(OwnerSectionRole), contract.Role)
                    || staged.ContainsKey(contract.SectionId))
                    return false;
                staged.Add(contract.SectionId, contract);
            }
        }
        catch
        {
            return false;
        }

        inventory = staged;
        return true;
    }
    internal bool TryBeginOperation(DailyV1OperationScopeKind kind = DailyV1OperationScopeKind.Application)
    {
        lock (operationSyncRoot)
        {
            if (exclusiveSessionReplacementInProgress
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            if (kind == DailyV1OperationScopeKind.RuntimeProtocol
                || !Enum.IsDefined(typeof(DailyV1OperationScopeKind), kind))
                return false;
            return TryAddOperationScope(kind);
        }
    }

    internal bool TryEndOperation(DailyV1OperationScopeKind kind = DailyV1OperationScopeKind.Application)
    {
        lock (operationSyncRoot)
        {
            if (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread())
                return false;
            if (kind == DailyV1OperationScopeKind.RuntimeProtocol
                || !Enum.IsDefined(typeof(DailyV1OperationScopeKind), kind))
                return false;
            return TryRemoveOperationScope(kind);
        }
    }

    /// <summary>
    /// Records one runtime operation during selected-profile bootstrap. The
    /// complete supported inventory is sealed before Core will admit any ID.
    /// </summary>
    internal bool TryRegisterRuntimeProtocolOperationContract(string operationContractId)
    {
        if (string.IsNullOrWhiteSpace(operationContractId)) return false;
        lock (operationSyncRoot)
        {
            if (runtimeProtocolOperationInventorySealed
                || activeOperationScopes.Count != 0
                || exclusiveSessionReplacementInProgress
                || AdmissionContext == null
                || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || !runtimeIdentityBound
                || !runtimeConfigurationBound
                || !AdmissionContext.IsOwnedByCurrentThread())
                return false;

            return expectedRuntimeProtocolOperationContracts.Add(operationContractId);
        }
    }

    /// <summary>
    /// Seals the Core-owned allowlist only when it exactly matches one complete
    /// supported selected-runtime catalogue.
    /// </summary>
    internal bool TrySealRuntimeProtocolOperationInventory()
    {
        lock (operationSyncRoot)
        {
            if (runtimeProtocolOperationInventorySealed
                || activeOperationScopes.Count != 0
                || exclusiveSessionReplacementInProgress
                || AdmissionContext == null
                || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                || !runtimeIdentityBound
                || !runtimeConfigurationBound
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !DailyV1RuntimeProtocolOperationContracts.IsSupportedSelectedOperationInventory(
                    expectedRuntimeProtocolOperationContracts))
                return false;

            runtimeProtocolOperationInventorySealed = true;
            return true;
        }
    }

    /// <summary>
    /// Prevents both application and direct runtime/P12 operations from
    /// entering the source session while a detached replacement is staged.
    /// This flag shares the same lock and operation registry as daily advance
    /// and P12 census scopes, so same-thread callbacks cannot bypass the app gate.
    /// </summary>
    internal bool TryBeginExclusiveSessionReplacement()
    {
        lock (operationSyncRoot)
        {
            if (exclusiveSessionReplacementInProgress
                || activeOperationScopes.Count != 0
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            exclusiveSessionReplacementInProgress = true;
            return true;
        }
    }

    internal bool TryEndExclusiveSessionReplacement()
    {
        lock (operationSyncRoot)
        {
            if (!exclusiveSessionReplacementInProgress
                || activeOperationScopes.Count != 0
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            exclusiveSessionReplacementInProgress = false;
            return true;
        }
    }

    internal bool TryAssessCaptureScope(DailyV1CaptureScopeAllowance allowance)
    {
        lock (operationSyncRoot)
        {
            if (AdmissionContext?.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                && selectedRuntimeCensusProtocolFaulted)
                return false;

            if ((exclusiveSessionReplacementInProgress
                    && allowance != DailyV1CaptureScopeAllowance.SessionReplacementValidation)
                || (AdmissionContext != null && !AdmissionContext.IsOwnedByCurrentThread()))
                return false;
            if (allowance == DailyV1CaptureScopeAllowance.None
                || allowance == DailyV1CaptureScopeAllowance.SessionReplacementValidation)
                return activeOperationScopes.Count == 0;
            if (activeOperationScopes.Count != 1)
                return false;

            DailyV1OperationScopeKind expected = allowance == DailyV1CaptureScopeAllowance.ContinuationSave
                ? DailyV1OperationScopeKind.ContinuationSave
                : allowance == DailyV1CaptureScopeAllowance.DailyAdvance
                    ? DailyV1OperationScopeKind.DailyAdvance
                    : (DailyV1OperationScopeKind)byte.MaxValue;
            return activeOperationScopes[0] == expected;
        }
    }

    internal bool TryBeginSelectedRuntimeDailyAdvanceOperation()
    {
        lock (operationSyncRoot)
        {
            if (!selectedRuntimeCensusProtocolReady
                || selectedRuntimeCensusProtocolFaulted
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread())
                return false;

            return TryBeginRuntimeProtocolOperation(
                DailyV1RuntimeProtocolOperationContracts.DailyAdvance,
                requireNoActiveRuntimeProtocolOperations: true);
        }
    }

    internal bool TryBeginRuntimeProtocolOperation(string operationContractId) =>
        TryBeginRuntimeProtocolOperation(
            operationContractId,
            requireNoActiveRuntimeProtocolOperations: false);

    /// <summary>
    /// Starts an outer runtime operation only when the protocol registry is
    /// idle. Admission and registration stay atomic in the Core execution state.
    /// </summary>
    internal bool TryBeginRuntimeProtocolOperation(
        string operationContractId,
        bool requireNoActiveRuntimeProtocolOperations)
    {
        lock (operationSyncRoot)
        {
            if (exclusiveSessionReplacementInProgress
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !runtimeProtocolOperationInventorySealed
                || string.IsNullOrWhiteSpace(operationContractId)
                || !expectedRuntimeProtocolOperationContracts.Contains(operationContractId)
                || (requireNoActiveRuntimeProtocolOperations
                    && activeRuntimeProtocolOperationContracts.Count != 0)
                || activeRuntimeProtocolOperationContracts.Count == int.MaxValue)
                return false;
            if (!TryAddOperationScope(DailyV1OperationScopeKind.RuntimeProtocol)) return false;
            activeRuntimeProtocolOperationContracts.Add(operationContractId);
            return true;
        }
    }

    internal bool TryEndRuntimeProtocolOperation()
    {
        lock (operationSyncRoot)
        {
            if (AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || activeRuntimeProtocolOperationContracts.Count == 0
                || !TryRemoveOperationScope(DailyV1OperationScopeKind.RuntimeProtocol))
                return false;
            activeRuntimeProtocolOperationContracts.RemoveAt(
                activeRuntimeProtocolOperationContracts.Count - 1);
            return true;
        }
    }

    private int activeOperationCount => activeOperationScopes.Count;

    private bool TryAddOperationScope(DailyV1OperationScopeKind kind)
    {
        if (activeOperationScopes.Count == int.MaxValue) return false;
        activeOperationScopes.Add(kind);
        return true;
    }

    private bool TryRemoveOperationScope(DailyV1OperationScopeKind kind)
    {
        for (int index = activeOperationScopes.Count - 1; index >= 0; index--)
        {
            if (activeOperationScopes[index] != kind) continue;
            activeOperationScopes.RemoveAt(index);
            return true;
        }
        return false;
    }

    internal long CompletedCoreSequence => completedCoreSequence;
    internal object RuntimeInstanceIdentity => runtimeInstanceIdentity;
    internal DailyCaptureEligibilityToken CurrentCaptureToken => currentCaptureToken;
    /// <summary>
    /// The mutation epoch paired with this runtime's operation registry,
    /// completed-boundary sequence, and capture token. Continuation census
    /// protocols attached to this runtime must share this exact state.
    /// </summary>
    internal DailyV1MutationEpochState MutationEpochState => mutationEpochState;
    internal DailyV1SelectedRuntimeOwnerGraph SelectedRuntimeOwnerGraph => selectedRuntimeOwnerGraph;
    internal object SelectedRuntimeOwnerCensusSourceIdentity
    {
        get { lock (operationSyncRoot) return selectedRuntimeOwnerCensusSourceIdentity; }
    }

    /// <summary>
    /// Binds the selected profile's exact census source to this Core execution
    /// state. The source supplies the existing registered owner readers; Core
    /// owns the serialized invocation, allowed outer scope, and epoch check.
    /// </summary>
    internal bool TryBindSelectedRuntimeOwnerCensusSource(
        object sourceIdentity,
        Func<DailyV1CaptureScopeAllowance, DailyV1OwnerSectionCensusCaptureResult> captureSource)
    {
        if (sourceIdentity == null || captureSource == null
            || AdmissionContext?.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !runtimeIdentityBound || !runtimeConfigurationBound)
            return false;

        lock (operationSyncRoot)
        {
            if (activeOperationScopes.Count != 0 || advanceLease != null || currentCaptureToken != null)
                return false;
            if (selectedRuntimeOwnerCensusSourceIdentity != null)
                return ReferenceEquals(selectedRuntimeOwnerCensusSourceIdentity, sourceIdentity)
                    && Equals(selectedRuntimeOwnerCensusSource, captureSource);
            selectedRuntimeOwnerCensusSourceIdentity = sourceIdentity;
            selectedRuntimeOwnerCensusSource = captureSource;
            return true;
        }
    }

    internal bool TryCaptureBoundOwnerSectionCensus(
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts,
        DailyV1CaptureScopeAllowance allowedOuterScope,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure)
    {
        ownerSections = null;
        mutationEpoch = 0L;
        failure = DailyCaptureEligibilityFailure.RuntimeFaulted;
        Func<DailyV1CaptureScopeAllowance, DailyV1OwnerSectionCensusCaptureResult> captureSource;
        DailyV1SelectedRuntimeOwnerGraph captureOwnerGraph;
        ActorChoiceStore captureActorChoiceStore;
        lock (operationSyncRoot)
        {
            captureSource = selectedRuntimeOwnerCensusSource;
            captureOwnerGraph = selectedRuntimeOwnerGraph;
            captureActorChoiceStore = selectedActorChoiceStore;
        }

        DailyV1OwnerSectionCensusCaptureResult result;
        if (captureSource == null)
        {
            if (hostPorts == null
                || !hostPorts.TryCaptureOwnerSectionCensus(
                    allowedOuterScope,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> hostSections,
                    out long hostMutationEpoch,
                    out failure))
                return false;
            result = DailyV1OwnerSectionCensusCaptureResult.Captured(hostSections, hostMutationEpoch);
        }
        else
        {
            result = captureSource(allowedOuterScope);
        }

        if (result == null || !result.Succeeded)
        {
            failure = result?.Failure ?? DailyCaptureEligibilityFailure.RuntimeFaulted;
            return false;
        }
        if (result.MutationEpoch != mutationEpochState.CurrentEpoch)
        {
            failure = DailyCaptureEligibilityFailure.OperationInProgress;
            return false;
        }

        IReadOnlyList<OwnerSectionCensusSnapshot> capturedSections = result.OwnerSections;
        if (captureOwnerGraph != null)
        {
            if (!DailyV1SelectedRuntimeOwnerCensusSource.TryRebindCoreOwnedNpcRuntimeStateSections(
                    captureOwnerGraph, capturedSections,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> coreRuntimeStateSections,
                    out _))
            {
                failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                return false;
            }
            capturedSections = coreRuntimeStateSections;
            if (!DailyV1SelectedRuntimeOwnerCensusSource.TryCreateHostOwnedCensusRemainder(
                    capturedSections, captureOwnerGraph, captureActorChoiceStore,
                    out IReadOnlyList<OwnerSectionCensusSnapshot> hostOwnedSections, out _))
            {
                failure = DailyCaptureEligibilityFailure.OwnerCoverageIncomplete;
                return false;
            }
            capturedSections = hostOwnedSections;
        }

        ownerSections = capturedSections;
        mutationEpoch = result.MutationEpoch;
        failure = DailyCaptureEligibilityFailure.None;
        return true;
    }

    private bool IsEligibleForUnadmittedRestoredOwnerCensus(out string diagnostic)
    {
        diagnostic = null;
        if (!HasAdmittedSelectedDailyExecution
            || !selectedRuntimeOwnerGraphBound
            || selectedRuntimeOwnerGraph == null
            || AdmissionContext == null
            || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !AdmissionContext.IsRestoredContinuation
            || !AdmissionContext.IsOwnedByCurrentThread())
        {
            diagnostic = "Core restored owner-census capture requires the exact restored Daily-v1 owner thread and graph.";
            return false;
        }

        if (advanceLease != null
            || ActiveOperationCount != 0
            || factualReadWorldPublished
            || currentCaptureToken != null
            || completedCoreSequence != 0L
            || mutationEpochState.HasActiveReservation
            || !mutationGuard.CanMutate)
        {
            diagnostic = "Core restored owner-census capture requires an unpublished, idle, quiescent private target boundary.";
            return false;
        }

        return true;
    }

    internal bool TryCaptureUnadmittedRestoredOwnerSectionCensus(
        IDailyV1SelectedRuntimeAdvanceHostPorts hostPorts,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out string diagnostic)
    {
        ownerSections = null;
        mutationEpoch = 0L;
        diagnostic = null;
        Func<DailyV1CaptureScopeAllowance, DailyV1OwnerSectionCensusCaptureResult> captureSource;
        lock (operationSyncRoot)
        {
            if (!IsEligibleForUnadmittedRestoredOwnerCensus(out diagnostic))
                return false;

            if (selectedRuntimeOwnerCensusSourceIdentity == null
                || selectedRuntimeOwnerCensusSource == null)
            {
                if (hostPorts == null)
                {
                    diagnostic = "The exact Core execution state has no bound selected-owner census source.";
                    return false;
                }
                if (!hostPorts.TryCaptureUnadmittedRestoredDailyOwnerVector(
                        out ownerSections, out mutationEpoch, out diagnostic))
                    return false;
                if (!IsEligibleForUnadmittedRestoredOwnerCensus(out diagnostic)
                    || mutationEpoch != mutationEpochState.CurrentEpoch)
                {
                    ownerSections = null;
                    mutationEpoch = 0L;
                    if (string.IsNullOrWhiteSpace(diagnostic))
                        diagnostic = DailyCaptureEligibilityFailure.OperationInProgress.ToString();
                    return false;
                }
                return true;
            }
            captureSource = selectedRuntimeOwnerCensusSource;
        }

        DailyV1OwnerSectionCensusCaptureResult result = captureSource(DailyV1CaptureScopeAllowance.None);
        if (result == null || !result.Succeeded)
        {
            diagnostic = result?.Diagnostic ?? result?.Failure.ToString()
                ?? DailyCaptureEligibilityFailure.RuntimeFaulted.ToString();
            return false;
        }
        lock (operationSyncRoot)
        {
            if (!IsEligibleForUnadmittedRestoredOwnerCensus(out diagnostic)
                || result.MutationEpoch != mutationEpochState.CurrentEpoch)
            {
                if (string.IsNullOrWhiteSpace(diagnostic))
                    diagnostic = DailyCaptureEligibilityFailure.OperationInProgress.ToString();
                return false;
            }
        }

        ownerSections = result.OwnerSections;
        mutationEpoch = result.MutationEpoch;
        return true;
    }

    /// <summary>
    /// Derives the exact P12 row cut for the selected Merchant daily state
    /// pass. The two plan owners each have one row, SpatialKnowledge owns its
    /// locations/routes pair, and CommercialKnowledge owns its market,
    /// liquidity, and share-receipt rows.
    /// </summary>
    internal bool TryCreateSelectedP12MerchantDailyTradeSectionIds(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        out IReadOnlyList<string> sectionIds)
    {
        sectionIds = null;
        if (graph == null || actor == null || graph.Npcs == null
            || !HasAdmittedSelectedDailyExecution
            || AdmissionContext == null || !AdmissionContext.IsOwnedByCurrentThread()
            || !ReferenceEquals(selectedRuntimeOwnerGraph, graph)
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || string.IsNullOrWhiteSpace(actor.RuntimeId)
            || actor.MerchantTradePlan == null || actor.TravelPlan == null
            || actor.SpatialKnowledge == null || actor.CommercialKnowledge == null
            || selectedRuntimeActorRoster == null)
            return false;

        int exactGraphActorCount = 0;
        foreach (DailyV1NpcRuntimeOwners candidate in graph.Npcs)
            if (candidate != null
                && string.Equals(candidate.RuntimeId, actor.RuntimeId, StringComparison.Ordinal))
            {
                if (!ReferenceEquals(candidate, actor)) return false;
                exactGraphActorCount++;
            }
        if (exactGraphActorCount != 1) return false;

        int exactRosterEntryCount = 0;
        foreach (DailyV1ActorTurnRosterProjection member in selectedRuntimeActorRoster)
            if (member != null
                && string.Equals(member.RuntimeId, actor.RuntimeId, StringComparison.Ordinal))
                exactRosterEntryCount++;
        if (exactRosterEntryCount != 1) return false;

        List<string> ids = new List<string>(7)
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                DailyV1SelectedOwnerSectionCensusContract.NpcMerchantTradePlanKind, actor.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, actor.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + actor.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + actor.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialMarketsPrefix + actor.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialLiquidityPrefix + actor.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcCommercialShareReceiptsPrefix + actor.RuntimeId
        };
        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in ids)
            if (string.IsNullOrWhiteSpace(id) || !unique.Add(id)) return false;
        ids.Sort(StringComparer.Ordinal);
        sectionIds = Array.AsReadOnly(ids.ToArray());
        return true;
    }

    /// <summary>
    /// Opens the Core-owned selected Merchant P12 operation. The host proves
    /// its exact compatibility projections against Core's seven-row cut and
    /// provides the registered census lease; Core owns the changed-row set
    /// and emits one receipt at operation exit.
    /// </summary>
    internal bool ExecuteSelectedP12MerchantDailyTrade(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IDailyV1SelectedP12MerchantDailyTradeProtocol protocol,
        Func<bool> operation)
    {
        if (operation == null) return false;
        if (!HasAdmittedSelectedDailyExecution) return operation();
        if (graph == null || actor == null || protocol == null)
        {
            FaultSelectedMerchantDailyTrade(protocol);
            return false;
        }
        if (!TryCreateSelectedP12MerchantDailyTradeSectionIds(
                graph, actor, out IReadOnlyList<string> sectionIds))
        {
            FaultSelectedMerchantDailyTrade(protocol);
            return false;
        }

        SelectedP12MerchantDailyTradeContext context = null;
        IDisposable protocolScope = null;
        lock (operationSyncRoot)
        {
            if (!HasAdmittedSelectedDailyExecution
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(selectedRuntimeOwnerGraph, graph)
                || selectedP12MerchantDailyTradeContext != null
                || selectedP12TravelPartyAdvanceContext != null
                || selectedP12TravelPartyStartContext != null
                || selectedP12SoloTravelStartContext != null
                || !AreRequiredSelectedP12Sections(sectionIds))
            {
                FaultSelectedMerchantDailyTrade(protocol);
                return false;
            }

            try
            {
                if (!protocol.TryValidateOwnerSections(graph, actor, sectionIds)
                    || !protocol.TryValidateUnchangedSections(sectionIds)
                    || !protocol.TryValidateMutationEpochCapacity()
                    || !protocol.TryEnterOperation(out protocolScope)
                    || protocolScope == null)
                {
                    FaultSelectedMerchantDailyTrade(protocol);
                    IDisposable rejectedScope = protocolScope;
                    protocolScope = null;
                    try { rejectedScope?.Dispose(); }
                    catch { FaultSelectedMerchantDailyTrade(protocol); }
                    return false;
                }

                context = new SelectedP12MerchantDailyTradeContext(
                    graph, actor, protocol, sectionIds, protocolScope);
                protocolScope = null;
                selectedP12MerchantDailyTradeContext = context;
            }
            catch
            {
                FaultSelectedMerchantDailyTrade(protocol);
                IDisposable failedScope = protocolScope;
                protocolScope = null;
                try { failedScope?.Dispose(); }
                catch { FaultSelectedMerchantDailyTrade(protocol); }
                return false;
            }
        }

        try
        {
            return operation();
        }
        finally
        {
            ExitSelectedP12MerchantDailyTrade(context);
        }
    }

    internal bool TryCanCommitSelectedP12MerchantDailyTradeMutation(
        IEnumerable<string> sectionIds,
        out bool handled)
    {
        lock (operationSyncRoot)
        {
            SelectedP12MerchantDailyTradeContext context = selectedP12MerchantDailyTradeContext;
            handled = context != null;
            if (context == null) return true;
            if (!IsCurrentSelectedMerchantDailyTradeContext(context)
                || !TryGetRequestedMerchantDailyTradeSections(context, sectionIds,
                    out HashSet<string> requested))
            {
                FaultSelectedMerchantDailyTrade(context.Protocol);
                return false;
            }

            requested.ExceptWith(context.ChangedSectionIds);
            if (requested.Count == 0) return true;
            try
            {
                if (context.Protocol.TryValidateUnchangedSections(ToOrderedSectionArray(requested)))
                    return true;
            }
            catch { }
            FaultSelectedMerchantDailyTrade(context.Protocol);
            return false;
        }
    }

    internal bool TryRecordSelectedP12MerchantDailyTradeMutation(
        IEnumerable<string> sectionIds,
        out bool handled)
    {
        lock (operationSyncRoot)
        {
            SelectedP12MerchantDailyTradeContext context = selectedP12MerchantDailyTradeContext;
            handled = context != null;
            if (context == null) return true;
            if (!IsCurrentSelectedMerchantDailyTradeContext(context)
                || !TryGetRequestedMerchantDailyTradeSections(context, sectionIds,
                    out HashSet<string> changed))
            {
                FaultSelectedMerchantDailyTrade(context.Protocol);
                return false;
            }
            context.ChangedSectionIds.UnionWith(changed);
            return true;
        }
    }

    private static bool TryGetRequestedMerchantDailyTradeSections(
        SelectedP12MerchantDailyTradeContext context,
        IEnumerable<string> sectionIds,
        out HashSet<string> requested)
    {
        requested = new HashSet<string>(StringComparer.Ordinal);
        if (context == null || sectionIds == null) return false;
        try
        {
            foreach (string sectionId in sectionIds)
                if (string.IsNullOrWhiteSpace(sectionId)
                    || !requested.Add(sectionId)
                    || !context.AllowedSectionIds.Contains(sectionId))
                    return false;
        }
        catch { return false; }
        return requested.Count != 0;
    }

    private bool IsCurrentSelectedMerchantDailyTradeContext(
        SelectedP12MerchantDailyTradeContext context) =>
        context != null
        && ReferenceEquals(selectedP12MerchantDailyTradeContext, context)
        && context.IsOwnedByCurrentThread()
        && HasAdmittedSelectedDailyExecution
        && AdmissionContext != null
        && AdmissionContext.IsOwnedByCurrentThread()
        && ReferenceEquals(selectedRuntimeOwnerGraph, context.Graph)
        && TryCreateSelectedP12MerchantDailyTradeSectionIds(
            context.Graph, context.Actor, out IReadOnlyList<string> currentSectionIds)
        && HaveExactSelectedP12SectionSet(
            currentSectionIds, ToOrderedSectionArray(context.AllowedSectionIds));

    private void ExitSelectedP12MerchantDailyTrade(
        SelectedP12MerchantDailyTradeContext context)
    {
        lock (operationSyncRoot)
        {
            try
            {
                if (!IsCurrentSelectedMerchantDailyTradeContext(context))
                    FaultSelectedMerchantDailyTrade(context?.Protocol);
                else if (context.ChangedSectionIds.Count != 0
                    && !context.Protocol.NotifyCommittedMutations(
                        ToOrderedSectionArray(context.ChangedSectionIds)))
                    FaultSelectedMerchantDailyTrade(context.Protocol);
            }
            catch { FaultSelectedMerchantDailyTrade(context?.Protocol); }
            finally
            {
                if (ReferenceEquals(selectedP12MerchantDailyTradeContext, context))
                    selectedP12MerchantDailyTradeContext = null;
                IDisposable protocolScope = context?.ProtocolScope;
                if (context != null) context.ProtocolScope = null;
                try { protocolScope?.Dispose(); }
                catch { FaultSelectedMerchantDailyTrade(context?.Protocol); }
            }
        }
    }

    private static void FaultSelectedMerchantDailyTrade(
        IDailyV1SelectedP12MerchantDailyTradeProtocol protocol)
    {
        try { protocol?.FaultClosed(); }
        catch { }
    }

    /// <summary>
    /// Executes the P12 TravelParty batch using section identities derived
    /// from the exact Core owner graph. The census adapter validates those
    /// same owners and supplies only the registered protocol scope; this
    /// state owns the serialized batch and its committed-section set.
    /// </summary>
    internal bool ExecuteSelectedP12TravelPartyAdvance(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IDailyV1SelectedP12TravelPartyAdvanceProtocol protocol,
        Func<bool> operation)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (operation == null) throw new ArgumentNullException(nameof(operation));

        // Unadmitted owner-graph tests and legacy composition retain their
        // existing no-census behavior. A live selected session always takes
        // the Core-owned branch below.
        if (!HasAdmittedSelectedDailyExecution)
            return operation();
        if (protocol == null) throw new ArgumentNullException(nameof(protocol));

        IReadOnlyList<string> sectionIds = CreateSelectedTravelPartyAdvanceSectionIds(graph);
        IDisposable runtimeProtocolScope = null;
        SelectedP12TravelPartyAdvanceContext context = null;
        lock (operationSyncRoot)
        {
            if (!HasAdmittedSelectedDailyExecution
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(selectedRuntimeOwnerGraph, graph)
                || selectedP12TravelPartyAdvanceContext != null
                || mutationEpochState.HasActiveReservation)
            {
                FaultSelectedTravelPartyAdvance(protocol);
                return false;
            }

            try
            {
                if (!TryCaptureTravelPartyStoreFacts(
                        graph, out int travelPartyCardinalityBefore,
                        out long travelPartyRevisionBefore)
                    || !TryCaptureSelectedTravelPartyCoreOwnerSectionBaselines(
                        graph, sectionIds, selectedRuntimeIdAllocator, selectedRecordSequence,
                        out Dictionary<string, SelectedP12OwnerSectionBaseline> coreBaselinesBefore)
                    || !protocol.TryValidateOwnerSections(graph, sectionIds)
                    || !protocol.TryValidateUnchangedSections(sectionIds)
                    || !TryCaptureTravelPartyStoreFacts(
                        graph, out int travelPartyCardinalityAfter,
                        out long travelPartyRevisionAfter)
                    || !TryCaptureSelectedTravelPartyCoreOwnerSectionBaselines(
                        graph, sectionIds, selectedRuntimeIdAllocator, selectedRecordSequence,
                        out Dictionary<string, SelectedP12OwnerSectionBaseline> coreBaselinesAfter)
                    || travelPartyCardinalityAfter != travelPartyCardinalityBefore
                    || travelPartyRevisionAfter != travelPartyRevisionBefore
                    || !HaveSameSelectedP12OwnerSectionBaselines(
                        coreBaselinesBefore, coreBaselinesAfter)
                    || !TryBeginRuntimeProtocolOperation(
                        DailyV1RuntimeProtocolOperationContracts.TravelPartyAdvance))
                {
                    FaultSelectedTravelPartyAdvance(protocol);
                    return false;
                }

                runtimeProtocolScope = new SelectedP12TravelPartyRuntimeProtocolScope(
                    this, protocol);
                context = new SelectedP12TravelPartyAdvanceContext(
                    graph, protocol, sectionIds, coreBaselinesAfter,
                    travelPartyCardinalityAfter, travelPartyRevisionAfter,
                    runtimeProtocolScope);
                selectedP12TravelPartyAdvanceContext = context;
            }
            catch
            {
                FaultSelectedTravelPartyAdvance(protocol);
                IDisposable failedScope = runtimeProtocolScope;
                runtimeProtocolScope = null;
                try { failedScope?.Dispose(); }
                catch { FaultSelectedTravelPartyAdvance(protocol); }
                return false;
            }
        }

        try
        {
            return operation();
        }
        finally
        {
            ExitSelectedP12TravelPartyAdvance(context);
        }
    }

    /// <summary>
    /// Begins the selected solo-travel P12 operation for one exact Core NPC.
    /// Core derives the complete touched-section set from its active owner
    /// graph, verifies the host's census preflight against that set, and owns
    /// the serialized scope and committed-section notification.
    /// </summary>
    internal bool TryBeginSelectedP12SoloTravelStart(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        IReadOnlyList<string> hostPreflightSectionIds,
        IDailyV1SelectedP12SoloTravelStartProtocol protocol,
        out IDisposable operationScope)
    {
        operationScope = null;
        if (graph == null || actor == null || hostPreflightSectionIds == null || protocol == null)
            return false;

        if (!TryCreateSelectedP12SoloTravelStartSectionIds(
                graph, actor, out IReadOnlyList<string> expectedSectionIds))
        {
            FaultSelectedSoloTravelStart(protocol);
            return false;
        }

        HashSet<string> supplied = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (string sectionId in hostPreflightSectionIds)
                if (string.IsNullOrWhiteSpace(sectionId) || !supplied.Add(sectionId))
                    throw new InvalidOperationException();
        }
        catch
        {
            FaultSelectedSoloTravelStart(protocol);
            return false;
        }

        bool exactSet = supplied.Count == expectedSectionIds.Count;
        foreach (string sectionId in expectedSectionIds)
            exactSet &= supplied.Contains(sectionId);
        if (!exactSet)
        {
            FaultSelectedSoloTravelStart(protocol);
            return false;
        }

        IDisposable protocolScope = null;
        SelectedP12SoloTravelStartContext context = null;
        lock (operationSyncRoot)
        {
            if (!HasAdmittedSelectedDailyExecution
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(selectedRuntimeOwnerGraph, graph)
                || selectedP12SoloTravelStartContext != null
                || selectedP12TravelPartyAdvanceContext != null
                || activeOperationCount <= 0
                || !AreRequiredSelectedP12Sections(expectedSectionIds))
            {
                FaultSelectedSoloTravelStart(protocol);
                return false;
            }

            try
            {
                if (!TryCaptureSelectedSoloTravelStartCoreOwnerSectionBaselines(
                        graph, actor, selectedRuntimeIdAllocator, selectedRecordSequence,
                        expectedSectionIds,
                        out Dictionary<string, SelectedP12OwnerSectionBaseline> coreBaselinesBefore)
                    || !protocol.TryValidateUnchangedSections(expectedSectionIds)
                    || !TryCaptureSelectedSoloTravelStartCoreOwnerSectionBaselines(
                        graph, actor, selectedRuntimeIdAllocator, selectedRecordSequence,
                        expectedSectionIds,
                        out Dictionary<string, SelectedP12OwnerSectionBaseline> coreBaselinesAfter)
                    || !HaveSameSelectedP12OwnerSectionBaselines(
                        coreBaselinesBefore, coreBaselinesAfter)
                    || !protocol.TryValidateMutationEpochCapacity()
                    || !protocol.TryEnterOperation(out protocolScope)
                    || protocolScope == null)
                {
                    FaultSelectedSoloTravelStart(protocol);
                    IDisposable rejectedScope = protocolScope;
                    protocolScope = null;
                    try { rejectedScope?.Dispose(); }
                    catch { FaultSelectedSoloTravelStart(protocol); }
                    return false;
                }

                context = new SelectedP12SoloTravelStartContext(
                    graph, actor, protocol, expectedSectionIds, coreBaselinesAfter, protocolScope);
                selectedP12SoloTravelStartContext = context;
            }
            catch
            {
                FaultSelectedSoloTravelStart(protocol);
                IDisposable failedScope = protocolScope;
                protocolScope = null;
                try { failedScope?.Dispose(); }
                catch { FaultSelectedSoloTravelStart(protocol); }
                return false;
            }
        }

        operationScope = new SelectedP12SoloTravelStartScope(this, context);
        return true;
    }

    /// <summary>
    /// Core's operation-specific census cut for selected solo travel start.
    /// It covers only the selected actor's account/travel/plan/knowledge
    /// owners, its current important-city membership when present, and the
    /// shared event allocator and record sequence touched by this operation.
    /// </summary>
    internal bool TryCreateSelectedP12SoloTravelStartSectionIds(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        out IReadOnlyList<string> sectionIds)
    {
        sectionIds = null;
        if (graph == null || actor == null
            || !HasAdmittedSelectedDailyExecution
            || AdmissionContext == null
            || !AdmissionContext.IsOwnedByCurrentThread()
            || !ReferenceEquals(selectedRuntimeOwnerGraph, graph)
            || !graph.ContainsExactNpcOwner(actor)
            || !ReferenceEquals(actor.Definitions, graph.Definitions)
            || string.IsNullOrWhiteSpace(actor.RuntimeId)
            || actor.MoneyAccount == null || actor.Travel == null
            || actor.TravelPlan == null || actor.SpatialKnowledge == null
            || selectedRuntimeIdAllocator == null || selectedRecordSequence == null
            || selectedRuntimeActorRoster == null)
            return false;

        int matchingRosterEntries = 0;
        foreach (DailyV1ActorTurnRosterProjection entry in selectedRuntimeActorRoster)
            if (entry != null
                && string.Equals(entry.RuntimeId, actor.RuntimeId, StringComparison.Ordinal))
                matchingRosterEntries++;
        if (matchingRosterEntries != 1) return false;

        List<string> ids = new List<string>
        {
            DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + actor.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix + actor.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, actor.RuntimeId),
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + actor.RuntimeId,
            DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + actor.RuntimeId,
            RuntimeIdAllocatorCensusProvider.EventsSectionId,
            SimulationRecordSequenceCensusProvider.SectionId
        };

        string currentCityRuntimeId = actor.Travel.CurrentCityRuntimeId;
        if (!string.IsNullOrWhiteSpace(currentCityRuntimeId))
        {
            DailyV1CityRuntimeOwners currentCity = null;
            foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            {
                if (city == null || !string.Equals(
                        city.RuntimeId, currentCityRuntimeId, StringComparison.Ordinal))
                    continue;
                if (currentCity != null) return false;
                currentCity = city;
            }

            if (currentCity == null || currentCity.NpcPresence == null) return false;
            if (currentCity.NpcPresence.Contains(actor.RuntimeId))
                ids.Add(DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(
                    currentCity.RuntimeId));
        }

        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in ids)
            if (string.IsNullOrWhiteSpace(id) || !unique.Add(id)) return false;
        ids.Sort(StringComparer.Ordinal);
        sectionIds = Array.AsReadOnly(ids.ToArray());
        return true;
    }

    /// <summary>
    /// Core derives the exact P12 owner footprint for a selected TravelParty
    /// start. The Unity path compares its existing prepared roster against this
    /// set; mixed or unselected rosters retain the established compatibility path.
    /// </summary>
    internal bool TryCreateSelectedP12TravelPartyStartSectionIds(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1NpcRuntimeOwners> members,
        DailyV1CityRuntimeOwners originCity,
        out IReadOnlyList<string> sectionIds)
    {
        sectionIds = null;
        if (graph == null || members == null || members.Count == 0
            || !HasAdmittedSelectedDailyExecution
            || AdmissionContext == null || !AdmissionContext.IsOwnedByCurrentThread()
            || !ReferenceEquals(selectedRuntimeOwnerGraph, graph)
            || graph.Npcs == null || graph.Cities == null || graph.TravelParties == null
            || !ReferenceEquals(graph.RuntimeIdAllocator, selectedRuntimeIdAllocator)
            || selectedRuntimeIdAllocator == null || selectedRecordSequence == null
            || selectedRuntimeActorRoster == null
            || (originCity != null && (!graph.ContainsExactCityOwner(originCity)
                || originCity.NpcPresence == null)))
            return false;

        List<string> ids = new List<string>
        {
            TravelPartyCensusProvider.SectionId,
            RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId,
            RuntimeIdAllocatorCensusProvider.EventsSectionId,
            SimulationRecordSequenceCensusProvider.SectionId
        };
        HashSet<string> memberIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> cityPresenceSectionIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<DailyV1NpcRuntimeOwners> memberOwners = new HashSet<DailyV1NpcRuntimeOwners>();
        foreach (DailyV1NpcRuntimeOwners member in members)
        {
            if (member == null || !graph.ContainsExactNpcOwner(member)
                || !ReferenceEquals(member.Definitions, graph.Definitions)
                || string.IsNullOrWhiteSpace(member.RuntimeId)
                || !memberIds.Add(member.RuntimeId) || !memberOwners.Add(member)
                || member.MoneyAccount == null || member.Travel == null
                || member.SpatialKnowledge == null)
                return false;

            int matchingRosterEntries = 0;
            foreach (DailyV1ActorTurnRosterProjection entry in selectedRuntimeActorRoster)
                if (entry != null && string.Equals(
                        entry.RuntimeId, member.RuntimeId, StringComparison.Ordinal))
                    matchingRosterEntries++;
            if (matchingRosterEntries != 1) return false;

            ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + member.RuntimeId);
            ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix + member.RuntimeId);

            ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + member.RuntimeId);
            ids.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + member.RuntimeId);

            int currentCityMatches = 0;
            foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            {
                if (city == null || !graph.ContainsExactCityOwner(city)
                    || city.NpcPresence == null
                    || !string.Equals(city.NpcPresence.CityRuntimeId,
                        city.RuntimeId, StringComparison.Ordinal))
                    return false;
                if (!city.NpcPresence.Contains(member.RuntimeId)) continue;
                currentCityMatches++;
                string cityPresenceSectionId = DailyV1SelectedOwnerSectionCensusContract
                    .CityImportantNpcSectionIdFor(city.RuntimeId);
                if (cityPresenceSectionIds.Add(cityPresenceSectionId))
                    ids.Add(cityPresenceSectionId);
            }
            if (currentCityMatches > 1) return false;
        }

        // The existing start transaction includes the prepared origin City
        // presence row even when no member currently has a residence there.
        if (originCity != null)
        {
            string originPresenceSectionId = DailyV1SelectedOwnerSectionCensusContract
                .CityImportantNpcSectionIdFor(originCity.RuntimeId);
            if (cityPresenceSectionIds.Add(originPresenceSectionId))
                ids.Add(originPresenceSectionId);
        }

        HashSet<string> unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in ids)
            if (string.IsNullOrWhiteSpace(id) || !unique.Add(id)) return false;
        ids.Sort(StringComparer.Ordinal);
        sectionIds = Array.AsReadOnly(ids.ToArray());
        return true;
    }

    /// <summary>
    /// Opens the Core-owned selected TravelParty-start P12 transaction. Core
    /// derives and verifies the complete owner-section cut, captures exact
    /// selected-owner baselines, serializes the operation, and owns committed
    /// section receipts through scope disposal.
    /// </summary>
    internal bool TryBeginSelectedP12TravelPartyStart(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1NpcRuntimeOwners> members,
        DailyV1CityRuntimeOwners originCity,
        IReadOnlyList<string> hostPreflightSectionIds,
        IDailyV1SelectedP12TravelPartyStartProtocol protocol,
        out IDisposable operationScope)
    {
        operationScope = null;
        if (graph == null || members == null || hostPreflightSectionIds == null || protocol == null)
            return false;

        if (!TryCreateSelectedP12TravelPartyStartSectionIds(
                graph, members, originCity, out IReadOnlyList<string> expectedSectionIds)
            || !HaveExactSelectedP12SectionSet(expectedSectionIds, hostPreflightSectionIds))
        {
            FaultSelectedTravelPartyStart(protocol);
            return false;
        }

        IDisposable protocolScope = null;
        SelectedP12TravelPartyStartContext context = null;
        lock (operationSyncRoot)
        {
            if (!HasAdmittedSelectedDailyExecution
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(selectedRuntimeOwnerGraph, graph)
                || selectedP12TravelPartyStartContext != null
                || selectedP12TravelPartyAdvanceContext != null
                || selectedP12SoloTravelStartContext != null
                || mutationEpochState.HasActiveReservation
                || !AreRequiredSelectedP12Sections(expectedSectionIds))
            {
                FaultSelectedTravelPartyStart(protocol);
                return false;
            }

            try
            {
                if (!TryCaptureSelectedTravelPartyStartCoreOwnerSectionBaselines(
                        graph, members, originCity, selectedRuntimeIdAllocator, selectedRecordSequence,
                        expectedSectionIds,
                        out Dictionary<string, SelectedP12OwnerSectionBaseline> coreBaselinesBefore)
                    || !protocol.TryValidateUnchangedSections(expectedSectionIds)
                    || !protocol.TryValidateMutationEpochCapacity()
                    || !TryCaptureSelectedTravelPartyStartCoreOwnerSectionBaselines(
                        graph, members, originCity, selectedRuntimeIdAllocator, selectedRecordSequence,
                        expectedSectionIds,
                        out Dictionary<string, SelectedP12OwnerSectionBaseline> coreBaselinesAfter)
                    || !HaveSameSelectedP12OwnerSectionBaselines(
                        coreBaselinesBefore, coreBaselinesAfter)
                    || !protocol.TryEnterOperation(out protocolScope)
                    || protocolScope == null)
                {
                    FaultSelectedTravelPartyStart(protocol);
                    IDisposable rejectedScope = protocolScope;
                    protocolScope = null;
                    try { rejectedScope?.Dispose(); }
                    catch { FaultSelectedTravelPartyStart(protocol); }
                    return false;
                }

                context = new SelectedP12TravelPartyStartContext(
                    graph, members, selectedRecordSequence, protocol,
                    expectedSectionIds, coreBaselinesAfter, protocolScope);
                selectedP12TravelPartyStartContext = context;
            }
            catch
            {
                FaultSelectedTravelPartyStart(protocol);
                IDisposable failedScope = protocolScope;
                protocolScope = null;
                try { failedScope?.Dispose(); }
                catch { FaultSelectedTravelPartyStart(protocol); }
                return false;
            }
        }

        operationScope = new SelectedP12TravelPartyStartScope(this, context);
        return true;
    }

    internal bool TryCanCommitSelectedP12TravelPartyStartMutation(
        IEnumerable<string> sectionIds,
        out bool handled)
    {
        lock (operationSyncRoot)
        {
            SelectedP12TravelPartyStartContext context = selectedP12TravelPartyStartContext;
            handled = context != null;
            if (context == null) return true;
            if (!IsCurrentSelectedTravelPartyStartContext(context)
                || !TryGetRequestedTravelPartyStartSections(context, sectionIds,
                    out HashSet<string> requested)
                || !TryValidateSelectedTravelPartyStartCoreOwnerRowsUnchanged(
                    context, requested, out HashSet<string> coreOwnedRows))
            {
                FaultSelectedTravelPartyStart(context.Protocol);
                return false;
            }

            requested.ExceptWith(coreOwnedRows);
            requested.RemoveWhere(context.ChangedSectionIds.Contains);
            if (requested.Count == 0) return true;
            try
            {
                if (context.Protocol.TryValidateUnchangedSections(ToOrderedSectionArray(requested)))
                    return true;
            }
            catch { }
            FaultSelectedTravelPartyStart(context.Protocol);
            return false;
        }
    }

    internal bool TryRecordSelectedP12TravelPartyStartMutation(
        IEnumerable<string> sectionIds,
        out bool handled)
    {
        lock (operationSyncRoot)
        {
            SelectedP12TravelPartyStartContext context = selectedP12TravelPartyStartContext;
            handled = context != null;
            if (context == null) return true;
            if (!IsCurrentSelectedTravelPartyStartContext(context)
                || !TryGetRequestedTravelPartyStartSections(context, sectionIds,
                    out HashSet<string> changed)
                || !TryCaptureSelectedTravelPartyStartCoreOwnerMutationBaselines(
                    context, changed,
                    out Dictionary<string, OwnerSectionCensusWitness> committedWitnesses,
                    out SelectedP12TravelPartyStoreReceipt travelPartyStoreReceipt))
            {
                FaultSelectedTravelPartyStart(context.Protocol);
                return false;
            }

            foreach (KeyValuePair<string, OwnerSectionCensusWitness> pair in committedWitnesses)
            {
                SelectedP12OwnerSectionBaseline baseline = context.CoreOwnerSectionBaselines[pair.Key];
                baseline.Cardinality = pair.Value.Cardinality;
                baseline.Revision = pair.Value.Revision;
            }
            if (travelPartyStoreReceipt == SelectedP12TravelPartyStoreReceipt.Added)
                context.TravelPartyStoreAdditionReceipted = true;
            else if (travelPartyStoreReceipt == SelectedP12TravelPartyStoreReceipt.CompensatingRemoval)
                context.TravelPartyStoreCompensationReceipted = true;
            context.ChangedSectionIds.UnionWith(changed);
            return true;
        }
    }

    private bool IsCurrentSelectedTravelPartyStartContext(
        SelectedP12TravelPartyStartContext context) =>
        context != null
        && ReferenceEquals(selectedP12TravelPartyStartContext, context)
        && context.IsOwnedByCurrentThread()
        && HasAdmittedSelectedDailyExecution
        && AdmissionContext != null
        && AdmissionContext.IsOwnedByCurrentThread()
        && ReferenceEquals(selectedRuntimeOwnerGraph, context.Graph)
        && ReferenceEquals(selectedRecordSequence, context.RecordSequence)
        && context.MemberRuntimeIds.Count > 0;

    private static bool TryGetRequestedTravelPartyStartSections(
        SelectedP12TravelPartyStartContext context,
        IEnumerable<string> sectionIds,
        out HashSet<string> requested)
    {
        requested = new HashSet<string>(StringComparer.Ordinal);
        if (context == null || sectionIds == null) return false;
        try
        {
            foreach (string sectionId in sectionIds)
                if (string.IsNullOrWhiteSpace(sectionId)
                    || !requested.Add(sectionId)
                    || !context.AllowedSectionIds.Contains(sectionId))
                    return false;
        }
        catch { return false; }
        return requested.Count != 0;
    }

    private void ExitSelectedP12TravelPartyStart(
        SelectedP12TravelPartyStartContext context)
    {
        lock (operationSyncRoot)
        {
            try
            {
                if (!IsCurrentSelectedTravelPartyStartContext(context)
                    || !TryValidateSelectedTravelPartyStartAllOwnerBaselines(context))
                    FaultSelectedTravelPartyStart(context?.Protocol);
                else if (context.ChangedSectionIds.Count != 0
                    && !context.Protocol.NotifyCommittedMutations(
                        ToOrderedSectionArray(context.ChangedSectionIds)))
                    FaultSelectedTravelPartyStart(context.Protocol);
            }
            catch { FaultSelectedTravelPartyStart(context?.Protocol); }
            finally
            {
                if (ReferenceEquals(selectedP12TravelPartyStartContext, context))
                    selectedP12TravelPartyStartContext = null;
                IDisposable protocolScope = context?.ProtocolScope;
                if (context != null) context.ProtocolScope = null;
                try { protocolScope?.Dispose(); }
                catch { FaultSelectedTravelPartyStart(context?.Protocol); }
            }
        }
    }

    private static void FaultSelectedTravelPartyStart(
        IDailyV1SelectedP12TravelPartyStartProtocol protocol)
    {
        try { protocol?.FaultClosed(); }
        catch { }
    }

    private bool AreRequiredSelectedP12Sections(IReadOnlyList<string> sectionIds)
    {
        if (sectionIds == null || !expectedOwnerSectionContractInventorySealed)
            return false;
        foreach (string sectionId in sectionIds)
        {
            if (string.IsNullOrWhiteSpace(sectionId)
                || !expectedOwnerSectionContracts.TryGetValue(
                    sectionId, out DailyV1OwnerSectionContractIdentity contract)
                || contract.Role != OwnerSectionRole.Required
                || contract.SchemaVersion <= 0)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Routes a selected solo-travel owner admission through the active Core
    /// operation. A false handled flag leaves unrelated host operations on
    /// their established protocol path.
    /// </summary>
    internal bool TryCanCommitSelectedP12SoloTravelStartMutation(
        IEnumerable<string> sectionIds,
        out bool handled)
    {
        lock (operationSyncRoot)
        {
            SelectedP12SoloTravelStartContext context = selectedP12SoloTravelStartContext;
            handled = context != null;
            if (context == null) return true;
            if (!IsCurrentSelectedSoloTravelStartContext(context)
                || !TryGetRequestedSoloTravelStartSections(
                    context, sectionIds, out HashSet<string> requested))
            {
                FaultSelectedSoloTravelStart(context.Protocol);
                return false;
            }

            if (!TryValidateSelectedSoloTravelStartCoreOwnerRowsUnchanged(
                    context, requested, out HashSet<string> coreOwnedRows))
            {
                FaultSelectedSoloTravelStart(context.Protocol);
                return false;
            }

            requested.ExceptWith(coreOwnedRows);
            requested.RemoveWhere(context.ChangedSectionIds.Contains);
            string[] ordered = ToOrderedSectionArray(requested);
            if (ordered.Length == 0) return true;
            try
            {
                if (context.Protocol.TryValidateUnchangedSections(ordered)) return true;
            }
            catch { }
            FaultSelectedSoloTravelStart(context.Protocol);
            return false;
        }
    }

    /// <summary>Records a committed mutation in the active Core travel-start scope.</summary>
    internal bool TryRecordSelectedP12SoloTravelStartMutation(
        IEnumerable<string> sectionIds,
        out bool handled)
    {
        lock (operationSyncRoot)
        {
            SelectedP12SoloTravelStartContext context = selectedP12SoloTravelStartContext;
            handled = context != null;
            if (context == null) return true;
            if (!IsCurrentSelectedSoloTravelStartContext(context)
                || !TryGetRequestedSoloTravelStartSections(
                    context, sectionIds, out HashSet<string> changed))
            {
                FaultSelectedSoloTravelStart(context.Protocol);
                return false;
            }

            if (!TryCaptureSelectedSoloTravelStartCoreOwnerMutationBaselines(
                    context, changed,
                    out Dictionary<string, OwnerSectionCensusWitness> committedCoreWitnesses))
            {
                FaultSelectedSoloTravelStart(context.Protocol);
                return false;
            }

            foreach (KeyValuePair<string, OwnerSectionCensusWitness> pair in committedCoreWitnesses)
            {
                SelectedP12OwnerSectionBaseline baseline = context.CoreOwnerSectionBaselines[pair.Key];
                baseline.Cardinality = pair.Value.Cardinality;
                baseline.Revision = pair.Value.Revision;
            }

            context.ChangedSectionIds.UnionWith(changed);
            return true;
        }
    }

    private bool IsCurrentSelectedSoloTravelStartContext(
        SelectedP12SoloTravelStartContext context) =>
        context != null
        && ReferenceEquals(selectedP12SoloTravelStartContext, context)
        && context.IsOwnedByCurrentThread()
        && HasAdmittedSelectedDailyExecution
        && AdmissionContext != null
        && AdmissionContext.IsOwnedByCurrentThread()
        && ReferenceEquals(selectedRuntimeOwnerGraph, context.Graph)
        && context.Graph.ContainsExactNpcOwner(context.Actor);

    private static bool TryGetRequestedSoloTravelStartSections(
        SelectedP12SoloTravelStartContext context,
        IEnumerable<string> sectionIds,
        out HashSet<string> requested)
    {
        requested = new HashSet<string>(StringComparer.Ordinal);
        if (sectionIds == null) return false;
        try
        {
            foreach (string sectionId in sectionIds)
                if (string.IsNullOrWhiteSpace(sectionId)
                    || !requested.Add(sectionId)
                    || !context.AllowedSectionIds.Contains(sectionId))
                    return false;
        }
        catch { return false; }
        return requested.Count != 0;
    }

    private void ExitSelectedP12SoloTravelStart(
        SelectedP12SoloTravelStartContext context)
    {
        lock (operationSyncRoot)
        {
            try
            {
                if (!IsCurrentSelectedSoloTravelStartContext(context))
                    FaultSelectedSoloTravelStart(context?.Protocol);
                else if (context.ChangedSectionIds.Count != 0
                    && !context.Protocol.NotifyCommittedMutations(
                        ToOrderedSectionArray(context.ChangedSectionIds)))
                    FaultSelectedSoloTravelStart(context.Protocol);
            }
            catch { FaultSelectedSoloTravelStart(context?.Protocol); }
            finally
            {
                if (ReferenceEquals(selectedP12SoloTravelStartContext, context))
                    selectedP12SoloTravelStartContext = null;
                IDisposable protocolScope = context?.ProtocolScope;
                if (context != null) context.ProtocolScope = null;
                try { protocolScope?.Dispose(); }
                catch { FaultSelectedSoloTravelStart(context?.Protocol); }
            }
        }
    }

    private static void FaultSelectedSoloTravelStart(
        IDailyV1SelectedP12SoloTravelStartProtocol protocol)
    {
        try { protocol?.FaultClosed(); }
        catch { }
    }

    /// <summary>
    /// Routes selected owner P12 admission through the active Core party
    /// operation. A false handled flag means this session has no such scope,
    /// allowing unrelated or legacy operations to use their existing path.
    /// </summary>
    internal bool TryCanCommitSelectedP12TravelPartyMutation(
        IEnumerable<string> sectionIds,
        out bool handled)
    {
        lock (operationSyncRoot)
        {
            SelectedP12TravelPartyAdvanceContext context = selectedP12TravelPartyAdvanceContext;
            handled = context != null;
            if (context == null) return true;
            if (!IsCurrentSelectedTravelPartyAdvanceContext(context)
                || !TryGetRequestedTravelPartySections(context, sectionIds, out HashSet<string> requested))
            {
                FaultSelectedTravelPartyAdvance(context.Protocol);
                return false;
            }

            if (requested.Contains(TravelPartyCensusProvider.SectionId))
            {
                if (!TryCaptureTravelPartyStoreFacts(
                        context.Graph, out int currentCardinality,
                        out long currentRevision)
                    || currentCardinality != context.CoreTravelPartyCardinality
                    || currentRevision != context.CoreTravelPartyRevision)
                {
                    FaultSelectedTravelPartyAdvance(context.Protocol);
                    return false;
                }

                // The Core graph owns this exact store and can validate its
                // local census directly. Core validates its selected
                // travel/spatial and City presence rows below. Compatibility-roster
                // and other rows remain under the host's exact live census protocol.
                requested.Remove(TravelPartyCensusProvider.SectionId);
            }
            if (!TryValidateSelectedP12CoreOwnerRowsUnchanged(
                    context, requested, out HashSet<string> coreOwnedRows))
            {
                FaultSelectedTravelPartyAdvance(context.Protocol);
                return false;
            }
            requested.ExceptWith(coreOwnedRows);
            requested.RemoveWhere(context.ChangedSectionIds.Contains);
            if (requested.Count == 0) return true;
            string[] ordered = ToOrderedSectionArray(requested);
            if (context.Protocol.TryValidateUnchangedSections(ordered)) return true;
            FaultSelectedTravelPartyAdvance(context.Protocol);
            return false;
        }
    }

    internal bool TryRecordSelectedP12TravelPartyMutation(
        IEnumerable<string> sectionIds,
        out bool handled)
    {
        lock (operationSyncRoot)
        {
            SelectedP12TravelPartyAdvanceContext context = selectedP12TravelPartyAdvanceContext;
            handled = context != null;
            if (context == null) return true;
            if (!IsCurrentSelectedTravelPartyAdvanceContext(context)
                || !TryGetRequestedTravelPartySections(context, sectionIds, out HashSet<string> changed))
            {
                FaultSelectedTravelPartyAdvance(context.Protocol);
                return false;
            }

            int committedTravelPartyCardinality = context.CoreTravelPartyCardinality;
            long committedTravelPartyRevision = context.CoreTravelPartyRevision;
            if (changed.Contains(TravelPartyCensusProvider.SectionId))
            {
                if (!TryCaptureTravelPartyStoreFacts(
                        context.Graph, out int committedCardinality,
                        out long committedRevision)
                    || context.CoreTravelPartyRevision == long.MaxValue
                    || committedRevision != context.CoreTravelPartyRevision + 1L
                    || context.CoreTravelPartyCardinality <= 0
                    || committedCardinality != context.CoreTravelPartyCardinality - 1)
                {
                    FaultSelectedTravelPartyAdvance(context.Protocol);
                    return false;
                }
                committedTravelPartyCardinality = committedCardinality;
                committedTravelPartyRevision = committedRevision;
            }

            if (!TryCaptureSelectedP12CoreOwnerMutationBaselines(
                    context, changed,
                    out Dictionary<string, OwnerSectionCensusWitness> committedCoreWitnesses))
            {
                FaultSelectedTravelPartyAdvance(context.Protocol);
                return false;
            }

            // Advance Core-owned baselines only after every reported row in
            // this commit has passed its exact owner/revision/cardinality
            // contract. Later member transitions in this same serialized
            // batch therefore compare against the last acknowledged commit.
            context.CoreTravelPartyCardinality = committedTravelPartyCardinality;
            context.CoreTravelPartyRevision = committedTravelPartyRevision;
            foreach (KeyValuePair<string, OwnerSectionCensusWitness> pair in committedCoreWitnesses)
            {
                SelectedP12OwnerSectionBaseline baseline = context.CoreOwnerSectionBaselines[pair.Key];
                baseline.Cardinality = pair.Value.Cardinality;
                baseline.Revision = pair.Value.Revision;
            }

            context.ChangedSectionIds.UnionWith(changed);
            return true;
        }
    }

    /// <summary>
    /// Admits one Core-owned NPC current-action slot write against the exact
    /// Required P12 section registered for that owner. Unity supplies the
    /// existing live census adapter; the action stage calls this before the
    /// Core slot revision changes.
    /// </summary>
    internal bool TryCanCommitSelectedNpcActionSlotMutation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners owner,
        IDailyV1SelectedP12NpcActionOwnerMutationProtocol protocol)
    {
        if (AdmissionContext == null) return true;
        if (protocol == null
            || !TryGetSelectedNpcActionSlotMutationSection(
                graph, owner, out string sectionId))
            return false;

        string[] sectionIds = { sectionId };
        try
        {
            return protocol.TryValidateOwnerSection(graph, owner, sectionId)
                && protocol.TryValidateUnchangedSections(sectionIds);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Publishes the exact current-action owner invalidation after the Core
    /// slot commit and successful host projection. A post-commit failure
    /// faults admission and revokes the current capture token.
    /// </summary>
    internal bool TryRecordSelectedNpcActionSlotMutation(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners owner,
        string sectionId,
        long committedRevision,
        IDailyV1SelectedP12NpcActionOwnerMutationProtocol protocol)
    {
        if (AdmissionContext == null) return true;
        if (protocol != null
            && TryGetSelectedNpcActionSlotMutationSection(
                graph, owner, out string expectedSectionId)
            && string.Equals(sectionId, expectedSectionId, StringComparison.Ordinal)
            && owner.ActionSlot?.Revision == committedRevision)
        {
            string[] sectionIds = { expectedSectionId };
            try
            {
                if (protocol.TryValidateOwnerSection(graph, owner, expectedSectionId)
                    && protocol.NotifyCommittedMutations(sectionIds))
                    return true;
            }
            catch { }
        }

        try { protocol?.FaultClosed(); }
        catch { }
        InvalidateCurrentCaptureToken();
        return false;
    }

    private bool TryGetSelectedNpcActionSlotMutationSection(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners owner,
        out string sectionId)
    {
        sectionId = null;
        if (graph == null || owner == null
            || string.IsNullOrWhiteSpace(owner.RuntimeId)
            || !graph.ContainsExactNpcOwner(owner)
            || owner.ActionSlot == null)
            return false;

        string candidateSectionId =
            DailyV1SelectedOwnerSectionCensusContract.NpcCurrentActionPrefix
            + owner.RuntimeId;
        lock (operationSyncRoot)
        {
            if (!HasAdmittedSelectedDailyExecution
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || activeOperationCount <= 0
                || !ReferenceEquals(selectedRuntimeOwnerGraph, graph)
                || !expectedOwnerSectionContractInventorySealed
                || !expectedOwnerSectionContracts.TryGetValue(
                    candidateSectionId,
                    out DailyV1OwnerSectionContractIdentity contract)
                || contract.Role != OwnerSectionRole.Required)
                return false;
        }

        sectionId = candidateSectionId;
        return true;
    }

    private bool IsCurrentSelectedTravelPartyAdvanceContext(
        SelectedP12TravelPartyAdvanceContext context) =>
        context != null
        && ReferenceEquals(selectedP12TravelPartyAdvanceContext, context)
        && context.IsOwnedByCurrentThread()
        && HasAdmittedSelectedDailyExecution
        && AdmissionContext != null
        && AdmissionContext.IsOwnedByCurrentThread()
        && ReferenceEquals(selectedRuntimeOwnerGraph, context.Graph);

    private static bool TryGetRequestedTravelPartySections(
        SelectedP12TravelPartyAdvanceContext context,
        IEnumerable<string> sectionIds,
        out HashSet<string> requested)
    {
        requested = new HashSet<string>(StringComparer.Ordinal);
        if (sectionIds == null) return false;
        try
        {
            foreach (string sectionId in sectionIds)
            {
                if (string.IsNullOrWhiteSpace(sectionId)
                    || !requested.Add(sectionId)
                    || !context.AllowedSectionIds.Contains(sectionId))
                    return false;
            }
        }
        catch { return false; }
        return true;
    }

    private static string[] ToOrderedSectionArray(HashSet<string> sectionIds)
    {
        string[] ordered = new string[sectionIds.Count];
        sectionIds.CopyTo(ordered);
        Array.Sort(ordered, StringComparer.Ordinal);
        return ordered;
    }

    private static bool TryCaptureSelectedTravelPartyCoreOwnerSectionBaselines(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IEnumerable<string> allowedSectionIds,
        RuntimeIdAllocator selectedAllocator,
        SimulationRecordSequence selectedRecordSequence,
        out Dictionary<string, SelectedP12OwnerSectionBaseline> baselines)
    {
        baselines = null;
        if (graph == null || graph.Npcs == null || graph.Cities == null || allowedSectionIds == null
            || selectedAllocator == null || !ReferenceEquals(graph.RuntimeIdAllocator, selectedAllocator)
            || selectedRecordSequence == null)
            return false;
        try
        {
            HashSet<string> allowed = new HashSet<string>(allowedSectionIds, StringComparer.Ordinal);
            Dictionary<string, object> expectedOwners = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
            {
                if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                    || npc.Travel == null || npc.SpatialKnowledge == null
                    || !expectedOwners.TryAdd(
                        DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, npc.RuntimeId), npc)
                    || !expectedOwners.TryAdd(
                        DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + npc.RuntimeId,
                        npc.SpatialKnowledge)
                    || !expectedOwners.TryAdd(
                        DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + npc.RuntimeId,
                        npc.SpatialKnowledge))
                    return false;
            }
            foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            {
                if (city == null || !graph.ContainsExactCityOwner(city)
                    || string.IsNullOrWhiteSpace(city.RuntimeId)
                    || city.NpcPresence == null
                    || !string.Equals(city.NpcPresence.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal)
                    || !expectedOwners.TryAdd(
                        DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId),
                        city.NpcPresence))
                    return false;
            }

            IOwnerSectionCensusProvider eventCounterProvider =
                new SelectedEventCounterCensusProvider(selectedAllocator);
            OwnerSectionCensusWitness eventCounter = eventCounterProvider.GetCurrentCensus();
            if (!expectedOwners.TryAdd(eventCounter.SectionId, eventCounter.OwnerInstanceIdentity))
                return false;
            IOwnerSectionCensusProvider recordSequenceProvider =
                new SelectedRecordSequenceCensusProvider(selectedRecordSequence);
            OwnerSectionCensusWitness recordSequence = recordSequenceProvider.GetCurrentCensus();
            if (!expectedOwners.TryAdd(recordSequence.SectionId, recordSequence.OwnerInstanceIdentity))
                return false;

            foreach (string sectionId in expectedOwners.Keys)
                if (!allowed.Contains(sectionId)) return false;

            Dictionary<string, SelectedP12OwnerSectionBaseline> captured =
                new Dictionary<string, SelectedP12OwnerSectionBaseline>(StringComparer.Ordinal);
            foreach (IOwnerSectionCensusProvider provider in
                DailyV1NpcOwnerCensusProviders.CreateTravelStateProviders(graph))
                if (!TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                        provider, expectedOwners, captured))
                    return false;
            foreach (IOwnerSectionCensusProvider provider in
                DailyV1NpcOwnerCensusProviders.CreateSpatialKnowledgeProviders(graph.Npcs))
                if (!TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                        provider, expectedOwners, captured))
                    return false;

            foreach (DailyV1CityRuntimeOwners city in graph.Cities)
                if (!TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                        new SelectedCityPresenceCensusProvider(city.NpcPresence),
                        expectedOwners, captured))
                    return false;
            if (!TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                    eventCounterProvider, expectedOwners, captured)
                || !TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                    recordSequenceProvider, expectedOwners, captured))
                return false;
            if (captured.Count != expectedOwners.Count) return false;
            baselines = captured;
            return true;
        }
        catch
        {
            baselines = null;
            return false;
        }
    }

    private static bool TryCaptureSelectedSoloTravelStartCoreOwnerSectionBaselines(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1NpcRuntimeOwners actor,
        RuntimeIdAllocator allocator,
        SimulationRecordSequence recordSequence,
        IEnumerable<string> allowedSectionIds,
        out Dictionary<string, SelectedP12OwnerSectionBaseline> baselines)
    {
        baselines = null;
        if (graph?.Npcs == null || actor == null || allowedSectionIds == null
            || !graph.ContainsExactNpcOwner(actor)
            || actor.MoneyAccount == null || actor.Travel == null || actor.TravelPlan == null
            || actor.SpatialKnowledge == null || allocator == null || recordSequence == null)
            return false;

        try
        {
            Dictionary<string, object> expectedOwners = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + actor.RuntimeId] =
                    actor.MoneyAccount,
                [DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, actor.RuntimeId)] = actor,
                [DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, actor.RuntimeId)] =
                    actor.TravelPlan,
                [DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + actor.RuntimeId] =
                    actor.SpatialKnowledge,
                [DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + actor.RuntimeId] =
                    actor.SpatialKnowledge
            };
            HashSet<string> allowed = new HashSet<string>(StringComparer.Ordinal);
            int suppliedSectionCount = 0;
            foreach (string sectionId in allowedSectionIds)
            {
                suppliedSectionCount++;
                if (string.IsNullOrWhiteSpace(sectionId) || !allowed.Add(sectionId)) return false;
            }
            if (allowed.Count != suppliedSectionCount) return false;

            IOwnerSectionCensusProvider eventCounterProvider =
                new SelectedEventCounterCensusProvider(allocator);
            OwnerSectionCensusWitness eventCounter = eventCounterProvider.GetCurrentCensus();
            IOwnerSectionCensusProvider recordSequenceProvider =
                new SelectedRecordSequenceCensusProvider(recordSequence);
            OwnerSectionCensusWitness sequence = recordSequenceProvider.GetCurrentCensus();
            expectedOwners.Add(eventCounter.SectionId, eventCounter.OwnerInstanceIdentity);
            expectedOwners.Add(sequence.SectionId, sequence.OwnerInstanceIdentity);

            string currentCityRuntimeId = actor.Travel.CurrentCityRuntimeId;
            DailyV1CityRuntimeOwners currentCity = null;
            if (!string.IsNullOrWhiteSpace(currentCityRuntimeId))
            {
                foreach (DailyV1CityRuntimeOwners city in graph.Cities)
                {
                    if (city == null || !string.Equals(
                            city.RuntimeId, currentCityRuntimeId, StringComparison.Ordinal))
                        continue;
                    if (currentCity != null) return false;
                    currentCity = city;
                }
            }
            string cityPresenceSectionId = currentCity == null ? null
                : DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(
                    currentCity.RuntimeId);
            if (cityPresenceSectionId != null && allowed.Contains(cityPresenceSectionId))
            {
                if (currentCity.NpcPresence == null
                    || !string.Equals(currentCity.NpcPresence.CityRuntimeId,
                        currentCity.RuntimeId, StringComparison.Ordinal)
                    || !currentCity.NpcPresence.Contains(actor.RuntimeId))
                    return false;
                expectedOwners.Add(cityPresenceSectionId, currentCity.NpcPresence);
            }
            if (allowed.Count != expectedOwners.Count) return false;
            foreach (string sectionId in expectedOwners.Keys)
                if (!allowed.Contains(sectionId)) return false;

            Dictionary<string, SelectedP12OwnerSectionBaseline> captured =
                new Dictionary<string, SelectedP12OwnerSectionBaseline>(StringComparer.Ordinal);
            foreach (IOwnerSectionCensusProvider provider in
                DailyV1NpcOwnerCensusProviders.CreateTravelStateProviders(graph))
            {
                OwnerSectionCensusWitness witness = provider?.GetCurrentCensus();
                if (witness != null && expectedOwners.ContainsKey(witness.SectionId)
                    && !TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                        provider, expectedOwners, captured))
                    return false;
            }
            foreach (IOwnerSectionCensusProvider provider in
                DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs))
            {
                OwnerSectionCensusWitness witness = provider?.GetCurrentCensus();
                if (witness != null && expectedOwners.ContainsKey(witness.SectionId)
                    && !TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                        provider, expectedOwners, captured))
                    return false;
            }
            foreach (IOwnerSectionCensusProvider provider in
                DailyV1NpcOwnerCensusProviders.CreateKnowledgeAndPlanProviders(graph))
            {
                OwnerSectionCensusWitness witness = provider?.GetCurrentCensus();
                if (witness != null && expectedOwners.ContainsKey(witness.SectionId)
                    && !TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                        provider, expectedOwners, captured))
                    return false;
            }
            foreach (IOwnerSectionCensusProvider provider in
                DailyV1NpcOwnerCensusProviders.CreateSpatialKnowledgeProviders(graph.Npcs))
            {
                OwnerSectionCensusWitness witness = provider?.GetCurrentCensus();
                if (witness != null && expectedOwners.ContainsKey(witness.SectionId)
                    && !TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                        provider, expectedOwners, captured))
                    return false;
            }

            if (cityPresenceSectionId != null && expectedOwners.ContainsKey(cityPresenceSectionId)
                && !TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                    new SelectedCityPresenceCensusProvider(currentCity.NpcPresence),
                    expectedOwners, captured))
                return false;
            if (!TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                    eventCounterProvider, expectedOwners, captured)
                || !TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                    recordSequenceProvider, expectedOwners, captured)
                || captured.Count != expectedOwners.Count)
                return false;

            string travelId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
                12, actor.RuntimeId);
            string moneyId = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix
                + actor.RuntimeId;
            string travelPlanId = DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind, actor.RuntimeId);
            string locationsId = DailyV1SelectedOwnerSectionCensusContract
                .NpcSpatialKnowledgeLocationsPrefix + actor.RuntimeId;
            string routesId = DailyV1SelectedOwnerSectionCensusContract
                .NpcSpatialKnowledgeRoutesPrefix + actor.RuntimeId;
            if (captured[travelId].Cardinality != 1
                || captured[moneyId].Cardinality != 1
                || captured[travelPlanId].Cardinality != 1
                || captured[eventCounter.SectionId].Cardinality != 1
                || captured[sequence.SectionId].Cardinality != 1
                || !ReferenceEquals(captured[locationsId].Owner, captured[routesId].Owner)
                || captured[locationsId].Revision != captured[routesId].Revision
                || (cityPresenceSectionId != null && expectedOwners.ContainsKey(cityPresenceSectionId)
                    && captured[cityPresenceSectionId].Cardinality <= 0))
                return false;
            baselines = captured;
            return true;
        }
        catch
        {
            baselines = null;
            return false;
        }
    }

    private static bool TryValidateSelectedSoloTravelStartCoreOwnerRowsUnchanged(
        SelectedP12SoloTravelStartContext context,
        HashSet<string> requested,
        out HashSet<string> coreOwnedRows)
    {
        coreOwnedRows = new HashSet<string>(StringComparer.Ordinal);
        if (context == null || requested == null) return false;
        HashSet<string> checkedSpatialOwners = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in requested)
        {
            if (!context.CoreOwnerSectionBaselines.TryGetValue(
                    sectionId, out SelectedP12OwnerSectionBaseline baseline))
                return false;

            if (TryGetSpatialKnowledgeSectionPair(
                    sectionId, out string runtimeId, out string locationsId, out string routesId))
            {
                if (!string.Equals(runtimeId, context.Actor.RuntimeId, StringComparison.Ordinal)
                    || !requested.Contains(locationsId) || !requested.Contains(routesId))
                    return false;
                if (!checkedSpatialOwners.Add(runtimeId)) continue;
                if (!context.CoreOwnerSectionBaselines.TryGetValue(
                        locationsId, out SelectedP12OwnerSectionBaseline locationsBaseline)
                    || !context.CoreOwnerSectionBaselines.TryGetValue(
                        routesId, out SelectedP12OwnerSectionBaseline routesBaseline)
                    || !ReferenceEquals(locationsBaseline.Owner, routesBaseline.Owner)
                    || !ReferenceEquals(locationsBaseline.Owner, context.Actor.SpatialKnowledge)
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        locationsId, locationsBaseline, out OwnerSectionCensusWitness locations)
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        routesId, routesBaseline, out OwnerSectionCensusWitness routes)
                    || locations.Revision != locationsBaseline.Revision
                    || locations.Cardinality != locationsBaseline.Cardinality
                    || routes.Revision != routesBaseline.Revision
                    || routes.Cardinality != routesBaseline.Cardinality
                    || locations.Revision != routes.Revision)
                    return false;
                coreOwnedRows.Add(locationsId);
                coreOwnedRows.Add(routesId);
                continue;
            }

            string moneyId = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix
                + context.Actor.RuntimeId;
            string travelId = DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(
                12, context.Actor.RuntimeId);
            string planId = DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
                DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind,
                context.Actor.RuntimeId);
            bool exactNpcOwner =
                string.Equals(sectionId, travelId, StringComparison.Ordinal)
                    ? ReferenceEquals(baseline.Owner, context.Actor)
                    : string.Equals(sectionId, moneyId, StringComparison.Ordinal)
                        ? ReferenceEquals(baseline.Owner, context.Actor.MoneyAccount)
                        : string.Equals(sectionId, planId, StringComparison.Ordinal)
                            ? ReferenceEquals(baseline.Owner, context.Actor.TravelPlan)
                            : false;
            bool exactRuntimeCounter =
                string.Equals(sectionId, RuntimeIdAllocatorCensusProvider.EventsSectionId,
                    StringComparison.Ordinal)
                || string.Equals(sectionId, SimulationRecordSequenceCensusProvider.SectionId,
                    StringComparison.Ordinal);
            bool exactCityPresence = TryGetSelectedSoloTravelStartCity(
                context.Graph, sectionId, out DailyV1CityRuntimeOwners city)
                && ReferenceEquals(baseline.Owner, city.NpcPresence);
            if ((!exactNpcOwner && !exactRuntimeCounter && !exactCityPresence)
                || !TryCaptureSelectedP12OwnerSectionWitness(
                    sectionId, baseline, out OwnerSectionCensusWitness current)
                || current.Cardinality != baseline.Cardinality
                || current.Revision != baseline.Revision
                || (exactCityPresence && !city.NpcPresence.Contains(context.Actor.RuntimeId)))
                return false;
            coreOwnedRows.Add(sectionId);
        }
        return true;
    }

    private static bool TryCaptureSelectedSoloTravelStartCoreOwnerMutationBaselines(
        SelectedP12SoloTravelStartContext context,
        HashSet<string> changed,
        out Dictionary<string, OwnerSectionCensusWitness> committedWitnesses)
    {
        committedWitnesses = new Dictionary<string, OwnerSectionCensusWitness>(StringComparer.Ordinal);
        if (context == null || changed == null || changed.Count == 0) return false;

        string travelSectionId =
            DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, context.Actor.RuntimeId);
        if (changed.Contains(travelSectionId))
        {
            if (!context.CoreOwnerSectionBaselines.TryGetValue(
                    travelSectionId, out SelectedP12OwnerSectionBaseline travelBaseline)
                || !ReferenceEquals(travelBaseline.Owner, context.Actor)
                || travelBaseline.Cardinality != 1
                || travelBaseline.Revision == long.MaxValue
                || !TryCaptureSelectedP12OwnerSectionWitness(
                    travelSectionId, travelBaseline, out OwnerSectionCensusWitness travel)
                || travel.Cardinality != 1
                || travel.Revision != travelBaseline.Revision + 1L)
                return false;
            committedWitnesses.Add(travelSectionId, travel);
        }

        HashSet<string> checkedSpatialOwners = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in changed)
        {
            if (!TryGetSpatialKnowledgeSectionPair(
                    sectionId, out string runtimeId, out string locationsId, out string routesId))
                continue;
            if (!string.Equals(runtimeId, context.Actor.RuntimeId, StringComparison.Ordinal)
                || !changed.Contains(locationsId) || !changed.Contains(routesId))
                return false;
            if (!checkedSpatialOwners.Add(runtimeId)) continue;

            if (!context.CoreOwnerSectionBaselines.TryGetValue(
                    locationsId, out SelectedP12OwnerSectionBaseline locationsBaseline)
                || !context.CoreOwnerSectionBaselines.TryGetValue(
                    routesId, out SelectedP12OwnerSectionBaseline routesBaseline)
                || !ReferenceEquals(locationsBaseline.Owner, routesBaseline.Owner)
                || !ReferenceEquals(locationsBaseline.Owner, context.Actor.SpatialKnowledge)
                || locationsBaseline.Revision != routesBaseline.Revision
                || locationsBaseline.Revision == long.MaxValue
                || locationsBaseline.Cardinality == int.MaxValue
                || routesBaseline.Cardinality == int.MaxValue
                || !TryCaptureSelectedP12OwnerSectionWitness(
                    locationsId, locationsBaseline, out OwnerSectionCensusWitness locations)
                || !TryCaptureSelectedP12OwnerSectionWitness(
                    routesId, routesBaseline, out OwnerSectionCensusWitness routes)
                || locations.Revision != locationsBaseline.Revision + 1L
                || routes.Revision != locations.Revision
                || locations.Cardinality < locationsBaseline.Cardinality
                || routes.Cardinality < routesBaseline.Cardinality
                || locations.Cardinality > locationsBaseline.Cardinality + 1
                || routes.Cardinality > routesBaseline.Cardinality + 1
                || ((locations.Cardinality - locationsBaseline.Cardinality)
                    + (routes.Cardinality - routesBaseline.Cardinality)) != 1)
                return false;

            committedWitnesses.Add(locationsId, locations);
            committedWitnesses.Add(routesId, routes);
        }

        string moneySectionId = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix
            + context.Actor.RuntimeId;
        string planSectionId = DailyV1SelectedOwnerSectionCensusContract.NpcPlanSectionIdFor(
            DailyV1SelectedOwnerSectionCensusContract.NpcTravelPlanKind,
            context.Actor.RuntimeId);
        foreach (string sectionId in changed)
        {
            if (committedWitnesses.ContainsKey(sectionId)) continue;
            if (TryGetSpatialKnowledgeSectionPair(
                    sectionId, out string spatialRuntimeId, out _, out _))
            {
                if (!string.Equals(spatialRuntimeId, context.Actor.RuntimeId, StringComparison.Ordinal))
                    return false;
                continue;
            }

            if (string.Equals(sectionId, travelSectionId, StringComparison.Ordinal))
                continue;

            if (string.Equals(sectionId, moneySectionId, StringComparison.Ordinal))
            {
                if (!TryCaptureSelectedSoloTravelStartSingleRevisionReceipt(
                        sectionId, context.Actor.MoneyAccount, context, out OwnerSectionCensusWitness money))
                    return false;
                committedWitnesses.Add(sectionId, money);
                continue;
            }
            if (string.Equals(sectionId, planSectionId, StringComparison.Ordinal))
            {
                if (!TryCaptureSelectedSoloTravelStartSingleRevisionReceipt(
                        sectionId, context.Actor.TravelPlan, context, out OwnerSectionCensusWitness plan))
                    return false;
                committedWitnesses.Add(sectionId, plan);
                continue;
            }
            if (string.Equals(sectionId, RuntimeIdAllocatorCensusProvider.EventsSectionId,
                    StringComparison.Ordinal)
                || string.Equals(sectionId, SimulationRecordSequenceCensusProvider.SectionId,
                    StringComparison.Ordinal))
            {
                if (!TryCaptureSelectedSoloTravelStartSingleRevisionReceipt(
                        sectionId, null, context, out OwnerSectionCensusWitness counter))
                    return false;
                committedWitnesses.Add(sectionId, counter);
                continue;
            }
            if (TryGetSelectedSoloTravelStartCity(
                    context.Graph, sectionId, out DailyV1CityRuntimeOwners city))
            {
                if (!context.CoreOwnerSectionBaselines.TryGetValue(
                        sectionId, out SelectedP12OwnerSectionBaseline cityBaseline)
                    || !ReferenceEquals(cityBaseline.Owner, city.NpcPresence)
                    || cityBaseline.Cardinality <= 0
                    || cityBaseline.Revision == long.MaxValue
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        sectionId, cityBaseline, out OwnerSectionCensusWitness presence)
                    || presence.Revision != cityBaseline.Revision + 1L
                    || presence.Cardinality != cityBaseline.Cardinality - 1
                    || city.NpcPresence.Contains(context.Actor.RuntimeId))
                    return false;
                committedWitnesses.Add(sectionId, presence);
                continue;
            }
            return false;
        }

        return committedWitnesses.Count == changed.Count;
    }

    private static bool TryCaptureSelectedSoloTravelStartSingleRevisionReceipt(
        string sectionId,
        object expectedOwner,
        SelectedP12SoloTravelStartContext context,
        out OwnerSectionCensusWitness witness)
    {
        witness = null;
        if (context == null
            || !context.CoreOwnerSectionBaselines.TryGetValue(
                sectionId, out SelectedP12OwnerSectionBaseline baseline)
            || baseline.Cardinality != 1
            || baseline.Revision == long.MaxValue
            || (expectedOwner != null && !ReferenceEquals(baseline.Owner, expectedOwner))
            || !TryCaptureSelectedP12OwnerSectionWitness(sectionId, baseline, out witness))
            return false;
        return witness.Cardinality == 1
            && witness.Revision == baseline.Revision + 1L;
    }

    private static bool TryGetSelectedSoloTravelStartCity(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string sectionId,
        out DailyV1CityRuntimeOwners city)
    {
        city = null;
        if (graph?.Cities == null || string.IsNullOrWhiteSpace(sectionId)
            || !sectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                StringComparison.Ordinal))
            return false;
        string cityRuntimeId = sectionId.Substring(
            DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix.Length);
        if (string.IsNullOrWhiteSpace(cityRuntimeId)) return false;
        foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
        {
            if (candidate == null || !string.Equals(
                    candidate.RuntimeId, cityRuntimeId, StringComparison.Ordinal))
                continue;
            if (city != null) return false;
            city = candidate;
        }
        return city != null && graph.ContainsExactCityOwner(city) && city.NpcPresence != null
            && string.Equals(city.NpcPresence.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal);
    }

    private static bool TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
        IOwnerSectionCensusProvider provider,
        IReadOnlyDictionary<string, object> expectedOwners,
        IDictionary<string, SelectedP12OwnerSectionBaseline> destination)
    {
        if (provider == null || expectedOwners == null || destination == null) return false;
        OwnerSectionCensusWitness witness;
        try { witness = provider.GetCurrentCensus(); }
        catch { return false; }
        int expectedSchemaVersion = witness == null ? 0
            : string.Equals(witness.SectionId,
                RuntimeIdAllocatorCensusProvider.EventsSectionId, StringComparison.Ordinal)
                ? RuntimeIdAllocatorCensusProvider.SchemaVersion
                : string.Equals(witness.SectionId,
                    RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId, StringComparison.Ordinal)
                    ? RuntimeIdAllocatorCensusProvider.SchemaVersion
                : string.Equals(witness.SectionId,
                    SimulationRecordSequenceCensusProvider.SectionId, StringComparison.Ordinal)
                    ? SimulationRecordSequenceCensusProvider.SchemaVersion
                    : string.Equals(witness.SectionId,
                        TravelPartyCensusProvider.SectionId, StringComparison.Ordinal)
                        ? TravelPartyCensusProvider.SchemaVersion
                    : witness.SectionId.StartsWith(
                        DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                        StringComparison.Ordinal)
                        ? DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion
                        : DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion;
        if (witness == null
            || !expectedOwners.TryGetValue(witness.SectionId, out object expectedOwner)
            || !ReferenceEquals(witness.OwnerInstanceIdentity, expectedOwner)
            || witness.SchemaVersion != expectedSchemaVersion
            || witness.Cardinality < 0
            || witness.Revision < 0L
            || destination.ContainsKey(witness.SectionId))
            return false;

        destination.Add(witness.SectionId,
            new SelectedP12OwnerSectionBaseline(provider, witness));
        return true;
    }

    private static bool HaveExactSelectedP12SectionSet(
        IReadOnlyList<string> expectedSectionIds,
        IReadOnlyList<string> suppliedSectionIds)
    {
        if (expectedSectionIds == null || suppliedSectionIds == null
            || expectedSectionIds.Count != suppliedSectionIds.Count)
            return false;
        HashSet<string> expected = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> supplied = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in expectedSectionIds)
            if (string.IsNullOrWhiteSpace(sectionId) || !expected.Add(sectionId)) return false;
        foreach (string sectionId in suppliedSectionIds)
            if (string.IsNullOrWhiteSpace(sectionId) || !supplied.Add(sectionId)) return false;
        return expected.SetEquals(supplied);
    }

    private static bool TryCaptureSelectedTravelPartyStartCoreOwnerSectionBaselines(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IReadOnlyList<DailyV1NpcRuntimeOwners> members,
        DailyV1CityRuntimeOwners originCity,
        RuntimeIdAllocator allocator,
        SimulationRecordSequence recordSequence,
        IEnumerable<string> allowedSectionIds,
        out Dictionary<string, SelectedP12OwnerSectionBaseline> baselines)
    {
        baselines = null;
        if (graph?.Npcs == null || graph.Cities == null || graph.TravelParties == null
            || members == null || members.Count == 0 || allowedSectionIds == null
            || allocator == null || !ReferenceEquals(graph.RuntimeIdAllocator, allocator)
            || recordSequence == null || (originCity != null && !graph.ContainsExactCityOwner(originCity)))
            return false;

        try
        {
            Dictionary<string, object> expectedOwners = new Dictionary<string, object>(StringComparer.Ordinal);
            if (!TryAddSelectedTravelPartyStartExpectedOwner(
                    expectedOwners, TravelPartyCensusProvider.SectionId, graph.TravelParties))
                return false;

            HashSet<string> memberIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DailyV1NpcRuntimeOwners member in members)
            {
                if (member == null || !graph.ContainsExactNpcOwner(member)
                    || !ReferenceEquals(member.Definitions, graph.Definitions)
                    || string.IsNullOrWhiteSpace(member.RuntimeId)
                    || !memberIds.Add(member.RuntimeId)
                    || member.MoneyAccount == null || member.Travel == null
                    || member.SpatialKnowledge == null
                    || !TryAddSelectedTravelPartyStartExpectedOwner(
                        expectedOwners,
                        DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + member.RuntimeId,
                        member.MoneyAccount)
                    || !TryAddSelectedTravelPartyStartExpectedOwner(
                        expectedOwners,
                        DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix + member.RuntimeId,
                        member)
                    || !TryAddSelectedTravelPartyStartExpectedOwner(
                        expectedOwners,
                        DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + member.RuntimeId,
                        member.SpatialKnowledge)
                    || !TryAddSelectedTravelPartyStartExpectedOwner(
                        expectedOwners,
                        DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + member.RuntimeId,
                        member.SpatialKnowledge))
                    return false;
            }

            foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            {
                if (city == null || !graph.ContainsExactCityOwner(city)
                    || string.IsNullOrWhiteSpace(city.RuntimeId) || city.NpcPresence == null
                    || !string.Equals(city.NpcPresence.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal))
                    return false;
                bool touched = ReferenceEquals(city, originCity);
                if (!touched)
                    foreach (DailyV1NpcRuntimeOwners member in members)
                        if (city.NpcPresence.Contains(member.RuntimeId))
                        {
                            touched = true;
                            break;
                        }
                if (touched && !TryAddSelectedTravelPartyStartExpectedOwner(
                        expectedOwners,
                        DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId),
                        city.NpcPresence))
                    return false;
            }

            SelectedTravelPartyCounterCensusProvider partyCounterProvider =
                new SelectedTravelPartyCounterCensusProvider(allocator);
            SelectedEventCounterCensusProvider eventCounterProvider =
                new SelectedEventCounterCensusProvider(allocator);
            SelectedRecordSequenceCensusProvider sequenceProvider =
                new SelectedRecordSequenceCensusProvider(recordSequence);
            OwnerSectionCensusWitness partyCounter = partyCounterProvider.GetCurrentCensus();
            OwnerSectionCensusWitness eventCounter = eventCounterProvider.GetCurrentCensus();
            OwnerSectionCensusWitness sequence = sequenceProvider.GetCurrentCensus();
            if (!TryAddSelectedTravelPartyStartExpectedOwner(
                    expectedOwners, partyCounter.SectionId, partyCounter.OwnerInstanceIdentity)
                || !TryAddSelectedTravelPartyStartExpectedOwner(
                    expectedOwners, eventCounter.SectionId, eventCounter.OwnerInstanceIdentity)
                || !TryAddSelectedTravelPartyStartExpectedOwner(
                    expectedOwners, sequence.SectionId, sequence.OwnerInstanceIdentity))
                return false;

            HashSet<string> allowed = new HashSet<string>(StringComparer.Ordinal);
            int suppliedCount = 0;
            foreach (string sectionId in allowedSectionIds)
            {
                suppliedCount++;
                if (string.IsNullOrWhiteSpace(sectionId) || !allowed.Add(sectionId)) return false;
            }
            if (allowed.Count != suppliedCount || !allowed.SetEquals(expectedOwners.Keys)) return false;

            List<IOwnerSectionCensusProvider> providers = new List<IOwnerSectionCensusProvider>
            {
                new TravelPartyCensusProvider(graph.TravelParties),
                partyCounterProvider,
                eventCounterProvider,
                sequenceProvider
            };
            providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateMoneyAccountProviders(graph.Npcs));
            providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateTravelStateProviders(graph));
            providers.AddRange(DailyV1NpcOwnerCensusProviders.CreateSpatialKnowledgeProviders(graph.Npcs));
            foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            {
                string citySectionId = DailyV1SelectedOwnerSectionCensusContract
                    .CityImportantNpcSectionIdFor(city.RuntimeId);
                if (expectedOwners.ContainsKey(citySectionId))
                    providers.Add(new SelectedCityPresenceCensusProvider(city.NpcPresence));
            }

            Dictionary<string, SelectedP12OwnerSectionBaseline> captured =
                new Dictionary<string, SelectedP12OwnerSectionBaseline>(StringComparer.Ordinal);
            foreach (IOwnerSectionCensusProvider provider in providers)
            {
                OwnerSectionCensusWitness witness;
                try { witness = provider?.GetCurrentCensus(); }
                catch { return false; }
                if (witness != null && expectedOwners.ContainsKey(witness.SectionId)
                    && !TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
                        provider, witness, expectedOwners, captured))
                    return false;
            }
            if (captured.Count != expectedOwners.Count) return false;

            OwnerSectionCensusWitness storeWitness = captured[TravelPartyCensusProvider.SectionId]
                .Provider.GetCurrentCensus();
            if (storeWitness.Cardinality != graph.TravelParties.ActiveParties.Count)
                return false;
            foreach (DailyV1NpcRuntimeOwners member in members)
            {
                string moneyId = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix + member.RuntimeId;
                string travelId = DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix + member.RuntimeId;
                string locationsId = DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + member.RuntimeId;
                string routesId = DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + member.RuntimeId;
                if (captured[moneyId].Cardinality != 1 || captured[travelId].Cardinality != 1
                    || !ReferenceEquals(captured[locationsId].Owner, member.SpatialKnowledge)
                    || !ReferenceEquals(captured[routesId].Owner, member.SpatialKnowledge)
                    || captured[locationsId].Revision != captured[routesId].Revision)
                    return false;
            }
            if (captured[partyCounter.SectionId].Cardinality != 1
                || captured[eventCounter.SectionId].Cardinality != 1
                || captured[sequence.SectionId].Cardinality != 1)
                return false;

            baselines = captured;
            return true;
        }
        catch
        {
            baselines = null;
            return false;
        }
    }

    private static bool TryAddSelectedTravelPartyStartExpectedOwner(
        IDictionary<string, object> expectedOwners,
        string sectionId,
        object owner)
    {
        if (expectedOwners == null || string.IsNullOrWhiteSpace(sectionId) || owner == null
            || expectedOwners.ContainsKey(sectionId))
            return false;
        expectedOwners.Add(sectionId, owner);
        return true;
    }

    private static bool TryCaptureSelectedTravelPartyCoreOwnerSectionBaseline(
        IOwnerSectionCensusProvider provider,
        OwnerSectionCensusWitness witness,
        IReadOnlyDictionary<string, object> expectedOwners,
        IDictionary<string, SelectedP12OwnerSectionBaseline> destination)
    {
        if (provider == null || witness == null || expectedOwners == null || destination == null)
            return false;
        int expectedSchemaVersion = string.Equals(witness.SectionId,
                RuntimeIdAllocatorCensusProvider.EventsSectionId, StringComparison.Ordinal)
                || string.Equals(witness.SectionId,
                    RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId, StringComparison.Ordinal)
            ? RuntimeIdAllocatorCensusProvider.SchemaVersion
            : string.Equals(witness.SectionId,
                SimulationRecordSequenceCensusProvider.SectionId, StringComparison.Ordinal)
                ? SimulationRecordSequenceCensusProvider.SchemaVersion
                : string.Equals(witness.SectionId,
                    TravelPartyCensusProvider.SectionId, StringComparison.Ordinal)
                    ? TravelPartyCensusProvider.SchemaVersion
                    : witness.SectionId.StartsWith(
                        DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                        StringComparison.Ordinal)
                        ? DailyV1SelectedOwnerSectionCensusContract.CitySectionSchemaVersion
                        : DailyV1SelectedOwnerSectionCensusContract.NpcSectionSchemaVersion;
        if (!expectedOwners.TryGetValue(witness.SectionId, out object expectedOwner)
            || !ReferenceEquals(witness.OwnerInstanceIdentity, expectedOwner)
            || witness.SchemaVersion != expectedSchemaVersion
            || witness.Cardinality < 0 || witness.Revision < 0L
            || destination.ContainsKey(witness.SectionId))
            return false;
        destination.Add(witness.SectionId,
            new SelectedP12OwnerSectionBaseline(provider, witness));
        return true;
    }

    private static bool TryValidateSelectedTravelPartyStartCoreOwnerRowsUnchanged(
        SelectedP12TravelPartyStartContext context,
        HashSet<string> requested,
        out HashSet<string> coreOwnedRows)
    {
        coreOwnedRows = new HashSet<string>(StringComparer.Ordinal);
        if (context == null || requested == null) return false;
        HashSet<string> checkedSpatialOwners = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in requested)
        {
            if (!context.CoreOwnerSectionBaselines.TryGetValue(
                    sectionId, out SelectedP12OwnerSectionBaseline baseline)
                || !IsExactSelectedTravelPartyStartOwner(context, sectionId, baseline))
                return false;

            if (TryGetSpatialKnowledgeSectionPair(
                    sectionId, out string runtimeId, out string locationsId, out string routesId))
            {
                if (!context.MemberRuntimeIds.Contains(runtimeId)
                    || !requested.Contains(locationsId) || !requested.Contains(routesId))
                    return false;
                if (!checkedSpatialOwners.Add(runtimeId)) continue;
                DailyV1NpcRuntimeOwners member = context.Graph.TryGetNpcOwner(runtimeId,
                    out DailyV1NpcRuntimeOwners exactMember) ? exactMember : null;
                if (member == null || !context.MemberRuntimeIds.Contains(member.RuntimeId)
                    || !context.CoreOwnerSectionBaselines.TryGetValue(
                        locationsId, out SelectedP12OwnerSectionBaseline locationsBaseline)
                    || !context.CoreOwnerSectionBaselines.TryGetValue(
                        routesId, out SelectedP12OwnerSectionBaseline routesBaseline)
                    || !ReferenceEquals(locationsBaseline.Owner, member.SpatialKnowledge)
                    || !ReferenceEquals(routesBaseline.Owner, member.SpatialKnowledge)
                    || locationsBaseline.Revision != routesBaseline.Revision
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        locationsId, locationsBaseline, out OwnerSectionCensusWitness locations)
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        routesId, routesBaseline, out OwnerSectionCensusWitness routes)
                    || locations.Revision != locationsBaseline.Revision
                    || locations.Cardinality != locationsBaseline.Cardinality
                    || routes.Revision != routesBaseline.Revision
                    || routes.Cardinality != routesBaseline.Cardinality
                    || locations.Revision != routes.Revision)
                    return false;
                coreOwnedRows.Add(locationsId);
                coreOwnedRows.Add(routesId);
                continue;
            }

            if (!TryCaptureSelectedP12OwnerSectionWitness(
                    sectionId, baseline, out OwnerSectionCensusWitness current)
                || current.Cardinality != baseline.Cardinality
                || current.Revision != baseline.Revision)
                return false;
            coreOwnedRows.Add(sectionId);
        }
        return true;
    }

    private static bool IsExactSelectedTravelPartyStartOwner(
        SelectedP12TravelPartyStartContext context,
        string sectionId,
        SelectedP12OwnerSectionBaseline baseline)
    {
        if (context == null || string.IsNullOrWhiteSpace(sectionId) || baseline == null)
            return false;
        if (string.Equals(sectionId, TravelPartyCensusProvider.SectionId, StringComparison.Ordinal))
            return ReferenceEquals(baseline.Owner, context.Graph.TravelParties);
        if (string.Equals(sectionId,
                RuntimeIdAllocatorCensusProvider.TravelPartiesSectionId, StringComparison.Ordinal))
            return context.Graph.RuntimeIdAllocator != null
                && ReferenceEquals(baseline.Owner,
                    context.Graph.RuntimeIdAllocator.CensusOwnerIdentity);
        if (string.Equals(sectionId,
                RuntimeIdAllocatorCensusProvider.EventsSectionId, StringComparison.Ordinal))
            return context.Graph.RuntimeIdAllocator != null
                && ReferenceEquals(baseline.Owner,
                    context.Graph.RuntimeIdAllocator.CensusOwnerIdentity);
        if (string.Equals(sectionId, SimulationRecordSequenceCensusProvider.SectionId,
                StringComparison.Ordinal))
            return ReferenceEquals(baseline.Owner, context.RecordSequence.CensusOwnerIdentity);

        string moneyPrefix = DailyV1SelectedOwnerSectionCensusContract.NpcMoneyAccountPrefix;
        if (sectionId.StartsWith(moneyPrefix, StringComparison.Ordinal))
        {
            string runtimeId = sectionId.Substring(moneyPrefix.Length);
            return context.MemberRuntimeIds.Contains(runtimeId)
                && context.Graph.TryGetNpcOwner(runtimeId, out DailyV1NpcRuntimeOwners member)
                && ReferenceEquals(baseline.Owner, member.MoneyAccount);
        }

        string travelPrefix = DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix;
        if (sectionId.StartsWith(travelPrefix, StringComparison.Ordinal))
        {
            string runtimeId = sectionId.Substring(travelPrefix.Length);
            return context.MemberRuntimeIds.Contains(runtimeId)
                && context.Graph.TryGetNpcOwner(runtimeId, out DailyV1NpcRuntimeOwners member)
                && ReferenceEquals(baseline.Owner, member);
        }

        if (sectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                StringComparison.Ordinal))
            return TryGetSelectedTravelPartyCityPresenceOwner(
                context.Graph, sectionId, baseline.Owner, out _);

        return TryGetSpatialKnowledgeSectionPair(
                sectionId, out string spatialRuntimeId, out _, out _)
            && context.MemberRuntimeIds.Contains(spatialRuntimeId)
            && context.Graph.TryGetNpcOwner(spatialRuntimeId, out DailyV1NpcRuntimeOwners spatialMember)
            && ReferenceEquals(baseline.Owner, spatialMember.SpatialKnowledge);
    }

    private static bool TryCaptureSelectedTravelPartyStartCoreOwnerMutationBaselines(
        SelectedP12TravelPartyStartContext context,
        HashSet<string> changed,
        out Dictionary<string, OwnerSectionCensusWitness> committedWitnesses,
        out SelectedP12TravelPartyStoreReceipt travelPartyStoreReceipt)
    {
        committedWitnesses = new Dictionary<string, OwnerSectionCensusWitness>(StringComparer.Ordinal);
        travelPartyStoreReceipt = SelectedP12TravelPartyStoreReceipt.None;
        if (context == null || changed == null || changed.Count == 0) return false;
        HashSet<string> checkedSpatialOwners = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in changed)
        {
            if (!context.CoreOwnerSectionBaselines.TryGetValue(
                    sectionId, out SelectedP12OwnerSectionBaseline baseline)
                || !IsExactSelectedTravelPartyStartOwner(context, sectionId, baseline))
                return false;

            if (TryGetSpatialKnowledgeSectionPair(
                    sectionId, out string runtimeId, out string locationsId, out string routesId))
            {
                if (!context.MemberRuntimeIds.Contains(runtimeId)
                    || !changed.Contains(locationsId) || !changed.Contains(routesId))
                    return false;
                if (!checkedSpatialOwners.Add(runtimeId)) continue;
                if (!context.CoreOwnerSectionBaselines.TryGetValue(
                        locationsId, out SelectedP12OwnerSectionBaseline locationsBaseline)
                    || !context.CoreOwnerSectionBaselines.TryGetValue(
                        routesId, out SelectedP12OwnerSectionBaseline routesBaseline)
                    || locationsBaseline.Revision != routesBaseline.Revision
                    || locationsBaseline.Revision == long.MaxValue
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        locationsId, locationsBaseline, out OwnerSectionCensusWitness locations)
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        routesId, routesBaseline, out OwnerSectionCensusWitness routes))
                    return false;

                int locationDelta = locations.Cardinality - locationsBaseline.Cardinality;
                int routeDelta = routes.Cardinality - routesBaseline.Cardinality;
                if (locations.Revision != locationsBaseline.Revision + 1L
                    || routes.Revision != locations.Revision
                    || (locationDelta != 0 && locationDelta != 1)
                    || (routeDelta != 0 && routeDelta != 1)
                    || locationDelta + routeDelta == 0)
                    return false;
                committedWitnesses.Add(locationsId, locations);
                committedWitnesses.Add(routesId, routes);
                continue;
            }

            if (baseline.Revision == long.MaxValue
                || !TryCaptureSelectedP12OwnerSectionWitness(
                    sectionId, baseline, out OwnerSectionCensusWitness current)
                || current.Revision != baseline.Revision + 1L)
                return false;

            if (string.Equals(sectionId, TravelPartyCensusProvider.SectionId, StringComparison.Ordinal))
            {
                bool added = baseline.Cardinality < int.MaxValue
                    && current.Cardinality == baseline.Cardinality + 1
                    && !context.TravelPartyStoreAdditionReceipted
                    && !context.TravelPartyStoreCompensationReceipted;
                bool compensatedRemoval = baseline.Cardinality > 0
                    && current.Cardinality == baseline.Cardinality - 1
                    && context.TravelPartyStoreAdditionReceipted
                    && !context.TravelPartyStoreCompensationReceipted
                    && current.Cardinality == context.InitialTravelPartyStoreCardinality;
                if (!added && !compensatedRemoval)
                    return false;
                travelPartyStoreReceipt = added
                    ? SelectedP12TravelPartyStoreReceipt.Added
                    : SelectedP12TravelPartyStoreReceipt.CompensatingRemoval;
            }
            else if (sectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                StringComparison.Ordinal))
            {
                bool added = baseline.Cardinality < int.MaxValue
                    && current.Cardinality == baseline.Cardinality + 1;
                bool removed = baseline.Cardinality > 0
                    && current.Cardinality == baseline.Cardinality - 1;
                if (!added && !removed) return false;
            }
            else if (current.Cardinality != 1)
                return false;

            committedWitnesses.Add(sectionId, current);
        }
        return true;
    }

    private static bool TryValidateSelectedTravelPartyStartAllOwnerBaselines(
        SelectedP12TravelPartyStartContext context)
    {
        if (context?.CoreOwnerSectionBaselines == null) return false;
        foreach (KeyValuePair<string, SelectedP12OwnerSectionBaseline> pair
            in context.CoreOwnerSectionBaselines)
        {
            if (!IsExactSelectedTravelPartyStartOwner(context, pair.Key, pair.Value)
                || !TryCaptureSelectedP12OwnerSectionWitness(
                    pair.Key, pair.Value, out OwnerSectionCensusWitness current)
                || current.Cardinality != pair.Value.Cardinality
                || current.Revision != pair.Value.Revision)
                return false;
        }
        return true;
    }

    private static bool HaveSameSelectedP12OwnerSectionBaselines(
        IReadOnlyDictionary<string, SelectedP12OwnerSectionBaseline> left,
        IReadOnlyDictionary<string, SelectedP12OwnerSectionBaseline> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        foreach (KeyValuePair<string, SelectedP12OwnerSectionBaseline> pair in left)
        {
            if (!right.TryGetValue(pair.Key, out SelectedP12OwnerSectionBaseline other)
                || !ReferenceEquals(pair.Value.Owner, other.Owner)
                || pair.Value.SchemaVersion != other.SchemaVersion
                || pair.Value.Cardinality != other.Cardinality
                || pair.Value.Revision != other.Revision)
                return false;
        }
        return true;
    }

    private static bool TryCaptureSelectedP12OwnerSectionWitness(
        string sectionId,
        SelectedP12OwnerSectionBaseline baseline,
        out OwnerSectionCensusWitness witness)
    {
        witness = null;
        if (string.IsNullOrWhiteSpace(sectionId) || baseline == null) return false;
        try { witness = baseline.Provider.GetCurrentCensus(); }
        catch { return false; }
        return witness != null
            && string.Equals(witness.SectionId, sectionId, StringComparison.Ordinal)
            && witness.SchemaVersion == baseline.SchemaVersion
            && ReferenceEquals(witness.OwnerInstanceIdentity, baseline.Owner)
            && witness.Cardinality >= 0
            && witness.Revision >= 0L;
    }

    private static bool TryValidateSelectedP12CoreOwnerRowsUnchanged(
        SelectedP12TravelPartyAdvanceContext context,
        HashSet<string> requested,
        out HashSet<string> coreOwnedRows)
    {
        coreOwnedRows = new HashSet<string>(StringComparer.Ordinal);
        if (context == null || requested == null) return false;
        HashSet<string> checkedSpatialOwners = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in requested)
        {
            if (!context.CoreOwnerSectionBaselines.TryGetValue(
                    sectionId, out SelectedP12OwnerSectionBaseline baseline))
                continue;

            if (TryGetSpatialKnowledgeSectionPair(
                    sectionId, out string runtimeId, out string locationsId, out string routesId))
            {
                if (!requested.Contains(locationsId) || !requested.Contains(routesId)) return false;
                if (!checkedSpatialOwners.Add(runtimeId)) continue;
                if (!context.CoreOwnerSectionBaselines.TryGetValue(
                        locationsId, out SelectedP12OwnerSectionBaseline locationsBaseline)
                    || !context.CoreOwnerSectionBaselines.TryGetValue(
                        routesId, out SelectedP12OwnerSectionBaseline routesBaseline)
                    || !ReferenceEquals(locationsBaseline.Owner, routesBaseline.Owner)
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        locationsId, locationsBaseline, out OwnerSectionCensusWitness locations)
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        routesId, routesBaseline, out OwnerSectionCensusWitness routes)
                    || locations.Revision != locationsBaseline.Revision
                    || locations.Cardinality != locationsBaseline.Cardinality
                    || routes.Revision != routesBaseline.Revision
                    || routes.Cardinality != routesBaseline.Cardinality
                    || locations.Revision != routes.Revision)
                    return false;
                coreOwnedRows.Add(locationsId);
                coreOwnedRows.Add(routesId);
                continue;
            }

            if (sectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                    StringComparison.Ordinal)
                && !TryGetSelectedTravelPartyCityPresenceOwner(
                    context.Graph, sectionId, baseline.Owner, out _))
                return false;

            if (!TryCaptureSelectedP12OwnerSectionWitness(
                    sectionId, baseline, out OwnerSectionCensusWitness current)
                || current.Cardinality != baseline.Cardinality
                || current.Revision != baseline.Revision)
                return false;
            coreOwnedRows.Add(sectionId);
        }
        return true;
    }

    private static bool TryCaptureSelectedP12CoreOwnerMutationBaselines(
        SelectedP12TravelPartyAdvanceContext context,
        HashSet<string> changed,
        out Dictionary<string, OwnerSectionCensusWitness> committedWitnesses)
    {
        committedWitnesses = new Dictionary<string, OwnerSectionCensusWitness>(StringComparer.Ordinal);
        if (context == null || changed == null) return false;
        HashSet<string> checkedSpatialOwners = new HashSet<string>(StringComparer.Ordinal);
        foreach (string sectionId in changed)
        {
            if (!context.CoreOwnerSectionBaselines.TryGetValue(
                    sectionId, out SelectedP12OwnerSectionBaseline baseline))
                continue;

            if (TryGetSpatialKnowledgeSectionPair(
                    sectionId, out string runtimeId, out string locationsId, out string routesId))
            {
                if (!changed.Contains(locationsId) || !changed.Contains(routesId)) return false;
                if (!checkedSpatialOwners.Add(runtimeId)) continue;
                if (!context.CoreOwnerSectionBaselines.TryGetValue(
                        locationsId, out SelectedP12OwnerSectionBaseline locationsBaseline)
                    || !context.CoreOwnerSectionBaselines.TryGetValue(
                        routesId, out SelectedP12OwnerSectionBaseline routesBaseline)
                    || !ReferenceEquals(locationsBaseline.Owner, routesBaseline.Owner)
                    || locationsBaseline.Revision != routesBaseline.Revision
                    || locationsBaseline.Revision == long.MaxValue
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        locationsId, locationsBaseline, out OwnerSectionCensusWitness locations)
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        routesId, routesBaseline, out OwnerSectionCensusWitness routes)
                    || locations.Revision != locationsBaseline.Revision + 1L
                    || routes.Revision != locations.Revision
                    || locations.Cardinality != locationsBaseline.Cardinality + 1
                    || routes.Cardinality != routesBaseline.Cardinality)
                    return false;
                committedWitnesses.Add(locationsId, locations);
                committedWitnesses.Add(routesId, routes);
                continue;
            }

            if (string.Equals(sectionId, RuntimeIdAllocatorCensusProvider.EventsSectionId,
                    StringComparison.Ordinal)
                || string.Equals(sectionId, SimulationRecordSequenceCensusProvider.SectionId,
                    StringComparison.Ordinal))
            {
                if (baseline.Cardinality != 1
                    || baseline.Revision == long.MaxValue
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        sectionId, baseline, out OwnerSectionCensusWitness sequenceOwner)
                    || sequenceOwner.Cardinality != 1
                    || sequenceOwner.Revision != baseline.Revision + 1L)
                    return false;
                committedWitnesses.Add(sectionId, sequenceOwner);
                continue;
            }

            if (sectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                    StringComparison.Ordinal))
            {
                if (!TryGetSelectedTravelPartyCityPresenceOwner(
                        context.Graph, sectionId, baseline.Owner, out _)
                    || baseline.Revision == long.MaxValue
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        sectionId, baseline, out OwnerSectionCensusWitness presence))
                    return false;

                bool addedMember = baseline.Cardinality < int.MaxValue
                    && presence.Cardinality == baseline.Cardinality + 1;
                bool removedMember = baseline.Cardinality > 0
                    && presence.Cardinality == baseline.Cardinality - 1;
                if (presence.Revision != baseline.Revision + 1L
                    || (!addedMember && !removedMember))
                    return false;

                committedWitnesses.Add(sectionId, presence);
                continue;
            }

            if (sectionId.StartsWith(
                    DailyV1SelectedOwnerSectionCensusContract.NpcTravelStatePrefix,
                    StringComparison.Ordinal))
            {
                if (baseline.Cardinality != 1
                    || baseline.Revision == long.MaxValue
                    || !TryCaptureSelectedP12OwnerSectionWitness(
                        sectionId, baseline, out OwnerSectionCensusWitness travel)
                    || travel.Cardinality != 1
                    || travel.Revision != baseline.Revision + 1L)
                    return false;
                committedWitnesses.Add(sectionId, travel);
            }
        }
        return true;
    }

    private static bool TryGetSelectedTravelPartyCityPresenceOwner(
        DailyV1SelectedRuntimeOwnerGraph graph,
        string sectionId,
        object expectedOwner,
        out DailyV1CityNpcPresenceOwner presenceOwner)
    {
        presenceOwner = null;
        if (graph?.Cities == null || expectedOwner == null
            || string.IsNullOrWhiteSpace(sectionId)
            || !sectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix,
                StringComparison.Ordinal))
            return false;

        string cityRuntimeId = sectionId.Substring(
            DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionPrefix.Length);
        if (string.IsNullOrWhiteSpace(cityRuntimeId)) return false;

        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (city == null || !graph.ContainsExactCityOwner(city)
                || string.IsNullOrWhiteSpace(city.RuntimeId)
                || city.NpcPresence == null
                || !string.Equals(city.NpcPresence.CityRuntimeId, city.RuntimeId, StringComparison.Ordinal))
                return false;
            if (!string.Equals(city.RuntimeId, cityRuntimeId, StringComparison.Ordinal)) continue;
            if (presenceOwner != null || !ReferenceEquals(city.NpcPresence, expectedOwner))
                return false;
            presenceOwner = city.NpcPresence;
        }
        return presenceOwner != null;
    }

    private static bool TryGetSpatialKnowledgeSectionPair(
        string sectionId,
        out string runtimeId,
        out string locationsId,
        out string routesId)
    {
        runtimeId = null;
        locationsId = null;
        routesId = null;
        if (string.IsNullOrEmpty(sectionId)) return false;
        string suffix;
        if (sectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix,
                StringComparison.Ordinal))
            suffix = sectionId.Substring(
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix.Length);
        else if (sectionId.StartsWith(
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix,
                StringComparison.Ordinal))
            suffix = sectionId.Substring(
                DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix.Length);
        else
            return false;
        if (string.IsNullOrWhiteSpace(suffix)) return false;
        runtimeId = suffix;
        locationsId = DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + suffix;
        routesId = DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + suffix;
        return true;
    }

    private IReadOnlyList<string> CreateSelectedTravelPartyAdvanceSectionIds(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph == null || graph.TravelParties == null)
            throw new InvalidOperationException("The selected TravelParty store is not part of the Core owner graph.");
        if (selectedRuntimeActorRoster == null)
            throw new InvalidOperationException("The Core-owned ordered runtime roster is not bound.");

        List<string> sectionIds = new List<string>
        {
            TravelPartyCensusProvider.SectionId
        };
        List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>(graph.Cities);
        cities.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));
        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (city?.NpcPresence == null)
                throw new InvalidOperationException("The selected City presence owner is missing from the Core graph.");
            sectionIds.Add(DailyV1SelectedOwnerSectionCensusContract.CityImportantNpcSectionIdFor(city.RuntimeId));
        }

        Dictionary<string, DailyV1NpcRuntimeOwners> selectedNpcsByRuntimeId =
            new Dictionary<string, DailyV1NpcRuntimeOwners>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || npc.Travel == null || npc.SpatialKnowledge == null
                || !selectedNpcsByRuntimeId.TryAdd(npc.RuntimeId, npc))
                throw new InvalidOperationException("The selected NPC travel and spatial owners are incomplete.");
        }

        // The selected owner graph is intentionally a subset of the live host
        // roster when compatibility NPCs are present. The Core-bound ordered
        // roster carries the exact full census identities, including those
        // unselected runtime members; P12 admission must cover every live
        // travel and spatial section touched by the batch.
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> graphRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1ActorTurnRosterProjection member in selectedRuntimeActorRoster)
        {
            if (member == null) continue;
            if (string.IsNullOrWhiteSpace(member.RuntimeId) || !runtimeIds.Add(member.RuntimeId))
                throw new InvalidOperationException("The Core-owned runtime roster contains an invalid or duplicate NPC identity.");

            if (selectedNpcsByRuntimeId.TryGetValue(member.RuntimeId, out DailyV1NpcRuntimeOwners selectedNpc))
            {
                if (!graphRuntimeIds.Add(member.RuntimeId)
                    || selectedNpc.Travel == null || selectedNpc.SpatialKnowledge == null)
                    throw new InvalidOperationException("The selected NPC owner graph does not match its Core runtime roster identity.");
            }

            sectionIds.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSectionIdFor(12, member.RuntimeId));
            sectionIds.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeLocationsPrefix + member.RuntimeId);
            sectionIds.Add(DailyV1SelectedOwnerSectionCensusContract.NpcSpatialKnowledgeRoutesPrefix + member.RuntimeId);
        }
        if (graphRuntimeIds.Count != selectedNpcsByRuntimeId.Count)
            throw new InvalidOperationException("The Core runtime roster omits a selected NPC owner graph identity.");

        if (selectedRuntimeIdAllocator == null || selectedRecordSequence == null)
            throw new InvalidOperationException("The selected allocator or record-sequence owner is not bound.");
        sectionIds.Add(RuntimeIdAllocatorCensusProvider.EventsSectionId);
        sectionIds.Add(SimulationRecordSequenceCensusProvider.SectionId);
        if (new HashSet<string>(sectionIds, StringComparer.Ordinal).Count != sectionIds.Count)
            throw new InvalidOperationException("The selected TravelParty P12 section inventory contains duplicate identities.");
        return Array.AsReadOnly(sectionIds.ToArray());
    }

    private static bool TryCaptureTravelPartyStoreFacts(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out int cardinality,
        out long revision)
    {
        cardinality = 0;
        revision = 0L;
        TravelPartyStore store = graph?.TravelParties;
        if (store == null) return false;

        try
        {
            using (store.EnterReadWindow())
            {
                return store.ValidateCensus(out cardinality, out revision)
                    && cardinality >= 0
                    && revision >= 0L;
            }
        }
        catch
        {
            cardinality = 0;
            revision = 0L;
            return false;
        }
    }

    private void ExitSelectedP12TravelPartyAdvance(
        SelectedP12TravelPartyAdvanceContext context)
    {
        lock (operationSyncRoot)
        {
            try
            {
                if (!IsCurrentSelectedTravelPartyAdvanceContext(context))
                    FaultSelectedTravelPartyAdvance(context?.Protocol);
                else if (context.ChangedSectionIds.Count != 0
                    && !context.Protocol.NotifyCommittedMutations(
                        ToOrderedSectionArray(context.ChangedSectionIds)))
                    FaultSelectedTravelPartyAdvance(context.Protocol);
            }
            catch { FaultSelectedTravelPartyAdvance(context?.Protocol); }
            finally
            {
                if (ReferenceEquals(selectedP12TravelPartyAdvanceContext, context))
                    selectedP12TravelPartyAdvanceContext = null;
                IDisposable runtimeProtocolScope = context?.RuntimeProtocolScope;
                if (context != null) context.RuntimeProtocolScope = null;
                try { runtimeProtocolScope?.Dispose(); }
                catch { FaultSelectedTravelPartyAdvance(context?.Protocol); }
            }
        }
    }

    private static void FaultSelectedTravelPartyAdvance(
        IDailyV1SelectedP12TravelPartyAdvanceProtocol protocol)
    {
        try { protocol?.FaultClosed(); }
        catch { }
    }

    /// <summary>
    /// Binds the host's registered census protocol to this exact admitted Core
    /// execution session. The execution state constructs and retains the one
    /// coordinator used by both daily reservations and selected owner writes.
    /// </summary>
    internal bool TryBindSelectedP12CrimeJusticeMutationProtocol(
        IDailyV1P12CrimeJusticeMutationProtocol protocol,
        DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1P12CrimeJusticeMutationCoordinator coordinator)
    {
        if (protocol == null) throw new ArgumentNullException(nameof(protocol));
        coordinator = null;
        lock (operationSyncRoot)
        {
            if (!HasAdmittedSelectedDailyExecution
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(selectedRuntimeOwnerGraph, graph))
            {
                protocol.FaultClosed();
                return false;
            }

            if (selectedP12CrimeJusticeMutationCoordinator == null)
            {
                selectedP12CrimeJusticeMutationCoordinator =
                    new DailyV1P12CrimeJusticeMutationCoordinator(
                        () => HasAdmittedSelectedDailyExecution,
                        () => AdmissionContext != null
                            && AdmissionContext.IsOwnedByCurrentThread(),
                        protocol.FaultClosed,
                        protocol);
                selectedP12CrimeJusticeMutationProtocol = protocol;
                coordinator = selectedP12CrimeJusticeMutationCoordinator;
                return true;
            }

            if (ReferenceEquals(selectedP12CrimeJusticeMutationProtocol, protocol))
            {
                coordinator = selectedP12CrimeJusticeMutationCoordinator;
                return true;
            }

            try { selectedP12CrimeJusticeMutationProtocol?.FaultClosed(); }
            catch { }
            try { protocol.FaultClosed(); }
            catch { }
            return false;
        }
    }

    /// <summary>
    /// Pins the selected Crime/Justice host compatibility identities and their
    /// Core owner relationships to this admitted execution session. Later
    /// phases must present the same bindings; the operation boundary consumes
    /// this retained value rather than selecting identities from Unity again.
    /// </summary>
    internal bool TryBindSelectedCrimeJusticeDailyMutationHostBinding(
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding)
    {
        if (graph == null || binding == null
            || !binding.HasExactCoreOwnerRelationships(graph))
        {
            FaultSelectedCrimeJusticeBinding(binding);
            MarkSelectedRuntimeCensusProtocolFaulted();
            return false;
        }

        DailyV1SelectedCrimeJusticeDailyMutationHostBinding prior;
        bool mismatch;
        lock (operationSyncRoot)
        {
            bool exactSession = HasAdmittedSelectedDailyExecution
                && AdmissionContext?.IsOwnedByCurrentThread() == true
                && ReferenceEquals(selectedRuntimeOwnerGraph, graph)
                && ReferenceEquals(binding.JusticeRecordsOwner, graph.JusticeRecordsOwner);
            prior = selectedCrimeJusticeDailyMutationHostBinding;
            mismatch = !exactSession
                || (prior != null && !prior.HasSameSessionBinding(binding));
            if (!mismatch && prior == null)
                selectedCrimeJusticeDailyMutationHostBinding = binding;
        }

        if (!mismatch) return true;

        FaultSelectedCrimeJusticeBinding(prior);
        FaultSelectedCrimeJusticeBinding(binding);
        MarkSelectedRuntimeCensusProtocolFaulted();
        return false;
    }

    internal bool TryGetSelectedCrimeJusticeDailyMutationHostBinding(
        DailyV1SelectedRuntimeOwnerGraph graph,
        out DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding)
    {
        lock (operationSyncRoot)
        {
            binding = selectedCrimeJusticeDailyMutationHostBinding;
            if (!HasAdmittedSelectedDailyExecution
                || AdmissionContext?.IsOwnedByCurrentThread() != true
                || !ReferenceEquals(selectedRuntimeOwnerGraph, graph)
                || binding == null
                || !binding.HasExactCoreOwnerRelationships(graph))
            {
                binding = null;
                return false;
            }
            return true;
        }
    }

    private static void FaultSelectedCrimeJusticeBinding(
        DailyV1SelectedCrimeJusticeDailyMutationHostBinding binding)
    {
        try { binding?.Protocol?.FaultClosed(); }
        catch { }
    }

    /// <summary>
    /// Compatibility assertion for existing internal callers. The supplied
    /// coordinator must be the instance created and retained by this Core
    /// execution state; hosts cannot inject or replace it.
    /// </summary>
    internal bool TryBindSelectedP12CrimeJusticeMutationCoordinator(
        IDailyV1P12CrimeJusticeMutationProtocol protocol,
        DailyV1P12CrimeJusticeMutationCoordinator coordinator,
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (!TryBindSelectedP12CrimeJusticeMutationProtocol(
                protocol, graph,
                out DailyV1P12CrimeJusticeMutationCoordinator coreCoordinator))
            return false;
        if (ReferenceEquals(coreCoordinator, coordinator)) return true;
        protocol.FaultClosed();
        return false;
    }

    /// <summary>Admits a selected NPC Crime/Justice owner leaf through the
    /// coordinator already pinned to this execution session.</summary>
    internal void EnsureSelectedP12CrimeJusticeOwnerMutationAllowed(
        DailyV1NpcRuntimeOwners owner)
    {
        DailyV1SelectedRuntimeOwnerGraph graph = selectedRuntimeOwnerGraph;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator =
            selectedP12CrimeJusticeMutationCoordinator;
        string sectionId = owner == null ? null
            : DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(owner.RuntimeId);
        if (!HasAdmittedSelectedDailyExecution
            || AdmissionContext == null
            || !AdmissionContext.IsOwnedByCurrentThread()
            || graph == null
            || owner == null
            || !graph.ContainsExactNpcOwner(owner)
            || !ReferenceEquals(owner.Definitions, graph.Definitions)
            || !owner.CanAdvanceCrimeJusticeRevision(1)
            || coordinator == null
            || !coordinator.CanCommit(owner, sectionId))
        {
            if (coordinator != null) coordinator.FaultSelected(this);
            throw new InvalidOperationException(
                "The selected Core NPC Crime/Justice mutation is outside its exact admitted P12 owner scope.");
        }
    }

    /// <summary>Commits the same NPC Crime/Justice revision and section
    /// invalidation previously forwarded through the Unity runtime wrapper.</summary>
    internal void NotifySelectedP12CrimeJusticeOwnerMutationCommitted(
        DailyV1NpcRuntimeOwners owner)
    {
        DailyV1SelectedRuntimeOwnerGraph graph = selectedRuntimeOwnerGraph;
        DailyV1P12CrimeJusticeMutationCoordinator coordinator =
            selectedP12CrimeJusticeMutationCoordinator;
        if (!HasAdmittedSelectedDailyExecution
            || AdmissionContext == null
            || !AdmissionContext.IsOwnedByCurrentThread()
            || graph == null
            || owner == null
            || !graph.ContainsExactNpcOwner(owner)
            || !ReferenceEquals(owner.Definitions, graph.Definitions)
            || coordinator == null)
        {
            if (coordinator != null) coordinator.FaultSelected(this);
            throw new InvalidOperationException(
                "The selected Core NPC Crime/Justice owner changed outside its exact admitted P12 session.");
        }

        owner.NotifyCrimeJusticeMutationCommitted();
        coordinator.NotifyCommitted(
            owner,
            DailyV1SelectedOwnerSectionCensusContract.NpcStatusCrimeSectionIdFor(owner.RuntimeId));
    }
    /// <summary>
    /// Owns selected Crime/Justice daily reservation state for this exact Core
    /// execution session. The host supplies only the protocol that reads the
    /// registered continuation census; the coordinator and its active scope
    /// remain attached to this Core state.
    /// </summary>
    internal void ExecuteSelectedP12CrimeJusticeDailyMutation(
        IDailyV1P12CrimeJusticeMutationProtocol protocol,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1SelectedCrimeJusticeDailyPhase phase,
        object crimeOwnerIdentity,
        object crimeReceiptCensusOwnerIdentity,
        object justiceOwnerIdentity,
        object justiceReceiptCensusOwnerIdentity,
        bool includeCrimeReceiptSection,
        bool includeJusticeReceiptSection,
        Action mutation)
    {
        if (protocol == null) throw new ArgumentNullException(nameof(protocol));
        if (!TryBindSelectedP12CrimeJusticeMutationProtocol(
                protocol, graph, out DailyV1P12CrimeJusticeMutationCoordinator coordinator))
            throw new InvalidOperationException(
                "The P12 Crime/Justice protocol is not bound to this admitted Core session and owner thread.");
        DailyV1P12CrimeJusticeMutationCoordinator boundCoordinator;
        lock (operationSyncRoot)
        {
            if (!HasAdmittedSelectedDailyExecution
                || AdmissionContext == null
                || !AdmissionContext.IsOwnedByCurrentThread()
                || !ReferenceEquals(selectedRuntimeOwnerGraph, graph)
                || !ReferenceEquals(selectedP12CrimeJusticeMutationCoordinator, coordinator)
                || !ReferenceEquals(selectedP12CrimeJusticeMutationProtocol, protocol))
            {
                protocol.FaultClosed();
                throw new InvalidOperationException(
                    "The P12 Crime/Justice operation is not bound to this admitted Core session, its exact census protocol, and its owner thread.");
            }

            if (selectedP12CrimeJusticeMutationCoordinator == null)
            {
                selectedP12CrimeJusticeMutationCoordinator = coordinator;
                selectedP12CrimeJusticeMutationProtocol = protocol;
            }
            else if (!ReferenceEquals(selectedP12CrimeJusticeMutationCoordinator, coordinator)
                || !ReferenceEquals(selectedP12CrimeJusticeMutationProtocol, protocol))
            {
                protocol.FaultClosed();
                throw new InvalidOperationException(
                    "The Core execution session cannot replace the P12 Crime/Justice coordinator or protocol already bound to its writers.");
            }

            boundCoordinator = selectedP12CrimeJusticeMutationCoordinator;
        }

        if (!boundCoordinator.CanBeginSelectedDailyMutation(this, graph))
            throw new InvalidOperationException(
                "The Core-owned P12 Crime/Justice daily operation could not begin.");

        boundCoordinator.ExecuteSelectedDailyMutation(
            this,
            graph,
            phase,
            crimeOwnerIdentity,
            crimeReceiptCensusOwnerIdentity,
            justiceOwnerIdentity,
            justiceReceiptCensusOwnerIdentity,
            includeCrimeReceiptSection,
            includeJusticeReceiptSection,
            mutation);
    }

    /// <summary>
    /// Compatibility overload that verifies a caller retained this session's
    /// exact Core-created coordinator before delegating to the session-owned
    /// daily operation.
    /// </summary>
    internal void ExecuteSelectedP12CrimeJusticeDailyMutation(
        IDailyV1P12CrimeJusticeMutationProtocol protocol,
        DailyV1P12CrimeJusticeMutationCoordinator coordinator,
        DailyV1SelectedRuntimeOwnerGraph graph,
        DailyV1SelectedCrimeJusticeDailyPhase phase,
        object crimeOwnerIdentity,
        object crimeReceiptCensusOwnerIdentity,
        object justiceOwnerIdentity,
        object justiceReceiptCensusOwnerIdentity,
        bool includeCrimeReceiptSection,
        bool includeJusticeReceiptSection,
        Action mutation)
    {
        if (!TryBindSelectedP12CrimeJusticeMutationCoordinator(
                protocol, coordinator, graph))
            throw new InvalidOperationException(
                "A selected P12 Crime/Justice mutation cannot replace its Core-owned coordinator or protocol.");
        ExecuteSelectedP12CrimeJusticeDailyMutation(
            protocol, graph, phase, crimeOwnerIdentity,
            crimeReceiptCensusOwnerIdentity, justiceOwnerIdentity,
            justiceReceiptCensusOwnerIdentity, includeCrimeReceiptSection,
            includeJusticeReceiptSection, mutation);
    }
    /// <summary>
    /// Core-owned ordered runtime identity slots used by selected Daily-v1
    /// turns. Null holes and unselected host identities remain in this exact
    /// sequence because actor-choice dispositions persist their ordinals.
    /// </summary>
    internal IReadOnlyList<DailyV1ActorTurnRosterProjection> SelectedRuntimeActorRoster =>
        selectedRuntimeActorRoster;
    internal bool SelectedRuntimeExecutionRootsBound => selectedRuntimeExecutionRootsBound;
    internal bool HasSelectedP12TravelPartyAdvanceInProgress
    {
        get
        {
            lock (operationSyncRoot)
                return selectedP12TravelPartyAdvanceContext != null;
        }
    }
    /// <summary>
    /// Whether this admitted state has the complete Core owner/configuration
    /// roots needed by the selected Daily-v1 execution path. Path selection is
    /// derived from Core-owned identities; a Unity adapter cannot opt the
    /// authoritative runtime in or out by reporting its own demographic view.
    /// </summary>
    internal bool HasAdmittedSelectedDailyExecution
    {
        get
        {
            DailyV1SelectedRuntimeOwnerGraph graph = selectedRuntimeOwnerGraph;
            return runtimeIdentityBound
                && runtimeConfigurationBound
                && AdmissionContext != null
                && AdmissionContext.Profile == SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
                && selectedRuntimeOwnerGraphBound
                && selectedRuntimeExecutionRootsBound
                && graph != null
                && graph.Definitions != null
                && ReferenceEquals(graph.Definitions.EffectiveConfiguration, ConfigurationIdentity)
                && graph.PersonStore != null
                && graph.ScheduledDirectives != null
                && graph.SpatialNetwork != null
                && graph.JusticeRecordsOwner != null
                && graph.TravelParties != null
                && graph.RuntimeIdentityIndex != null
                && selectedRuntimeIdAllocator != null
                && selectedRandomSource != null
                && selectedRecordSequence != null
                && selectedDecisionStore != null
                && selectedActorChoiceStore != null
                && ReferenceEquals(selectedActorChoiceStore.PersonStore, graph.PersonStore)
                && HasSelectedCityAndNpcOwners(graph);
        }
    }
    internal RuntimeIdAllocator SelectedRuntimeIdAllocator => selectedRuntimeIdAllocator;
    internal IAuthoritativeRandomSource SelectedRandomSource => selectedRandomSource;
    internal SimulationRecordSequence SelectedRecordSequence => selectedRecordSequence;
    internal NpcDecisionStore SelectedDecisionStore => selectedDecisionStore;
    internal ActorChoiceStore SelectedActorChoiceStore => selectedActorChoiceStore;
    /// <summary>
    /// One immutable Core bundle for the exact decision roots bound to this
    /// execution state. Per-day contexts reuse it; a host cannot recreate a
    /// parallel root bundle from projected runtime values.
    /// </summary>
    internal DailyV1ActorTurnCoreRoots SelectedRuntimeActorTurnCoreRoots =>
        selectedRuntimeActorTurnCoreRoots;

    internal bool TryBindSelectedRuntimeOwnerGraph(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        P12ECoreOwnerRestoreStage p12EOwners = graph?.P12EOwners;
        if (p12EOwners != null
            && (p12EOwners.CrimeSocialAppraisal == null
                || !ReferenceEquals(p12EOwners.CrimeSocialAppraisal.SimulationTime, SimulationTime)))
            return false;

        if (selectedRuntimeOwnerGraphBound)
            return ReferenceEquals(selectedRuntimeOwnerGraph, graph);

        selectedRuntimeOwnerGraph = graph;
        selectedRuntimeOwnerGraphBound = true;
        return true;
    }

    internal bool TryReplaceSelectedRuntimeOwnerGraph(
        DailyV1SelectedRuntimeOwnerGraph expected,
        DailyV1SelectedRuntimeOwnerGraph replacement)
    {
        if (!selectedRuntimeOwnerGraphBound
            || expected == null
            || replacement == null
            || !ReferenceEquals(selectedRuntimeOwnerGraph, expected)
            || !ReferenceEquals(expected.Definitions, replacement.Definitions))
            return false;

        DailyV1ActorTurnCoreRoots replacementActorTurnRoots = null;
        if (selectedRuntimeExecutionRootsBound)
        {
            if (!ReferenceEquals(replacement.PersonStore, selectedActorChoiceStore?.PersonStore)
                || selectedRuntimeIdAllocator == null
                || !replacement.TryBindRuntimeIdAllocator(selectedRuntimeIdAllocator))
                return false;
            try
            {
                replacementActorTurnRoots = new DailyV1ActorTurnCoreRoots(this, replacement);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        selectedRuntimeOwnerGraph = replacement;
        if (replacementActorTurnRoots != null)
            selectedRuntimeActorTurnCoreRoots = replacementActorTurnRoots;
        return true;
    }

    /// <summary>
    /// Binds the runtime's exact actor identity/ordinal projection after
    /// constructor membership has been composed. Domain owner state remains
    /// in the selected graph; this ordered identity roster is the single Core
    /// source for actor-turn ordinals and preserves unselected runtime slots.
    /// </summary>
    internal bool TryBindSelectedRuntimeActorRoster(
        IEnumerable<DailyV1ActorTurnRosterProjection> runtimeRoster)
    {
        if (!selectedRuntimeOwnerGraphBound || selectedRuntimeOwnerGraph == null
            || selectedRuntimeActorRoster != null
            || !TryCreateValidatedSelectedRuntimeActorRoster(
                selectedRuntimeOwnerGraph, runtimeRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> candidate))
            return false;

        selectedRuntimeActorRoster = candidate;
        return true;
    }

    /// <summary>
    /// Binds the initial selected actor order from Core's admitted owner graph.
    /// Unity may later compare its runtime projections with this exact sequence,
    /// but does not choose actor-turn ordinals for the shared runtime.
    /// </summary>
    internal bool TryBindInitialSelectedRuntimeActorRosterFromOwnerGraph()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = selectedRuntimeOwnerGraph;
        if (!selectedRuntimeOwnerGraphBound || graph == null
            || graph.PersonStore == null || selectedRuntimeActorRoster != null)
            return false;

        List<DailyV1ActorTurnRosterProjection> initialRoster =
            new List<DailyV1ActorTurnRosterProjection>(graph.Npcs.Count);
        foreach (DailyV1NpcRuntimeOwners owner in graph.Npcs)
        {
            if (owner == null || string.IsNullOrWhiteSpace(owner.RuntimeId)) return false;
            PersonId personId = null;
            if (graph.PersonStore.TryGetByMaterializedNpcRuntimeId(
                    owner.RuntimeId, out PersonRuntime person))
            {
                if (person == null || person.PersonId == null
                    || !person.IsMaterialized
                    || !string.Equals(person.MaterializedNpcRuntimeId,
                        owner.RuntimeId, StringComparison.Ordinal))
                    return false;
                personId = person.PersonId;
            }
            initialRoster.Add(new DailyV1ActorTurnRosterProjection(
                owner.RuntimeId, personId));
        }

        return TryBindSelectedRuntimeActorRoster(initialRoster);
    }

    /// <summary>
    /// Binds a legacy/unpublished roster once, or proves that a host projection
    /// exactly matches the Core-created order and PersonId instances. A Unity
    /// host cannot replace the roster that determines persisted actor ordinals.
    /// </summary>
    internal bool TryBindOrValidateSelectedRuntimeActorRoster(
        IEnumerable<DailyV1ActorTurnRosterProjection> runtimeRoster)
    {
        if (!selectedRuntimeOwnerGraphBound || selectedRuntimeOwnerGraph == null
            || !TryCreateValidatedSelectedRuntimeActorRoster(
                selectedRuntimeOwnerGraph, runtimeRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> candidate))
            return false;

        IReadOnlyList<DailyV1ActorTurnRosterProjection> existing = selectedRuntimeActorRoster;
        if (existing == null)
        {
            selectedRuntimeActorRoster = candidate;
            return true;
        }

        if (existing.Count != candidate.Count) return false;
        for (int index = 0; index < existing.Count; index++)
        {
            DailyV1ActorTurnRosterProjection left = existing[index];
            DailyV1ActorTurnRosterProjection right = candidate[index];
            if (left == null || right == null)
            {
                if (left != null || right != null) return false;
                continue;
            }
            if (!string.Equals(left.RuntimeId, right.RuntimeId, StringComparison.Ordinal)
                || !ReferenceEquals(left.PersonId, right.PersonId))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Prepares the same ordered runtime identity roster with one newly
    /// materialized PersonId. The caller publishes this projection as part of
    /// the existing PersonStore/NPC bind transaction; runtime ID order and
    /// holes are unchanged.
    /// </summary>
    internal bool TryCreateSelectedRuntimeActorRosterWithPersonBinding(
        string runtimeId,
        PersonId personId,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> expectedRoster,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        expectedRoster = selectedRuntimeActorRoster;
        replacementRoster = null;
        if (!selectedRuntimeOwnerGraphBound
            || selectedRuntimeOwnerGraph == null
            || selectedRuntimeActorRoster == null
            || string.IsNullOrWhiteSpace(runtimeId)
            || personId == null)
            return false;

        List<DailyV1ActorTurnRosterProjection> candidate =
            new List<DailyV1ActorTurnRosterProjection>(selectedRuntimeActorRoster.Count);
        bool replaced = false;
        foreach (DailyV1ActorTurnRosterProjection member in selectedRuntimeActorRoster)
        {
            if (member != null
                && string.Equals(member.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                if (replaced || member.PersonId != null) return false;
                replaced = true;
                candidate.Add(new DailyV1ActorTurnRosterProjection(member.RuntimeId, personId));
            }
            else
            {
                candidate.Add(member);
            }
        }

        return replaced && TryCreateValidatedSelectedRuntimeActorRoster(
            selectedRuntimeOwnerGraph, candidate, out replacementRoster);
    }

    /// <summary>
    /// Publishes an already validated roster projection with compare-and-swap
    /// semantics so a caller can compensate it if its paired owner bind fails.
    /// </summary>
    internal bool TryCommitSelectedRuntimeActorRoster(
        IReadOnlyList<DailyV1ActorTurnRosterProjection> expectedRoster,
        IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        if (!selectedRuntimeOwnerGraphBound
            || selectedRuntimeOwnerGraph == null
            || expectedRoster == null
            || replacementRoster == null
            || !ReferenceEquals(selectedRuntimeActorRoster, expectedRoster))
            return false;

        selectedRuntimeActorRoster = replacementRoster;
        return true;
    }

    internal bool TryCreateSelectedRuntimeActorRosterWithAppendedMember(
        DailyV1ActorTurnRosterProjection member,
        DailyV1SelectedRuntimeOwnerGraph replacementGraph,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        replacementRoster = null;
        if (selectedRuntimeActorRoster == null || member == null
            || replacementGraph == null)
            return false;

        List<DailyV1ActorTurnRosterProjection> candidate =
            new List<DailyV1ActorTurnRosterProjection>(selectedRuntimeActorRoster.Count + 1);
        candidate.AddRange(selectedRuntimeActorRoster);
        candidate.Add(member);
        return TryCreateValidatedSelectedRuntimeActorRoster(
            replacementGraph, candidate, out replacementRoster);
    }

    internal bool TryCreateSelectedRuntimeActorRosterWithoutMember(
        string runtimeId,
        DailyV1SelectedRuntimeOwnerGraph replacementGraph,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        replacementRoster = null;
        if (selectedRuntimeActorRoster == null
            || string.IsNullOrWhiteSpace(runtimeId)
            || replacementGraph == null)
            return false;

        List<DailyV1ActorTurnRosterProjection> candidate =
            new List<DailyV1ActorTurnRosterProjection>(selectedRuntimeActorRoster.Count);
        bool removed = false;
        foreach (DailyV1ActorTurnRosterProjection member in selectedRuntimeActorRoster)
        {
            if (member != null && string.Equals(member.RuntimeId, runtimeId, StringComparison.Ordinal))
            {
                if (removed) return false;
                removed = true;
                continue;
            }
            candidate.Add(member);
        }

        return removed && TryCreateValidatedSelectedRuntimeActorRoster(
            replacementGraph, candidate, out replacementRoster);
    }

    /// <summary>
    /// Atomically publishes a new selected graph together with its exact
    /// ordered actor identity roster. Membership callers prepare and validate
    /// both snapshots before invoking this single validation-and-publication point.
    /// </summary>
    internal bool TryReplaceSelectedRuntimeMembership(
        DailyV1SelectedRuntimeOwnerGraph expectedGraph,
        DailyV1SelectedRuntimeOwnerGraph replacementGraph,
        IReadOnlyList<DailyV1ActorTurnRosterProjection> expectedRoster,
        IReadOnlyList<DailyV1ActorTurnRosterProjection> replacementRoster)
    {
        if (!selectedRuntimeOwnerGraphBound
            || expectedGraph == null
            || replacementGraph == null
            || !ReferenceEquals(selectedRuntimeOwnerGraph, expectedGraph)
            || !ReferenceEquals(selectedRuntimeActorRoster, expectedRoster)
            || !ReferenceEquals(expectedGraph.Definitions, replacementGraph.Definitions)
            || !TryCreateValidatedSelectedRuntimeActorRoster(
                replacementGraph, replacementRoster,
                out IReadOnlyList<DailyV1ActorTurnRosterProjection> validatedRoster))
            return false;

        DailyV1ActorTurnCoreRoots replacementActorTurnRoots = null;
        if (selectedRuntimeExecutionRootsBound)
        {
            if (selectedRuntimeIdAllocator == null
                || !replacementGraph.TryBindRuntimeIdAllocator(selectedRuntimeIdAllocator)
                || selectedActorChoiceStore == null
                || !ReferenceEquals(replacementGraph.PersonStore, selectedActorChoiceStore.PersonStore))
                return false;
            try
            {
                replacementActorTurnRoots = new DailyV1ActorTurnCoreRoots(this, replacementGraph);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        selectedRuntimeOwnerGraph = replacementGraph;
        selectedRuntimeActorRoster = validatedRoster;
        if (replacementActorTurnRoots != null)
            selectedRuntimeActorTurnCoreRoots = replacementActorTurnRoots;
        return true;
    }

    private static bool TryCreateValidatedSelectedRuntimeActorRoster(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IEnumerable<DailyV1ActorTurnRosterProjection> runtimeRoster,
        out IReadOnlyList<DailyV1ActorTurnRosterProjection> roster)
    {
        roster = null;
        if (graph == null || runtimeRoster == null) return false;

        List<DailyV1ActorTurnRosterProjection> candidate =
            new List<DailyV1ActorTurnRosterProjection>();
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1ActorTurnRosterProjection member in runtimeRoster)
        {
            if (member != null
                && (string.IsNullOrWhiteSpace(member.RuntimeId)
                    || !runtimeIds.Add(member.RuntimeId)))
                return false;
            candidate.Add(member);
        }

        if (!DailyV1ActorTurnExecutionStage.TryBuildRoster(
                graph, candidate, out IReadOnlyList<DailyV1ActorTurnRosterEntry> entries))
            return false;
        List<DailyV1NpcRuntimeOwners> selectedOwners =
            new List<DailyV1NpcRuntimeOwners>(graph.Npcs.Count);
        foreach (DailyV1ActorTurnRosterEntry entry in entries)
            if (entry?.ActorOwner != null)
                selectedOwners.Add(entry.ActorOwner);
        if (!graph.MatchesExactOwnerInstances(graph.Cities, selectedOwners))
            return false;

        roster = candidate.AsReadOnly();
        return true;
    }

    /// <summary>
    /// Binds the selected runtime's mutable decision roots beside its clock,
    /// admission, graph and capture state. Unity may project these same
    /// instances, but it cannot substitute a different RNG, choice store,
    /// allocator, sequence or decision store at a Core execution boundary.
    /// </summary>
    internal bool TryBindSelectedRuntimeExecutionRoots(
        RuntimeIdAllocator runtimeIdAllocator,
        IAuthoritativeRandomSource randomSource,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore,
        ActorChoiceStore actorChoiceStore)
    {
        if (!runtimeIdentityBound || !runtimeConfigurationBound
            || !selectedRuntimeOwnerGraphBound || selectedRuntimeOwnerGraph == null
            || runtimeIdAllocator == null || randomSource == null
            || recordSequence == null || decisionStore == null || actorChoiceStore == null
            || selectedRuntimeOwnerGraph.PersonStore == null
            || !ReferenceEquals(actorChoiceStore.PersonStore, selectedRuntimeOwnerGraph.PersonStore))
            return false;

        if (selectedRuntimeExecutionRootsBound)
            return ReferenceEquals(selectedRuntimeIdAllocator, runtimeIdAllocator)
                && ReferenceEquals(selectedRuntimeOwnerGraph.RuntimeIdAllocator, runtimeIdAllocator)
                && ReferenceEquals(selectedRandomSource, randomSource)
                && ReferenceEquals(selectedRecordSequence, recordSequence)
                && ReferenceEquals(selectedDecisionStore, decisionStore)
                && ReferenceEquals(selectedActorChoiceStore, actorChoiceStore);

        selectedRuntimeIdAllocator = runtimeIdAllocator;
        selectedRandomSource = randomSource;
        selectedRecordSequence = recordSequence;
        selectedDecisionStore = decisionStore;
        selectedActorChoiceStore = actorChoiceStore;
        try
        {
            selectedRuntimeActorTurnCoreRoots = new DailyV1ActorTurnCoreRoots(
                this, selectedRuntimeOwnerGraph);
            if (!selectedRuntimeOwnerGraph.TryBindRuntimeIdAllocator(runtimeIdAllocator))
                throw new ArgumentException(
                    "The selected owner graph is already bound to a different runtime ID allocator.",
                    nameof(runtimeIdAllocator));
        }
        catch (ArgumentException)
        {
            selectedRuntimeIdAllocator = null;
            selectedRandomSource = null;
            selectedRecordSequence = null;
            selectedDecisionStore = null;
            selectedActorChoiceStore = null;
            selectedRuntimeActorTurnCoreRoots = null;
            return false;
        }
        selectedRuntimeExecutionRootsBound = true;
        return true;
    }

    internal bool TryBindSelectedTravelEventRecorders(
        DomainEventRecorder partyRecorder,
        DomainEventRecorder soloRecorder)
    {
        if (!runtimeIdentityBound || !selectedRuntimeExecutionRootsBound
            || selectedRuntimeOwnerGraph == null
            || selectedRuntimeIdAllocator == null
            || SimulationTime == null
            || selectedRecordSequence == null)
            return false;

        if ((partyRecorder != null
                && !partyRecorder.UsesIdentityOwners(
                    selectedRuntimeIdAllocator, SimulationTime, selectedRecordSequence))
            || (soloRecorder != null
                && !soloRecorder.UsesIdentityOwners(
                    selectedRuntimeIdAllocator, SimulationTime, selectedRecordSequence)))
            return false;

        if (selectedTravelEventRecordersBound)
            return ReferenceEquals(selectedTravelPartyEventRecorder, partyRecorder)
                && ReferenceEquals(selectedSoloTravelEventRecorder, soloRecorder);

        selectedTravelPartyEventRecorder = partyRecorder;
        selectedSoloTravelEventRecorder = soloRecorder;
        selectedTravelEventRecordersBound = true;
        return true;
    }

    internal bool TryRecordSelectedTravelPartyArrival(
        TravelPartyRuntime party,
        out bool eventRecorded)
    {
        eventRecorded = false;
        DailyV1SelectedRuntimeOwnerGraph graph = selectedRuntimeOwnerGraph;
        if (!HasAdmittedSelectedDailyExecution
            || !selectedTravelEventRecordersBound
            || !AdvanceLeaseHeld
            || !HasSelectedP12TravelPartyAdvanceInProgress
            || party == null
            || graph?.TravelParties == null)
            return false;

        if (selectedTravelPartyEventRecorder == null)
        {
            eventRecorded = true;
            return true;
        }

        eventRecorded = selectedTravelPartyEventRecorder.Record(
            (eventId, absoluteDay, recordSequence) => new TravelPartyArrivedEvent(
                eventId,
                absoluteDay,
                recordSequence,
                party.TravelPartyId,
                party.OriginLocationRuntimeId,
                party.DestinationLocationRuntimeId,
                party.RouteRuntimeId,
                party.TravelDaysTotal,
                party.TravelerRuntimeIds,
                party.EscortRuntimeIds,
                party.OriginDecisionId));
        return true;
    }

    internal bool TryRecordSelectedSoloTravelArrival(
        DailyV1SelectedSoloTravelDayResult result,
        out bool eventRecorded)
    {
        eventRecorded = false;
        DailyV1SelectedRuntimeOwnerGraph graph = selectedRuntimeOwnerGraph;
        DailyV1NpcRuntimeOwners npc = result?.Npc;
        if (!HasAdmittedSelectedDailyExecution
            || !selectedTravelEventRecordersBound
            || !AdvanceLeaseHeld
            || result == null
            || !result.ShouldReport
            || !result.WasCommitted
            || !result.Arrived
            || npc == null
            || graph == null
            || !graph.ContainsExactNpcOwner(npc)
            || string.IsNullOrWhiteSpace(npc.Travel.CurrentLocationRuntimeId))
            return false;

        if (selectedSoloTravelEventRecorder == null)
        {
            eventRecorded = true;
            return true;
        }

        eventRecorded = selectedSoloTravelEventRecorder.Record(
            (eventId, absoluteDay, recordSequence) => new NpcArrivedEvent(
                eventId,
                absoluteDay,
                recordSequence,
                npc.RuntimeId,
                npc.Travel.CurrentLocationRuntimeId,
                result.OriginDecisionId));
        return true;
    }

    private static bool HasSelectedCityAndNpcOwners(DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph.Cities == null || graph.Npcs == null)
            return false;
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
            if (city == null || city.Population == null
                || !ReferenceEquals(city.Definitions, graph.Definitions))
                return false;
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
            if (npc == null || npc.Lifecycle == null || npc.Travel == null
                || npc.ActionSlot == null || !ReferenceEquals(npc.Definitions, graph.Definitions))
                return false;
        return true;
    }

    internal bool HasExactSelectedRuntimeExecutionRoots(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        IAuthoritativeRandomSource randomSource,
        RuntimeIdAllocator runtimeIdAllocator,
        SimulationRecordSequence recordSequence,
        NpcDecisionStore decisionStore)
    {
        return selectedRuntimeExecutionRootsBound
            && ReferenceEquals(selectedRuntimeOwnerGraph, graph)
            && ReferenceEquals(graph?.PersonStore, personStore)
            && ReferenceEquals(graph?.RuntimeIdAllocator, runtimeIdAllocator)
            && ReferenceEquals(selectedActorChoiceStore, actorChoiceStore)
            && ReferenceEquals(actorChoiceStore?.PersonStore, personStore)
            && ReferenceEquals(selectedRandomSource, randomSource)
            && ReferenceEquals(selectedRuntimeIdAllocator, runtimeIdAllocator)
            && ReferenceEquals(selectedRecordSequence, recordSequence)
            && ReferenceEquals(selectedDecisionStore, decisionStore);
    }

    internal bool HasExactSelectedRuntimeActorTurnRoots(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        ActorChoiceStore actorChoiceStore,
        IAuthoritativeRandomSource randomSource)
    {
        return selectedRuntimeExecutionRootsBound
            && selectedRuntimeActorTurnCoreRoots != null
            && selectedRuntimeActorTurnCoreRoots.IsBoundTo(
                this, graph, personStore, actorChoiceStore)
            && ReferenceEquals(selectedRuntimeOwnerGraph, graph)
            && ReferenceEquals(graph?.PersonStore, personStore)
            && ReferenceEquals(graph?.RuntimeIdAllocator, selectedRuntimeIdAllocator)
            && ReferenceEquals(selectedActorChoiceStore, actorChoiceStore)
            && ReferenceEquals(actorChoiceStore?.PersonStore, personStore)
            && ReferenceEquals(selectedRandomSource, randomSource);
    }

    internal bool HasExactSelectedRuntimeOwnerGraph(
        DailyV1EffectiveDefinitions definitions,
        IEnumerable<DailyV1CityRuntimeOwners> cityOwners,
        IEnumerable<DailyV1NpcRuntimeOwners> npcOwners) =>
        selectedRuntimeOwnerGraph != null
        && ReferenceEquals(selectedRuntimeOwnerGraph.Definitions, definitions)
        && selectedRuntimeOwnerGraph.MatchesExactOwnerInstances(cityOwners, npcOwners);

    internal void InvalidateCurrentCaptureToken()
    {
        currentCaptureToken = null;
    }

    internal bool TryCompareCurrentCaptureToken(
        DailyCaptureEligibilityToken token,
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay,
        IReadOnlyList<OwnerSectionCensusSnapshot> currentOwnerSections,
        long currentMutationEpoch)
    {
        return MatchesBoundRuntimeIdentity(admissionContext, configurationIdentity,
                calendarIdentity, compositionProfile, worldId, absoluteDay)
            && token != null
            && ReferenceEquals(token.RuntimeInstanceIdentity, runtimeInstanceIdentity)
            && ReferenceEquals(token.AdmissionContext, admissionContext)
            && ReferenceEquals(token.ConfigurationIdentity, configurationIdentity)
            && ReferenceEquals(token.CalendarIdentity, calendarIdentity)
            && token.CompositionProfile == compositionProfile
            && ReferenceEquals(token.WorldId, worldId)
            && token.AbsoluteDay == absoluteDay
            && token.CompletedCoreSequence == completedCoreSequence
            && token.CompletedCoreSequence > 0L
            && (token.BoundaryProvenance == DailyCaptureBoundaryProvenance.CompletedAdvance
                || token.BoundaryProvenance == DailyCaptureBoundaryProvenance.RestoredContinuation)
            && token.MutationEpoch == currentMutationEpoch
            && AreOwnerSectionsEqual(token.OwnerSections, currentOwnerSections);
    }

    /// <summary>
    /// Revalidates the current completed boundary while exactly one P12-G
    /// continuation-save operation is registered. The returned owner list is
    /// the exact census source which must be retained by detached staging.
    /// </summary>
    internal bool TryCaptureContinuationSaveBoundaryEvidence(
        IDailyV1RuntimeAdvanceEvidenceBoundary boundary,
        out DailyCaptureEligibilityToken token,
        out IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out long mutationEpoch,
        out DailyCaptureEligibilityFailure failure)
    {
        token = null;
        ownerSections = null;
        mutationEpoch = 0L;
        failure = DailyCaptureEligibilityFailure.OperationInProgress;
        if (boundary == null || !ReferenceEquals(boundary.ExecutionState, this)
            || !TryAssessCaptureScope(DailyV1CaptureScopeAllowance.ContinuationSave)
            || AdmissionContext == null
            || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !AdmissionContext.IsOwnedByCurrentThread()
            || AdvanceLeaseHeld || !mutationGuard.CanMutate
            || SimulationTime == null || WorldId == null
            || !runtimeIdentityBound || !runtimeConfigurationBound)
            return false;

        DailyCaptureEligibilityToken current = currentCaptureToken;
        if (current == null)
        {
            failure = DailyCaptureEligibilityFailure.NoCompletedBoundary;
            return false;
        }

        if (current.BoundaryProvenance != DailyCaptureBoundaryProvenance.CompletedAdvance
            && current.BoundaryProvenance != DailyCaptureBoundaryProvenance.RestoredContinuation)
        {
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        if (!boundary.TryCaptureContinuationSaveEvidence(
                out IReadOnlyList<OwnerSectionCensusSnapshot> captured,
                out long capturedMutationEpoch,
                out failure))
            return false;

        if (captured == null || capturedMutationEpoch < 0L
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(captured)
            || !TryCompareCurrentCaptureToken(
                current, AdmissionContext, ConfigurationIdentity, CalendarIdentity,
                CompositionProfile, WorldId, SimulationTime.AbsoluteDay,
                captured, capturedMutationEpoch)
            || !TryAssessCaptureScope(DailyV1CaptureScopeAllowance.ContinuationSave)
            || !AdmissionContext.IsOwnedByCurrentThread()
            || !mutationGuard.CanMutate)
        {
            failure = DailyCaptureEligibilityFailure.StaleToken;
            return false;
        }

        token = current;
        ownerSections = captured;
        mutationEpoch = capturedMutationEpoch;
        failure = DailyCaptureEligibilityFailure.None;
        return true;
    }

    internal static bool AreOwnerSectionsEqual(
        IReadOnlyList<OwnerSectionCensusSnapshot> left,
        IReadOnlyList<OwnerSectionCensusSnapshot> right)
    {
        if (left == null || right == null || left.Count != right.Count) return false;
        for (int index = 0; index < left.Count; index++)
        {
            OwnerSectionCensusSnapshot first = left[index];
            OwnerSectionCensusSnapshot second = right[index];
            if (first == null
                || second == null
                || !string.Equals(first.SectionId, second.SectionId, StringComparison.Ordinal)
                || first.SchemaVersion != second.SchemaVersion
                || first.Role != second.Role
                || !ReferenceEquals(first.OwnerInstanceIdentity, second.OwnerInstanceIdentity)
                || first.Cardinality != second.Cardinality
                || first.Revision != second.Revision)
                return false;
        }

        return true;
    }

    internal bool CanRecordCompletedCoreCount(long requestedCoreCount)
    {
        return requestedCoreCount > 0L
            && completedCoreSequence <= long.MaxValue - requestedCoreCount;
    }

    internal bool TryRecordCompletedCore()
    {
        if (completedCoreSequence == long.MaxValue)
            return false;

        completedCoreSequence++;
        return true;
    }

    internal bool TryAcquireAdvanceLease(out DailyV1AdvanceLease lease)
    {
        lease = null;
        if (advanceLease != null)
            return false;

        DailyV1AdvanceLease candidate = new DailyV1AdvanceLease(this);
        advanceLease = candidate;
        advanceLeaseInitialCompletedSequence = completedCoreSequence;
        lease = candidate;
        return true;
    }

    internal bool TryBeginAdvance(bool invalidateCaptureToken, out DailyV1AdvanceLease lease)
    {
        if (!TryAcquireAdvanceLease(out lease)) return false;
        if (invalidateCaptureToken) InvalidateCurrentCaptureToken();
        return true;
    }
    internal void ReleaseAdvanceLease(DailyV1AdvanceLease lease)
    {
        if (ReferenceEquals(advanceLease, lease))
            advanceLease = null;
    }

    internal bool TryInstallRestoredCompletedCoreSequence(long restoredSequence)
    {
        if (restoredSequence <= 0L
            || completedCoreSequence != 0L
            || AdvanceLeaseHeld)
            return false;

        completedCoreSequence = restoredSequence;
        return true;
    }

    internal bool TryCreateCompletedAdvanceToken(
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay,
        long mutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyCaptureEligibilityToken token)
    {
        token = null;
        if (!MatchesBoundRuntimeIdentity(admissionContext, configurationIdentity,
                calendarIdentity, compositionProfile, worldId, absoluteDay)
            || completedCoreSequence <= 0L
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(ownerSections))
            return false;

        token = new DailyCaptureEligibilityToken(
            runtimeInstanceIdentity,
            admissionContext,
            configurationIdentity,
            calendarIdentity,
            compositionProfile,
            worldId,
            absoluteDay,
            completedCoreSequence,
            DailyCaptureBoundaryProvenance.CompletedAdvance,
            mutationEpoch,
            ownerSections);
        currentCaptureToken = token;
        return true;
    }

    /// <summary>
    /// Issues a completed-advance token only for a positive boundary recorded
    /// during this exact held runtime lease. The P12 census remains the source
    /// of owner snapshots and mutation epoch; this state owns the publication
    /// decision and binds the token to its own admitted runtime identities.
    /// </summary>
    internal bool TryIssueCompletedAdvanceCaptureToken(
        long mutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyCaptureEligibilityToken token)
    {
        token = null;
        if (!runtimeIdentityBound
            || !runtimeConfigurationBound
            || AdmissionContext == null
            || AdmissionContext.Profile != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || !AdvanceLeaseHeld
            || currentCaptureToken != null
            || completedCoreSequence <= advanceLeaseInitialCompletedSequence
            || SimulationTime == null
            || mutationEpoch < 0L
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(ownerSections))
            return false;

        return TryCreateCompletedAdvanceToken(
            AdmissionContext,
            ConfigurationIdentity,
            CalendarIdentity,
            CompositionProfile,
            WorldId,
            SimulationTime.AbsoluteDay,
            mutationEpoch,
            ownerSections,
            out token);
    }

    internal bool TryCreateRestoredContinuationToken(
        SimulationRuntimeAdmissionContext admissionContext,
        EffectiveSimulationConfiguration configurationIdentity,
        SimulationCalendar calendarIdentity,
        SimulationRuntimeCompositionProfile compositionProfile,
        WorldId worldId,
        long absoluteDay,
        long restoredCoreSequence,
        long mutationEpoch,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        out DailyCaptureEligibilityToken token)
    {
        token = null;
        if (!MatchesBoundRuntimeIdentity(admissionContext, configurationIdentity,
                calendarIdentity, compositionProfile, worldId, absoluteDay)
            || restoredCoreSequence <= 0L || completedCoreSequence != 0L || AdvanceLeaseHeld
            || !DailyV1SelectedOwnerSectionCensusContract.IsCompleteSnapshot(ownerSections))
            return false;

        DailyCaptureEligibilityToken candidate = new DailyCaptureEligibilityToken(
            runtimeInstanceIdentity,
            admissionContext,
            configurationIdentity,
            calendarIdentity,
            compositionProfile,
            worldId,
            absoluteDay,
            restoredCoreSequence,
            DailyCaptureBoundaryProvenance.RestoredContinuation,
            mutationEpoch,
            ownerSections);
        if (!TryInstallRestoredCompletedCoreSequence(restoredCoreSequence))
            return false;

        currentCaptureToken = candidate;
        token = candidate;
        return true;
    }
}

/// <summary>
/// Idempotent handle for one runtime advance window. Disposing an older handle
/// cannot release a later lease from the same boundary state.
/// </summary>
internal sealed class DailyV1AdvanceLease : IDisposable
{
    private DailyV1ExecutionBoundaryState owner;

    internal DailyV1AdvanceLease(DailyV1ExecutionBoundaryState owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Dispose()
    {
        DailyV1ExecutionBoundaryState current = owner;
        owner = null;
        current?.ReleaseAdvanceLease(this);
    }
}
