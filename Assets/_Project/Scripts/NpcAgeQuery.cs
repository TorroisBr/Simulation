using System;

public enum NpcAgeQueryFailure
{
    None = 0,
    InvalidNpc = 1,
    BirthDateUnknown = 2,
    BirthDateInFuture = 3,
    InvalidCalendar = 4,
    InvalidCurrentDay = 5
}

/// <summary>
/// Immutable chronological age result. For dead NPCs this remains the age at the
/// queried simulation day; it is not an age-at-death or death-history value.
/// </summary>
public sealed class NpcAgeSnapshot
{
    public long BirthAbsoluteDay { get; }
    public SimulationDate BirthDate { get; }
    public SimulationDate CurrentDate { get; }
    public long AgeInDays { get; }
    public long CompletedYears { get; }

    public NpcAgeSnapshot(
        long birthAbsoluteDay,
        SimulationDate birthDate,
        SimulationDate currentDate,
        long ageInDays,
        long completedYears)
    {
        BirthAbsoluteDay = birthAbsoluteDay;
        BirthDate = birthDate;
        CurrentDate = currentDate;
        AgeInDays = ageInDays;
        CompletedYears = completedYears;
    }
}

/// <summary>
/// Canonical, read-only age calculation over simulation time and the configured
/// calendar. No age state is stored or advanced on NpcRuntime.
/// </summary>
public static class NpcAgeQuery
{
    public static bool TryCalculate(
        NpcRuntime npc,
        SimulationTime simulationTime,
        CalendarDefinition calendarDefinition,
        out NpcAgeSnapshot age,
        out NpcAgeQueryFailure failure)
    {
        age = null;

        if (simulationTime == null)
        {
            failure = NpcAgeQueryFailure.InvalidCurrentDay;
            return false;
        }

        return TryCalculate(
            npc,
            simulationTime.AbsoluteDay,
            calendarDefinition,
            out age,
            out failure);
    }

    public static bool TryCalculate(
        NpcRuntime npc,
        SimulationTime simulationTime,
        SimulationCalendar calendar,
        out NpcAgeSnapshot age,
        out NpcAgeQueryFailure failure)
    {
        age = null;

        if (simulationTime == null)
        {
            failure = NpcAgeQueryFailure.InvalidCurrentDay;
            return false;
        }

        return TryCalculate(
            npc,
            simulationTime.AbsoluteDay,
            calendar,
            out age,
            out failure);
    }

    public static bool TryCalculate(
        NpcRuntime npc,
        long currentAbsoluteDay,
        CalendarDefinition calendarDefinition,
        out NpcAgeSnapshot age,
        out NpcAgeQueryFailure failure)
    {
        age = null;

        if (npc == null)
        {
            failure = NpcAgeQueryFailure.InvalidNpc;
            return false;
        }

        if (npc.HasKnownBirthDay == false)
        {
            failure = NpcAgeQueryFailure.BirthDateUnknown;
            return false;
        }

        if (currentAbsoluteDay < 0L)
        {
            failure = NpcAgeQueryFailure.InvalidCurrentDay;
            return false;
        }

        long birthAbsoluteDay = npc.BirthAbsoluteDay.Value;
        if (birthAbsoluteDay > currentAbsoluteDay)
        {
            failure = NpcAgeQueryFailure.BirthDateInFuture;
            return false;
        }

        if (calendarDefinition == null
            || calendarDefinition.TryValidate(out _) == false)
        {
            failure = NpcAgeQueryFailure.InvalidCalendar;
            return false;
        }

        SimulationCalendar calendar;
        try
        {
            calendar = new SimulationCalendar(calendarDefinition);
        }
        catch (ArgumentException)
        {
            failure = NpcAgeQueryFailure.InvalidCalendar;
            return false;
        }
        catch (OverflowException)
        {
            failure = NpcAgeQueryFailure.InvalidCalendar;
            return false;
        }

        return TryCalculate(
            npc,
            currentAbsoluteDay,
            calendar,
            out age,
            out failure);
    }

    public static bool TryCalculate(
        NpcRuntime npc,
        long currentAbsoluteDay,
        SimulationCalendar calendar,
        out NpcAgeSnapshot age,
        out NpcAgeQueryFailure failure)
    {
        age = null;

        if (npc == null)
        {
            failure = NpcAgeQueryFailure.InvalidNpc;
            return false;
        }

        if (npc.HasKnownBirthDay == false)
        {
            failure = NpcAgeQueryFailure.BirthDateUnknown;
            return false;
        }

        if (currentAbsoluteDay < 0L)
        {
            failure = NpcAgeQueryFailure.InvalidCurrentDay;
            return false;
        }

        if (calendar == null)
        {
            failure = NpcAgeQueryFailure.InvalidCalendar;
            return false;
        }

        long birthAbsoluteDay = npc.BirthAbsoluteDay.Value;
        if (birthAbsoluteDay > currentAbsoluteDay)
        {
            failure = NpcAgeQueryFailure.BirthDateInFuture;
            return false;
        }

        SimulationDate birthDate = calendar.GetDate(birthAbsoluteDay);
        SimulationDate currentDate = calendar.GetDate(currentAbsoluteDay);
        long completedYears = calendar.CalculateAge(birthDate, currentDate);
        age = new NpcAgeSnapshot(
            birthAbsoluteDay,
            birthDate,
            currentDate,
            currentAbsoluteDay - birthAbsoluteDay,
            completedYears);
        failure = NpcAgeQueryFailure.None;
        return true;
    }
}
