using System;
using System.Collections.Generic;

internal interface IDailyV1SelectedDemographyBoundary
{
    bool TryApplyPersonDeath(PersonId personId, out string failure);
    /// <summary>
    /// Returns represented residents from host compatibility NPCs outside the
    /// exact selected Core owner graph. PersonStore residents and selected Core
    /// NPC residence owners are counted by Core and must not be included here.
    /// </summary>
    IReadOnlyDictionary<string, int> BuildCompatibilityResidentFloorSupplement(
        IReadOnlyCollection<string> selectedNpcRuntimeIds);
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
            List<string> selectedNpcRuntimeIds = BuildSelectedNpcRuntimeIds(graph);
            IReadOnlyDictionary<string, int> floors = BuildRepresentedResidentFloors(
                graph,
                personStore,
                boundary.BuildCompatibilityResidentFloorSupplement(selectedNpcRuntimeIds));
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

        Dictionary<string, int> selectedLegacyResidentFloors = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (city != null && !string.IsNullOrWhiteSpace(city.RuntimeId))
                selectedLegacyResidentFloors[city.RuntimeId] = 0;
        }

        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || npc.Lifecycle == null || npc.Residence == null)
                throw new InvalidOperationException(
                    "Selected demography requires the exact Core NPC lifecycle and residence owners.");

            if (personStore.TryGetByMaterializedNpcRuntimeId(
                    npc.RuntimeId, out PersonRuntime person))
            {
                if (person == null || person.PersonId == null || !person.IsMaterialized
                    || !string.Equals(person.MaterializedNpcRuntimeId, npc.RuntimeId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Selected demography found an inconsistent Core PersonStore materialization binding.");

                // Person-backed NPC residence is authoritative in PersonStore
                // and is already included in personResidentFloors.
                continue;
            }

            string settlementRuntimeId = npc.Residence.SettlementRuntimeId;
            if (npc.Lifecycle.LifeState != NpcLifeState.Alive
                || string.IsNullOrWhiteSpace(settlementRuntimeId)
                || !selectedLegacyResidentFloors.TryGetValue(
                    settlementRuntimeId, out int selectedResidents))
                continue;

            if (selectedResidents == int.MaxValue)
                throw new InvalidOperationException(
                    "Selected demography exceeded the representable legacy resident count.");
            selectedLegacyResidentFloors[settlementRuntimeId] = selectedResidents + 1;
        }

        Dictionary<string, int> floors = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                || residentFloorSupplement == null
                || !personResidentFloors.TryGetValue(city.RuntimeId, out int personFloor)
                || !selectedLegacyResidentFloors.TryGetValue(city.RuntimeId, out int selectedLegacyFloor)
                || !residentFloorSupplement.TryGetValue(city.RuntimeId, out int supplement)
                || personFloor < 0 || selectedLegacyFloor < 0 || supplement < 0
                || personFloor > int.MaxValue - selectedLegacyFloor
                || personFloor + selectedLegacyFloor > int.MaxValue - supplement)
                continue;

            floors[city.RuntimeId] = personFloor + selectedLegacyFloor + supplement;
        }

        return floors;
    }

    private static List<string> BuildSelectedNpcRuntimeIds(
        DailyV1SelectedRuntimeOwnerGraph graph)
    {
        if (graph?.Npcs == null)
            throw new ArgumentNullException(nameof(graph));

        List<string> runtimeIds = new List<string>(graph.Npcs.Count);
        HashSet<string> uniqueRuntimeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DailyV1NpcRuntimeOwners npc in graph.Npcs)
        {
            if (npc == null || string.IsNullOrWhiteSpace(npc.RuntimeId)
                || !uniqueRuntimeIds.Add(npc.RuntimeId))
                throw new InvalidOperationException(
                    "Selected demography requires unique exact Core NPC runtime identities.");
            runtimeIds.Add(npc.RuntimeId);
        }

        runtimeIds.Sort(StringComparer.Ordinal);
        return runtimeIds;
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
