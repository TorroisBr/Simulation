using System;
using NUnit.Framework;
using UnityEngine;

public sealed class CalendarFoundationTests
{
    [Test]
    public void AbsoluteDayZeroMapsToFirstCalendarDate()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));

        SimulationDate date = calendar.GetDate(0L);

        Assert.That(date.AbsoluteDay, Is.EqualTo(0L));
        Assert.That(date.Year, Is.EqualTo(0L));
        Assert.That(date.Month, Is.EqualTo(1));
        Assert.That(date.DayOfMonth, Is.EqualTo(1));
        Assert.That(date.DayOfYear, Is.EqualTo(1L));
    }

    [Test]
    public void AdvanceOneAbsoluteDayAdvancesCalendarDate()
    {
        SimulationTime time = new SimulationTime();
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));

        time.AdvanceDay();

        SimulationDate date = calendar.GetDate(time.AbsoluteDay);

        Assert.That(date.AbsoluteDay, Is.EqualTo(1L));
        Assert.That(date.DayOfMonth, Is.EqualTo(2));
    }

    [Test]
    public void LastDayOfMonthRollsToNextMonth()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));

        Assert.That(calendar.GetDate(5L).Month, Is.EqualTo(1));
        Assert.That(calendar.GetDate(5L).DayOfMonth, Is.EqualTo(6));
        Assert.That(calendar.GetDate(6L).Month, Is.EqualTo(2));
        Assert.That(calendar.GetDate(6L).DayOfMonth, Is.EqualTo(1));
    }

    [Test]
    public void LastDayOfYearRollsToNextYear()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));

        SimulationDate lastDay = calendar.GetDate(11L);
        SimulationDate nextYear = calendar.GetDate(12L);

        Assert.That(lastDay.Year, Is.EqualTo(0L));
        Assert.That(lastDay.Month, Is.EqualTo(2));
        Assert.That(lastDay.DayOfMonth, Is.EqualTo(6));
        Assert.That(nextYear.Year, Is.EqualTo(1L));
        Assert.That(nextYear.Month, Is.EqualTo(1));
        Assert.That(nextYear.DayOfMonth, Is.EqualTo(1));
    }

    [Test]
    public void DaysPerYearComesFromCalendar()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(4, 3, 7));

        Assert.That(calendar.DaysPerYear, Is.EqualTo(84L));
        Assert.That(calendar.GetDate(84L).Year, Is.EqualTo(1L));
    }

    [Test]
    public void ConversionIsDeterministic()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(3, 2, 5));

        Assert.That(calendar.GetDate(17L), Is.EqualTo(calendar.GetDate(17L)));
        Assert.That(calendar.GetAbsoluteDay(calendar.GetDate(17L)), Is.EqualTo(17L));
    }

    [Test]
    public void ConversionDoesNotMutateSimulationTime()
    {
        SimulationTime time = new SimulationTime(17L);
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(3, 2, 5));

        calendar.GetDate(time.AbsoluteDay);

        Assert.That(time.AbsoluteDay, Is.EqualTo(17L));
    }

    [Test]
    public void CalendarRejectsZeroMonths()
    {
        Assert.Throws<ArgumentException>(() => new SimulationCalendar(new CalendarDefinition(0, 1, 1)));
    }

    [Test]
    public void CalendarRejectsZeroDaysPerMonth()
    {
        Assert.Throws<ArgumentException>(() => new SimulationCalendar(new CalendarDefinition(1, 0, 1)));
    }

    [Test]
    public void CalendarRejectsNegativeConfiguration()
    {
        Assert.Throws<ArgumentException>(() => new SimulationCalendar(new CalendarDefinition(1, 1, -1)));
    }

    [Test]
    public void InvalidMonthRejected()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(12, 1, 30));

        Assert.Throws<ArgumentException>(() => calendar.GetAbsoluteDay(RawDate(0L, 13, 1)));
    }

    [Test]
    public void InvalidDayOfMonthRejected()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(12, 1, 30));

        Assert.Throws<ArgumentException>(() => calendar.GetAbsoluteDay(RawDate(0L, 12, 31)));
    }

    [Test]
    public void DateToAbsoluteDayRoundTrips()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));
        long[] absoluteDays = { 0L, 1L, 5L, 6L, 11L, 12L, 29L };

        foreach (long absoluteDay in absoluteDays)
        {
            Assert.That(calendar.GetAbsoluteDay(calendar.GetDate(absoluteDay)), Is.EqualTo(absoluteDay));
        }
    }

    [Test]
    public void AbsoluteDayToDateRoundTrips()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(2, 2, 3));
        SimulationDate original = calendar.GetDate(19L);
        SimulationDate roundTrip = calendar.GetDate(calendar.GetAbsoluteDay(original));

        Assert.That(roundTrip, Is.EqualTo(original));
    }

    [Test]
    public void BirthDateAtCurrentDateHasAgeZero()
    {
        SimulationCalendar calendar = ConventionalCalendar();
        SimulationDate current = calendar.GetDate(64L);

        Assert.That(calendar.CalculateAge(current, current), Is.EqualTo(0L));
    }

    [Test]
    public void BeforeFirstBirthdayAgeIsZero()
    {
        SimulationCalendar calendar = ConventionalCalendar();

        Assert.That(calendar.CalculateAge(calendar.GetDate(64L), calendar.GetDate(423L)), Is.EqualTo(0L));
    }

    [Test]
    public void OnFirstBirthdayAgeIsOne()
    {
        SimulationCalendar calendar = ConventionalCalendar();

        Assert.That(calendar.CalculateAge(calendar.GetDate(64L), calendar.GetDate(424L)), Is.EqualTo(1L));
    }

    [Test]
    public void AfterFirstBirthdayAgeIsOne()
    {
        SimulationCalendar calendar = ConventionalCalendar();

        Assert.That(calendar.CalculateAge(calendar.GetDate(64L), calendar.GetDate(425L)), Is.EqualTo(1L));
    }

    [Test]
    public void MultipleYearsCalculateCorrectAge()
    {
        SimulationCalendar calendar = ConventionalCalendar();

        Assert.That(calendar.CalculateAge(calendar.GetDate(64L), calendar.GetDate(1144L)), Is.EqualTo(3L));
    }

    [Test]
    public void OneDayYearProducesBirthdayEveryAbsoluteDay()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 1));
        SimulationDate birthDate = calendar.GetDate(0L);

        Assert.That(calendar.CalculateAge(birthDate, calendar.GetDate(1L)), Is.EqualTo(1L));
        Assert.That(calendar.CalculateAge(birthDate, calendar.GetDate(2L)), Is.EqualTo(2L));
    }

    [Test]
    public void VeryShortYearDoesNotAssume365Days()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 2));
        SimulationDate birthDate = calendar.GetDate(0L);

        Assert.That(calendar.CalculateAge(birthDate, calendar.GetDate(2L)), Is.EqualTo(1L));
    }

    [Test]
    public void VeryLongYearDoesNotAssume365Days()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(1, 1, 700));
        SimulationDate birthDate = calendar.GetDate(0L);

        Assert.That(calendar.CalculateAge(birthDate, calendar.GetDate(699L)), Is.EqualTo(0L));
        Assert.That(calendar.CalculateAge(birthDate, calendar.GetDate(700L)), Is.EqualTo(1L));
    }

    [Test]
    public void FutureBirthDateRejected()
    {
        SimulationCalendar calendar = ConventionalCalendar();

        Assert.Throws<ArgumentException>(() => calendar.CalculateAge(calendar.GetDate(1L), calendar.GetDate(0L)));
    }

    [Test]
    public void AgeCalculationIsPure()
    {
        SimulationCalendar calendar = ConventionalCalendar();
        SimulationDate birthDate = calendar.GetDate(64L);
        SimulationDate currentDate = calendar.GetDate(424L);

        long first = calendar.CalculateAge(birthDate, currentDate);
        long second = calendar.CalculateAge(birthDate, currentDate);

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void AgeCalculationIsDeterministic()
    {
        SimulationCalendar calendar = ConventionalCalendar();

        Assert.That(CalendarAgeCalculator.CalculateAge(calendar.GetDate(64L), calendar.GetDate(1144L), calendar), Is.EqualTo(3L));
        Assert.That(CalendarAgeCalculator.CalculateAge(calendar.GetDate(64L), calendar.GetDate(1144L), calendar), Is.EqualTo(3L));
    }

    [Test]
    public void AgeCalculationDoesNotConsumeRng()
    {
        SimulationCalendar calendar = ConventionalCalendar();
        UnityEngine.Random.InitState(12345);
        UnityEngine.Random.State stateBeforeAge = UnityEngine.Random.state;
        int expectedNextValue = UnityEngine.Random.Range(0, int.MaxValue);

        UnityEngine.Random.state = stateBeforeAge;
        calendar.CalculateAge(calendar.GetDate(64L), calendar.GetDate(424L));
        int actualNextValue = UnityEngine.Random.Range(0, int.MaxValue);

        Assert.That(actualNextValue, Is.EqualTo(expectedNextValue));
    }

    [Test]
    public void AgeCalculationDoesNotAdvanceTime()
    {
        SimulationTime time = new SimulationTime(424L);
        SimulationCalendar calendar = ConventionalCalendar();

        calendar.CalculateAge(calendar.GetDate(64L), calendar.GetDate(time.AbsoluteDay));

        Assert.That(time.AbsoluteDay, Is.EqualTo(424L));
    }

    [Test]
    public void LongAbsoluteDayDoesNotOverflowIntermediateMath()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(int.MaxValue, 1, int.MaxValue));
        SimulationDate date = calendar.GetDate(long.MaxValue);

        Assert.That(calendar.GetAbsoluteDay(date), Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void CalendarDateEqualityIsValueBased()
    {
        SimulationCalendar calendar = ConventionalCalendar();
        SimulationDate first = calendar.GetDate(424L);
        SimulationDate second = new SimulationDate(424L, 1L, 3, 1, 5, 5, 65L, 30L, 360L);

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first == second, Is.True);
        Assert.That(calendar.GetDate(425L), Is.Not.EqualTo(first));
    }

    [Test]
    public void CalendarDefinitionStableDuringRuntimeIsDocumented()
    {
        CalendarDefinition definition = new CalendarDefinition(2, 1, 2);
        SimulationCalendar calendar = new SimulationCalendar(definition);

        definition.monthsPerYear = 10;
        definition.weeksPerMonth = 10;
        definition.daysPerWeek = 10;

        SimulationDate date = calendar.GetDate(4L);

        Assert.That(date.Year, Is.EqualTo(1L));
        Assert.That(date.Month, Is.EqualTo(1));
        Assert.That(date.DayOfMonth, Is.EqualTo(1));
    }

    [Test]
    public void UnequalMonthLengthsConvertCorrectly()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(new[] { 10, 20, 5 }, 1));

        Assert.That(calendar.GetDate(9L).Month, Is.EqualTo(1));
        Assert.That(calendar.GetDate(9L).DayOfMonth, Is.EqualTo(10));
        Assert.That(calendar.GetDate(10L).Month, Is.EqualTo(2));
        Assert.That(calendar.GetDate(10L).DayOfMonth, Is.EqualTo(1));
        Assert.That(calendar.GetDate(30L).Month, Is.EqualTo(3));
        Assert.That(calendar.GetDate(30L).DayOfMonth, Is.EqualTo(1));
    }

    [Test]
    public void UnequalMonthYearBoundaryCorrect()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(new[] { 10, 20, 5 }, 1));

        SimulationDate lastDay = calendar.GetDate(34L);
        SimulationDate nextYear = calendar.GetDate(35L);

        Assert.That(lastDay.Year, Is.EqualTo(0L));
        Assert.That(lastDay.Month, Is.EqualTo(3));
        Assert.That(lastDay.DayOfMonth, Is.EqualTo(5));
        Assert.That(nextYear.Year, Is.EqualTo(1L));
        Assert.That(nextYear.Month, Is.EqualTo(1));
        Assert.That(nextYear.DayOfMonth, Is.EqualTo(1));
    }

    private static SimulationCalendar ConventionalCalendar()
    {
        return new SimulationCalendar(new CalendarDefinition(12, 1, 30));
    }

    private static SimulationDate RawDate(long year, int month, int dayOfMonth)
    {
        return new SimulationDate(0L, year, month, 0, dayOfMonth, 0, 0L, 0L, 0L);
    }
}
