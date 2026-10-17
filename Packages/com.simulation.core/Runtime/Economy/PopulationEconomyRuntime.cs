using System;

public sealed class PopulationEconomyRuntime
{
    private readonly string cityRuntimeId;
    private readonly string populationEconomicRuntimeId;
    private readonly ConsumptionPaymentMode paymentMode;
    private readonly MoneyAccountRuntime moneyAccount;

    public string CityRuntimeId => cityRuntimeId;
    public string PopulationEconomicRuntimeId => populationEconomicRuntimeId;
    public string RuntimeId => populationEconomicRuntimeId;
    public ConsumptionPaymentMode PaymentMode => paymentMode;
    public MoneyAccountRuntime MoneyAccount => moneyAccount;

    public PopulationEconomyRuntime(
        string cityRuntimeId,
        ConsumptionPaymentMode paymentMode,
        float initialPurchasingPower,
        MarketLiquidityMode marketLiquidityMode)
    {
        if (string.IsNullOrWhiteSpace(cityRuntimeId))
        {
            throw new ArgumentException(
                "PopulationEconomyRuntime requires a non-empty city RuntimeId.",
                nameof(cityRuntimeId));
        }

        if (!Enum.IsDefined(typeof(ConsumptionPaymentMode), paymentMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(paymentMode),
                paymentMode,
                "PopulationConsumptionConfig contains an unsupported payment mode.");
        }

        if (paymentMode == ConsumptionPaymentMode.AccountBacked
            && marketLiquidityMode != MarketLiquidityMode.AccountBacked)
        {
            throw new InvalidOperationException(
                "Account-backed population consumption requires an account-backed city market.");
        }

        if (paymentMode == ConsumptionPaymentMode.AccountBacked
            && !IsValidNonNegativeFiniteAmount(initialPurchasingPower))
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialPurchasingPower),
                initialPurchasingPower,
                "PopulationConsumptionConfig requires finite, non-negative initial purchasing power.");
        }

        this.cityRuntimeId = cityRuntimeId;
        populationEconomicRuntimeId = "population-" + cityRuntimeId;
        this.paymentMode = paymentMode;
        moneyAccount = paymentMode == ConsumptionPaymentMode.AccountBacked
            ? new MoneyAccountRuntime(initialPurchasingPower)
            : null;
    }

    private static bool IsValidNonNegativeFiniteAmount(float amount)
    {
        return amount >= 0f
            && float.IsNaN(amount) == false
            && float.IsInfinity(amount) == false;
    }
}