using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SettlementPopulationCensusTests
{
    private readonly List<GameObject> simulationObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject simulationObject in simulationObjects)
        {
            if (simulationObject != null)
            {
                Object.DestroyImmediate(simulationObject);
            }
        }

        simulationObjects.Clear();
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void SelectedAuthoredProfileHasTwoDistinctPopulationOwnersAndExpectedInitialValues()
    {
        SimulationConfigData config = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(
            "Assets/_Project/Data/Simulations/Simulation-GeneralTest.asset");
        Assert.That(config, Is.Not.Null);
        GameObject simulationObject = new GameObject("p12d-population-census-profile-test");
        simulationObjects.Add(simulationObject);
        TesteSimulacao simulation = simulationObject.AddComponent<TesteSimulacao>();
        typeof(TesteSimulacao).GetField("simulationConfig", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(simulation, config);

        simulation.Start();

        IReadOnlyList<CityRuntime> cities = simulation.Runtime.Cities;
        Assert.That(cities, Has.Count.EqualTo(2));
        string[] runtimeIds = cities.Select(city => city.RuntimeId).OrderBy(id => id, System.StringComparer.Ordinal).ToArray();
        Assert.That(runtimeIds.Distinct().Count(), Is.EqualTo(2));
        Assert.That(cities.Select(city => city.Population.CurrentPopulation), Is.EquivalentTo(new[] { 1000, 800 }));

        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            simulation.Bootstrap.SettlementPopulationCensusProviders;
        Assert.That(providers, Has.Count.EqualTo(4));
        CityRuntime[] orderedCities = cities.OrderBy(city => city.RuntimeId, System.StringComparer.Ordinal).ToArray();
        for (int cityIndex = 0; cityIndex < orderedCities.Length; cityIndex++)
        {
            AssertInitialPair(providers[cityIndex * 2], providers[cityIndex * 2 + 1], orderedCities[cityIndex]);
        }

        OwnerSectionCensusWitness genealogy = simulation.Bootstrap.GenealogyCensusProvider.GetCurrentCensus();
        Assert.That(genealogy.SectionId, Is.EqualTo(GenealogyCensusProvider.SectionId));
        Assert.That(genealogy.OwnerInstanceIdentity, Is.Not.Null);
        Assert.That(genealogy.Cardinality, Is.Zero);
        Assert.That(genealogy.Revision, Is.Zero);
    }

    [Test]
    public void ProvidersUseStableLengthPrefixedIdsAndExactOwnersInRuntimeIdOrder()
    {
        CityRuntime later = CreateCity("z/city", 800);
        CityRuntime earlier = CreateCity("a", 0);

        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            SettlementPopulationCensusProvider.CreateProviders(new[] { later, earlier });

        Assert.That(providers, Has.Count.EqualTo(4));
        AssertInitialPair(providers[0], providers[1], earlier);
        AssertInitialPair(providers[2], providers[3], later);
        Assert.That(providers[0].GetCurrentCensus().SectionId,
            Is.EqualTo(SettlementPopulationCensusProvider.AggregateSectionPrefix + "1:a"));
        Assert.That(providers[1].GetCurrentCensus().SectionId,
            Is.EqualTo(SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix + "1:a"));
        Assert.That(providers[2].GetCurrentCensus().SectionId,
            Is.EqualTo(SettlementPopulationCensusProvider.AggregateSectionPrefix + "6:z/city"));
        Assert.That(providers[3].GetCurrentCensus().SectionId,
            Is.EqualTo(SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix + "6:z/city"));
        Assert.That(providers[0].GetCurrentCensus().Cardinality, Is.EqualTo(1),
            "An aggregate owner remains one section even when its current population is zero.");
        Assert.That(providers[0].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(earlier.Population));
        Assert.That(providers[1].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(earlier.Population));
        Assert.That(providers[2].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(later.Population));
        Assert.That(providers[3].GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(later.Population));

        OwnerSectionCensusWitness[] firstRead = providers.Select(provider => provider.GetCurrentCensus()).ToArray();
        OwnerSectionCensusWitness[] secondRead = providers.Select(provider => provider.GetCurrentCensus()).ToArray();
        for (int i = 0; i < firstRead.Length; i++)
        {
            Assert.That(secondRead[i].SectionId, Is.EqualTo(firstRead[i].SectionId));
            Assert.That(secondRead[i].OwnerInstanceIdentity, Is.SameAs(firstRead[i].OwnerInstanceIdentity));
            Assert.That(secondRead[i].Cardinality, Is.EqualTo(firstRead[i].Cardinality));
            Assert.That(secondRead[i].Revision, Is.EqualTo(firstRead[i].Revision));
        }
    }

    [Test]
    public void ReceiptCensusTracksNewInstallReplayConflictAndStaleRejection()
    {
        CityRuntime city = CreateCity("receipt-city", 100);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            SettlementPopulationCensusProvider.CreateProviders(new[] { city });
        IOwnerSectionCensusProvider aggregateProvider = providers[0];
        IOwnerSectionCensusProvider receiptProvider = providers[1];
        SettlementPopulationTransition netZero = Propose(city.Population, new PopulationChangeSet(3, 3, 0, 0));

        Assert.That(TryApplyWithReceipt(city.Population,
            "operation-1", "fingerprint-1", netZero, out bool newlyApplied, out PopulationTransitionFailure applyFailure),
            Is.True, applyFailure.ToString());
        Assert.That(newlyApplied, Is.True);
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(100));
        AssertWitness(aggregateProvider, city.Population, 1, 1L);
        AssertWitness(receiptProvider, city.Population, 1, 1L);

        Assert.That(TryApplyWithReceipt(city.Population,
            "operation-1", "fingerprint-1", netZero, out newlyApplied, out applyFailure),
            Is.True, applyFailure.ToString());
        Assert.That(newlyApplied, Is.False);
        AssertWitness(aggregateProvider, city.Population, 1, 1L);
        AssertWitness(receiptProvider, city.Population, 1, 1L);

        Assert.That(TryApplyWithReceipt(city.Population,
            "operation-1", "different-fingerprint", netZero, out newlyApplied, out PopulationTransitionFailure conflictFailure),
            Is.False);
        Assert.That(newlyApplied, Is.False);
        Assert.That(conflictFailure, Is.EqualTo(PopulationTransitionFailure.OperationIdentityConflict));
        AssertWitness(aggregateProvider, city.Population, 1, 1L);
        AssertWitness(receiptProvider, city.Population, 1, 1L);

        SettlementPopulationTransition stale = Propose(city.Population, new PopulationChangeSet(1, 0, 0, 0));
        Assert.That(SettlementPopulationSystem.TryApply(
            city.Population,
            Propose(city.Population, new PopulationChangeSet(2, 0, 0, 0)),
            out _), Is.True);
        Assert.That(TryApplyWithReceipt(city.Population,
            "operation-stale", "fingerprint-stale", stale, out newlyApplied, out PopulationTransitionFailure staleFailure),
            Is.False);
        Assert.That(newlyApplied, Is.False);
        Assert.That(staleFailure, Is.EqualTo(PopulationTransitionFailure.StaleState));
        AssertWitness(aggregateProvider, city.Population, 1, 2L);
        AssertWitness(receiptProvider, city.Population, 1, 1L);
    }

    [Test]
    public void UnreceiptedNetZeroTransitionAdvancesAggregateRevisionWithoutChangingPopulation()
    {
        CityRuntime city = CreateCity("net-zero-city", 100);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            SettlementPopulationCensusProvider.CreateProviders(new[] { city });
        SettlementPopulationTransition transition = Propose(
            city.Population,
            new PopulationChangeSet(5, 5, 0, 0));

        Assert.That(SettlementPopulationSystem.TryApply(city.Population, transition, out PopulationTransitionFailure failure),
            Is.True, failure.ToString());

        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(100));
        AssertWitness(providers[0], city.Population, 1, 1L);
        AssertWitness(providers[1], city.Population, 0, 0L);
    }

    [Test]
    public void RestorePruningAdvancesReceiptRevisionOnceAndDistinguishesSameCardinalityReplacement()
    {
        CityRuntime city = CreateCity("rollback-city", 10);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            SettlementPopulationCensusProvider.CreateProviders(new[] { city });
        IOwnerSectionCensusProvider aggregateProvider = providers[0];
        IOwnerSectionCensusProvider receiptProvider = providers[1];
        SettlementPopulationTransition original = Propose(city.Population, new PopulationChangeSet(1, 0, 0, 0));

        Assert.That(TryApplyWithReceipt(city.Population,
            "original-operation", "original-fingerprint", original, out _, out PopulationTransitionFailure failure),
            Is.True, failure.ToString());
        OwnerSectionCensusWitness originalAggregate = aggregateProvider.GetCurrentCensus();
        OwnerSectionCensusWitness originalReceipts = receiptProvider.GetCurrentCensus();
        Assert.That(originalAggregate.Cardinality, Is.EqualTo(1));
        Assert.That(originalAggregate.Revision, Is.EqualTo(1L));
        Assert.That(originalReceipts.Cardinality, Is.EqualTo(1));
        Assert.That(originalReceipts.Revision, Is.EqualTo(1L));

        RestoreSnapshot(city.Population, 10, 0L);
        OwnerSectionCensusWitness restoredAggregate = aggregateProvider.GetCurrentCensus();
        OwnerSectionCensusWitness restoredReceipts = receiptProvider.GetCurrentCensus();
        Assert.That(restoredAggregate.Cardinality, Is.EqualTo(1));
        Assert.That(restoredAggregate.Revision, Is.Zero);
        Assert.That(restoredReceipts.Cardinality, Is.Zero);
        Assert.That(restoredReceipts.Revision, Is.EqualTo(2L));

        RestoreSnapshot(city.Population, 10, 0L);
        Assert.That(receiptProvider.GetCurrentCensus().Revision, Is.EqualTo(2L),
            "A restore that prunes no additional receipts must not advance the receipt-local revision.");

        SettlementPopulationTransition replacement = Propose(city.Population, new PopulationChangeSet(2, 0, 0, 0));
        Assert.That(TryApplyWithReceipt(city.Population,
            "replacement-operation", "replacement-fingerprint", replacement, out _, out failure),
            Is.True, failure.ToString());
        OwnerSectionCensusWitness replacementAggregate = aggregateProvider.GetCurrentCensus();
        OwnerSectionCensusWitness replacementReceipts = receiptProvider.GetCurrentCensus();
        Assert.That(replacementAggregate.Cardinality, Is.EqualTo(originalAggregate.Cardinality));
        Assert.That(replacementAggregate.Revision, Is.EqualTo(originalAggregate.Revision));
        Assert.That(replacementReceipts.Cardinality, Is.EqualTo(originalReceipts.Cardinality));
        Assert.That(replacementReceipts.Revision, Is.EqualTo(3L));
        Assert.That(replacementReceipts.Revision, Is.Not.EqualTo(originalReceipts.Revision));
    }

    [Test]
    public void PairedMigrationChangesBothAggregatesAndRejectedReplayLeavesAllWitnessesUnchanged()
    {
        CityRuntime origin = CreateCity("a-origin", 10);
        CityRuntime destination = CreateCity("z-destination", 4);
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            SettlementPopulationCensusProvider.CreateProviders(new[] { destination, origin });

        Assert.That(TryApplyPairedMigration(
            origin.Population,
            destination.Population,
            0L,
            0L,
            10,
            4,
            out PopulationTransitionFailure failure), Is.True, failure.ToString());
        Assert.That(origin.Population.CurrentPopulation, Is.EqualTo(9));
        Assert.That(destination.Population.CurrentPopulation, Is.EqualTo(5));
        AssertWitness(providers[0], origin.Population, 1, 1L);
        AssertWitness(providers[1], origin.Population, 0, 0L);
        AssertWitness(providers[2], destination.Population, 1, 1L);
        AssertWitness(providers[3], destination.Population, 0, 0L);
        OwnerSectionCensusWitness[] afterSuccess = providers.Select(provider => provider.GetCurrentCensus()).ToArray();

        Assert.That(TryApplyPairedMigration(
            origin.Population,
            destination.Population,
            0L,
            0L,
            10,
            4,
            out PopulationTransitionFailure staleFailure), Is.False);
        Assert.That(staleFailure, Is.EqualTo(PopulationTransitionFailure.StaleState));
        AssertWitnessesUnchanged(providers, afterSuccess);
    }

    private static CityRuntime CreateCity(string runtimeId, int initialPopulation)
    {
        CityData cityData = SimulationTestFactory.CreateCityData("definition-" + runtimeId);
        cityData.initialPopulation = initialPopulation;
        return new CityRuntime(
            runtimeId,
            cityData,
            new SpatialLocationRuntime("location-" + runtimeId));
    }

    private static SettlementPopulationTransition Propose(
        SettlementPopulationRuntime population,
        PopulationChangeSet changes)
    {
        Assert.That(SettlementPopulationSystem.TryPropose(
            population,
            changes,
            out SettlementPopulationTransition transition,
            out PopulationTransitionFailure failure), Is.True, failure.ToString());
        return transition;
    }

    private static bool TryApplyWithReceipt(
        SettlementPopulationRuntime population,
        string operationIdentity,
        string operationFingerprint,
        SettlementPopulationTransition transition,
        out bool newlyApplied,
        out PopulationTransitionFailure failure)
    {
        MethodInfo method = typeof(SettlementPopulationRuntime).GetMethod(
            "TryApplyTransitionWithReceipt",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments =
        {
            operationIdentity,
            operationFingerprint,
            transition,
            false,
            PopulationTransitionFailure.None
        };
        bool applied = (bool)method.Invoke(population, arguments);
        newlyApplied = (bool)arguments[3];
        failure = (PopulationTransitionFailure)arguments[4];
        return applied;
    }

    private static void RestoreSnapshot(SettlementPopulationRuntime population, int count, long expectedRevision)
    {
        MethodInfo method = typeof(SettlementPopulationRuntime).GetMethod(
            "RestoreSnapshot",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(population, new object[] { count, expectedRevision });
    }

    private static bool TryApplyPairedMigration(
        SettlementPopulationRuntime origin,
        SettlementPopulationRuntime destination,
        long originExpectedRevision,
        long destinationExpectedRevision,
        int originPopulationBefore,
        int destinationPopulationBefore,
        out PopulationTransitionFailure failure)
    {
        MethodInfo method = typeof(SettlementPopulationRuntime).GetMethod(
            "TryApplyPairedMigration",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments =
        {
            origin,
            destination,
            originExpectedRevision,
            destinationExpectedRevision,
            originPopulationBefore,
            destinationPopulationBefore,
            PopulationTransitionFailure.None
        };
        bool applied = (bool)method.Invoke(null, arguments);
        failure = (PopulationTransitionFailure)arguments[6];
        return applied;
    }

    private static void AssertInitialPair(
        IOwnerSectionCensusProvider aggregateProvider,
        IOwnerSectionCensusProvider receiptProvider,
        CityRuntime city)
    {
        OwnerSectionCensusWitness aggregate = aggregateProvider.GetCurrentCensus();
        OwnerSectionCensusWitness receipts = receiptProvider.GetCurrentCensus();
        Assert.That(aggregate.SchemaVersion, Is.EqualTo(SettlementPopulationCensusProvider.SchemaVersion));
        Assert.That(receipts.SchemaVersion, Is.EqualTo(SettlementPopulationCensusProvider.SchemaVersion));
        Assert.That(aggregate.OwnerInstanceIdentity, Is.SameAs(city.Population));
        Assert.That(receipts.OwnerInstanceIdentity, Is.SameAs(city.Population));
        Assert.That(aggregate.Cardinality, Is.EqualTo(1));
        Assert.That(aggregate.Revision, Is.Zero);
        Assert.That(receipts.Cardinality, Is.Zero);
        Assert.That(receipts.Revision, Is.Zero);
    }

    private static void AssertWitness(
        IOwnerSectionCensusProvider provider,
        SettlementPopulationRuntime owner,
        int cardinality,
        long revision)
    {
        OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
        Assert.That(witness.OwnerInstanceIdentity, Is.SameAs(owner));
        Assert.That(witness.Cardinality, Is.EqualTo(cardinality));
        Assert.That(witness.Revision, Is.EqualTo(revision));
    }

    private static void AssertWitnessesUnchanged(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        IReadOnlyList<OwnerSectionCensusWitness> expected)
    {
        Assert.That(providers.Count, Is.EqualTo(expected.Count));
        for (int i = 0; i < providers.Count; i++)
        {
            OwnerSectionCensusWitness actual = providers[i].GetCurrentCensus();
            Assert.That(actual.SectionId, Is.EqualTo(expected[i].SectionId));
            Assert.That(actual.SchemaVersion, Is.EqualTo(expected[i].SchemaVersion));
            Assert.That(actual.OwnerInstanceIdentity, Is.SameAs(expected[i].OwnerInstanceIdentity));
            Assert.That(actual.Cardinality, Is.EqualTo(expected[i].Cardinality));
            Assert.That(actual.Revision, Is.EqualTo(expected[i].Revision));
        }
    }
}
