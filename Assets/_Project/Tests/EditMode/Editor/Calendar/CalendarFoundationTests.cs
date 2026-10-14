using System;
using NUnit.Framework;
using UnityEngine;

public sealed class CalendarFoundationTests
{


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
    public void ConversionDoesNotMutateSimulationTime()
    {
        SimulationTime time = new SimulationTime(17L);
        SimulationCalendar calendar = new SimulationCalendar(new CalendarDefinition(3, 2, 5));

        calendar.GetDate(time.AbsoluteDay);

        Assert.That(time.AbsoluteDay, Is.EqualTo(17L));
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











    private static SimulationCalendar ConventionalCalendar()
    {
        return new SimulationCalendar(new CalendarDefinition(12, 1, 30));
    }

    private static SimulationDate RawDate(long year, int month, int dayOfMonth)
    {
        return new SimulationDate(0L, year, month, 0, dayOfMonth, 0, 0L, 0L, 0L);
    }
}
