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
        PopulationConsumptionConfig config,
        MarketLiquidityMode marketLiquidityMode)
    {
        if (string.IsNullOrWhiteSpace(cityRuntimeId) == true)
        {
            throw new ArgumentException(
                "PopulationEconomyRuntime requires a non-empty city RuntimeId.",
                nameof(cityRuntimeId));
        }

        if (config == null)
        {
            config = new PopulationConsumptionConfig();
        }

        if (Enum.IsDefined(typeof(ConsumptionPaymentMode), config.paymentMode) == false)
        {
            throw new ArgumentOutOfRangeException(
                nameof(config),
                config.paymentMode,
                "PopulationConsumptionConfig contains an unsupported payment mode.");
        }

        if (config.paymentMode == ConsumptionPaymentMode.AccountBacked
            && marketLiquidityMode != MarketLiquidityMode.AccountBacked)
        {
            throw new InvalidOperationException(
                "Account-backed population consumption requires an account-backed city market.");
        }

        if (config.paymentMode == ConsumptionPaymentMode.AccountBacked
            && IsValidNonNegativeFiniteAmount(config.initialPurchasingPower) == false)
        {
            throw new ArgumentOutOfRangeException(
                nameof(config),
                config.initialPurchasingPower,
                "PopulationConsumptionConfig requires finite, non-negative initial purchasing power.");
        }

        this.cityRuntimeId = cityRuntimeId;
        populationEconomicRuntimeId = "population-" + cityRuntimeId;
        paymentMode = config.paymentMode;
        moneyAccount = paymentMode == ConsumptionPaymentMode.AccountBacked
            ? new MoneyAccountRuntime(config.initialPurchasingPower)
            : null;
    }

    private static bool IsValidNonNegativeFiniteAmount(float amount)
    {
        return amount >= 0f
            && float.IsNaN(amount) == false
            && float.IsInfinity(amount) == false;
    }
}
