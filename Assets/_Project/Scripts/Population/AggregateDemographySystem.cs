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
        return TryPropose(
            population,
            representedResidentFloor,
            provider,
            0L,
            out transition,
            out failure);
    }

    public static bool TryPropose(
        SettlementPopulationRuntime population,
        int representedResidentFloor,
        IAggregateDemographyProvider provider,
        long currentAbsoluteDay,
        out AggregateDemographyTransition transition,
        out AggregateDemographyFailure failure)
    {
        transition = null;
        failure = AggregateDemographyFailure.None;

        if (population != null && !population.CanMutate)
        {
            failure = AggregateDemographyFailure.RuntimeFaulted;
            return false;
        }

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
            representedResidentFloor,
            currentAbsoluteDay);
        AggregateDemographyChange change;
        try
        {
            change = provider.GetChange(context);
        }
        catch
        {
            population.RestoreSnapshot(context.CurrentPopulation, context.PopulationRevision);
            failure = AggregateDemographyFailure.InvalidProvider;
            return false;
        }

        // A provider is policy, not mutation authority. Detect a provider that caused
        // population state to change while deciding instead of accepting a mixed snapshot.
        if (population.Revision != context.PopulationRevision
            || population.CurrentPopulation != context.CurrentPopulation)
        {
            population.RestoreSnapshot(context.CurrentPopulation, context.PopulationRevision);
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
        return TryApplyCore(population, representedResidentFloor, transition,
            null, out _, out failure);
    }

    internal static bool TryApplyWithReceipt(
        SettlementPopulationRuntime population,
        int representedResidentFloor,
        AggregateDemographyTransition transition,
        string operationIdentity,
        out bool newlyApplied,
        out AggregateDemographyFailure failure)
    {
        if (string.IsNullOrWhiteSpace(operationIdentity))
        {
            newlyApplied = false;
            failure = AggregateDemographyFailure.InvalidTransition;
            return false;
        }

        return TryApplyCore(population, representedResidentFloor, transition,
            operationIdentity, out newlyApplied, out failure);
    }

    private static bool TryApplyCore(
        SettlementPopulationRuntime population,
        int representedResidentFloor,
        AggregateDemographyTransition transition,
        string operationIdentity,
        out bool newlyApplied,
        out AggregateDemographyFailure failure)
    {
        newlyApplied = false;
        failure = AggregateDemographyFailure.None;

        string operationFingerprint = null;
        if (operationIdentity != null)
        {
            if (population == null || transition == null
                || !string.Equals(population.SettlementRuntimeId,
                    transition.SettlementRuntimeId, StringComparison.Ordinal))
            {
                failure = AggregateDemographyFailure.InvalidSettlement;
                return false;
            }

            if (!IsValidTransitionShape(transition))
            {
                failure = AggregateDemographyFailure.InvalidTransition;
                return false;
            }

            if (representedResidentFloor != transition.RepresentedResidentFloor)
            {
                failure = AggregateDemographyFailure.StaleState;
                return false;
            }

            operationFingerprint = CreateOperationFingerprint(transition);
            PopulationOperationReceiptResolution receiptResolution =
                population.ResolveOperationReceipt(operationIdentity, operationFingerprint);
            if (receiptResolution == PopulationOperationReceiptResolution.Matching)
            {
                return true;
            }

            if (receiptResolution == PopulationOperationReceiptResolution.Conflicting)
            {
                failure = AggregateDemographyFailure.OperationIdentityConflict;
                return false;
            }
        }

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

        bool applied;
        bool populationApplied;
        if (operationIdentity == null)
        {
            applied = SettlementPopulationSystem.TryApply(
                population,
                populationTransition,
                out populationFailure);
            populationApplied = applied;
        }
        else
        {
            applied = population.TryApplyTransitionWithReceipt(
                operationIdentity,
                operationFingerprint,
                populationTransition,
                out populationApplied,
                out populationFailure);
        }

        if (!applied)
        {
            failure = MapPopulationFailure(populationFailure);
            return false;
        }

        newlyApplied = populationApplied;
        return true;
    }

    private static string CreateOperationFingerprint(AggregateDemographyTransition transition)
    {
        return SpatialStableKey.Encode(
            "aggregate-demography-v1",
            transition.SettlementRuntimeId,
            transition.ExpectedPopulationRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            transition.PopulationBefore.ToString(System.Globalization.CultureInfo.InvariantCulture),
            transition.RepresentedResidentFloor.ToString(System.Globalization.CultureInfo.InvariantCulture),
            transition.Births.ToString(System.Globalization.CultureInfo.InvariantCulture),
            transition.Deaths.ToString(System.Globalization.CultureInfo.InvariantCulture),
            transition.NetChange.ToString(System.Globalization.CultureInfo.InvariantCulture),
            transition.PopulationAfter.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
            case PopulationTransitionFailure.RuntimeFaulted:
                return AggregateDemographyFailure.RuntimeFaulted;
            case PopulationTransitionFailure.StaleState:
                return AggregateDemographyFailure.StaleState;
            case PopulationTransitionFailure.NegativeChange:
                return AggregateDemographyFailure.NegativeChange;
            case PopulationTransitionFailure.OperationIdentityConflict:
                return AggregateDemographyFailure.OperationIdentityConflict;
            case PopulationTransitionFailure.InvalidSettlement:
                return AggregateDemographyFailure.InvalidSettlement;
            case PopulationTransitionFailure.InvalidTransition:
            default:
                return AggregateDemographyFailure.InvalidTransition;
        }
    }
}
