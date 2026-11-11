using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1SelectedDemographyStageTests
{
    [Test]
    public void AggregatePopulationWritesUseExactSelectedCityOwnersInStableIdentityOrder()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph();
        PersonStore people = new PersonStore();
        RecordingBoundary boundary = new RecordingBoundary(graph.Cities);
        RecordingAggregateProvider provider = new RecordingAggregateProvider(boundary.Events);
        int[] initialPopulations = graph.Cities.Select(city => city.Population.CurrentPopulation).ToArray();

        DailyV1SelectedDemographyResult result = DailyV1SelectedDemographyStage.Advance(
            graph,
            people,
            1L,
            new SimulationCalendar(new CalendarDefinition(2, 1, 2)),
            new EffectiveNaturalMortalityConfiguration(),
            new EffectiveAggregateDemographyConfiguration(true, 1d, 0d),
            null,
            provider,
            boundary);

        Assert.That(graph.Cities.Count, Is.GreaterThan(0));
        Assert.That(result.AggregateSettlementsProcessed, Is.EqualTo(graph.Cities.Count));
        Assert.That(result.AggregateBirthsApplied, Is.EqualTo(graph.Cities.Count));
        Assert.That(result.AggregateDeathsApplied, Is.Zero);
        string[] expectedCityOrder = graph.Cities.Select(city => city.RuntimeId)
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();
        Assert.That(provider.Contexts.Select(context => context.SettlementRuntimeId),
            Is.EqualTo(expectedCityOrder));
        for (int index = 0; index < graph.Cities.Count; index++)
        {
            Assert.That(graph.Cities[index].Population.CurrentPopulation,
                Is.EqualTo(initialPopulations[index] + 1));
            Assert.That(graph.Cities[index].Population.Revision, Is.EqualTo(1L));
        }
        Assert.That(boundary.Events.First(), Is.EqualTo("floors"));
    }

    [Test]
    public void NamedMortalityUsesPersonStoreBeforeFloorSnapshotAndAggregatePolicy()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph();
        PersonStore people = new PersonStore();
        PersonId personId = new PersonId("daily-demography-person");
        Assert.That(people.TryRegister(new PersonRuntime(personId, 0L), out PersonStoreFailure failure),
            Is.True, failure.ToString());
        Assert.That(graph.TryBindPersonStore(people), Is.True);

        RecordingBoundary boundary = new RecordingBoundary(graph.Cities);
        boundary.DeathToApply = personId;
        RecordingAggregateProvider provider = new RecordingAggregateProvider(boundary.Events);

        DailyV1SelectedDemographyResult result = DailyV1SelectedDemographyStage.Advance(
            graph,
            people,
            1L,
            new SimulationCalendar(new CalendarDefinition(2, 1, 2)),
            new EffectiveNaturalMortalityConfiguration(true, 1d),
            new EffectiveAggregateDemographyConfiguration(true, 1d, 0d),
            new CertainMortalitySamples(),
            provider,
            boundary);

        Assert.That(result.NamedPersonsEvaluated, Is.EqualTo(1));
        Assert.That(result.NamedDeathsApplied, Is.EqualTo(1));
        Assert.That(boundary.DeathSectionIds,
            Is.EqualTo(new[] { DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(personId) }));
        Assert.That(result.AggregateBirthsApplied, Is.EqualTo(graph.Cities.Count));
        Assert.That(boundary.Events[0], Is.EqualTo("death:" + personId.Value));
        Assert.That(boundary.Events[1], Is.EqualTo("floors"));
        Assert.That(boundary.Events[2], Does.StartWith("policy:"));
    }

    [Test]
    public void SelectedResidentDeathCommitsPersonPopulationAndMaterializedNpcOwnersInCore()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph();
        PersonStore people = new PersonStore();
        DailyV1CityRuntimeOwners city = graph.Cities[0];
        DailyV1NpcRuntimeOwners npc = graph.Npcs[3];
        PersonRuntime person = new PersonRuntime(new PersonId("core-owned-resident-death"), 0L);
        Assert.That(people.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            person, city.RuntimeId), Is.True);
        Assert.That(people.TryBindMaterializedNpc(
            person.PersonId, npc.RuntimeId, out PersonStoreFailure bindFailure),
            Is.True, bindFailure.ToString());
        Assert.That(graph.TryBindPersonStore(people), Is.True);

        long populationRevision = city.Population.Revision;
        int populationBefore = city.Population.CurrentPopulation;
        long personRevision = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
        RecordingBoundary boundary = new RecordingBoundary(graph.Cities)
        {
            DeathToApply = person.PersonId
        };

        DailyV1SelectedDemographyResult result = DailyV1SelectedDemographyStage.Advance(
            graph, people, 1L,
            new SimulationCalendar(new CalendarDefinition(2, 1, 2)),
            new EffectiveNaturalMortalityConfiguration(true, 1d),
            new EffectiveAggregateDemographyConfiguration(),
            new CertainMortalitySamples(), null, boundary);

        Assert.That(result.NamedDeathsApplied, Is.EqualTo(1));
        Assert.That(boundary.DeathSectionIds,
            Is.EquivalentTo(new[]
            {
                DailyV1SelectedOwnerSectionCensusContract.NpcLifeResidenceSectionIdFor(person.PersonId),
                DailyV1SelectedOwnerSectionCensusContract.NpcLifeStatePrefix + npc.RuntimeId,
                DailyV1SelectedOwnerSectionCensusContract.CityPopulationAggregateSectionPrefix
                    + P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(city.RuntimeId)
            }));
        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(1L));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person),
            Is.EqualTo(personRevision + 2L));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore - 1));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevision + 1L));
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Dead));
        Assert.That(npc.Lifecycle.LifeStateRevision, Is.EqualTo(1L));
        Assert.That(npc.ActionSlot.Revision, Is.Zero);
    }

    [Test]
    public void SelectedNpcOnlyResidentDeathCommitsCoreLifecycleResidenceAndPopulationOwners()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph();
        PersonStore people = new PersonStore();
        Assert.That(graph.TryBindPersonStore(people), Is.True);
        DailyV1CityRuntimeOwners city = graph.Cities[0];
        DailyV1NpcRuntimeOwners npc = graph.Npcs[3];
        Assert.That(npc.Residence.TryInstallExact(city.RuntimeId, 0L), Is.True);

        int populationBefore = city.Population.CurrentPopulation;
        long populationRevisionBefore = city.Population.Revision;
        long lifeRevisionBefore = npc.Lifecycle.LifeStateRevision;
        long residenceRevisionBefore = npc.Residence.Revision;
        long actionRevisionBefore = npc.ActionSlot.Revision;
        Assert.That(populationBefore, Is.GreaterThan(0));

        Assert.That(DailyV1SelectedNpcResidentDeathOperation.TryPrepare(
            graph, npc, city, null,
            out DailyV1SelectedNpcResidentDeathPlan plan,
            out string prepareFailure), Is.True, prepareFailure);

        int admissionCalls = 0;
        int notificationCalls = 0;
        int populationAtNotification = -1;
        long populationRevisionAtNotification = -1L;
        Assert.That(DailyV1SelectedNpcResidentDeathOperation.TryCommit(
            graph,
            plan,
            null,
            () => { admissionCalls++; return true; },
            () =>
            {
                notificationCalls++;
                populationAtNotification = city.Population.CurrentPopulation;
                populationRevisionAtNotification = city.Population.Revision;
            },
            out string commitFailure), Is.True, commitFailure);

        Assert.That(admissionCalls, Is.EqualTo(1));
        Assert.That(notificationCalls, Is.EqualTo(1));
        Assert.That(populationAtNotification, Is.EqualTo(populationBefore - 1));
        Assert.That(populationRevisionAtNotification, Is.EqualTo(populationRevisionBefore + 1L));
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Dead));
        Assert.That(npc.Lifecycle.LifeStateRevision, Is.EqualTo(lifeRevisionBefore + 1L));
        Assert.That(npc.Residence.SettlementRuntimeId, Is.Null);
        Assert.That(npc.Residence.Revision, Is.EqualTo(residenceRevisionBefore + 1L));
        Assert.That(npc.ActionSlot.CurrentAction, Is.Null);
        Assert.That(npc.ActionSlot.CurrentPayload, Is.Null);
        Assert.That(npc.ActionSlot.Revision, Is.EqualTo(actionRevisionBefore));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore - 1));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevisionBefore + 1L));
    }

    [Test]
    public void SelectedNpcOnlyResidentDeathRejectsPersistedPersonIdAliasBeforeCensusOrOwnerWrite()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph();
        PersonStore people = new PersonStore();
        DailyV1CityRuntimeOwners city = graph.Cities[0];
        PersonRuntime person = new PersonRuntime(
            new PersonId("selected-npc-only-resident-death-person-alias"), 0L);
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            person, city.RuntimeId), Is.True);
        Assert.That(people.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(graph.TryBindPersonStore(people), Is.True);

        DailyV1NpcRuntimeOwners npc = graph.Npcs[3];
        Assert.That(npc.Residence.TryInstallExact(city.RuntimeId, 0L), Is.True);
        int populationBefore = city.Population.CurrentPopulation;
        long populationRevisionBefore = city.Population.Revision;

        Assert.That(DailyV1SelectedNpcResidentDeathOperation.TryPrepare(
            graph, npc, city, person.PersonId,
            out DailyV1SelectedNpcResidentDeathPlan plan,
            out string failure), Is.False);

        Assert.That(plan, Is.Null);
        Assert.That(failure, Does.Contain("PersonId"));
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(npc.Lifecycle.LifeStateRevision, Is.Zero);
        Assert.That(npc.Residence.SettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(npc.Residence.Revision, Is.Zero);
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevisionBefore));
    }

    [Test]
    public void SelectedNpcOnlyResidentDeathRejectsStaleResidenceBeforeAnyOwnerWrite()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph();
        PersonStore people = new PersonStore();
        Assert.That(graph.TryBindPersonStore(people), Is.True);
        DailyV1CityRuntimeOwners city = graph.Cities[0];
        DailyV1NpcRuntimeOwners npc = graph.Npcs[3];
        Assert.That(npc.Residence.TryInstallExact(city.RuntimeId, 0L), Is.True);

        Assert.That(DailyV1SelectedNpcResidentDeathOperation.TryPrepare(
            graph, npc, city, null,
            out DailyV1SelectedNpcResidentDeathPlan plan,
            out string prepareFailure), Is.True, prepareFailure);

        int populationBefore = city.Population.CurrentPopulation;
        long populationRevisionBefore = city.Population.Revision;
        Assert.That(npc.Residence.TryInstallExact("city/stale-residence", 1L), Is.True);
        int admissionCalls = 0;
        int notificationCalls = 0;

        Assert.That(DailyV1SelectedNpcResidentDeathOperation.TryCommit(
            graph,
            plan,
            null,
            () => { admissionCalls++; return true; },
            () => notificationCalls++,
            out _), Is.False);

        Assert.That(admissionCalls, Is.Zero);
        Assert.That(notificationCalls, Is.Zero);
        Assert.That(npc.Lifecycle.LifeState, Is.EqualTo(NpcLifeState.Alive));
        Assert.That(npc.Lifecycle.LifeStateRevision, Is.Zero);
        Assert.That(npc.Residence.SettlementRuntimeId, Is.EqualTo("city/stale-residence"));
        Assert.That(npc.Residence.Revision, Is.EqualTo(1L));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevisionBefore));
    }

    [Test]
    public void SelectedDeathRejectsStalePopulationBeforeMutatingPersonOrNpcOwners()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph();
        PersonStore people = new PersonStore();
        DailyV1CityRuntimeOwners city = graph.Cities[0];
        PersonRuntime person = new PersonRuntime(new PersonId("stale-core-death"), 0L);
        Assert.That(people.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            person, city.RuntimeId), Is.True);
        Assert.That(graph.TryBindPersonStore(people), Is.True);
        Assert.That(DailyV1SelectedPersonDeathOperation.TryPrepare(
            graph, people, person, 1L,
            out DailyV1SelectedPersonDeathPlan plan, out string prepareFailure),
            Is.True, prepareFailure);

        Assert.That(SettlementPopulationSystem.TryPropose(
            city.Population, new PopulationChangeSet(1, 0, 0, 0),
            out SettlementPopulationTransition competingTransition,
            out PopulationTransitionFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(SettlementPopulationSystem.TryApply(
            city.Population, competingTransition, out PopulationTransitionFailure applyFailure),
            Is.True, applyFailure.ToString());
        long personRevision = PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person);
        int populationAfterCompetingWrite = city.Population.CurrentPopulation;
        long populationRevisionAfterCompetingWrite = city.Population.Revision;

        Assert.That(DailyV1SelectedPersonDeathOperation.TryCommit(
            graph, people, plan, out _), Is.False);
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(PersonRuntimeOwnerBridge.GetLifeResidenceRevision(person), Is.EqualTo(personRevision));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationAfterCompetingWrite));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevisionAfterCompetingWrite));
    }

    [Test]
    public void RepresentedResidentFloorCombinesPersonRowsSelectedCoreNpcOwnersAndHostRemainder()
    {
        DailyV1SelectedRuntimeOwnerGraph graph = CreateGraph();
        PersonStore people = new PersonStore();
        DailyV1CityRuntimeOwners residentCity = graph.Cities[0];
        DailyV1CityRuntimeOwners otherCity = graph.Cities[1];

        PersonRuntime livingResident = new PersonRuntime(new PersonId("core-resident"), 0L);
        Assert.That(people.TryRegister(livingResident, out PersonStoreFailure livingFailure),
            Is.True, livingFailure.ToString());
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            livingResident, residentCity.RuntimeId), Is.True);

        PersonRuntime materializedResident = new PersonRuntime(new PersonId("materialized-core-resident"), 0L);
        Assert.That(people.TryRegister(materializedResident, out PersonStoreFailure materializedFailure),
            Is.True, materializedFailure.ToString());
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            materializedResident, residentCity.RuntimeId), Is.True);
        DailyV1NpcRuntimeOwners personBackedNpc = graph.Npcs[3];
        Assert.That(people.TryBindMaterializedNpc(
            materializedResident.PersonId, personBackedNpc.RuntimeId, out PersonStoreFailure bindFailure),
            Is.True, bindFailure.ToString());

        PersonRuntime deadResident = new PersonRuntime(new PersonId("dead-resident"), 0L, 1L);
        Assert.That(people.TryRegister(deadResident, out PersonStoreFailure deadFailure),
            Is.True, deadFailure.ToString());
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            deadResident, residentCity.RuntimeId), Is.True);

        PersonRuntime outsideResident = new PersonRuntime(new PersonId("outside-resident"), 0L);
        Assert.That(people.TryRegister(outsideResident, out PersonStoreFailure outsideFailure),
            Is.True, outsideFailure.ToString());
        Assert.That(PersonRuntimeOwnerBridge.TrySetResidenceSettlementRuntimeId(
            outsideResident, "not-in-selected-city-graph"), Is.True);

        Assert.That(graph.TryBindPersonStore(people), Is.True,
            "The selected owner graph must bind the exact PersonStore before demography executes.");

        DailyV1NpcRuntimeOwners selectedLegacyResident = graph.Npcs[0];
        Assert.That(selectedLegacyResident.Residence.TryInstallExact(residentCity.RuntimeId, 0L), Is.True);
        DailyV1NpcRuntimeOwners deadLegacyResident = graph.Npcs[1];
        Assert.That(deadLegacyResident.Residence.TryInstallExact(residentCity.RuntimeId, 0L), Is.True);
        Assert.That(deadLegacyResident.Lifecycle.TrySetLifeState(NpcLifeState.Dead), Is.True);
        DailyV1NpcRuntimeOwners otherCityLegacyResident = graph.Npcs[2];
        Assert.That(otherCityLegacyResident.Residence.TryInstallExact(otherCity.RuntimeId, 0L), Is.True);

        RecordingBoundary boundary = new RecordingBoundary(graph.Cities, residentCity.RuntimeId, 1);
        RecordingAggregateProvider provider = new RecordingAggregateProvider(boundary.Events);

        DailyV1SelectedDemographyStage.Advance(
            graph,
            people,
            2L,
            new SimulationCalendar(new CalendarDefinition(2, 1, 2)),
            new EffectiveNaturalMortalityConfiguration(),
            new EffectiveAggregateDemographyConfiguration(true, 0d, 0d),
            null,
            provider,
            boundary);

        Assert.That(provider.Contexts, Has.Count.EqualTo(graph.Cities.Count));
        Assert.That(provider.Contexts.Single(context => context.SettlementRuntimeId == residentCity.RuntimeId)
            .RepresentedResidentFloor, Is.EqualTo(4),
            "Two living PersonStore residents, one selected Core legacy NPC, and one host compatibility resident count once each.");
        Assert.That(provider.Contexts.Single(context => context.SettlementRuntimeId == otherCity.RuntimeId)
            .RepresentedResidentFloor, Is.EqualTo(1),
            "The living selected Core NPC resident is counted for its own City; the dead resident and outside Person are excluded.");
        Assert.That(boundary.SelectedNpcRuntimeIds,
            Is.EqualTo(graph.Npcs.Select(npc => npc.RuntimeId).OrderBy(id => id, StringComparer.Ordinal)));
    }

    private static DailyV1SelectedRuntimeOwnerGraph CreateGraph()
    {
        DailyV1EffectiveDefinitions definitions = DailyV1CapturedSelectedContentFixture.CreateDefinitions();
        DailyV1SelectedRuntimeOwnerGraphBuilder builder = new DailyV1SelectedRuntimeOwnerGraphBuilder(definitions);
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        Assert.That(builder.TryCreateConfiguredCities(allocator, _ => true), Is.True);
        Assert.That(builder.TryCreateConfiguredNpcs(allocator,
            cityDefinitionId => builder.ResolveUniqueStartingCity(cityDefinitionId)?.RuntimeId), Is.True);
        Assert.That(builder.TryBuild(out DailyV1SelectedRuntimeOwnerGraph graph), Is.True);
        return graph;
    }

    private sealed class RecordingBoundary : IDailyV1SelectedDemographyBoundary
    {
        private readonly IReadOnlyDictionary<string, int> floorSupplements;
        internal readonly List<string> Events = new List<string>();
        internal IReadOnlyList<string> SelectedNpcRuntimeIds { get; private set; }
        internal PersonId DeathToApply;
        internal IReadOnlyList<string> DeathSectionIds { get; private set; }

        internal RecordingBoundary(
            IReadOnlyList<DailyV1CityRuntimeOwners> cities,
            int residentFloorSupplement = 0)
        {
            Dictionary<string, int> values = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (DailyV1CityRuntimeOwners city in cities)
                values.Add(city.RuntimeId, residentFloorSupplement);
            floorSupplements = values;
        }

        internal RecordingBoundary(
            IReadOnlyList<DailyV1CityRuntimeOwners> cities,
            string supplementalCityRuntimeId,
            int residentFloorSupplement)
        {
            Dictionary<string, int> values = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (DailyV1CityRuntimeOwners city in cities)
                values.Add(city.RuntimeId, string.Equals(
                    city.RuntimeId, supplementalCityRuntimeId, StringComparison.Ordinal)
                    ? residentFloorSupplement
                    : 0);
            floorSupplements = values;
        }

        public bool TryValidatePersonDeathProjection(
            DailyV1SelectedPersonDeathPlan plan,
            out bool currentActionChanged,
            out string failure)
        {
            currentActionChanged = plan?.ExpectedAction != null;
            failure = plan == null ? "Missing death plan." : null;
            return plan != null;
        }

        public bool TryBeginPersonDeathOperation(
            DailyV1SelectedPersonDeathPlan plan,
            bool currentActionChanged,
            string operationId,
            IReadOnlyList<string> sectionIds,
            Func<bool> localRevisionCapacity,
            out IDailyV1SelectedPersonDeathP12Scope scope)
        {
            scope = null;
            Events.Add("death:" + plan.Person.PersonId.Value);
            DeathSectionIds = sectionIds?.ToArray();
            if (DeathToApply == null || !DeathToApply.Equals(plan.Person.PersonId))
            {
                return false;
            }
            if (localRevisionCapacity == null || !localRevisionCapacity())
            {
                return false;
            }

            scope = new SuccessfulPersonDeathScope();
            return true;
        }

        public bool TryPublishPersonDeathProjection(
            DailyV1SelectedPersonDeathPlan plan,
            bool currentActionChanged) => true;

        public void FaultPersonDeathRuntime() { }

        public void AdvancePersonDeathWorldRevision() { }

        private sealed class SuccessfulPersonDeathScope : IDailyV1SelectedPersonDeathP12Scope
        {
            public void Complete(bool operationSucceeded) { }
            public void Dispose() { }
        }

        public IReadOnlyDictionary<string, int> BuildCompatibilityResidentFloorSupplement(
            IReadOnlyCollection<string> selectedNpcRuntimeIds)
        {
            Events.Add("floors");
            SelectedNpcRuntimeIds = selectedNpcRuntimeIds.ToArray();
            return floorSupplements;
        }
    }

    private sealed class RecordingAggregateProvider : IAggregateDemographyProvider
    {
        private readonly List<string> events;
        internal readonly List<AggregateDemographyContext> Contexts =
            new List<AggregateDemographyContext>();

        internal RecordingAggregateProvider(List<string> events)
        {
            this.events = events;
        }

        public AggregateDemographyChange GetChange(AggregateDemographyContext context)
        {
            Contexts.Add(context);
            events.Add("policy:" + context.SettlementRuntimeId);
            return new AggregateDemographyChange(1, 0);
        }
    }

    private sealed class CertainMortalitySamples : IPersonNaturalMortalitySampleProvider
    {
        public double GetSample(PersonId personId, long currentAbsoluteDay) => 0d;
    }
}
