using System;

public enum MarketLiquidityMode
{
    Open,
    AccountBacked
}

[Serializable]
public sealed class MarketCounterpartyRuntime
{
    private readonly string counterpartyRuntimeId;
    private readonly MarketLiquidityMode liquidityMode;
    private readonly MoneyAccountRuntime moneyAccount;

    public string CounterpartyRuntimeId => counterpartyRuntimeId;
    public MarketLiquidityMode LiquidityMode => liquidityMode;
    public MarketLiquidityMode Mode => liquidityMode;
    public MoneyAccountRuntime MoneyAccount => moneyAccount;

    private MarketCounterpartyRuntime(
        string counterpartyRuntimeId,
        MarketLiquidityMode liquidityMode,
        MoneyAccountRuntime moneyAccount)
    {
        if (liquidityMode == MarketLiquidityMode.AccountBacked && moneyAccount == null)
        {
            throw new ArgumentNullException(nameof(moneyAccount), "Account-backed market counterparties require a MoneyAccountRuntime.");
        }

        this.counterpartyRuntimeId = string.IsNullOrWhiteSpace(counterpartyRuntimeId) == true
            ? null
            : counterpartyRuntimeId;
        this.liquidityMode = liquidityMode;
        this.moneyAccount = moneyAccount;
    }

    public static MarketCounterpartyRuntime CreateOpen(string counterpartyRuntimeId = null)
    {
        return new MarketCounterpartyRuntime(counterpartyRuntimeId, MarketLiquidityMode.Open, null);
    }

    public static MarketCounterpartyRuntime CreateAccountBacked(
        string counterpartyRuntimeId,
        MoneyAccountRuntime moneyAccount)
    {
        return new MarketCounterpartyRuntime(counterpartyRuntimeId, MarketLiquidityMode.AccountBacked, moneyAccount);
    }
}
