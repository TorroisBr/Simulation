using System;
using System.Collections.Generic;

internal interface IDailyV1SelectedDemographyBoundary
{
    bool TryApplyPersonDeath(PersonId personId, out string failure);
    /// <summary>
    /// Returns the residual represented residents from the host's full roster
    /// after subtracting the exact living PersonStore residents supplied by Core.
    /// </summary>
    IReadOnlyDictionary<string, int> BuildResidentFloorSupplement();
}

internal sealed class DailyV1SelectedDemographyDiagnostic
{
    internal bool IsError { get; }
    internal string Code { get; }
    internal string Identity { get; }
    internal string Message { get; }

    internal DailyV1SelectedDemographyDiagnostic(bool isError, string code, string identity, string message)
    {
        IsError = isError;
        Code = code ?? string.Empty;
        Identity = identity ?? string.Empty;
        Message = message ?? string.Empty;
    }
}

internal sealed class DailyV1SelectedDemographyResult
{
    private readonly List<DailyV1SelectedDemographyDiagnostic> diagnostics =
        new List<DailyV1SelectedDemographyDiagnostic>();

    internal long AbsoluteDay { get; }
    internal int NamedPersonsEvaluated { get; set; }
    internal int NamedDeathsApplied { get; set; }
    internal int AggregateSettlementsProcessed { get; set; }
    internal int AggregateBirthsApplied { get; set; }
    internal int AggregateDeathsApplied { get; set; }
    internal IReadOnlyList<DailyV1SelectedDemographyDiagnostic> Diagnostics => diagnostics.AsReadOnly();

    internal DailyV1SelectedDemographyResult(long absoluteDay)
    {
        AbsoluteDay = absoluteDay;
    }

    internal void Add(bool isError, string code, string identity, string message)
    {
        diagnostics.Add(new DailyV1SelectedDemographyDiagnostic(isError, code, identity, message));
    }
}

/// <summary>
/// Owns the selected Daily-v1 demographic ordering and aggregate population
/// writes. The host supplies only the existing person-death writer and the
/// represented-resident facts derived from that same runtime.
/// </summary>
internal static class DailyV1SelectedDemographyStage
{
    internal static DailyV1SelectedDemographyResult Advance(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        long absoluteDay,
        SimulationCalendar calendar,
        EffectiveNaturalMortalityConfiguration mortalityConfiguration,
        EffectiveAggregateDemographyConfiguration aggregateConfiguration,
        IPersonNaturalMortalitySampleProvider mortalitySamples,
        IAggregateDemographyProvider aggregateProvider,
        IDailyV1SelectedDemographyBoundary boundary)
    {
        if (graph == null || personStore == null || calendar == null || boundary == null)
            throw new ArgumentNullException("Selected demography requires its exact owner graph, people, calendar and host boundary.");
        if (graph.PersonStore != null && !ReferenceEquals(graph.PersonStore, personStore))
            throw new ArgumentException("Selected demography must use the PersonStore already bound to its owner graph.", nameof(personStore));
        if (absoluteDay < 0L) throw new ArgumentOutOfRangeException(nameof(absoluteDay));

        DailyV1SelectedDemographyResult result = new DailyV1SelectedDemographyResult(absoluteDay);
        if (mortalityConfiguration != null && mortalityConfiguration.Enabled)
            ApplyNaturalMortality(graph, personStore, absoluteDay, calendar,
                mortalityConfiguration, mortalitySamples, boundary, result);

        if (aggregateConfiguration != null && aggregateConfiguration.Enabled)
        {
            IAggregateDemographyProvider provider = aggregateProvider
                ?? new DeterministicAggregateDemographyProvider(
                    calendar,
                    aggregateConfiguration.AnnualBirthRate,
                    aggregateConfiguration.AnnualDeathRate);
            IReadOnlyDictionary<string, int> floors = BuildRepresentedResidentFloors(
                graph, personStore, boundary.BuildResidentFloorSupplement());
            ApplyAggregateDemography(graph, provider, floors, absoluteDay, result);
        }

        return result;
    }

    private static IReadOnlyDictionary<string, int> BuildRepresentedResidentFloors(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        IReadOnlyDictionary<string, int> residentFloorSupplement)
    {
        Dictionary<string, int> personResidentFloors = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (city != null && !string.IsNullOrWhiteSpace(city.RuntimeId))
                personResidentFloors[city.RuntimeId] = 0;
        }

        foreach (PersonRuntime person in personStore.Persons)
        {
            if (person == null || person.PersonId == null || person.DeathAbsoluteDay.HasValue
                || string.IsNullOrWhiteSpace(person.ResidenceSettlementRuntimeId)
                || !personResidentFloors.TryGetValue(person.ResidenceSettlementRuntimeId, out int count))
                continue;

            if (count < int.MaxValue)
                personResidentFloors[person.ResidenceSettlementRuntimeId] = count + 1;
        }

        Dictionary<string, int> floors = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                || residentFloorSupplement == null
                || !personResidentFloors.TryGetValue(city.RuntimeId, out int personFloor)
                || !residentFloorSupplement.TryGetValue(city.RuntimeId, out int supplement)
                || personFloor < 0 || supplement < 0 || personFloor > int.MaxValue - supplement)
                continue;

            floors[city.RuntimeId] = personFloor + supplement;
        }

        return floors;
    }

    private static void ApplyNaturalMortality(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        long absoluteDay,
        SimulationCalendar calendar,
        EffectiveNaturalMortalityConfiguration configuration,
        IPersonNaturalMortalitySampleProvider mortalitySamples,
        IDailyV1SelectedDemographyBoundary boundary,
        DailyV1SelectedDemographyResult result)
    {
        IPersonNaturalMortalitySampleProvider samples = mortalitySamples
            ?? new DeterministicDemographicSampleProvider();
        List<PersonRuntime> snapshot = new List<PersonRuntime>(personStore.Persons);
        snapshot.RemoveAll(person => person == null || person.PersonId == null);
        snapshot.Sort((left, right) => StringComparer.Ordinal.Compare(
            left.PersonId.Value,
            right.PersonId.Value));

        foreach (PersonRuntime person in snapshot)
        {
            if (person.DeathAbsoluteDay.HasValue) continue;
            result.NamedPersonsEvaluated++;

            double sample;
            try
            {
                sample = samples.GetSample(person.PersonId, absoluteDay);
            }
            catch (Exception exception)
            {
                result.Add(true, "NaturalMortalitySampleFailed", person.PersonId.Value, exception.Message);
                continue;
            }

            if (!PersonNaturalMortalityQuery.TryEvaluate(
                    person,
                    absoluteDay,
                    calendar,
                    configuration.AnnualProbability,
                    sample,
                    out PersonNaturalMortalityEvaluation evaluation,
                    out PersonNaturalMortalityQueryFailure evaluationFailure))
            {
                result.Add(
                    evaluationFailure != PersonNaturalMortalityQueryFailure.BirthDateUnknown,
                    "NaturalMortalityEvaluationRejected",
                    person.PersonId.Value,
                    evaluationFailure.ToString());
                continue;
            }

            if (!evaluation.ShouldDie) continue;
            if (!boundary.TryApplyPersonDeath(person.PersonId, out string deathFailure))
            {
                result.Add(true, "NaturalMortalityApplyRejected", person.PersonId.Value,
                    deathFailure ?? string.Empty);
                continue;
            }

            result.NamedDeathsApplied++;
        }
    }

    private static void ApplyAggregateDemography(
        DailyV1SelectedRuntimeOwnerGraph graph,
        IAggregateDemographyProvider provider,
        IReadOnlyDictionary<string, int> floors,
        long absoluteDay,
        DailyV1SelectedDemographyResult result)
    {
        List<DailyV1CityRuntimeOwners> cities = new List<DailyV1CityRuntimeOwners>(graph.Cities);
        cities.Sort((left, right) => StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        foreach (DailyV1CityRuntimeOwners city in cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId) || city.Population == null)
            {
                result.Add(true, "AggregatePopulationOwnerMissing", city?.RuntimeId ?? string.Empty,
                    "The selected owner graph did not provide its exact aggregate population owner.");
                continue;
            }

            if (floors == null || !floors.TryGetValue(city.RuntimeId, out int representedResidentFloor))
            {
                result.Add(true, "RepresentedResidentFloorMissing", city.RuntimeId,
                    "The world did not provide a represented-resident floor.");
                continue;
            }

            try
            {
                if (!AggregateDemographySystem.TryPropose(
                        city.Population,
                        representedResidentFloor,
                        provider,
                        absoluteDay,
                        out AggregateDemographyTransition transition,
                        out AggregateDemographyFailure proposalFailure))
                {
                    result.Add(true, "AggregateDemographyProposalRejected", city.RuntimeId,
                        proposalFailure.ToString());
                    continue;
                }

                if (!AggregateDemographySystem.TryApply(
                        city.Population,
                        representedResidentFloor,
                        transition,
                        out AggregateDemographyFailure applyFailure))
                {
                    result.Add(true, "AggregateDemographyApplyRejected", city.RuntimeId,
                        applyFailure.ToString());
                    continue;
                }

                result.AggregateSettlementsProcessed++;
                result.AggregateBirthsApplied += transition.Births;
                result.AggregateDeathsApplied += transition.Deaths;
            }
            catch (Exception exception)
            {
                result.Add(true, "AggregateDemographyProviderFailed", city.RuntimeId, exception.Message);
            }
        }
    }
}
