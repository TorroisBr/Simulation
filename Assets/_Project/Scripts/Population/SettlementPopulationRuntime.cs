using System;

/// <summary>
/// Mutable aggregate population state owned by a future settlement runtime.
/// This ledger intentionally has no independent runtime identity and no named-NPC awareness.
/// </summary>
public sealed class SettlementPopulationRuntime
{
    private readonly string settlementRuntimeId;
    private int currentPopulation;
    private long revision;

    public string SettlementRuntimeId => settlementRuntimeId;
    public int CurrentPopulation => currentPopulation;
    public long Revision => revision;

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
        revision = 0L;
    }

    internal bool TryApplyValidatedTransition(
        int populationAfter,
        out PopulationTransitionFailure failure)
    {
        failure = PopulationTransitionFailure.None;

        if (populationAfter < 0)
        {
            failure = PopulationTransitionFailure.InvalidTransition;
            return false;
        }

        if (revision == long.MaxValue)
        {
            failure = PopulationTransitionFailure.RevisionOverflow;
            return false;
        }

        // Both fields are committed together after the system has validated the transition.
        currentPopulation = populationAfter;
        revision++;
        return true;
    }

    internal void CommitValidatedTransition(int populationAfter)
    {
        // The migration boundary validates both aggregates before either commit begins.
        currentPopulation = populationAfter;
        revision++;
    }
}
