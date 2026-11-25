public enum PersonAgeQueryFailure
{
    None = 0,
    InvalidPerson = 1,
    BirthDateUnknown = 2,
    BirthDateInFuture = 3,
    InvalidCalendar = 4,
    InvalidCurrentDay = 5
}

/// <summary>
/// Immutable chronological age result. Age is derived for the requested day and
/// is not stored or advanced on PersonRuntime.
/// </summary>
public sealed class PersonAgeSnapshot
{
    public long BirthAbsoluteDay { get; }
    public SimulationDate BirthDate { get; }
    public SimulationDate CurrentDate { get; }
    public long AgeInDays { get; }
    public long CompletedYears { get; }

    public PersonAgeSnapshot(
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

public enum PersonMaturityQueryFailure
{
    None = 0,
    InvalidPerson,
    BirthDateUnknown,
    BirthDateInFuture,
    InvalidCalendar,
    InvalidCurrentDay,
    InvalidMaturityAge,
    InvalidConfiguration
}

/// <summary>
/// Immutable maturity observation derived for one requested world day.
/// It is not stored on PersonRuntime.
/// </summary>
public sealed class PersonMaturitySnapshot
{
    public PersonMaturitySnapshot(
        long ageInDays,
        long completedYears,
        long maturityAgeYears,
        bool isMature)
    {
        AgeInDays = ageInDays;
        CompletedYears = completedYears;
        MaturityAgeYears = maturityAgeYears;
        IsMature = isMature;
    }

    public long AgeInDays { get; }

    public long CompletedYears { get; }

    public long MaturityAgeYears { get; }

    public bool IsMature { get; }
}
