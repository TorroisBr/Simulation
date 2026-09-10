using System;

[Serializable]
public sealed class SimulationTime
{
    private long absoluteDay;

    public long AbsoluteDay => absoluteDay;

    public SimulationTime()
        : this(0L)
    {
    }

    public SimulationTime(long absoluteDay)
    {
        if (absoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteDay), "AbsoluteDay cannot be negative.");
        }

        this.absoluteDay = absoluteDay;
    }

    public void AdvanceDay()
    {
        if (absoluteDay == long.MaxValue)
        {
            throw new InvalidOperationException("SimulationTime cannot advance beyond the maximum AbsoluteDay.");
        }

        absoluteDay++;
    }
}

public struct SimulationDate
{
    public long AbsoluteDay { get; }
    public long Year { get; }
    public int Month { get; }
    public int WeekOfMonth { get; }
    public int DayOfMonth { get; }
    public int DayOfWeek { get; }
    public long DayOfYear { get; }
    public long DaysPerMonth { get; }
    public long DaysPerYear { get; }

    public SimulationDate(
        long absoluteDay,
        long year,
        int month,
        int weekOfMonth,
        int dayOfMonth,
        int dayOfWeek,
        long dayOfYear,
        long daysPerMonth,
        long daysPerYear)
    {
        AbsoluteDay = absoluteDay;
        Year = year;
        Month = month;
        WeekOfMonth = weekOfMonth;
        DayOfMonth = dayOfMonth;
        DayOfWeek = dayOfWeek;
        DayOfYear = dayOfYear;
        DaysPerMonth = daysPerMonth;
        DaysPerYear = daysPerYear;
    }
}
