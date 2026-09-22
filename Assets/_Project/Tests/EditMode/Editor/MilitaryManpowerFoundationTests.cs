using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class MilitaryManpowerFoundationTests
{
    [Test]
    public void LegacyBootstrap_PreservesRosterAndManagedAmountCannotBypassAuthority()
    {
        ArmedForceStore forces = CreateForceStore(out ArmedForceId forceId);
        ContingentId contingentId = new ContingentId("legacy-cohort");
        Assert.That(forces.TryRegisterContingent(new ContingentRecord(
            contingentId, forceId, 8L, new ContingentOriginReference("legacy", "unknown"), "service"), out _), Is.True);

        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces);
        Assert.That(manpower.TryGet(contingentId, out ContingentManpowerState state), Is.True);
        Assert.That(state.SourceId, Is.Null);
        Assert.That(state.LivingRosterAmount, Is.EqualTo(8L));
        Assert.That(state.AvailableAmount, Is.EqualTo(8L));
        Assert.That(state.Cohorts, Has.Count.EqualTo(1));
        Assert.That(state.Cohorts[0].InjuryState, Is.EqualTo(ManpowerInjuryState.Healthy));
        Assert.That(state.Cohorts[0].CustodyState, Is.EqualTo(ManpowerCustodyState.Free));

        Assert.That(forces.TryReplaceContingent(new ContingentRecord(
            contingentId, forceId, 9L, new ContingentOriginReference("legacy", "unknown"), "service"),
            out ArmedForceFoundationFailure amountFailure), Is.False);
        Assert.That(amountFailure.Code, Is.EqualTo(ArmedForceFoundationFailureCode.ManpowerAmountMutationRequired));
        Assert.That(forces.TryRegisterContingent(new ContingentRecord(
            new ContingentId("bypass"), forceId, 1L, new ContingentOriginReference("x", "y"), "service"),
            out ArmedForceFoundationFailure registerFailure), Is.False);
        Assert.That(registerFailure.Code, Is.EqualTo(ArmedForceFoundationFailureCode.ManpowerAuthorityRequired));
        Assert.That(manpower.TryRegisterContingent(new ContingentRecord(
            new ContingentId("positive-registration"), forceId, 1L,
            new ContingentOriginReference("x", "y"), "service"),
            out ContingentManpowerFailure positiveFailure), Is.False);
        Assert.That(positiveFailure.Code, Is.EqualTo(ContingentManpowerFailureCode.InitialRosterMustBeAllocated));

        Assert.That(forces.TryReplaceContingent(new ContingentRecord(
            contingentId, forceId, 8L, new ContingentOriginReference("legacy", "unknown"), "service",
            new[] { new ArmedForceCharacteristic("unit", "updated") }), out _), Is.True);
        Assert.That(manpower.TryGet(contingentId, out state), Is.True);
        Assert.That(state.LivingRosterAmount, Is.EqualTo(8L));
    }

    [Test]
    public void LegacyBootstrap_PreservesRosterOnForceTerminatedBeforeManpowerWasAttached()
    {
        TestManpowerSourceProvider provider = new TestManpowerSourceProvider();
        ManpowerSourceId sourceId = new ManpowerSourceId("legacy-terminated-source");
        provider.Set(sourceId, 6L, 6L, "legacy-terminated:v1");
        ArmedForceStore forces = CreateForceStore(out ArmedForceId forceId);
        ContingentId contingentId = new ContingentId("legacy-terminated-roster");
        Assert.That(forces.TryRegisterContingent(new ContingentRecord(
            contingentId, forceId, 6L, new ContingentOriginReference("legacy", "terminated"), "service"), out _), Is.True);
        Assert.That(forces.TryTerminate(forceId, 1L, out _), Is.True,
            "The pre-D6A store allowed termination while a direct contingent roster remained.");

        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces, provider);

        Assert.That(manpower.TryGet(contingentId, out ContingentManpowerState state), Is.True);
        Assert.That(state.LivingRosterAmount, Is.EqualTo(6L));
        Assert.That(state.AvailableAmount, Is.EqualTo(6L));
        Assert.That(manpower.ValidateInvariants().Violations,
            Has.Some.Contains("retains direct living manpower"),
            "Composition preserves historical state; diagnostics still report the lifecycle inconsistency.");
        Assert.That(manpower.TrySetSourceBinding(contingentId, sourceId, 0L, "legacy-terminated:v1", out _), Is.True,
            "Explicit source binding can reconcile historical accounting without reactivating the force.");
        Assert.That(manpower.TryAllocate(contingentId, 1L, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available,
            1L, "legacy-terminated:v1", out ContingentManpowerFailure allocationFailure), Is.False);
        Assert.That(allocationFailure.Code, Is.EqualTo(ContingentManpowerFailureCode.ForceNotActive));
        Assert.That(manpower.TryDemobilize(contingentId, 6L, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available,
            1L, "legacy-terminated:v1", out _), Is.True);
        Assert.That(manpower.TrySetSourceBinding(contingentId, null, 2L, null, out _), Is.True);
        Assert.That(manpower.TryGet(contingentId, out state), Is.True);
        Assert.That(state.LivingRosterAmount, Is.Zero);
        Assert.That(state.SourceId, Is.Null);
    }

    [Test]
    public void Allocation_NormalizesCohortsAndKeepsInjuryCustodyAndAvailabilityIndependent()
    {
        TestManpowerSourceProvider provider = new TestManpowerSourceProvider();
        ManpowerSourceId sourceId = new ManpowerSourceId("pool-a");
        provider.Set(sourceId, 100L, 100L, "pool-a:v1");
        ArmedForceStore forces = CreateForceStore(out ArmedForceId forceId, out ArmedForceId custodianId);
        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces, provider);
        ContingentId contingentId = new ContingentId("roster-a");
        Assert.That(manpower.TryRegisterContingent(new ContingentRecord(
            contingentId, forceId, 0L, new ContingentOriginReference("source", "pool-a"), "service"), out _), Is.True);
        Assert.That(manpower.TrySetSourceBinding(contingentId, sourceId, 0L, "pool-a:v1", out _), Is.True);

        Assert.That(manpower.TryAllocate(contingentId, 50L, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available, 1L, "pool-a:v1", out _), Is.True);
        Assert.That(manpower.TryAllocate(contingentId, 20L, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available, 2L, "pool-a:v1", out _), Is.True);
        Assert.That(manpower.TryAllocate(contingentId, 20L, ManpowerInjuryState.Wounded,
            ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Unavailable, 3L, "pool-a:v1", out _), Is.True);
        Assert.That(manpower.TryAllocate(contingentId, 5L, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Captured, custodianId, ManpowerAvailabilityState.Unavailable, 4L, "pool-a:v1", out _), Is.True);
        Assert.That(manpower.TryAllocate(contingentId, 5L, ManpowerInjuryState.Wounded,
            ManpowerCustodyState.Captured, custodianId, ManpowerAvailabilityState.Unavailable, 5L, "pool-a:v1", out _), Is.True);

        Assert.That(manpower.TryGet(contingentId, out ContingentManpowerState state), Is.True);
        Assert.That(state.LivingRosterAmount, Is.EqualTo(100L));
        Assert.That(state.AvailableAmount, Is.EqualTo(70L));
        Assert.That(state.Cohorts, Has.Count.EqualTo(4));
        Assert.That(state.Cohorts[0].InjuryState, Is.EqualTo(ManpowerInjuryState.Healthy));
        Assert.That(state.Cohorts[0].CustodyState, Is.EqualTo(ManpowerCustodyState.Free));
        Assert.That(state.Cohorts[0].Amount, Is.EqualTo(70L), "Repeated semantic cohort keys are merged canonically.");
        Assert.That(forces.TryGetContingent(contingentId, out ContingentRecord record), Is.True);
        Assert.That(record.Amount, Is.EqualTo(state.LivingRosterAmount));
        Assert.That(forces.PersonStore.Persons, Is.Empty, "Aggregate manpower allocation does not create Person identities.");

        long storeRevision = manpower.Revision;
        long forceRevision = forces.Revision;
        Assert.That(manpower.TryAllocate(contingentId, 1L, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Captured, custodianId, ManpowerAvailabilityState.Available,
            state.Revision, "pool-a:v1", out ContingentManpowerFailure invalidCaptured), Is.False);
        Assert.That(invalidCaptured.Code, Is.EqualTo(ContingentManpowerFailureCode.CapturedCohortMustBeUnavailable));
        Assert.That(manpower.Revision, Is.EqualTo(storeRevision));
        Assert.That(forces.Revision, Is.EqualTo(forceRevision));
        Assert.That(manpower.TryDemobilize(contingentId, 1L, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Captured, custodianId, ManpowerAvailabilityState.Unavailable,
            state.Revision, "pool-a:v1", out ContingentManpowerFailure capturedDemobilization), Is.False);
        Assert.That(capturedDemobilization.Code, Is.EqualTo(ContingentManpowerFailureCode.CapturedRosterCannotBeDemobilized));
        Assert.That(manpower.ValidateInvariants().IsValid, Is.True);
    }

    [Test]
    public void AllocationAcrossContingents_UsesSourceCapacityAndFailureIsAtomic()
    {
        TestManpowerSourceProvider provider = new TestManpowerSourceProvider();
        ManpowerSourceId sourceId = new ManpowerSourceId("shared-pool");
        provider.Set(sourceId, 9L, 8L, "shared:v1");
        ArmedForceStore forces = CreateForceStore(out ArmedForceId forceId);
        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces, provider);
        ContingentId first = RegisterBoundEmpty(manpower, forceId, "first", sourceId, "shared:v1");
        ContingentId second = RegisterBoundEmpty(manpower, forceId, "second", sourceId, "shared:v1");

        Assert.That(manpower.TryAllocate(first, 5L, ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Available, 1L, "shared:v1", out _), Is.True);
        Assert.That(manpower.TryAllocate(second, 3L, ManpowerInjuryState.Wounded, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Unavailable, 1L, "shared:v1", out _), Is.True);
        Assert.That(manpower.TryGetSourceAllocation(sourceId, out long allocated, out _), Is.True);
        Assert.That(allocated, Is.EqualTo(8L));

        long manpowerRevision = manpower.Revision;
        long forceRevision = forces.Revision;
        Assert.That(manpower.TryAllocate(second, 2L, ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Available, 2L, "shared:v1", out ContingentManpowerFailure capacityFailure), Is.False);
        Assert.That(capacityFailure.Code, Is.EqualTo(ContingentManpowerFailureCode.SourceCapacityExceeded));
        Assert.That(manpower.Revision, Is.EqualTo(manpowerRevision));
        Assert.That(forces.Revision, Is.EqualTo(forceRevision));
        Assert.That(manpower.TryGet(second, out ContingentManpowerState secondState), Is.True);
        Assert.That(secondState.LivingRosterAmount, Is.EqualTo(3L));

        Assert.That(manpower.TryAllocate(first, 1L, ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Available, 2L, "shared:v1", out ContingentManpowerFailure factualFailure), Is.False);
        Assert.That(factualFailure.Code, Is.EqualTo(ContingentManpowerFailureCode.SourceFactualAmountExceeded));

        Assert.That(manpower.TryDemobilize(first, 2L, ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Available, 2L, "shared:v1", out ContingentManpowerFailure demobilizationFailure), Is.True, demobilizationFailure.ToString());
        Assert.That(manpower.TryGet(first, out ContingentManpowerState firstAfterDemobilization), Is.True);
        Assert.That(firstAfterDemobilization.LivingRosterAmount, Is.EqualTo(3L));
        Assert.That(manpower.TryGetSourceAllocation(sourceId, out allocated, out _), Is.True);
        Assert.That(allocated, Is.EqualTo(6L));
        Assert.That(provider.TryGetSnapshot(sourceId, out ManpowerSourceCapacitySnapshot unchangedSource), Is.True);
        Assert.That(unchangedSource.FactualLivingAmount, Is.EqualTo(8L), "Demobilization does not mutate source-domain population.");
    }

    [Test]
    public void BindingLegacyRosterRequiresExplicitCurrentCapacityAndCannotTransferNonemptyRoster()
    {
        TestManpowerSourceProvider provider = new TestManpowerSourceProvider();
        ManpowerSourceId sourceA = new ManpowerSourceId("binding-source-a");
        ManpowerSourceId sourceB = new ManpowerSourceId("binding-source-b");
        provider.Set(sourceA, 7L, 8L, "binding-a:v1");
        provider.Set(sourceB, 20L, 20L, "binding-b:v1");
        ArmedForceStore forces = CreateForceStore(out ArmedForceId forceId);
        ContingentId id = new ContingentId("legacy-to-bound");
        Assert.That(forces.TryRegisterContingent(new ContingentRecord(
            id, forceId, 8L, new ContingentOriginReference("not-a-source", "do-not-infer"), "service"), out _), Is.True);
        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces, provider);
        long revision = manpower.Revision;
        Assert.That(manpower.TrySetSourceBinding(id, sourceA, 0L, "binding-a:v1",
            out ContingentManpowerFailure capacityFailure), Is.False);
        Assert.That(capacityFailure.Code, Is.EqualTo(ContingentManpowerFailureCode.SourceCapacityExceeded));
        Assert.That(manpower.Revision, Is.EqualTo(revision));
        Assert.That(manpower.TryGet(id, out ContingentManpowerState unbound), Is.True);
        Assert.That(unbound.SourceId, Is.Null);

        provider.Set(sourceA, 8L, 8L, "binding-a:v2");
        Assert.That(manpower.TrySetSourceBinding(id, sourceA, 0L, "binding-a:v1",
            out ContingentManpowerFailure staleSource), Is.False);
        Assert.That(staleSource.Code, Is.EqualTo(ContingentManpowerFailureCode.StaleSourceSnapshot));
        Assert.That(manpower.TrySetSourceBinding(id, sourceA, 0L, "binding-a:v2", out _), Is.True);
        Assert.That(manpower.TrySetSourceBinding(id, sourceB, 1L, "binding-b:v1",
            out ContingentManpowerFailure transferFailure), Is.False);
        Assert.That(transferFailure.Code, Is.EqualTo(ContingentManpowerFailureCode.SourceBindingChangeRequiresEmptyRoster));
        Assert.That(manpower.TryGet(id, out ContingentManpowerState bound), Is.True);
        Assert.That(bound.SourceId, Is.EqualTo(sourceA));
        Assert.That(bound.LivingRosterAmount, Is.EqualTo(8L));
    }

    [Test]
    public void AllocationOverflow_IsRejectedWithoutPartialMirrorOrRosterMutation()
    {
        TestManpowerSourceProvider provider = new TestManpowerSourceProvider();
        ManpowerSourceId sourceId = new ManpowerSourceId("maximum-source");
        provider.Set(sourceId, long.MaxValue, long.MaxValue, "maximum:v1");
        ArmedForceStore forces = CreateForceStore(out ArmedForceId forceId);
        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces, provider);
        ContingentId first = RegisterBoundEmpty(manpower, forceId, "maximum-first", sourceId, "maximum:v1");
        ContingentId second = RegisterBoundEmpty(manpower, forceId, "maximum-second", sourceId, "maximum:v1");
        Assert.That(manpower.TryAllocate(first, long.MaxValue, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available, 1L, "maximum:v1", out _), Is.True);

        long forceRevision = forces.Revision;
        long manpowerRevision = manpower.Revision;
        Assert.That(manpower.TryAllocate(second, 1L, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available, 1L, "maximum:v1",
            out ContingentManpowerFailure overflow), Is.False);
        Assert.That(overflow.Code, Is.EqualTo(ContingentManpowerFailureCode.RosterAmountOverflow));
        Assert.That(forces.Revision, Is.EqualTo(forceRevision));
        Assert.That(manpower.Revision, Is.EqualTo(manpowerRevision));
        Assert.That(manpower.TryGet(second, out ContingentManpowerState untouched), Is.True);
        Assert.That(untouched.LivingRosterAmount, Is.Zero);
        Assert.That(forces.TryGetContingent(second, out ContingentRecord emptyRecord), Is.True);
        Assert.That(emptyRecord.Amount, Is.Zero);
    }

    [Test]
    public void AllocationChangesMilitaryRosterWithoutChangingPopulationOrPersonState()
    {
        ArmedForceStore forces = CreateForceStore(out ArmedForceId forceId);
        SettlementPopulationRuntime population = new SettlementPopulationRuntime("recruiting-settlement", 100);
        TestManpowerSourceProvider provider = new TestManpowerSourceProvider();
        ManpowerSourceId sourceId = new ManpowerSourceId("population-readonly-source");
        provider.Set(sourceId, 100L, population.CurrentPopulation, "population:v1");
        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces, provider);
        ContingentId contingentId = RegisterBoundEmpty(manpower, forceId, "population-allocation", sourceId, "population:v1");

        Assert.That(manpower.TryAllocate(contingentId, 10L, ManpowerInjuryState.Healthy,
            ManpowerCustodyState.Free, null, ManpowerAvailabilityState.Available,
            1L, "population:v1", out ContingentManpowerFailure failure), Is.True, failure?.ToString());

        Assert.That(population.CurrentPopulation, Is.EqualTo(100));
        Assert.That(population.Revision, Is.Zero);
        Assert.That(forces.PersonStore.Persons, Is.Empty);
        Assert.That(manpower.TryGet(contingentId, out ContingentManpowerState state), Is.True);
        Assert.That(state.LivingRosterAmount, Is.EqualTo(10L));
    }

    [Test]
    public void SourceFingerprintChangesAndDisappearanceAreExplicitAndReadOnly()
    {
        TestManpowerSourceProvider provider = new TestManpowerSourceProvider();
        ManpowerSourceId sourceId = new ManpowerSourceId("source-changing");
        provider.Set(sourceId, 20L, 12L, "source:v1");
        ArmedForceStore forces = CreateForceStore(out ArmedForceId forceId);
        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces, provider);
        ContingentId id = RegisterBoundEmpty(manpower, forceId, "source-bound", sourceId, "source:v1");
        Assert.That(manpower.TryAllocate(id, 5L, ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Available, 1L, "source:v1", out _), Is.True);

        provider.Set(sourceId, 4L, 4L, "source:v2");
        Assert.That(manpower.TryAllocate(id, 1L, ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Available, 2L, "source:v1", out ContingentManpowerFailure stale), Is.False);
        Assert.That(stale.Code, Is.EqualTo(ContingentManpowerFailureCode.StaleSourceSnapshot));
        Assert.That(manpower.TryGet(id, out ContingentManpowerState state), Is.True);
        Assert.That(state.LivingRosterAmount, Is.EqualTo(5L));
        Assert.That(manpower.TryDemobilize(id, 1L, ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Available, 2L, "source:v2", out _), Is.True,
            "Demobilization releases local allocation even when current source capacity is already below the roster.");
        Assert.That(manpower.TryGet(id, out state), Is.True);
        Assert.That(state.LivingRosterAmount, Is.EqualTo(4L));

        provider.Remove(sourceId);
        Assert.That(manpower.TryAllocate(id, 1L, ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Available, 3L, "source:v2", out ContingentManpowerFailure missing), Is.False);
        Assert.That(missing.Code, Is.EqualTo(ContingentManpowerFailureCode.SourceUnresolved));
        Assert.That(manpower.TryDemobilize(id, 1L, ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Available, 3L, "source:v2", out _), Is.True,
            "Demobilization only reduces the local allocation and remains available after the source disappears.");
        Assert.That(manpower.TryGet(id, out state), Is.True);
        Assert.That(state.SourceId, Is.EqualTo(sourceId));
        Assert.That(state.LivingRosterAmount, Is.EqualTo(3L));
        Assert.That(manpower.ValidateInvariants().Violations, Has.Some.Contains("unresolved"));
        Assert.That(manpower.TryDemobilize(id, 3L, ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Available, 4L, "source:v2", out _), Is.True);
        Assert.That(forces.TryTerminate(forceId, 1L, out _), Is.True,
            "Once the roster is explicitly released, the direct force termination guard clears.");
    }

    [Test]
    public void RuntimeClonesLegacyAndSuppliedManpowerStateOntoItsClonedForceStore()
    {
        PersonStore persons = new PersonStore();
        ArmedForceStore forces = new ArmedForceStore(persons);
        ArmedForceId forceId = new ArmedForceId("runtime-manpower-force");
        ContingentId contingentId = new ContingentId("runtime-manpower-contingent");
        Assert.That(forces.TryRegister(new ArmedForceRecord(forceId, "Runtime Force", 0L), out _), Is.True);
        Assert.That(forces.TryRegisterContingent(new ContingentRecord(
            contingentId, forceId, 3L, new ContingentOriginReference("legacy", "runtime"), "service"), out _), Is.True);
        ContingentManpowerStateStore sourceManpower = new ContingentManpowerStateStore(forces);
        Assert.That(sourceManpower.TryRedistribute(contingentId, new[]
        {
            new ContingentManpowerCohort(ManpowerInjuryState.Wounded, ManpowerCustodyState.Free,
                null, ManpowerAvailabilityState.Available, 3L)
        }, 0L, out _), Is.True);

        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(0L), null, null,
            economyEnabled: false,
            personStore: persons,
            armedForceStore: forces,
            contingentManpowerStateStore: sourceManpower);

        Assert.That(runtime.ArmedForceStore, Is.Not.SameAs(forces));
        Assert.That(runtime.ContingentManpowerStateStore.ArmedForceStore, Is.SameAs(runtime.ArmedForceStore));
        Assert.That(runtime.ContingentManpowerStateStore, Is.Not.SameAs(sourceManpower));
        Assert.That(runtime.ContingentManpowerStateStore.TryGet(contingentId, out ContingentManpowerState runtimeState), Is.True);
        Assert.That(runtimeState.LivingRosterAmount, Is.EqualTo(3L));
        Assert.That(runtimeState.AvailableAmount, Is.EqualTo(3L));
        Assert.That(runtimeState.Revision, Is.EqualTo(1L));
        Assert.That(runtimeState.SourceId, Is.Null, "Legacy bootstrap must not infer a source from provenance.");

        SimulationRuntime defaultBootstrapRuntime = new SimulationRuntime(
            new SimulationTime(0L), null, null,
            economyEnabled: false,
            personStore: persons,
            armedForceStore: forces);
        Assert.That(defaultBootstrapRuntime.ContingentManpowerStateStore.TryGet(contingentId, out ContingentManpowerState defaultState), Is.True);
        Assert.That(defaultState.LivingRosterAmount, Is.EqualTo(3L));
        Assert.That(defaultState.AvailableAmount, Is.EqualTo(3L));
        Assert.That(defaultState.SourceId, Is.Null);

        Assert.That(sourceManpower.TryRedistribute(contingentId, new[]
        {
            new ContingentManpowerCohort(ManpowerInjuryState.Wounded, ManpowerCustodyState.Free,
                null, ManpowerAvailabilityState.Unavailable, 3L)
        }, 1L, out _), Is.True);
        Assert.That(runtime.ContingentManpowerStateStore.TryGet(contingentId, out runtimeState), Is.True);
        Assert.That(runtimeState.AvailableAmount, Is.EqualTo(3L));
        Assert.That(runtimeState.Revision, Is.EqualTo(1L));
        Assert.That(runtime.ArmedForceStore.TryReplaceContingent(new ContingentRecord(
            contingentId, forceId, 4L, new ContingentOriginReference("legacy", "runtime"), "service"),
            out ArmedForceFoundationFailure bypass), Is.False);
        Assert.That(bypass.Code, Is.EqualTo(ArmedForceFoundationFailureCode.ManpowerAmountMutationRequired));
        Assert.That(persons.Persons, Is.Empty);
    }

    [Test]
    public void RedistributionPreservesRosterAndTerminationHonorsDirectRosterAndCustody()
    {
        ArmedForceStore forces = CreateForceStore(out ArmedForceId forceId, out ArmedForceId custodianId);
        ContingentId id = new ContingentId("custody-roster");
        Assert.That(forces.TryRegisterContingent(new ContingentRecord(
            id, forceId, 10L, new ContingentOriginReference("legacy", "unit"), "service"), out _), Is.True);
        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces);

        Assert.That(forces.TryTerminate(forceId, 1L, out ArmedForceFoundationFailure direct), Is.False);
        Assert.That(direct.Code, Is.EqualTo(ArmedForceFoundationFailureCode.ForceHasManagedManpower));
        Assert.That(manpower.TryRedistribute(id, new[]
        {
            new ContingentManpowerCohort(ManpowerInjuryState.Healthy, ManpowerCustodyState.Captured,
                custodianId, ManpowerAvailabilityState.Unavailable, 10L)
        }, 0L, out _), Is.True);
        Assert.That(forces.TryTerminate(custodianId, 1L, out ArmedForceFoundationFailure custody), Is.False);
        Assert.That(custody.Code, Is.EqualTo(ArmedForceFoundationFailureCode.ForceCustodiesManagedManpower));
        Assert.That(manpower.TryRedistribute(id, new[]
        {
            new ContingentManpowerCohort(ManpowerInjuryState.Wounded, ManpowerCustodyState.Free,
                null, ManpowerAvailabilityState.Unavailable, 10L)
        }, 1L, out _), Is.True);
        Assert.That(forces.TryTerminate(custodianId, 1L, out _), Is.True);
        Assert.That(manpower.TryGet(id, out ContingentManpowerState state), Is.True);
        Assert.That(state.LivingRosterAmount, Is.EqualTo(10L));
        Assert.That(state.AvailableAmount, Is.EqualTo(0L));

        Assert.That(manpower.TryRedistribute(id, Array.Empty<ContingentManpowerCohort>(), 2L,
            out ContingentManpowerFailure rosterFailure), Is.False);
        Assert.That(rosterFailure.Code, Is.EqualTo(ContingentManpowerFailureCode.RedistributionChangesLivingRoster));
    }

    [Test]
    public void Diagnostics_CaptureManpowerFingerprintCapacityAndDiff()
    {
        TestManpowerSourceProvider provider = new TestManpowerSourceProvider();
        ManpowerSourceId sourceId = new ManpowerSourceId("diagnostic-source");
        provider.Set(sourceId, 12L, 12L, "diag:v1");
        ArmedForceStore forces = CreateForceStore(out ArmedForceId forceId);
        ContingentManpowerStateStore manpower = new ContingentManpowerStateStore(forces, provider);
        ContingentId id = RegisterBoundEmpty(manpower, forceId, "diagnostic-contingent", sourceId, "diag:v1");
        Assert.That(manpower.TryAllocate(id, 5L, ManpowerInjuryState.Healthy, ManpowerCustodyState.Free,
            null, ManpowerAvailabilityState.Available, 1L, "diag:v1", out _), Is.True);

        WorldStateSnapshot before = Capture(forces, manpower);
        Assert.That(WorldStateDiagnostics.Validate(before).IsValid, Is.True);
        string canonical = WorldStateCanonicalWriter.Write(before);
        Assert.That(canonical, Does.Contain("CONTINGENT_MANPOWER|diagnostic-contingent|diagnostic-source|"));
        Assert.That(canonical, Does.Contain("CONTINGENT_MANPOWER_COHORT|diagnostic-contingent|Healthy|Free|"));

        provider.Set(sourceId, 4L, 4L, "diag:v2");
        WorldStateSnapshot belowCapacity = Capture(forces, manpower);
        Assert.That(WorldStateDiagnostics.Validate(belowCapacity).Errors, Has.Some.Matches<WorldStateInvariantIssue>(issue =>
            issue.Code == "ManpowerSourceCapacityExceeded"));

        Assert.That(manpower.TryRedistribute(id, new[]
        {
            new ContingentManpowerCohort(ManpowerInjuryState.Wounded, ManpowerCustodyState.Free,
                null, ManpowerAvailabilityState.Unavailable, 5L)
        }, 2L, out _), Is.True);
        WorldStateSnapshot after = Capture(forces, manpower);
        WorldStateDiff diff = WorldStateDiagnostics.Compare(before, after);
        Assert.That(diff.Differences, Has.Some.Matches<WorldStateDifference>(d =>
            d.Section == "ContingentManpower" && d.Identity == id.Value && d.Field == "Fingerprint"));
    }

    private static ContingentId RegisterBoundEmpty(
        ContingentManpowerStateStore manpower,
        ArmedForceId forceId,
        string id,
        ManpowerSourceId sourceId,
        string sourceFingerprint)
    {
        ContingentId contingentId = new ContingentId(id);
        Assert.That(manpower.TryRegisterContingent(new ContingentRecord(
            contingentId, forceId, 0L, new ContingentOriginReference("explicit", id), "service"), out _), Is.True);
        Assert.That(manpower.TrySetSourceBinding(contingentId, sourceId, 0L, sourceFingerprint, out _), Is.True);
        return contingentId;
    }

    private static ArmedForceStore CreateForceStore(out ArmedForceId forceId, out ArmedForceId custodianId)
    {
        ArmedForceStore store = new ArmedForceStore(new PersonStore());
        forceId = new ArmedForceId("force-manpower");
        custodianId = new ArmedForceId("force-custodian");
        Assert.That(store.TryRegister(new ArmedForceRecord(forceId, "Manpower", 0L), out _), Is.True);
        Assert.That(store.TryRegister(new ArmedForceRecord(custodianId, "Custodian", 0L), out _), Is.True);
        return store;
    }

    private static ArmedForceStore CreateForceStore(out ArmedForceId forceId)
        => CreateForceStore(out forceId, out _);

    private static WorldStateSnapshot Capture(ArmedForceStore forces, ContingentManpowerStateStore manpower)
        => WorldStateSnapshotBuilder.BuildSnapshot(new WorldStateSnapshotContext(
            simulationTime: new SimulationTime(0L),
            personStore: forces.PersonStore,
            armedForceStore: forces,
            contingentManpowerStateStore: manpower));

    private sealed class TestManpowerSourceProvider : IManpowerSourceSnapshotProvider
    {
        private readonly Dictionary<string, ManpowerSourceCapacitySnapshot> snapshots =
            new Dictionary<string, ManpowerSourceCapacitySnapshot>(StringComparer.Ordinal);

        public bool TryGetSnapshot(ManpowerSourceId sourceId, out ManpowerSourceCapacitySnapshot snapshot)
        {
            snapshot = null;
            return sourceId != null && snapshots.TryGetValue(sourceId.Value, out snapshot);
        }

        public void Set(ManpowerSourceId sourceId, long capacity, long? factualLivingAmount, string fingerprint)
            => snapshots[sourceId.Value] = new ManpowerSourceCapacitySnapshot(sourceId, capacity, factualLivingAmount, fingerprint);

        public void Remove(ManpowerSourceId sourceId) => snapshots.Remove(sourceId.Value);
    }
}
