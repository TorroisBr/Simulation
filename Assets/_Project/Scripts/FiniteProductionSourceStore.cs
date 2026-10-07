using System;
using UnityEngine;

[Serializable]
public sealed class FiniteProductionSourceStore
{
    [SerializeField] private FiniteProductionSourceState source;

    public FiniteProductionSourceState Source => source;

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

    internal bool CanInstall(long expectedRevision, long day) =>
        source != null && source.Revision == expectedRevision
        && source.Revision < long.MaxValue && day > source.LastProcessedDay;

    internal void Install(long expectedRevision, long day, int produced)
    {
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

    internal void Install(long expectedRevision, long day, int produced)
    {
        remainingReserve -= produced;
        revision = expectedRevision + 1;
        lastProcessedDay = day;
    }
}
