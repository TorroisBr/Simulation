using System;

/// <summary>
/// Mutable aggregate population state owned by a future settlement runtime.
/// This ledger intentionally has no independent runtime identity and no named-NPC awareness.
/// </summary>
public sealed class SettlementPopulationRuntime
{
    private readonly string settlementRuntimeId;
    private int currentPopulation;

    public string SettlementRuntimeId => settlementRuntimeId;
    public int CurrentPopulation => currentPopulation;

    public SettlementPopulationRuntime(string settlementRuntimeId, int currentPopulation)
    {
        if (string.IsNullOrWhiteSpace(settlementRuntimeId) == true)
        {
            throw new ArgumentException(
                "SettlementPopulationRuntime requires a non-empty settlement RuntimeId.",
                nameof(settlementRuntimeId));
        }

        if (currentPopulation < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentPopulation),
                "Settlement population cannot be negative.");
        }

        this.settlementRuntimeId = settlementRuntimeId;
        this.currentPopulation = currentPopulation;
    }

    internal void SetPopulationAfterValidatedTransition(int populationAfter)
    {
        if (populationAfter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(populationAfter),
                "Settlement population cannot be negative.");
        }

        currentPopulation = populationAfter;
    }
}
