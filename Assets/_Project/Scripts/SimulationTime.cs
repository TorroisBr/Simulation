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

public struct SimulationDate : IEquatable<SimulationDate>, IComparable<SimulationDate>
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

    public bool Equals(SimulationDate other)
    {
        return AbsoluteDay == other.AbsoluteDay
            && Year == other.Year
            && Month == other.Month
            && WeekOfMonth == other.WeekOfMonth
            && DayOfMonth == other.DayOfMonth
            && DayOfWeek == other.DayOfWeek
            && DayOfYear == other.DayOfYear
            && DaysPerMonth == other.DaysPerMonth
            && DaysPerYear == other.DaysPerYear;
    }

    public override bool Equals(object obj)
    {
        return obj is SimulationDate other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = AbsoluteDay.GetHashCode();
            hash = (hash * 397) ^ Year.GetHashCode();
            hash = (hash * 397) ^ Month;
            hash = (hash * 397) ^ WeekOfMonth;
            hash = (hash * 397) ^ DayOfMonth;
            hash = (hash * 397) ^ DayOfWeek;
            hash = (hash * 397) ^ DayOfYear.GetHashCode();
            hash = (hash * 397) ^ DaysPerMonth.GetHashCode();
            hash = (hash * 397) ^ DaysPerYear.GetHashCode();
            return hash;
        }
    }

    public int CompareTo(SimulationDate other)
    {
        return AbsoluteDay.CompareTo(other.AbsoluteDay);
    }

    public static bool operator ==(SimulationDate left, SimulationDate right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SimulationDate left, SimulationDate right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"Year {Year} / Month {Month} / Day {DayOfMonth}";
    }
}
