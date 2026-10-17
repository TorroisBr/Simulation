using System;

/// <summary>
/// Narrow Unity-facing entry points for the shared chronological owner algorithms.
/// </summary>
public static class PersonChronologyOwnerBridge
{
    public static bool TryCalculateAge(
        PersonRuntime person,
        long currentAbsoluteDay,
        CalendarDefinition calendarDefinition,
        out PersonAgeSnapshot age,
        out PersonAgeQueryFailure failure)
    {
        return PersonAgeCalculator.TryCalculate(
            person,
            currentAbsoluteDay,
            calendarDefinition,
            out age,
            out failure);
    }

    public static bool TryCalculateAge(
        PersonRuntime person,
        long currentAbsoluteDay,
        SimulationCalendar calendar,
        out PersonAgeSnapshot age,
        out PersonAgeQueryFailure failure)
    {
        return PersonAgeCalculator.TryCalculate(
            person,
            currentAbsoluteDay,
            calendar,
            out age,
            out failure);
    }

    public static bool TryCalculateMaturity(
        PersonRuntime person,
        long currentAbsoluteDay,
        CalendarDefinition calendarDefinition,
        long maturityAgeYears,
        out PersonMaturitySnapshot maturity,
        out PersonMaturityQueryFailure failure)
    {
        return PersonMaturityCalculator.TryCalculate(
            person,
            currentAbsoluteDay,
            calendarDefinition,
            maturityAgeYears,
            out maturity,
            out failure);
    }

    public static bool TryCalculateMaturity(
        PersonRuntime person,
        long currentAbsoluteDay,
        SimulationCalendar calendar,
        long maturityAgeYears,
        out PersonMaturitySnapshot maturity,
        out PersonMaturityQueryFailure failure)
    {
        return PersonMaturityCalculator.TryCalculate(
            person,
            currentAbsoluteDay,
            calendar,
            maturityAgeYears,
            out maturity,
            out failure);
    }

    public static bool TryCalculateMaturity(
        PersonRuntime person,
        long currentAbsoluteDay,
        CalendarDefinition calendarDefinition,
        EffectivePopulationConfiguration configuration,
        out PersonMaturitySnapshot maturity,
        out PersonMaturityQueryFailure failure)
    {
        return PersonMaturityCalculator.TryCalculate(
            person,
            currentAbsoluteDay,
            calendarDefinition,
            configuration,
            out maturity,
            out failure);
    }
}
