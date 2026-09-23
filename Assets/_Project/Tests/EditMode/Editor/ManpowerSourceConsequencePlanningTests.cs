using System;
using NUnit.Framework;

public sealed class ManpowerSourceConsequencePlanningTests
{
    [SetUp]
    public void SetUp()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [TearDown]
    public void TearDown()
    {
        SimulationTestFactory.CleanupDefinitions();
    }

    [Test]
    public void UnconfiguredSourceIsUnsupportedRatherThanImpliedByRuntime()
    {
        CityRuntime city = CreateCity("unconfigured-settlement", 100);
        SimulationRuntime world = CreateWorld(new[] { city });

        ManpowerSourceConsequencePlanningResult result = world.ManpowerSourceConsequencePlanningService.TryPlan(
            Request("unconfigured-source", 10L));

        Assert.That(result.Status, Is.EqualTo(ManpowerSourceConsequencePlanningStatus.Unsupported));
        Assert.That(result.Failure.Code, Is.EqualTo(ManpowerSourceConsequenceFailureCode.SourceNotRegistered));
        Assert.That(result.Proposal, Is.Null);
    }

    [Test]
    public void GenericD6ASnapshotDoesNotImplyD6B1PlannerConfiguration()
    {
        ManpowerSourceId sourceId = new ManpowerSourceId("generic-d6a-only-source");
        TestSnapshotProvider provider = new TestSnapshotProvider();
        provider.Set(sourceId, 400L, 100L, "d6a-snapshot-v1");
        SimulationRuntime world = CreateWorld(
            new[] { CreateCity("generic-provider-city", 100) },
            provider: provider);

        ManpowerSourceConsequencePlanningResult result = world.ManpowerSourceConsequencePlanningService.TryPlan(
            new ManpowerSourceConsequenceRequest(sourceId, Death(10L)));

        Assert.That(result.Status, Is.EqualTo(ManpowerSourceConsequencePlanningStatus.Unsupported));
        Assert.That(result.Failure.Code, Is.EqualTo(ManpowerSourceConsequenceFailureCode.PlannerNotConfigured));
    }

    [Test]
    public void SourceRoutingUsesExactRegistrationAndNeverParsesSettlementLikeIds()
    {
        CityRuntime north = CreateCity("settlement-north", 100);
        CityRuntime south = CreateCity("settlement-south", 50);
        ManpowerSourceId northSource = new ManpowerSourceId("opaque-source-17");
        ManpowerSourceId southSource = new ManpowerSourceId("opaque-source-03");
        SimulationRuntime world = CreateWorld(
            new[] { north, south },
            new[]
            {
                new SettlementManpowerSourceRegistration(northSource, south, 700L),
                new SettlementManpowerSourceRegistration(southSource, north, 900L)
            });

        ManpowerSourceConsequencePlanningResult routed = world.ManpowerSourceConsequencePlanningService.TryPlan(
            new ManpowerSourceConsequenceRequest(northSource, Death(7L)));
        ManpowerSourceConsequencePlanningResult cityIdOnly = world.ManpowerSourceConsequencePlanningService.TryPlan(
            Request("settlement-south", 7L));

        Assert.That(routed.IsPlanned, Is.True, routed.Failure.ToString());
        Assert.That(routed.Proposal.SettlementPopulationDeath.SettlementRuntimeId, Is.EqualTo("settlement-south"));
        Assert.That(routed.Proposal.SettlementPopulationDeath.PopulationBefore, Is.EqualTo(50));
        Assert.That(cityIdOnly.Status, Is.EqualTo(ManpowerSourceConsequencePlanningStatus.Unsupported));
        Assert.That(cityIdOnly.Failure.Code, Is.EqualTo(ManpowerSourceConsequenceFailureCode.SourceNotRegistered));
    }

    [Test]
    public void MatchingSettlementStringInGenericSnapshotStillDoesNotCreatePlannerRoute()
    {
        CityRuntime city = CreateCity("looks-like-a-city-id", 100);
        ManpowerSourceId cityNamedSource = new ManpowerSourceId(city.RuntimeId);
        TestSnapshotProvider provider = new TestSnapshotProvider();
        provider.Set(cityNamedSource, 100L, 100L, "snapshot-is-not-a-planner");
        SimulationRuntime world = CreateWorld(new[] { city }, provider: provider);

        ManpowerSourceConsequencePlanningResult result = world.ManpowerSourceConsequencePlanningService.TryPlan(
            new ManpowerSourceConsequenceRequest(cityNamedSource, Death(1L)));

        Assert.That(result.Status, Is.EqualTo(ManpowerSourceConsequencePlanningStatus.Unsupported));
        Assert.That(result.Failure.Code, Is.EqualTo(ManpowerSourceConsequenceFailureCode.PlannerNotConfigured));
    }

    [Test]
    public void SettlementDeathProposalCapturesTypedTransitionAndValidatesWithoutMutation()
    {
        CityRuntime city = CreateCity("death-proposal-city", 100);
        PersonStore persons = new PersonStore();
        SimulationRuntime world = CreateWorld(
            new[] { city },
            new[] { Registration("death-source", city, 400L) },
            personStore: persons);
        AddResidents(world, city, 20, "death-proposal-resident");
        long revisionBefore = city.Population.Revision;
        int peopleBefore = persons.Persons.Count;
        long dayBefore = world.CurrentDay;

        ManpowerSourceConsequencePlanningResult result = world.ManpowerSourceConsequencePlanningService.TryPlan(
            Request("death-source", 10L));
        ManpowerSourceConsequenceValidationResult validation =
            world.ManpowerSourceConsequencePlanningService.TryValidateCurrent(result.Proposal);

        Assert.That(result.Status, Is.EqualTo(ManpowerSourceConsequencePlanningStatus.Planned), result.Failure.ToString());
        Assert.That(result.Proposal.Disposition, Is.EqualTo(ManpowerSourceConsequenceDisposition.DomainTransitionRequired));
        Assert.That(result.Proposal.SettlementPopulationDeath, Is.Not.Null);
        Assert.That(result.Proposal.SettlementPopulationDeath.SourceId.Value, Is.EqualTo("death-source"));
        Assert.That(result.Proposal.SettlementPopulationDeath.SettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(result.Proposal.SettlementPopulationDeath.ExpectedPopulationRevision, Is.EqualTo(revisionBefore));
        Assert.That(result.Proposal.SettlementPopulationDeath.PopulationBefore, Is.EqualTo(100));
        Assert.That(result.Proposal.SettlementPopulationDeath.RepresentedResidentFloor, Is.EqualTo(20));
        Assert.That(result.Proposal.SettlementPopulationDeath.Deaths, Is.EqualTo(10));
        Assert.That(result.Proposal.SettlementPopulationDeath.PopulationAfter, Is.EqualTo(90));
        Assert.That(result.Proposal.SettlementPopulationDeath.Transition.Births, Is.Zero);
        Assert.That(result.Proposal.SettlementPopulationDeath.Transition.Deaths, Is.EqualTo(10));
        Assert.That(result.Proposal.PlannerRuleKey, Is.Not.EqualTo(result.Proposal.SourceId.Value));
        Assert.That(result.Proposal.PlannerRuleKey, Is.EqualTo("settlement.aggregate-population-death"));
        Assert.That(result.Proposal.PlannerConfigurationIdentity,
            Is.EqualTo("deaths:int32;represented-resident-floor:v1"));
        Assert.That(result.Proposal.ProposalVersion, Is.EqualTo(1));
        Assert.That(result.Proposal.DependencyFingerprint, Does.Match("^[0-9a-f]{64}$"));
        Assert.That(validation.Status, Is.EqualTo(ManpowerSourceConsequenceValidationStatus.Current));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(100));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
        Assert.That(persons.Persons.Count, Is.EqualTo(peopleBefore));
        Assert.That(world.CurrentDay, Is.EqualTo(dayBefore));
    }

    [TestCase(80L, true)]
    [TestCase(81L, false)]
    public void DeathAmountMayReachButNotCrossRepresentedResidentFloor(long amount, bool expectedSuccess)
    {
        CityRuntime city = CreateCity("death-floor-boundary", 100);
        SimulationRuntime world = CreateWorld(
            new[] { city },
            new[] { Registration("floor-source", city, 500L) });
        AddResidents(world, city, 20, "floor-boundary-resident");

        ManpowerSourceConsequencePlanningResult result = world.ManpowerSourceConsequencePlanningService.TryPlan(
            Request("floor-source", amount));

        Assert.That(result.IsPlanned, Is.EqualTo(expectedSuccess), result.Failure.ToString());
        if (expectedSuccess)
        {
            Assert.That(result.Proposal.SettlementPopulationDeath.PopulationAfter, Is.EqualTo(20));
        }
        else
        {
            Assert.That(result.Failure.Code, Is.EqualTo(ManpowerSourceConsequenceFailureCode.AggregateDemographyRejected));
            Assert.That(result.Failure.AggregateFailure,
                Is.EqualTo(AggregateDemographyFailure.WouldViolateRepresentedResidentFloor));
        }
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(100));
        Assert.That(city.Population.Revision, Is.Zero);
    }

    [Test]
    public void DeathAmountBeyondInt32IsRejectedWithoutTruncationOrMutation()
    {
        CityRuntime city = CreateCity("death-wide-amount", int.MaxValue);
        SimulationRuntime world = CreateWorld(
            new[] { city },
            new[] { Registration("wide-source", city, long.MaxValue) });
        int populationBefore = city.Population.CurrentPopulation;
        long revisionBefore = city.Population.Revision;

        ManpowerSourceConsequencePlanningResult result = world.ManpowerSourceConsequencePlanningService.TryPlan(
            Request("wide-source", (long)int.MaxValue + 1L));

        Assert.That(result.Status, Is.EqualTo(ManpowerSourceConsequencePlanningStatus.Failed));
        Assert.That(result.Failure.Code, Is.EqualTo(ManpowerSourceConsequenceFailureCode.AmountNotRepresentable));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(populationBefore));
        Assert.That(city.Population.Revision, Is.EqualTo(revisionBefore));
    }

    [TestCase(0L)]
    [TestCase(-1L)]
    public void NonPositiveDeathAmountIsRejected(long amount)
    {
        CityRuntime city = CreateCity("nonpositive-death", 100);
        SimulationRuntime world = CreateWorld(
            new[] { city },
            new[] { Registration("nonpositive-source", city, 50L) });

        ManpowerSourceConsequencePlanningResult result = world.ManpowerSourceConsequencePlanningService.TryPlan(
            Request("nonpositive-source", amount));

        Assert.That(result.Status, Is.EqualTo(ManpowerSourceConsequencePlanningStatus.Failed));
        Assert.That(result.Failure.Code, Is.EqualTo(ManpowerSourceConsequenceFailureCode.InvalidAmount));
    }

    [Test]
    public void UnsupportedEffectKindIsDistinctFromInvalidRequest()
    {
        CityRuntime city = CreateCity("unsupported-effect-city", 20);
        SimulationRuntime world = CreateWorld(
            new[] { city },
            new[] { Registration("unsupported-effect-source", city, 15L) });

        ManpowerSourceConsequencePlanningResult result = world.ManpowerSourceConsequencePlanningService.TryPlan(
            new ManpowerSourceConsequenceRequest(
                new ManpowerSourceId("unsupported-effect-source"),
                new ManpowerSourceEffect((ManpowerSourceEffectKind)12, 1L)));

        Assert.That(result.Status, Is.EqualTo(ManpowerSourceConsequencePlanningStatus.Unsupported));
        Assert.That(result.Failure.Code, Is.EqualTo(ManpowerSourceConsequenceFailureCode.UnsupportedEffect));
    }

    [Test]
    public void PopulationRevisionAndCurrentAmountChangesMakeProposalStale()
    {
        CityRuntime city = CreateCity("population-stale-city", 100);
        SimulationRuntime world = CreateWorld(
            new[] { city },
            new[] { Registration("population-stale-source", city, 50L) });
        ManpowerSourceConsequenceProposal proposal = Plan(world, "population-stale-source", 10L);

        ApplyPopulationChange(city.Population, new PopulationChangeSet(1, 0, 0, 0));

        ManpowerSourceConsequenceValidationResult validation =
            world.ManpowerSourceConsequencePlanningService.TryValidateCurrent(proposal);
        Assert.That(validation.Status, Is.EqualTo(ManpowerSourceConsequenceValidationStatus.Stale));
        Assert.That(validation.Reason, Is.EqualTo(ManpowerSourceConsequenceValidationReason.SettlementPopulationChanged));
    }

    [Test]
    public void RepresentedResidentFloorChangeIsStaleEvenWhenPopulationRevisionIsUnchanged()
    {
        CityRuntime city = CreateCity("floor-stale-city", 100);
        PersonStore persons = new PersonStore();
        SimulationRuntime world = CreateWorld(
            new[] { city },
            new[] { Registration("floor-stale-source", city, 75L) },
            personStore: persons);
        ManpowerSourceConsequenceProposal proposal = Plan(world, "floor-stale-source", 10L);
        long populationRevision = city.Population.Revision;
        AddResidents(world, city, 1, "floor-stale-resident");

        ManpowerSourceConsequenceValidationResult validation =
            world.ManpowerSourceConsequencePlanningService.TryValidateCurrent(proposal);

        Assert.That(city.Population.Revision, Is.EqualTo(populationRevision));
        Assert.That(validation.Status, Is.EqualTo(ManpowerSourceConsequenceValidationStatus.Stale));
        Assert.That(validation.Reason, Is.EqualTo(ManpowerSourceConsequenceValidationReason.RepresentedResidentFloorChanged));
    }

    [Test]
    public void MutationToUnrelatedSettlementDoesNotStaleSourceProposal()
    {
        CityRuntime sourceCity = CreateCity("unrelated-source-city", 100);
        CityRuntime otherCity = CreateCity("unrelated-other-city", 80);
        SimulationRuntime world = CreateWorld(
            new[] { sourceCity, otherCity },
            new[]
            {
                Registration("source-one", sourceCity, 50L),
                Registration("source-two", otherCity, 70L)
            });
        ManpowerSourceConsequenceProposal proposal = Plan(world, "source-one", 10L);
        ApplyPopulationChange(otherCity.Population, new PopulationChangeSet(3, 0, 0, 0));

        ManpowerSourceConsequenceValidationResult validation =
            world.ManpowerSourceConsequencePlanningService.TryValidateCurrent(proposal);

        Assert.That(validation.Status, Is.EqualTo(ManpowerSourceConsequenceValidationStatus.Current));
    }

    [Test]
    public void CapacityIsSeparateFromPopulationAndDoesNotEnterConsequenceFreshness()
    {
        ManpowerSourceConsequenceProposal lowCapacityProposal = CreateCapacityComparisonProposal(25L, out ManpowerSourceCapacitySnapshot lowSnapshot);
        ManpowerSourceConsequenceProposal highCapacityProposal = CreateCapacityComparisonProposal(900L, out ManpowerSourceCapacitySnapshot highSnapshot);

        Assert.That(lowSnapshot.Capacity, Is.EqualTo(25L));
        Assert.That(highSnapshot.Capacity, Is.EqualTo(900L));
        Assert.That(lowSnapshot.FactualLivingAmount, Is.EqualTo(100L));
        Assert.That(highSnapshot.FactualLivingAmount, Is.EqualTo(100L));
        Assert.That(lowSnapshot.Fingerprint, Is.Not.EqualTo(highSnapshot.Fingerprint));
        Assert.That(lowCapacityProposal.DependencyFingerprint, Is.EqualTo(highCapacityProposal.DependencyFingerprint));
    }

    [Test]
    public void D6ASnapshotRemainsCoherentWhenSettlementPopulationChanges()
    {
        CityRuntime city = CreateCity("snapshot-coherence-city", 100);
        SimulationRuntime world = CreateWorld(
            new[] { city },
            new[] { Registration("snapshot-coherence-source", city, 750L) });
        IManpowerSourceSnapshotProvider provider = world.ContingentManpowerStateStore.SourceProvider;
        ManpowerSourceId sourceId = new ManpowerSourceId("snapshot-coherence-source");
        Assert.That(provider.TryGetSnapshot(sourceId, out ManpowerSourceCapacitySnapshot before), Is.True);

        ApplyPopulationChange(city.Population, new PopulationChangeSet(3, 0, 0, 0));

        Assert.That(provider.TryGetSnapshot(sourceId, out ManpowerSourceCapacitySnapshot after), Is.True);
        Assert.That(before.Capacity, Is.EqualTo(750L));
        Assert.That(after.Capacity, Is.EqualTo(750L));
        Assert.That(before.FactualLivingAmount, Is.EqualTo(100L));
        Assert.That(after.FactualLivingAmount, Is.EqualTo(103L));
        Assert.That(before.Fingerprint, Is.Not.EqualTo(after.Fingerprint));
    }

    [Test]
    public void DuplicateSourceIdsAndDuplicateSettlementAuthoritiesAreRejectedAtComposition()
    {
        CityRuntime first = CreateCity("duplicate-source-first", 30);
        CityRuntime second = CreateCity("duplicate-source-second", 40);

        ArgumentException duplicateSource = Assert.Throws<ArgumentException>(() => CreateWorld(
            new[] { first, second },
            new[]
            {
                Registration("duplicate-source-id", first, 1L),
                Registration("duplicate-source-id", second, 2L)
            }));
        Assert.That(duplicateSource.Message, Does.Contain("Duplicate ManpowerSourceId"));

        ArgumentException duplicateAuthority = Assert.Throws<ArgumentException>(() => CreateWorld(
            new[] { first, second },
            new[]
            {
                Registration("authority-source-a", first, 1L),
                Registration("authority-source-b", first, 2L)
            }));
        Assert.That(duplicateAuthority.Message, Does.Contain("same settlement population authority"));
    }

    [Test]
    public void RegistrationRequiresExactSettlementObjectFromComposedWorld()
    {
        CityRuntime worldCity = CreateCity("world-city-instance", 50);
        CityRuntime differentInstance = CreateCity("world-city-instance", 50);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => CreateWorld(
            new[] { worldCity },
            new[] { Registration("wrong-city-instance-source", differentInstance, 20L) }));

        Assert.That(exception.Message, Does.Contain("exact CityRuntime"));
    }

    [Test]
    public void SettlementRegistrationCannotSilentlyReplaceDifferentGenericD6AProvider()
    {
        CityRuntime city = CreateCity("conflicting-provider-city", 50);
        TestSnapshotProvider provider = new TestSnapshotProvider();
        provider.Set(new ManpowerSourceId("conflicting-provider-source"), 10L, 50L, "generic");

        Assert.Throws<ArgumentException>(() => CreateWorld(
            new[] { city },
            new[] { Registration("conflicting-provider-source", city, 10L) },
            provider: provider));
    }

    [Test]
    public void SettlementRegistryCannotCrossSimulationRuntimeBoundaries()
    {
        CityRuntime firstCity = CreateCity("cross-runtime-city", 100);
        SimulationRuntime firstWorld = CreateWorld(
            new[] { firstCity },
            new[] { Registration("cross-runtime-source", firstCity, 40L) });
        IManpowerSourceSnapshotProvider firstWorldProvider = firstWorld.ContingentManpowerStateStore.SourceProvider;
        CityRuntime secondCity = CreateCity("cross-runtime-city", 100);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => CreateWorld(
            new[] { secondCity },
            provider: firstWorldProvider));

        Assert.That(exception.Message, Does.Contain("cannot be reused across SimulationRuntime compositions"));
    }

    [Test]
    public void PlannerIdentityIsCapturedAtCompositionAndLaterChangeIsRejected()
    {
        CityRuntime city = CreateCity("planner-identity-city", 100);
        MutablePlannerIdentity identity = new MutablePlannerIdentity(
            "settlement.aggregate-population-death",
            "deaths:int32;represented-resident-floor:v1",
            1);
        SimulationRuntime world = CreateWorld(
            new[] { city },
            new[]
            {
                new SettlementManpowerSourceRegistration(
                    new ManpowerSourceId("planner-identity-source"),
                    city,
                    100L,
                    identity)
            });
        ManpowerSourceConsequenceProposal proposal = Plan(world, "planner-identity-source", 5L);
        identity.RuleKey = "settlement.aggregate-population-death.changed";

        ManpowerSourceConsequenceValidationResult validation =
            world.ManpowerSourceConsequencePlanningService.TryValidateCurrent(proposal);
        ManpowerSourceConsequencePlanningResult laterPlan = world.ManpowerSourceConsequencePlanningService.TryPlan(
            Request("planner-identity-source", 5L));

        Assert.That(validation.Status, Is.EqualTo(ManpowerSourceConsequenceValidationStatus.Stale));
        Assert.That(validation.Reason, Is.EqualTo(ManpowerSourceConsequenceValidationReason.PlannerIdentityChanged));
        Assert.That(laterPlan.Status, Is.EqualTo(ManpowerSourceConsequencePlanningStatus.Failed));
        Assert.That(laterPlan.Failure.Code, Is.EqualTo(ManpowerSourceConsequenceFailureCode.PlannerIdentityChanged));
    }

    [Test]
    public void PlanningAndValidationLeavePopulationMilitaryPeopleBattlesAndTimeUntouched()
    {
        CityRuntime city = CreateCity("no-mutation-city", 100);
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId forceId = new ArmedForceId("no-mutation-force");
        ContingentId contingentId = new ContingentId("no-mutation-contingent");
        Assert.That(forces.TryRegister(new ArmedForceRecord(forceId, "No mutation", 0L), out _), Is.True);
        Assert.That(forces.TryRegisterContingent(new ContingentRecord(
            contingentId,
            forceId,
            12L,
            new ContingentOriginReference("legacy", "no-mutation"),
            "service"), out _), Is.True);
        SimulationRuntime world = CreateWorld(
            new[] { city },
            new[] { Registration("no-mutation-source", city, 60L) },
            personStore: persons,
            armedForceStore: forces,
            day: 17L);
        AddResidents(world, city, 1, "no-mutation-resident");
        long populationRevision = city.Population.Revision;
        int population = city.Population.CurrentPopulation;
        long manpowerRevision = world.ContingentManpowerStateStore.Revision;
        int rosterCount = world.ContingentManpowerStateStore.States.Count;
        int personCount = persons.Persons.Count;
        int npcCount = world.NpcRuntimes.Count;
        int battleCount = world.BattleStore.Count;
        Assert.That(world.ArmedForceStore.TryGetContingent(contingentId, out ContingentRecord contingentBefore), Is.True);

        ManpowerSourceConsequenceProposal proposal = Plan(world, "no-mutation-source", 5L);
        ManpowerSourceConsequenceValidationResult validation =
            world.ManpowerSourceConsequencePlanningService.TryValidateCurrent(proposal);

        Assert.That(validation.Status, Is.EqualTo(ManpowerSourceConsequenceValidationStatus.Current));
        Assert.That(city.Population.CurrentPopulation, Is.EqualTo(population));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevision));
        Assert.That(world.ContingentManpowerStateStore.Revision, Is.EqualTo(manpowerRevision));
        Assert.That(world.ContingentManpowerStateStore.States.Count, Is.EqualTo(rosterCount));
        Assert.That(persons.Persons.Count, Is.EqualTo(personCount));
        Assert.That(world.NpcRuntimes.Count, Is.EqualTo(npcCount));
        Assert.That(world.BattleStore.Count, Is.EqualTo(battleCount));
        Assert.That(world.ArmedForceStore.TryGetContingent(contingentId, out ContingentRecord contingentAfter), Is.True);
        Assert.That(contingentAfter.Amount, Is.EqualTo(contingentBefore.Amount));
        Assert.That(world.CurrentDay, Is.EqualTo(17L));
    }

    [Test]
    public void RepeatedUnsupportedDiagnosticsAreDeterministic()
    {
        CityRuntime city = CreateCity("diagnostic-city", 20);
        SimulationRuntime world = CreateWorld(new[] { city });
        ManpowerSourceConsequenceRequest request = Request("not-registered", 1L);

        ManpowerSourceConsequencePlanningResult first = world.ManpowerSourceConsequencePlanningService.TryPlan(request);
        ManpowerSourceConsequencePlanningResult second = world.ManpowerSourceConsequencePlanningService.TryPlan(request);

        Assert.That(first.Status, Is.EqualTo(second.Status));
        Assert.That(first.Failure.Code, Is.EqualTo(second.Failure.Code));
        Assert.That(first.Failure.Message, Is.EqualTo(second.Failure.Message));
    }

    private static ManpowerSourceConsequenceProposal CreateCapacityComparisonProposal(
        long capacity,
        out ManpowerSourceCapacitySnapshot snapshot)
    {
        CityRuntime city = CreateCity("capacity-comparison-city", 100);
        SimulationRuntime world = CreateWorld(
            new[] { city },
            new[] { Registration("capacity-comparison-source", city, capacity) });
        IManpowerSourceSnapshotProvider provider = world.ContingentManpowerStateStore.SourceProvider;
        Assert.That(provider.TryGetSnapshot(
            new ManpowerSourceId("capacity-comparison-source"),
            out snapshot), Is.True);
        return Plan(world, "capacity-comparison-source", 10L);
    }

    private static ManpowerSourceConsequenceProposal Plan(
        SimulationRuntime world,
        string sourceId,
        long amount)
    {
        ManpowerSourceConsequencePlanningResult result = world.ManpowerSourceConsequencePlanningService.TryPlan(
            Request(sourceId, amount));
        Assert.That(result.Status, Is.EqualTo(ManpowerSourceConsequencePlanningStatus.Planned), result.Failure.ToString());
        return result.Proposal;
    }

    private static void AddResidents(
        SimulationRuntime world,
        CityRuntime city,
        int count,
        string idPrefix)
    {
        for (int i = 0; i < count; i++)
        {
            PersonRuntime person = new PersonRuntime(new PersonId(idPrefix + "-" + i), 0L);
            Assert.That(world.PersonStore.TryRegister(person, out PersonStoreFailure storeFailure), Is.True, storeFailure.ToString());
            Assert.That(PersonResidenceMembershipSystem.TryBindExistingResident(
                person,
                city,
                world,
                out PersonResidenceMembershipFailure membershipFailure), Is.True, membershipFailure.ToString());
        }
    }

    private static SimulationRuntime CreateWorld(
        CityRuntime[] cities,
        SettlementManpowerSourceRegistration[] registrations = null,
        IManpowerSourceSnapshotProvider provider = null,
        PersonStore personStore = null,
        ArmedForceStore armedForceStore = null,
        long day = 0L)
    {
        return new SimulationRuntime(
            new SimulationTime(day),
            cities,
            null,
            personStore: personStore,
            armedForceStore: armedForceStore,
            manpowerSourceProvider: provider,
            settlementManpowerSourceRegistrations: registrations);
    }

    private static CityRuntime CreateCity(string runtimeId, int population)
    {
        CityData data = SimulationTestFactory.CreateCityData("definition-" + runtimeId);
        data.initialPopulation = population;
        return new CityRuntime(runtimeId, data, new SpatialLocationRuntime("location-" + runtimeId));
    }

    private static SettlementManpowerSourceRegistration Registration(
        string sourceId,
        CityRuntime city,
        long capacity)
    {
        return new SettlementManpowerSourceRegistration(new ManpowerSourceId(sourceId), city, capacity);
    }

    private static ManpowerSourceConsequenceRequest Request(string sourceId, long amount)
    {
        return new ManpowerSourceConsequenceRequest(new ManpowerSourceId(sourceId), Death(amount));
    }

    private static ManpowerSourceEffect Death(long amount)
    {
        return new ManpowerSourceEffect(ManpowerSourceEffectKind.Death, amount);
    }

    private static void ApplyPopulationChange(
        SettlementPopulationRuntime population,
        PopulationChangeSet change)
    {
        Assert.That(SettlementPopulationSystem.TryPropose(
            population,
            change,
            out SettlementPopulationTransition transition,
            out PopulationTransitionFailure proposalFailure), Is.True, proposalFailure.ToString());
        Assert.That(SettlementPopulationSystem.TryApply(population, transition, out PopulationTransitionFailure applyFailure),
            Is.True, applyFailure.ToString());
    }

    private sealed class TestSnapshotProvider : IManpowerSourceSnapshotProvider
    {
        private readonly System.Collections.Generic.Dictionary<string, ManpowerSourceCapacitySnapshot> snapshots =
            new System.Collections.Generic.Dictionary<string, ManpowerSourceCapacitySnapshot>(StringComparer.Ordinal);

        public bool TryGetSnapshot(ManpowerSourceId sourceId, out ManpowerSourceCapacitySnapshot snapshot)
        {
            snapshot = null;
            return sourceId != null && snapshots.TryGetValue(sourceId.Value, out snapshot);
        }

        public void Set(ManpowerSourceId sourceId, long capacity, long? living, string fingerprint)
        {
            snapshots[sourceId.Value] = new ManpowerSourceCapacitySnapshot(sourceId, capacity, living, fingerprint);
        }
    }

    private sealed class MutablePlannerIdentity : IManpowerSourceConsequencePlannerIdentity
    {
        public string RuleKey { get; set; }
        public string ConfigurationIdentity { get; set; }
        public int Version { get; set; }

        public MutablePlannerIdentity(string ruleKey, string configurationIdentity, int version)
        {
            RuleKey = ruleKey;
            ConfigurationIdentity = configurationIdentity;
            Version = version;
        }
    }
}
