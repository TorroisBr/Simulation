using System;

/// <summary>
/// Pure proposal and atomic application boundary for aggregate-only births and deaths.
/// It has no world, calendar, configuration, Person, NPC, or implicit randomness dependency.
/// </summary>
public static class AggregateDemographySystem
{
    public static bool TryPropose(
        SettlementPopulationRuntime population,
        int representedResidentFloor,
        IAggregateDemographyProvider provider,
        out AggregateDemographyTransition transition,
        out AggregateDemographyFailure failure)
    {
        transition = null;
        failure = AggregateDemographyFailure.None;

        if (TryValidatePopulationAndFloor(population, representedResidentFloor, out failure) == false)
        {
            return false;
        }

        if (provider == null)
        {
            failure = AggregateDemographyFailure.InvalidProvider;
            return false;
        }

        if (population.Revision == long.MaxValue)
        {
            failure = AggregateDemographyFailure.RevisionOverflow;
            return false;
        }

        AggregateDemographyContext context = new AggregateDemographyContext(
            population.SettlementRuntimeId,
            population.Revision,
            population.CurrentPopulation,
            representedResidentFloor);
        AggregateDemographyChange change = provider.GetChange(context);

        // A provider is policy, not mutation authority. Detect a provider that caused
        // population state to change while deciding instead of accepting a mixed snapshot.
        if (population.Revision != context.PopulationRevision
            || population.CurrentPopulation != context.CurrentPopulation)
        {
            failure = AggregateDemographyFailure.StaleState;
            return false;
        }

        return TryPropose(
            population,
            representedResidentFloor,
            change,
            out transition,
            out failure);
    }

    public static bool TryPropose(
        SettlementPopulationRuntime population,
        int representedResidentFloor,
        AggregateDemographyChange change,
        out AggregateDemographyTransition transition,
        out AggregateDemographyFailure failure)
    {
        transition = null;
        failure = AggregateDemographyFailure.None;

        if (TryValidatePopulationAndFloor(population, representedResidentFloor, out failure) == false)
        {
            return false;
        }

        if (change.HasNegativeChange == true)
        {
            failure = AggregateDemographyFailure.NegativeChange;
            return false;
        }

        if (population.Revision == long.MaxValue)
        {
            failure = AggregateDemographyFailure.RevisionOverflow;
            return false;
        }

        PopulationChangeSet populationChange = new PopulationChangeSet(
            change.Births,
            change.Deaths,
            0,
            0);
        if (SettlementPopulationSystem.TryPropose(
                population,
                populationChange,
                out SettlementPopulationTransition populationTransition,
                out PopulationTransitionFailure populationFailure) == false)
        {
            failure = MapPopulationFailure(populationFailure);
            return false;
        }

        if (populationTransition.PopulationAfter < representedResidentFloor)
        {
            failure = AggregateDemographyFailure.WouldViolateRepresentedResidentFloor;
            return false;
        }

        transition = new AggregateDemographyTransition(
            population.SettlementRuntimeId,
            population.Revision,
            population.CurrentPopulation,
            representedResidentFloor,
            change,
            populationTransition.PopulationAfter);
        return true;
    }

    public static bool TryApply(
        SettlementPopulationRuntime population,
        int representedResidentFloor,
        AggregateDemographyTransition transition,
        out AggregateDemographyFailure failure)
    {
        failure = AggregateDemographyFailure.None;

        if (TryValidatePopulationAndFloor(population, representedResidentFloor, out failure) == false)
        {
            return false;
        }

        if (IsValidTransitionShape(transition) == false)
        {
            failure = AggregateDemographyFailure.InvalidTransition;
            return false;
        }

        if (string.Equals(
                population.SettlementRuntimeId,
                transition.SettlementRuntimeId,
                StringComparison.Ordinal) == false)
        {
            failure = AggregateDemographyFailure.InvalidSettlement;
            return false;
        }

        if (population.Revision != transition.ExpectedPopulationRevision
            || population.CurrentPopulation != transition.PopulationBefore
            || representedResidentFloor != transition.RepresentedResidentFloor)
        {
            failure = AggregateDemographyFailure.StaleState;
            return false;
        }

        if (population.Revision == long.MaxValue)
        {
            failure = AggregateDemographyFailure.RevisionOverflow;
            return false;
        }

        if (transition.PopulationAfter < representedResidentFloor)
        {
            failure = AggregateDemographyFailure.WouldViolateRepresentedResidentFloor;
            return false;
        }

        PopulationChangeSet change = new PopulationChangeSet(
            transition.Births,
            transition.Deaths,
            0,
            0);
        if (SettlementPopulationSystem.TryPropose(
                population,
                change,
                out SettlementPopulationTransition populationTransition,
                out PopulationTransitionFailure populationFailure) == false)
        {
            failure = MapPopulationFailure(populationFailure);
            return false;
        }

        if (populationTransition.ExpectedRevision != transition.ExpectedPopulationRevision
            || populationTransition.PopulationBefore != transition.PopulationBefore
            || populationTransition.Births != transition.Births
            || populationTransition.Deaths != transition.Deaths
            || populationTransition.Immigrations != 0
            || populationTransition.Emigrations != 0
            || populationTransition.NetChange != transition.NetChange
            || populationTransition.PopulationAfter != transition.PopulationAfter)
        {
            failure = AggregateDemographyFailure.InvalidTransition;
            return false;
        }

        if (SettlementPopulationSystem.TryApply(
                population,
                populationTransition,
                out populationFailure) == false)
        {
            failure = MapPopulationFailure(populationFailure);
            return false;
        }

        return true;
    }

    private static bool TryValidatePopulationAndFloor(
        SettlementPopulationRuntime population,
        int representedResidentFloor,
        out AggregateDemographyFailure failure)
    {
        failure = AggregateDemographyFailure.None;
        if (population == null || string.IsNullOrWhiteSpace(population.SettlementRuntimeId) == true)
        {
            failure = AggregateDemographyFailure.InvalidSettlement;
            return false;
        }

        if (representedResidentFloor < 0
            || representedResidentFloor > population.CurrentPopulation)
        {
            failure = AggregateDemographyFailure.InvalidRepresentedResidentFloor;
            return false;
        }

        return true;
    }

    private static bool IsValidTransitionShape(AggregateDemographyTransition transition)
    {
        if (transition == null
            || string.IsNullOrWhiteSpace(transition.SettlementRuntimeId) == true
            || transition.ExpectedPopulationRevision < 0L
            || transition.PopulationBefore < 0
            || transition.RepresentedResidentFloor < 0
            || transition.RepresentedResidentFloor > transition.PopulationBefore
            || transition.Births < 0
            || transition.Deaths < 0
            || transition.PopulationAfter < 0)
        {
            return false;
        }

        long netChange = (long)transition.Births - transition.Deaths;
        return transition.NetChange == netChange
            && (long)transition.PopulationBefore + netChange == transition.PopulationAfter;
    }

    private static AggregateDemographyFailure MapPopulationFailure(
        PopulationTransitionFailure failure)
    {
        switch (failure)
        {
            case PopulationTransitionFailure.WouldUnderflow:
                return AggregateDemographyFailure.WouldUnderflow;
            case PopulationTransitionFailure.WouldOverflow:
                return AggregateDemographyFailure.WouldOverflow;
            case PopulationTransitionFailure.RevisionOverflow:
                return AggregateDemographyFailure.RevisionOverflow;
            case PopulationTransitionFailure.StaleState:
                return AggregateDemographyFailure.StaleState;
            case PopulationTransitionFailure.NegativeChange:
                return AggregateDemographyFailure.NegativeChange;
            case PopulationTransitionFailure.InvalidSettlement:
                return AggregateDemographyFailure.InvalidSettlement;
            case PopulationTransitionFailure.InvalidTransition:
            default:
                return AggregateDemographyFailure.InvalidTransition;
        }
    }
}
