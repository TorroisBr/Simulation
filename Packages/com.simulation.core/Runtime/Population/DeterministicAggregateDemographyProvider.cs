using System;

/// <summary>
/// Default aggregate-only policy. Annual rates are converted using the runtime's
/// immutable calendar, and fractional daily events use entity/day-scoped samples.
/// </summary>
public sealed class DeterministicAggregateDemographyProvider : IAggregateDemographyProvider
{
    private readonly SimulationCalendar calendar;
    private readonly double annualBirthRate;
    private readonly double annualDeathRate;
    private readonly IAggregateDemographySampleProvider samples;

    public DeterministicAggregateDemographyProvider(
        SimulationCalendar calendar,
        double annualBirthRate,
        double annualDeathRate,
        IAggregateDemographySampleProvider samples = null)
    {
        this.calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
        this.annualBirthRate = annualBirthRate;
        this.annualDeathRate = annualDeathRate;
        this.samples = samples ?? new DeterministicDemographicSampleProvider();
    }

    public AggregateDemographyChange GetChange(AggregateDemographyContext context)
    {
        int births = CalculateDailyEvents(
            annualBirthRate,
            calendar.DaysPerYear,
            samples.GetSample(
                context.SettlementRuntimeId,
                context.CurrentAbsoluteDay,
                AggregateDemographicOutcomeKind.Birth));
        int deaths = CalculateDailyEvents(
            annualDeathRate,
            calendar.DaysPerYear,
            samples.GetSample(
                context.SettlementRuntimeId,
                context.CurrentAbsoluteDay,
                AggregateDemographicOutcomeKind.Death));

        deaths = Math.Min(deaths, context.AggregateOnlyPopulation);
        return new AggregateDemographyChange(births, deaths);
    }

    private static int CalculateDailyEvents(
        double annualRate,
        long daysPerYear,
        double deterministicSample)
    {
        if (double.IsNaN(annualRate)
            || double.IsInfinity(annualRate)
            || annualRate < 0d
            || daysPerYear <= 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(annualRate));
        }

        if (double.IsNaN(deterministicSample)
            || double.IsInfinity(deterministicSample)
            || deterministicSample < 0d
            || deterministicSample >= 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(deterministicSample));
        }

        double dailyRate = annualRate / daysPerYear;
        if (dailyRate >= int.MaxValue)
        {
            return int.MaxValue;
        }

        int wholeEvents = (int)Math.Floor(dailyRate);
        double fractionalEvent = dailyRate - wholeEvents;
        if (deterministicSample < fractionalEvent && wholeEvents < int.MaxValue)
        {
            wholeEvents++;
        }

        return wholeEvents;
    }
}
