using System;

public enum PersonNaturalMortalityQueryFailure
{
    None = 0,
    InvalidPerson = 1,
    BirthDateUnknown = 2,
    BirthDateInFuture = 3,
    DeathAlreadyRecorded = 4,
    InvalidCalendar = 5,
    InvalidCurrentDay = 6,
    InvalidAnnualProbability = 7,
    InvalidDeterministicSample = 8
}

/// <summary>
/// Immutable pure evaluation for one Person on one day. Randomness ownership is
/// external: the deterministic sample is an explicit input and is preserved in
/// the result for replay and testing.
/// </summary>
public sealed class PersonNaturalMortalityEvaluation
{
    public PersonAgeSnapshot Age { get; }
    public long CurrentAbsoluteDay => Age.CurrentDate.AbsoluteDay;
    public long CompletedYears => Age.CompletedYears;
    public double AnnualProbability { get; }
    public double DailyProbability { get; }
    public double DeterministicSample { get; }
    public bool ShouldDie { get; }

    public PersonNaturalMortalityEvaluation(
        PersonAgeSnapshot age,
        double annualProbability,
        double dailyProbability,
        double deterministicSample)
    {
        Age = age ?? throw new ArgumentNullException(nameof(age));
        AnnualProbability = annualProbability;
        DailyProbability = dailyProbability;
        DeterministicSample = deterministicSample;
        ShouldDie = deterministicSample < dailyProbability;
    }
}

/// <summary>
/// Configuration-neutral natural-mortality evaluation. The caller supplies the
/// annual probability selected for the derived age and a deterministic sample.
/// Conversion to a daily probability uses the active calendar's year length;
/// no terrestrial year length is assumed.
/// </summary>
public static class PersonNaturalMortalityQuery
{
    public static bool TryEvaluate(
        PersonRuntime person,
        long currentAbsoluteDay,
        CalendarDefinition calendarDefinition,
        double annualProbability,
        double deterministicSample,
        out PersonNaturalMortalityEvaluation evaluation,
        out PersonNaturalMortalityQueryFailure failure)
    {
        evaluation = null;
        if (calendarDefinition == null || calendarDefinition.TryValidate(out _) == false)
        {
            failure = PersonNaturalMortalityQueryFailure.InvalidCalendar;
            return false;
        }

        try
        {
            return TryEvaluate(
                person,
                currentAbsoluteDay,
                new SimulationCalendar(calendarDefinition),
                annualProbability,
                deterministicSample,
                out evaluation,
                out failure);
        }
        catch (ArgumentException)
        {
            failure = PersonNaturalMortalityQueryFailure.InvalidCalendar;
            return false;
        }
        catch (OverflowException)
        {
            failure = PersonNaturalMortalityQueryFailure.InvalidCalendar;
            return false;
        }
    }

    public static bool TryEvaluate(
        PersonRuntime person,
        long currentAbsoluteDay,
        SimulationCalendar calendar,
        double annualProbability,
        double deterministicSample,
        out PersonNaturalMortalityEvaluation evaluation,
        out PersonNaturalMortalityQueryFailure failure)
    {
        evaluation = null;
        if (double.IsNaN(annualProbability)
            || double.IsInfinity(annualProbability)
            || annualProbability < 0d
            || annualProbability > 1d)
        {
            failure = PersonNaturalMortalityQueryFailure.InvalidAnnualProbability;
            return false;
        }

        if (double.IsNaN(deterministicSample)
            || double.IsInfinity(deterministicSample)
            || deterministicSample < 0d
            || deterministicSample >= 1d)
        {
            failure = PersonNaturalMortalityQueryFailure.InvalidDeterministicSample;
            return false;
        }

        if (person != null && person.DeathAbsoluteDay.HasValue)
        {
            failure = PersonNaturalMortalityQueryFailure.DeathAlreadyRecorded;
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

        double dailyProbability = annualProbability >= 1d
            ? 1d
            : 1d - Math.Pow(1d - annualProbability, 1d / calendar.DaysPerYear);
        evaluation = new PersonNaturalMortalityEvaluation(
            age,
            annualProbability,
            dailyProbability,
            deterministicSample);
        failure = PersonNaturalMortalityQueryFailure.None;
        return true;
    }

    private static PersonNaturalMortalityQueryFailure Map(PersonAgeQueryFailure failure)
    {
        switch (failure)
        {
            case PersonAgeQueryFailure.InvalidPerson:
                return PersonNaturalMortalityQueryFailure.InvalidPerson;
            case PersonAgeQueryFailure.BirthDateUnknown:
                return PersonNaturalMortalityQueryFailure.BirthDateUnknown;
            case PersonAgeQueryFailure.BirthDateInFuture:
                return PersonNaturalMortalityQueryFailure.BirthDateInFuture;
            case PersonAgeQueryFailure.InvalidCalendar:
                return PersonNaturalMortalityQueryFailure.InvalidCalendar;
            case PersonAgeQueryFailure.InvalidCurrentDay:
                return PersonNaturalMortalityQueryFailure.InvalidCurrentDay;
            default:
                return PersonNaturalMortalityQueryFailure.InvalidCalendar;
        }
    }
}
