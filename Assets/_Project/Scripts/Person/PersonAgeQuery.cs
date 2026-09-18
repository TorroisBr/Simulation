using System;

public enum PersonAgeQueryFailure
{
    None = 0,
    InvalidPerson = 1,
    BirthDateUnknown = 2,
    BirthDateInFuture = 3,
    InvalidCalendar = 4,
    InvalidCurrentDay = 5
}

/// <summary>
/// Immutable chronological age result. Age is derived for the requested day and
/// is not stored or advanced on PersonRuntime.
/// </summary>
public sealed class PersonAgeSnapshot
{
    public long BirthAbsoluteDay { get; }
    public SimulationDate BirthDate { get; }
    public SimulationDate CurrentDate { get; }
    public long AgeInDays { get; }
    public long CompletedYears { get; }

    public PersonAgeSnapshot(
        long birthAbsoluteDay,
        SimulationDate birthDate,
        SimulationDate currentDate,
        long ageInDays,
        long completedYears)
    {
        BirthAbsoluteDay = birthAbsoluteDay;
        BirthDate = birthDate;
        CurrentDate = currentDate;
        AgeInDays = ageInDays;
        CompletedYears = completedYears;
    }
}

/// <summary>
/// Pure chronological query for PersonRuntime. It never mutates the Person,
/// world time, calendar, NPC roster, or population aggregates.
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

        return TryCalculate(
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

        return TryCalculate(
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
