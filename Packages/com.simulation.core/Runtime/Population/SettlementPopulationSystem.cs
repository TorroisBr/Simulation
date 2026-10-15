using System;

/// <summary>
/// Pure proposal and atomic application boundary for settlement aggregate population.
/// It intentionally does not advance simulation time, record history, or consult RNG.
/// </summary>
public static class SettlementPopulationSystem
{
    public static bool TryPropose(
        SettlementPopulationRuntime population,
        PopulationChangeSet changes,
        out SettlementPopulationTransition transition,
        out PopulationTransitionFailure failure)
    {
        transition = null;
        failure = PopulationTransitionFailure.None;

        if (population == null || string.IsNullOrWhiteSpace(population.SettlementRuntimeId) == true)
        {
            failure = PopulationTransitionFailure.InvalidSettlement;
            return false;
        }

        if (changes.HasNegativeChange == true)
        {
            failure = PopulationTransitionFailure.NegativeChange;
            return false;
        }

        long populationAfterLong = (long)population.CurrentPopulation + changes.NetChange;
        if (populationAfterLong < 0L)
        {
            failure = PopulationTransitionFailure.WouldUnderflow;
            return false;
        }

        if (populationAfterLong > int.MaxValue)
        {
            failure = PopulationTransitionFailure.WouldOverflow;
            return false;
        }

        transition = new SettlementPopulationTransition(
            population.SettlementRuntimeId,
            population.Revision,
            population.CurrentPopulation,
            changes,
            changes.NetChange,
            (int)populationAfterLong);
        return true;
    }

    public static bool TryApply(
        SettlementPopulationRuntime population,
        SettlementPopulationTransition transition,
        out PopulationTransitionFailure failure)
    {
        failure = PopulationTransitionFailure.None;

        if (population == null || string.IsNullOrWhiteSpace(population.SettlementRuntimeId) == true)
        {
            failure = PopulationTransitionFailure.InvalidSettlement;
            return false;
        }

        return population.TryApplyTransition(transition, out failure);
    }
}
