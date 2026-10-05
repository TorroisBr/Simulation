using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class P12PopulationLifecycleInvalidationTests
{
    [SetUp]
    public void SetUp() => SimulationTestFactory.CleanupDefinitions();

    [TearDown]
    public void TearDown() => SimulationTestFactory.CleanupDefinitions();

    [Test]
    public void PopulationProvidersUseExactCityOwnersAndRejectEnabledDailyWriters()
    {
        CityRuntime first = CreateCity("p12/census:city-a", 10);
        CityRuntime second = CreateCity("p12/census:city-b", 20);
        CityRuntime[] cities = { first, second };
        IReadOnlyList<IOwnerSectionCensusProvider> providers =
            SettlementPopulationCensusProvider.CreateProviders(cities);

        Assert.That(providers.Count, Is.EqualTo(4));
        foreach (CityRuntime city in cities)
        {
            string encodedId = city.RuntimeId.Length + ":" + city.RuntimeId;
            string aggregateId = SettlementPopulationCensusProvider.AggregateSectionPrefix + encodedId;
            string receiptsId = SettlementPopulationCensusProvider.OperationReceiptsSectionPrefix + encodedId;
            OwnerSectionCensusWitness aggregate = FindWitness(providers, aggregateId);
            OwnerSectionCensusWitness receipts = FindWitness(providers, receiptsId);
            Assert.That(aggregate.SchemaVersion, Is.EqualTo(SettlementPopulationCensusProvider.SchemaVersion));
            Assert.That(aggregate.Cardinality, Is.EqualTo(1));
            Assert.That(aggregate.Revision, Is.EqualTo(city.Population.Revision));
            Assert.That(aggregate.OwnerInstanceIdentity, Is.SameAs(city.Population));
            Assert.That(receipts.OwnerInstanceIdentity, Is.SameAs(city.Population));
        }

        SimulationRuntime runtime = CreateP12Runtime(cities, null);
        Assert.That(runtime.Configuration.NaturalMortality.Enabled, Is.False);
        Assert.That(runtime.Configuration.AggregateDemography.Enabled, Is.False);
        AssertCensus(runtime);

        AssertP12ProfileRejects(CreateP12Configuration(naturalMortalityEnabled: true), "natural");
        AssertP12ProfileRejects(CreateP12Configuration(aggregateDemographyEnabled: true), "aggregate");
    }

    [Test]
    public void P12BoundRuntimeRejectsUnwitnessedNpcInjuryWithoutMutation()
    {
        CityRuntime city = CreateCity("p12-unwitnessed-injury-city", 10);
        NpcRuntime npc = CreateNpc("p12-unwitnessed-injury-npc");
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, new[] { npc });

        Assert.That(npc.TryApplyInjury(NpcInjurySeverity.SeriouslyInjured), Is.False,
            "The selected P12 profile has no owner section or operation for injury severity.");
        Assert.That(npc.InjurySeverity, Is.EqualTo(NpcInjurySeverity.None));
        AssertNpcRevision(runtime, npc.RuntimeId, residence: false, expected: 0L);
        AssertNpcRevision(runtime, npc.RuntimeId, residence: true, expected: 0L);
        AssertEpoch(runtime, 0L);
        AssertCensus(runtime);
    }

    [Test]
    public void SelectedDailyProfileTracksImmigrationEmigrationAndResidentDeathOnceEach()
    {
        CityRuntime city = CreateCity("p12-population-lifecycle-city", 10);
        NpcRuntime npc = CreateNpc("p12-population-lifecycle-npc");
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, new[] { npc });

        AssertEpoch(runtime, 0L);
        Assert.That(runtime.TryApplyImmigration(
            npc, city, out _, out NpcPopulationLifecycleFailure immigrationFailure),
            Is.True, immigrationFailure.ToString());
        AssertEpoch(runtime, 1L);
        Assert.That(city.CurrentPopulation, Is.EqualTo(11));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
        AssertNpcRevision(runtime, npc.RuntimeId, residence: true, expected: 1L);
        AssertCensus(runtime);

        Assert.That(runtime.TryApplyEmigration(
            npc, city, out _, out NpcPopulationLifecycleFailure emigrationFailure),
            Is.True, emigrationFailure.ToString());
        AssertEpoch(runtime, 2L);
        Assert.That(city.CurrentPopulation, Is.EqualTo(10));
        Assert.That(city.Population.Revision, Is.EqualTo(2L));
        AssertNpcRevision(runtime, npc.RuntimeId, residence: true, expected: 2L);
        AssertCensus(runtime);

        Assert.That(runtime.TryApplyImmigration(
            npc, city, out _, out immigrationFailure), Is.True, immigrationFailure.ToString());
        Assert.That(runtime.TryApplyResidentDeath(
            npc, city, out _, out NpcPopulationLifecycleFailure deathFailure),
            Is.True, deathFailure.ToString());
        AssertEpoch(runtime, 4L);
        Assert.That(npc.IsDead, Is.True);
        Assert.That(city.CurrentPopulation, Is.EqualTo(10));
        Assert.That(city.Population.Revision, Is.EqualTo(4L));
        AssertNpcRevision(runtime, npc.RuntimeId, residence: true, expected: 4L);
        AssertNpcRevision(runtime, npc.RuntimeId, residence: false, expected: 1L);
        AssertCensus(runtime);
    }

    [Test]
    public void PairedResidenceMigrationRequiresRuntimeBoundaryAndAdvancesOneEpoch()
    {
        CityRuntime origin = CreateCity("p12-migration-origin", 10);
        CityRuntime destination = CreateCity("p12-migration-destination", 20);
        NpcRuntime npc = CreateNpc("p12-migration-npc");
        Assert.That(SettlementPopulationMembershipSystem.TryBindExistingResident(
            origin,
            npc,
            SimulationTestFactory.CreateAuthoritativeNpcRoster(new[] { npc }),
            out PopulationMembershipFailure bindFailure), Is.True, bindFailure.ToString());
        SimulationRuntime runtime = CreateP12Runtime(new[] { origin, destination }, new[] { npc });
        Assert.That(NpcResidenceMigrationSystem.TryPropose(
            npc,
            origin,
            destination,
            runtime.GetAuthoritativeNpcRoster(),
            out NpcResidenceMigrationTransition transition,
            out NpcResidenceMigrationFailure proposeFailure), Is.True, proposeFailure.ToString());

        bool bypassed = NpcResidenceMigrationSystem.TryApply(
            npc,
            origin,
            destination,
            runtime.GetAuthoritativeNpcRoster(),
            transition,
            out _);
        Assert.That(bypassed, Is.False, "Static migration must not bypass the admitted runtime operation.");
        Assert.That(origin.CurrentPopulation, Is.EqualTo(10));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(20));
        Assert.That(origin.Population.Revision, Is.Zero);
        Assert.That(destination.Population.Revision, Is.Zero);
        AssertEpoch(runtime, 0L);
        AssertCensus(runtime);

        Assert.That(runtime.TryApplyResidenceMigration(
            npc,
            origin,
            destination,
            transition,
            out NpcResidenceMigrationFailure applyFailure), Is.True, applyFailure.ToString());
        AssertEpoch(runtime, 1L);
        Assert.That(origin.CurrentPopulation, Is.EqualTo(9));
        Assert.That(destination.CurrentPopulation, Is.EqualTo(21));
        Assert.That(origin.Population.Revision, Is.EqualTo(1L));
        Assert.That(destination.Population.Revision, Is.EqualTo(1L));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.EqualTo(destination.RuntimeId));
        AssertNpcRevision(runtime, npc.RuntimeId, residence: true, expected: 2L);
        AssertCensus(runtime);
    }

    [Test]
    public void PersonResidenceBindingAndReceiptFreeDeathUseCompleteOneEpochBoundaries()
    {
        CityRuntime city = CreateCity("p12-person-lifecycle-city", 5);
        PersonStore people = new PersonStore();
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, null, people);
        PersonRuntime person = new PersonRuntime(new PersonId("p12-person-lifecycle-person"), 0L);

        Assert.That(runtime.TryRegisterPerson(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        AssertEpoch(runtime, 1L);
        Assert.That(runtime.TryBindExistingPersonResident(
            person.PersonId, city, out PersonResidenceMembershipFailure bindFailure),
            Is.True, bindFailure.ToString());
        AssertEpoch(runtime, 2L);
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        AssertPersonRevision(runtime, person.PersonId, expected: 1L);

        Assert.That(runtime.TryProposePersonDeath(
            person.PersonId, out PersonDeathTransition proposed, out PersonDeathLifecycleFailure proposeFailure),
            Is.True, proposeFailure.ToString());
        PersonDeathTransition keyed = WithOperationIdentity(proposed, "p12-unsupported-keyed-death");
        Assert.That(runtime.TryApplyPersonDeath(keyed, out PersonDeathLifecycleFailure keyedFailure), Is.False);
        Assert.That(keyedFailure, Is.EqualTo(PersonDeathLifecycleFailure.RuntimeFaulted));
        Assert.That(PersonDeathLifecycleSystem.TryApplyDeath(runtime, keyed, out PersonDeathLifecycleFailure staticKeyedFailure), Is.False);
        Assert.That(staticKeyedFailure, Is.EqualTo(PersonDeathLifecycleFailure.RuntimeFaulted));
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
        AssertEpoch(runtime, 2L);

        Assert.That(runtime.TryApplyPersonDeath(
            person.PersonId, out _, out PersonDeathLifecycleFailure deathFailure), Is.True, deathFailure.ToString());
        AssertEpoch(runtime, 3L);
        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(0L));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
        AssertPersonRevision(runtime, person.PersonId, expected: 3L);
        AssertCensus(runtime);
    }

    [Test]
    public void MaterializedResidentDeathReconcilesPersonAndNpcOwnersInOneEpoch()
    {
        CityRuntime city = CreateCity("p12-person-backed-death-city", 5);
        PersonStore people = new PersonStore();
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, null, people);
        PersonRuntime person = new PersonRuntime(new PersonId("p12-person-backed-death-person"), 0L);

        Assert.That(runtime.TryRegisterPerson(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(runtime.TryBindExistingPersonResident(
            person.PersonId, city, out PersonResidenceMembershipFailure bindFailure),
            Is.True, bindFailure.ToString());
        long epochBeforeMaterialization = ReadEpoch(runtime);
        Assert.That(runtime.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc("p12-person-backed-death-definition"),
            "p12-person-backed-death-npc",
            null,
            0f,
            out NpcRuntime npc,
            out PersonMaterializationFailure materializationFailure), Is.True, materializationFailure.ToString());
        Assert.That(NpcLifecycleCensusProvider.CreateProviders(runtime.NpcRuntimes).Count, Is.EqualTo(1));
        AssertCensus(runtime);
        Assert.That(ReadEpoch(runtime), Is.EqualTo(epochBeforeMaterialization + 1L));

        long epochBeforeDeath = ReadEpoch(runtime);
        Assert.That(runtime.TryApplyResidentDeath(
            npc, city, out _, out NpcPopulationLifecycleFailure deathFailure), Is.True, deathFailure.ToString());
        Assert.That(ReadEpoch(runtime), Is.EqualTo(epochBeforeDeath + 1L));
        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(runtime.CurrentDay));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(npc.IsDead, Is.True);
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
        AssertPersonRevision(runtime, person.PersonId, expected: 3L);
        AssertNpcRevision(runtime, npc.RuntimeId, residence: false, expected: 1L);
        AssertCensus(runtime);
    }

    [Test]
    public void NamedBirthAndUnwrappedBoundOwnerWritesFailBeforeMutation()
    {
        CityRuntime city = CreateCity("p12-unsupported-birth-city", 7);
        NpcRuntime npc = CreateNpc("p12-unsupported-direct-owner-npc");
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, new[] { npc });

        Assert.That(runtime.TryApplyNamedBirth(
            city,
            new PersonId("p12-unsupported-birth"),
            out PersonBirthTransition birth,
            out PersonBirthLifecycleFailure birthFailure), Is.False);
        Assert.That(birth, Is.Null);
        Assert.That(birthFailure, Is.EqualTo(PersonBirthLifecycleFailure.RuntimeFaulted));
        Assert.That(city.CurrentPopulation, Is.EqualTo(7));
        Assert.That(city.Population.Revision, Is.Zero);

        Assert.That(runtime.TryProposeNamedBirth(
            city,
            new PersonId("p12-unsupported-direct-birth"),
            out PersonBirthTransition proposedBirth,
            out PersonBirthLifecycleFailure proposeFailure), Is.True, proposeFailure.ToString());
        Assert.That(PersonBirthLifecycleSystem.TryApplyNamedBirth(
            runtime, proposedBirth, out PersonBirthLifecycleFailure directBirthFailure), Is.False);
        Assert.That(directBirthFailure, Is.EqualTo(PersonBirthLifecycleFailure.RuntimeFaulted));

        PersonRuntime person = new PersonRuntime(new PersonId("p12-direct-membership-person"), 0L);
        Assert.That(runtime.TryRegisterPerson(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        long epochBeforeDirectMembership = ReadEpoch(runtime);
        Assert.That(PersonResidenceMembershipSystem.TryBindExistingResident(
            person, city, runtime, out PersonResidenceMembershipFailure directPersonBindFailure), Is.False);
        Assert.That(directPersonBindFailure, Is.EqualTo(PersonResidenceMembershipFailure.RuntimeFaulted));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(SettlementPopulationMembershipSystem.TryBindExistingResident(
            city,
            npc,
            runtime.GetAuthoritativeNpcRoster(),
            out PopulationMembershipFailure directNpcBindFailure), Is.False);
        Assert.That(directNpcBindFailure, Is.EqualTo(PopulationMembershipFailure.RuntimeFaulted));
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.Null);
        AssertEpoch(runtime, epochBeforeDirectMembership);

        Assert.That(npc.TryApplyDeath(), Is.False);
        Assert.That(npc.IsAlive, Is.True);
        AssertNpcRevision(runtime, npc.RuntimeId, residence: false, expected: 0L);
        AssertEpoch(runtime, epochBeforeDirectMembership);
        AssertCensus(runtime);
    }

    [Test]
    public void PopulationOperationReservesEpochAndLocalRevisionBeforeItsFirstWrite()
    {
        CityRuntime city = CreateCity("p12-reservation-city", 10);
        NpcRuntime npc = CreateNpc("p12-reservation-npc");
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, new[] { npc });

        SetMutationEpoch(runtime, long.MaxValue);
        Assert.That(runtime.TryApplyImmigration(
            npc, city, out _, out NpcPopulationLifecycleFailure epochFailure), Is.False);
        Assert.That(epochFailure, Is.EqualTo(NpcPopulationLifecycleFailure.RuntimeFaulted));
        Assert.That(city.CurrentPopulation, Is.EqualTo(10));
        Assert.That(city.Population.Revision, Is.Zero);
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.Null);
        AssertNpcRevision(runtime, npc.RuntimeId, residence: true, expected: 0L);

        CityRuntime saturatedCity = CreateCity("p12-local-revision-city", 10);
        NpcRuntime secondNpc = CreateNpc("p12-local-revision-npc");
        SetPopulationRevision(saturatedCity.Population, long.MaxValue);
        SimulationRuntime secondRuntime = CreateP12Runtime(new[] { saturatedCity }, new[] { secondNpc });

        Assert.That(secondRuntime.TryApplyImmigration(
            secondNpc, saturatedCity, out _, out NpcPopulationLifecycleFailure revisionFailure), Is.False);
        Assert.That(revisionFailure, Is.EqualTo(NpcPopulationLifecycleFailure.RuntimeFaulted));
        Assert.That(saturatedCity.CurrentPopulation, Is.EqualTo(10));
        Assert.That(saturatedCity.Population.Revision, Is.EqualTo(long.MaxValue));
        Assert.That(secondNpc.ResidenceSettlementRuntimeId, Is.Null);
        AssertNpcRevision(secondRuntime, secondNpc.RuntimeId, residence: true, expected: 0L);
        AssertEpoch(secondRuntime, 0L);
        AssertCensus(secondRuntime);
    }

    [Test]
    public void PersonAndNpcLocalRevisionSaturationRejectsLifecycleWritesBeforeMutation()
    {
        CityRuntime npcCity = CreateCity("p12-npc-local-revision-city", 10);
        NpcRuntime npc = CreateNpc("p12-npc-local-revision-owner");
        SetNpcResidenceRevision(npc, long.MaxValue);
        SimulationRuntime npcRuntime = CreateP12Runtime(new[] { npcCity }, new[] { npc });
        Assert.That(npcRuntime.TryApplyImmigration(
            npc, npcCity, out _, out NpcPopulationLifecycleFailure npcFailure), Is.False);
        Assert.That(npcFailure, Is.EqualTo(NpcPopulationLifecycleFailure.RuntimeFaulted));
        Assert.That(npcCity.CurrentPopulation, Is.EqualTo(10));
        Assert.That(npcCity.Population.Revision, Is.Zero);
        Assert.That(npc.ResidenceSettlementRuntimeId, Is.Null);
        AssertEpoch(npcRuntime, 0L);
        AssertCensus(npcRuntime);

        CityRuntime personCity = CreateCity("p12-person-local-revision-city", 10);
        PersonRuntime person = new PersonRuntime(new PersonId("p12-person-local-revision-owner"), 0L);
        PersonStore personStore = new PersonStore();
        Assert.That(personStore.TryRegister(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        SetPersonLifeResidenceRevision(person, long.MaxValue);
        SimulationRuntime personRuntime = CreateP12Runtime(new[] { personCity }, null, personStore);
        Assert.That(personRuntime.TryBindExistingPersonResident(
            person.PersonId, personCity, out PersonResidenceMembershipFailure personFailure), Is.False);
        Assert.That(personFailure, Is.EqualTo(PersonResidenceMembershipFailure.RuntimeFaulted));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(personCity.Population.Revision, Is.Zero);
        AssertEpoch(personRuntime, 0L);
        AssertCensus(personRuntime);
    }

    private static SimulationRuntime CreateP12Runtime(
        IReadOnlyList<CityRuntime> cities,
        IReadOnlyList<NpcRuntime> npcs,
        PersonStore people = null,
        EffectiveSimulationConfiguration configuration = null)
    {
        configuration = configuration ?? CreateP12Configuration();
        return new SimulationRuntime(
            new SimulationTime(),
            cities,
            npcs,
            configuration: configuration,
            personStore: people,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
    }

    private static EffectiveSimulationConfiguration CreateP12Configuration(
        bool naturalMortalityEnabled = false,
        bool aggregateDemographyEnabled = false)
    {
        EffectiveSimulationConfiguration defaults = SimulationConfigurationDefaults.Create();
        return new EffectiveSimulationConfiguration(
            defaults.Population,
            new EffectiveEconomyConfiguration(false),
            defaults.Travel,
            defaults.Crime,
            defaults.GuardCrime,
            new EffectiveNaturalMortalityConfiguration(enabled: naturalMortalityEnabled),
            new EffectiveAggregateDemographyConfiguration(enabled: aggregateDemographyEnabled),
            defaults.MerchantTrade,
            defaults.CommercialKnowledge);
    }

    private static void AssertP12ProfileRejects(
        EffectiveSimulationConfiguration configuration,
        string runtimeId)
    {
        CityRuntime city = CreateCity("p12-profile-reject-" + runtimeId, 5);
        Assert.Throws<InvalidOperationException>(() => new SimulationRuntime(
            new SimulationTime(),
            new[] { city },
            null,
            configuration: configuration,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1()));
    }

    private static CityRuntime CreateCity(string runtimeId, int population)
    {
        CityData definition = SimulationTestFactory.CreateCityData("definition-" + runtimeId);
        definition.initialPopulation = population;
        return new CityRuntime(runtimeId, definition, new SpatialLocationRuntime("location-" + runtimeId));
    }

    private static NpcRuntime CreateNpc(string runtimeId) =>
        new NpcRuntime(runtimeId, SimulationTestFactory.CreateNpc(runtimeId));

    private static PersonDeathTransition WithOperationIdentity(
        PersonDeathTransition transition,
        string operationIdentity)
    {
        MethodInfo method = typeof(PersonDeathTransition).GetMethod(
            "WithOperationIdentity", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        return (PersonDeathTransition)method.Invoke(transition, new object[] { operationIdentity });
    }

    private static void AssertEpoch(SimulationRuntime runtime, long expected)
    {
        Assert.That(ReadEpoch(runtime), Is.EqualTo(expected));
    }

    private static long ReadEpoch(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryReadNpcRosterCensusMutationEpoch(
            out long epoch, out ContinuationCensusFailure failure), Is.True, failure.ToString());
        return epoch;
    }

    private static OwnerSectionCensusWitness FindWitness(
        IReadOnlyList<IOwnerSectionCensusProvider> providers,
        string sectionId)
    {
        foreach (IOwnerSectionCensusProvider provider in providers)
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (witness.SectionId == sectionId) return witness;
        }
        Assert.Fail("Expected census provider was not present: " + sectionId);
        return null;
    }

    private static void AssertCensus(SimulationRuntime runtime)
    {
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure failure), Is.True,
            failure.ToString());
    }

    private static void AssertNpcRevision(
        SimulationRuntime runtime,
        string runtimeId,
        bool residence,
        long expected)
    {
        foreach (IOwnerSectionCensusProvider provider in NpcLifecycleCensusProvider.CreateProviders(runtime.NpcRuntimes))
        {
            string id = provider.GetCurrentCensus().SectionId;
            if (id == NpcLifecycleCensusProvider.SectionIdFor(runtimeId, residence))
            {
                Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(expected));
                return;
            }
        }
        Assert.Fail("Expected NPC lifecycle provider was not present.");
    }

    private static void AssertPersonRevision(SimulationRuntime runtime, PersonId personId, long expected)
    {
        foreach (IOwnerSectionCensusProvider provider in PersonLifeResidenceCensusProvider.CreateProviders(runtime.PersonStore.Persons))
        {
            if (provider.GetCurrentCensus().SectionId == PersonLifeResidenceCensusProvider.SectionIdFor(personId))
            {
                Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(expected));
                return;
            }
        }
        Assert.Fail("Expected Person life/residence provider was not present.");
    }

    private static void SetMutationEpoch(SimulationRuntime runtime, long value)
    {
        FieldInfo protocolField = typeof(SimulationRuntime).GetField(
            "npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(protocolField, Is.Not.Null);
        ContinuationCensusProtocol protocol = (ContinuationCensusProtocol)protocolField.GetValue(runtime);
        FieldInfo epochField = typeof(ContinuationCensusProtocol).GetField(
            "mutationEpoch", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(epochField, Is.Not.Null);
        epochField.SetValue(protocol, value);
    }

    private static void SetPopulationRevision(SettlementPopulationRuntime population, long value)
    {
        FieldInfo revision = typeof(SettlementPopulationRuntime).GetField(
            "revision", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(revision, Is.Not.Null);
        revision.SetValue(population, value);
    }

    private static void SetPersonLifeResidenceRevision(PersonRuntime person, long value)
    {
        FieldInfo revision = typeof(PersonRuntime).GetField(
            "lifeResidenceRevision", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(revision, Is.Not.Null);
        revision.SetValue(person, value);
    }

    private static void SetNpcResidenceRevision(NpcRuntime npc, long value)
    {
        FieldInfo revision = typeof(NpcRuntime).GetField(
            "residenceRevision", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(revision, Is.Not.Null);
        revision.SetValue(npc, value);
    }
}
