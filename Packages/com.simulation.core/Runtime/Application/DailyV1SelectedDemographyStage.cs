using System;
using System.Collections.Generic;

internal interface IDailyV1SelectedDemographyBoundary
{
    /// <summary>
    /// Admits the exact Core-prepared death operation through the host's
    /// existing owner-section protocol, then invokes Core's commit. The host
    /// may validate census bindings and publish compatibility notifications;
    /// it does not choose or apply the death.
    /// </summary>
    bool TryApplyPersonDeath(
        DailyV1SelectedPersonDeathPlan plan,
        Func<bool> coreCommit,
        out string failure);
    /// <summary>
    /// Returns represented residents from host compatibility NPCs outside the
    /// exact selected Core owner graph. PersonStore residents and selected Core
    /// NPC residence owners are counted by Core and must not be included here.
    /// </summary>
    IReadOnlyDictionary<string, int> BuildCompatibilityResidentFloorSupplement(
        IReadOnlyCollection<string> selectedNpcRuntimeIds);
}

/// <summary>
/// Immutable expected-state witness for one selected Person death. The
/// captured object references are the exact Core owners in the admitted
/// graph; the host uses them only to bind the existing P12 census operation.
/// </summary>
internal sealed class DailyV1SelectedPersonDeathPlan
{
    internal PersonRuntime Person { get; }
    internal long AbsoluteDay { get; }
    internal DailyV1NpcRuntimeOwners MaterializedNpc { get; }
    internal DailyV1CityRuntimeOwners ResidenceCity { get; }
    internal long ExpectedPopulationRevision { get; }
    internal int ExpectedPopulation { get; }
    internal SettlementPopulationTransition PopulationTransition { get; }
    internal DailyV1ActionDefinition ExpectedAction { get; }
    internal DailyV1NpcActionPayloadOwner ExpectedPayload { get; }
    internal bool IsResident => ResidenceCity != null;

    internal DailyV1SelectedPersonDeathPlan(
        PersonRuntime person,
        long absoluteDay,
        DailyV1NpcRuntimeOwners materializedNpc,
        DailyV1CityRuntimeOwners residenceCity,
        long expectedPopulationRevision,
        int expectedPopulation,
        SettlementPopulationTransition populationTransition,
        DailyV1ActionDefinition expectedAction,
        DailyV1NpcActionPayloadOwner expectedPayload)
    {
        Person = person ?? throw new ArgumentNullException(nameof(person));
        AbsoluteDay = absoluteDay;
        MaterializedNpc = materializedNpc;
        ResidenceCity = residenceCity;
        ExpectedPopulationRevision = expectedPopulationRevision;
        ExpectedPopulation = expectedPopulation;
        PopulationTransition = populationTransition;
        ExpectedAction = expectedAction;
        ExpectedPayload = expectedPayload;
    }
}

/// <summary>
/// Core-owned selected Person-death proposal and commit. The host participates
/// only in P12 admission/notification around this operation.
/// </summary>
internal static class DailyV1SelectedPersonDeathOperation
{
    internal static bool TryPrepare(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        PersonRuntime person,
        long absoluteDay,
        out DailyV1SelectedPersonDeathPlan plan,
        out string failure)
    {
        plan = null;
        failure = "Selected Person death requires the exact Core owner graph and PersonStore.";
        if (graph == null || personStore == null || person == null
            || absoluteDay < 0L
            || !ReferenceEquals(graph.PersonStore, personStore)
            || person.PersonId == null
            || !personStore.TryGet(person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, person))
            return false;

        if (person.DeathAbsoluteDay.HasValue
            || (person.BirthAbsoluteDay.HasValue && person.BirthAbsoluteDay.Value > absoluteDay))
        {
            failure = "The selected Person is already dead or has a future birth day.";
            return false;
        }

        DailyV1NpcRuntimeOwners materializedNpc = null;
        if (!string.IsNullOrWhiteSpace(person.MaterializedNpcRuntimeId))
        {
            if (!personStore.TryGetByMaterializedNpcRuntimeId(
                    person.MaterializedNpcRuntimeId, out PersonRuntime materializedPerson)
                || !ReferenceEquals(materializedPerson, person)
                || !graph.TryGetNpcOwner(person.MaterializedNpcRuntimeId, out materializedNpc)
                || !graph.ContainsExactNpcOwner(materializedNpc)
                || materializedNpc.Lifecycle?.LifeState != NpcLifeState.Alive)
            {
                failure = "The selected Person materialization does not bind to its exact living Core NPC owner.";
                return false;
            }
        }

        DailyV1CityRuntimeOwners residenceCity = null;
        long expectedPopulationRevision = 0L;
        int expectedPopulation = 0;
        SettlementPopulationTransition populationTransition = null;
        if (!string.IsNullOrWhiteSpace(person.ResidenceSettlementRuntimeId))
        {
            int matches = 0;
            foreach (DailyV1CityRuntimeOwners candidate in graph.Cities)
            {
                if (candidate == null
                    || !string.Equals(candidate.RuntimeId,
                        person.ResidenceSettlementRuntimeId, StringComparison.Ordinal))
                    continue;
                matches++;
                residenceCity = candidate;
            }

            if (matches != 1 || residenceCity?.Population == null
                || residenceCity.Population.CurrentPopulation <= 0
                || !graph.ContainsExactCityOwner(residenceCity)
                || !SettlementPopulationSystem.TryPropose(
                    residenceCity.Population,
                    new PopulationChangeSet(0, 1, 0, 0),
                    out populationTransition,
                    out _))
            {
                failure = "The selected resident Person has no unique live Core City population owner.";
                return false;
            }

            expectedPopulationRevision = residenceCity.Population.Revision;
            expectedPopulation = residenceCity.Population.CurrentPopulation;
        }

        DailyV1ActionDefinition expectedAction = materializedNpc?.ActionSlot?.CurrentAction;
        DailyV1NpcActionPayloadOwner expectedPayload = materializedNpc?.ActionSlot?.CurrentPayload;
        if (materializedNpc != null
            && !materializedNpc.CanCommitPersonDeathAfterValidation(
                NpcInjurySeverity.None, expectedAction, expectedPayload))
        {
            failure = "The materialized Core NPC owner cannot admit its death transition.";
            return false;
        }

        plan = new DailyV1SelectedPersonDeathPlan(
            person, absoluteDay, materializedNpc, residenceCity,
            expectedPopulationRevision, expectedPopulation, populationTransition,
            expectedAction, expectedPayload);
        failure = null;
        return true;
    }

    internal static bool TryCommit(
        DailyV1SelectedRuntimeOwnerGraph graph,
        PersonStore personStore,
        DailyV1SelectedPersonDeathPlan plan,
        out string failure)
    {
        failure = "Selected Person death expected state changed before Core commit.";
        if (graph == null || personStore == null || plan == null
            || !ReferenceEquals(graph.PersonStore, personStore)
            || plan.Person.PersonId == null
            || !personStore.TryGet(plan.Person.PersonId, out PersonRuntime registeredPerson)
            || !ReferenceEquals(registeredPerson, plan.Person)
            || plan.Person.DeathAbsoluteDay.HasValue
            || (plan.Person.BirthAbsoluteDay.HasValue
                && plan.Person.BirthAbsoluteDay.Value > plan.AbsoluteDay))
            return false;

        string expectedResidenceId = plan.ResidenceCity?.RuntimeId;
        if (!string.Equals(plan.Person.ResidenceSettlementRuntimeId,
                expectedResidenceId, StringComparison.Ordinal))
            return false;

        DailyV1NpcRuntimeOwners materializedNpc = plan.MaterializedNpc;
        string expectedNpcId = materializedNpc?.RuntimeId;
        if (!string.Equals(plan.Person.MaterializedNpcRuntimeId,
                expectedNpcId, StringComparison.Ordinal)
            || (materializedNpc != null
                && (!graph.ContainsExactNpcOwner(materializedNpc)
                    || !personStore.TryGetByMaterializedNpcRuntimeId(
                        materializedNpc.RuntimeId, out PersonRuntime boundPerson)
                    || !ReferenceEquals(boundPerson, plan.Person)
                    || !materializedNpc.CanCommitPersonDeathAfterValidation(
                        NpcInjurySeverity.None, plan.ExpectedAction, plan.ExpectedPayload))))
            return false;

        if (!PersonRuntimeOwnerBridge.CanAdvanceP12LifeResidenceRevision(
                plan.Person, plan.IsResident ? 2 : 1)
            || !PersonRuntimeOwnerBridge.CanCommitP12LifeResidenceMutation(plan.Person))
        {
            failure = "The selected Person lifecycle revision or writer is unavailable.";
            return false;
        }

        if (plan.IsResident)
        {
            SettlementPopulationRuntime population = plan.ResidenceCity?.Population;
            if (population == null
                || !graph.ContainsExactCityOwner(plan.ResidenceCity)
                || population.Revision != plan.ExpectedPopulationRevision
                || population.CurrentPopulation != plan.ExpectedPopulation
                || plan.ExpectedPopulation <= 0
                || !PopulationOwnerBridge.CanAdvanceP12AggregateRevision(population)
                || plan.PopulationTransition == null
                || plan.PopulationTransition.ExpectedRevision != plan.ExpectedPopulationRevision
                || plan.PopulationTransition.PopulationBefore != plan.ExpectedPopulation)
            {
                failure = "The selected resident population owner changed or cannot advance its revision.";
                return false;
            }
        }

        // All owner identities, revisions, bindings, and the population
        // transition are checked before the first authoritative write.
        if (plan.IsResident
            && !SettlementPopulationSystem.TryApply(
                plan.ResidenceCity.Population, plan.PopulationTransition, out _))
        {
            failure = "The prepared selected resident population transition was rejected.";
            return false;
        }

        if (!PersonRuntimeOwnerBridge.RecordDeathAfterValidation(
                plan.Person, plan.AbsoluteDay))
        {
            failure = "The selected Person death write was rejected after preflight.";
            return false;
        }

        if (plan.IsResident
            && !PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(plan.Person, null))
        {
            failure = "The selected Person residence clear was rejected after preflight.";
            return false;
        }

        if (materializedNpc != null
            && !materializedNpc.TryCommitPersonDeathAfterValidation(
                NpcInjurySeverity.None,
                plan.ExpectedAction,
                plan.ExpectedPayload,
                out _))
        {
            failure = "The selected materialized NPC death write was rejected after preflight.";
            return false;
        }

        failure = null;
        return true;
    }
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
/// Owns the selected Daily-v1 demographic ordering, Person-death proposals and
/// Core owner writes. The host supplies only the existing P12 census admission
/// boundary and represented-resident facts for unselected compatibility NPCs.
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
            if (!DailyV1SelectedPersonDeathOperation.TryPrepare(
                    graph, personStore, person, absoluteDay,
                    out DailyV1SelectedPersonDeathPlan plan,
                    out string prepareFailure))
            {
                result.Add(true, "NaturalMortalityApplyRejected", person.PersonId.Value,
                    prepareFailure ?? string.Empty);
                continue;
            }

            string coreFailure = null;
            if (!boundary.TryApplyPersonDeath(
                    plan,
                    () => DailyV1SelectedPersonDeathOperation.TryCommit(
                        graph, personStore, plan, out coreFailure),
                    out string deathFailure))
            {
                result.Add(true, "NaturalMortalityApplyRejected", person.PersonId.Value,
                    deathFailure ?? coreFailure ?? string.Empty);
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
