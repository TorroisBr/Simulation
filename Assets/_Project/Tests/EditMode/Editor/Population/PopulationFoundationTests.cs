using System;
using System.Linq;
using NUnit.Framework;

public sealed class PopulationFoundationTests
{
    [Test]
    public void SettlementPopulationStartsWithProvidedCount()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);

        Assert.That(population.SettlementRuntimeId, Is.EqualTo("settlement-a"));
        Assert.That(population.CurrentPopulation, Is.EqualTo(1000));
    }

    [Test]
    public void NegativeInitialPopulationIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePopulation(-1));
    }

    [Test]
    public void PopulationRuntimeHasNoIndependentRuntimeId()
    {
        Assert.That(typeof(SettlementPopulationRuntime).GetProperty("RuntimeId"), Is.Null);
        Assert.That(typeof(SettlementPopulationRuntime).GetProperty("SettlementRuntimeId"), Is.Not.Null);
    }

    [Test]
    public void BirthsIncreasePopulation()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(12, 0, 0, 0));

        Assert.That(transition.NetChange, Is.EqualTo(12L));
        Apply(population, transition);
        Assert.That(population.CurrentPopulation, Is.EqualTo(1012));
    }

    [Test]
    public void DeathsDecreasePopulation()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(0, 5, 0, 0));

        Apply(population, transition);
        Assert.That(population.CurrentPopulation, Is.EqualTo(995));
    }

    [Test]
    public void ImmigrationsIncreasePopulation()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(0, 0, 20, 0));

        Apply(population, transition);
        Assert.That(population.CurrentPopulation, Is.EqualTo(1020));
    }

    [Test]
    public void EmigrationsDecreasePopulation()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(0, 0, 0, 7));

        Apply(population, transition);
        Assert.That(population.CurrentPopulation, Is.EqualTo(993));
    }

    [Test]
    public void CombinedChangeSetCalculatesCorrectNetChange()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        PopulationChangeSet changes = new PopulationChangeSet(12, 5, 20, 7);
        SettlementPopulationTransition transition = Propose(population, changes);

        Assert.That(changes.NetChange, Is.EqualTo(20L));
        Assert.That(transition.NetChange, Is.EqualTo(20L));
        Assert.That(transition.PopulationAfter, Is.EqualTo(1020));
    }

    [Test]
    public void ProposalDoesNotMutatePopulation()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);

        Propose(population, new PopulationChangeSet(12, 5, 20, 7));

        Assert.That(population.CurrentPopulation, Is.EqualTo(1000));
    }

    [Test]
    public void ProposalIsDeterministic()
    {
        PopulationChangeSet changes = new PopulationChangeSet(12, 5, 20, 7);
        SettlementPopulationTransition first = Propose(CreatePopulation(1000), changes);
        SettlementPopulationTransition second = Propose(CreatePopulation(1000), changes);

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void TransitionCopiesPopulationBefore()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);

        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(12, 0, 0, 0));

        Assert.That(transition.PopulationBefore, Is.EqualTo(1000));
    }

    [Test]
    public void TransitionStoresPopulationAfter()
    {
        SettlementPopulationTransition transition = Propose(CreatePopulation(1000), new PopulationChangeSet(12, 5, 20, 7));

        Assert.That(transition.PopulationAfter, Is.EqualTo(1020));
    }

    [Test]
    public void TransitionIsUnaffectedByLaterRuntimeMutation()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(12, 0, 0, 0));

        Apply(population, transition);

        Assert.That(transition.PopulationBefore, Is.EqualTo(1000));
        Assert.That(transition.PopulationAfter, Is.EqualTo(1012));
        Assert.That(transition.NetChange, Is.EqualTo(12L));
    }

    [Test]
    public void NegativeBirthsAreRejected()
    {
        AssertProposalRejected(new PopulationChangeSet(-1, 0, 0, 0), PopulationTransitionFailure.NegativeChange);
    }

    [Test]
    public void NegativeDeathsAreRejected()
    {
        AssertProposalRejected(new PopulationChangeSet(0, -1, 0, 0), PopulationTransitionFailure.NegativeChange);
    }

    [Test]
    public void NegativeImmigrationsAreRejected()
    {
        AssertProposalRejected(new PopulationChangeSet(0, 0, -1, 0), PopulationTransitionFailure.NegativeChange);
    }

    [Test]
    public void NegativeEmigrationsAreRejected()
    {
        AssertProposalRejected(new PopulationChangeSet(0, 0, 0, -1), PopulationTransitionFailure.NegativeChange);
    }

    [Test]
    public void DeathsCannotUnderflowPopulation()
    {
        AssertProposalRejected(
            new PopulationChangeSet(0, 101, 0, 0),
            PopulationTransitionFailure.WouldUnderflow,
            100);
    }

    [Test]
    public void EmigrationsCannotUnderflowPopulation()
    {
        AssertProposalRejected(
            new PopulationChangeSet(0, 0, 0, 101),
            PopulationTransitionFailure.WouldUnderflow,
            100);
    }

    [Test]
    public void CombinedDeathsAndEmigrationsCannotUnderflowPopulation()
    {
        AssertProposalRejected(
            new PopulationChangeSet(0, 60, 0, 41),
            PopulationTransitionFailure.WouldUnderflow,
            100);
    }

    [Test]
    public void PopulationOverflowIsRejected()
    {
        AssertProposalRejected(
            new PopulationChangeSet(1, 0, 0, 0),
            PopulationTransitionFailure.WouldOverflow,
            int.MaxValue);
    }

    [Test]
    public void RejectedProposalLeavesRuntimeUnchanged()
    {
        SettlementPopulationRuntime population = CreatePopulation(100);
        bool proposed = SettlementPopulationSystem.TryPropose(
            population,
            new PopulationChangeSet(0, 101, 0, 0),
            out SettlementPopulationTransition transition,
            out PopulationTransitionFailure failure);

        Assert.That(proposed, Is.False);
        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(PopulationTransitionFailure.WouldUnderflow));
        Assert.That(population.CurrentPopulation, Is.EqualTo(100));
    }

    [Test]
    public void ValidTransitionAppliesAtomically()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(12, 5, 20, 7));

        bool applied = SettlementPopulationSystem.TryApply(population, transition, out PopulationTransitionFailure failure);

        Assert.That(applied, Is.True);
        Assert.That(failure, Is.EqualTo(PopulationTransitionFailure.None));
        Assert.That(population.CurrentPopulation, Is.EqualTo(1020));
    }

    [Test]
    public void StaleTransitionIsRejected()
    {
        SettlementPopulationRuntime population = CreatePopulation(100);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(10, 0, 0, 0));
        Apply(population, Propose(population, new PopulationChangeSet(0, 5, 0, 0)));

        bool applied = SettlementPopulationSystem.TryApply(population, transition, out PopulationTransitionFailure failure);

        Assert.That(applied, Is.False);
        Assert.That(failure, Is.EqualTo(PopulationTransitionFailure.StaleState));
    }

    [Test]
    public void RejectedStaleTransitionDoesNotMutatePopulation()
    {
        SettlementPopulationRuntime population = CreatePopulation(100);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(10, 0, 0, 0));
        Apply(population, Propose(population, new PopulationChangeSet(0, 5, 0, 0)));

        bool applied = SettlementPopulationSystem.TryApply(population, transition, out _);

        Assert.That(applied, Is.False);
        Assert.That(population.CurrentPopulation, Is.EqualTo(95));
    }

    [Test]
    public void PreviouslyAppliedTransitionCannotBlindlyApplyAgain()
    {
        SettlementPopulationRuntime population = CreatePopulation(100);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(10, 0, 0, 0));

        Assert.That(SettlementPopulationSystem.TryApply(population, transition, out _), Is.True);
        Assert.That(SettlementPopulationSystem.TryApply(population, transition, out PopulationTransitionFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PopulationTransitionFailure.StaleState));
        Assert.That(population.CurrentPopulation, Is.EqualTo(110));
    }

    [Test]
    public void FailedApplicationNeverPartiallyMutates()
    {
        SettlementPopulationRuntime population = CreatePopulation(100);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(10, 0, 0, 0));
        Apply(population, Propose(population, new PopulationChangeSet(0, 1, 0, 0)));
        int populationBeforeFailedApply = population.CurrentPopulation;

        bool applied = SettlementPopulationSystem.TryApply(population, transition, out _);

        Assert.That(applied, Is.False);
        Assert.That(population.CurrentPopulation, Is.EqualTo(populationBeforeFailedApply));
    }

    [Test]
    public void SettlementRuntimeIdIsPreserved()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(1, 0, 0, 0));

        Assert.That(transition.SettlementRuntimeId, Is.EqualTo("settlement-a"));
        Apply(population, transition);
        Assert.That(population.SettlementRuntimeId, Is.EqualTo("settlement-a"));
    }

    [Test]
    public void PopulationFoundationDoesNotDependOnNpcRuntime()
    {
        Type[] productionTypes =
        {
            typeof(SettlementPopulationRuntime),
            typeof(PopulationChangeSet),
            typeof(SettlementPopulationTransition),
            typeof(SettlementPopulationSystem)
        };

        foreach (Type productionType in productionTypes)
        {
            Type[] referencedTypes = productionType
                .GetMembers()
                .Select(member => member.MemberType == System.Reflection.MemberTypes.Field
                    ? ((System.Reflection.FieldInfo)member).FieldType
                    : member.MemberType == System.Reflection.MemberTypes.Property
                        ? ((System.Reflection.PropertyInfo)member).PropertyType
                        : null)
                .Where(type => type != null)
                .ToArray();

            Assert.That(referencedTypes, Has.None.EqualTo(typeof(NpcRuntime)), productionType.Name);
        }
    }

    [Test]
    public void PopulationChangeDoesNotCreateNpc()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        Propose(population, new PopulationChangeSet(12, 0, 0, 0));

        Assert.That(population.CurrentPopulation, Is.EqualTo(1000));
        Assert.That(typeof(SettlementPopulationRuntime).GetFields(
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic)
            .Any(field => field.FieldType == typeof(NpcRuntime)), Is.False);
    }

    [Test]
    public void PopulationChangeDoesNotAllocateRuntimeIdentity()
    {
        RuntimeIdAllocator allocator = new RuntimeIdAllocator();
        string firstCityId = allocator.AllocateCityId();
        SettlementPopulationRuntime population = CreatePopulation(1000);

        Propose(population, new PopulationChangeSet(12, 0, 0, 0));

        string secondCityId = allocator.AllocateCityId();
        Assert.That(firstCityId, Is.EqualTo("city-000001"));
        Assert.That(secondCityId, Is.EqualTo("city-000002"));
    }

    [Test]
    public void PopulationProposalDoesNotAdvanceSimulationTime()
    {
        SimulationTime time = new SimulationTime(17L);

        Propose(CreatePopulation(1000), new PopulationChangeSet(12, 0, 0, 0));

        Assert.That(time.AbsoluteDay, Is.EqualTo(17L));
    }

    [Test]
    public void PopulationProposalDoesNotConsumeRng()
    {
        Random random = new Random(1234);
        int first = random.Next();

        Propose(CreatePopulation(1000), new PopulationChangeSet(12, 0, 0, 0));

        Random expectedRandom = new Random(1234);
        Assert.That(first, Is.EqualTo(expectedRandom.Next()));
        Assert.That(random.Next(), Is.EqualTo(expectedRandom.Next()));
    }

    [Test]
    public void SameInputsProduceEquivalentTransitionValues()
    {
        SettlementPopulationTransition first = Propose(
            new SettlementPopulationRuntime("settlement-a", 1000),
            new PopulationChangeSet(12, 5, 20, 7));
        SettlementPopulationTransition second = Propose(
            new SettlementPopulationRuntime("settlement-a", 1000),
            new PopulationChangeSet(12, 5, 20, 7));

        Assert.That(first.SettlementRuntimeId, Is.EqualTo(second.SettlementRuntimeId));
        Assert.That(first.PopulationBefore, Is.EqualTo(second.PopulationBefore));
        Assert.That(first.Births, Is.EqualTo(second.Births));
        Assert.That(first.Deaths, Is.EqualTo(second.Deaths));
        Assert.That(first.Immigrations, Is.EqualTo(second.Immigrations));
        Assert.That(first.Emigrations, Is.EqualTo(second.Emigrations));
        Assert.That(first.NetChange, Is.EqualTo(second.NetChange));
        Assert.That(first.PopulationAfter, Is.EqualTo(second.PopulationAfter));
    }

    [Test]
    public void ZeroPopulationWithZeroChangesIsValid()
    {
        SettlementPopulationTransition transition = Propose(CreatePopulation(0), new PopulationChangeSet(0, 0, 0, 0));

        Assert.That(transition.PopulationBefore, Is.EqualTo(0));
        Assert.That(transition.PopulationAfter, Is.EqualTo(0));
    }

    [Test]
    public void ZeroPopulationCanReceiveBirths()
    {
        SettlementPopulationRuntime population = CreatePopulation(0);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(1, 0, 0, 0));

        Apply(population, transition);
        Assert.That(population.CurrentPopulation, Is.EqualTo(1));
    }

    [Test]
    public void ZeroPopulationCannotReceiveDeaths()
    {
        AssertProposalRejected(
            new PopulationChangeSet(0, 1, 0, 0),
            PopulationTransitionFailure.WouldUnderflow,
            0);
    }

    [Test]
    public void ZeroChangeProducesNoPopulationDifference()
    {
        SettlementPopulationTransition transition = Propose(CreatePopulation(1000), new PopulationChangeSet(0, 0, 0, 0));

        Assert.That(transition.NetChange, Is.EqualTo(0L));
        Assert.That(transition.PopulationBefore, Is.EqualTo(transition.PopulationAfter));
    }

    [Test]
    public void NetZeroChangeCanStillBeRepresented()
    {
        SettlementPopulationTransition transition = Propose(CreatePopulation(1000), new PopulationChangeSet(5, 5, 0, 0));

        Assert.That(transition.Births, Is.EqualTo(5));
        Assert.That(transition.Deaths, Is.EqualTo(5));
        Assert.That(transition.NetChange, Is.EqualTo(0L));
        Assert.That(transition.PopulationBefore, Is.EqualTo(transition.PopulationAfter));
    }

    [Test]
    public void NetZeroTransitionCannotBeAppliedTwice()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(5, 5, 0, 0));

        Assert.That(SettlementPopulationSystem.TryApply(population, transition, out _), Is.True);
        Assert.That(SettlementPopulationSystem.TryApply(population, transition, out PopulationTransitionFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PopulationTransitionFailure.StaleState));
        Assert.That(population.CurrentPopulation, Is.EqualTo(1000));
        Assert.That(population.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void ZeroChangeTransitionCannotBeAppliedTwice()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(0, 0, 0, 0));

        Assert.That(SettlementPopulationSystem.TryApply(population, transition, out _), Is.True);
        Assert.That(SettlementPopulationSystem.TryApply(population, transition, out PopulationTransitionFailure failure), Is.False);
        Assert.That(failure, Is.EqualTo(PopulationTransitionFailure.StaleState));
        Assert.That(population.CurrentPopulation, Is.EqualTo(1000));
        Assert.That(population.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void SuccessfulApplyIncrementsRevision()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition transition = Propose(population, new PopulationChangeSet(10, 0, 0, 0));

        Assert.That(population.Revision, Is.EqualTo(0L));
        Apply(population, transition);

        Assert.That(population.CurrentPopulation, Is.EqualTo(1010));
        Assert.That(population.Revision, Is.EqualTo(1L));
    }

    [Test]
    public void RejectedApplyDoesNotIncrementRevision()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition staleTransition = Propose(population, new PopulationChangeSet(10, 0, 0, 0));
        Apply(population, Propose(population, new PopulationChangeSet(1, 0, 0, 0)));
        long revisionBeforeRejectedApply = population.Revision;

        bool applied = SettlementPopulationSystem.TryApply(population, staleTransition, out PopulationTransitionFailure failure);

        Assert.That(applied, Is.False);
        Assert.That(failure, Is.EqualTo(PopulationTransitionFailure.StaleState));
        Assert.That(population.Revision, Is.EqualTo(revisionBeforeRejectedApply));
    }

    [Test]
    public void StaleTransitionIsRejectedEvenWhenPopulationCountMatchesAgain()
    {
        SettlementPopulationRuntime population = CreatePopulation(100);
        SettlementPopulationTransition transitionA = Propose(population, new PopulationChangeSet(10, 0, 0, 0));

        Apply(population, Propose(population, new PopulationChangeSet(10, 0, 0, 0)));
        Apply(population, Propose(population, new PopulationChangeSet(0, 10, 0, 0)));

        Assert.That(population.CurrentPopulation, Is.EqualTo(100));
        Assert.That(population.Revision, Is.EqualTo(2L));
        bool applied = SettlementPopulationSystem.TryApply(population, transitionA, out PopulationTransitionFailure failure);

        Assert.That(applied, Is.False);
        Assert.That(failure, Is.EqualTo(PopulationTransitionFailure.StaleState));
        Assert.That(population.CurrentPopulation, Is.EqualTo(100));
        Assert.That(population.Revision, Is.EqualTo(2L));
    }

    [Test]
    public void RepeatedProposalWithoutApplyDoesNotChangeRevision()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);

        Propose(population, new PopulationChangeSet(10, 0, 0, 0));
        Propose(population, new PopulationChangeSet(10, 0, 0, 0));

        Assert.That(population.CurrentPopulation, Is.EqualTo(1000));
        Assert.That(population.Revision, Is.EqualTo(0L));
    }

    [Test]
    public void ProposalCapturesCurrentRevision()
    {
        SettlementPopulationRuntime population = CreatePopulation(1000);
        SettlementPopulationTransition first = Propose(population, new PopulationChangeSet(10, 0, 0, 0));

        Apply(population, first);
        SettlementPopulationTransition second = Propose(population, new PopulationChangeSet(5, 0, 0, 0));

        Assert.That(first.ExpectedRevision, Is.EqualTo(0L));
        Assert.That(second.ExpectedRevision, Is.EqualTo(1L));
    }

    private static SettlementPopulationRuntime CreatePopulation(int count)
    {
        return new SettlementPopulationRuntime("settlement-a", count);
    }

    private static SettlementPopulationTransition Propose(
        SettlementPopulationRuntime population,
        PopulationChangeSet changes)
    {
        bool proposed = SettlementPopulationSystem.TryPropose(
            population,
            changes,
            out SettlementPopulationTransition transition,
            out PopulationTransitionFailure failure);

        Assert.That(proposed, Is.True, failure.ToString());
        Assert.That(transition, Is.Not.Null);
        return transition;
    }

    private static void Apply(
        SettlementPopulationRuntime population,
        SettlementPopulationTransition transition)
    {
        bool applied = SettlementPopulationSystem.TryApply(
            population,
            transition,
            out PopulationTransitionFailure failure);

        Assert.That(applied, Is.True, failure.ToString());
    }

    private static void AssertProposalRejected(
        PopulationChangeSet changes,
        PopulationTransitionFailure expectedFailure,
        int populationCount = 100)
    {
        SettlementPopulationRuntime population = CreatePopulation(populationCount);
        bool proposed = SettlementPopulationSystem.TryPropose(
            population,
            changes,
            out SettlementPopulationTransition transition,
            out PopulationTransitionFailure failure);

        Assert.That(proposed, Is.False);
        Assert.That(transition, Is.Null);
        Assert.That(failure, Is.EqualTo(expectedFailure));
        Assert.That(population.CurrentPopulation, Is.EqualTo(populationCount));
    }
}
