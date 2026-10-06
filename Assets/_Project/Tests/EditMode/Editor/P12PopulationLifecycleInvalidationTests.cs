using System;
using System.Collections;
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
    public void DirectPersonDeathClearsPopulatedAndPreservesEmptyCurrentActionOwners()
    {
        foreach (bool installAction in new[] { false, true })
        {
            CityRuntime city = CreateCity("p12-direct-person-death-city-" + installAction, 5);
            PersonStore people = new PersonStore();
            SimulationRuntime runtime = CreateP12Runtime(new[] { city }, null, people);
            PersonRuntime person = new PersonRuntime(
                new PersonId("p12-direct-person-death-person-" + installAction), 0L);
            Assert.That(runtime.TryRegisterPerson(person, out PersonStoreFailure registerFailure),
                Is.True, registerFailure.ToString());
            Assert.That(runtime.TryMaterializePerson(
                person.PersonId,
                SimulationTestFactory.CreateNpc("p12-direct-person-death-definition-" + installAction),
                "p12-direct-person-death-npc-" + installAction,
                null,
                0f,
                out NpcRuntime npc,
                out PersonMaterializationFailure materializeFailure), Is.True, materializeFailure.ToString());
            NpcCurrentActionCensusProvider actionProvider = new NpcCurrentActionCensusProvider(npc);
            if (installAction)
            {
                using (EnterDailyAdvanceOperation(runtime))
                {
                    npc.SetCurrentActionRuntime(new NpcActionRuntime(
                        SimulationTestFactory.CreateAction(
                            "p12-direct-person-death-action", NpcActionType.Normal)));
                }
            }

            long epochBeforeDeath = ReadEpoch(runtime);
            long actionRevisionBeforeDeath = actionProvider.GetCurrentCensus().Revision;
            Assert.That(runtime.TryApplyPersonDeath(
                person.PersonId, out _, out PersonDeathLifecycleFailure deathFailure),
                Is.True, deathFailure.ToString());

            Assert.That(person.DeathAbsoluteDay, Is.EqualTo(runtime.CurrentDay));
            Assert.That(npc.IsDead, Is.True);
            Assert.That(npc.CurrentActionRuntime, Is.Null);
            Assert.That(actionProvider.GetCurrentCensus().Cardinality, Is.Zero);
            Assert.That(actionProvider.GetCurrentCensus().Revision,
                Is.EqualTo(actionRevisionBeforeDeath + (installAction ? 1L : 0L)));
            Assert.That(ReadEpoch(runtime), Is.EqualTo(epochBeforeDeath + 1L));
            AssertCensus(runtime);
        }
    }

    [Test]
    public void DirectPersonDeathRejectsSaturatedCurrentActionBeforeAnyLifecycleWrite()
    {
        CityRuntime city = CreateCity("p12-direct-person-death-saturated-city", 5);
        PersonStore people = new PersonStore();
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, null, people);
        PersonRuntime person = new PersonRuntime(new PersonId("p12-direct-person-death-saturated-person"), 0L);
        Assert.That(runtime.TryRegisterPerson(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        Assert.That(runtime.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc("p12-direct-person-death-saturated-definition"),
            "p12-direct-person-death-saturated-npc",
            null,
            0f,
            out NpcRuntime npc,
            out PersonMaterializationFailure materializeFailure), Is.True, materializeFailure.ToString());
        NpcActionRuntime action = new NpcActionRuntime(
            SimulationTestFactory.CreateAction("p12-direct-person-death-saturated-action", NpcActionType.Normal));
        using (EnterDailyAdvanceOperation(runtime))
        {
            npc.SetCurrentActionRuntime(action);
        }
        SetNpcCurrentActionRevisionAndBaseline(runtime, npc, long.MaxValue);
        AssertCensus(runtime);
        long epochBeforeDeath = ReadEpoch(runtime);
        long personRevisionBeforeDeath = ReadPersonLifecycleRevision(runtime, person.PersonId);

        Assert.That(runtime.TryApplyPersonDeath(
            person.PersonId, out _, out PersonDeathLifecycleFailure deathFailure), Is.False);
        Assert.That(deathFailure, Is.EqualTo(PersonDeathLifecycleFailure.RuntimeFaulted));
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(npc.IsAlive, Is.True);
        Assert.That(npc.CurrentActionRuntime, Is.SameAs(action));
        Assert.That(new NpcCurrentActionCensusProvider(npc).GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue));
        Assert.That(ReadNpcLifecycleRevision(runtime, npc.RuntimeId, residence: false), Is.Zero);
        Assert.That(ReadPersonLifecycleRevision(runtime, person.PersonId), Is.EqualTo(personRevisionBeforeDeath));
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
        Assert.That(city.Population.Revision, Is.Zero);
        Assert.That(ReadEpoch(runtime), Is.EqualTo(epochBeforeDeath));
        AssertCensus(runtime);
    }

    [Test]
    public void LegacyResidentDeathWithEmptyActionKeepsEmptyOwnerAndPublishesOneEpoch()
    {
        CityRuntime city = CreateCity("p12-legacy-empty-death-city", 5);
        NpcRuntime npc = CreateNpc("p12-legacy-empty-death-npc");
        NpcRuntime[] roster = { npc };
        Assert.That(SettlementPopulationMembershipSystem.TryBindExistingResident(
            city, npc, SimulationTestFactory.CreateAuthoritativeNpcRoster(roster),
            out PopulationMembershipFailure bindFailure), Is.True, bindFailure.ToString());
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, roster);
        NpcCurrentActionCensusProvider actionProvider = new NpcCurrentActionCensusProvider(npc);
        long lifeRevisionBefore = ReadNpcLifecycleRevision(runtime, npc.RuntimeId, residence: false);
        long residenceRevisionBefore = ReadNpcLifecycleRevision(runtime, npc.RuntimeId, residence: true);

        Assert.That(actionProvider.GetCurrentCensus().Cardinality, Is.Zero);
        Assert.That(runtime.TryApplyResidentDeath(
            npc, city, out _, out NpcPopulationLifecycleFailure deathFailure), Is.True, deathFailure.ToString());

        Assert.That(npc.IsDead, Is.True);
        Assert.That(npc.CurrentActionRuntime, Is.Null);
        Assert.That(actionProvider.GetCurrentCensus().Cardinality, Is.Zero);
        Assert.That(actionProvider.GetCurrentCensus().Revision, Is.Zero);
        Assert.That(ReadNpcLifecycleRevision(runtime, npc.RuntimeId, residence: false), Is.EqualTo(lifeRevisionBefore + 1L));
        Assert.That(ReadNpcLifecycleRevision(runtime, npc.RuntimeId, residence: true), Is.EqualTo(residenceRevisionBefore + 1L));
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
        AssertEpoch(runtime, 1L);
        AssertCensus(runtime);
    }

    [Test]
    public void CurrentActionSlotAndRuntimeWritesUseExactOwnerAndAdmittedBoundaries()
    {
        CityRuntime city = CreateCity("p12-current-action-city", 10);
        NpcRuntime actor = CreateNpc("p12-current-action-actor");
        NpcRuntime secondActor = CreateNpc("p12-current-action-second-actor");
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, new[] { actor, secondActor });

        NpcCurrentActionCensusProvider initialProvider = new NpcCurrentActionCensusProvider(actor);
        OwnerSectionCensusWitness initial = initialProvider.GetCurrentCensus();
        Assert.That(initial.SectionId, Is.EqualTo(NpcCurrentActionCensusProvider.SectionIdFor(actor.RuntimeId)));
        Assert.That(initial.SchemaVersion, Is.EqualTo(NpcCurrentActionCensusProvider.SchemaVersion));
        Assert.That(initial.OwnerInstanceIdentity, Is.SameAs(actor));
        Assert.That(initial.Cardinality, Is.Zero);
        Assert.That(initial.Revision, Is.Zero);

        NpcActionData sharedDefinition = SimulationTestFactory.CreateAction(
            "p12-current-action-first", NpcActionType.Normal);
        NpcActionRuntime first = new NpcActionRuntime(sharedDefinition);
        actor.SetCurrentActionRuntime(first);
        Assert.That(actor.CurrentActionRuntime, Is.Null,
            "a selected-profile action write outside an admitted runtime operation must stop before commit");
        AssertEpoch(runtime, 0L);

        using (EnterDailyAdvanceOperation(runtime))
        {
            actor.SetCurrentActionRuntime(first);
            Assert.That(actor.CurrentActionRuntime, Is.SameAs(first));
            Assert.That(secondActor.CurrentActionRuntime, Is.Null);

            OwnerSectionCensusWitness installed = initialProvider.GetCurrentCensus();
            Assert.That(installed.Cardinality, Is.EqualTo(1));
            Assert.That(installed.Revision, Is.EqualTo(1L));
            AssertEpoch(runtime, 1L);

            secondActor.SetCurrentActionRuntime(first);
            Assert.That(secondActor.CurrentActionRuntime, Is.Null,
                "one mutable NpcActionRuntime cannot be installed into two current-action owner slots");
            Assert.That(initialProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
            AssertEpoch(runtime, 1L);
        }

        CommercialDecisionEvidence commercialEvidence = new CommercialDecisionEvidence(
            "p12-item", "p12-location", "p12-origin", "p12-destination", null,
            null, null, 2, 1f, 3f, 4f, 5f, 6f, 7f);
        CommercialScoutingEvidence scoutingEvidence = new CommercialScoutingEvidence(
            "p12-destination", "p12-route", 0, 0, false, 0L, 1f, 1, 2f);

        first.SetSuccessChanceMultiplier(0.25f);
        first.SetOriginDecisionId("rejected-decision");
        first.SetStableOccurrenceKey("rejected-occurrence");
        first.SetCommercialDecisionEvidence(commercialEvidence);
        first.SetCommercialScoutingEvidence(scoutingEvidence);
        Assert.That(first.SuccessChanceMultiplier, Is.EqualTo(1f),
            "a bound action runtime rejects writes outside the selected-profile owner boundary");
        Assert.That(first.OriginDecisionId, Is.Null);
        Assert.That(first.StableOccurrenceKey, Is.Null);
        Assert.That(first.CommercialDecisionEvidence, Is.Null);
        Assert.That(first.CommercialScoutingEvidence, Is.Null);
        Assert.That(initialProvider.GetCurrentCensus().Revision, Is.EqualTo(1L));
        AssertEpoch(runtime, 1L);

        using (EnterDailyAdvanceOperation(runtime))
        {
            first.SetSuccessChanceMultiplier(0.25f);
            first.SetOriginDecisionId("p12-decision");
            first.SetStableOccurrenceKey("p12-occurrence");
            first.SetCommercialDecisionEvidence(commercialEvidence);
            first.SetCommercialScoutingEvidence(scoutingEvidence);
        }

        Assert.That(first.SuccessChanceMultiplier, Is.EqualTo(0.25f));
        Assert.That(first.OriginDecisionId, Is.EqualTo("p12-decision"));
        Assert.That(first.StableOccurrenceKey, Is.EqualTo("p12-occurrence"));
        Assert.That(first.CommercialDecisionEvidence, Is.SameAs(commercialEvidence));
        Assert.That(first.CommercialScoutingEvidence, Is.SameAs(scoutingEvidence));
        Assert.That(initialProvider.GetCurrentCensus().Revision, Is.EqualTo(6L));
        AssertEpoch(runtime, 6L);

        using (EnterDailyAdvanceOperation(runtime))
        {
            first.SetSuccessChanceMultiplier(0.25f);
            first.SetOriginDecisionId("p12-decision");
            first.SetStableOccurrenceKey("p12-occurrence");
            first.SetCommercialDecisionEvidence(commercialEvidence);
            first.SetCommercialScoutingEvidence(scoutingEvidence);
        }
        Assert.That(initialProvider.GetCurrentCensus().Revision, Is.EqualTo(6L),
            "no-op action owner setters do not advance the local revision");
        AssertEpoch(runtime, 6L);

        NpcActionRuntime replacement = new NpcActionRuntime(sharedDefinition);
        using (EnterDailyAdvanceOperation(runtime))
        {
            actor.SetCurrentActionRuntime(replacement);
        }
        Assert.That(actor.CurrentActionRuntime, Is.SameAs(replacement));
        Assert.That(replacement, Is.Not.SameAs(first));
        Assert.That(replacement.Action, Is.SameAs(sharedDefinition),
            "replacing the mutable runtime preserves the exact shared immutable action definition");
        Assert.That(initialProvider.GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(initialProvider.GetCurrentCensus().Revision, Is.EqualTo(7L));
        AssertEpoch(runtime, 7L);

        first.SetOriginDecisionId("detached-action-write");
        Assert.That(initialProvider.GetCurrentCensus().Revision, Is.EqualTo(7L),
            "a stale detached action object no longer reports mutations against its former owner");
        AssertEpoch(runtime, 7L);

        using (EnterDailyAdvanceOperation(runtime))
        {
            actor.SetCurrentActionRuntime(null);
        }
        Assert.That(initialProvider.GetCurrentCensus().Cardinality, Is.Zero);
        Assert.That(initialProvider.GetCurrentCensus().Revision, Is.EqualTo(8L));
        AssertEpoch(runtime, 8L);

        SetNpcCurrentActionRevision(actor, long.MaxValue);
        using (EnterDailyAdvanceOperation(runtime))
        {
            actor.SetCurrentActionRuntime(new NpcActionRuntime(
                SimulationTestFactory.CreateAction("p12-current-action-saturated", NpcActionType.Normal)));
        }
        Assert.That(actor.CurrentActionRuntime, Is.Null,
            "local revision exhaustion rejects before changing the action slot");
        AssertEpoch(runtime, 8L);
    }

    [Test]
    public void CurrentActionRosterRemovalAndReusedRuntimeIdDoNotRebindPriorOwner()
    {
        CityRuntime city = CreateCity("p12-current-action-roster-city", 10);
        NpcRuntime originalNpc = CreateNpc("p12-current-action-reused-id");
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, new[] { originalNpc });
        NpcActionRuntime originalAction = new NpcActionRuntime(
            SimulationTestFactory.CreateAction("p12-current-action-roster-action", NpcActionType.Normal));

        using (EnterDailyAdvanceOperation(runtime))
        {
            originalNpc.SetCurrentActionRuntime(originalAction);
        }

        string sectionId = NpcCurrentActionCensusProvider.SectionIdFor(originalNpc.RuntimeId);
        OwnerSectionCensusWitness originalWitness = FindWitness(
            NpcCurrentActionCensusProvider.CreateProviders(runtime.NpcRuntimes), sectionId);
        Assert.That(originalWitness.OwnerInstanceIdentity, Is.SameAs(originalNpc));
        Assert.That(originalWitness.Cardinality, Is.EqualTo(1));
        Assert.That(originalWitness.Revision, Is.EqualTo(1L));
        AssertEpoch(runtime, 1L);

        Assert.That(runtime.TryUnregisterNpc(originalNpc.RuntimeId, out WorldNpcRegistryFailure unregisterFailure),
            Is.True, unregisterFailure.ToString());
        Assert.That(runtime.NpcRuntimes, Is.Empty);
        Assert.That(NpcCurrentActionCensusProvider.CreateProviders(runtime.NpcRuntimes), Is.Empty,
            "roster removal withdraws the old current-action section");
        Assert.That(originalNpc.CurrentActionRuntime, Is.SameAs(originalAction));
        AssertEpoch(runtime, 2L);
        AssertCensus(runtime);

        NpcRuntime replacementNpc = CreateNpc(originalNpc.RuntimeId);
        Assert.That(runtime.TryRegisterNpc(replacementNpc, out WorldNpcRegistryFailure registerFailure),
            Is.True, registerFailure.ToString());

        OwnerSectionCensusWitness replacementWitness = FindWitness(
            NpcCurrentActionCensusProvider.CreateProviders(runtime.NpcRuntimes), sectionId);
        Assert.That(replacementWitness.OwnerInstanceIdentity, Is.SameAs(replacementNpc),
            "a reused RuntimeId receives a witness for the new exact NPC instance");
        Assert.That(replacementWitness.OwnerInstanceIdentity, Is.Not.SameAs(originalNpc));
        Assert.That(replacementWitness.Cardinality, Is.Zero);
        Assert.That(replacementWitness.Revision, Is.Zero);
        Assert.That(replacementNpc.CurrentActionRuntime, Is.Null,
            "the new NPC does not inherit the removed NPC's mutable action runtime");
        AssertEpoch(runtime, 3L);

        originalAction.SetOriginDecisionId("detached-old-owner-write");
        Assert.That(originalNpc.CurrentActionRuntime, Is.SameAs(originalAction));
        Assert.That(replacementNpc.CurrentActionRuntime, Is.Null);
        Assert.That(FindWitness(
            NpcCurrentActionCensusProvider.CreateProviders(runtime.NpcRuntimes), sectionId).Revision,
            Is.Zero,
            "a stale action reference cannot mutate the new owner witness after same-id registration");
        AssertEpoch(runtime, 3L);
        AssertCensus(runtime);
    }

    [Test]
    public void CurrentActionCensusRejectsMismatchedSlotsAndAliasedMutableRuntimes()
    {
        NpcActionData action = SimulationTestFactory.CreateAction(
            "p12-current-action-census", NpcActionType.Normal);
        NpcActionRuntime shared = new NpcActionRuntime(action);
        NpcRuntime first = CreateNpc("p12-current-action-census-first");
        NpcRuntime second = CreateNpc("p12-current-action-census-second");
        first.SetCurrentActionRuntime(shared);
        second.SetCurrentActionRuntime(shared);

        Assert.Throws<ArgumentException>(() => NpcCurrentActionCensusProvider.CreateProviders(
            new[] { first, second }));

        NpcRuntime mismatched = CreateNpc("p12-current-action-census-mismatch");
        mismatched.SetCurrentActionRuntime(new NpcActionRuntime(action));
        FieldInfo currentAction = typeof(NpcRuntime).GetField(
            "currentAction", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(currentAction, Is.Not.Null);
        currentAction.SetValue(mismatched, SimulationTestFactory.CreateAction(
            "p12-current-action-census-other", NpcActionType.Normal));

        Assert.Throws<ArgumentException>(() => NpcCurrentActionCensusProvider.CreateProviders(
            new[] { mismatched }));

        NpcRuntime legacy = CreateNpc("legacy-current-action-replacement");
        currentAction.SetValue(legacy, action);
        NpcActionRuntime replacement = new NpcActionRuntime(action);
        legacy.SetCurrentActionRuntime(replacement);
        Assert.That(legacy.CurrentActionRuntime, Is.SameAs(replacement),
            "outside P12, the existing setter keeps its ability to replace and repair serialized slot pairs");
        Assert.That(legacy.HasConsistentCurrentActionSlot, Is.True);
    }

    [Test]
    public void AutonomousDailyDecisionInstallsCurrentActionThroughP12OwnerBoundary()
    {
        NpcRuntime actor = CreateNpc("p12-current-action-autonomous-actor");
        NpcActionData action = SimulationTestFactory.CreateAction(
            "p12-current-action-autonomous", NpcActionType.Normal);
        action.baseUtility = 10f;
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(),
            null,
            new[] { actor },
            configuration: CreateP12Configuration(),
            configuredActions: new[] { action },
            npcDecisionSystem: new NpcDecisionSystem(new List<INpcActionProvider>()),
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());

        runtime.AdvanceDay();

        Assert.That(runtime.CurrentDay, Is.EqualTo(1L));
        Assert.That(actor.CurrentActionRuntime, Is.Not.Null);
        Assert.That(actor.CurrentActionRuntime.Action, Is.SameAs(action));
        OwnerSectionCensusWitness actionWitness = new NpcCurrentActionCensusProvider(actor).GetCurrentCensus();
        Assert.That(actionWitness.Cardinality, Is.EqualTo(1));
        Assert.That(actionWitness.Revision, Is.EqualTo(1L));
        Assert.That(runtime.TryAssessNpcRosterCensus(out ContinuationCensusFailure censusFailure), Is.True,
            censusFailure.ToString());
    }

    [Test]
    public void ResidentDeathClearsCurrentActionAndReservesItsRevisionBeforeMutation()
    {
        CityRuntime city = CreateCity("p12-current-action-death-city", 5);
        NpcRuntime actor = CreateNpc("p12-current-action-death-actor");
        NpcRuntime[] roster = { actor };
        Assert.That(SettlementPopulationMembershipSystem.TryBindExistingResident(
            city, actor, SimulationTestFactory.CreateAuthoritativeNpcRoster(roster),
            out PopulationMembershipFailure membershipFailure),
            Is.True, membershipFailure.ToString());
        actor.SetCurrentActionRuntime(new NpcActionRuntime(
            SimulationTestFactory.CreateAction("p12-current-action-death", NpcActionType.Normal)));
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, roster);
        NpcCurrentActionCensusProvider provider = new NpcCurrentActionCensusProvider(actor);
        long epochBefore = ReadEpoch(runtime);
        long revisionBefore = provider.GetCurrentCensus().Revision;

        Assert.That(runtime.TryApplyResidentDeath(
            actor, city, out _, out NpcPopulationLifecycleFailure deathFailure),
            Is.True, deathFailure.ToString());

        Assert.That(actor.IsDead, Is.True);
        Assert.That(actor.CurrentActionRuntime, Is.Null);
        Assert.That(provider.GetCurrentCensus().Cardinality, Is.Zero);
        Assert.That(provider.GetCurrentCensus().Revision, Is.EqualTo(revisionBefore + 1L));
        Assert.That(ReadEpoch(runtime), Is.EqualTo(epochBefore + 1L),
            "the resident lifecycle and CurrentAction owner changes share one operation epoch");
        AssertCensus(runtime);

        CityRuntime saturatedCity = CreateCity("p12-current-action-death-saturated-city", 5);
        NpcRuntime saturatedActor = CreateNpc("p12-current-action-death-saturated-actor");
        NpcRuntime[] saturatedRoster = { saturatedActor };
        Assert.That(SettlementPopulationMembershipSystem.TryBindExistingResident(
            saturatedCity, saturatedActor, SimulationTestFactory.CreateAuthoritativeNpcRoster(saturatedRoster),
            out membershipFailure),
            Is.True, membershipFailure.ToString());
        saturatedActor.SetCurrentActionRuntime(new NpcActionRuntime(
            SimulationTestFactory.CreateAction("p12-current-action-death-saturated", NpcActionType.Normal)));
        SetNpcCurrentActionRevision(saturatedActor, long.MaxValue);
        SimulationRuntime saturatedRuntime = CreateP12Runtime(new[] { saturatedCity }, saturatedRoster);
        long saturatedLifeRevision = ReadNpcLifecycleRevision(
            saturatedRuntime, saturatedActor.RuntimeId, residence: false);
        long saturatedResidenceRevision = ReadNpcLifecycleRevision(
            saturatedRuntime, saturatedActor.RuntimeId, residence: true);
        long saturatedPopulationRevision = saturatedCity.Population.Revision;

        Assert.That(saturatedRuntime.TryApplyResidentDeath(
            saturatedActor, saturatedCity, out _, out NpcPopulationLifecycleFailure saturatedFailure), Is.False);
        Assert.That(saturatedFailure, Is.EqualTo(NpcPopulationLifecycleFailure.RuntimeFaulted));
        Assert.That(saturatedActor.IsAlive, Is.True);
        Assert.That(saturatedActor.CurrentActionRuntime, Is.Not.Null);
        Assert.That(saturatedActor.ResidenceSettlementRuntimeId, Is.EqualTo(saturatedCity.RuntimeId));
        Assert.That(saturatedCity.CurrentPopulation, Is.EqualTo(5));
        Assert.That(saturatedCity.Population.Revision, Is.EqualTo(saturatedPopulationRevision));
        Assert.That(ReadNpcLifecycleRevision(
            saturatedRuntime, saturatedActor.RuntimeId, residence: false), Is.EqualTo(saturatedLifeRevision));
        Assert.That(ReadNpcLifecycleRevision(
            saturatedRuntime, saturatedActor.RuntimeId, residence: true), Is.EqualTo(saturatedResidenceRevision));
        Assert.That(new NpcCurrentActionCensusProvider(saturatedActor).GetCurrentCensus().Revision,
            Is.EqualTo(long.MaxValue));
        AssertEpoch(saturatedRuntime, 0L);
        AssertCensus(saturatedRuntime);
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

        NpcActionRuntime currentAction = new NpcActionRuntime(
            SimulationTestFactory.CreateAction("p12-person-backed-death-current-action", NpcActionType.Normal));
        using (EnterDailyAdvanceOperation(runtime))
        {
            npc.SetCurrentActionRuntime(currentAction);
        }
        NpcCurrentActionCensusProvider actionProvider = new NpcCurrentActionCensusProvider(npc);
        long actionRevisionBeforeDeath = actionProvider.GetCurrentCensus().Revision;

        long epochBeforeDeath = ReadEpoch(runtime);
        Assert.That(runtime.TryApplyResidentDeath(
            npc, city, out _, out NpcPopulationLifecycleFailure deathFailure), Is.True, deathFailure.ToString());
        Assert.That(ReadEpoch(runtime), Is.EqualTo(epochBeforeDeath + 1L));
        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(runtime.CurrentDay));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(npc.IsDead, Is.True);
        Assert.That(npc.CurrentActionRuntime, Is.Null);
        Assert.That(actionProvider.GetCurrentCensus().Cardinality, Is.Zero);
        Assert.That(actionProvider.GetCurrentCensus().Revision, Is.EqualTo(actionRevisionBeforeDeath + 1L));
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
        AssertPersonRevision(runtime, person.PersonId, expected: 3L);
        AssertNpcRevision(runtime, npc.RuntimeId, residence: false, expected: 1L);
        AssertCensus(runtime);
    }

    [Test]
    public void PersonBackedResidentDeathWithEmptyActionKeepsEmptyOwnerAndPublishesOneEpoch()
    {
        (SimulationRuntime runtime, CityRuntime city, PersonRuntime person, NpcRuntime npc) =
            CreateMaterializedResidentFixture("p12-person-backed-empty-death");
        NpcCurrentActionCensusProvider actionProvider = new NpcCurrentActionCensusProvider(npc);
        long epochBeforeDeath = ReadEpoch(runtime);
        long personRevisionBefore = ReadPersonLifecycleRevision(runtime, person.PersonId);
        long lifeRevisionBefore = ReadNpcLifecycleRevision(runtime, npc.RuntimeId, residence: false);

        Assert.That(actionProvider.GetCurrentCensus().Cardinality, Is.Zero);
        Assert.That(runtime.TryApplyResidentDeath(
            npc, city, out _, out NpcPopulationLifecycleFailure deathFailure), Is.True, deathFailure.ToString());

        Assert.That(person.DeathAbsoluteDay, Is.EqualTo(runtime.CurrentDay));
        Assert.That(person.ResidenceSettlementRuntimeId, Is.Null);
        Assert.That(npc.IsDead, Is.True);
        Assert.That(npc.CurrentActionRuntime, Is.Null);
        Assert.That(actionProvider.GetCurrentCensus().Cardinality, Is.Zero);
        Assert.That(actionProvider.GetCurrentCensus().Revision, Is.Zero);
        Assert.That(ReadPersonLifecycleRevision(runtime, person.PersonId), Is.EqualTo(personRevisionBefore + 2L));
        Assert.That(ReadNpcLifecycleRevision(runtime, npc.RuntimeId, residence: false), Is.EqualTo(lifeRevisionBefore + 1L));
        Assert.That(city.CurrentPopulation, Is.EqualTo(4));
        Assert.That(city.Population.Revision, Is.EqualTo(1L));
        Assert.That(ReadEpoch(runtime), Is.EqualTo(epochBeforeDeath + 1L));
        AssertCensus(runtime);
    }

    [Test]
    public void PersonBackedResidentDeathRejectsSaturatedCurrentActionBeforeAnyLifecycleWrite()
    {
        (SimulationRuntime runtime, CityRuntime city, PersonRuntime person, NpcRuntime npc) =
            CreateMaterializedResidentFixture("p12-person-backed-saturated-death");
        NpcActionRuntime action = new NpcActionRuntime(
            SimulationTestFactory.CreateAction("p12-person-backed-saturated-death-action", NpcActionType.Normal));
        using (EnterDailyAdvanceOperation(runtime))
        {
            npc.SetCurrentActionRuntime(action);
        }
        SetNpcCurrentActionRevisionAndBaseline(runtime, npc, long.MaxValue);
        AssertCensus(runtime);
        long epochBeforeDeath = ReadEpoch(runtime);
        long personRevisionBefore = ReadPersonLifecycleRevision(runtime, person.PersonId);
        long lifeRevisionBefore = ReadNpcLifecycleRevision(runtime, npc.RuntimeId, residence: false);
        long populationRevisionBefore = city.Population.Revision;

        Assert.That(runtime.TryApplyResidentDeath(
            npc, city, out _, out NpcPopulationLifecycleFailure deathFailure), Is.False);
        Assert.That(deathFailure, Is.EqualTo(NpcPopulationLifecycleFailure.RuntimeFaulted));
        Assert.That(person.DeathAbsoluteDay, Is.Null);
        Assert.That(person.ResidenceSettlementRuntimeId, Is.EqualTo(city.RuntimeId));
        Assert.That(npc.IsAlive, Is.True);
        Assert.That(npc.CurrentActionRuntime, Is.SameAs(action));
        Assert.That(city.CurrentPopulation, Is.EqualTo(5));
        Assert.That(city.Population.Revision, Is.EqualTo(populationRevisionBefore));
        Assert.That(ReadPersonLifecycleRevision(runtime, person.PersonId), Is.EqualTo(personRevisionBefore));
        Assert.That(ReadNpcLifecycleRevision(runtime, npc.RuntimeId, residence: false), Is.EqualTo(lifeRevisionBefore));
        Assert.That(new NpcCurrentActionCensusProvider(npc).GetCurrentCensus().Revision, Is.EqualTo(long.MaxValue));
        Assert.That(ReadEpoch(runtime), Is.EqualTo(epochBeforeDeath));
        AssertCensus(runtime);
    }

    [Test]
    public void LivePersonBindAndMaterializeActorChoicesRespectP12CurrentActionOwner()
    {
        P12ActorChoiceFixture bound = CreateP12ActorChoiceFixture(
            "p12-actor-choice-bound", preexistingActor: true, atCityLocation: true, inventoryAmount: 10);
        NpcCurrentActionCensusProvider boundActionProvider = new NpcCurrentActionCensusProvider(bound.Actor);
        string boundSectionId = boundActionProvider.GetCurrentCensus().SectionId;
        Assert.That(bound.Runtime.TryBindExistingNpcToPerson(
            bound.Person.PersonId, bound.Actor.RuntimeId, out PersonMaterializationFailure bindFailure),
            Is.True, bindFailure.ToString());
        Assert.That(boundActionProvider.GetCurrentCensus().SectionId, Is.EqualTo(boundSectionId));
        Assert.That(boundActionProvider.GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(bound.Actor));
        InstallPriorAction(bound.Runtime, bound.Actor, "p12-actor-choice-bound-prior");
        long boundRevisionBeforeChoice = boundActionProvider.GetCurrentCensus().Revision;
        CaptureP12ActorChoice(bound.Runtime, bound.Person.PersonId, bound.SellAction.DefinitionId,
            "p12-actor-choice-bound-input");

        bound.Runtime.AdvanceDay();

        Assert.That(bound.Runtime.ActorChoiceStore.Inputs, Has.Count.EqualTo(1));
        ActorChoiceInput accepted = bound.Runtime.ActorChoiceStore.Inputs[0];
        Assert.That(accepted.Status, Is.EqualTo(ActorChoiceInputStatus.AttemptReturned));
        Assert.That(accepted.Dispositions, Has.Count.EqualTo(2));
        Assert.That(accepted.Dispositions[0].Kind, Is.EqualTo(ActorChoiceDispositionKind.DispatchStarted));
        Assert.That(accepted.Dispositions[1].AttemptOutcome, Is.EqualTo(ActorChoiceAttemptOutcome.Succeeded));
        Assert.That(bound.Actor.CurrentActionRuntime, Is.Not.Null);
        Assert.That(bound.Actor.CurrentActionRuntime.Action, Is.SameAs(bound.SellAction));
        Assert.That(boundActionProvider.GetCurrentCensus().Cardinality, Is.EqualTo(1));
        Assert.That(boundActionProvider.GetCurrentCensus().Revision, Is.GreaterThan(boundRevisionBeforeChoice));
        AssertCensus(bound.Runtime);

        P12ActorChoiceFixture materialized = CreateP12ActorChoiceFixture(
            "p12-actor-choice-materialized", preexistingActor: false, atCityLocation: false, inventoryAmount: 0);
        Assert.That(materialized.Runtime.TryMaterializePerson(
            materialized.Person.PersonId,
            SimulationTestFactory.CreateNpc("p12-actor-choice-materialized-definition", NpcJobType.Merchant, MerchantBehavior.Local),
            "p12-actor-choice-materialized-npc",
            null,
            0f,
            out NpcRuntime materializedActor,
            out PersonMaterializationFailure materializeFailure), Is.True, materializeFailure.ToString());
        NpcCurrentActionCensusProvider materializedActionProvider =
            new NpcCurrentActionCensusProvider(materializedActor);
        Assert.That(materializedActionProvider.GetCurrentCensus().OwnerInstanceIdentity, Is.SameAs(materializedActor));
        InstallPriorAction(materialized.Runtime, materializedActor, "p12-actor-choice-materialized-prior");
        CaptureP12ActorChoice(materialized.Runtime, materialized.Person.PersonId,
            materialized.SellAction.DefinitionId, "p12-actor-choice-materialized-input");

        materialized.Runtime.AdvanceDay();

        Assert.That(materialized.Runtime.ActorChoiceStore.Inputs, Has.Count.EqualTo(1));
        ActorChoiceInput rejected = materialized.Runtime.ActorChoiceStore.Inputs[0];
        Assert.That(rejected.Status, Is.EqualTo(ActorChoiceInputStatus.Rejected));
        Assert.That(rejected.Dispositions, Has.Count.EqualTo(1));
        Assert.That(rejected.Dispositions[0].Failure, Is.EqualTo(ActorChoiceFailure.ActionUnavailable));
        Assert.That(materializedActor.CurrentActionRuntime, Is.Null,
            "the supported choice clears the old slot before recording its current-truth rejection");
        Assert.That(materializedActionProvider.GetCurrentCensus().Cardinality, Is.Zero);
        Assert.That(materializedActionProvider.GetCurrentCensus().Revision, Is.EqualTo(2L));
        AssertCensus(materialized.Runtime);
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

    private static (SimulationRuntime Runtime, CityRuntime City, PersonRuntime Person, NpcRuntime Npc)
        CreateMaterializedResidentFixture(string prefix)
    {
        CityRuntime city = CreateCity(prefix + "-city", 5);
        PersonStore people = new PersonStore();
        SimulationRuntime runtime = CreateP12Runtime(new[] { city }, null, people);
        PersonRuntime person = new PersonRuntime(new PersonId(prefix + "-person"), 0L);
        Assert.That(runtime.TryRegisterPerson(person, out PersonStoreFailure registerFailure),
            Is.True, registerFailure.ToString());
        AssertCensus(runtime);
        Assert.That(runtime.TryBindExistingPersonResident(
            person.PersonId, city, out PersonResidenceMembershipFailure bindFailure),
            Is.True, bindFailure.ToString());
        AssertCensus(runtime);
        Assert.That(runtime.TryMaterializePerson(
            person.PersonId,
            SimulationTestFactory.CreateNpc(prefix + "-definition"),
            prefix + "-npc",
            null,
            0f,
            out NpcRuntime npc,
            out PersonMaterializationFailure materializeFailure), Is.True, materializeFailure.ToString());
        AssertCensus(runtime);
        return (runtime, city, person, npc);
    }

    private static P12ActorChoiceFixture CreateP12ActorChoiceFixture(
        string prefix,
        bool preexistingActor,
        bool atCityLocation,
        int inventoryAmount)
    {
        RecordFixture records = SimulationTestFactory.CreateRecordFixture();
        ItemData item = SimulationTestFactory.CreateItem(prefix + "-item", 10f);
        CityRuntime city = SimulationTestFactory.CreateAccountBackedCity(
            prefix + "-city",
            prefix + "-location",
            1000f,
            SimulationTestFactory.CreateMarketItem(item, 100, 100));
        NpcActionData sellAction = SimulationTestFactory.CreateAction(
            prefix + "-sell-goods", NpcActionType.SellGoods, NpcActionCategory.Commerce);
        MerchantSystem merchantSystem = SimulationTestFactory.CreateMerchantSystem(
            null, records.Time, records.DecisionRecorder, maxTradeAmount: 5);
        NpcDecisionSystem decisionSystem = new NpcDecisionSystem(
            new List<INpcActionProvider> { merchantSystem });
        EffectiveSimulationConfiguration configuration = SimulationConfigurationResolver.ResolveOrThrow(
            contentOverrides: new SimulationConfigurationOverrides(
                economy: new EconomyConfigurationOverrides(false),
                merchantTrade: new MerchantTradeConfigurationOverrides(enabled: true)));

        PersonStore people = new PersonStore();
        PersonId personId = new PersonId(prefix + "-person");
        PersonRuntime person = new PersonRuntime(personId);
        Assert.That(people.TryRegister(person, out PersonStoreFailure personFailure),
            Is.True, personFailure.ToString());

        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        LocationId locationId = new LocationId(city.Location.RuntimeId);
        HexId anchorHexId = new HexId(prefix + "-anchor");
        Assert.That(spatial.TryRegisterHex(
            new HexRecord(anchorHexId), out SpatialAuthorityFailure hexFailure), Is.True, hexFailure?.ToString());
        Assert.That(spatial.TryRegisterLocation(
            new LocationRecord(locationId, anchorHexId), out SpatialAuthorityFailure locationFailure),
            Is.True, locationFailure?.ToString());
        LegacySpatialAnchorBindingStore anchorBindings = new LegacySpatialAnchorBindingStore(spatial);
        Assert.That(anchorBindings.TryBindCity(city.RuntimeId, locationId, out SpatialAnchorBindingFailure anchorFailure),
            Is.True, anchorFailure?.ToString());
        PersonSpatialPositionStore positions = new PersonSpatialPositionStore(
            people,
            spatial,
            new SpatialPassageTraversalOptionResolver(spatial.PassageAuthority));
        StablePositionReference position = atCityLocation
            ? StablePositionReference.ForLocation(locationId)
            : StablePositionReference.ForHex(anchorHexId);
        Assert.That(positions.TrySetAt(personId, position, out PersonSpatialPositionFailure positionFailure),
            Is.True, positionFailure?.ToString());

        NpcRuntime actor = null;
        if (preexistingActor)
        {
            actor = new NpcRuntime(
                prefix + "-npc",
                SimulationTestFactory.CreateNpc(prefix + "-merchant-definition", NpcJobType.Merchant, MerchantBehavior.Local),
                city,
                0f);
            if (inventoryAmount > 0)
                actor.Inventory.AddItem(item, inventoryAmount, 1f);
        }

        SimulationRuntime runtime = new SimulationRuntime(
            records.Time,
            new[] { city },
            actor == null ? null : new[] { actor },
            configuredActions: new[] { sellAction },
            npcDecisionSystem: decisionSystem,
            decisionRecorder: records.DecisionRecorder,
            merchantSystem: merchantSystem,
            configuration: configuration,
            personStore: people,
            spatialAuthorityStore: spatial,
            legacySpatialAnchorBindingStore: anchorBindings,
            personSpatialPositionStore: positions,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        return new P12ActorChoiceFixture(runtime, records, city, person, actor, item, sellAction);
    }

    private static void InstallPriorAction(SimulationRuntime runtime, NpcRuntime npc, string definitionId)
    {
        using (EnterDailyAdvanceOperation(runtime))
        {
            npc.SetCurrentActionRuntime(new NpcActionRuntime(
                SimulationTestFactory.CreateAction(definitionId, NpcActionType.Normal)));
        }
    }

    private static void CaptureP12ActorChoice(
        SimulationRuntime runtime,
        PersonId personId,
        string actionDefinitionId,
        string commandId)
    {
        Assert.That(runtime.ActorChoiceStore.TryCapture(
            commandId,
            personId,
            actionDefinitionId,
            WorldCommandOrigin.LocalPlayer,
            WorldCommandAuthorityMode.Request,
            runtime.CurrentDay,
            out _,
            out ActorChoiceStoreFailureCode failure), Is.True, failure.ToString());
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

    private static void SetNpcCurrentActionRevision(NpcRuntime npc, long value)
    {
        FieldInfo revision = typeof(NpcRuntime).GetField(
            "currentActionRevision", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(revision, Is.Not.Null);
        revision.SetValue(npc, value);
    }

    private static void SetNpcCurrentActionRevisionAndBaseline(
        SimulationRuntime runtime,
        NpcRuntime npc,
        long value)
    {
        SetNpcCurrentActionRevision(npc, value);
        FieldInfo protocolField = typeof(SimulationRuntime).GetField(
            "npcRosterCensusProtocol", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(protocolField, Is.Not.Null);
        ContinuationCensusProtocol protocol = (ContinuationCensusProtocol)protocolField.GetValue(runtime);
        FieldInfo sectionsField = typeof(ContinuationCensusProtocol).GetField(
            "registeredSections", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(sectionsField, Is.Not.Null);
        IDictionary sections = sectionsField.GetValue(protocol) as IDictionary;
        Assert.That(sections, Is.Not.Null);
        string sectionId = NpcCurrentActionCensusProvider.SectionIdFor(npc.RuntimeId);
        Assert.That(sections.Contains(sectionId), Is.True);
        object section = sections[sectionId];
        FieldInfo baselineRevision = section.GetType().GetField("LastRevision", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(baselineRevision, Is.Not.Null);
        baselineRevision.SetValue(section, value);
    }

    private static long ReadNpcLifecycleRevision(
        SimulationRuntime runtime,
        string runtimeId,
        bool residence)
    {
        string sectionId = NpcLifecycleCensusProvider.SectionIdFor(runtimeId, residence);
        foreach (IOwnerSectionCensusProvider provider in NpcLifecycleCensusProvider.CreateProviders(runtime.NpcRuntimes))
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (witness.SectionId == sectionId) return witness.Revision;
        }
        Assert.Fail("Expected NPC lifecycle provider was not present: " + sectionId);
        return -1L;
    }

    private static long ReadPersonLifecycleRevision(SimulationRuntime runtime, PersonId personId)
    {
        string sectionId = PersonLifeResidenceCensusProvider.SectionIdFor(personId);
        foreach (IOwnerSectionCensusProvider provider in PersonLifeResidenceCensusProvider.CreateProviders(runtime.PersonStore.Persons))
        {
            OwnerSectionCensusWitness witness = provider.GetCurrentCensus();
            if (witness.SectionId == sectionId) return witness.Revision;
        }
        Assert.Fail("Expected Person lifecycle provider was not present: " + sectionId);
        return -1L;
    }

    private static IDisposable EnterDailyAdvanceOperation(SimulationRuntime runtime)
    {
        MethodInfo begin = typeof(SimulationRuntime).GetMethod(
            "TryEnterRuntimeAdmissionOperation", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(begin, Is.Not.Null);
        object[] arguments = { "runtime.advance-day", null };
        Assert.That(begin.Invoke(runtime, arguments), Is.EqualTo(true));
        IDisposable scope = arguments[1] as IDisposable;
        Assert.That(scope, Is.Not.Null);
        return scope;
    }

    private sealed class P12ActorChoiceFixture
    {
        public SimulationRuntime Runtime { get; }
        public RecordFixture Records { get; }
        public CityRuntime City { get; }
        public PersonRuntime Person { get; }
        public NpcRuntime Actor { get; }
        public ItemData Item { get; }
        public NpcActionData SellAction { get; }

        public P12ActorChoiceFixture(
            SimulationRuntime runtime,
            RecordFixture records,
            CityRuntime city,
            PersonRuntime person,
            NpcRuntime actor,
            ItemData item,
            NpcActionData sellAction)
        {
            Runtime = runtime;
            Records = records;
            City = city;
            Person = person;
            Actor = actor;
            Item = item;
            SellAction = sellAction;
        }
    }
}
