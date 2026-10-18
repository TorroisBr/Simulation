using System;
using System.Collections.Generic;
using System.Globalization;

internal enum DailyV1CommercialKnowledgeSource
{
    DirectObservation,
    InitialScenarioKnowledge,
    SharedByNpc
}

internal sealed class DailyV1CommercialMarketObservation
{
    internal string LocationRuntimeId { get; }
    internal DailyV1ItemDefinition ItemDefinition { get; }
    internal string ItemDefinitionId => ItemDefinition?.DefinitionId;
    internal float ObservedPrice { get; }
    internal int ObservedStock { get; }
    internal long ObservedDay { get; }
    internal long ReceivedDay { get; }
    internal DailyV1CommercialKnowledgeSource Source { get; }
    internal string SourceRuntimeId { get; }

    internal DailyV1CommercialMarketObservation(string locationRuntimeId,
        DailyV1ItemDefinition itemDefinition, float observedPrice, int observedStock,
        long observedDay, long receivedDay, DailyV1CommercialKnowledgeSource source,
        string sourceRuntimeId = null)
    {
        if (string.IsNullOrWhiteSpace(locationRuntimeId))
            throw new ArgumentException("A commercial observation requires a LocationRuntimeId.", nameof(locationRuntimeId));
        if (itemDefinition == null || string.IsNullOrWhiteSpace(itemDefinition.DefinitionId))
            throw new ArgumentException("A commercial observation requires an admitted item definition.", nameof(itemDefinition));
        if (observedDay < 0L) throw new ArgumentOutOfRangeException(nameof(observedDay));
        if (receivedDay < observedDay) throw new ArgumentOutOfRangeException(nameof(receivedDay));
        if (!Enum.IsDefined(typeof(DailyV1CommercialKnowledgeSource), source)) throw new ArgumentOutOfRangeException(nameof(source));
        if (source == DailyV1CommercialKnowledgeSource.SharedByNpc && string.IsNullOrWhiteSpace(sourceRuntimeId))
            throw new ArgumentException("Shared commercial knowledge requires the source NPC RuntimeId.", nameof(sourceRuntimeId));

        LocationRuntimeId = locationRuntimeId;
        ItemDefinition = itemDefinition;
        ObservedPrice = UnityMathfCompatibility.Max(0f, observedPrice);
        ObservedStock = UnityMathfCompatibility.Max(0, observedStock);
        ObservedDay = observedDay;
        ReceivedDay = receivedDay;
        Source = source;
        SourceRuntimeId = source == DailyV1CommercialKnowledgeSource.SharedByNpc ? sourceRuntimeId : null;
    }
}

internal sealed class DailyV1CommercialLiquidityObservation
{
    internal string LocationRuntimeId { get; }
    internal MarketLiquidityMode LiquidityMode { get; }
    internal float ObservedPurchasingPower { get; }
    internal long ObservedDay { get; }
    internal long ReceivedDay { get; }
    internal DailyV1CommercialKnowledgeSource Source { get; }
    internal string SourceRuntimeId { get; }

    internal DailyV1CommercialLiquidityObservation(string locationRuntimeId,
        MarketLiquidityMode liquidityMode, float observedPurchasingPower, long observedDay,
        long receivedDay, DailyV1CommercialKnowledgeSource source, string sourceRuntimeId = null)
    {
        if (string.IsNullOrWhiteSpace(locationRuntimeId))
            throw new ArgumentException("A liquidity observation requires a LocationRuntimeId.", nameof(locationRuntimeId));
        if (liquidityMode != MarketLiquidityMode.Open && liquidityMode != MarketLiquidityMode.AccountBacked)
            throw new ArgumentOutOfRangeException(nameof(liquidityMode));
        if (liquidityMode == MarketLiquidityMode.AccountBacked
            && (float.IsNaN(observedPurchasingPower) || float.IsInfinity(observedPurchasingPower)
                || observedPurchasingPower < 0f))
            throw new ArgumentOutOfRangeException(nameof(observedPurchasingPower));
        if (observedDay < 0L) throw new ArgumentOutOfRangeException(nameof(observedDay));
        if (receivedDay < observedDay) throw new ArgumentOutOfRangeException(nameof(receivedDay));
        if (!Enum.IsDefined(typeof(DailyV1CommercialKnowledgeSource), source)) throw new ArgumentOutOfRangeException(nameof(source));
        if (source == DailyV1CommercialKnowledgeSource.SharedByNpc && string.IsNullOrWhiteSpace(sourceRuntimeId))
            throw new ArgumentException("Shared liquidity knowledge requires the source NPC RuntimeId.", nameof(sourceRuntimeId));

        LocationRuntimeId = locationRuntimeId;
        LiquidityMode = liquidityMode;
        ObservedPurchasingPower = liquidityMode == MarketLiquidityMode.Open ? 0f : observedPurchasingPower;
        ObservedDay = observedDay;
        ReceivedDay = receivedDay;
        Source = source;
        SourceRuntimeId = source == DailyV1CommercialKnowledgeSource.SharedByNpc ? sourceRuntimeId : null;
    }
}

internal sealed class DailyV1CommercialKnowledgeShareValue
{
    internal bool IsMarketObservation { get; }
    internal string LocationRuntimeId { get; }
    internal DailyV1ItemDefinition ItemDefinition { get; }
    internal string ItemDefinitionId => ItemDefinition?.DefinitionId;
    internal float ObservedPrice { get; }
    internal int ObservedStock { get; }
    internal MarketLiquidityMode LiquidityMode { get; }
    internal float ObservedPurchasingPower { get; }
    internal long ObservedDay { get; }
    internal long SourceReceivedDay { get; }
    internal DailyV1CommercialKnowledgeSource Source { get; }
    internal string SourceRuntimeId { get; }

    internal DailyV1CommercialKnowledgeShareValue(string locationRuntimeId,
        DailyV1ItemDefinition itemDefinition, float observedPrice, int observedStock,
        MarketLiquidityMode liquidityMode, float observedPurchasingPower,
        long observedDay, long sourceReceivedDay, DailyV1CommercialKnowledgeSource source,
        string sourceRuntimeId, bool isMarketObservation)
    {
        LocationRuntimeId = locationRuntimeId;
        ItemDefinition = itemDefinition;
        ObservedPrice = observedPrice;
        ObservedStock = observedStock;
        LiquidityMode = liquidityMode;
        ObservedPurchasingPower = observedPurchasingPower;
        ObservedDay = observedDay;
        SourceReceivedDay = sourceReceivedDay;
        Source = source;
        SourceRuntimeId = sourceRuntimeId;
        IsMarketObservation = isMarketObservation;
    }

    internal static DailyV1CommercialKnowledgeShareValue Capture(DailyV1CommercialMarketObservation observation)
    {
        if (observation == null || observation.ItemDefinition == null
            || !string.Equals(observation.ItemDefinition.DefinitionId, observation.ItemDefinitionId, StringComparison.Ordinal))
            throw new ArgumentException("A market share snapshot requires an exact admitted item node.", nameof(observation));
        return new DailyV1CommercialKnowledgeShareValue(observation.LocationRuntimeId,
            observation.ItemDefinition, observation.ObservedPrice, observation.ObservedStock,
            MarketLiquidityMode.Open, 0f, observation.ObservedDay, observation.ReceivedDay,
            observation.Source, observation.SourceRuntimeId, true);
    }

    internal static DailyV1CommercialKnowledgeShareValue Capture(DailyV1CommercialLiquidityObservation observation)
    {
        if (observation == null) throw new ArgumentNullException(nameof(observation));
        return new DailyV1CommercialKnowledgeShareValue(observation.LocationRuntimeId,
            null, 0f, 0, observation.LiquidityMode, observation.ObservedPurchasingPower,
            observation.ObservedDay, observation.ReceivedDay, observation.Source,
            observation.SourceRuntimeId, false);
    }

    internal bool TryCreateShared(DailyV1EffectiveDefinitions definitions, string senderRuntimeId,
        long receivedDay, out DailyV1CommercialMarketObservation market,
        out DailyV1CommercialLiquidityObservation liquidity)
    {
        market = null;
        liquidity = null;
        try
        {
            if (IsMarketObservation)
            {
                if (ItemDefinition == null || definitions == null
                    || !definitions.Items.TryGetValue(ItemDefinitionId, out DailyV1ItemDefinition admitted)
                    || !ReferenceEquals(admitted, ItemDefinition)) return false;
                market = new DailyV1CommercialMarketObservation(LocationRuntimeId, ItemDefinition,
                    ObservedPrice, ObservedStock, ObservedDay, receivedDay,
                    DailyV1CommercialKnowledgeSource.SharedByNpc, senderRuntimeId);
            }
            else
            {
                liquidity = new DailyV1CommercialLiquidityObservation(LocationRuntimeId,
                    LiquidityMode, ObservedPurchasingPower, ObservedDay, receivedDay,
                    DailyV1CommercialKnowledgeSource.SharedByNpc, senderRuntimeId);
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

    internal string StableFingerprint => StableKeyEncoding.Encode(
        IsMarketObservation ? "market" : "liquidity",
        LocationRuntimeId, ItemDefinitionId,
        ObservedPrice.ToString("R", CultureInfo.InvariantCulture),
        ObservedStock.ToString(CultureInfo.InvariantCulture),
        ((int)LiquidityMode).ToString(CultureInfo.InvariantCulture),
        ObservedPurchasingPower.ToString("R", CultureInfo.InvariantCulture),
        ObservedDay.ToString(CultureInfo.InvariantCulture),
        SourceReceivedDay.ToString(CultureInfo.InvariantCulture),
        ((int)Source).ToString(CultureInfo.InvariantCulture), SourceRuntimeId);
}

internal sealed class DailyV1CommercialKnowledgeShareReceipt
{
    internal string OperationIdentity { get; }
    internal string Fingerprint { get; }
    internal string BoundaryOccurrenceId { get; }
    internal string SenderRuntimeId { get; }
    internal string SenderPersonId { get; }
    internal string ReceiverRuntimeId { get; }
    internal string ReceiverPersonId { get; }
    internal string SourceSnapshotFingerprint { get; }
    internal int AppliedObservationCount { get; }

    internal DailyV1CommercialKnowledgeShareReceipt(string operationIdentity, string fingerprint,
        string boundaryOccurrenceId, string senderRuntimeId, string senderPersonId,
        string receiverRuntimeId, string receiverPersonId, string sourceSnapshotFingerprint,
        int appliedObservationCount)
    {
        OperationIdentity = operationIdentity;
        Fingerprint = fingerprint;
        BoundaryOccurrenceId = boundaryOccurrenceId;
        SenderRuntimeId = senderRuntimeId;
        SenderPersonId = senderPersonId;
        ReceiverRuntimeId = receiverRuntimeId;
        ReceiverPersonId = receiverPersonId;
        SourceSnapshotFingerprint = sourceSnapshotFingerprint;
        AppliedObservationCount = appliedObservationCount;
    }

    internal bool IsValid => !string.IsNullOrWhiteSpace(BoundaryOccurrenceId)
        && !string.IsNullOrWhiteSpace(SenderRuntimeId) && !string.IsNullOrWhiteSpace(ReceiverRuntimeId)
        && !string.Equals(SenderRuntimeId, ReceiverRuntimeId, StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(SourceSnapshotFingerprint)
        && !string.IsNullOrWhiteSpace(Fingerprint) && AppliedObservationCount >= 0
        && SenderPersonId != null && ReceiverPersonId != null
        && string.Equals(OperationIdentity, StableKeyEncoding.Encode(
            "commercial-sharing-edge/v1", BoundaryOccurrenceId, SenderRuntimeId, ReceiverRuntimeId),
            StringComparison.Ordinal);
}

internal sealed class DailyV1CommercialKnowledgeShareBatch
{
    private readonly IReadOnlyList<DailyV1CommercialKnowledgeShareValue> values;
    internal string BoundaryOccurrenceId { get; }
    internal string SenderRuntimeId { get; }
    internal string SenderPersonId { get; }
    internal string ReceiverRuntimeId { get; }
    internal string ReceiverPersonId { get; }
    internal string SourceSnapshotFingerprint { get; }
    internal long ReceivedDay { get; }
    internal int MaxSuccessfulUpdates { get; }
    internal IReadOnlyList<DailyV1CommercialKnowledgeShareValue> Values => values;
    internal string OperationIdentity => StableKeyEncoding.Encode(
        "commercial-sharing-edge/v1", BoundaryOccurrenceId, SenderRuntimeId, ReceiverRuntimeId);

    internal DailyV1CommercialKnowledgeShareBatch(string boundaryOccurrenceId,
        string senderRuntimeId, string senderPersonId, string receiverRuntimeId,
        string receiverPersonId, string sourceSnapshotFingerprint, long receivedDay,
        int maxSuccessfulUpdates, IReadOnlyList<DailyV1CommercialKnowledgeShareValue> values)
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
        List<DailyV1CommercialKnowledgeShareValue> copy = new List<DailyV1CommercialKnowledgeShareValue>(values.Count);
        foreach (DailyV1CommercialKnowledgeShareValue value in values)
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
                ReceivedDay.ToString(CultureInfo.InvariantCulture),
                MaxSuccessfulUpdates.ToString(CultureInfo.InvariantCulture),
                values.Count.ToString(CultureInfo.InvariantCulture)
            };
            foreach (DailyV1CommercialKnowledgeShareValue value in values) fields.Add(value.StableFingerprint);
            return StableKeyEncoding.Encode(fields.ToArray());
        }
    }
}

internal sealed class DailyV1CommercialKnowledgeObservationInstall
{
    internal DailyV1CommercialKnowledgeOwner Owner { get; }
    internal long ExpectedRevision { get; }
    internal long NextRevision { get; }
    internal List<DailyV1CommercialMarketObservation> NextMarketObservations { get; }
    internal List<DailyV1CommercialLiquidityObservation> NextLiquidityObservations { get; }

    internal DailyV1CommercialKnowledgeObservationInstall(DailyV1CommercialKnowledgeOwner owner,
        long expectedRevision, long nextRevision, List<DailyV1CommercialMarketObservation> markets,
        List<DailyV1CommercialLiquidityObservation> liquidity)
    {
        Owner = owner;
        ExpectedRevision = expectedRevision;
        NextRevision = nextRevision;
        NextMarketObservations = markets;
        NextLiquidityObservations = liquidity;
    }
}

internal sealed class DailyV1CommercialKnowledgeShareBatchCommit
{
    internal DailyV1CommercialKnowledgeOwner Owner { get; }
    internal long ExpectedRevision { get; }
    internal bool IsReplay { get; }
    internal List<DailyV1CommercialMarketObservation> NextMarketObservations { get; }
    internal List<DailyV1CommercialLiquidityObservation> NextLiquidityObservations { get; }
    internal List<DailyV1CommercialKnowledgeShareReceipt> NextReceipts { get; }
    internal DailyV1CommercialKnowledgeShareReceipt Receipt { get; }

    internal DailyV1CommercialKnowledgeShareBatchCommit(DailyV1CommercialKnowledgeOwner owner,
        long expectedRevision, bool replay, List<DailyV1CommercialMarketObservation> markets,
        List<DailyV1CommercialLiquidityObservation> liquidity,
        List<DailyV1CommercialKnowledgeShareReceipt> receipts,
        DailyV1CommercialKnowledgeShareReceipt receipt)
    {
        Owner = owner;
        ExpectedRevision = expectedRevision;
        IsReplay = replay;
        NextMarketObservations = markets;
        NextLiquidityObservations = liquidity;
        NextReceipts = receipts;
        Receipt = receipt;
    }
}

/// <summary>Single selected NPC authority for typed commercial knowledge, revisions, and share receipts.</summary>
internal sealed class DailyV1CommercialKnowledgeOwner
{
    private readonly List<DailyV1CommercialMarketObservation> observations = new List<DailyV1CommercialMarketObservation>();
    private readonly List<DailyV1CommercialLiquidityObservation> liquidityObservations = new List<DailyV1CommercialLiquidityObservation>();
    private readonly List<DailyV1CommercialKnowledgeShareReceipt> shareReceipts = new List<DailyV1CommercialKnowledgeShareReceipt>();
    private Func<bool> mutationAdmission;
    private Action mutationCommitted;
    internal DailyV1EffectiveDefinitions Definitions { get; }
    internal IReadOnlyList<DailyV1CommercialMarketObservation> Observations => observations.AsReadOnly();
    internal IReadOnlyList<DailyV1CommercialLiquidityObservation> LiquidityObservations => liquidityObservations.AsReadOnly();
    internal long Revision { get; private set; }

    internal DailyV1CommercialKnowledgeOwner(DailyV1EffectiveDefinitions definitions)
    {
        Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
    }

    internal bool TryReadCensus(out int marketCount, out int liquidityCount,
        out int shareReceiptCount, out long currentRevision)
    {
        marketCount = observations.Count;
        liquidityCount = liquidityObservations.Count;
        shareReceiptCount = shareReceipts.Count;
        currentRevision = Revision;
        return true;
    }

    internal bool TryCopyOwnerSnapshot(out IReadOnlyList<DailyV1CommercialMarketObservation> markets,
        out IReadOnlyList<DailyV1CommercialLiquidityObservation> liquidity,
        out IReadOnlyList<DailyV1CommercialKnowledgeShareReceipt> receipts)
    {
        markets = new List<DailyV1CommercialMarketObservation>(observations).AsReadOnly();
        liquidity = new List<DailyV1CommercialLiquidityObservation>(liquidityObservations).AsReadOnly();
        receipts = new List<DailyV1CommercialKnowledgeShareReceipt>(shareReceipts).AsReadOnly();
        return true;
    }

    internal static bool TryCreateFromOwnerSnapshot(DailyV1EffectiveDefinitions definitions,
        IReadOnlyList<DailyV1CommercialMarketObservation> markets,
        IReadOnlyList<DailyV1CommercialLiquidityObservation> liquidity,
        IReadOnlyList<DailyV1CommercialKnowledgeShareReceipt> receipts,
        long exactRevision, out DailyV1CommercialKnowledgeOwner staged)
    {
        staged = null;
        if (definitions == null || markets == null || liquidity == null || receipts == null || exactRevision < 0L) return false;
        DailyV1CommercialKnowledgeOwner candidate = new DailyV1CommercialKnowledgeOwner(definitions);
        HashSet<string> marketKeys = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> liquidityKeys = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> receiptIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1CommercialMarketObservation row in markets)
        {
            if (row == null || row.ItemDefinition == null
                || !definitions.Items.TryGetValue(row.ItemDefinitionId, out DailyV1ItemDefinition admitted)
                || !ReferenceEquals(admitted, row.ItemDefinition)
                || !marketKeys.Add(StableKeyEncoding.Encode(row.LocationRuntimeId, row.ItemDefinitionId))) return false;
            candidate.observations.Add(row);
        }
        foreach (DailyV1CommercialLiquidityObservation row in liquidity)
        {
            if (row == null || !liquidityKeys.Add(row.LocationRuntimeId)) return false;
            candidate.liquidityObservations.Add(row);
        }
        foreach (DailyV1CommercialKnowledgeShareReceipt row in receipts)
        {
            if (row == null || !row.IsValid || !receiptIds.Add(row.OperationIdentity)) return false;
            candidate.shareReceipts.Add(row);
        }
        candidate.Revision = exactRevision;
        staged = candidate;
        return true;
    }

    internal bool RecordObservation(DailyV1CommercialMarketObservation observation)
    {
        if (!IsAdmitted(observation)) return false;
        int index = FindObservationIndex(observations, observation.LocationRuntimeId, observation.ItemDefinitionId);
        if (index >= 0 && !ShouldReplace(observations[index], observation)) return false;
        if (Revision == long.MaxValue || !CanCommit()) return false;
        if (index < 0) observations.Add(observation); else observations[index] = observation;
        Revision++;
        NotifyCommitted();
        return true;
    }

    internal bool CanImproveWith(DailyV1CommercialMarketObservation observation)
    {
        if (!IsAdmitted(observation)) return false;
        int index = FindObservationIndex(observations, observation.LocationRuntimeId, observation.ItemDefinitionId);
        return index < 0 || ShouldReplace(observations[index], observation);
    }

    internal bool TryGetObservation(string locationRuntimeId, string itemDefinitionId,
        out DailyV1CommercialMarketObservation observation)
    {
        int index = FindObservationIndex(observations, locationRuntimeId, itemDefinitionId);
        observation = index < 0 ? null : observations[index];
        return index >= 0;
    }

    internal bool RecordLiquidityObservation(DailyV1CommercialLiquidityObservation observation)
    {
        if (observation == null) return false;
        int index = FindLiquidityObservationIndex(liquidityObservations, observation.LocationRuntimeId);
        if (index >= 0 && !ShouldReplace(liquidityObservations[index], observation)) return false;
        if (Revision == long.MaxValue || !CanCommit()) return false;
        if (index < 0) liquidityObservations.Add(observation); else liquidityObservations[index] = observation;
        Revision++;
        NotifyCommitted();
        return true;
    }

    internal bool CanImproveWith(DailyV1CommercialLiquidityObservation observation)
    {
        if (observation == null) return false;
        int index = FindLiquidityObservationIndex(liquidityObservations, observation.LocationRuntimeId);
        return index < 0 || ShouldReplace(liquidityObservations[index], observation);
    }

    internal bool TryGetLiquidityObservation(string locationRuntimeId,
        out DailyV1CommercialLiquidityObservation observation)
    {
        int index = FindLiquidityObservationIndex(liquidityObservations, locationRuntimeId);
        observation = index < 0 ? null : liquidityObservations[index];
        return index >= 0;
    }

    internal bool TryPrepareDirectObservationBatch(
        IReadOnlyList<DailyV1CommercialMarketObservation> marketRows,
        DailyV1CommercialLiquidityObservation liquidityRow,
        out DailyV1CommercialKnowledgeObservationInstall prepared)
    {
        prepared = null;
        if (marketRows == null) return false;
        List<DailyV1CommercialMarketObservation> nextMarkets = new List<DailyV1CommercialMarketObservation>(observations);
        List<DailyV1CommercialLiquidityObservation> nextLiquidity = new List<DailyV1CommercialLiquidityObservation>(liquidityObservations);
        long changes = 0L;
        foreach (DailyV1CommercialMarketObservation row in marketRows)
        {
            if (!IsAdmitted(row)) return false;
            int index = FindObservationIndex(nextMarkets, row.LocationRuntimeId, row.ItemDefinitionId);
            if (index < 0) { nextMarkets.Add(row); changes++; }
            else if (ShouldReplace(nextMarkets[index], row)) { nextMarkets[index] = row; changes++; }
        }
        if (liquidityRow != null)
        {
            int index = FindLiquidityObservationIndex(nextLiquidity, liquidityRow.LocationRuntimeId);
            if (index < 0) { nextLiquidity.Add(liquidityRow); changes++; }
            else if (ShouldReplace(nextLiquidity[index], liquidityRow)) { nextLiquidity[index] = liquidityRow; changes++; }
        }
        if (changes > long.MaxValue - Revision) return false;
        prepared = new DailyV1CommercialKnowledgeObservationInstall(this, Revision,
            Revision + changes, nextMarkets, nextLiquidity);
        return true;
    }

    internal bool CanInstall(DailyV1CommercialKnowledgeObservationInstall prepared) =>
        prepared != null && ReferenceEquals(prepared.Owner, this) && Revision == prepared.ExpectedRevision
        && (prepared.NextRevision == Revision || CanCommit());

    internal void InstallPrepared(DailyV1CommercialKnowledgeObservationInstall prepared)
    {
        if (prepared == null || !ReferenceEquals(prepared.Owner, this) || Revision != prepared.ExpectedRevision
            || prepared.NextRevision == Revision || !CanCommit()) return;
        observations.Clear(); observations.AddRange(prepared.NextMarketObservations);
        liquidityObservations.Clear(); liquidityObservations.AddRange(prepared.NextLiquidityObservations);
        Revision = prepared.NextRevision;
        NotifyCommitted();
    }

    internal bool TryGetShareReceipt(string operationIdentity,
        out DailyV1CommercialKnowledgeShareReceipt receipt)
    {
        receipt = null;
        if (string.IsNullOrWhiteSpace(operationIdentity)) return false;
        foreach (DailyV1CommercialKnowledgeShareReceipt candidate in shareReceipts)
            if (candidate != null && string.Equals(candidate.OperationIdentity, operationIdentity, StringComparison.Ordinal))
            { receipt = candidate; return true; }
        return false;
    }

    internal bool TryPrepareShareBatch(DailyV1CommercialKnowledgeShareBatch batch,
        out DailyV1CommercialKnowledgeShareBatchCommit prepared)
    {
        prepared = null;
        if (batch == null) return false;
        string fingerprint = batch.Fingerprint;
        if (TryGetShareReceipt(batch.OperationIdentity, out DailyV1CommercialKnowledgeShareReceipt existing))
        {
            if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal)) return false;
            prepared = new DailyV1CommercialKnowledgeShareBatchCommit(this, Revision, true,
                null, null, null, existing);
            return true;
        }
        if (Revision == long.MaxValue) return false;
        List<DailyV1CommercialMarketObservation> nextMarkets = new List<DailyV1CommercialMarketObservation>(observations);
        List<DailyV1CommercialLiquidityObservation> nextLiquidity = new List<DailyV1CommercialLiquidityObservation>(liquidityObservations);
        int applied = 0;
        foreach (DailyV1CommercialKnowledgeShareValue value in batch.Values)
        {
            if (!value.TryCreateShared(Definitions, batch.SenderRuntimeId, batch.ReceivedDay,
                    out DailyV1CommercialMarketObservation market, out DailyV1CommercialLiquidityObservation liquidity)) return false;
            if (market != null)
            {
                int index = FindObservationIndex(nextMarkets, market.LocationRuntimeId, market.ItemDefinitionId);
                if (index < 0) { nextMarkets.Add(market); applied++; }
                else if (ShouldReplace(nextMarkets[index], market)) { nextMarkets[index] = market; applied++; }
            }
            else
            {
                int index = FindLiquidityObservationIndex(nextLiquidity, liquidity.LocationRuntimeId);
                if (index < 0) { nextLiquidity.Add(liquidity); applied++; }
                else if (ShouldReplace(nextLiquidity[index], liquidity)) { nextLiquidity[index] = liquidity; applied++; }
            }
            if (applied >= batch.MaxSuccessfulUpdates) break;
        }
        DailyV1CommercialKnowledgeShareReceipt receipt = new DailyV1CommercialKnowledgeShareReceipt(
            batch.OperationIdentity, fingerprint, batch.BoundaryOccurrenceId, batch.SenderRuntimeId,
            batch.SenderPersonId, batch.ReceiverRuntimeId, batch.ReceiverPersonId,
            batch.SourceSnapshotFingerprint, applied);
        List<DailyV1CommercialKnowledgeShareReceipt> nextReceipts = new List<DailyV1CommercialKnowledgeShareReceipt>(shareReceipts) { receipt };
        prepared = new DailyV1CommercialKnowledgeShareBatchCommit(this, Revision, false,
            nextMarkets, nextLiquidity, nextReceipts, receipt);
        return true;
    }

    internal bool TryCommitShareBatch(DailyV1CommercialKnowledgeShareBatchCommit commit,
        out DailyV1CommercialKnowledgeShareReceipt receipt)
    {
        receipt = null;
        if (commit == null || !ReferenceEquals(commit.Owner, this)) return false;
        if (commit.IsReplay)
        {
            if (TryGetShareReceipt(commit.Receipt.OperationIdentity, out DailyV1CommercialKnowledgeShareReceipt existing)
                && string.Equals(existing.Fingerprint, commit.Receipt.Fingerprint, StringComparison.Ordinal))
            { receipt = existing; return true; }
            return false;
        }
        if (Revision != commit.ExpectedRevision || Revision == long.MaxValue
            || commit.NextMarketObservations == null || commit.NextLiquidityObservations == null
            || commit.NextReceipts == null || TryGetShareReceipt(commit.Receipt.OperationIdentity, out _)
            || !CanCommit()) return false;
        observations.Clear(); observations.AddRange(commit.NextMarketObservations);
        liquidityObservations.Clear(); liquidityObservations.AddRange(commit.NextLiquidityObservations);
        shareReceipts.Clear(); shareReceipts.AddRange(commit.NextReceipts);
        Revision++;
        NotifyCommitted();
        receipt = commit.Receipt;
        return true;
    }

    internal void BindMutationBoundary(Func<bool> admission, Action committed)
    {
        if (admission == null) throw new ArgumentNullException(nameof(admission));
        if (committed == null) throw new ArgumentNullException(nameof(committed));
        if (mutationAdmission != null || mutationCommitted != null)
            throw new InvalidOperationException("DailyV1CommercialKnowledgeOwner is already bound to a mutation boundary.");
        mutationAdmission = admission;
        mutationCommitted = committed;
    }

    internal bool UnbindMutationBoundary(Func<bool> admission, Action committed)
    {
        if (!ReferenceEquals(mutationAdmission, admission) || !ReferenceEquals(mutationCommitted, committed)) return false;
        mutationAdmission = null;
        mutationCommitted = null;
        return true;
    }

    private bool IsAdmitted(DailyV1CommercialMarketObservation row) => row != null
        && row.ItemDefinition != null
        && Definitions.Items.TryGetValue(row.ItemDefinitionId, out DailyV1ItemDefinition admitted)
        && ReferenceEquals(admitted, row.ItemDefinition);

    private bool CanCommit()
    {
        if (mutationAdmission == null) return true;
        try { return mutationAdmission(); } catch { return false; }
    }

    private void NotifyCommitted()
    {
        try { mutationCommitted?.Invoke(); } catch { }
    }

    private static int FindObservationIndex(IReadOnlyList<DailyV1CommercialMarketObservation> source,
        string locationRuntimeId, string itemDefinitionId)
    {
        if (string.IsNullOrWhiteSpace(locationRuntimeId) || string.IsNullOrWhiteSpace(itemDefinitionId)) return -1;
        for (int i = 0; i < source.Count; i++)
        {
            DailyV1CommercialMarketObservation row = source[i];
            if (row != null && string.Equals(row.LocationRuntimeId, locationRuntimeId, StringComparison.Ordinal)
                && string.Equals(row.ItemDefinitionId, itemDefinitionId, StringComparison.Ordinal)) return i;
        }
        return -1;
    }

    private static int FindLiquidityObservationIndex(IReadOnlyList<DailyV1CommercialLiquidityObservation> source,
        string locationRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(locationRuntimeId)) return -1;
        for (int i = 0; i < source.Count; i++)
        {
            DailyV1CommercialLiquidityObservation row = source[i];
            if (row != null && string.Equals(row.LocationRuntimeId, locationRuntimeId, StringComparison.Ordinal)) return i;
        }
        return -1;
    }

    private static bool ShouldReplace(DailyV1CommercialMarketObservation existing,
        DailyV1CommercialMarketObservation incoming) => existing == null
        || incoming.ObservedDay > existing.ObservedDay
        || (incoming.ObservedDay == existing.ObservedDay
            && GetSourcePriority(incoming.Source) > GetSourcePriority(existing.Source));

    private static bool ShouldReplace(DailyV1CommercialLiquidityObservation existing,
        DailyV1CommercialLiquidityObservation incoming) => existing == null
        || incoming.ObservedDay > existing.ObservedDay
        || (incoming.ObservedDay == existing.ObservedDay
            && GetSourcePriority(incoming.Source) > GetSourcePriority(existing.Source));

    internal static int GetSourcePriority(DailyV1CommercialKnowledgeSource source)
    {
        switch (source)
        {
            case DailyV1CommercialKnowledgeSource.DirectObservation: return 3;
            case DailyV1CommercialKnowledgeSource.SharedByNpc: return 2;
            case DailyV1CommercialKnowledgeSource.InitialScenarioKnowledge: return 1;
            default: return 0;
        }
    }
}
