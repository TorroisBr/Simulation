/// <summary>
/// Unity time adapter for the shared, pure chronological-age calculation.
/// </summary>
public static class PersonAgeQuery
{
    public static bool TryCalculate(
        PersonRuntime person,
        SimulationTime simulationTime,
        CalendarDefinition calendarDefinition,
        out PersonAgeSnapshot age,
        out PersonAgeQueryFailure failure)
    {
        age = null;
        if (simulationTime == null)
        {
            failure = PersonAgeQueryFailure.InvalidCurrentDay;
            return false;
        }

        return PersonChronologyOwnerBridge.TryCalculateAge(
            person,
            simulationTime.AbsoluteDay,
            calendarDefinition,
            out age,
            out failure);
    }

    public static bool TryCalculate(
        PersonRuntime person,
        SimulationTime simulationTime,
        SimulationCalendar calendar,
        out PersonAgeSnapshot age,
        out PersonAgeQueryFailure failure)
    {
        age = null;
        if (simulationTime == null)
        {
            failure = PersonAgeQueryFailure.InvalidCurrentDay;
            return false;
        }

        return PersonChronologyOwnerBridge.TryCalculateAge(
            person,
            simulationTime.AbsoluteDay,
            calendar,
            out age,
            out failure);
    }

    public static bool TryCalculate(
        PersonRuntime person,
        long currentAbsoluteDay,
        CalendarDefinition calendarDefinition,
        out PersonAgeSnapshot age,
        out PersonAgeQueryFailure failure)
    {
        return PersonChronologyOwnerBridge.TryCalculateAge(
            person,
            currentAbsoluteDay,
            calendarDefinition,
            out age,
            out failure);
    }

    public static bool TryCalculate(
        PersonRuntime person,
        long currentAbsoluteDay,
        SimulationCalendar calendar,
        out PersonAgeSnapshot age,
        out PersonAgeQueryFailure failure)
    {
        return PersonChronologyOwnerBridge.TryCalculateAge(
            person,
            currentAbsoluteDay,
            calendar,
            out age,
            out failure);
    }
}
