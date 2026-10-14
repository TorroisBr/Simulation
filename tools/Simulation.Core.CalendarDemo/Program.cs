using System;
using System.Globalization;

internal static class Program
{
    private static int Main()
    {
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(new[] { 10, 20, 5 }, 1));
        long[] absoluteDays = { 0L, 10L, 30L, 35L };

        Console.WriteLine("Simulation.Core calendar capability demo; not a world or AdvanceDay demo.");
        Console.WriteLine("Semantic identity: " + calendar.SemanticIdentity);
        Console.WriteLine("Semantic version: " + calendar.SemanticVersion);
        foreach (long absoluteDay in absoluteDays)
        {
            SimulationDate date = calendar.GetDate(absoluteDay);
            Console.WriteLine("Absolute day " + absoluteDay.ToString(CultureInfo.InvariantCulture) + " => " + Format(date));
        }

        SimulationDate birthDate = calendar.GetDate(9L);
        long ageBefore = calendar.CalculateAge(birthDate, calendar.GetDate(43L));
        long ageOn = calendar.CalculateAge(birthDate, calendar.GetDate(44L));
        Console.WriteLine("Completed age before birthday: " + ageBefore.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine("Completed age on birthday: " + ageOn.ToString(CultureInfo.InvariantCulture));

        try
        {
            calendar.GetDate(-1L);
            Console.Error.WriteLine("Negative absolute day was not rejected.");
            return 1;
        }
        catch (ArgumentOutOfRangeException)
        {
            Console.WriteLine("Negative absolute day: rejected");
        }

        return 0;
    }

    private static string Format(SimulationDate date)
    {
        return "Year " + date.Year.ToString(CultureInfo.InvariantCulture)
            + " / Month " + date.Month.ToString(CultureInfo.InvariantCulture)
            + " / Day " + date.DayOfMonth.ToString(CultureInfo.InvariantCulture);
    }
}
