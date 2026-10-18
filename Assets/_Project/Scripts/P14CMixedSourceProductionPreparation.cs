using System;
using System.Collections.Generic;

/// <summary>Prepared P14-C replacement shared by direct daily and P18D execution.</summary>
internal sealed class P14CMixedSourceProductionPreparation
{
    private readonly MarketRuntime market;
    private readonly FiniteProductionSourceStore finiteSource;
    private readonly long finiteSourceRevision;
    private readonly long absoluteDay;
    private readonly int finiteAppliedQuantity;

    internal readonly PreparedMarketState MarketState;
    internal readonly long MarketRevision;
    internal readonly long MarketRevisionIncrements;
    internal long PostInstallMarketRevision => checked(MarketRevision + MarketRevisionIncrements);
    internal readonly int OpeningStock;
    internal readonly int PostProductionStock;
    internal readonly int AppliedQuantity;
    internal readonly IReadOnlyList<CityDailyMaterialFlowSourceOutcome> Outcomes;

    internal P14CMixedSourceProductionPreparation(
        MarketRuntime market,
        FiniteProductionSourceStore finiteSource,
        long finiteSourceRevision,
        long absoluteDay,
        int finiteAppliedQuantity,
        PreparedMarketState marketState,
        long marketRevision,
        long marketRevisionIncrements,
        int openingStock,
        int postProductionStock,
        int appliedQuantity,
        IReadOnlyList<CityDailyMaterialFlowSourceOutcome> outcomes)
    {
        this.market = market ?? throw new ArgumentNullException(nameof(market));
        this.finiteSource = finiteSource ?? throw new ArgumentNullException(nameof(finiteSource));
        this.finiteSourceRevision = finiteSourceRevision;
        this.absoluteDay = absoluteDay;
        this.finiteAppliedQuantity = finiteAppliedQuantity;
        MarketState = marketState ?? throw new ArgumentNullException(nameof(marketState));
        MarketRevision = marketRevision;
        MarketRevisionIncrements = marketRevisionIncrements;
        OpeningStock = openingStock;
        PostProductionStock = postProductionStock;
        AppliedQuantity = appliedQuantity;
        Outcomes = outcomes ?? throw new ArgumentNullException(nameof(outcomes));
    }

    internal bool CanInstall()
    {
        if (!market.CanInstall(MarketRevision, MarketRevisionIncrements)) return false;
        if (MarketRevisionIncrements > 0
            && !market.CanInstallMaterialFlow(MarketRevision, MarketRevisionIncrements)) return false;
        FiniteProductionSourceState currentSource = finiteSource.Source;
        if (currentSource == null || currentSource.Revision != finiteSourceRevision) return false;
        // A rejected finite contributor has no owner mutation to install. Its
        // revision capacity must not reject other independently accepted
        // contributors in the same deterministic market preparation. The
        // source must still match the revision captured by this preparation.
        return finiteAppliedQuantity == 0
            || finiteSource.CanInstall(finiteSourceRevision, absoluteDay);
    }

    internal bool Install()
    {
        if (!CanInstall()) return false;
        if (MarketRevisionIncrements > 0)
            market.InstallPrepared(MarketRevision, MarketRevisionIncrements, MarketState);
        if (finiteAppliedQuantity > 0)
            finiteSource.Install(finiteSourceRevision, absoluteDay, finiteAppliedQuantity);
        if (MarketRevisionIncrements > 0)
            market.NotifyMaterialFlowInstalled();
        return true;
    }
}
