using System;

public enum PersonMaturityQueryFailure
{
    None = 0,
    InvalidPerson,
    BirthDateUnknown,
    BirthDateInFuture,
    InvalidCalendar,
    InvalidCurrentDay,
    InvalidMaturityAge,
    InvalidConfiguration
}

/// <summary>
/// Immutable maturity observation derived for one requested world day.
/// It is not stored on PersonRuntime.
/// </summary>
public sealed class PersonMaturitySnapshot
{
    public PersonMaturitySnapshot(
        long ageInDays,
        long completedYears,
        long maturityAgeYears,
        bool isMature)
    {
        AgeInDays = ageInDays;
        CompletedYears = completedYears;
        MaturityAgeYears = maturityAgeYears;
        IsMature = isMature;
    }

    public long AgeInDays { get; }

    public long CompletedYears { get; }

    public long MaturityAgeYears { get; }

    public bool IsMature { get; }
}

/// <summary>
/// Pure maturity query. Calendar and completed-year semantics come from PersonAgeQuery.
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
        maturity = null;
        if (maturityAgeYears < 0L)
        {
            failure = PersonMaturityQueryFailure.InvalidMaturityAge;
            return false;
        }

        if (PersonAgeQuery.TryCalculate(
            person,
            currentAbsoluteDay,
            calendarDefinition,
            out PersonAgeSnapshot age,
            out PersonAgeQueryFailure ageFailure) == false)
        {
            failure = Map(ageFailure);
            return false;
        }

        maturity = CreateSnapshot(age, maturityAgeYears);
        failure = PersonMaturityQueryFailure.None;
        return true;
    }

    public static bool TryCalculate(
        PersonRuntime person,
        long currentAbsoluteDay,
        SimulationCalendar calendar,
        long maturityAgeYears,
        out PersonMaturitySnapshot maturity,
        out PersonMaturityQueryFailure failure)
    {
        maturity = null;
        if (maturityAgeYears < 0L)
        {
            failure = PersonMaturityQueryFailure.InvalidMaturityAge;
            return false;
        }

        if (PersonAgeQuery.TryCalculate(
            person,
            currentAbsoluteDay,
            calendar,
            out PersonAgeSnapshot age,
            out PersonAgeQueryFailure ageFailure) == false)
        {
            failure = Map(ageFailure);
            return false;
        }

        maturity = CreateSnapshot(age, maturityAgeYears);
        failure = PersonMaturityQueryFailure.None;
        return true;
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

        return TryCalculate(
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
        maturity = null;
        if (configuration == null)
        {
            failure = PersonMaturityQueryFailure.InvalidConfiguration;
            return false;
        }

        return TryCalculate(
            person,
            currentAbsoluteDay,
            calendarDefinition,
            configuration.MaturityAgeYears,
            out maturity,
            out failure);
    }

    private static PersonMaturitySnapshot CreateSnapshot(
        PersonAgeSnapshot age,
        long maturityAgeYears)
    {
        return new PersonMaturitySnapshot(
            age.AgeInDays,
            age.CompletedYears,
            maturityAgeYears,
            age.CompletedYears >= maturityAgeYears);
    }

    private static PersonMaturityQueryFailure Map(PersonAgeQueryFailure failure)
    {
        switch (failure)
        {
            case PersonAgeQueryFailure.InvalidPerson:
                return PersonMaturityQueryFailure.InvalidPerson;
            case PersonAgeQueryFailure.BirthDateUnknown:
                return PersonMaturityQueryFailure.BirthDateUnknown;
            case PersonAgeQueryFailure.BirthDateInFuture:
                return PersonMaturityQueryFailure.BirthDateInFuture;
            case PersonAgeQueryFailure.InvalidCalendar:
                return PersonMaturityQueryFailure.InvalidCalendar;
            case PersonAgeQueryFailure.InvalidCurrentDay:
                return PersonMaturityQueryFailure.InvalidCurrentDay;
            default:
                return PersonMaturityQueryFailure.InvalidConfiguration;
        }
    }
}
