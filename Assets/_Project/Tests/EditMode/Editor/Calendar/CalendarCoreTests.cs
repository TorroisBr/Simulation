using System;
using NUnit.Framework;

public sealed class CalendarCoreTests
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

    [Test]
    public void SimulationCalendarRejectsNullDefinition()
    {
        Assert.Throws<ArgumentNullException>(() => new SimulationCalendar(null));
    }

    [Test]
    public void CalendarDefinitionRejectsNullCustomMonthLengths()
    {
        Assert.Throws<ArgumentNullException>(() => new CalendarDefinition((System.Collections.Generic.IList<int>)null, 1));
    }

    [Test]
    public void CustomMonthLengthsMustMatchMonthCount()
    {
        CalendarDefinition definition = new CalendarDefinition(new[] { 10, 20, 5 }, 1);
        definition.monthsPerYear = 2;

        Assert.That(definition.TryValidate(out _), Is.False);
        Assert.Throws<ArgumentException>(() => new SimulationCalendar(definition));
    }

    [Test]
    public void CustomMonthLengthsMustBePositive()
    {
        CalendarDefinition definition = new CalendarDefinition(new[] { 10, 0, 5 }, 1);

        Assert.That(definition.TryValidate(out _), Is.False);
        Assert.Throws<ArgumentException>(() => new SimulationCalendar(definition));
    }

    [Test]
    public void UniformMonthLengthBeyondDayOfMonthRangeIsRejected()
    {
        CalendarDefinition definition = new CalendarDefinition(1, int.MaxValue, int.MaxValue);

        Assert.That(definition.TryValidate(out string diagnostic), Is.False);
        Assert.That(diagnostic, Does.Contain("supported day-of-month range"));
        Assert.Throws<ArgumentException>(() => new SimulationCalendar(definition));
    }

    [Test]
    public void LegacyResolverUsesSafeDefaultForNullAndInvalidDefinitions()
    {
        CalendarDefinition missing = CalendarDefinition.CreateValidatedOrDefault(null, out string missingDiagnostic);
        CalendarDefinition invalid = CalendarDefinition.CreateValidatedOrDefault(new CalendarDefinition(0, 1, 1), out string invalidDiagnostic);

        Assert.That(missing.DaysPerYear, Is.EqualTo(1L));
        Assert.That(missingDiagnostic, Does.Contain("missing"));
        Assert.That(invalid.DaysPerYear, Is.EqualTo(1L));
        Assert.That(invalidDiagnostic, Does.Contain("safe default"));
    }

    [Test]
    public void CustomCalendarSnapshotsMonthLengthsAtConstruction()
    {
        CalendarDefinition definition = new CalendarDefinition(new[] { 10, 20, 5 }, 1);
        SimulationCalendar calendar = new SimulationCalendar(definition);
        definition.monthLengths[0] = 2;
        definition.monthLengths[1] = 2;
        definition.monthLengths[2] = 2;

        Assert.That(calendar.GetDate(9L).Month, Is.EqualTo(1));
        Assert.That(calendar.GetDate(10L).Month, Is.EqualTo(2));
        Assert.That(calendar.DaysPerYear, Is.EqualTo(35L));
    }

    [Test]
    public void SemanticIdentityAndVersionReflectEffectiveCalendar()
    {
        SimulationCalendar conventional = ConventionalCalendar();
        SimulationCalendar unequal = new SimulationCalendar(new CalendarDefinition(new[] { 10, 20, 5 }, 1));

        Assert.That(conventional.SemanticIdentity, Is.EqualTo("simulation-calendar"));
        Assert.That(conventional.SemanticVersion, Is.EqualTo("12/30/360/uniform:30"));
        Assert.That(unequal.SemanticIdentity, Is.EqualTo("simulation-calendar"));
        Assert.That(unequal.SemanticVersion, Is.EqualTo("3/1/35/custom:10,20,5"));
    }

    [Test]
    public void SimulationDateValueSemanticsCoverEveryField()
    {
        SimulationDate original = new SimulationDate(1L, 2L, 3, 4, 5, 6, 7L, 8L, 9L);
        SimulationDate equal = new SimulationDate(1L, 2L, 3, 4, 5, 6, 7L, 8L, 9L);
        SimulationDate[] differingFields =
        {
            new SimulationDate(10L, 2L, 3, 4, 5, 6, 7L, 8L, 9L),
            new SimulationDate(1L, 10L, 3, 4, 5, 6, 7L, 8L, 9L),
            new SimulationDate(1L, 2L, 10, 4, 5, 6, 7L, 8L, 9L),
            new SimulationDate(1L, 2L, 3, 10, 5, 6, 7L, 8L, 9L),
            new SimulationDate(1L, 2L, 3, 4, 10, 6, 7L, 8L, 9L),
            new SimulationDate(1L, 2L, 3, 4, 5, 10, 7L, 8L, 9L),
            new SimulationDate(1L, 2L, 3, 4, 5, 6, 10L, 8L, 9L),
            new SimulationDate(1L, 2L, 3, 4, 5, 6, 7L, 10L, 9L),
            new SimulationDate(1L, 2L, 3, 4, 5, 6, 7L, 8L, 10L)
        };

        Assert.That(original.Equals(equal), Is.True);
        Assert.That(original.Equals((object)equal), Is.True);
        Assert.That(original == equal, Is.True);
        Assert.That(original != equal, Is.False);
        Assert.That(original.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
        Assert.That(original.CompareTo(equal), Is.Zero);
        Assert.That(original.ToString(), Is.EqualTo("Year 2 / Month 3 / Day 5"));
        foreach (SimulationDate differing in differingFields)
        {
            Assert.That(original.Equals(differing), Is.False);
            Assert.That(original == differing, Is.False);
            Assert.That(original != differing, Is.True);
        }
        Assert.That(original.CompareTo(new SimulationDate(2L, 2L, 3, 4, 5, 6, 7L, 8L, 9L)), Is.LessThan(0));
    }

    [Test]
    public void NonUniformCalendarDemoSequenceMatchesExpectedDatesAndAge()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(new[] { 10, 20, 5 }, 1));
        long[] absoluteDays = { 0L, 10L, 30L, 35L };
        int[] expectedMonths = { 1, 2, 3, 1 };
        int[] expectedDays = { 1, 1, 1, 1 };
        long[] expectedYears = { 0L, 0L, 0L, 1L };

        for (int i = 0; i < absoluteDays.Length; i++)
        {
            SimulationDate date = calendar.GetDate(absoluteDays[i]);
            Assert.That(date.AbsoluteDay, Is.EqualTo(absoluteDays[i]));
            Assert.That(date.Year, Is.EqualTo(expectedYears[i]));
            Assert.That(date.Month, Is.EqualTo(expectedMonths[i]));
            Assert.That(date.DayOfMonth, Is.EqualTo(expectedDays[i]));
        }

        SimulationDate birthDate = calendar.GetDate(9L);
        Assert.That(calendar.CalculateAge(birthDate, calendar.GetDate(43L)), Is.EqualTo(0L));
        Assert.That(calendar.CalculateAge(birthDate, calendar.GetDate(44L)), Is.EqualTo(1L));
    }

    [TestCase("en-US")]
    [TestCase("pt-BR")]
    public void CalendarParityVectorsAreCultureIndependent(string cultureName)
    {
        System.Globalization.CultureInfo originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo(cultureName);
            System.Globalization.CultureInfo.CurrentCulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = culture;

            SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(new[] { 10, 20, 5 }, 1));
            SimulationDate date = calendar.GetDate(30L);
            Assert.That(calendar.SemanticIdentity, Is.EqualTo("simulation-calendar"));
            Assert.That(calendar.SemanticVersion, Is.EqualTo("3/1/35/custom:10,20,5"));
            Assert.That(calendar.GetAbsoluteDay(date), Is.EqualTo(30L));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUiCulture;
        }
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
