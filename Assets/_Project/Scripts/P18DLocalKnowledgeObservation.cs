using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Builds one ordered local-observation step per actor in the frozen runtime
/// roster. Each actor owns one atomic receipt covering both spatial discovery
/// and its optional direct merchant-market observation.
/// </summary>
public sealed class NpcLocalKnowledgeDailyBoundaryStepProvider : IP18DDailyBoundaryStepProvider
{
    private const string OperationKind = "npc.local-knowledge-observation";
    private const string OperationVersion = "1";
    private readonly Func<IReadOnlyList<NpcRuntime>> roster;
    private readonly bool merchantKnowledgeEnabled;
    private string activeOccurrenceId;
    private IReadOnlyList<BoundaryContinuationStep> activeSteps;
    private Dictionary<string, NpcRuntime> activeActors =
        new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);

    public NpcLocalKnowledgeDailyBoundaryStepProvider(
        Func<IReadOnlyList<NpcRuntime>> roster,
        bool merchantKnowledgeEnabled)
    {
        this.roster = roster ?? throw new ArgumentNullException(nameof(roster));
        this.merchantKnowledgeEnabled = merchantKnowledgeEnabled;
    }

    public bool TryCreateSteps(DailyBoundaryOperation operation, int firstOrdinal,
        out IReadOnlyList<BoundaryContinuationStep> steps, out TimelineFailure failure)
    {
        steps = null;
        failure = TimelineFailure.ContinuationFailed;
        if (operation == null || firstOrdinal < 0) return false;

        if (activeOccurrenceId == operation.OccurrenceId && activeSteps != null)
        {
            if (activeSteps.Count > int.MaxValue - firstOrdinal) return false;
            steps = RebaseOrdinals(activeSteps, firstOrdinal);
            activeSteps = steps;
            failure = TimelineFailure.None;
            return true;
        }

        IReadOnlyList<NpcRuntime> currentRoster = roster();
        if (currentRoster == null || currentRoster.Count > int.MaxValue - firstOrdinal) return false;

        List<BoundaryContinuationStep> created = new List<BoundaryContinuationStep>(currentRoster.Count);
        Dictionary<string, NpcRuntime> nextActors = new Dictionary<string, NpcRuntime>(StringComparer.Ordinal);
        HashSet<string> runtimeIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < currentRoster.Count; i++)
        {
            NpcRuntime actor = currentRoster[i];
            if (actor == null || string.IsNullOrWhiteSpace(actor.RuntimeId)
                || !runtimeIds.Add(actor.RuntimeId)) return false;

            string stepId = CreateStepId(actor.RuntimeId);
            if (!nextActors.TryAdd(stepId, actor)) return false;
            created.Add(new BoundaryContinuationStep(
                firstOrdinal + i,
                stepId,
                actor.RuntimeId,
                OperationKind,
                OperationVersion,
                NpcLocalKnowledgeObservationRuntime.GetRevisionToken(actor),
                merchantKnowledgeEnabled ? "merchant-observation-enabled" : "merchant-observation-disabled",
                actor.PersonId != null ? actor.PersonId.Value : string.Empty));
        }

        activeOccurrenceId = operation.OccurrenceId;
        activeActors = nextActors;
        activeSteps = created.AsReadOnly();
        steps = activeSteps;
        failure = TimelineFailure.None;
        return true;
    }

    public bool OwnsStep(BoundaryContinuationStep step) => step != null
        && step.OperationKind == OperationKind
        && step.OperationVersion == OperationVersion
        && activeActors.ContainsKey(step.StepId)
        && activeActors.TryGetValue(step.StepId, out NpcRuntime actor)
        && actor.RuntimeId == step.OwnerId;

    public bool TryPrepareStep(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step, out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        if (manifest == null || manifest.BoundaryOccurrenceId != activeOccurrenceId
            || !IsExactManifestStep(manifest, step)
            || !OwnsStep(step) || !activeActors.TryGetValue(step.StepId, out NpcRuntime actor))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        bool includeMerchantKnowledge = string.Equals(step.Payload,
            "merchant-observation-enabled", StringComparison.Ordinal);
        if (!includeMerchantKnowledge && !string.Equals(step.Payload,
                "merchant-observation-disabled", StringComparison.Ordinal))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }

        return actor.LocalKnowledgeObservationRuntime.TryPrepareStep(
            actor, manifest, step, includeMerchantKnowledge,
            out prepared, out failure);
    }

    /// <summary>Prepares the later merchant-owned market observation occurrence for a frozen actor.</summary>
    public bool TryPreparePostShareMarketObservation(NpcRuntime actor,
        BoundaryContinuationManifest manifest, BoundaryContinuationStep merchantStep,
        out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        if (actor == null || activeOccurrenceId != manifest?.BoundaryOccurrenceId
            || activeSteps == null || !activeActors.TryGetValue(
                CreateStepId(actor.RuntimeId), out NpcRuntime frozenActor)
            || !ReferenceEquals(frozenActor, actor))
        {
            failure = TimelineFailure.ContinuationFailed;
            return false;
        }
        return actor.LocalKnowledgeObservationRuntime.TryPreparePostShareMarketObservation(
            actor, manifest, merchantStep, out prepared, out failure);
    }

    private bool IsExactManifestStep(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step)
    {
        if (manifest == null || step == null || step.Ordinal < 0
            || step.Ordinal >= manifest.Steps.Count
            || !ReferenceEquals(manifest.Steps[step.Ordinal], step)
            || activeSteps == null) return false;

        for (int i = 0; i < activeSteps.Count; i++)
        {
            BoundaryContinuationStep frozen = activeSteps[i];
            if (frozen.Ordinal == step.Ordinal) return ReferenceEquals(frozen, step);
        }
        return false;
    }

    private static string CreateStepId(string runtimeId) =>
        "npc-local-knowledge:" + SpatialStableKey.Encode(runtimeId);

    private static IReadOnlyList<BoundaryContinuationStep> RebaseOrdinals(
        IReadOnlyList<BoundaryContinuationStep> previous, int firstOrdinal)
    {
        List<BoundaryContinuationStep> rebased = new List<BoundaryContinuationStep>(previous.Count);
        for (int i = 0; i < previous.Count; i++)
        {
            BoundaryContinuationStep step = previous[i];
            rebased.Add(new BoundaryContinuationStep(firstOrdinal + i, step.StepId,
                step.OwnerId, step.OperationKind, step.OperationVersion,
                step.OwnerRevision, step.Payload, step.PersonId, step.Disposition));
        }
        return rebased.AsReadOnly();
    }
}

/// <summary>
/// Per-NpcRuntime owner for one daily local-observation occurrence. The receipt
/// covers spatial and commercial knowledge together, so retries never replay
/// only one side of the actor's observation.
/// </summary>
[Serializable]
public sealed class NpcLocalKnowledgeObservationRuntime
{
    private const string OperationKind = "npc.local-knowledge-observation";
    private const string OperationVersion = "1";

    [UnityEngine.SerializeField] private long revision;
    [UnityEngine.SerializeField] private List<NpcLocalKnowledgeObservationReceipt> receipts =
        new List<NpcLocalKnowledgeObservationReceipt>();

    public long Revision => revision;
    private List<NpcLocalKnowledgeObservationReceipt> ReceiptList =>
        receipts ?? (receipts = new List<NpcLocalKnowledgeObservationReceipt>());

    internal static string GetRevisionToken(NpcRuntime actor)
    {
        if (actor == null) return string.Empty;
        return SpatialStableKey.Encode(
            actor.LocalKnowledgeObservationRuntime.Revision.ToString(CultureInfo.InvariantCulture),
            actor.SpatialKnowledge.Revision.ToString(CultureInfo.InvariantCulture),
            actor.CommercialKnowledge.Revision.ToString(CultureInfo.InvariantCulture));
    }

    internal bool TryPrepareStep(NpcRuntime actor,
        BoundaryContinuationManifest manifest, BoundaryContinuationStep step,
        bool merchantKnowledgeEnabled,
        out IBoundaryContinuationStepCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        failure = TimelineFailure.ContinuationFailed;
        if (actor == null || manifest == null || step == null
            || !IsExactManifestStep(manifest, step)
            || actor.RuntimeId != step.OwnerId
            || step.OperationKind != OperationKind
            || step.OperationVersion != OperationVersion
            || step.PersonId != (actor.PersonId != null ? actor.PersonId.Value : string.Empty)
            || manifest.AbsoluteDay <= 0L) return false;

        string operationIdentity = CreateOperationIdentity(manifest, step);
        string descriptorFingerprint = CreateDescriptorFingerprint(manifest, step);
        if (TryGetReceipt(operationIdentity, out NpcLocalKnowledgeObservationReceipt existing))
        {
            if (existing.DescriptorFingerprint != descriptorFingerprint) return false;
        prepared = new PreparedNpcLocalKnowledgeObservationCommit(this,
                actor, existing, null, null, null, null, revision, true,
                merchantKnowledgeEnabled, true, manifest.AbsoluteDay);
            failure = TimelineFailure.None;
            return true;
        }

        if (revision == long.MaxValue || step.OwnerRevision != GetRevisionToken(actor)) return false;
        if (!TryCapture(actor, manifest.AbsoluteDay, merchantKnowledgeEnabled,
                true,
                out NpcLocalKnowledgeObservationSnapshot snapshot)) return false;

        if (!actor.SpatialKnowledge.TryPrepareDiscoverLocations(snapshot.LocationIds,
                out SpatialKnowledgeDiscoveryInstall spatialInstall)
            || !actor.CommercialKnowledge.TryPrepareDirectObservationBatch(
                snapshot.MarketObservations, snapshot.LiquidityObservation,
                out CommercialKnowledgeObservationInstall commercialInstall)) return false;

        NpcLocalKnowledgeObservationReceipt receipt = new NpcLocalKnowledgeObservationReceipt(
            operationIdentity, descriptorFingerprint, snapshot.Fingerprint,
            actor.RuntimeId, step.PersonId, manifest.AbsoluteDay,
            snapshot.LocationIds.Count, snapshot.MarketObservations.Count,
            snapshot.LiquidityObservation != null, snapshot,
            spatialInstall.ExpectedRevision, spatialInstall.NextRevision,
            commercialInstall.ExpectedRevision, commercialInstall.NextRevision);
        List<NpcLocalKnowledgeObservationReceipt> nextReceipts =
            new List<NpcLocalKnowledgeObservationReceipt>(ReceiptList) { receipt };
        prepared = new PreparedNpcLocalKnowledgeObservationCommit(this, actor,
            receipt, snapshot, spatialInstall, commercialInstall, nextReceipts,
            revision, false, merchantKnowledgeEnabled, true,
            manifest.AbsoluteDay);
        failure = TimelineFailure.None;
        return true;
    }

    /// <summary>
    /// Prepares the distinct market observation performed inside merchant trade-state
    /// advancement, after the earlier shared observation and commercial-sharing steps.
    /// This operation intentionally does not rediscover CurrentLocation: legacy
    /// ObserveCurrentMarket discovers CurrentCity.Location only.
    /// </summary>
    internal bool TryPreparePostShareMarketObservation(
        NpcRuntime actor,
        BoundaryContinuationManifest manifest,
        BoundaryContinuationStep merchantStep,
        out IBoundaryContinuationStepCommit prepared,
        out TimelineFailure failure)
    {
        prepared = null;
        failure = TimelineFailure.ContinuationFailed;
        if (actor == null || manifest == null || merchantStep == null
            || !IsExactManifestStep(manifest, merchantStep)
            || actor.RuntimeId != merchantStep.OwnerId
            || merchantStep.PersonId != (actor.PersonId != null ? actor.PersonId.Value : string.Empty)
            || string.IsNullOrWhiteSpace(actor.RuntimeId)
            || manifest.AbsoluteDay <= 0L)
        {
            return false;
        }

        string childStepId = "merchant-trade-observation:" + actor.RuntimeId;
        string operationIdentity = SpatialStableKey.Encode(
            manifest.ContinuationId, manifest.BoundaryOccurrenceId,
            merchantStep.StepId, childStepId, OperationKind, OperationVersion);
        string descriptorFingerprint = SpatialStableKey.Encode(
            manifest.WorldId, manifest.ProfileId, manifest.BoundaryOccurrenceId,
            manifest.ContinuationId, manifest.AbsoluteDay.ToString(CultureInfo.InvariantCulture),
            manifest.ConfigurationIdentity, manifest.ContentIdentity,
            merchantStep.StepId, merchantStep.OwnerId, merchantStep.PersonId,
            childStepId, OperationKind, OperationVersion, merchantStep.OwnerRevision,
            merchantStep.Payload, merchantStep.Disposition);

        if (TryGetReceipt(operationIdentity, out NpcLocalKnowledgeObservationReceipt existing))
        {
            if (existing.DescriptorFingerprint != descriptorFingerprint) return false;
            prepared = new PreparedNpcLocalKnowledgeObservationCommit(this, actor,
                existing, null, null, null, null, revision, true,
                true, false,
                manifest.AbsoluteDay);
            failure = TimelineFailure.None;
            return true;
        }

        if (revision == long.MaxValue
            || !TryCapture(actor, manifest.AbsoluteDay, true,
                false,
                out NpcLocalKnowledgeObservationSnapshot snapshot)
            || !actor.SpatialKnowledge.TryPrepareDiscoverLocations(snapshot.LocationIds,
                out SpatialKnowledgeDiscoveryInstall spatialInstall)
            || !actor.CommercialKnowledge.TryPrepareDirectObservationBatch(
                snapshot.MarketObservations, snapshot.LiquidityObservation,
                out CommercialKnowledgeObservationInstall commercialInstall))
        {
            return false;
        }

        NpcLocalKnowledgeObservationReceipt receipt = new NpcLocalKnowledgeObservationReceipt(
            operationIdentity, descriptorFingerprint, snapshot.Fingerprint,
            actor.RuntimeId, merchantStep.PersonId, manifest.AbsoluteDay,
            snapshot.LocationIds.Count, snapshot.MarketObservations.Count,
            snapshot.LiquidityObservation != null, snapshot,
            spatialInstall.ExpectedRevision, spatialInstall.NextRevision,
            commercialInstall.ExpectedRevision, commercialInstall.NextRevision);
        List<NpcLocalKnowledgeObservationReceipt> nextReceipts =
            new List<NpcLocalKnowledgeObservationReceipt>(ReceiptList) { receipt };
        prepared = new PreparedNpcLocalKnowledgeObservationCommit(this, actor,
            receipt, snapshot, spatialInstall, commercialInstall, nextReceipts,
            revision, false, true, false,
            manifest.AbsoluteDay);
        failure = TimelineFailure.None;
        return true;
    }

    private static bool IsExactManifestStep(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step) => manifest != null && step != null
        && step.Ordinal >= 0 && step.Ordinal < manifest.Steps.Count
        && ReferenceEquals(manifest.Steps[step.Ordinal], step);

    internal bool TryGetReceipt(string operationIdentity,
        out NpcLocalKnowledgeObservationReceipt receipt)
    {
        receipt = null;
        if (string.IsNullOrWhiteSpace(operationIdentity)) return false;
        foreach (NpcLocalKnowledgeObservationReceipt candidate in ReceiptList)
        {
            if (candidate == null || candidate.OperationIdentity != operationIdentity) continue;
            receipt = candidate;
            return true;
        }
        return false;
    }

    internal bool TryCommit(PreparedNpcLocalKnowledgeObservationCommit commit,
        out TimelineFailure failure)
    {
        failure = TimelineFailure.ContinuationFailed;
        if (commit == null || commit.Owner != this) return false;
        if (TryGetReceipt(commit.Receipt.OperationIdentity,
                out NpcLocalKnowledgeObservationReceipt retained))
        {
            if (retained.DescriptorFingerprint == commit.Receipt.DescriptorFingerprint
                && retained.SnapshotFingerprint == commit.Receipt.SnapshotFingerprint)
            {
                failure = TimelineFailure.None;
                return true;
            }
            return false;
        }
        if (commit.IsReplay) return false;

        NpcRuntime actor = commit.Actor;
        if (actor == null || revision != commit.ExpectedOwnerRevision
            || revision == long.MaxValue
            || TryGetReceipt(commit.Receipt.OperationIdentity, out _)
            || actor.RuntimeId != commit.Receipt.ActorRuntimeId
            || (actor.PersonId != null ? actor.PersonId.Value : string.Empty) != commit.Receipt.PersonId
            || commit.SourceSnapshot == null
            || !TryCapture(actor, commit.AbsoluteDay, commit.MerchantKnowledgeEnabled,
                commit.IncludeCurrentLocation,
                out NpcLocalKnowledgeObservationSnapshot current)
            || !current.Matches(commit.SourceSnapshot)
            || !commit.SpatialInstall.CanInstall(actor.SpatialKnowledge)
            || !commit.CommercialInstall.CanInstall(actor.CommercialKnowledge)) return false;

        // Both child installs were prepared and preflighted above. This actor
        // owner installs them together with the receipt under the serialized
        // SimulationRuntime advance window.
        actor.SpatialKnowledge.InstallPrepared(commit.SpatialInstall);
        actor.CommercialKnowledge.InstallPrepared(commit.CommercialInstall);
        receipts = commit.NextReceipts;
        revision++;
        failure = TimelineFailure.None;
        return true;
    }

    private static string CreateOperationIdentity(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step) => SpatialStableKey.Encode(
            manifest.ContinuationId, manifest.BoundaryOccurrenceId,
            step.StepId, OperationKind, OperationVersion);

    private static string CreateDescriptorFingerprint(BoundaryContinuationManifest manifest,
        BoundaryContinuationStep step) => SpatialStableKey.Encode(
            manifest.WorldId, manifest.ProfileId,
            manifest.BoundaryOccurrenceId, manifest.ContinuationId,
            manifest.AbsoluteDay.ToString(CultureInfo.InvariantCulture),
            manifest.ConfigurationIdentity, manifest.ContentIdentity,
            step.StepId, step.OwnerId, step.PersonId,
            step.OperationKind, step.OperationVersion, step.OwnerRevision,
            step.Payload, step.Disposition);

    private static bool TryCapture(NpcRuntime actor, long absoluteDay,
        bool merchantKnowledgeEnabled,
        bool includeCurrentLocation,
        out NpcLocalKnowledgeObservationSnapshot snapshot)
    {
        snapshot = null;
        if (actor == null || absoluteDay < 0L) return false;

        SpatialLocationRuntime currentLocation = actor.CurrentLocation;
        CityRuntime currentCity = actor.CurrentCity;
        SpatialLocationRuntime cityLocation = currentCity != null ? currentCity.Location : null;
        MarketRuntime currentMarket = currentCity != null ? currentCity.Market : null;
        MarketCounterpartyRuntime currentCounterparty = currentCity != null
            ? currentCity.MarketCounterparty : null;
        bool eligible = actor.IsAlive && currentLocation != null && !actor.IsTraveling;
        bool marketEligible = actor.IsAlive && !actor.IsTraveling
            && (!includeCurrentLocation || currentLocation != null);
        bool merchant = marketEligible && merchantKnowledgeEnabled && currentCity != null
            && actor.NpcData != null && actor.NpcData.job != null
            && actor.NpcData.job.jobType == NpcJobType.Merchant;

        List<string> locationIds = new List<string>();
        List<CommercialMarketObservation> marketObservations =
            new List<CommercialMarketObservation>();
        CommercialLiquidityObservation liquidityObservation = null;
        List<string> fingerprintParts = new List<string>
        {
            actor.RuntimeId ?? string.Empty,
            actor.PersonId != null ? actor.PersonId.Value : string.Empty,
            actor.IsAlive ? "alive" : "not-alive",
            actor.IsTraveling ? "traveling" : "not-traveling",
            merchantKnowledgeEnabled ? "merchant-enabled" : "merchant-disabled",
            includeCurrentLocation ? "include-current-location" : "market-location-only",
            currentLocation != null ? currentLocation.RuntimeId ?? string.Empty : string.Empty,
            currentCity != null ? currentCity.RuntimeId ?? string.Empty : string.Empty,
            cityLocation != null ? cityLocation.RuntimeId ?? string.Empty : string.Empty,
            merchant ? "merchant-eligible" : "merchant-ineligible",
            absoluteDay.ToString(CultureInfo.InvariantCulture)
        };

        if (eligible && includeCurrentLocation)
        {
            string locationId = currentLocation.RuntimeId;
            locationIds.Add(locationId);
            fingerprintParts.Add("local-location");
            fingerprintParts.Add(locationId ?? string.Empty);
        }

        List<ItemData> itemReferences = new List<ItemData>();
        List<MarketItemRuntime> marketItemReferences = new List<MarketItemRuntime>();
        if (merchant)
        {
            locationIds.Add(cityLocation != null ? cityLocation.RuntimeId : null);
            fingerprintParts.Add("merchant-location");
            fingerprintParts.Add(cityLocation != null ? cityLocation.RuntimeId ?? string.Empty : string.Empty);

            if (cityLocation != null)
            {
                foreach (MarketItemRuntime marketItem in currentMarket.Items)
                {
                    if (marketItem == null || marketItem.Item == null
                        || string.IsNullOrWhiteSpace(marketItem.Item.DefinitionId)) continue;

                    ItemData item = marketItem.Item;
                    itemReferences.Add(item);
                    marketItemReferences.Add(marketItem);
                    marketObservations.Add(new CommercialMarketObservation(
                        cityLocation.RuntimeId, item, marketItem.CurrentPrice,
                        marketItem.Amount, absoluteDay, absoluteDay,
                        CommercialKnowledgeSource.DirectObservation));
                    fingerprintParts.Add("market-item");
                    fingerprintParts.Add(item.DefinitionId);
                    fingerprintParts.Add(marketItem.CurrentPrice.ToString("R", CultureInfo.InvariantCulture));
                    fingerprintParts.Add(marketItem.Amount.ToString(CultureInfo.InvariantCulture));
                }

                MarketCounterpartyRuntime counterparty = currentCounterparty;
                if (counterparty == null) return false;
                float observedPurchasingPower = counterparty.LiquidityMode == MarketLiquidityMode.AccountBacked
                    ? counterparty.MoneyAccount.Balance
                    : 0f;
                liquidityObservation = new CommercialLiquidityObservation(
                    cityLocation.RuntimeId, counterparty.LiquidityMode,
                    observedPurchasingPower, absoluteDay, absoluteDay,
                    CommercialKnowledgeSource.DirectObservation);
                fingerprintParts.Add("market-liquidity");
                fingerprintParts.Add(counterparty.LiquidityMode.ToString());
                fingerprintParts.Add(observedPurchasingPower.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        snapshot = new NpcLocalKnowledgeObservationSnapshot(
            currentLocation, currentCity, cityLocation, currentMarket, currentCounterparty,
            locationIds.AsReadOnly(), marketObservations.AsReadOnly(),
            liquidityObservation, itemReferences.AsReadOnly(), marketItemReferences.AsReadOnly(),
            SpatialStableKey.Encode(fingerprintParts.ToArray()));
        return true;
    }
}

[Serializable]
internal sealed class NpcLocalKnowledgeObservationReceipt
{
    [UnityEngine.SerializeField] private string operationIdentity;
    [UnityEngine.SerializeField] private string descriptorFingerprint;
    [UnityEngine.SerializeField] private string snapshotFingerprint;
    [UnityEngine.SerializeField] private string actorRuntimeId;
    [UnityEngine.SerializeField] private string personId;
    [UnityEngine.SerializeField] private long absoluteDay;
    [UnityEngine.SerializeField] private int locationCount;
    [UnityEngine.SerializeField] private int marketObservationCount;
    [UnityEngine.SerializeField] private bool liquidityObserved;
    [UnityEngine.SerializeField] private long spatialRevisionBefore;
    [UnityEngine.SerializeField] private long spatialRevisionAfter;
    [UnityEngine.SerializeField] private long commercialRevisionBefore;
    [UnityEngine.SerializeField] private long commercialRevisionAfter;
    [NonSerialized] private NpcLocalKnowledgeObservationSnapshot sourceSnapshot;

    public string OperationIdentity => operationIdentity;
    public string DescriptorFingerprint => descriptorFingerprint;
    public string SnapshotFingerprint => snapshotFingerprint;
    public string ActorRuntimeId => actorRuntimeId;
    public string PersonId => personId;
    public long SpatialRevisionBefore => spatialRevisionBefore;
    public long SpatialRevisionAfter => spatialRevisionAfter;
    public long CommercialRevisionBefore => commercialRevisionBefore;
    public long CommercialRevisionAfter => commercialRevisionAfter;
    internal NpcLocalKnowledgeObservationSnapshot SourceSnapshot => sourceSnapshot;

    public NpcLocalKnowledgeObservationReceipt(string operationIdentity,
        string descriptorFingerprint, string snapshotFingerprint,
        string actorRuntimeId, string personId, long absoluteDay,
        int locationCount, int marketObservationCount, bool liquidityObserved,
        NpcLocalKnowledgeObservationSnapshot sourceSnapshot,
        long spatialRevisionBefore, long spatialRevisionAfter,
        long commercialRevisionBefore, long commercialRevisionAfter)
    {
        this.operationIdentity = operationIdentity;
        this.descriptorFingerprint = descriptorFingerprint;
        this.snapshotFingerprint = snapshotFingerprint;
        this.actorRuntimeId = actorRuntimeId;
        this.personId = personId;
        this.absoluteDay = absoluteDay;
        this.locationCount = locationCount;
        this.marketObservationCount = marketObservationCount;
        this.liquidityObserved = liquidityObserved;
        this.sourceSnapshot = sourceSnapshot;
        this.spatialRevisionBefore = spatialRevisionBefore;
        this.spatialRevisionAfter = spatialRevisionAfter;
        this.commercialRevisionBefore = commercialRevisionBefore;
        this.commercialRevisionAfter = commercialRevisionAfter;
    }
}

internal sealed class NpcLocalKnowledgeObservationSnapshot
{
    public SpatialLocationRuntime CurrentLocation { get; }
    public CityRuntime CurrentCity { get; }
    public SpatialLocationRuntime CityLocation { get; }
    public MarketRuntime Market { get; }
    public MarketCounterpartyRuntime Counterparty { get; }
    public IReadOnlyList<string> LocationIds { get; }
    public IReadOnlyList<CommercialMarketObservation> MarketObservations { get; }
    public CommercialLiquidityObservation LiquidityObservation { get; }
    public IReadOnlyList<ItemData> ItemReferences { get; }
    public IReadOnlyList<MarketItemRuntime> MarketItemReferences { get; }
    public string Fingerprint { get; }

    public NpcLocalKnowledgeObservationSnapshot(SpatialLocationRuntime currentLocation,
        CityRuntime currentCity, SpatialLocationRuntime cityLocation,
        MarketRuntime market, MarketCounterpartyRuntime counterparty,
        IReadOnlyList<string> locationIds,
        IReadOnlyList<CommercialMarketObservation> marketObservations,
        CommercialLiquidityObservation liquidityObservation,
        IReadOnlyList<ItemData> itemReferences,
        IReadOnlyList<MarketItemRuntime> marketItemReferences, string fingerprint)
    {
        CurrentLocation = currentLocation;
        CurrentCity = currentCity;
        CityLocation = cityLocation;
        Market = market;
        Counterparty = counterparty;
        LocationIds = locationIds;
        MarketObservations = marketObservations;
        LiquidityObservation = liquidityObservation;
        ItemReferences = itemReferences;
        MarketItemReferences = marketItemReferences;
        Fingerprint = fingerprint;
    }

    public bool Matches(NpcLocalKnowledgeObservationSnapshot other)
    {
        if (other == null || Fingerprint != other.Fingerprint
            || !ReferenceEquals(CurrentLocation, other.CurrentLocation)
            || !ReferenceEquals(CurrentCity, other.CurrentCity)
            || !ReferenceEquals(CityLocation, other.CityLocation)
            || !ReferenceEquals(Market, other.Market)
            || !ReferenceEquals(Counterparty, other.Counterparty)
            || ItemReferences.Count != other.ItemReferences.Count
            || MarketItemReferences.Count != other.MarketItemReferences.Count) return false;
        for (int i = 0; i < ItemReferences.Count; i++)
        {
            if (!ReferenceEquals(ItemReferences[i], other.ItemReferences[i])
                || !ReferenceEquals(MarketItemReferences[i], other.MarketItemReferences[i])) return false;
        }
        return true;
    }
}

internal sealed class PreparedNpcLocalKnowledgeObservationCommit : IBoundaryContinuationStepCommit
{
    private static readonly IReadOnlyList<DueWorkReference> NoTimelineFacts =
        Array.Empty<DueWorkReference>();
    private static readonly IReadOnlyList<string> NoSignals = Array.Empty<string>();

    public NpcLocalKnowledgeObservationRuntime Owner { get; }
    public NpcRuntime Actor { get; }
    public NpcLocalKnowledgeObservationReceipt Receipt { get; }
    public NpcLocalKnowledgeObservationSnapshot SourceSnapshot { get; }
    public SpatialKnowledgeDiscoveryInstall SpatialInstall { get; }
    public CommercialKnowledgeObservationInstall CommercialInstall { get; }
    public List<NpcLocalKnowledgeObservationReceipt> NextReceipts { get; }
    public long ExpectedOwnerRevision { get; }
    public bool IsReplay { get; }
    public bool MerchantKnowledgeEnabled { get; }
    public bool IncludeCurrentLocation { get; }
    public long AbsoluteDay { get; }

    public IReadOnlyList<DueWorkReference> RetainedTimelineFacts => NoTimelineFacts;
    public IReadOnlyList<string> RetainedSourceSignals => NoSignals;

    public PreparedNpcLocalKnowledgeObservationCommit(
        NpcLocalKnowledgeObservationRuntime owner, NpcRuntime actor,
        NpcLocalKnowledgeObservationReceipt receipt,
        NpcLocalKnowledgeObservationSnapshot sourceSnapshot,
        SpatialKnowledgeDiscoveryInstall spatialInstall,
        CommercialKnowledgeObservationInstall commercialInstall,
        List<NpcLocalKnowledgeObservationReceipt> nextReceipts,
        long expectedOwnerRevision, bool isReplay,
        bool merchantKnowledgeEnabled, bool includeCurrentLocation, long absoluteDay)
    {
        Owner = owner;
        Actor = actor;
        Receipt = receipt;
        SourceSnapshot = sourceSnapshot;
        SpatialInstall = spatialInstall;
        CommercialInstall = commercialInstall;
        NextReceipts = nextReceipts;
        ExpectedOwnerRevision = expectedOwnerRevision;
        IsReplay = isReplay;
        MerchantKnowledgeEnabled = merchantKnowledgeEnabled;
        IncludeCurrentLocation = includeCurrentLocation;
        AbsoluteDay = absoluteDay;
    }

    public bool TryCommit(out TimelineFailure failure) => Owner.TryCommit(this, out failure);
}
