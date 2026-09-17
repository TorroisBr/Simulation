using System;

/// <summary>
/// Pure conversion service for the stable CalendarDefinition used by one simulation run.
/// AbsoluteDay is zero-based: day zero is Year 0 / Month 1 / Day 1.
/// </summary>
public sealed class SimulationCalendar
{
    private readonly int monthsPerYear;
    private readonly int daysPerWeek;
    private readonly int uniformDaysPerMonth;
    private readonly int[] customMonthLengths;
    private readonly long[] customMonthStarts;
    private readonly long daysPerYear;

    public SimulationCalendar(CalendarDefinition definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (definition.TryValidate(out string diagnostic) == false)
        {
            throw new ArgumentException(diagnostic, nameof(definition));
        }

        monthsPerYear = definition.MonthsPerYear;
        daysPerWeek = definition.DaysPerWeek;
        daysPerYear = definition.DaysPerYear;

        if (definition.UsesCustomMonthLengths)
        {
            customMonthLengths = definition.GetCustomMonthLengthsSnapshot();
            customMonthStarts = new long[customMonthLengths.Length + 1];

            for (int i = 0; i < customMonthLengths.Length; i++)
            {
                customMonthStarts[i + 1] = checked(customMonthStarts[i] + customMonthLengths[i]);
            }
        }
        else
        {
            uniformDaysPerMonth = checked(definition.WeeksPerMonth * definition.DaysPerWeek);
        }
    }

    public int MonthsPerYear => monthsPerYear;
    public int DaysPerWeek => daysPerWeek;
    public long DaysPerYear => daysPerYear;
    public bool UsesCustomMonthLengths => customMonthLengths != null;

    public SimulationDate GetDate(long absoluteDay)
    {
        if (absoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteDay), "AbsoluteDay cannot be negative.");
        }

        long year = absoluteDay / daysPerYear;
        long zeroBasedDayOfYear = absoluteDay % daysPerYear;
        int monthIndex = FindMonthIndex(zeroBasedDayOfYear);
        long zeroBasedDayOfMonth = zeroBasedDayOfYear - GetMonthStart(monthIndex);
        int dayOfMonth = checked((int)zeroBasedDayOfMonth + 1);
        int daysInMonth = GetDaysInMonthByIndex(monthIndex);
        int weekOfMonth = (dayOfMonth - 1) / daysPerWeek + 1;
        int dayOfWeek = (dayOfMonth - 1) % daysPerWeek + 1;

        return new SimulationDate(
            absoluteDay,
            year,
            monthIndex + 1,
            weekOfMonth,
            dayOfMonth,
            dayOfWeek,
            zeroBasedDayOfYear + 1L,
            daysInMonth,
            daysPerYear);
    }

    public long GetAbsoluteDay(SimulationDate date)
    {
        if (date.Year < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(date), "Calendar year cannot be negative.");
        }

        if (date.Month < 1 || date.Month > monthsPerYear)
        {
            throw new ArgumentException($"Month {date.Month} is invalid for a {monthsPerYear}-month calendar.", nameof(date));
        }

        int monthIndex = date.Month - 1;
        int daysInMonth = GetDaysInMonthByIndex(monthIndex);

        if (date.DayOfMonth < 1 || date.DayOfMonth > daysInMonth)
        {
            throw new ArgumentException($"Day {date.DayOfMonth} is invalid for Month {date.Month}, which has {daysInMonth} days.", nameof(date));
        }

        try
        {
            long yearStart = checked(date.Year * daysPerYear);
            long monthStart = GetMonthStart(monthIndex);
            long dateOffset = date.DayOfMonth - 1L;
            return checked(checked(yearStart + monthStart) + dateOffset);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(date), "The date exceeds the supported AbsoluteDay range.");
        }
    }

    public long ToAbsoluteDay(SimulationDate date)
    {
        return GetAbsoluteDay(date);
    }

    public int GetDaysInMonth(int month)
    {
        if (month < 1 || month > monthsPerYear)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, $"Month must be between 1 and {monthsPerYear}.");
        }

        return GetDaysInMonthByIndex(month - 1);
    }

    public long CalculateAge(SimulationDate birthDate, SimulationDate currentDate)
    {
        return CalendarAgeCalculator.CalculateAge(birthDate, currentDate, this);
    }

    private int FindMonthIndex(long zeroBasedDayOfYear)
    {
        if (customMonthLengths == null)
        {
            return checked((int)(zeroBasedDayOfYear / uniformDaysPerMonth));
        }

        int low = 0;
        int high = customMonthLengths.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;

            if (zeroBasedDayOfYear < customMonthStarts[middle])
            {
                high = middle - 1;
            }
            else if (zeroBasedDayOfYear >= customMonthStarts[middle + 1])
            {
                low = middle + 1;
            }
            else
            {
                return middle;
            }
        }

        throw new InvalidOperationException("The calendar month lookup produced no result.");
    }

    private long GetMonthStart(int monthIndex)
    {
        return customMonthStarts != null
            ? customMonthStarts[monthIndex]
            : checked((long)monthIndex * uniformDaysPerMonth);
    }

    private int GetDaysInMonthByIndex(int monthIndex)
    {
        return customMonthLengths != null
            ? customMonthLengths[monthIndex]
            : uniformDaysPerMonth;
    }
}

public static class CalendarAgeCalculator
{
    public static long CalculateAge(
        SimulationDate birthDate,
        SimulationDate currentDate,
        CalendarDefinition definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        return CalculateAge(birthDate, currentDate, new SimulationCalendar(definition));
    }

    public static long CalculateAge(
        SimulationDate birthDate,
        SimulationDate currentDate,
        SimulationCalendar calendar)
    {
        if (calendar == null)
        {
            throw new ArgumentNullException(nameof(calendar));
        }

        long birthAbsoluteDay = calendar.GetAbsoluteDay(birthDate);
        long currentAbsoluteDay = calendar.GetAbsoluteDay(currentDate);

        if (birthAbsoluteDay > currentAbsoluteDay)
        {
            throw new ArgumentException("Birth date cannot be later than the current date.", nameof(birthDate));
        }

        SimulationDate canonicalBirthDate = calendar.GetDate(birthAbsoluteDay);
        SimulationDate canonicalCurrentDate = calendar.GetDate(currentAbsoluteDay);
        long age = canonicalCurrentDate.Year - canonicalBirthDate.Year;

        if (canonicalCurrentDate.Month < canonicalBirthDate.Month
            || (canonicalCurrentDate.Month == canonicalBirthDate.Month
                && canonicalCurrentDate.DayOfMonth < canonicalBirthDate.DayOfMonth))
        {
            age--;
        }

        return age;
    }
}
