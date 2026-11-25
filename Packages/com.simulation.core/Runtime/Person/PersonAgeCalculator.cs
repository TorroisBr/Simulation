using System;

/// <summary>
/// Pure chronological query for PersonRuntime. It never mutates the Person,
/// world time, calendar, NPC roster, or population aggregates.
/// </summary>
internal static class PersonAgeCalculator
{
    public static bool TryCalculate(
        PersonRuntime person,
        long currentAbsoluteDay,
        CalendarDefinition calendarDefinition,
        out PersonAgeSnapshot age,
        out PersonAgeQueryFailure failure)
    {
        age = null;
        if (person == null)
        {
            failure = PersonAgeQueryFailure.InvalidPerson;
            return false;
        }

        if (person.HasKnownBirthDay == false)
        {
            failure = PersonAgeQueryFailure.BirthDateUnknown;
            return false;
        }

        if (currentAbsoluteDay < 0L)
        {
            failure = PersonAgeQueryFailure.InvalidCurrentDay;
            return false;
        }

        if (person.BirthAbsoluteDay.Value > currentAbsoluteDay)
        {
            failure = PersonAgeQueryFailure.BirthDateInFuture;
            return false;
        }

        if (calendarDefinition == null || calendarDefinition.TryValidate(out _) == false)
        {
            failure = PersonAgeQueryFailure.InvalidCalendar;
            return false;
        }

        try
        {
            return TryCalculate(
                person,
                currentAbsoluteDay,
                new SimulationCalendar(calendarDefinition),
                out age,
                out failure);
        }
        catch (ArgumentException)
        {
            failure = PersonAgeQueryFailure.InvalidCalendar;
            return false;
        }
        catch (OverflowException)
        {
            failure = PersonAgeQueryFailure.InvalidCalendar;
            return false;
        }
    }

    public static bool TryCalculate(
        PersonRuntime person,
        long currentAbsoluteDay,
        SimulationCalendar calendar,
        out PersonAgeSnapshot age,
        out PersonAgeQueryFailure failure)
    {
        age = null;
        if (person == null)
        {
            failure = PersonAgeQueryFailure.InvalidPerson;
            return false;
        }

        if (person.HasKnownBirthDay == false)
        {
            failure = PersonAgeQueryFailure.BirthDateUnknown;
            return false;
        }

        if (currentAbsoluteDay < 0L)
        {
            failure = PersonAgeQueryFailure.InvalidCurrentDay;
            return false;
        }

        if (calendar == null)
        {
            failure = PersonAgeQueryFailure.InvalidCalendar;
            return false;
        }

        long birthAbsoluteDay = person.BirthAbsoluteDay.Value;
        if (birthAbsoluteDay > currentAbsoluteDay)
        {
            failure = PersonAgeQueryFailure.BirthDateInFuture;
            return false;
        }

        try
        {
            SimulationDate birthDate = calendar.GetDate(birthAbsoluteDay);
            SimulationDate currentDate = calendar.GetDate(currentAbsoluteDay);
            long completedYears = calendar.CalculateAge(birthDate, currentDate);
            age = new PersonAgeSnapshot(
                birthAbsoluteDay,
                birthDate,
                currentDate,
                currentAbsoluteDay - birthAbsoluteDay,
                completedYears);
            failure = PersonAgeQueryFailure.None;
            return true;
        }
        catch (ArgumentException)
        {
            failure = PersonAgeQueryFailure.InvalidCalendar;
            return false;
        }
        catch (OverflowException)
        {
            failure = PersonAgeQueryFailure.InvalidCalendar;
            return false;
        }
    }
}
