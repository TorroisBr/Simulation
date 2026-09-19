using System;
using System.Collections.Generic;

public interface IPersonNaturalMortalitySampleProvider
{
    double GetSample(PersonId personId, long currentAbsoluteDay);
}

public enum AggregateDemographicOutcomeKind
{
    Birth = 0,
    Death = 1
}

public interface IAggregateDemographySampleProvider
{
    double GetSample(
        string settlementRuntimeId,
        long currentAbsoluteDay,
        AggregateDemographicOutcomeKind outcomeKind);
}

/// <summary>
/// Stable, registration-order-independent samples for autonomous demographic work.
/// This is deliberately separate from UnityEngine.Random and from action randomness.
/// </summary>
public sealed class DeterministicDemographicSampleProvider :
    IPersonNaturalMortalitySampleProvider,
    IAggregateDemographySampleProvider
{
    public double GetSample(PersonId personId, long currentAbsoluteDay)
    {
        return GetSample(personId?.Value, currentAbsoluteDay, "person");
    }

    public double GetSample(
        string settlementRuntimeId,
        long currentAbsoluteDay,
        AggregateDemographicOutcomeKind outcomeKind)
    {
        return GetSample(
            settlementRuntimeId,
            currentAbsoluteDay,
            outcomeKind == AggregateDemographicOutcomeKind.Birth ? "birth" : "death");
    }

    private static double GetSample(string entityId, long currentAbsoluteDay, string outcome)
    {
        if (string.IsNullOrWhiteSpace(entityId) || currentAbsoluteDay < 0L)
        {
            return 0.5d;
        }

        ulong hash = 14695981039346656037UL;
        Mix(ref hash, outcome);
        Mix(ref hash, entityId);
        unchecked
        {
            hash ^= (ulong)currentAbsoluteDay;
            hash *= 1099511628211UL;
            hash ^= (ulong)currentAbsoluteDay >> 32;
            hash *= 1099511628211UL;
        }

        // Keep the result in [0, 1), exactly representable as a 53-bit fraction.
        return (hash >> 11) * (1d / 9007199254740992d);
    }

    private static void Mix(ref ulong hash, string value)
    {
        foreach (char character in value ?? string.Empty)
        {
            unchecked
            {
                hash ^= character;
                hash *= 1099511628211UL;
            }
        }

        unchecked
        {
            hash ^= 0xffUL;
            hash *= 1099511628211UL;
        }
    }
}

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

public enum DailyDemographyDiagnosticSeverity
{
    Warning = 0,
    Error = 1
}

public sealed class DailyDemographyDiagnostic
{
    public DailyDemographyDiagnosticSeverity Severity { get; }
    public string Code { get; }
    public string Identity { get; }
    public string Message { get; }

    public DailyDemographyDiagnostic(
        DailyDemographyDiagnosticSeverity severity,
        string code,
        string identity,
        string message)
    {
        Severity = severity;
        Code = code ?? string.Empty;
        Identity = identity ?? string.Empty;
        Message = message ?? string.Empty;
    }
}

public sealed class DailyDemographyReport
{
    private readonly List<DailyDemographyDiagnostic> diagnostics =
        new List<DailyDemographyDiagnostic>();

    public long AbsoluteDay { get; }
    public int NamedPersonsEvaluated { get; internal set; }
    public int NamedDeathsApplied { get; internal set; }
    public int AggregateSettlementsProcessed { get; internal set; }
    public int AggregateBirthsApplied { get; internal set; }
    public int AggregateDeathsApplied { get; internal set; }
    public IReadOnlyList<DailyDemographyDiagnostic> Diagnostics => diagnostics.AsReadOnly();
    public bool HasErrors
    {
        get
        {
            foreach (DailyDemographyDiagnostic diagnostic in diagnostics)
            {
                if (diagnostic != null
                    && diagnostic.Severity == DailyDemographyDiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool IsValid => HasErrors == false;

    internal DailyDemographyReport(long absoluteDay)
    {
        AbsoluteDay = absoluteDay;
    }

    internal void Add(
        DailyDemographyDiagnosticSeverity severity,
        string code,
        string identity,
        string message)
    {
        diagnostics.Add(new DailyDemographyDiagnostic(severity, code, identity, message));
    }
}

/// <summary>
/// The one daily demographic phase owned by SimulationRuntime. It evaluates named
/// mortality from a stable Person snapshot, recalculates floors at the world boundary,
/// and then runs aggregate-only transitions in stable settlement order.
/// </summary>
public static class DailyDemographicSystem
{
    public static DailyDemographyReport Advance(
        SimulationRuntime world,
        IPersonNaturalMortalitySampleProvider mortalitySamples,
        IAggregateDemographyProvider aggregateProvider)
    {
        if (world == null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        DailyDemographyReport report = new DailyDemographyReport(world.CurrentDay);
        EffectiveNaturalMortalityConfiguration mortality = world.Configuration.NaturalMortality;
        if (mortality != null && mortality.Enabled)
        {
            ApplyNaturalMortality(world, mortality, mortalitySamples, report);
        }

        EffectiveAggregateDemographyConfiguration aggregate = world.Configuration.AggregateDemography;
        if ((aggregate != null && aggregate.Enabled) || aggregateProvider != null)
        {
            IAggregateDemographyProvider provider = aggregateProvider
                ?? new DeterministicAggregateDemographyProvider(
                    world.Calendar,
                    aggregate.AnnualBirthRate,
                    aggregate.AnnualDeathRate);
            ApplyAggregateDemography(world, provider, report);
        }

        return report;
    }

    private static void ApplyNaturalMortality(
        SimulationRuntime world,
        EffectiveNaturalMortalityConfiguration configuration,
        IPersonNaturalMortalitySampleProvider mortalitySamples,
        DailyDemographyReport report)
    {
        IPersonNaturalMortalitySampleProvider samples = mortalitySamples
            ?? new DeterministicDemographicSampleProvider();
        List<PersonRuntime> snapshot = new List<PersonRuntime>(world.PersonStore.Persons);
        snapshot.RemoveAll(person => person == null || person.PersonId == null);
        snapshot.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.PersonId.Value,
            right.PersonId.Value));

        foreach (PersonRuntime person in snapshot)
        {
            if (person.DeathAbsoluteDay.HasValue)
            {
                continue;
            }

            report.NamedPersonsEvaluated++;
            double sample;
            try
            {
                sample = samples.GetSample(person.PersonId, world.CurrentDay);
            }
            catch (Exception exception)
            {
                report.Add(
                    DailyDemographyDiagnosticSeverity.Error,
                    "NaturalMortalitySampleFailed",
                    person.PersonId.Value,
                    exception.Message);
                continue;
            }

            if (PersonNaturalMortalityQuery.TryEvaluate(
                    person,
                    world.CurrentDay,
                    world.Calendar,
                    configuration.AnnualProbability,
                    sample,
                    out PersonNaturalMortalityEvaluation evaluation,
                    out PersonNaturalMortalityQueryFailure evaluationFailure) == false)
            {
                DailyDemographyDiagnosticSeverity severity = evaluationFailure == PersonNaturalMortalityQueryFailure.BirthDateUnknown
                    ? DailyDemographyDiagnosticSeverity.Warning
                    : DailyDemographyDiagnosticSeverity.Error;
                report.Add(
                    severity,
                    "NaturalMortalityEvaluationRejected",
                    person.PersonId.Value,
                    evaluationFailure.ToString());
                continue;
            }

            if (evaluation.ShouldDie == false)
            {
                continue;
            }

            if (world.TryApplyPersonDeath(
                    person.PersonId,
                    out _,
                    out PersonDeathLifecycleFailure deathFailure) == false)
            {
                report.Add(
                    DailyDemographyDiagnosticSeverity.Error,
                    "NaturalMortalityApplyRejected",
                    person.PersonId.Value,
                    deathFailure.ToString());
                continue;
            }

            report.NamedDeathsApplied++;
        }
    }

    private static void ApplyAggregateDemography(
        SimulationRuntime world,
        IAggregateDemographyProvider provider,
        DailyDemographyReport report)
    {
        List<CityRuntime> settlements = new List<CityRuntime>(world.Cities);
        settlements.RemoveAll(city => city == null || string.IsNullOrWhiteSpace(city.RuntimeId));
        settlements.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        foreach (CityRuntime settlement in settlements)
        {
            SettlementPopulationPresenceSummary presence =
                SettlementPopulationPresenceQuery.BuildSummary(
                    settlement,
                    world.NpcRuntimes,
                    world.PersonStore.Persons);
            int representedResidentFloor = presence?.RepresentedResidentCount ?? 0;

            try
            {
                if (AggregateDemographySystem.TryPropose(
                        settlement.Population,
                        representedResidentFloor,
                        provider,
                        world.CurrentDay,
                        out AggregateDemographyTransition transition,
                        out AggregateDemographyFailure proposalFailure) == false)
                {
                    report.Add(
                        DailyDemographyDiagnosticSeverity.Error,
                        "AggregateDemographyProposalRejected",
                        settlement.RuntimeId,
                        proposalFailure.ToString());
                    continue;
                }

                if (AggregateDemographySystem.TryApply(
                        settlement.Population,
                        representedResidentFloor,
                        transition,
                        out AggregateDemographyFailure applyFailure) == false)
                {
                    report.Add(
                        DailyDemographyDiagnosticSeverity.Error,
                        "AggregateDemographyApplyRejected",
                        settlement.RuntimeId,
                        applyFailure.ToString());
                    continue;
                }

                report.AggregateSettlementsProcessed++;
                report.AggregateBirthsApplied += transition.Births;
                report.AggregateDeathsApplied += transition.Deaths;
            }
            catch (Exception exception)
            {
                report.Add(
                    DailyDemographyDiagnosticSeverity.Error,
                    "AggregateDemographyProviderFailed",
                    settlement.RuntimeId,
                    exception.Message);
            }
        }
    }
}
