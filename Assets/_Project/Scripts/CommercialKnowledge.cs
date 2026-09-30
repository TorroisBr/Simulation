using System;
using System.Collections.Generic;
using UnityEngine;

public enum CommercialKnowledgeSource
{
    DirectObservation,
    InitialScenarioKnowledge,
    SharedByNpc
}

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
        this.observedPrice = Mathf.Max(0f, observedPrice);
        this.observedStock = Mathf.Max(0, observedStock);
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

    private CommercialKnowledgeShareValue(string locationRuntimeId, string itemDefinitionId,
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
}

[Serializable]
public sealed class CommercialKnowledgeRuntime
{
    [SerializeField] private List<CommercialMarketObservation> observations = new List<CommercialMarketObservation>();
    [SerializeField] private List<CommercialLiquidityObservation> liquidityObservations = new List<CommercialLiquidityObservation>();
    [SerializeField] private List<CommercialKnowledgeShareReceipt> shareReceipts = new List<CommercialKnowledgeShareReceipt>();
    [SerializeField] private long revision;

    public IReadOnlyList<CommercialMarketObservation> Observations => ObservationList;
    public IReadOnlyList<CommercialLiquidityObservation> LiquidityObservations => LiquidityObservationList;
    public long Revision => revision;
    internal bool TryReadCensus(out int marketCount, out int liquidityCount, out int shareReceiptCount, out long currentRevision)
    {
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

    public bool RecordObservation(CommercialMarketObservation observation)
    {
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindObservationIndex(observation.LocationRuntimeId, observation.ItemDefinitionId);

        if (existingIndex < 0)
        {
            if (revision == long.MaxValue) return false;
            ObservationList.Add(observation);
            revision++;
            return true;
        }

        CommercialMarketObservation existing = ObservationList[existingIndex];

        if (ShouldReplace(existing, observation) == false)
        {
            return false;
        }

        if (revision == long.MaxValue) return false;
        ObservationList[existingIndex] = observation;
        revision++;
        return true;
    }

    public bool CanImproveWith(CommercialMarketObservation observation)
    {
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindObservationIndex(observation.LocationRuntimeId, observation.ItemDefinitionId);
        return existingIndex < 0 || ShouldReplace(ObservationList[existingIndex], observation);
    }

    public bool TryGetObservation(string locationRuntimeId, string itemDefinitionId, out CommercialMarketObservation observation)
    {
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
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindLiquidityObservationIndex(observation.LocationRuntimeId);

        if (existingIndex < 0)
        {
            if (revision == long.MaxValue) return false;
            LiquidityObservationList.Add(observation);
            revision++;
            return true;
        }

        if (ShouldReplace(LiquidityObservationList[existingIndex], observation) == false)
        {
            return false;
        }

        if (revision == long.MaxValue) return false;
        LiquidityObservationList[existingIndex] = observation;
        revision++;
        return true;
    }

    public bool CanImproveWith(CommercialLiquidityObservation observation)
    {
        if (observation == null)
        {
            return false;
        }

        int existingIndex = FindLiquidityObservationIndex(observation.LocationRuntimeId);
        return existingIndex < 0 || ShouldReplace(LiquidityObservationList[existingIndex], observation);
    }

    public bool TryGetLiquidityObservation(string locationRuntimeId, out CommercialLiquidityObservation observation)
    {
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

    internal bool CanInstall(CommercialKnowledgeObservationInstall prepared) =>
        prepared != null && prepared.Owner == this && revision == prepared.ExpectedRevision;

    internal void InstallPrepared(CommercialKnowledgeObservationInstall prepared)
    {
        observations = prepared.NextMarketObservations;
        liquidityObservations = prepared.NextLiquidityObservations;
        revision = prepared.NextRevision;
    }

    public bool TryGetShareReceipt(string operationIdentity, out CommercialKnowledgeShareReceipt receipt)
    {
        receipt = null;
        if (string.IsNullOrWhiteSpace(operationIdentity)) return false;
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
            || commit.NextReceipts == null || TryGetShareReceipt(commit.Receipt.OperationIdentity, out _)) return false;
        observations = commit.NextMarketObservations;
        liquidityObservations = commit.NextLiquidityObservations;
        shareReceipts = commit.NextReceipts;
        revision++;
        receipt = commit.Receipt;
        return true;
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
        if (observation == null || currentAbsoluteDay <= observation.ObservedDay)
        {
            return 0L;
        }

        return currentAbsoluteDay - observation.ObservedDay;
    }

    public float GetFreshness(CommercialMarketObservation observation, long currentAbsoluteDay)
    {
        if (observation == null)
        {
            return 0f;
        }

        long ageDays = GetAgeDays(observation, Math.Max(0L, currentAbsoluteDay));

        if (ageDays <= freshForDays)
        {
            return 1f;
        }

        if (ageDays >= maxUsefulAgeDays)
        {
            return 0f;
        }

        float decayRange = maxUsefulAgeDays - freshForDays;
        return 1f - (ageDays - freshForDays) / decayRange;
    }

    public long GetAgeDays(CommercialLiquidityObservation observation, long currentAbsoluteDay)
    {
        if (observation == null || currentAbsoluteDay <= observation.ObservedDay)
        {
            return 0L;
        }

        return currentAbsoluteDay - observation.ObservedDay;
    }

    public float GetFreshness(CommercialLiquidityObservation observation, long currentAbsoluteDay)
    {
        if (observation == null)
        {
            return 0f;
        }

        long ageDays = GetAgeDays(observation, Math.Max(0L, currentAbsoluteDay));

        if (ageDays <= freshForDays)
        {
            return 1f;
        }

        if (ageDays >= maxUsefulAgeDays)
        {
            return 0f;
        }

        float decayRange = maxUsefulAgeDays - freshForDays;
        return 1f - (ageDays - freshForDays) / decayRange;
    }
}
