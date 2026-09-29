using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class DailyDemographyBoundaryStepProviderTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void BoundaryOwnerKeepsSortedMortalityThenRecomputedFloorAndReceiptReport()
    {
        CityRuntime city = CreateCity("boundary-demography-city", 2);
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                naturalMortality: new NaturalMortalityConfigurationOverrides(
                    enabled: true,
                    annualProbability: 1d),
                aggregateDemography: new AggregateDemographyConfigurationOverrides(
                    enabled: true,
                    annualBirthRate: 0d,
                    annualDeathRate: 0d)));
        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(1L),
            new[] { city },
            null,
            configuration: configuration,
            calendarDefinition: new CalendarDefinition(2, 1, 2));

        PersonRuntime later = new PersonRuntime(new PersonId("z-person"), 0L);
        PersonRuntime earlier = new PersonRuntime(new PersonId("a-person"), 0L);
        Assert.That(world.TryRegisterPerson(later, out _), Is.True);
        Assert.That(world.TryRegisterPerson(earlier, out _), Is.True);
        Assert.That(world.TryBindExistingPersonResident(later.PersonId, city, out _), Is.True);
        Assert.That(world.TryBindExistingPersonResident(earlier.PersonId, city, out _), Is.True);

        RecordingMortalitySamples mortality = new RecordingMortalitySamples();
        RecordingAggregateProvider aggregate = new RecordingAggregateProvider(1, 0);
        DailyDemographyBoundaryStepProvider provider = new DailyDemographyBoundaryStepProvider(
            world, mortality, aggregate);
        DailyBoundaryOperation operation = new DailyBoundaryOperation(
            "boundary-demography-world", "intraday-v1", 1L);
        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> steps,
            out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(steps, Has.Count.EqualTo(1));
        Assert.That(steps[0].Ordinal, Is.Zero);
        BoundaryContinuationManifest manifest = new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "configuration/v1", steps);

        Assert.That(provider.TryPrepareStep(manifest, steps[0],
            out IBoundaryContinuationStepCommit prepared,
            out failure), Is.True, failure.ToString());
        Assert.That(prepared.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(provider.TryResolveReceipt(manifest, steps[0],
            out DailyDemographyBoundaryReceipt receipt,
            out failure), Is.True, failure.ToString());

        Assert.That(mortality.RequestedPersonIds, Is.EqualTo(new[] { "a-person", "z-person" }));
        Assert.That(earlier.DeathAbsoluteDay, Is.EqualTo(1L));
        Assert.That(later.DeathAbsoluteDay, Is.EqualTo(1L));
        Assert.That(city.CurrentPopulation, Is.EqualTo(1));
        Assert.That(city.Population.Revision, Is.EqualTo(3L));
        Assert.That(aggregate.Contexts, Has.Count.EqualTo(1));
        Assert.That(aggregate.Contexts[0].RepresentedResidentFloor, Is.EqualTo(0));
        Assert.That(aggregate.Contexts[0].CurrentPopulation, Is.EqualTo(0));
        Assert.That(receipt.Report.NamedPersonsEvaluated, Is.EqualTo(2));
        Assert.That(receipt.Report.NamedDeathsApplied, Is.EqualTo(2));
        Assert.That(receipt.Report.AggregateSettlementsProcessed, Is.EqualTo(1));
        Assert.That(receipt.Report.AggregateBirthsApplied, Is.EqualTo(1));
        Assert.That(receipt.Report.AggregateDeathsApplied, Is.Zero);
        Assert.That(receipt.Report.Diagnostics, Is.Empty);

        Assert.That(provider.TryPrepareStep(manifest, steps[0], out prepared, out failure),
            Is.True, failure.ToString());
        Assert.That(prepared.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(aggregate.CallCount, Is.EqualTo(1));
        Assert.That(mortality.RequestedPersonIds, Has.Count.EqualTo(2));
        Assert.That(city.CurrentPopulation, Is.EqualTo(1));
        Assert.That(city.Population.Revision, Is.EqualTo(3L));
    }

    [Test]
    public void RetainedAggregateProposalIsReusedAfterInstallInterruption()
    {
        CityRuntime city = CreateCity("retained-proposal-city", 10);
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                naturalMortality: new NaturalMortalityConfigurationOverrides(enabled: false),
                aggregateDemography: new AggregateDemographyConfigurationOverrides(
                    enabled: true,
                    annualBirthRate: 0d,
                    annualDeathRate: 0d)));
        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(1L), new[] { city }, null,
            configuration: configuration,
            calendarDefinition: new CalendarDefinition(2, 1, 2));
        RecordingAggregateProvider aggregate = new RecordingAggregateProvider(1, 0);
        DailyDemographyBoundaryStepProvider provider = new DailyDemographyBoundaryStepProvider(
            world, aggregateProvider: aggregate);
        BoundaryContinuationManifest manifest = CreateManifest(provider,
            new DailyBoundaryOperation("retained-proposal-world", "intraday-v1", 1L),
            out BoundaryContinuationStep step);

        object owner = GetPrivateField(provider, "owner");
        bool interruptOnce = true;
        SetPrivateField(owner, "interruptAggregateInstallForTest",
            new Func<AggregateDemographyTransition, bool>(_ =>
            {
                if (!interruptOnce) return false;
                interruptOnce = false;
                return true;
            }));

        Assert.That(provider.TryPrepareStep(manifest, step,
            out IBoundaryContinuationStepCommit prepared,
            out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(prepared.TryCommit(out failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.DispatchFailed));
        Assert.That(aggregate.CallCount, Is.EqualTo(1));
        Assert.That(city.CurrentPopulation, Is.EqualTo(10));
        Assert.That(city.Population.Revision, Is.Zero);

        Assert.That(provider.TryPrepareStep(manifest, step, out prepared, out failure),
            Is.True, failure.ToString());
        Assert.That(prepared.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(provider.TryResolveReceipt(manifest, step,
            out DailyDemographyBoundaryReceipt receipt,
            out failure), Is.True, failure.ToString());

        Assert.That(aggregate.CallCount, Is.EqualTo(1));
        Assert.That(city.CurrentPopulation, Is.EqualTo(11));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
        Assert.That(receipt.Report.AggregateSettlementsProcessed, Is.EqualTo(1));
        Assert.That(receipt.Report.AggregateBirthsApplied, Is.EqualTo(1));
        Assert.That(receipt.Report.Diagnostics, Is.Empty);
    }

    [Test]
    public void BoundaryOwnerRetainsEvaluationDiagnosticAndDoesNotResampleOnReceiptReplay()
    {
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                naturalMortality: new NaturalMortalityConfigurationOverrides(
                    enabled: true,
                    annualProbability: 1d),
                aggregateDemography: new AggregateDemographyConfigurationOverrides(enabled: false)));
        SimulationRuntime world = new SimulationRuntime(
            new SimulationTime(1L), null, null,
            configuration: configuration,
            calendarDefinition: new CalendarDefinition(2, 1, 2));
        PersonRuntime unknownBirth = new PersonRuntime(new PersonId("unknown-birth"));
        Assert.That(world.TryRegisterPerson(unknownBirth, out _), Is.True);

        RecordingMortalitySamples mortality = new RecordingMortalitySamples();
        DailyDemographyBoundaryStepProvider provider = new DailyDemographyBoundaryStepProvider(
            world, mortality);
        BoundaryContinuationManifest manifest = CreateManifest(provider,
            new DailyBoundaryOperation("diagnostic-demography-world", "intraday-v1", 1L),
            out BoundaryContinuationStep step);
        Assert.That(provider.TryPrepareStep(manifest, step,
            out IBoundaryContinuationStepCommit prepared,
            out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(prepared.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(provider.TryResolveReceipt(manifest, step,
            out DailyDemographyBoundaryReceipt firstReceipt,
            out failure), Is.True, failure.ToString());

        Assert.That(firstReceipt.Report.NamedPersonsEvaluated, Is.EqualTo(1));
        Assert.That(firstReceipt.Report.NamedDeathsApplied, Is.Zero);
        Assert.That(firstReceipt.Report.Diagnostics, Has.Count.EqualTo(1));
        Assert.That(firstReceipt.Report.Diagnostics[0].Severity,
            Is.EqualTo(DailyDemographyDiagnosticSeverity.Warning));
        Assert.That(firstReceipt.Report.Diagnostics[0].Code,
            Is.EqualTo("NaturalMortalityEvaluationRejected"));
        Assert.That(firstReceipt.Report.Diagnostics[0].Identity, Is.EqualTo("unknown-birth"));
        Assert.That(firstReceipt.Report.Diagnostics[0].Message,
            Is.EqualTo(PersonNaturalMortalityQueryFailure.BirthDateUnknown.ToString()));

        Assert.That(provider.TryPrepareStep(manifest, step, out prepared, out failure),
            Is.True, failure.ToString());
        Assert.That(prepared.TryCommit(out failure), Is.True, failure.ToString());
        Assert.That(mortality.RequestedPersonIds, Is.EqualTo(new[] { "unknown-birth" }));
        Assert.That(firstReceipt.Report.Diagnostics, Has.Count.EqualTo(1));
    }

    [Test]
    public void BoundaryDescriptorRejectsChangedNpcMaterializationCardinality()
    {
        SimulationRuntime world = CreateWorldForMortality();
        PersonRuntime person = new PersonRuntime(new PersonId("cardinality-person"), 0L);
        Assert.That(world.TryRegisterPerson(person, out _), Is.True);
        DailyDemographyBoundaryStepProvider provider =
            new DailyDemographyBoundaryStepProvider(world);
        BoundaryContinuationManifest manifest = CreateManifest(provider,
            new DailyBoundaryOperation("cardinality-demography-world", "intraday-v1", 1L),
            out BoundaryContinuationStep step);

        Assert.That(world.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc("cardinality-person-definition"),
            "cardinality-person-npc",
            null,
            0f,
            out _,
            out _), Is.True);

        Assert.That(provider.TryPrepareStep(manifest, step,
            out _, out TimelineFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(TimelineFailure.ContinuationFailed));
    }

    private static CityRuntime CreateCity(string runtimeId, int population)
    {
        CityData data = SimulationTestFactory.CreateCityData(runtimeId + "-definition");
        data.initialPopulation = population;
        return new CityRuntime(runtimeId, data,
            new SpatialLocationRuntime(runtimeId + "-location"));
    }

    private static SimulationRuntime CreateWorldForMortality()
    {
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                naturalMortality: new NaturalMortalityConfigurationOverrides(
                    enabled: true,
                    annualProbability: 1d),
                aggregateDemography: new AggregateDemographyConfigurationOverrides(enabled: false)));
        return new SimulationRuntime(
            new SimulationTime(1L), null, null,
            configuration: configuration,
            calendarDefinition: new CalendarDefinition(2, 1, 2));
    }

    private static BoundaryContinuationManifest CreateManifest(
        DailyDemographyBoundaryStepProvider provider,
        DailyBoundaryOperation operation,
        out BoundaryContinuationStep step)
    {
        Assert.That(provider.TryCreateSteps(operation, 0,
            out IReadOnlyList<BoundaryContinuationStep> steps,
            out TimelineFailure failure), Is.True, failure.ToString());
        Assert.That(steps, Has.Count.EqualTo(1));
        step = steps[0];
        return new BoundaryContinuationManifest(
            operation, "daily-boundary", "1", "configuration/v1", steps);
    }

    private static object GetPrivateField(object instance, string fieldName)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return field.GetValue(instance);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(instance, value);
    }

    private sealed class RecordingMortalitySamples : IPersonNaturalMortalitySampleProvider
    {
        public List<string> RequestedPersonIds { get; } = new List<string>();

        public double GetSample(PersonId personId, long currentAbsoluteDay)
        {
            RequestedPersonIds.Add(personId.Value);
            return 0d;
        }
    }

    private sealed class RecordingAggregateProvider : IAggregateDemographyProvider
    {
        private readonly AggregateDemographyChange change;

        public List<AggregateDemographyContext> Contexts { get; } =
            new List<AggregateDemographyContext>();
        public int CallCount => Contexts.Count;

        public RecordingAggregateProvider(int births, int deaths)
        {
            change = new AggregateDemographyChange(births, deaths);
        }

        public AggregateDemographyChange GetChange(AggregateDemographyContext context)
        {
            Contexts.Add(context);
            return new AggregateDemographyChange(
                change.Births + Contexts.Count - 1,
                change.Deaths);
        }
    }
}
