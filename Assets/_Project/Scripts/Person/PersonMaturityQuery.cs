/// <summary>
/// Unity time adapter for the shared, pure maturity calculation.
/// </summary>
public static class PersonMaturityQuery
{
    public static bool TryCalculate(
        PersonRuntime person,
        long currentAbsoluteDay,
        CalendarDefinition calendarDefinition,
        long maturityAgeYears,
        out PersonMaturitySnapshot maturity,
        out PersonMaturityQueryFailure failure)
    {
        return PersonChronologyOwnerBridge.TryCalculateMaturity(
            person,
            currentAbsoluteDay,
            calendarDefinition,
            maturityAgeYears,
            out maturity,
            out failure);
    }

    public static bool TryCalculate(
        PersonRuntime person,
        long currentAbsoluteDay,
        SimulationCalendar calendar,
        long maturityAgeYears,
        out PersonMaturitySnapshot maturity,
        out PersonMaturityQueryFailure failure)
    {
        return PersonChronologyOwnerBridge.TryCalculateMaturity(
            person,
            currentAbsoluteDay,
            calendar,
            maturityAgeYears,
            out maturity,
            out failure);
    }

    public static bool TryCalculate(
        PersonRuntime person,
        SimulationTime simulationTime,
        CalendarDefinition calendarDefinition,
        long maturityAgeYears,
        out PersonMaturitySnapshot maturity,
        out PersonMaturityQueryFailure failure)
    {
        maturity = null;
        if (simulationTime == null)
        {
            failure = PersonMaturityQueryFailure.InvalidCurrentDay;
            return false;
        }

        return PersonChronologyOwnerBridge.TryCalculateMaturity(
            person,
            simulationTime.AbsoluteDay,
            calendarDefinition,
            maturityAgeYears,
            out maturity,
            out failure);
    }

    public static bool TryCalculate(
        PersonRuntime person,
        long currentAbsoluteDay,
        CalendarDefinition calendarDefinition,
        EffectivePopulationConfiguration configuration,
        out PersonMaturitySnapshot maturity,
        out PersonMaturityQueryFailure failure)
    {
        return PersonChronologyOwnerBridge.TryCalculateMaturity(
            person,
            currentAbsoluteDay,
            calendarDefinition,
            configuration,
            out maturity,
            out failure);
    }
}
