using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class AggregateDemographyFoundationTests
{
    [Test]
    public void ProviderReceivesExplicitAggregateSnapshotExactlyOnce()
    {
        SettlementPopulationRuntime population = CreatePopulation(20);
        RecordingProvider provider = new RecordingProvider(new AggregateDemographyChange(3, 2));

        Assert.That(AggregateDemographySystem.TryPropose(
            population,
            7,
            provider,
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure failure), Is.True, failure.ToString());

        Assert.That(provider.CallCount, Is.EqualTo(1));
        Assert.That(provider.LastContext.SettlementRuntimeId, Is.EqualTo("settlement-a"));
        Assert.That(provider.LastContext.PopulationRevision, Is.EqualTo(0L));
        Assert.That(provider.LastContext.CurrentPopulation, Is.EqualTo(20));
        Assert.That(provider.LastContext.RepresentedResidentFloor, Is.EqualTo(7));
        Assert.That(provider.LastContext.AggregateOnlyPopulation, Is.EqualTo(13));
        Assert.That(transition.Births, Is.EqualTo(3));
        Assert.That(transition.Deaths, Is.EqualTo(2));
        Assert.That(population.CurrentPopulation, Is.EqualTo(20));
        Assert.That(population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void NullProviderIsRejectedWithoutMutation()
    {
        SettlementPopulationRuntime population = CreatePopulation(20);

        Assert.That(AggregateDemographySystem.TryPropose(
            population,
            0,
            (IAggregateDemographyProvider)null,
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(AggregateDemographyFailure.InvalidProvider));
        Assert.That(population.CurrentPopulation, Is.EqualTo(20));
        Assert.That(population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void SameInputsProduceEquivalentTransitions()
    {
        AggregateDemographyTransition first = Propose(
            CreatePopulation(20),
            7,
            new AggregateDemographyChange(3, 2));
        AggregateDemographyTransition second = Propose(
            CreatePopulation(20),
            7,
            new AggregateDemographyChange(3, 2));

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }

    [Test]
    public void StochasticProviderOwnsExplicitRandomSourceAndRemainsReproducible()
    {
        SequenceRandomSource firstRandom = new SequenceRandomSource(0.25d);
        SequenceRandomSource secondRandom = new SequenceRandomSource(0.25d);

        AggregateDemographyTransition first = Propose(
            CreatePopulation(10),
            0,
            new RandomChoiceProvider(firstRandom));
        AggregateDemographyTransition second = Propose(
            CreatePopulation(10),
            0,
            new RandomChoiceProvider(secondRandom));

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.Births, Is.EqualTo(1));
        Assert.That(first.Deaths, Is.EqualTo(0));
        Assert.That(firstRandom.CallCount, Is.EqualTo(1));
        Assert.That(secondRandom.CallCount, Is.EqualTo(1));
    }

    [Test]
    public void AggregateBirthsAndDeathsApplyAsOnePopulationTransition()
    {
        SettlementPopulationRuntime population = CreatePopulation(20);
        AggregateDemographyTransition transition = Propose(
            population,
            4,
            new AggregateDemographyChange(5, 2));

        Assert.That(AggregateDemographySystem.TryApply(
            population,
            4,
            transition,
            out AggregateDemographyFailure failure), Is.True, failure.ToString());

        Assert.That(population.CurrentPopulation, Is.EqualTo(23));
        Assert.That(population.Revision, Is.EqualTo(1L));
        Assert.That(transition.NetChange, Is.EqualTo(3L));
    }

    [Test]
    public void AggregateBirthDoesNotCreatePerson()
    {
        PersonStore store = new PersonStore();
        SettlementPopulationRuntime population = CreatePopulation(10);
        AggregateDemographyTransition transition = Propose(
            population,
            0,
            new AggregateDemographyChange(1, 0));

        Apply(population, 0, transition);

        Assert.That(population.CurrentPopulation, Is.EqualTo(11));
        Assert.That(store.Persons, Is.Empty);
    }

    [Test]
    public void AggregateDeathDoesNotDeleteOrMutatePerson()
    {
        PersonStore store = new PersonStore();
        PersonRuntime person = new PersonRuntime(new PersonId("represented-person"), 3L);
        Assert.That(store.TryRegister(person, out _), Is.True);
        SettlementPopulationRuntime population = CreatePopulation(10);
        AggregateDemographyTransition transition = Propose(
            population,
            1,
            new AggregateDemographyChange(0, 2));

        Apply(population, 1, transition);

        Assert.That(population.CurrentPopulation, Is.EqualTo(8));
        Assert.That(store.Persons, Has.Count.EqualTo(1));
        Assert.That(store.TryGet(person.PersonId, out PersonRuntime stored), Is.True);
        Assert.That(stored, Is.SameAs(person));
        Assert.That(stored.BirthAbsoluteDay, Is.EqualTo(3L));
    }

    [TestCase(PopulationRepresentationMode.Aggregate)]
    [TestCase(PopulationRepresentationMode.Hybrid)]
    public void AggregateAndHybridRepresentationUseSameAggregateBoundary(
        PopulationRepresentationMode representationMode)
    {
        EffectivePopulationConfiguration configuration = new EffectivePopulationConfiguration(
            representationMode,
            NpcDecisionSimulationScope.RelevantOnly);
        SettlementPopulationRuntime population = CreatePopulation(10);

        AggregateDemographyTransition transition = Propose(
            population,
            2,
            new AggregateDemographyChange(2, 1));
        Apply(population, 2, transition);

        Assert.That(configuration.RepresentationMode, Is.EqualTo(representationMode));
        Assert.That(population.CurrentPopulation, Is.EqualTo(11));
    }

    [Test]
    public void AggregateFoundationDoesNotReferencePersonNpcWorldOrCalendarTypes()
    {
        Type[] forbiddenTypes =
        {
            typeof(PersonRuntime),
            typeof(PersonStore),
            typeof(NpcRuntime),
            typeof(SimulationRuntime),
            typeof(SimulationCalendar)
        };
        Type[] productionTypes =
        {
            typeof(AggregateDemographyChange),
            typeof(AggregateDemographyContext),
            typeof(IAggregateDemographyProvider),
            typeof(IAggregateDemographyRandomSource),
            typeof(AggregateDemographyTransition),
            typeof(AggregateDemographySystem)
        };

        foreach (Type productionType in productionTypes)
        {
            Type[] exposedTypes = productionType
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .SelectMany(GetExposedTypes)
                .ToArray();

            foreach (Type forbiddenType in forbiddenTypes)
            {
                Assert.That(exposedTypes, Has.None.EqualTo(forbiddenType), productionType.Name);
            }
        }
    }

    [Test]
    public void PopulationUnderflowIsRejectedWithoutMutation()
    {
        SettlementPopulationRuntime population = CreatePopulation(2);

        Assert.That(AggregateDemographySystem.TryPropose(
            population,
            0,
            new AggregateDemographyChange(0, 3),
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(AggregateDemographyFailure.WouldUnderflow));
        Assert.That(population.CurrentPopulation, Is.EqualTo(2));
        Assert.That(population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void PopulationOverflowIsRejectedWithoutMutation()
    {
        SettlementPopulationRuntime population = CreatePopulation(int.MaxValue);

        Assert.That(AggregateDemographySystem.TryPropose(
            population,
            0,
            new AggregateDemographyChange(1, 0),
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(AggregateDemographyFailure.WouldOverflow));
        Assert.That(population.CurrentPopulation, Is.EqualTo(int.MaxValue));
        Assert.That(population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void NegativeProviderOutputIsRejectedWithoutMutation()
    {
        SettlementPopulationRuntime population = CreatePopulation(10);

        Assert.That(AggregateDemographySystem.TryPropose(
            population,
            0,
            new RecordingProvider(new AggregateDemographyChange(-1, 0)),
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(AggregateDemographyFailure.NegativeChange));
        Assert.That(population.CurrentPopulation, Is.EqualTo(10));
        Assert.That(population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void DeathsCannotCrossExplicitRepresentedResidentFloor()
    {
        SettlementPopulationRuntime population = CreatePopulation(10);

        Assert.That(AggregateDemographySystem.TryPropose(
            population,
            8,
            new AggregateDemographyChange(0, 3),
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(AggregateDemographyFailure.WouldViolateRepresentedResidentFloor));
        Assert.That(population.CurrentPopulation, Is.EqualTo(10));
        Assert.That(population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void DeathsMayReachExplicitRepresentedResidentFloorExactly()
    {
        SettlementPopulationRuntime population = CreatePopulation(10);
        AggregateDemographyTransition transition = Propose(
            population,
            8,
            new AggregateDemographyChange(0, 2));

        Apply(population, 8, transition);

        Assert.That(population.CurrentPopulation, Is.EqualTo(8));
    }

    [TestCase(-1)]
    [TestCase(11)]
    public void InvalidRepresentedResidentFloorIsRejected(int floor)
    {
        SettlementPopulationRuntime population = CreatePopulation(10);

        Assert.That(AggregateDemographySystem.TryPropose(
            population,
            floor,
            new AggregateDemographyChange(0, 0),
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(AggregateDemographyFailure.InvalidRepresentedResidentFloor));
    }

    [Test]
    public void PopulationMutationMakesProposalStale()
    {
        SettlementPopulationRuntime population = CreatePopulation(10);
        AggregateDemographyTransition stale = Propose(
            population,
            2,
            new AggregateDemographyChange(1, 0));
        Apply(population, 2, Propose(population, 2, new AggregateDemographyChange(0, 1)));
        int populationBeforeRejectedApply = population.CurrentPopulation;
        long revisionBeforeRejectedApply = population.Revision;

        Assert.That(AggregateDemographySystem.TryApply(
            population,
            2,
            stale,
            out AggregateDemographyFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(AggregateDemographyFailure.StaleState));
        Assert.That(population.CurrentPopulation, Is.EqualTo(populationBeforeRejectedApply));
        Assert.That(population.Revision, Is.EqualTo(revisionBeforeRejectedApply));
    }

    [Test]
    public void ChangedRepresentedResidentFloorMakesProposalStale()
    {
        SettlementPopulationRuntime population = CreatePopulation(10);
        AggregateDemographyTransition transition = Propose(
            population,
            2,
            new AggregateDemographyChange(0, 1));

        Assert.That(AggregateDemographySystem.TryApply(
            population,
            3,
            transition,
            out AggregateDemographyFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(AggregateDemographyFailure.StaleState));
        Assert.That(population.CurrentPopulation, Is.EqualTo(10));
        Assert.That(population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void AppliedProposalCannotApplyTwice()
    {
        SettlementPopulationRuntime population = CreatePopulation(10);
        AggregateDemographyTransition transition = Propose(
            population,
            0,
            new AggregateDemographyChange(1, 0));
        Apply(population, 0, transition);

        Assert.That(AggregateDemographySystem.TryApply(
            population,
            0,
            transition,
            out AggregateDemographyFailure failure), Is.False);

        Assert.That(failure, Is.EqualTo(AggregateDemographyFailure.StaleState));
        Assert.That(population.CurrentPopulation, Is.EqualTo(11));
        Assert.That(population.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void RevisionOverflowIsRejectedWithoutMutation()
    {
        SettlementPopulationRuntime population = CreatePopulation(10);
        SetPrivateField(population, "revision", long.MaxValue);

        Assert.That(AggregateDemographySystem.TryPropose(
            population,
            0,
            new AggregateDemographyChange(1, 0),
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(AggregateDemographyFailure.RevisionOverflow));
        Assert.That(population.CurrentPopulation, Is.EqualTo(10));
        Assert.That(population.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void RevisionOverflowDoesNotInvokeProviderOrConsumeItsRandomness()
    {
        SettlementPopulationRuntime population = CreatePopulation(10);
        SetPrivateField(population, "revision", long.MaxValue);
        SequenceRandomSource randomSource = new SequenceRandomSource(0.25d);
        RandomChoiceProvider provider = new RandomChoiceProvider(randomSource);

        Assert.That(AggregateDemographySystem.TryPropose(
            population,
            0,
            provider,
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure failure), Is.False);

        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(AggregateDemographyFailure.RevisionOverflow));
        Assert.That(randomSource.CallCount, Is.EqualTo(0));
        Assert.That(population.CurrentPopulation, Is.EqualTo(10));
        Assert.That(population.Revision, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void TransitionFactsAreReadOnlyAndCannotBePubliclyConstructed()
    {
        Assert.That(typeof(AggregateDemographyTransition).GetConstructors(), Is.Empty);
        Assert.That(typeof(AggregateDemographyTransition).GetProperties(),
            Has.All.Matches<PropertyInfo>(property => property.CanWrite == false));
    }

    private static SettlementPopulationRuntime CreatePopulation(int population)
    {
        return new SettlementPopulationRuntime("settlement-a", population);
    }

    private static AggregateDemographyTransition Propose(
        SettlementPopulationRuntime population,
        int representedResidentFloor,
        AggregateDemographyChange change)
    {
        Assert.That(AggregateDemographySystem.TryPropose(
            population,
            representedResidentFloor,
            change,
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure failure), Is.True, failure.ToString());
        return transition;
    }

    private static AggregateDemographyTransition Propose(
        SettlementPopulationRuntime population,
        int representedResidentFloor,
        IAggregateDemographyProvider provider)
    {
        Assert.That(AggregateDemographySystem.TryPropose(
            population,
            representedResidentFloor,
            provider,
            out AggregateDemographyTransition transition,
            out AggregateDemographyFailure failure), Is.True, failure.ToString());
        return transition;
    }

    private static void Apply(
        SettlementPopulationRuntime population,
        int representedResidentFloor,
        AggregateDemographyTransition transition)
    {
        Assert.That(AggregateDemographySystem.TryApply(
            population,
            representedResidentFloor,
            transition,
            out AggregateDemographyFailure failure), Is.True, failure.ToString());
    }

    private static Type[] GetExposedTypes(MemberInfo member)
    {
        if (member is FieldInfo field)
        {
            return new[] { field.FieldType };
        }

        if (member is PropertyInfo property)
        {
            return new[] { property.PropertyType };
        }

        if (member is MethodInfo method)
        {
            return method.GetParameters()
                .Select(parameter => parameter.ParameterType.IsByRef
                    ? parameter.ParameterType.GetElementType()
                    : parameter.ParameterType)
                .Append(method.ReturnType)
                .ToArray();
        }

        return Array.Empty<Type>();
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(instance, value);
    }

    private sealed class RecordingProvider : IAggregateDemographyProvider
    {
        private readonly AggregateDemographyChange change;

        public int CallCount { get; private set; }
        public AggregateDemographyContext LastContext { get; private set; }

        public RecordingProvider(AggregateDemographyChange change)
        {
            this.change = change;
        }

        public AggregateDemographyChange GetChange(AggregateDemographyContext context)
        {
            CallCount++;
            LastContext = context;
            return change;
        }
    }

    private sealed class RandomChoiceProvider : IAggregateDemographyProvider
    {
        private readonly IAggregateDemographyRandomSource randomSource;

        public RandomChoiceProvider(IAggregateDemographyRandomSource randomSource)
        {
            this.randomSource = randomSource;
        }

        public AggregateDemographyChange GetChange(AggregateDemographyContext context)
        {
            return randomSource.NextUnitInterval() < 0.5d
                ? new AggregateDemographyChange(1, 0)
                : new AggregateDemographyChange(0, 1);
        }
    }

    private sealed class SequenceRandomSource : IAggregateDemographyRandomSource
    {
        private readonly double value;

        public int CallCount { get; private set; }

        public SequenceRandomSource(double value)
        {
            this.value = value;
        }

        public double NextUnitInterval()
        {
            CallCount++;
            return value;
        }
    }
}
