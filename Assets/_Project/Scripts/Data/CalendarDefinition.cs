using System;

[Serializable]
public sealed class CalendarDefinition
{
    private const int DefaultDimension = 1;

    public int monthsPerYear = DefaultDimension;
    public int weeksPerMonth = DefaultDimension;
    public int daysPerWeek = DefaultDimension;

    public int MonthsPerYear => Math.Max(DefaultDimension, monthsPerYear);
    public int WeeksPerMonth => Math.Max(DefaultDimension, weeksPerMonth);
    public int DaysPerWeek => Math.Max(DefaultDimension, daysPerWeek);
    public long DaysPerMonth => (long)WeeksPerMonth * DaysPerWeek;
    public long DaysPerYear => DaysPerMonth > long.MaxValue / MonthsPerYear
        ? long.MaxValue
        : DaysPerMonth * MonthsPerYear;

    public CalendarDefinition()
    {
    }

    public CalendarDefinition(int monthsPerYear, int weeksPerMonth, int daysPerWeek)
    {
        this.monthsPerYear = monthsPerYear;
        this.weeksPerMonth = weeksPerMonth;
        this.daysPerWeek = daysPerWeek;
    }

    public bool TryValidate(out string diagnostic)
    {
        if (monthsPerYear <= 0 || weeksPerMonth <= 0 || daysPerWeek <= 0)
        {
            diagnostic = $"CalendarDefinition is invalid: MonthsPerYear={monthsPerYear}, WeeksPerMonth={weeksPerMonth}, DaysPerWeek={daysPerWeek}. All values must be greater than zero.";
            return false;
        }

        long daysPerMonth = (long)weeksPerMonth * daysPerWeek;

        if (daysPerMonth > int.MaxValue)
        {
            diagnostic = $"CalendarDefinition is invalid: WeeksPerMonth={weeksPerMonth} and DaysPerWeek={daysPerWeek} exceed the supported month length.";
            return false;
        }

        if (daysPerMonth > long.MaxValue / monthsPerYear)
        {
            diagnostic = $"CalendarDefinition is invalid: MonthsPerYear={monthsPerYear}, WeeksPerMonth={weeksPerMonth}, DaysPerWeek={daysPerWeek} exceed the supported day range.";
            return false;
        }

        diagnostic = null;
        return true;
    }

    public SimulationDate GetDate(long absoluteDay)
    {
        CalendarDefinition effectiveCalendar = CreateValidatedOrDefault(this, out _);
        long daysPerMonth = effectiveCalendar.DaysPerMonth;
        long daysPerYear = effectiveCalendar.DaysPerYear;

        if (absoluteDay <= 0L)
        {
            return new SimulationDate(0L, 0L, 0, 0, 0, 0, 0L, daysPerMonth, daysPerYear);
        }

        long zeroBasedDay = absoluteDay - 1L;
        long year = zeroBasedDay / daysPerYear + 1L;
        long dayOfYear = zeroBasedDay % daysPerYear + 1L;
        int month = (int)((dayOfYear - 1L) / daysPerMonth) + 1;
        int dayOfMonth = (int)((dayOfYear - 1L) % daysPerMonth) + 1;
        int weekOfMonth = (dayOfMonth - 1) / effectiveCalendar.DaysPerWeek + 1;
        int dayOfWeek = (dayOfMonth - 1) % effectiveCalendar.DaysPerWeek + 1;

        return new SimulationDate(
            absoluteDay,
            year,
            month,
            weekOfMonth,
            dayOfMonth,
            dayOfWeek,
            dayOfYear,
            daysPerMonth,
            daysPerYear);
    }

    public static CalendarDefinition CreateValidatedOrDefault(CalendarDefinition calendar, out string diagnostic)
    {
        if (calendar == null)
        {
            diagnostic = "CalendarDefinition is missing. Using safe default calendar 1 x 1 x 1.";
            return CreateDefault();
        }

        if (calendar.TryValidate(out diagnostic) == false)
        {
            diagnostic += " Using safe default calendar 1 x 1 x 1.";
            return CreateDefault();
        }

        return calendar;
    }

    public static CalendarDefinition CreateDefault()
    {
        return new CalendarDefinition(DefaultDimension, DefaultDimension, DefaultDimension);
    }
}
