using System;
using System.Collections.Generic;

[Serializable]
public sealed class CalendarDefinition
{
    private const int DefaultDimension = 1;

    public int monthsPerYear = DefaultDimension;
    public int weeksPerMonth = DefaultDimension;
    public int daysPerWeek = DefaultDimension;

    // An empty list preserves the original uniform-month representation. When populated,
    // it must contain one positive length for every month in the year.
    public List<int> monthLengths = new List<int>();

    public int MonthsPerYear => monthsPerYear;
    public int WeeksPerMonth => weeksPerMonth;
    public int DaysPerWeek => daysPerWeek;
    public bool UsesCustomMonthLengths => monthLengths != null && monthLengths.Count > 0;

    // This property remains for the existing uniform-calendar API. For a variable-length
    // calendar it returns the first month length; callers that need the actual length use
    // GetDaysInMonth instead.
    public long DaysPerMonth => UsesCustomMonthLengths
        ? monthLengths[0]
        : (long)weeksPerMonth * daysPerWeek;

    public long DaysPerYear
    {
        get
        {
            return TryCalculateDaysPerYear(out long daysPerYear) ? daysPerYear : 0L;
        }
    }

    public CalendarDefinition()
    {
    }

    public CalendarDefinition(int monthsPerYear, int weeksPerMonth, int daysPerWeek)
    {
        this.monthsPerYear = monthsPerYear;
        this.weeksPerMonth = weeksPerMonth;
        this.daysPerWeek = daysPerWeek;
    }

    public CalendarDefinition(IList<int> monthLengths, int daysPerWeek)
    {
        if (monthLengths == null)
        {
            throw new ArgumentNullException(nameof(monthLengths));
        }

        this.monthsPerYear = monthLengths.Count;
        this.weeksPerMonth = DefaultDimension;
        this.daysPerWeek = daysPerWeek;
        this.monthLengths = new List<int>(monthLengths);
    }

    public bool TryValidate(out string diagnostic)
    {
        if (monthsPerYear <= 0 || weeksPerMonth <= 0 || daysPerWeek <= 0)
        {
            diagnostic = $"CalendarDefinition is invalid: MonthsPerYear={monthsPerYear}, WeeksPerMonth={weeksPerMonth}, DaysPerWeek={daysPerWeek}. All values must be greater than zero.";
            return false;
        }

        if (UsesCustomMonthLengths && monthLengths.Count != monthsPerYear)
        {
            diagnostic = $"CalendarDefinition is invalid: MonthLengths has {monthLengths.Count} entries but MonthsPerYear is {monthsPerYear}.";
            return false;
        }

        try
        {
            long daysPerYear = 0L;

            if (UsesCustomMonthLengths)
            {
                for (int i = 0; i < monthLengths.Count; i++)
                {
                    int monthLength = monthLengths[i];

                    if (monthLength <= 0)
                    {
                        diagnostic = $"CalendarDefinition is invalid: MonthLengths[{i}]={monthLength}. Every month must contain at least one day.";
                        return false;
                    }

                    daysPerYear = checked(daysPerYear + monthLength);
                }
            }
            else
            {
                long daysPerMonth = checked((long)weeksPerMonth * daysPerWeek);

                if (daysPerMonth > int.MaxValue)
                {
                    diagnostic = $"CalendarDefinition is invalid: WeeksPerMonth={weeksPerMonth} and DaysPerWeek={daysPerWeek} exceed the supported day-of-month range.";
                    return false;
                }

                daysPerYear = checked(daysPerMonth * monthsPerYear);
            }

            if (daysPerYear <= 0L)
            {
                diagnostic = "CalendarDefinition is invalid: DaysPerYear must be greater than zero.";
                return false;
            }

            diagnostic = null;
            return true;
        }
        catch (OverflowException)
        {
            diagnostic = $"CalendarDefinition is invalid: the configured dimensions overflow the supported day range. MonthsPerYear={monthsPerYear}, WeeksPerMonth={weeksPerMonth}, DaysPerWeek={daysPerWeek}.";
            return false;
        }
    }

    public int GetDaysInMonth(int month)
    {
        ValidateOrThrow();

        if (month < 1 || month > monthsPerYear)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, $"Month must be between 1 and {monthsPerYear}.");
        }

        return UsesCustomMonthLengths ? monthLengths[month - 1] : checked(weeksPerMonth * daysPerWeek);
    }

    public SimulationDate GetDate(long absoluteDay)
    {
        return new SimulationCalendar(this).GetDate(absoluteDay);
    }

    public long GetAbsoluteDay(SimulationDate date)
    {
        return new SimulationCalendar(this).GetAbsoluteDay(date);
    }

    public void ValidateOrThrow()
    {
        if (TryValidate(out string diagnostic) == false)
        {
            throw new ArgumentException(diagnostic, nameof(CalendarDefinition));
        }
    }

    internal int[] GetCustomMonthLengthsSnapshot()
    {
        return monthLengths == null ? Array.Empty<int>() : monthLengths.ToArray();
    }

    private bool TryCalculateDaysPerYear(out long daysPerYear)
    {
        daysPerYear = 0L;

        if (TryValidate(out _) == false)
        {
            return false;
        }

        if (UsesCustomMonthLengths)
        {
            for (int i = 0; i < monthLengths.Count; i++)
            {
                daysPerYear += monthLengths[i];
            }

            return true;
        }

        daysPerYear = (long)weeksPerMonth * daysPerWeek * monthsPerYear;
        return true;
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
            // This legacy resolver is retained for the existing Unity sandbox bootstrap.
            // The pure calendar service and conversion methods reject invalid definitions.
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
