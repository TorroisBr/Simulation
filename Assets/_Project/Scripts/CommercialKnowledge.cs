using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CommercialMarketObservation
{
    [SerializeField] private string locationRuntimeId;
    [SerializeField] private string itemDefinitionId;
    [NonSerialized] private ItemData itemDefinition;
    [SerializeField] private float observedPrice;
    [SerializeField] private int observedStock;
    [SerializeField] private long observedDay;
    [SerializeField] private long receivedDay;
    [SerializeField] private CommercialKnowledgeSource source;
    [SerializeField] private string sourceRuntimeId;

    public string LocationRuntimeId => locationRuntimeId;
    public string ItemDefinitionId => itemDefinitionId;
    public ItemData ItemDefinition => itemDefinition;
    public float ObservedPrice => observedPrice;
    public int ObservedStock => observedStock;
    public long ObservedDay => observedDay;
    public long ReceivedDay => receivedDay;
    public CommercialKnowledgeSource Source => source;
    public string SourceRuntimeId => sourceRuntimeId;

    public CommercialMarketObservation(
        string locationRuntimeId,
        ItemData itemDefinition,
        float observedPrice,
        int observedStock,
        long observedDay,
        long receivedDay,
        CommercialKnowledgeSource source)
        : this(
            locationRuntimeId,
            itemDefinition,
            observedPrice,
            observedStock,
            observedDay,
            receivedDay,
            source,
            null)
    {
    }

    public CommercialMarketObservation(
        string locationRuntimeId,
        ItemData itemDefinition,
        float observedPrice,
        int observedStock,
        long observedDay,
        long receivedDay,
        CommercialKnowledgeSource source,
        string sourceRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(locationRuntimeId) == true)
        {
            throw new ArgumentException("A commercial observation requires a LocationRuntimeId.", nameof(locationRuntimeId));
        }

        if (itemDefinition == null || string.IsNullOrWhiteSpace(itemDefinition.DefinitionId) == true)
        {
            throw new ArgumentException("A commercial observation requires an item with a DefinitionId.", nameof(itemDefinition));
        }

        if (observedDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(observedDay), "ObservedDay cannot be negative.");
        }

        if (receivedDay < observedDay)
        {
            throw new ArgumentOutOfRangeException(nameof(receivedDay), "ReceivedDay cannot be earlier than ObservedDay.");
        }

        if (source == CommercialKnowledgeSource.SharedByNpc
            && string.IsNullOrWhiteSpace(sourceRuntimeId) == true)
        {
            throw new ArgumentException("Shared commercial knowledge requires the source NPC RuntimeId.", nameof(sourceRuntimeId));
        }

        this.locationRuntimeId = locationRuntimeId;
        itemDefinitionId = itemDefinition.DefinitionId;
        this.itemDefinition = itemDefinition;
        this.observedPrice = UnityMathfCompatibility.Max(0f, observedPrice);
        this.observedStock = UnityMathfCompatibility.Max(0, observedStock);
        this.observedDay = observedDay;
        this.receivedDay = receivedDay;
        this.source = source;
        this.sourceRuntimeId = source == CommercialKnowledgeSource.SharedByNpc ? sourceRuntimeId : null;
    }
}

[Serializable]
public sealed class CommercialLiquidityObservation
{
    [SerializeField] private string locationRuntimeId;
    [SerializeField] private MarketLiquidityMode liquidityMode;
    [SerializeField] private float observedPurchasingPower;
    [SerializeField] private long observedDay;
    [SerializeField] private long receivedDay;
    [SerializeField] private CommercialKnowledgeSource source;
    [SerializeField] private string sourceRuntimeId;

    public string LocationRuntimeId => locationRuntimeId;
    public MarketLiquidityMode LiquidityMode => liquidityMode;
    public MarketLiquidityMode Mode => liquidityMode;
    public float ObservedPurchasingPower => observedPurchasingPower;
    public float PurchasingPower => observedPurchasingPower;
    public long ObservedDay => observedDay;
    public long ReceivedDay => receivedDay;
    public CommercialKnowledgeSource Source => source;
    public string SourceRuntimeId => sourceRuntimeId;
    public string SourceNpcRuntimeId => sourceRuntimeId;

    public CommercialLiquidityObservation(
        string locationRuntimeId,
        MarketLiquidityMode liquidityMode,
        float observedPurchasingPower,
        long observedDay,
        long receivedDay,
        CommercialKnowledgeSource source,
        string sourceRuntimeId = null)
    {
        if (string.IsNullOrWhiteSpace(locationRuntimeId) == true)
        {
            throw new ArgumentException("A liquidity observation requires a LocationRuntimeId.", nameof(locationRuntimeId));
        }

        if (liquidityMode != MarketLiquidityMode.Open
            && liquidityMode != MarketLiquidityMode.AccountBacked)
        {
            throw new ArgumentOutOfRangeException(nameof(liquidityMode));
        }

        if (liquidityMode == MarketLiquidityMode.AccountBacked
            && (float.IsNaN(observedPurchasingPower)
                || float.IsInfinity(observedPurchasingPower)
                || observedPurchasingPower < 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(observedPurchasingPower));
        }

        if (observedDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(observedDay));
        }

        if (receivedDay < observedDay)
        {
            throw new ArgumentOutOfRangeException(nameof(receivedDay));
        }

        if (source == CommercialKnowledgeSource.SharedByNpc
            && string.IsNullOrWhiteSpace(sourceRuntimeId) == true)
        {
            throw new ArgumentException("Shared liquidity knowledge requires the source NPC RuntimeId.", nameof(sourceRuntimeId));
        }

        this.locationRuntimeId = locationRuntimeId;
        this.liquidityMode = liquidityMode;
        this.observedPurchasingPower = liquidityMode == MarketLiquidityMode.Open ? 0f : observedPurchasingPower;
        this.observedDay = observedDay;
        this.receivedDay = receivedDay;
        this.source = source;
        this.sourceRuntimeId = source == CommercialKnowledgeSource.SharedByNpc ? sourceRuntimeId : null;
    }
}

/// <summary>Immutable value copy used by one retained commercial-sharing phase snapshot.</summary>
[Serializable]
public sealed class CommercialKnowledgeShareValue
{
    public bool IsMarketObservation { get; }
    public string LocationRuntimeId { get; }
    public string ItemDefinitionId { get; }
    public ItemData ItemDefinition { get; }
    public float ObservedPrice { get; }
    public int ObservedStock { get; }
    public MarketLiquidityMode LiquidityMode { get; }
    public float ObservedPurchasingPower { get; }
    public long ObservedDay { get; }
    public long SourceReceivedDay { get; }
    public CommercialKnowledgeSource Source { get; }
    public string SourceRuntimeId { get; }
    public string SortKey => IsMarketObservation ? "item:" + ItemDefinitionId : "liquidity";

    internal CommercialKnowledgeShareValue(string locationRuntimeId, string itemDefinitionId,
        ItemData itemDefinition, float observedPrice, int observedStock, MarketLiquidityMode liquidityMode,
        float observedPurchasingPower, long observedDay, long sourceReceivedDay,
        CommercialKnowledgeSource source, string sourceRuntimeId, bool isMarketObservation)
    {
        IsMarketObservation = isMarketObservation;
        LocationRuntimeId = locationRuntimeId;
        ItemDefinitionId = itemDefinitionId;
        ItemDefinition = itemDefinition;
        ObservedPrice = observedPrice;
        ObservedStock = observedStock;
        LiquidityMode = liquidityMode;
        ObservedPurchasingPower = observedPurchasingPower;
        ObservedDay = observedDay;
        SourceReceivedDay = sourceReceivedDay;
        Source = source;
        SourceRuntimeId = sourceRuntimeId;
    }

    public static CommercialKnowledgeShareValue Capture(CommercialMarketObservation observation)
    {
        if (observation == null || observation.ItemDefinition == null
            || observation.ItemDefinition.DefinitionId != observation.ItemDefinitionId)
            throw new ArgumentException("A market share snapshot requires a stable item definition.", nameof(observation));
        return new CommercialKnowledgeShareValue(observation.LocationRuntimeId, observation.ItemDefinitionId,
            observation.ItemDefinition, observation.ObservedPrice, observation.ObservedStock,
            MarketLiquidityMode.Open, 0f, observation.ObservedDay, observation.ReceivedDay,
            observation.Source, observation.SourceRuntimeId, true);
    }

    public static CommercialKnowledgeShareValue Capture(CommercialLiquidityObservation observation)
    {
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return new CommercialKnowledgeShareValue(observation.LocationRuntimeId, null, null, 0f, 0,
            observation.LiquidityMode, observation.ObservedPurchasingPower, observation.ObservedDay,
            observation.ReceivedDay, observation.Source, observation.SourceRuntimeId, false);
    }

    internal bool TryCreateShared(string senderRuntimeId, long receivedDay,
        out CommercialMarketObservation market, out CommercialLiquidityObservation liquidity)
    {
        market = null;
        liquidity = null;
        try
        {
            if (IsMarketObservation)
            {
                if (ItemDefinition == null || ItemDefinition.DefinitionId != ItemDefinitionId) return false;
                market = new CommercialMarketObservation(LocationRuntimeId, ItemDefinition, ObservedPrice,
                    ObservedStock, ObservedDay, receivedDay, CommercialKnowledgeSource.SharedByNpc,
                    senderRuntimeId);
            }
            else
            {
                liquidity = new CommercialLiquidityObservation(LocationRuntimeId, LiquidityMode,
                    ObservedPurchasingPower, ObservedDay, receivedDay,
                    CommercialKnowledgeSource.SharedByNpc, senderRuntimeId);
            }
            return true;
        }
        catch (ArgumentException)
        {
            market = null;
            liquidity = null;
            return false;
        }
    }

    internal string StableFingerprint => SpatialStableKey.Encode(
        IsMarketObservation ? "market" : "liquidity", LocationRuntimeId, ItemDefinitionId,
        ObservedPrice.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        ObservedStock.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ((int)LiquidityMode).ToString(System.Globalization.CultureInfo.InvariantCulture),
        ObservedPurchasingPower.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        ObservedDay.ToString(System.Globalization.CultureInfo.InvariantCulture),
        SourceReceivedDay.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ((int)Source).ToString(System.Globalization.CultureInfo.InvariantCulture), SourceRuntimeId);
}

/// <summary>One frozen sender-to-receiver operation owned by a recipient knowledge projection.</summary>
public sealed class CommercialKnowledgeShareBatch
{
    private readonly IReadOnlyList<CommercialKnowledgeShareValue> values;
    public string BoundaryOccurrenceId { get; }
    public string SenderRuntimeId { get; }
    public string SenderPersonId { get; }
    public string ReceiverRuntimeId { get; }
    public string ReceiverPersonId { get; }
    public string SourceSnapshotFingerprint { get; }
    public long ReceivedDay { get; }
    public int MaxSuccessfulUpdates { get; }
    public IReadOnlyList<CommercialKnowledgeShareValue> Values => values;
    public string OperationIdentity => SpatialStableKey.Encode(
        "commercial-sharing-edge/v1", BoundaryOccurrenceId, SenderRuntimeId, ReceiverRuntimeId);

    public CommercialKnowledgeShareBatch(string boundaryOccurrenceId, string senderRuntimeId,
        string senderPersonId, string receiverRuntimeId, string receiverPersonId,
        string sourceSnapshotFingerprint, long receivedDay, int maxSuccessfulUpdates,
        IReadOnlyList<CommercialKnowledgeShareValue> values)
    {
        if (string.IsNullOrWhiteSpace(boundaryOccurrenceId) || string.IsNullOrWhiteSpace(senderRuntimeId)
            || string.IsNullOrWhiteSpace(receiverRuntimeId) || senderRuntimeId == receiverRuntimeId
            || string.IsNullOrWhiteSpace(sourceSnapshotFingerprint) || receivedDay < 0L
            || maxSuccessfulUpdates <= 0 || values == null)
            throw new ArgumentException("Commercial sharing batch identity and bounds are required.");
        BoundaryOccurrenceId = boundaryOccurrenceId;
        SenderRuntimeId = senderRuntimeId;
        SenderPersonId = senderPersonId ?? string.Empty;
        ReceiverRuntimeId = receiverRuntimeId;
        ReceiverPersonId = receiverPersonId ?? string.Empty;
        SourceSnapshotFingerprint = sourceSnapshotFingerprint;
        ReceivedDay = receivedDay;
        MaxSuccessfulUpdates = maxSuccessfulUpdates;
        List<CommercialKnowledgeShareValue> copy = new List<CommercialKnowledgeShareValue>(values.Count);
        foreach (CommercialKnowledgeShareValue value in values)
            copy.Add(value ?? throw new ArgumentException("Commercial sharing values cannot be null.", nameof(values)));
        this.values = copy.AsReadOnly();
    }

    internal string Fingerprint
    {
        get
        {
            List<string> fields = new List<string>
            {
                OperationIdentity, SenderPersonId, ReceiverPersonId, SourceSnapshotFingerprint,
                ReceivedDay.ToString(System.Globalization.CultureInfo.InvariantCulture),
                MaxSuccessfulUpdates.ToString(System.Globalization.CultureInfo.InvariantCulture),
                values.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };
            foreach (CommercialKnowledgeShareValue value in values) fields.Add(value.StableFingerprint);
            return SpatialStableKey.Encode(fields.ToArray());
        }
    }
}

[Serializable]
public sealed class CommercialKnowledgeShareReceipt
{
    [SerializeField] private string operationIdentity;
    [SerializeField] private string fingerprint;
    [SerializeField] private string boundaryOccurrenceId;
    [SerializeField] private string senderRuntimeId;
    [SerializeField] private string senderPersonId;
    [SerializeField] private string receiverRuntimeId;
    [SerializeField] private string receiverPersonId;
    [SerializeField] private string sourceSnapshotFingerprint;
    [SerializeField] private int appliedObservationCount;

    public string OperationIdentity => operationIdentity;
    public string Fingerprint => fingerprint;
    public string BoundaryOccurrenceId => boundaryOccurrenceId;
    public string SenderRuntimeId => senderRuntimeId;
    public string SenderPersonId => senderPersonId;
    public string ReceiverRuntimeId => receiverRuntimeId;
    public string ReceiverPersonId => receiverPersonId;
    public string SourceSnapshotFingerprint => sourceSnapshotFingerprint;
    public int AppliedObservationCount => appliedObservationCount;

    internal CommercialKnowledgeShareReceipt(CommercialKnowledgeShareBatch batch, string fingerprint,
        int appliedObservationCount)
    {
        operationIdentity = batch.OperationIdentity;
        this.fingerprint = fingerprint;
        boundaryOccurrenceId = batch.BoundaryOccurrenceId;
        senderRuntimeId = batch.SenderRuntimeId;
        senderPersonId = batch.SenderPersonId;
        receiverRuntimeId = batch.ReceiverRuntimeId;
        receiverPersonId = batch.ReceiverPersonId;
        sourceSnapshotFingerprint = batch.SourceSnapshotFingerprint;
        this.appliedObservationCount = appliedObservationCount;
    }

    private CommercialKnowledgeShareReceipt(string operationIdentity, string fingerprint,
        string boundaryOccurrenceId, string senderRuntimeId, string senderPersonId,
        string receiverRuntimeId, string receiverPersonId, string sourceSnapshotFingerprint,
        int appliedObservationCount)
    {
        this.operationIdentity = operationIdentity;
        this.fingerprint = fingerprint;
        this.boundaryOccurrenceId = boundaryOccurrenceId;
        this.senderRuntimeId = senderRuntimeId;
        this.senderPersonId = senderPersonId;
        this.receiverRuntimeId = receiverRuntimeId;
        this.receiverPersonId = receiverPersonId;
        this.sourceSnapshotFingerprint = sourceSnapshotFingerprint;
        this.appliedObservationCount = appliedObservationCount;
    }

    /// <summary>Reconstructs an immutable captured receipt without replaying its sharing operation.</summary>
    internal static bool TryCreateFromOwnerSnapshot(string operationIdentity, string fingerprint,
        string boundaryOccurrenceId, string senderRuntimeId, string senderPersonId,
        string receiverRuntimeId, string receiverPersonId, string sourceSnapshotFingerprint,
        int appliedObservationCount, out CommercialKnowledgeShareReceipt receipt)
    {
        receipt = null;
        if (string.IsNullOrWhiteSpace(boundaryOccurrenceId)
            || string.IsNullOrWhiteSpace(senderRuntimeId) || string.IsNullOrWhiteSpace(receiverRuntimeId)
            || string.Equals(senderRuntimeId, receiverRuntimeId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(sourceSnapshotFingerprint)
            || string.IsNullOrWhiteSpace(fingerprint) || appliedObservationCount < 0
            || senderPersonId == null || receiverPersonId == null
            || !string.Equals(operationIdentity, SpatialStableKey.Encode(
                "commercial-sharing-edge/v1", boundaryOccurrenceId, senderRuntimeId, receiverRuntimeId),
                StringComparison.Ordinal)) return false;
        receipt = new CommercialKnowledgeShareReceipt(operationIdentity, fingerprint,
            boundaryOccurrenceId, senderRuntimeId, senderPersonId, receiverRuntimeId,
            receiverPersonId, sourceSnapshotFingerprint, appliedObservationCount);
        return true;
    }

    internal static CommercialKnowledgeShareReceipt FromSelectedOwner(
        DailyV1CommercialKnowledgeShareReceipt receipt)
    {
        if (receipt == null || !receipt.IsValid) return null;
        return new CommercialKnowledgeShareReceipt(receipt.OperationIdentity, receipt.Fingerprint,
            receipt.BoundaryOccurrenceId, receipt.SenderRuntimeId, receipt.SenderPersonId,
            receipt.ReceiverRuntimeId, receipt.ReceiverPersonId, receipt.SourceSnapshotFingerprint,
            receipt.AppliedObservationCount);
    }
}

/// <summary>Prepared recipient-owned batch. Knowledge and its edge receipt install together.</summary>
public sealed class CommercialKnowledgeShareBatchCommit
{
    private readonly CommercialKnowledgeRuntime owner;
    private readonly long expectedRevision;
    private readonly bool replay;
    private readonly List<CommercialMarketObservation> nextMarketObservations;
    private readonly List<CommercialLiquidityObservation> nextLiquidityObservations;
    private readonly List<CommercialKnowledgeShareReceipt> nextReceipts;
    private readonly CommercialKnowledgeShareReceipt receipt;
    private readonly DailyV1CommercialKnowledgeShareBatchCommit selectedCommit;

    internal CommercialKnowledgeShareBatchCommit(CommercialKnowledgeRuntime owner, long expectedRevision,
        bool replay, List<CommercialMarketObservation> nextMarketObservations,
        List<CommercialLiquidityObservation> nextLiquidityObservations,
        List<CommercialKnowledgeShareReceipt> nextReceipts, CommercialKnowledgeShareReceipt receipt)
    {
        this.owner = owner;
        this.expectedRevision = expectedRevision;
        this.replay = replay;
        this.nextMarketObservations = nextMarketObservations;
        this.nextLiquidityObservations = nextLiquidityObservations;
        this.nextReceipts = nextReceipts;
        this.receipt = receipt;
    }

    internal CommercialKnowledgeShareBatchCommit(CommercialKnowledgeRuntime owner,
        DailyV1CommercialKnowledgeShareBatchCommit selectedCommit,
        CommercialKnowledgeShareReceipt receipt)
    {
        this.owner = owner;
        this.selectedCommit = selectedCommit;
        this.receipt = receipt;
    }

    public CommercialKnowledgeShareReceipt Receipt => receipt;
    public bool TryCommit(out CommercialKnowledgeShareReceipt committed)
    {
        committed = null;
        if (owner == null || !owner.TryCommitShareBatch(this, out committed)) return false;
        return true;
    }

    internal long ExpectedRevision => expectedRevision;
    internal bool IsReplay => replay;
    internal List<CommercialMarketObservation> NextMarketObservations => nextMarketObservations;
    internal List<CommercialLiquidityObservation> NextLiquidityObservations => nextLiquidityObservations;
    internal List<CommercialKnowledgeShareReceipt> NextReceipts => nextReceipts;
    internal DailyV1CommercialKnowledgeShareBatchCommit SelectedCommit => selectedCommit;
}

[Serializable]
public sealed class CommercialKnowledgeRuntime
{
    [SerializeField] private List<CommercialMarketObservation> observations = new List<CommercialMarketObservation>();
    [SerializeField] private List<CommercialLiquidityObservation> liquidityObservations = new List<CommercialLiquidityObservation>();
    [SerializeField] private List<CommercialKnowledgeShareReceipt> shareReceipts = new List<CommercialKnowledgeShareReceipt>();
    [SerializeField] private long revision;
    [NonSerialized] private Func<bool> p12MutationAdmission;
    [NonSerialized] private Action p12MutationCommitted;
    [NonSerialized] private DailyV1CommercialKnowledgeOwner selectedOwner;
    [NonSerialized] private DailyV1DefinitionAdapter.SelectedBindings selectedBindings;
    [NonSerialized] private Dictionary<DailyV1CommercialKnowledgeShareReceipt, CommercialKnowledgeShareReceipt> selectedReceiptProjections;

    public IReadOnlyList<CommercialMarketObservation> Observations => selectedOwner == null
        ? ObservationList : ProjectSelectedMarketObservations();
    public IReadOnlyList<CommercialLiquidityObservation> LiquidityObservations => selectedOwner == null
        ? LiquidityObservationList : ProjectSelectedLiquidityObservations();
    public long Revision => selectedOwner == null ? revision : selectedOwner.Revision;
    internal DailyV1CommercialKnowledgeOwner SelectedOwner => selectedOwner;

    internal CommercialKnowledgeRuntime() { }

    internal CommercialKnowledgeRuntime(DailyV1CommercialKnowledgeOwner selectedOwner,
        DailyV1DefinitionAdapter.SelectedBindings selectedBindings)
    {
        if (selectedOwner == null) throw new ArgumentNullException(nameof(selectedOwner));
        this.selectedOwner = selectedOwner;
        this.selectedBindings = selectedBindings;
        selectedReceiptProjections = new Dictionary<DailyV1CommercialKnowledgeShareReceipt, CommercialKnowledgeShareReceipt>();
    }

    internal bool TryBindSelectedDailyV1Owner(DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings)
    {
        if (definitions == null || bindings == null) return false;
        if (selectedOwner != null)
            return ReferenceEquals(selectedOwner.Definitions, definitions)
                && TryBindSelectedDailyV1Owner(selectedOwner, bindings);
        if (!TryCreateSelectedOwner(definitions, bindings, out DailyV1CommercialKnowledgeOwner owner)) return false;
        return TryBindSelectedDailyV1Owner(owner, bindings);
    }

    internal bool TryCreateSelectedOwner(DailyV1EffectiveDefinitions definitions,
        DailyV1DefinitionAdapter.SelectedBindings bindings,
        out DailyV1CommercialKnowledgeOwner owner)
    {
        owner = null;
        if (definitions == null || bindings == null) return false;
        if (selectedOwner != null)
        {
            if (!ReferenceEquals(selectedOwner.Definitions, definitions)) return false;
            foreach (DailyV1CommercialMarketObservation row in selectedOwner.Observations)
                if (row == null || !bindings.TryGetAsset(row.ItemDefinition, out ItemData item)
                    || item == null || !bindings.TryGet(item, out DailyV1ItemDefinition node)
                    || !ReferenceEquals(node, row.ItemDefinition)) return false;
            owner = selectedOwner;
            return true;
        }

        List<DailyV1CommercialMarketObservation> markets = new List<DailyV1CommercialMarketObservation>(ObservationList.Count);
        foreach (CommercialMarketObservation row in ObservationList)
        {
            if (row == null || row.ItemDefinition == null
                || !bindings.TryGet(row.ItemDefinition, out DailyV1ItemDefinition node)
                || !definitions.Items.TryGetValue(node.DefinitionId, out DailyV1ItemDefinition admitted)
                || !ReferenceEquals(admitted, node)
                || !string.Equals(row.ItemDefinitionId, node.DefinitionId, StringComparison.Ordinal)
                || !TryToSelectedSource(row.Source, out DailyV1CommercialKnowledgeSource source)) return false;
            try
            {
                markets.Add(new DailyV1CommercialMarketObservation(row.LocationRuntimeId, node,
                    row.ObservedPrice, row.ObservedStock, row.ObservedDay, row.ReceivedDay,
                    source, row.SourceRuntimeId));
            }
            catch (ArgumentException) { return false; }
        }

        List<DailyV1CommercialLiquidityObservation> liquidity = new List<DailyV1CommercialLiquidityObservation>(LiquidityObservationList.Count);
        foreach (CommercialLiquidityObservation row in LiquidityObservationList)
        {
            if (row == null || !TryToSelectedSource(row.Source, out DailyV1CommercialKnowledgeSource source)) return false;
            try
            {
                liquidity.Add(new DailyV1CommercialLiquidityObservation(row.LocationRuntimeId,
                    row.LiquidityMode, row.ObservedPurchasingPower, row.ObservedDay,
                    row.ReceivedDay, source, row.SourceRuntimeId));
            }
            catch (ArgumentException) { return false; }
        }

        List<DailyV1CommercialKnowledgeShareReceipt> receipts = new List<DailyV1CommercialKnowledgeShareReceipt>(ShareReceipts.Count);
        foreach (CommercialKnowledgeShareReceipt row in ShareReceipts)
        {
            if (row == null || !CommercialKnowledgeShareReceipt.TryCreateFromOwnerSnapshot(
                    row.OperationIdentity, row.Fingerprint, row.BoundaryOccurrenceId,
                    row.SenderRuntimeId, row.SenderPersonId, row.ReceiverRuntimeId,
                    row.ReceiverPersonId, row.SourceSnapshotFingerprint,
                    row.AppliedObservationCount, out CommercialKnowledgeShareReceipt validated)) return false;
            receipts.Add(new DailyV1CommercialKnowledgeShareReceipt(
                validated.OperationIdentity, validated.Fingerprint, validated.BoundaryOccurrenceId,
                validated.SenderRuntimeId, validated.SenderPersonId, validated.ReceiverRuntimeId,
                validated.ReceiverPersonId, validated.SourceSnapshotFingerprint,
                validated.AppliedObservationCount));
        }
        return DailyV1CommercialKnowledgeOwner.TryCreateFromOwnerSnapshot(
            definitions, markets, liquidity, receipts, revision, out owner);
    }

    internal bool TryBindSelectedDailyV1Owner(DailyV1CommercialKnowledgeOwner owner,
        DailyV1DefinitionAdapter.SelectedBindings bindings)
    {
        if (owner == null || bindings == null) return false;
        if (selectedOwner != null && !ReferenceEquals(selectedOwner, owner)) return false;
        if (!owner.TryCopyOwnerSnapshot(
                out IReadOnlyList<DailyV1CommercialMarketObservation> markets,
                out IReadOnlyList<DailyV1CommercialLiquidityObservation> liquidity,
                out IReadOnlyList<DailyV1CommercialKnowledgeShareReceipt> receipts)) return false;
        foreach (DailyV1CommercialMarketObservation row in markets)
        {
            if (row == null || !bindings.TryGetAsset(row.ItemDefinition, out ItemData item)
                || item == null || !bindings.TryGet(item, out DailyV1ItemDefinition node)
                || !ReferenceEquals(node, row.ItemDefinition)) return false;
        }
        if (selectedOwner != null)
        {
            selectedBindings = bindings;
            return true;
        }
        if ((p12MutationAdmission == null) != (p12MutationCommitted == null)) return false;
        if (p12MutationAdmission != null)
        {
            try { owner.BindMutationBoundary(p12MutationAdmission, p12MutationCommitted); }
            catch (InvalidOperationException) { return false; }
        }
        selectedOwner = owner;
        selectedBindings = bindings;
        observations = new List<CommercialMarketObservation>();
        liquidityObservations = new List<CommercialLiquidityObservation>();
        shareReceipts = new List<CommercialKnowledgeShareReceipt>();
        revision = 0L;
        selectedReceiptProjections = new Dictionary<DailyV1CommercialKnowledgeShareReceipt, CommercialKnowledgeShareReceipt>();
        return true;
    }

    private static bool TryToSelectedSource(CommercialKnowledgeSource source,
        out DailyV1CommercialKnowledgeSource selectedSource)
    {
        selectedSource = default;
        if (!Enum.IsDefined(typeof(CommercialKnowledgeSource), source)) return false;
        selectedSource = (DailyV1CommercialKnowledgeSource)(int)source;
        return Enum.IsDefined(typeof(DailyV1CommercialKnowledgeSource), selectedSource);
    }

    private static CommercialKnowledgeSource FromSelectedSource(DailyV1CommercialKnowledgeSource source) =>
        (CommercialKnowledgeSource)(int)source;

    private bool TryCreateSelectedMarketObservation(CommercialMarketObservation observation,
        out DailyV1CommercialMarketObservation selected)
    {
        selected = null;
        if (selectedOwner == null || selectedBindings == null || observation == null
            || observation.ItemDefinition == null
            || !selectedBindings.TryGet(observation.ItemDefinition, out DailyV1ItemDefinition node)
            || !selectedOwner.Definitions.Items.TryGetValue(node.DefinitionId, out DailyV1ItemDefinition admitted)
            || !ReferenceEquals(node, admitted)
            || !string.Equals(observation.ItemDefinitionId, node.DefinitionId, StringComparison.Ordinal)
            || !TryToSelectedSource(observation.Source, out DailyV1CommercialKnowledgeSource source)) return false;
        try
        {
            selected = new DailyV1CommercialMarketObservation(observation.LocationRuntimeId,
                node, observation.ObservedPrice, observation.ObservedStock, observation.ObservedDay,
                observation.ReceivedDay, source, observation.SourceRuntimeId);
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    private static bool TryCreateSelectedLiquidityObservation(CommercialLiquidityObservation observation,
        out DailyV1CommercialLiquidityObservation selected)
    {
        selected = null;
        if (observation == null || !TryToSelectedSource(observation.Source,
                out DailyV1CommercialKnowledgeSource source)) return false;
        try
        {
            selected = new DailyV1CommercialLiquidityObservation(observation.LocationRuntimeId,
                observation.LiquidityMode, observation.ObservedPurchasingPower, observation.ObservedDay,
                observation.ReceivedDay, source, observation.SourceRuntimeId);
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    private bool TryProjectSelectedMarketObservation(DailyV1CommercialMarketObservation observation,
        out CommercialMarketObservation projected)
    {
        projected = null;
        if (observation == null || selectedBindings == null
            || !selectedBindings.TryGetAsset(observation.ItemDefinition, out ItemData item) || item == null)
            return false;
        try
        {
            projected = new CommercialMarketObservation(observation.LocationRuntimeId, item,
                observation.ObservedPrice, observation.ObservedStock, observation.ObservedDay,
                observation.ReceivedDay, FromSelectedSource(observation.Source), observation.SourceRuntimeId);
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    private static bool TryProjectSelectedLiquidityObservation(
        DailyV1CommercialLiquidityObservation observation,
        out CommercialLiquidityObservation projected)
    {
        projected = null;
        if (observation == null) return false;
        try
        {
            projected = new CommercialLiquidityObservation(observation.LocationRuntimeId,
                observation.LiquidityMode, observation.ObservedPurchasingPower, observation.ObservedDay,
                observation.ReceivedDay, FromSelectedSource(observation.Source), observation.SourceRuntimeId);
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    private IReadOnlyList<CommercialMarketObservation> ProjectSelectedMarketObservations()
    {
        if (selectedBindings == null || selectedOwner == null) return Array.Empty<CommercialMarketObservation>();
        List<CommercialMarketObservation> projected = new List<CommercialMarketObservation>(selectedOwner.Observations.Count);
        foreach (DailyV1CommercialMarketObservation row in selectedOwner.Observations)
        {
            if (row == null || !selectedBindings.TryGetAsset(row.ItemDefinition, out ItemData item) || item == null)
                return Array.Empty<CommercialMarketObservation>();
            projected.Add(new CommercialMarketObservation(row.LocationRuntimeId, item,
                row.ObservedPrice, row.ObservedStock, row.ObservedDay, row.ReceivedDay,
                FromSelectedSource(row.Source), row.SourceRuntimeId));
        }
        return projected.AsReadOnly();
    }

    private IReadOnlyList<CommercialLiquidityObservation> ProjectSelectedLiquidityObservations()
    {
        if (selectedOwner == null) return Array.Empty<CommercialLiquidityObservation>();
        List<CommercialLiquidityObservation> projected = new List<CommercialLiquidityObservation>(selectedOwner.LiquidityObservations.Count);
        foreach (DailyV1CommercialLiquidityObservation row in selectedOwner.LiquidityObservations)
        {
            if (row == null) return Array.Empty<CommercialLiquidityObservation>();
            projected.Add(new CommercialLiquidityObservation(row.LocationRuntimeId, row.LiquidityMode,
                row.ObservedPurchasingPower, row.ObservedDay, row.ReceivedDay,
                FromSelectedSource(row.Source), row.SourceRuntimeId));
        }
        return projected.AsReadOnly();
    }

    private CommercialKnowledgeShareReceipt ProjectSelectedReceipt(DailyV1CommercialKnowledgeShareReceipt receipt)
    {
        if (receipt == null || !receipt.IsValid) return null;
        if (selectedReceiptProjections == null)
            selectedReceiptProjections = new Dictionary<DailyV1CommercialKnowledgeShareReceipt, CommercialKnowledgeShareReceipt>();
        if (!selectedReceiptProjections.TryGetValue(receipt, out CommercialKnowledgeShareReceipt projected))
        {
            projected = CommercialKnowledgeShareReceipt.FromSelectedOwner(receipt);
            if (projected != null) selectedReceiptProjections.Add(receipt, projected);
        }
        return projected;
    }
    internal bool TryReadCensus(out int marketCount, out int liquidityCount, out int shareReceiptCount, out long currentRevision)
    {
        if (selectedOwner != null)
            return selectedOwner.TryReadCensus(out marketCount, out liquidityCount, out shareReceiptCount, out currentRevision);
        marketCount = 0; liquidityCount = 0; shareReceiptCount = 0; currentRevision = revision;
        if (observations == null || liquidityObservations == null || shareReceipts == null) return false;
        marketCount = observations.Count;
        liquidityCount = liquidityObservations.Count;
        shareReceiptCount = shareReceipts.Count;
        return true;
    }
    private List<CommercialKnowledgeShareReceipt> ShareReceipts => shareReceipts ?? (shareReceipts = new List<CommercialKnowledgeShareReceipt>());

    private List<CommercialMarketObservation> ObservationList => observations ?? (observations = new List<CommercialMarketObservation>());
    private List<CommercialLiquidityObservation> LiquidityObservationList => liquidityObservations ?? (liquidityObservations = new List<CommercialLiquidityObservation>());

    /// <summary>Copies the exact owner lists for a detached snapshot, rejecting absent backing state.</summary>
    internal bool TryCopyOwnerSnapshot(
        out IReadOnlyList<CommercialMarketObservation> markets,
        out IReadOnlyList<CommercialLiquidityObservation> liquidity,
        out IReadOnlyList<CommercialKnowledgeShareReceipt> receipts)
    {
        markets = null;
        liquidity = null;
        receipts = null;
        if (selectedOwner != null)
        {
            if (selectedBindings == null
                || !selectedOwner.TryCopyOwnerSnapshot(
                    out IReadOnlyList<DailyV1CommercialMarketObservation> selectedMarkets,
                    out IReadOnlyList<DailyV1CommercialLiquidityObservation> selectedLiquidity,
                    out IReadOnlyList<DailyV1CommercialKnowledgeShareReceipt> selectedReceipts)) return false;
            List<CommercialMarketObservation> projectedMarkets = new List<CommercialMarketObservation>(selectedMarkets.Count);
            foreach (DailyV1CommercialMarketObservation row in selectedMarkets)
            {
                if (row == null || !selectedBindings.TryGetAsset(row.ItemDefinition, out ItemData item) || item == null) return false;
                projectedMarkets.Add(new CommercialMarketObservation(row.LocationRuntimeId, item,
                    row.ObservedPrice, row.ObservedStock, row.ObservedDay, row.ReceivedDay,
                    FromSelectedSource(row.Source), row.SourceRuntimeId));
            }
            List<CommercialLiquidityObservation> projectedLiquidity = new List<CommercialLiquidityObservation>(selectedLiquidity.Count);
            foreach (DailyV1CommercialLiquidityObservation row in selectedLiquidity)
            {
                if (row == null) return false;
                projectedLiquidity.Add(new CommercialLiquidityObservation(row.LocationRuntimeId,
                    row.LiquidityMode, row.ObservedPurchasingPower, row.ObservedDay,
                    row.ReceivedDay, FromSelectedSource(row.Source), row.SourceRuntimeId));
            }
            List<CommercialKnowledgeShareReceipt> projectedReceipts = new List<CommercialKnowledgeShareReceipt>(selectedReceipts.Count);
            foreach (DailyV1CommercialKnowledgeShareReceipt row in selectedReceipts)
            {
                CommercialKnowledgeShareReceipt projected = ProjectSelectedReceipt(row);
                if (projected == null) return false;
                projectedReceipts.Add(projected);
            }
            markets = projectedMarkets.AsReadOnly();
            liquidity = projectedLiquidity.AsReadOnly();
            receipts = projectedReceipts.AsReadOnly();
            return true;
        }
        if (observations == null || liquidityObservations == null || shareReceipts == null) return false;
        markets = new List<CommercialMarketObservation>(observations).AsReadOnly();
        liquidity = new List<CommercialLiquidityObservation>(liquidityObservations).AsReadOnly();
        receipts = new List<CommercialKnowledgeShareReceipt>(shareReceipts).AsReadOnly();
        return true;
    }

    /// <summary>Builds an unpublished exact owner without replaying observations or sharing batches.</summary>
    internal static bool TryCreateFromOwnerSnapshot(
        IReadOnlyList<CommercialMarketObservation> markets,
        IReadOnlyList<CommercialLiquidityObservation> liquidity,
        IReadOnlyList<CommercialKnowledgeShareReceipt> receipts,
        long exactRevision,
        out CommercialKnowledgeRuntime staged)
    {
        staged = null;
        if (markets == null || liquidity == null || receipts == null || exactRevision < 0L) return false;
        CommercialKnowledgeRuntime candidate = new CommercialKnowledgeRuntime();
        HashSet<string> marketKeys = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> liquidityKeys = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> receiptIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (CommercialMarketObservation row in markets)
        {
            if (row == null || row.ItemDefinition == null
                || !string.Equals(row.ItemDefinition.DefinitionId, row.ItemDefinitionId, StringComparison.Ordinal)
                || !marketKeys.Add(SpatialStableKey.Encode(row.LocationRuntimeId, row.ItemDefinitionId)))
                return false;
            candidate.observations.Add(row);
        }
        foreach (CommercialLiquidityObservation row in liquidity)
        {
            if (row == null || !liquidityKeys.Add(row.LocationRuntimeId)) return false;
            candidate.liquidityObservations.Add(row);
        }
        foreach (CommercialKnowledgeShareReceipt row in receipts)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.OperationIdentity)
                || !receiptIds.Add(row.OperationIdentity)) return false;
            candidate.shareReceipts.Add(row);
        }
        candidate.revision = exactRevision;
        staged = candidate;
        return true;
    }

    private bool TryCreateSelectedShareBatch(CommercialKnowledgeShareBatch batch,
        out DailyV1CommercialKnowledgeShareBatch selectedBatch)
    {
        selectedBatch = null;
        if (selectedOwner == null || selectedBindings == null || batch == null) return false;
        List<DailyV1CommercialKnowledgeShareValue> values =
            new List<DailyV1CommercialKnowledgeShareValue>(batch.Values.Count);
        foreach (CommercialKnowledgeShareValue value in batch.Values)
        {
            if (value == null || !TryToSelectedSource(value.Source,
                    out DailyV1CommercialKnowledgeSource source)) return false;
            DailyV1ItemDefinition item = null;
            if (value.IsMarketObservation)
            {
                if (value.ItemDefinition == null
                    || !selectedBindings.TryGet(value.ItemDefinition, out item)
                    || !selectedOwner.Definitions.Items.TryGetValue(value.ItemDefinitionId,
                        out DailyV1ItemDefinition admitted)
                    || !ReferenceEquals(item, admitted)
                    || !string.Equals(item.DefinitionId, value.ItemDefinitionId,
                        StringComparison.Ordinal)) return false;
            }
            else if (value.ItemDefinition != null || value.ItemDefinitionId != null)
            {
                return false;
            }
            values.Add(new DailyV1CommercialKnowledgeShareValue(value.LocationRuntimeId,
                item, value.ObservedPrice, value.ObservedStock, value.LiquidityMode,
                value.ObservedPurchasingPower, value.ObservedDay, value.SourceReceivedDay,
                source, value.SourceRuntimeId, value.IsMarketObservation));
        }
        try
        {
            selectedBatch = new DailyV1CommercialKnowledgeShareBatch(
                batch.BoundaryOccurrenceId, batch.SenderRuntimeId, batch.SenderPersonId,
                batch.ReceiverRuntimeId, batch.ReceiverPersonId, batch.SourceSnapshotFingerprint,
                batch.ReceivedDay, batch.MaxSuccessfulUpdates, values);
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    public bool RecordObservation(CommercialMarketObservation observation)
    {
        if (selectedOwner != null)
            return TryCreateSelectedMarketObservation(observation, out DailyV1CommercialMarketObservation selected)
                && selectedOwner.RecordObservation(selected);
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindObservationIndex(observation.LocationRuntimeId, observation.ItemDefinitionId);

        if (existingIndex < 0)
        {
            if (revision == long.MaxValue) return false;
            if (!CanCommitP12Mutation()) return false;
            ObservationList.Add(observation);
            revision++;
            NotifyP12MutationCommitted();
            return true;
        }

        CommercialMarketObservation existing = ObservationList[existingIndex];

        if (ShouldReplace(existing, observation) == false)
        {
            return false;
        }

        if (revision == long.MaxValue) return false;
        if (!CanCommitP12Mutation()) return false;
        ObservationList[existingIndex] = observation;
        revision++;
        NotifyP12MutationCommitted();
        return true;
    }

    public bool CanImproveWith(CommercialMarketObservation observation)
    {
        if (selectedOwner != null)
            return TryCreateSelectedMarketObservation(observation, out DailyV1CommercialMarketObservation selected)
                && selectedOwner.CanImproveWith(selected);
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindObservationIndex(observation.LocationRuntimeId, observation.ItemDefinitionId);
        return existingIndex < 0 || ShouldReplace(ObservationList[existingIndex], observation);
    }

    public bool TryGetObservation(string locationRuntimeId, string itemDefinitionId, out CommercialMarketObservation observation)
    {
        if (selectedOwner != null)
        {
            observation = null;
            return selectedOwner.TryGetObservation(locationRuntimeId, itemDefinitionId,
                    out DailyV1CommercialMarketObservation selected)
                && TryProjectSelectedMarketObservation(selected, out observation);
        }
        int index = FindObservationIndex(locationRuntimeId, itemDefinitionId);

        if (index >= 0)
        {
            observation = ObservationList[index];
            return true;
        }

        observation = null;
        return false;
    }

    public bool RecordLiquidityObservation(CommercialLiquidityObservation observation)
    {
        if (selectedOwner != null)
            return TryCreateSelectedLiquidityObservation(observation,
                    out DailyV1CommercialLiquidityObservation selected)
                && selectedOwner.RecordLiquidityObservation(selected);
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindLiquidityObservationIndex(observation.LocationRuntimeId);

        if (existingIndex < 0)
        {
            if (revision == long.MaxValue) return false;
            if (!CanCommitP12Mutation()) return false;
            LiquidityObservationList.Add(observation);
            revision++;
            NotifyP12MutationCommitted();
            return true;
        }

        if (ShouldReplace(LiquidityObservationList[existingIndex], observation) == false)
        {
            return false;
        }

        if (revision == long.MaxValue) return false;
        if (!CanCommitP12Mutation()) return false;
        LiquidityObservationList[existingIndex] = observation;
        revision++;
        NotifyP12MutationCommitted();
        return true;
    }

    public bool CanImproveWith(CommercialLiquidityObservation observation)
    {
        if (selectedOwner != null)
            return TryCreateSelectedLiquidityObservation(observation,
                    out DailyV1CommercialLiquidityObservation selected)
                && selectedOwner.CanImproveWith(selected);
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindLiquidityObservationIndex(observation.LocationRuntimeId);
        return existingIndex < 0 || ShouldReplace(LiquidityObservationList[existingIndex], observation);
    }

    public bool TryGetLiquidityObservation(string locationRuntimeId, out CommercialLiquidityObservation observation)
    {
        if (selectedOwner != null)
        {
            observation = null;
            return selectedOwner.TryGetLiquidityObservation(locationRuntimeId,
                    out DailyV1CommercialLiquidityObservation selected)
                && TryProjectSelectedLiquidityObservation(selected, out observation);
        }
        int index = FindLiquidityObservationIndex(locationRuntimeId);

        if (index >= 0)
        {
            observation = LiquidityObservationList[index];
            return true;
        }

        observation = null;
        return false;
    }

    internal bool TryPrepareDirectObservationBatch(
        IReadOnlyList<CommercialMarketObservation> marketObservations,
        CommercialLiquidityObservation liquidityObservation,
        out CommercialKnowledgeObservationInstall prepared)
    {
        prepared = null;
        if (marketObservations == null) return false;
        if (selectedOwner != null)
        {
            List<DailyV1CommercialMarketObservation> selectedMarkets =
                new List<DailyV1CommercialMarketObservation>(marketObservations.Count);
            foreach (CommercialMarketObservation observation in marketObservations)
            {
                if (!TryCreateSelectedMarketObservation(observation,
                        out DailyV1CommercialMarketObservation selected)) return false;
                selectedMarkets.Add(selected);
            }
            DailyV1CommercialLiquidityObservation liquidity = null;
            if (liquidityObservation != null
                && !TryCreateSelectedLiquidityObservation(liquidityObservation, out liquidity)) return false;
            if (!selectedOwner.TryPrepareDirectObservationBatch(selectedMarkets, liquidity,
                    out DailyV1CommercialKnowledgeObservationInstall selectedPrepared)) return false;
            prepared = new CommercialKnowledgeObservationInstall(this, selectedPrepared);
            return true;
        }

        List<CommercialMarketObservation> nextMarkets =
            new List<CommercialMarketObservation>(ObservationList);
        List<CommercialLiquidityObservation> nextLiquidity =
            new List<CommercialLiquidityObservation>(LiquidityObservationList);
        long appliedChanges = 0L;

        foreach (CommercialMarketObservation observation in marketObservations)
        {
            if (observation == null || observation.ItemDefinition == null
                || observation.ItemDefinition.DefinitionId != observation.ItemDefinitionId) return false;

            int index = FindObservationIndex(nextMarkets,
                observation.LocationRuntimeId, observation.ItemDefinitionId);
            if (index < 0)
            {
                nextMarkets.Add(observation);
                appliedChanges++;
            }
            else if (ShouldReplace(nextMarkets[index], observation))
            {
                nextMarkets[index] = observation;
                appliedChanges++;
            }
        }

        if (liquidityObservation != null)
        {
            int index = FindLiquidityObservationIndex(nextLiquidity,
                liquidityObservation.LocationRuntimeId);
            if (index < 0)
            {
                nextLiquidity.Add(liquidityObservation);
                appliedChanges++;
            }
            else if (ShouldReplace(nextLiquidity[index], liquidityObservation))
            {
                nextLiquidity[index] = liquidityObservation;
                appliedChanges++;
            }
        }

        if (appliedChanges > long.MaxValue - revision) return false;
        prepared = new CommercialKnowledgeObservationInstall(this, revision,
            revision + appliedChanges, nextMarkets, nextLiquidity);
        return true;
    }

    internal bool CanInstall(CommercialKnowledgeObservationInstall prepared)
    {
        if (prepared == null || prepared.Owner != this) return false;
        if (prepared.SelectedInstall != null)
            return selectedOwner != null && selectedOwner.CanInstall(prepared.SelectedInstall);
        return selectedOwner == null && revision == prepared.ExpectedRevision
            && (prepared.NextRevision == revision || CanCommitP12Mutation());
    }

    internal void InstallPrepared(CommercialKnowledgeObservationInstall prepared)
    {
        if (prepared != null && prepared.Owner == this && prepared.SelectedInstall != null)
        {
            if (selectedOwner != null) selectedOwner.InstallPrepared(prepared.SelectedInstall);
            return;
        }
        if (prepared == null || prepared.Owner != this || revision != prepared.ExpectedRevision
            || prepared.NextRevision == revision || !CanCommitP12Mutation()) return;
        observations = prepared.NextMarketObservations;
        liquidityObservations = prepared.NextLiquidityObservations;
        revision = prepared.NextRevision;
        NotifyP12MutationCommitted();
    }

    public bool TryGetShareReceipt(string operationIdentity, out CommercialKnowledgeShareReceipt receipt)
    {
        receipt = null;
        if (string.IsNullOrWhiteSpace(operationIdentity)) return false;
        if (selectedOwner != null)
        {
            return selectedOwner.TryGetShareReceipt(operationIdentity,
                    out DailyV1CommercialKnowledgeShareReceipt selectedReceipt)
                && (receipt = ProjectSelectedReceipt(selectedReceipt)) != null;
        }
        foreach (CommercialKnowledgeShareReceipt candidate in ShareReceipts)
        {
            if (candidate == null || candidate.OperationIdentity != operationIdentity) continue;
            receipt = candidate;
            return true;
        }
        return false;
    }

    public bool TryPrepareShareBatch(CommercialKnowledgeShareBatch batch,
        out CommercialKnowledgeShareBatchCommit prepared)
    {
        prepared = null;
        if (batch == null) return false;
        if (selectedOwner != null)
        {
            if (!TryCreateSelectedShareBatch(batch, out DailyV1CommercialKnowledgeShareBatch selectedBatch)
                || !selectedOwner.TryPrepareShareBatch(selectedBatch,
                    out DailyV1CommercialKnowledgeShareBatchCommit selectedPrepared)) return false;
            CommercialKnowledgeShareReceipt selectedReceipt = ProjectSelectedReceipt(selectedPrepared.Receipt);
            if (selectedReceipt == null) return false;
            prepared = new CommercialKnowledgeShareBatchCommit(this, selectedPrepared, selectedReceipt);
            return true;
        }
        string fingerprint = batch.Fingerprint;
        if (TryGetShareReceipt(batch.OperationIdentity, out CommercialKnowledgeShareReceipt existing))
        {
            if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal)) return false;
            prepared = new CommercialKnowledgeShareBatchCommit(this, revision, true,
                null, null, null, existing);
            return true;
        }
        if (revision == long.MaxValue) return false;

        List<CommercialMarketObservation> nextMarkets = new List<CommercialMarketObservation>(ObservationList);
        List<CommercialLiquidityObservation> nextLiquidity = new List<CommercialLiquidityObservation>(LiquidityObservationList);
        int applied = 0;
        foreach (CommercialKnowledgeShareValue value in batch.Values)
        {
            if (!value.TryCreateShared(batch.SenderRuntimeId, batch.ReceivedDay,
                    out CommercialMarketObservation market, out CommercialLiquidityObservation liquidity)) return false;
            if (market != null)
            {
                int index = FindObservationIndex(nextMarkets, market.LocationRuntimeId, market.ItemDefinitionId);
                if (index < 0)
                {
                    nextMarkets.Add(market);
                    applied++;
                }
                else if (ShouldReplace(nextMarkets[index], market))
                {
                    nextMarkets[index] = market;
                    applied++;
                }
            }
            else
            {
                int index = FindLiquidityObservationIndex(nextLiquidity, liquidity.LocationRuntimeId);
                if (index < 0)
                {
                    nextLiquidity.Add(liquidity);
                    applied++;
                }
                else if (ShouldReplace(nextLiquidity[index], liquidity))
                {
                    nextLiquidity[index] = liquidity;
                    applied++;
                }
            }
            if (applied >= batch.MaxSuccessfulUpdates) break;
        }

        CommercialKnowledgeShareReceipt receipt = new CommercialKnowledgeShareReceipt(batch, fingerprint, applied);
        List<CommercialKnowledgeShareReceipt> nextReceipts = new List<CommercialKnowledgeShareReceipt>(ShareReceipts)
        { receipt };
        prepared = new CommercialKnowledgeShareBatchCommit(this, revision, false,
            nextMarkets, nextLiquidity, nextReceipts, receipt);
        return true;
    }

    internal bool TryCommitShareBatch(CommercialKnowledgeShareBatchCommit commit,
        out CommercialKnowledgeShareReceipt receipt)
    {
        receipt = null;
        if (commit == null) return false;
        if (commit.SelectedCommit != null)
        {
            if (selectedOwner == null
                || !selectedOwner.TryCommitShareBatch(commit.SelectedCommit,
                    out DailyV1CommercialKnowledgeShareReceipt selectedReceipt)) return false;
            receipt = ProjectSelectedReceipt(selectedReceipt);
            return receipt != null;
        }
        if (selectedOwner != null) return false;
        if (commit.IsReplay)
        {
            if (TryGetShareReceipt(commit.Receipt.OperationIdentity, out CommercialKnowledgeShareReceipt existing)
                && existing.Fingerprint == commit.Receipt.Fingerprint)
            {
                receipt = existing;
                return true;
            }
            return false;
        }
        if (revision != commit.ExpectedRevision || revision == long.MaxValue
            || commit.NextMarketObservations == null || commit.NextLiquidityObservations == null
            || commit.NextReceipts == null || TryGetShareReceipt(commit.Receipt.OperationIdentity, out _)
            || !CanCommitP12Mutation()) return false;
        observations = commit.NextMarketObservations;
        liquidityObservations = commit.NextLiquidityObservations;
        shareReceipts = commit.NextReceipts;
        revision++;
        NotifyP12MutationCommitted();
        receipt = commit.Receipt;
        return true;
    }

    internal void BindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (p12MutationAdmission != null || p12MutationCommitted != null)
            throw new InvalidOperationException("CommercialKnowledgeRuntime is already bound to a P12 mutation boundary.");
        if (selectedOwner != null) selectedOwner.BindMutationBoundary(admission, committed);
        p12MutationAdmission = admission;
        p12MutationCommitted = committed;
    }

    internal bool UnbindP12MutationBoundary(Func<bool> admission, Action committed)
    {
        if (!ReferenceEquals(p12MutationAdmission, admission)
            || !ReferenceEquals(p12MutationCommitted, committed)) return false;
        if (selectedOwner != null && !selectedOwner.UnbindMutationBoundary(admission, committed)) return false;
        p12MutationAdmission = null;
        p12MutationCommitted = null;
        return true;
    }

    private bool CanCommitP12Mutation()
    {
        if (p12MutationAdmission == null) return true;
        try { return p12MutationAdmission(); }
        catch { return false; }
    }

    private void NotifyP12MutationCommitted()
    {
        if (p12MutationCommitted == null) return;
        try { p12MutationCommitted(); }
        catch { }
    }

    private int FindObservationIndex(string locationRuntimeId, string itemDefinitionId)
    {
        return FindObservationIndex(ObservationList, locationRuntimeId, itemDefinitionId);
    }

    private static int FindObservationIndex(IReadOnlyList<CommercialMarketObservation> source,
        string locationRuntimeId, string itemDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(locationRuntimeId) == true || string.IsNullOrWhiteSpace(itemDefinitionId) == true)
        {
            return -1;
        }

        for (int i = 0; i < source.Count; i++)
        {
            CommercialMarketObservation candidate = source[i];

            if (candidate != null
                && string.Equals(candidate.LocationRuntimeId, locationRuntimeId, StringComparison.Ordinal) == true
                && string.Equals(candidate.ItemDefinitionId, itemDefinitionId, StringComparison.Ordinal) == true)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindLiquidityObservationIndex(string locationRuntimeId)
    {
        return FindLiquidityObservationIndex(LiquidityObservationList, locationRuntimeId);
    }

    private static int FindLiquidityObservationIndex(IReadOnlyList<CommercialLiquidityObservation> source,
        string locationRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(locationRuntimeId) == true)
        {
            return -1;
        }

        for (int i = 0; i < source.Count; i++)
        {
            CommercialLiquidityObservation candidate = source[i];

            if (candidate != null
                && string.Equals(candidate.LocationRuntimeId, locationRuntimeId, StringComparison.Ordinal) == true)
            {
                return i;
            }
        }

        return -1;
    }

    private static bool ShouldReplace(CommercialMarketObservation existing, CommercialMarketObservation incoming)
    {
        if (existing == null)
        {
            return true;
        }

        if (incoming.ObservedDay != existing.ObservedDay)
        {
            return incoming.ObservedDay > existing.ObservedDay;
        }

        int incomingPriority = GetSourcePriority(incoming.Source);
        int existingPriority = GetSourcePriority(existing.Source);

        // Same observation day and source priority preserves the existing snapshot.
        return incomingPriority > existingPriority;
    }

    private static bool ShouldReplace(CommercialLiquidityObservation existing, CommercialLiquidityObservation incoming)
    {
        if (existing == null)
        {
            return true;
        }

        if (incoming.ObservedDay != existing.ObservedDay)
        {
            return incoming.ObservedDay > existing.ObservedDay;
        }

        return GetSourcePriority(incoming.Source) > GetSourcePriority(existing.Source);
    }

    public static int GetSourcePriority(CommercialKnowledgeSource source)
    {
        switch (source)
        {
            case CommercialKnowledgeSource.DirectObservation:
                return 3;
            case CommercialKnowledgeSource.SharedByNpc:
                return 2;
            case CommercialKnowledgeSource.InitialScenarioKnowledge:
                return 1;
            default:
                return 0;
        }
    }
}

internal sealed class CommercialKnowledgeObservationInstall
{
    internal CommercialKnowledgeRuntime Owner { get; }
    internal DailyV1CommercialKnowledgeObservationInstall SelectedInstall { get; }
    internal long ExpectedRevision { get; }
    internal long NextRevision { get; }
    internal List<CommercialMarketObservation> NextMarketObservations { get; }
    internal List<CommercialLiquidityObservation> NextLiquidityObservations { get; }

    internal CommercialKnowledgeObservationInstall(CommercialKnowledgeRuntime owner,
        long expectedRevision, long nextRevision,
        List<CommercialMarketObservation> nextMarketObservations,
        List<CommercialLiquidityObservation> nextLiquidityObservations)
    {
        Owner = owner;
        ExpectedRevision = expectedRevision;
        NextRevision = nextRevision;
        NextMarketObservations = nextMarketObservations;
        NextLiquidityObservations = nextLiquidityObservations;
    }

    internal CommercialKnowledgeObservationInstall(CommercialKnowledgeRuntime owner,
        DailyV1CommercialKnowledgeObservationInstall selectedInstall)
    {
        Owner = owner;
        SelectedInstall = selectedInstall;
        if (selectedInstall != null)
        {
            ExpectedRevision = selectedInstall.ExpectedRevision;
            NextRevision = selectedInstall.NextRevision;
        }
    }

    internal bool CanInstall(CommercialKnowledgeRuntime owner) => owner != null
        && owner.CanInstall(this);
}

[Serializable]
public sealed class CommercialKnowledgeSettings
{
    public int freshForDays = 7;
    public int maxUsefulAgeDays = 30;
    public int maxSharedObservationsPerInteraction = 2;
}

public sealed class CommercialKnowledgePolicy
{
    private const int DefaultFreshForDays = 7;
    private const int DefaultMaxUsefulAgeDays = 30;

    private readonly int freshForDays;
    private readonly int maxUsefulAgeDays;

    public int FreshForDays => freshForDays;
    public int MaxUsefulAgeDays => maxUsefulAgeDays;

    public CommercialKnowledgePolicy(EffectiveCommercialKnowledgeConfiguration configuration)
    {
        int configuredFreshDays = configuration != null
            ? configuration.FreshForDays
            : DefaultFreshForDays;
        int configuredMaxAge = configuration != null
            ? configuration.MaxUsefulAgeDays
            : DefaultMaxUsefulAgeDays;

        freshForDays = Math.Min(Math.Max(0, configuredFreshDays), int.MaxValue - 1);

        if (configuredMaxAge <= 0)
        {
            configuredMaxAge = DefaultMaxUsefulAgeDays;
        }

        maxUsefulAgeDays = configuredMaxAge > freshForDays
            ? configuredMaxAge
            : freshForDays + 1;
    }

    public long GetAgeDays(CommercialMarketObservation observation, long currentAbsoluteDay)
    {
        return observation == null ? 0L : GetAgeDays(observation.ObservedDay, currentAbsoluteDay);
    }

    public float GetFreshness(CommercialMarketObservation observation, long currentAbsoluteDay)
    {
        return observation == null ? 0f : GetFreshness(observation.ObservedDay, currentAbsoluteDay);
    }

    internal long GetAgeDays(DailyV1CommercialMarketObservation observation, long currentAbsoluteDay)
    {
        return observation == null ? 0L : GetAgeDays(observation.ObservedDay, currentAbsoluteDay);
    }

    internal float GetFreshness(DailyV1CommercialMarketObservation observation, long currentAbsoluteDay)
    {
        return observation == null ? 0f : DailyV1CommercialKnowledgeFreshness.GetFreshness(
            observation.ObservedDay,
            currentAbsoluteDay,
            new EffectiveCommercialKnowledgeConfiguration(freshForDays, maxUsefulAgeDays));
    }

    public long GetAgeDays(CommercialLiquidityObservation observation, long currentAbsoluteDay)
    {
        return observation == null ? 0L : GetAgeDays(observation.ObservedDay, currentAbsoluteDay);
    }

    public float GetFreshness(CommercialLiquidityObservation observation, long currentAbsoluteDay)
    {
        return observation == null ? 0f : GetFreshness(observation.ObservedDay, currentAbsoluteDay);
    }

    internal long GetAgeDays(DailyV1CommercialLiquidityObservation observation, long currentAbsoluteDay)
    {
        return observation == null ? 0L : GetAgeDays(observation.ObservedDay, currentAbsoluteDay);
    }

    internal float GetFreshness(DailyV1CommercialLiquidityObservation observation, long currentAbsoluteDay)
    {
        return observation == null ? 0f : DailyV1CommercialKnowledgeFreshness.GetFreshness(
            observation.ObservedDay,
            currentAbsoluteDay,
            new EffectiveCommercialKnowledgeConfiguration(freshForDays, maxUsefulAgeDays));
    }

    private long GetAgeDays(long observedDay, long currentAbsoluteDay)
    {
        if (currentAbsoluteDay <= observedDay)
            return 0L;
        return currentAbsoluteDay - observedDay;
    }

    private float GetFreshness(long observedDay, long currentAbsoluteDay)
    {
        long ageDays = GetAgeDays(observedDay, Math.Max(0L, currentAbsoluteDay));
        if (ageDays <= freshForDays)
            return 1f;
        if (ageDays >= maxUsefulAgeDays)
            return 0f;
        float decayRange = maxUsefulAgeDays - freshForDays;
        return 1f - (ageDays - freshForDays) / decayRange;
    }
}
