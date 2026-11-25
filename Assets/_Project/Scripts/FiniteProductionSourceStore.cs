using System;
using UnityEngine;

[Serializable]
public sealed class FiniteProductionSourceStore
{
    [SerializeField] private FiniteProductionSourceState source;
    [NonSerialized] private DailyV1FiniteProductionSourceOwner selectedOwner;

    /// <summary>
    /// Compatibility projection for existing Unity callers. Selected finite
    /// source state is owned by the shared Core City owner.
    /// </summary>
    public FiniteProductionSourceState Source
    {
        get
        {
            if (selectedOwner == null) return source;
            FiniteSourceSnapshot snapshot = selectedOwner.CaptureSnapshot();
            if (source == null) source = new FiniteProductionSourceState(snapshot);
            else source.Refresh(snapshot);
            return source;
        }
    }

    internal FiniteProductionSourceStore(DailyV1FiniteProductionSourceOwner selectedOwner)
    {
        this.selectedOwner = selectedOwner ?? throw new ArgumentNullException(nameof(selectedOwner));
    }

    internal FiniteProductionSourceStore(
        LocalMaterialFlowProfile profile,
        CityProductionConfig config,
        string settlementSemanticId,
        string marketStoreSemanticId)
    {
        if (profile != LocalMaterialFlowProfile.FiniteReserveDaily
            && profile != LocalMaterialFlowProfile.MixedSourcesDaily)
            throw new ArgumentException("Finite source state requires a finite-source profile.", nameof(profile));
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (config.item == null || string.IsNullOrWhiteSpace(config.item.DefinitionId))
            throw new ArgumentException("Finite production requires an item definition identity.", nameof(config));
        if (string.IsNullOrWhiteSpace(config.productionSourceId))
            throw new ArgumentException("Finite production requires an authored source identity.", nameof(config));
        if (string.IsNullOrWhiteSpace(config.contentRevision))
            throw new ArgumentException("Finite production requires a compatible content revision.", nameof(config));
        if (string.IsNullOrWhiteSpace(settlementSemanticId))
            throw new ArgumentException("Finite production requires a title-owning settlement identity.", nameof(settlementSemanticId));
        if (string.IsNullOrWhiteSpace(marketStoreSemanticId))
            throw new ArgumentException("Finite production requires a custodial Market store identity.", nameof(marketStoreSemanticId));
        if (config.amountPerDay <= 0)
            throw new ArgumentOutOfRangeException(nameof(config), "Finite production requires a positive daily output limit.");
        if (config.initialReserve < 0)
            throw new ArgumentOutOfRangeException(nameof(config), "Finite production reserve cannot be negative.");

        source = new FiniteProductionSourceState(
            config.productionSourceId,
            settlementSemanticId,
            marketStoreSemanticId,
            config.item.DefinitionId,
            config.contentRevision,
            config.amountPerDay,
            config.initialReserve);
    }

    internal FiniteProductionSourceStore(
        LocalMaterialFlowProfile profile,
        DailyV1CityProductionConfig config,
        DailyV1ItemDefinition item,
        string settlementSemanticId,
        string marketStoreSemanticId)
    {
        if (profile != LocalMaterialFlowProfile.FiniteReserveDaily
            && profile != LocalMaterialFlowProfile.MixedSourcesDaily)
            throw new ArgumentException("Finite source state requires a finite-source profile.", nameof(profile));
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (item == null || string.IsNullOrWhiteSpace(item.DefinitionId)
            || !string.Equals(config.item, item.DefinitionId, StringComparison.Ordinal))
            throw new ArgumentException("Finite production requires its exact admitted item definition identity.", nameof(item));
        if (string.IsNullOrWhiteSpace(config.productionSourceId))
            throw new ArgumentException("Finite production requires an authored source identity.", nameof(config));
        if (string.IsNullOrWhiteSpace(config.contentRevision))
            throw new ArgumentException("Finite production requires a compatible content revision.", nameof(config));
        if (string.IsNullOrWhiteSpace(settlementSemanticId))
            throw new ArgumentException("Finite production requires a title-owning settlement identity.", nameof(settlementSemanticId));
        if (string.IsNullOrWhiteSpace(marketStoreSemanticId))
            throw new ArgumentException("Finite production requires a custodial Market store identity.", nameof(marketStoreSemanticId));
        if (config.amountPerDay <= 0)
            throw new ArgumentOutOfRangeException(nameof(config), "Finite production requires a positive daily output limit.");
        if (config.initialReserve < 0)
            throw new ArgumentOutOfRangeException(nameof(config), "Finite source reserve cannot be negative.");

        source = new FiniteProductionSourceState(
            config.productionSourceId,
            settlementSemanticId,
            marketStoreSemanticId,
            item.DefinitionId,
            config.contentRevision,
            config.amountPerDay,
            config.initialReserve);
    }

    internal bool CanInstall(long expectedRevision, long day) =>
        selectedOwner != null
            ? selectedOwner.CanInstall(expectedRevision, day)
            : source != null && source.Revision == expectedRevision
                && source.Revision < long.MaxValue && day > source.LastProcessedDay;

    internal void Install(long expectedRevision, long day, int produced)
    {
        if (selectedOwner != null)
        {
            if (!selectedOwner.TryInstall(expectedRevision, day, produced))
                throw new InvalidOperationException("The selected Core finite-source owner rejected a prepared install.");
            return;
        }
        source.Install(expectedRevision, day, produced);
    }
}

[Serializable]
public sealed class FiniteProductionSourceState
{
    [SerializeField] private string productionSourceId;
    [SerializeField] private string settlementSemanticId;
    [SerializeField] private string marketStoreSemanticId;
    [SerializeField] private string itemDefinitionId;
    [SerializeField] private string contentRevision;
    [SerializeField] private int dailyOutputLimit;
    [SerializeField] private int initialReserve;
    [SerializeField] private int remainingReserve;
    [SerializeField] private long revision;
    [SerializeField] private long lastProcessedDay = long.MinValue;

    public string ProductionSourceId => productionSourceId;
    public string SettlementSemanticId => settlementSemanticId;
    public string MarketStoreSemanticId => marketStoreSemanticId;
    public string ItemDefinitionId => itemDefinitionId;
    public string ContentRevision => contentRevision;
    public int DailyOutputLimit => dailyOutputLimit;
    public int InitialReserve => initialReserve;
    public int RemainingReserve => remainingReserve;
    public long Revision => revision;
    public long LastProcessedDay => lastProcessedDay;

    internal FiniteProductionSourceState(
        string sourceId,
        string settlementId,
        string storeId,
        string itemId,
        string revisionId,
        int dailyLimit,
        int reserve)
    {
        productionSourceId = sourceId;
        settlementSemanticId = settlementId;
        marketStoreSemanticId = storeId;
        itemDefinitionId = itemId;
        contentRevision = revisionId;
        dailyOutputLimit = dailyLimit;
        initialReserve = reserve;
        remainingReserve = reserve;
    }

    internal FiniteProductionSourceState(FiniteSourceSnapshot snapshot)
    {
        Refresh(snapshot);
    }

    internal void Refresh(FiniteSourceSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        productionSourceId = snapshot.ProductionSourceId;
        settlementSemanticId = snapshot.SettlementSemanticId;
        marketStoreSemanticId = snapshot.MarketStoreSemanticId;
        itemDefinitionId = snapshot.ItemDefinitionId;
        contentRevision = snapshot.ContentRevision;
        dailyOutputLimit = snapshot.DailyOutputLimit;
        initialReserve = snapshot.InitialReserve;
        remainingReserve = snapshot.RemainingReserve;
        revision = snapshot.Revision;
        lastProcessedDay = snapshot.LastProcessedDay;
    }

    internal void Install(long expectedRevision, long day, int produced)
    {
        remainingReserve -= produced;
        revision = expectedRevision + 1;
        lastProcessedDay = day;
    }
}
