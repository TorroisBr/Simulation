using System;
using System.Collections.Generic;

/// <summary>
/// World-boundary snapshot of living represented residents. The aggregate
/// population system receives these values explicitly and never discovers them.
/// </summary>
public sealed class RepresentedResidentFloorSnapshot
{
    private readonly Dictionary<string, int> floors;

    internal RepresentedResidentFloorSnapshot(Dictionary<string, int> floors)
    {
        this.floors = floors ?? new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public bool TryGetFloor(string settlementRuntimeId, out int floor)
    {
        floor = 0;
        return string.IsNullOrWhiteSpace(settlementRuntimeId) == false
            && floors.TryGetValue(settlementRuntimeId, out floor);
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

    internal DailyDemographyReport Copy()
    {
        DailyDemographyReport copy = new DailyDemographyReport(AbsoluteDay)
        {
            NamedPersonsEvaluated = NamedPersonsEvaluated,
            NamedDeathsApplied = NamedDeathsApplied,
            AggregateSettlementsProcessed = AggregateSettlementsProcessed,
            AggregateBirthsApplied = AggregateBirthsApplied,
            AggregateDeathsApplied = AggregateDeathsApplied
        };
        foreach (DailyDemographyDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic != null)
            {
                copy.Add(diagnostic.Severity, diagnostic.Code, diagnostic.Identity, diagnostic.Message);
            }
        }

        return copy;
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
        if (aggregate != null && aggregate.Enabled)
        {
            IAggregateDemographyProvider provider = aggregateProvider
                ?? new DeterministicAggregateDemographyProvider(
                    world.Calendar,
                    aggregate.AnnualBirthRate,
                    aggregate.AnnualDeathRate);
            ApplyAggregateDemography(
                world,
                provider,
                world.BuildRepresentedResidentFloorSnapshot(),
                report);
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
        RepresentedResidentFloorSnapshot floors,
        DailyDemographyReport report)
    {
        List<CityRuntime> settlements = new List<CityRuntime>(world.Cities);
        settlements.RemoveAll(city => city == null || string.IsNullOrWhiteSpace(city.RuntimeId));
        settlements.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        foreach (CityRuntime settlement in settlements)
        {
            if (floors == null
                || floors.TryGetFloor(settlement.RuntimeId, out int representedResidentFloor) == false)
            {
                report.Add(
                    DailyDemographyDiagnosticSeverity.Error,
                    "RepresentedResidentFloorMissing",
                    settlement.RuntimeId,
                    "The world did not provide a represented-resident floor.");
                continue;
            }

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
