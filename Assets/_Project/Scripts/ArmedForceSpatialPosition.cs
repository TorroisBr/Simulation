using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public enum ArmedForceSpatialFailureCode
{
    None = 0,
    ForceNotRegistered = 1,
    ForceTerminated = 2,
    InvalidSpatialReference = 3,
    SpatialReferenceNotRegistered = 4,
    RevisionOverflow = 5,
    InvalidInvariant = 6,
    RuntimeFaulted = 7,
    MovementProfileNotConfigured = 8,
    ForceNotSelected = 9,
    MovementStateStale = 10,
    MovementRejected = 11,
    InsufficientCarriedSupply = 12,
    CrossingAlreadyCommitted = 13,
    InvalidMovementInput = 14,
    DirectPositionMutationBlocked = 15,
    RuntimeOperationInProgress = 16
}

public sealed class ArmedForceSpatialFailure : IEquatable<ArmedForceSpatialFailure>
{
    private static readonly ArmedForceSpatialFailure none =
        new ArmedForceSpatialFailure(ArmedForceSpatialFailureCode.None, string.Empty, null);

    private ArmedForceSpatialFailure(
        ArmedForceSpatialFailureCode code,
        string message,
        SpatialAuthorityFailure authorityFailure)
    {
        Code = code;
        Message = message ?? string.Empty;
        AuthorityFailure = authorityFailure;
    }

    public static ArmedForceSpatialFailure None => none;
    public ArmedForceSpatialFailureCode Code { get; }
    public string Message { get; }
    public SpatialAuthorityFailure AuthorityFailure { get; }
    public bool IsFailure => Code != ArmedForceSpatialFailureCode.None;

    internal static ArmedForceSpatialFailure Create(
        ArmedForceSpatialFailureCode code,
        string message,
        SpatialAuthorityFailure authorityFailure = null)
    {
        return code == ArmedForceSpatialFailureCode.None
            ? None
            : new ArmedForceSpatialFailure(code, message, authorityFailure);
    }

    public bool Equals(ArmedForceSpatialFailure other)
    {
        return other != null
            && Code == other.Code
            && string.Equals(Message, other.Message, StringComparison.Ordinal)
            && Equals(AuthorityFailure, other.AuthorityFailure);
    }

    public override bool Equals(object obj) => Equals(obj as ArmedForceSpatialFailure);
    public override int GetHashCode() => ((int)Code * 397)
        ^ StringComparer.Ordinal.GetHashCode(Message)
        ^ (AuthorityFailure == null ? 0 : AuthorityFailure.GetHashCode());

    public override string ToString() => Code
        + (string.IsNullOrEmpty(Message) ? string.Empty : ": " + Message);
}

public sealed class ArmedForceSpatialPosition
{
    public ArmedForceId ForceId { get; }
    public SpatialReference Position { get; }

    public ArmedForceSpatialPosition(ArmedForceId forceId, SpatialReference position)
    {
        ForceId = forceId ?? throw new ArgumentNullException(nameof(forceId));
        Position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public string StableKey => ForceId.Value;
}

/// <summary>Immutable inputs for the bounded one-force, one-passage P16-A profile.</summary>
public sealed class P16AMilitaryMovementProfile
{
    public const string ProfileIdentity = "P16A-MilitaryOneHop";
    public const string ProfileRevision = "v1";
    public const string TraversalContextIdentity = "P16A-MilitaryOneHop";
    public const string TraversalContextRevision = "v1";

    public ArmedForceId SelectedForceId { get; }
    public string ItemDefinitionId { get; }
    public string ItemContentRevision { get; }
    public decimal InitialQuantity { get; }
    public decimal QuantityPerCrossing { get; }
    public long TargetBoundaryDay { get; }

    public P16AMilitaryMovementProfile(
        ArmedForceId selectedForceId,
        string itemDefinitionId,
        string itemContentRevision,
        decimal initialQuantity,
        decimal quantityPerCrossing,
        long targetBoundaryDay)
    {
        if (selectedForceId == null) throw new ArgumentNullException(nameof(selectedForceId));
        if (string.IsNullOrWhiteSpace(itemDefinitionId)) throw new ArgumentException("A compatible ItemData.DefinitionId is required.", nameof(itemDefinitionId));
        if (string.IsNullOrWhiteSpace(itemContentRevision)) throw new ArgumentException("The item content revision is required.", nameof(itemContentRevision));
        if (initialQuantity < 0m) throw new ArgumentOutOfRangeException(nameof(initialQuantity));
        if (quantityPerCrossing <= 0m) throw new ArgumentOutOfRangeException(nameof(quantityPerCrossing));
        if (targetBoundaryDay < 0L) throw new ArgumentOutOfRangeException(nameof(targetBoundaryDay));
        SelectedForceId = selectedForceId;
        ItemDefinitionId = itemDefinitionId;
        ItemContentRevision = itemContentRevision;
        InitialQuantity = initialQuantity;
        QuantityPerCrossing = quantityPerCrossing;
        TargetBoundaryDay = targetBoundaryDay;
    }

    internal TraversalCostContext CreateTraversalContext() =>
        new TraversalCostContext(TraversalContextIdentity, TraversalContextRevision, 1m, 1m, 1m);
}

/// <summary>One retained successful crossing; it is also the one-shot receipt guard.</summary>
public sealed class P16ACrossingReceipt
{
    public string OperationId { get; }
    public string ForceId { get; }
    public string SourceHexId { get; }
    public string DestinationHexId { get; }
    public TraversalOptionRef Option { get; }
    public TraversalOptionKind OptionKind { get; }
    public string OptionIdentity { get; }
    public string OptionContentIdentity { get; }
    public string OptionContentRevision { get; }
    public string TraversalContextIdentity { get; }
    public string TraversalContextRevision { get; }
    public PassageCondition PassageCondition { get; }
    public long PassageAuthorityRevision { get; }
    public long ForceStoreRevision { get; }
    public long LogicalBoundary { get; }
    public long AcceptedOrder { get; }
    public long OwnerRevision { get; }
    public decimal SupplyDebited { get; }
    public WorldCommandOrigin? AcceptedOrigin { get; }
    public string AuthorityId { get; }

    internal P16ACrossingReceipt(string operationId, string forceId, string sourceHexId, string destinationHexId,
        TraversalOptionRef option, string optionContentIdentity, string optionContentRevision,
        string contextIdentity, string contextRevision, PassageCondition passageCondition,
        long passageAuthorityRevision, long forceStoreRevision, long logicalBoundary, long acceptedOrder,
        long ownerRevision, decimal supplyDebited, WorldCommandOrigin? acceptedOrigin = null, string authorityId = null)
    {
        OperationId = operationId; ForceId = forceId; SourceHexId = sourceHexId; DestinationHexId = destinationHexId;
        Option = option;
        OptionKind = option.Kind;
        OptionIdentity = option.Kind == TraversalOptionKind.Connection ? "connection:" + option.ConnectionId.Value
            : option.Kind == TraversalOptionKind.Crossing ? "crossing:" + option.CrossingId.Value
            : "wilderness:" + option.RuleIdentity + ":" + option.RuleVersion;
        OptionContentIdentity = optionContentIdentity; OptionContentRevision = optionContentRevision;
        TraversalContextIdentity = contextIdentity; TraversalContextRevision = contextRevision;
        PassageCondition = passageCondition; PassageAuthorityRevision = passageAuthorityRevision;
        ForceStoreRevision = forceStoreRevision; LogicalBoundary = logicalBoundary; AcceptedOrder = acceptedOrder;
        OwnerRevision = ownerRevision; SupplyDebited = supplyDebited;
        AcceptedOrigin = acceptedOrigin; AuthorityId = authorityId;
    }
}

/// <summary>Exact immutable P16-A semantic-state export and staged-hydration validation input.</summary>
public sealed class P16AStateSnapshot
{
    public string ProfileIdentity => P16AMilitaryMovementProfile.ProfileIdentity;
    public string ProfileRevision => P16AMilitaryMovementProfile.ProfileRevision;
    public string ForceId { get; }
    public string PositionStableKey { get; }
    public string ItemDefinitionId { get; }
    public string ItemContentRevision { get; }
    public decimal InitialQuantity { get; }
    public decimal QuantityPerCrossing { get; }
    public long TargetBoundaryDay { get; }
    public decimal CurrentQuantity { get; }
    public P16ACrossingReceipt Receipt { get; }
    public long OwnerRevision { get; }
    public bool RequiresP17AProvenance { get; }
    public string TrustedAuthorityId { get; }

    public P16AStateSnapshot(string forceId, string positionStableKey, string itemDefinitionId,
        string itemContentRevision, decimal initialQuantity, decimal quantityPerCrossing,
        long targetBoundaryDay, decimal currentQuantity, P16ACrossingReceipt receipt, long ownerRevision,
        bool requiresP17AProvenance = false, string trustedAuthorityId = null)
    {
        ForceId = forceId; PositionStableKey = positionStableKey; ItemDefinitionId = itemDefinitionId;
        ItemContentRevision = itemContentRevision; InitialQuantity = initialQuantity;
        QuantityPerCrossing = quantityPerCrossing; TargetBoundaryDay = targetBoundaryDay;
        CurrentQuantity = currentQuantity;
        Receipt = receipt; OwnerRevision = ownerRevision;
        RequiresP17AProvenance = requiresP17AProvenance; TrustedAuthorityId = trustedAuthorityId;
    }
}

public sealed class ArmedForceSpatialInvariantReport
{
    public IReadOnlyList<string> Violations { get; }
    public bool IsValid => Violations.Count == 0;

    internal ArmedForceSpatialInvariantReport(IEnumerable<string> violations)
    {
        List<string> values = violations == null
            ? new List<string>()
            : new List<string>(violations);
        values.Sort(StringComparer.Ordinal);
        Violations = new ReadOnlyCollection<string>(values);
    }
}

/// <summary>
/// Authoritative optional current physical position for ArmedForce identities.
/// It is deliberately separate from force identity, hierarchy, detachment,
/// composition, lifecycle, and the legacy OperationalLocationReference shim.
/// P16-A adds a selected-profile carried item and one atomic single-passage mutation;
/// ordinary stores preserve their existing initialization/spatial behavior.
/// </summary>
public sealed class ArmedForceSpatialStateStore : IAuthoritativeMutationGuardBindable
{
    private readonly MutationGuardBinding mutationGuardBinding = new MutationGuardBinding();
    private readonly ArmedForceStore armedForceStore;
    private readonly SpatialAuthorityStore spatialAuthorityStore;
    private readonly LocalTopologyStore localTopologyStore;
    private readonly Dictionary<string, OperationalRecord> recordsByForceId =
        new Dictionary<string, OperationalRecord>(StringComparer.Ordinal);
    private P16AMilitaryMovementProfile p16Profile;
    private bool requiresP17AProvenance;
    private string trustedP17AAuthorityId;
    private long revision;

    public ArmedForceSpatialStateStore(
        ArmedForceStore armedForceStore,
        SpatialAuthorityStore spatialAuthorityStore,
        LocalTopologyStore localTopologyStore = null)
    {
        this.armedForceStore = armedForceStore ?? throw new ArgumentNullException(nameof(armedForceStore));
        this.spatialAuthorityStore = spatialAuthorityStore ?? throw new ArgumentNullException(nameof(spatialAuthorityStore));
        this.localTopologyStore = localTopologyStore;
    }

    /// <summary>
    /// Creates the separate P16-A proving owner with its authored initial force
    /// position and carried item before publication into a runtime profile.
    /// </summary>
    public static bool TryCreateP16A(
        ArmedForceStore armedForceStore,
        SpatialAuthorityStore spatialAuthorityStore,
        ArmedForceId selectedForceId,
        HexId initialHexId,
        string itemDefinitionId,
        string itemContentRevision,
        decimal initialQuantity,
        decimal quantityPerCrossing,
        long targetBoundaryDay,
        out ArmedForceSpatialStateStore store,
        out ArmedForceSpatialFailure failure)
    {
        store = null;
        if (armedForceStore == null || spatialAuthorityStore == null || selectedForceId == null
            || initialHexId == null || string.IsNullOrWhiteSpace(itemDefinitionId)
            || string.IsNullOrWhiteSpace(itemContentRevision) || initialQuantity < 0m
            || quantityPerCrossing <= 0m || targetBoundaryDay < 0L)
            return Fail(ArmedForceSpatialFailureCode.InvalidMovementInput,
                "P16-A initial truth requires one active force, registered Hex, compatible item revision, nonnegative finite stock, positive debit, and a nonnegative target day.", out failure);

        if (!armedForceStore.TryGet(selectedForceId, out ArmedForceRecord force))
            return Fail(ArmedForceSpatialFailureCode.ForceNotRegistered, "The selected P16-A ArmedForceId is not registered.", out failure);
        if (!force.IsActive)
            return Fail(ArmedForceSpatialFailureCode.ForceTerminated, "The selected P16-A ArmedForce must be active.", out failure);
        if (!spatialAuthorityStore.TryGet(initialHexId, out _))
            return Fail(ArmedForceSpatialFailureCode.SpatialReferenceNotRegistered,
                "The P16-A initial Hex is not registered.", out failure);

        P16AMilitaryMovementProfile profile;
        try
        {
            profile = new P16AMilitaryMovementProfile(selectedForceId, itemDefinitionId,
                itemContentRevision, initialQuantity, quantityPerCrossing, targetBoundaryDay);
        }
        catch (ArgumentException)
        {
            return Fail(ArmedForceSpatialFailureCode.InvalidMovementInput, "The P16-A profile inputs are invalid.", out failure);
        }

        store = new ArmedForceSpatialStateStore(armedForceStore, spatialAuthorityStore);
        store.p16Profile = profile;
        store.recordsByForceId.Add(selectedForceId.Value,
            new OperationalRecord(SpatialReference.ForHex(initialHexId), initialQuantity, null));
        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    /// <summary>Performs the profile's only successful crossing, consuming carried supply atomically.</summary>
    internal bool TryExecuteP16ACrossing(
        ArmedForceId forceId,
        HexId sourceHexId,
        HexId destinationHexId,
        TraversalOptionRef option,
        TraversalCostContext context,
        string operationId,
        long expectedOwnerRevision,
        long expectedForceStoreRevision,
        long expectedPassageAuthorityRevision,
        long logicalBoundary,
        long acceptedOrder,
        out P16ACrossingReceipt receipt,
        out ArmedForceSpatialFailure failure)
    {
        return TryExecuteP16ACrossingCore(forceId, sourceHexId, destinationHexId, option, context,
            operationId, expectedOwnerRevision, expectedForceStoreRevision,
            expectedPassageAuthorityRevision, logicalBoundary, acceptedOrder, null, null, out receipt, out failure);
    }

    /// <summary>Bound P17-A crossing entry; runtime supplies provenance only after capability admission.</summary>
    internal bool TryExecuteP17ACrossing(
        ArmedForceId forceId,
        HexId sourceHexId,
        HexId destinationHexId,
        TraversalOptionRef option,
        TraversalCostContext context,
        string operationId,
        long expectedOwnerRevision,
        long expectedForceStoreRevision,
        long expectedPassageAuthorityRevision,
        long logicalBoundary,
        long acceptedOrder,
        WorldCommandOrigin acceptedOrigin,
        string authorityId,
        out P16ACrossingReceipt receipt,
        out ArmedForceSpatialFailure failure)
    {
        return TryExecuteP16ACrossingCore(forceId, sourceHexId, destinationHexId, option, context,
            operationId, expectedOwnerRevision, expectedForceStoreRevision,
            expectedPassageAuthorityRevision, logicalBoundary, acceptedOrder, acceptedOrigin, authorityId,
            out receipt, out failure);
    }

    internal bool ConfigureP17AProvenance(string authorityId)
    {
        if (string.IsNullOrWhiteSpace(authorityId) || p16Profile == null || revision != 0L
            || !recordsByForceId.TryGetValue(p16Profile.SelectedForceId.Value, out OperationalRecord record)
            || record.Receipt != null || requiresP17AProvenance)
            return false;
        trustedP17AAuthorityId = authorityId;
        requiresP17AProvenance = true;
        return true;
    }

    private bool TryExecuteP16ACrossingCore(
        ArmedForceId forceId,
        HexId sourceHexId,
        HexId destinationHexId,
        TraversalOptionRef option,
        TraversalCostContext context,
        string operationId,
        long expectedOwnerRevision,
        long expectedForceStoreRevision,
        long expectedPassageAuthorityRevision,
        long logicalBoundary,
        long acceptedOrder,
        WorldCommandOrigin? acceptedOrigin,
        string authorityId,
        out P16ACrossingReceipt receipt,
        out ArmedForceSpatialFailure failure)
    {
        receipt = null;
        if (!mutationGuardBinding.CanMutate)
            return Fail(ArmedForceSpatialFailureCode.RuntimeFaulted, "The SimulationRuntime is faulted.", out failure);
        if (p16Profile == null)
            return Fail(ArmedForceSpatialFailureCode.MovementProfileNotConfigured, "This owner has no P16-A proving profile.", out failure);
        if (forceId == null)
            return Fail(ArmedForceSpatialFailureCode.InvalidMovementInput, "A selected ArmedForceId is required.", out failure);
        if (!armedForceStore.TryGet(forceId, out _))
            return Fail(ArmedForceSpatialFailureCode.ForceNotRegistered, "The movement ArmedForceId is not registered.", out failure);
        if (forceId != p16Profile.SelectedForceId)
            return Fail(ArmedForceSpatialFailureCode.ForceNotSelected, "The P16-A profile binds exactly one selected ArmedForceId.", out failure);
        bool validP17Origin = acceptedOrigin.HasValue
            && (acceptedOrigin.Value == WorldCommandOrigin.GM || acceptedOrigin.Value == WorldCommandOrigin.Scenario);
        if ((requiresP17AProvenance && (!validP17Origin || authorityId != trustedP17AAuthorityId))
            || (!requiresP17AProvenance && (acceptedOrigin.HasValue || !string.IsNullOrEmpty(authorityId))))
            return Fail(ArmedForceSpatialFailureCode.InvalidMovementInput,
                "P17-A crossing provenance must match the configured trusted scenario/GM authority.", out failure);
        if (TryResolveActiveForce(forceId, out failure) == false) return false;
        if (string.IsNullOrWhiteSpace(operationId) || sourceHexId == null || destinationHexId == null
            || option == null || context == null || logicalBoundary != p16Profile.TargetBoundaryDay
            || acceptedOrder != 0L
            || !IsP16AContext(context))
            return Fail(ArmedForceSpatialFailureCode.InvalidMovementInput,
                "Movement requires a stable operation at its prebound target day, adjacent Hex endpoints, explicit option, fixed P16-A context, and the sole P16-A invocation order.", out failure);
        if (expectedOwnerRevision != revision || expectedForceStoreRevision != armedForceStore.Revision
            || expectedPassageAuthorityRevision != spatialAuthorityStore.Revision)
            return Fail(ArmedForceSpatialFailureCode.MovementStateStale,
                "A force, spatial owner, or passage revision changed before execution.", out failure);
        if (!recordsByForceId.TryGetValue(forceId.Value, out OperationalRecord current)
            || current.Position == null || current.Position.Kind != SpatialReferenceKind.Hex
            || current.Position.HexId != sourceHexId)
            return Fail(ArmedForceSpatialFailureCode.MovementStateStale,
                "The selected force is not at the exact expected source Hex.", out failure);
        if (current.Receipt != null)
            return Fail(ArmedForceSpatialFailureCode.CrossingAlreadyCommitted,
                "The selected P16-A profile already committed its one successful crossing.", out failure);
        if (!spatialAuthorityStore.TryGet(sourceHexId, out _) || !spatialAuthorityStore.TryGet(destinationHexId, out _))
            return Fail(ArmedForceSpatialFailureCode.SpatialReferenceNotRegistered,
                "Both P16-A endpoints must be registered Hexes.", out failure);

        HexBoundaryKey boundary = new HexBoundaryKey(sourceHexId, destinationHexId);
        if (!TryGetPassageOption(boundary, option, out PassageOptionState optionState))
            return Fail(ArmedForceSpatialFailureCode.MovementRejected,
                "The selected passage option is not registered on this endpoint boundary.", out failure);
        if (!spatialAuthorityStore.PassageAuthority.TryEvaluatePassage(sourceHexId, destinationHexId,
                option, context, out PassageEvaluation evaluation, out SpatialAuthorityFailure passageFailure))
            return Fail(ArmedForceSpatialFailureCode.MovementRejected,
                "P8 rejected the passage evaluation.", out failure, passageFailure);
        if (!evaluation.IsAvailable)
            return Fail(ArmedForceSpatialFailureCode.MovementRejected,
                "P8 reports the selected passage blocked or closed.", out failure);
        if (current.Supply < p16Profile.QuantityPerCrossing)
            return Fail(ArmedForceSpatialFailureCode.InsufficientCarriedSupply,
                "The force does not carry enough of its compatible P16-A item.", out failure);
        if (CanAdvanceRevision(out failure) == false) return false;

        // Revalidate all effective inputs immediately before the single owner replacement.
        if (expectedOwnerRevision != revision || expectedForceStoreRevision != armedForceStore.Revision
            || expectedPassageAuthorityRevision != spatialAuthorityStore.Revision
            || !TryGetPassageOption(boundary, option, out PassageOptionState currentOption)
            || currentOption.ContentIdentity != optionState.ContentIdentity
            || currentOption.ContentRevision != optionState.ContentRevision
            || currentOption.Condition != evaluation.Condition
            || !spatialAuthorityStore.PassageAuthority.TryEvaluatePassage(sourceHexId, destinationHexId,
                option, context, out PassageEvaluation confirmed, out _)
            || !confirmed.IsAvailable)
            return Fail(ArmedForceSpatialFailureCode.MovementStateStale,
                "Force, owner, or passage facts changed during movement preparation.", out failure);

        decimal nextQuantity = current.Supply - p16Profile.QuantityPerCrossing;
        long nextRevision = revision + 1L;
        P16ACrossingReceipt committed = new P16ACrossingReceipt(operationId, forceId.Value,
            sourceHexId.Value, destinationHexId.Value, option, optionState.ContentIdentity,
            optionState.ContentRevision, context.MovementProfileIdentity, context.MovementProfileRevision,
            evaluation.Condition, expectedPassageAuthorityRevision, expectedForceStoreRevision,
            logicalBoundary, acceptedOrder, nextRevision, p16Profile.QuantityPerCrossing,
            acceptedOrigin, authorityId);
        recordsByForceId[forceId.Value] = new OperationalRecord(
            SpatialReference.ForHex(destinationHexId), nextQuantity, committed);
        revision = nextRevision;
        receipt = committed;
        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    public P16AStateSnapshot CaptureP16AState()
    {
        if (p16Profile == null) return null;
        recordsByForceId.TryGetValue(p16Profile.SelectedForceId.Value, out OperationalRecord record);
        if (record == null) return null;
        return new P16AStateSnapshot(p16Profile.SelectedForceId.Value, record.Position?.StableKey,
            p16Profile.ItemDefinitionId, p16Profile.ItemContentRevision, p16Profile.InitialQuantity,
            p16Profile.QuantityPerCrossing, p16Profile.TargetBoundaryDay, record.Supply, record.Receipt, revision,
            requiresP17AProvenance, trustedP17AAuthorityId);
    }

    /// <summary>Pure relationship/invariant seam for future exact staged hydration.</summary>
    public static bool ValidateP16AStateForHydration(
        P16AStateSnapshot state,
        ArmedForceStore armedForceStore,
        SpatialAuthorityStore spatialAuthorityStore,
        out string diagnostic)
    {
        diagnostic = string.Empty;
        if (state == null || armedForceStore == null || spatialAuthorityStore == null
            || string.IsNullOrWhiteSpace(state.ForceId) || string.IsNullOrWhiteSpace(state.ItemDefinitionId)
            || string.IsNullOrWhiteSpace(state.ItemContentRevision) || state.InitialQuantity < 0m
            || state.TargetBoundaryDay < 0L
            || state.QuantityPerCrossing <= 0m || state.CurrentQuantity < 0m || state.CurrentQuantity > state.InitialQuantity
            || state.OwnerRevision < 0
            || (state.RequiresP17AProvenance && string.IsNullOrWhiteSpace(state.TrustedAuthorityId))
            || (!state.RequiresP17AProvenance && !string.IsNullOrEmpty(state.TrustedAuthorityId)))
        { diagnostic = "P16-A staged state has invalid identities, quantities, or revision."; return false; }
        if (!armedForceStore.TryGet(new ArmedForceId(state.ForceId), out ArmedForceRecord force) || !force.IsActive)
        { diagnostic = "P16-A staged state does not resolve to an active ArmedForce."; return false; }
        if (string.IsNullOrWhiteSpace(state.PositionStableKey) || !state.PositionStableKey.StartsWith("hex:", StringComparison.Ordinal)
            || !spatialAuthorityStore.TryGet(new HexId(state.PositionStableKey.Substring("hex:".Length)), out _))
        { diagnostic = "P16-A staged state position does not resolve to a registered Hex."; return false; }
        if (state.Receipt == null)
        {
            if (state.CurrentQuantity != state.InitialQuantity)
            { diagnostic = "Unmoved P16-A staged state must retain its authored stock."; return false; }
        }
        else if (string.IsNullOrWhiteSpace(state.Receipt.OperationId)
            || state.Receipt.ForceId != state.ForceId || state.Receipt.OwnerRevision <= 0
            || state.Receipt.OwnerRevision > state.OwnerRevision
            || state.Receipt.ForceStoreRevision < 0 || state.Receipt.ForceStoreRevision > armedForceStore.Revision
            || state.Receipt.LogicalBoundary != state.TargetBoundaryDay || state.Receipt.AcceptedOrder != 0L
            || state.Receipt.SupplyDebited != state.QuantityPerCrossing
            || state.CurrentQuantity != state.InitialQuantity - state.QuantityPerCrossing
            || state.Receipt.SourceHexId == state.Receipt.DestinationHexId
            || state.PositionStableKey != "hex:" + state.Receipt.DestinationHexId
            || state.Receipt.Option == null
            || !Enum.IsDefined(typeof(TraversalOptionKind), state.Receipt.OptionKind)
            || state.Receipt.Option.Kind != state.Receipt.OptionKind
            || state.Receipt.TraversalContextIdentity != P16AMilitaryMovementProfile.TraversalContextIdentity
            || state.Receipt.TraversalContextRevision != P16AMilitaryMovementProfile.TraversalContextRevision
            || state.Receipt.PassageAuthorityRevision > spatialAuthorityStore.Revision
            || (state.RequiresP17AProvenance
                && (!state.Receipt.AcceptedOrigin.HasValue
                    || (state.Receipt.AcceptedOrigin.Value != WorldCommandOrigin.GM
                        && state.Receipt.AcceptedOrigin.Value != WorldCommandOrigin.Scenario)
                    || state.Receipt.AuthorityId != state.TrustedAuthorityId))
            || (!state.RequiresP17AProvenance
                && (state.Receipt.AcceptedOrigin.HasValue || !string.IsNullOrEmpty(state.Receipt.AuthorityId)))
            || !spatialAuthorityStore.TryGetGeometricBoundary(new HexId(state.Receipt.SourceHexId),
                new HexId(state.Receipt.DestinationHexId), out HexBoundaryKey receiptBoundary, out _)
            || !HasPassageReceiptRelationship(spatialAuthorityStore, receiptBoundary, state.Receipt)
            || !spatialAuthorityStore.TryGet(new HexId(state.Receipt.DestinationHexId), out _))
        { diagnostic = "P16-A staged receipt relationships or committed quantities are inconsistent."; return false; }
        return true;
    }

    public ArmedForceStore ArmedForceStore => armedForceStore;
    public SpatialAuthorityStore SpatialAuthorityStore => spatialAuthorityStore;
    public LocalTopologyStore LocalTopologyStore => localTopologyStore;
    public long Revision => revision;
    public int Count
    {
        get { int count = 0; foreach (OperationalRecord record in recordsByForceId.Values) if (record.Position != null) count++; return count; }
    }

    public P16AMilitaryMovementProfile P16Profile => p16Profile;
    public decimal? P16CurrentQuantity
    {
        get
        {
            if (p16Profile == null || !recordsByForceId.TryGetValue(p16Profile.SelectedForceId.Value, out OperationalRecord record))
                return null;
            return record.Supply;
        }
    }
    public P16ACrossingReceipt P16Receipt
    {
        get
        {
            if (p16Profile == null || !recordsByForceId.TryGetValue(p16Profile.SelectedForceId.Value, out OperationalRecord record))
                return null;
            return record.Receipt;
        }
    }

    public IReadOnlyList<ArmedForceSpatialPosition> Positions
    {
        get
        {
            List<ArmedForceSpatialPosition> result = new List<ArmedForceSpatialPosition>();
            foreach (KeyValuePair<string, OperationalRecord> entry in recordsByForceId)
            {
                if (entry.Value.Position != null)
                    result.Add(new ArmedForceSpatialPosition(new ArmedForceId(entry.Key), entry.Value.Position));
            }

            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.StableKey, right.StableKey));
            return new ReadOnlyCollection<ArmedForceSpatialPosition>(result);
        }
    }

    public bool TryGetPosition(ArmedForceId forceId, out SpatialReference position)
    {
        position = null;
        if (forceId != null && recordsByForceId.TryGetValue(forceId.Value, out OperationalRecord record))
        { position = record.Position; return position != null; }
        return false;
    }

    public bool TrySetPosition(
        ArmedForceId forceId,
        SpatialReference position,
        out ArmedForceSpatialFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.RuntimeFaulted,
                "The SimulationRuntime is faulted.");
            return false;
        }

        if (TryResolveActiveForce(forceId, out failure) == false)
        {
            return false;
        }

        if (p16Profile != null && p16Profile.SelectedForceId == forceId)
            return Fail(ArmedForceSpatialFailureCode.DirectPositionMutationBlocked,
                "The selected P16-A force can only relocate through its military crossing authority.", out failure);

        if (TryResolvePosition(position, out failure) == false)
        {
            return false;
        }

        if (recordsByForceId.TryGetValue(forceId.Value, out OperationalRecord currentRecord)
            && currentRecord.Position != null && currentRecord.Position.Equals(position))
        {
            failure = ArmedForceSpatialFailure.None;
            return true;
        }

        if (CanAdvanceRevision(out failure) == false)
        {
            return false;
        }

        recordsByForceId[forceId.Value] = new OperationalRecord(position, currentRecord?.Supply ?? 0m, currentRecord?.Receipt);
        revision++;
        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    public bool TryClearPosition(
        ArmedForceId forceId,
        out ArmedForceSpatialFailure failure)
    {
        if (!mutationGuardBinding.CanMutate)
        {
            failure = ArmedForceSpatialFailure.Create(
                ArmedForceSpatialFailureCode.RuntimeFaulted,
                "The SimulationRuntime is faulted.");
            return false;
        }

        if (TryResolveActiveForce(forceId, out failure) == false)
        {
            return false;
        }

        if (p16Profile != null && p16Profile.SelectedForceId == forceId)
            return Fail(ArmedForceSpatialFailureCode.DirectPositionMutationBlocked,
                "The selected P16-A force position is retained by its crossing state.", out failure);

        if (!recordsByForceId.TryGetValue(forceId.Value, out OperationalRecord record) || record.Position == null)
        {
            failure = ArmedForceSpatialFailure.None;
            return true;
        }

        if (CanAdvanceRevision(out failure) == false)
        {
            return false;
        }

        recordsByForceId.Remove(forceId.Value);
        revision++;
        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    /// <summary>
    /// Checks whether the force's current position proves presence in the
    /// required typed area. A valid query may return compatible=false when no
    /// current position exists or the references are incompatible. Invalid or
    /// unresolved references return false with a failure.
    /// </summary>
    public bool TryCheckCompatibility(
        ArmedForceId forceId,
        SpatialReference requiredArea,
        out bool compatible,
        out ArmedForceSpatialFailure failure)
    {
        compatible = false;
        if (TryResolveActiveForce(forceId, out failure) == false)
        {
            return false;
        }

        if (TryResolvePosition(requiredArea, out failure, "Required spatial area") == false)
        {
            return false;
        }

        if (!recordsByForceId.TryGetValue(forceId.Value, out OperationalRecord currentRecord)
            || currentRecord.Position == null)
        {
            failure = ArmedForceSpatialFailure.None;
            return true;
        }
        SpatialReference currentPosition = currentRecord.Position;

        if (TryResolvePosition(currentPosition, out failure, "Current spatial position") == false)
        {
            return false;
        }

        SpatialResolution requiredResolution;
        SpatialAuthorityFailure requiredFailure;
        spatialAuthorityStore.TryResolve(
            requiredArea,
            localTopologyStore,
            out requiredResolution,
            out requiredFailure);
        SpatialResolution currentResolution;
        SpatialAuthorityFailure currentFailure;
        spatialAuthorityStore.TryResolve(
            currentPosition,
            localTopologyStore,
            out currentResolution,
            out currentFailure);

        switch (requiredArea.Kind)
        {
            case SpatialReferenceKind.Hex:
                compatible = currentResolution.Hex != null
                    && requiredResolution.Hex != null
                    && currentResolution.Hex.Id == requiredResolution.Hex.Id;
                break;
            case SpatialReferenceKind.Location:
                compatible = currentResolution.Location != null
                    && requiredResolution.Location != null
                    && currentResolution.Location.Id == requiredResolution.Location.Id;
                break;
            case SpatialReferenceKind.SubLocation:
                compatible = currentPosition.Equals(requiredArea);
                break;
            default:
                return Fail(
                    ArmedForceSpatialFailureCode.InvalidSpatialReference,
                    "SpatialReference kind is invalid.",
                    out failure);
        }

        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    public ArmedForceSpatialInvariantReport ValidateInvariants()
    {
        List<string> violations = new List<string>();
        foreach (KeyValuePair<string, OperationalRecord> entry in recordsByForceId)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                violations.Add("ArmedForce spatial position has an empty force identity.");
                continue;
            }

            ArmedForceId forceId = new ArmedForceId(entry.Key);
            if (armedForceStore.TryGet(forceId, out _) == false)
            {
                violations.Add("ArmedForce spatial position references a missing force: " + entry.Key + ".");
            }

            if (entry.Value == null)
            {
                violations.Add("ArmedForce spatial operational record is null for " + entry.Key + ".");
                continue;
            }
            if (entry.Value.Position != null
                && TryResolvePosition(entry.Value.Position, out ArmedForceSpatialFailure failure) == false)
            {
                violations.Add("ArmedForce spatial position is unresolved for " + entry.Key + ": " + failure + ".");
            }
        }

        if (p16Profile != null)
        {
            if (!armedForceStore.TryGet(p16Profile.SelectedForceId, out ArmedForceRecord selected) || !selected.IsActive)
                violations.Add("P16-A selected force is absent or terminated.");
            if (!recordsByForceId.TryGetValue(p16Profile.SelectedForceId.Value, out OperationalRecord selectedRecord)
                || selectedRecord.Position == null || selectedRecord.Position.Kind != SpatialReferenceKind.Hex)
                violations.Add("P16-A selected force must retain a factual Hex position.");
            P16AStateSnapshot state = CaptureP16AState();
            if (!ValidateP16AStateForHydration(state, armedForceStore, spatialAuthorityStore, out string diagnostic))
                violations.Add("P16-A state is invalid: " + diagnostic);
        }

        return new ArmedForceSpatialInvariantReport(violations);
    }

    internal ArmedForceSpatialStateStore Clone(
        ArmedForceStore targetArmedForceStore,
        SpatialAuthorityStore targetSpatialAuthorityStore,
        LocalTopologyStore targetLocalTopologyStore)
    {
        if (targetArmedForceStore == null) throw new ArgumentNullException(nameof(targetArmedForceStore));
        if (targetSpatialAuthorityStore == null) throw new ArgumentNullException(nameof(targetSpatialAuthorityStore));

        ArmedForceSpatialStateStore copy = new ArmedForceSpatialStateStore(
            targetArmedForceStore,
            targetSpatialAuthorityStore,
            targetLocalTopologyStore);
        copy.revision = revision;
        copy.requiresP17AProvenance = requiresP17AProvenance;
        copy.trustedP17AAuthorityId = trustedP17AAuthorityId;
        foreach (KeyValuePair<string, OperationalRecord> entry in recordsByForceId)
        {
            if (targetArmedForceStore.TryGet(new ArmedForceId(entry.Key), out _) == false)
            {
                throw new ArgumentException(
                    "The ArmedForce spatial state references a force absent from the target ArmedForceStore.",
                    nameof(targetArmedForceStore));
            }

            if (entry.Value.Position != null && targetSpatialAuthorityStore.TryResolve(
                    entry.Value.Position, targetLocalTopologyStore, out _, out SpatialAuthorityFailure failure) == false)
            {
                throw new ArgumentException(
                    "The ArmedForce spatial state references an area absent from the target spatial authority: " + failure,
                    nameof(targetSpatialAuthorityStore));
            }

            copy.recordsByForceId.Add(entry.Key, new OperationalRecord(
                entry.Value.Position, entry.Value.Supply, entry.Value.Receipt));
        }

        if (p16Profile != null)
        {
            copy.p16Profile = new P16AMilitaryMovementProfile(
                new ArmedForceId(p16Profile.SelectedForceId.Value), p16Profile.ItemDefinitionId,
                p16Profile.ItemContentRevision, p16Profile.InitialQuantity, p16Profile.QuantityPerCrossing,
                p16Profile.TargetBoundaryDay);
            if (!ValidateP16AStateForHydration(copy.CaptureP16AState(), targetArmedForceStore,
                    targetSpatialAuthorityStore, out string diagnostic))
                throw new ArgumentException("The P16-A state is inconsistent with the target clone graph: " + diagnostic,
                    nameof(targetArmedForceStore));
        }

        return copy;
    }

    private bool TryResolveActiveForce(
        ArmedForceId forceId,
        out ArmedForceSpatialFailure failure)
    {
        if (forceId == null || armedForceStore.TryGet(forceId, out ArmedForceRecord force) == false)
        {
            return Fail(
                ArmedForceSpatialFailureCode.ForceNotRegistered,
                "The ArmedForceId is not registered.",
                out failure);
        }

        if (force.IsActive == false)
        {
            return Fail(
                ArmedForceSpatialFailureCode.ForceTerminated,
                "A terminated ArmedForce cannot receive a current physical position update or query.",
                out failure);
        }

        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    private bool TryGetPassageOption(HexBoundaryKey boundary, TraversalOptionRef option, out PassageOptionState state)
    {
        foreach (PassageOptionState candidate in spatialAuthorityStore.PassageAuthority.OptionStates)
        {
            if (candidate.Boundary.Equals(boundary) && candidate.Option.Equals(option))
            { state = candidate; return true; }
        }
        state = null;
        return false;
    }

    private static bool HasPassageReceiptRelationship(
        SpatialAuthorityStore authority,
        HexBoundaryKey boundary,
        P16ACrossingReceipt receipt)
    {
        foreach (PassageOptionState state in authority.PassageAuthority.OptionStates)
        {
            if (state.Boundary.Equals(boundary) && state.Option.Equals(receipt.Option))
                return state.ContentIdentity == receipt.OptionContentIdentity
                    && state.ContentRevision == receipt.OptionContentRevision;
        }
        return false;
    }

    private static bool IsP16AContext(TraversalCostContext context) =>
        context.MovementProfileIdentity == P16AMilitaryMovementProfile.TraversalContextIdentity
        && context.MovementProfileRevision == P16AMilitaryMovementProfile.TraversalContextRevision
        && context.EffortPerDistanceUnit == 1m && context.TerrainEffortMultiplier == 1m
        && context.ImpairedPassageEffortMultiplier == 1m;

    private sealed class OperationalRecord
    {
        public SpatialReference Position { get; }
        public decimal Supply { get; }
        public P16ACrossingReceipt Receipt { get; }
        public OperationalRecord(SpatialReference position, decimal supply, P16ACrossingReceipt receipt)
        { Position = position; Supply = supply; Receipt = receipt; }
    }

    private bool TryResolvePosition(
        SpatialReference position,
        out ArmedForceSpatialFailure failure,
        string label = "SpatialReference")
    {
        if (position == null)
        {
            return Fail(
                ArmedForceSpatialFailureCode.InvalidSpatialReference,
                label + " is required.",
                out failure);
        }

        if (spatialAuthorityStore.TryResolve(
                position,
                localTopologyStore,
                out _,
                out SpatialAuthorityFailure authorityFailure) == false)
        {
            return Fail(
                ArmedForceSpatialFailureCode.SpatialReferenceNotRegistered,
                label + " is not registered in the supplied SpatialAuthorityStore.",
                out failure,
                authorityFailure);
        }

        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    private bool CanAdvanceRevision(out ArmedForceSpatialFailure failure)
    {
        if (revision == long.MaxValue)
        {
            return Fail(
                ArmedForceSpatialFailureCode.RevisionOverflow,
                "ArmedForce spatial revision cannot advance further.",
                out failure);
        }

        failure = ArmedForceSpatialFailure.None;
        return true;
    }

    private static bool Fail(
        ArmedForceSpatialFailureCode code,
        string message,
        out ArmedForceSpatialFailure failure,
        SpatialAuthorityFailure authorityFailure = null)
    {
        failure = ArmedForceSpatialFailure.Create(code, message, authorityFailure);
        return false;
    }

    internal bool CanBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.CanBindTo(guard);
    internal bool TryBindMutationGuard(AuthoritativeMutationGuard guard) => mutationGuardBinding.TryBindTo(guard);
    bool IAuthoritativeMutationGuardBindable.CanBindMutationGuard(AuthoritativeMutationGuard guard) => CanBindMutationGuard(guard);
    bool IAuthoritativeMutationGuardBindable.TryBindMutationGuard(AuthoritativeMutationGuard guard) => TryBindMutationGuard(guard);
}
