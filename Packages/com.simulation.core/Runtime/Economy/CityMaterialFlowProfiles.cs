/// <summary>Payment policy for a City's population consumption owner.</summary>
public enum ConsumptionPaymentMode
{
    Free,
    AccountBacked
}

/// <summary>Source policy for a City's daily production configuration.</summary>
public enum CityProductionSourceKind
{
    Unspecified,
    ExogenousDaily,
    FiniteReserveDaily
}

/// <summary>Selected local production contract for a City runtime.</summary>
public enum LocalMaterialFlowProfile
{
    ExogenousDaily,
    FiniteReserveDaily,
    MixedSourcesDaily
}
